using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Primitives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DcmFind;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var app = new CommandApp<FindCommand>();
        return await app.RunAsync(args);
    }
}

public class FindCommand : AsyncCommand<FindCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-d|--directory")]
        [Description("Search for *.dcm files in this directory")]
        [DefaultValue(".")]
        public string? Directory { get; init; }

        [CommandOption("-f|--file-pattern")]
        [Description("Only query files that satisfy this file pattern")]
        [DefaultValue("*")]
        public string? FilePattern { get; init; }

        [CommandOption("-r|--recursive")]
        [Description("Search recursively in nested directories")]
        [DefaultValue(true)]
        public bool? Recursive { get; init; }

        [CommandOption("-l|--limit")]
        [Description("Limit results and stop finding after this many results")]
        public int? Limit { get; init; }

        [CommandOption("-q|--query <QUERY>")]
        [Description("The query that should be applied")]
        public string[]? Query { get; init; }

        [CommandOption("-p|--parallelism")]
        [Description("Parse files in parallel")]
        [DefaultValue(8)]
        public int? Parallelism { get; init; }

        [CommandOption("--progress")]
        [Description("Show progress")]
        [DefaultValue(false)]
        public bool? Progress { get; init; }

        public override ValidationResult Validate()
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                return ValidationResult.Error($"Directory {Directory} does not exist");
            }

            foreach (var query in Query ?? Enumerable.Empty<string>())
            {
                if (!QueryParser.TryParse(query, out _))
                {
                    return ValidationResult.Error($"Query {query} could not be parsed");
                }

                return base.Validate();
            }

            if (FilePattern == null || string.IsNullOrEmpty(FilePattern))
            {
                return ValidationResult.Error("File pattern is empty");
            }

            return base.Validate();
        }
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Disposal only happens after all tasks are completed")]
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var directory = new DirectoryInfo(settings.Directory!).FullName;
        var filePattern = settings.FilePattern!;
        var recursive = settings.Recursive ?? true;
        var query = settings.Query;
        var limit = settings.Limit ?? int.MaxValue;
        var parallelism = settings.Parallelism ?? 8;
        var progress = settings.Progress ?? false;
        var now = DateTime.UtcNow;
        var allTasks = new List<Task>();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        // Setup channels
        var filesChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(parallelism * 100)
        {
            SingleWriter = true,
            SingleReader = false,
            AllowSynchronousContinuations = false
        });
        var matchedFilesChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });
        var consoleOutputChannel = Channel.CreateBounded<ConsoleOutput>(new BoundedChannelOptions(parallelism * 100)
        {
            SingleWriter = false,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });
        // Parse query
        var queries = new List<IQuery>();
        foreach (var q in settings.Query ?? Array.Empty<string>())
        {
            if (!QueryParser.TryParse(q, out var parsedQuery) || parsedQuery == null)
            {
                throw new ArgumentException($"Query cannot be parsed: {query}");
            }

            queries.Add(parsedQuery);
        }
        
        // Enumerate files
        var files = Files(directory, filePattern, recursive);

        var filesTask = Task.Run(async () =>
        {
            try
            {
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);

                    // Exclude files that were created after starting DcmFind
                    if (fileInfo.CreationTimeUtc >= now)
                    {
                        continue;
                    }

                    await filesChannel.Writer.WriteAsync(file, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            finally
            {
                // Close files channel when all files were enumerated, or when the operation is canceled
                filesChannel.Writer.Complete();
            }
        }, cancellationToken);
        allTasks.Add(filesTask);

        // Match files against query
        var matchTasks = new List<Task>(parallelism);
        for (var i = 0; i < parallelism; i++)
        {
            var matchTask = Task.Run(async () =>
            {
                try
                {
                    while (await filesChannel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        while (filesChannel.Reader.TryRead(out string? file))
                        {
                            if (progress)
                            {
                                consoleOutputChannel.Writer.TryWrite(new ConsoleOutput(file, true));
                            }
                            
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            var dicomFile = await ToDicomFileAsync(file);

                            if (dicomFile == null)
                            {
                                continue;
                            }

                            if (await MatchesAsync(file, queries))
                            {
                                await matchedFilesChannel.Writer.WriteAsync(file, cancellationToken);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignored
                }
            }, cancellationToken);
            matchTasks.Add(matchTask);
            allTasks.Add(matchTask);
        }
        var matchingFinishedTask = Task.Run(async () =>
        {
            // Close matched files channel when all matchers have completed
            await Task.WhenAll(matchTasks);
            matchedFilesChannel.Writer.Complete();
        }, cancellationToken);
        allTasks.Add(matchingFinishedTask);

        // Collect output
        var numberOfMatchedFiles = 0;
        var writeMatchedFilesToOutputTask = Task.Run(async () =>
        {
            try
            {
                while (await matchedFilesChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (matchedFilesChannel.Reader.TryRead(out string? file))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        numberOfMatchedFiles++;
                        if (numberOfMatchedFiles <= limit)
                        {
                            await consoleOutputChannel.Writer.WriteAsync(new ConsoleOutput(file, false), cancellationToken);
                        }
                        else
                        {
                            // If limit has been reached, bail early and stop all earlier stages
                            cts.Cancel();
                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            finally
            {
                consoleOutputChannel.Writer.Complete();
            }
        }, cancellationToken);
        allTasks.Add(writeMatchedFilesToOutputTask);

        // Write output to Console task
        var writeOutputTask = Task.Run(async () =>
        {
            try
            {
                string previousStringToWrite = "";
                bool overwritePreviousString = false;
                var outputToWrite = new StringBuilder();
                while (await consoleOutputChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (consoleOutputChannel.Reader.TryRead(out var output))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var stringToWrite = output.StringToWrite;
                        var overwrite = output.Overwrite;

                        if (overwritePreviousString)
                        {
                            // Print carriage return to move cursor back to begin
                            outputToWrite.Append('\r');
                            // Write new output
                            outputToWrite.Append(stringToWrite);
                            // If new string is shorter than previous string, overwrite with spaces
                            outputToWrite.Append(' ', Math.Max(previousStringToWrite.Length - stringToWrite.Length, 0));
                        }
                        else
                        {
                            outputToWrite.Append(stringToWrite);
                        }

                        // If we're not going to overwrite this string, immediately append a carriage return
                        if (!overwrite)
                        {
                            outputToWrite.Append("\r\n");
                        }
                        
                        Console.Write(outputToWrite.ToString());

                        outputToWrite.Clear();

                        overwritePreviousString = overwrite;
                        previousStringToWrite = stringToWrite;
                    }
                }

                if (overwritePreviousString)
                {
                    // Print carriage return to move cursor back to begin
                    outputToWrite.Append('\r');
                    // Overwrite last content with spaces
                    outputToWrite.Append(' ', previousStringToWrite.Length);
                    Console.WriteLine(outputToWrite.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }, cancellationToken);
        
        allTasks.Add(writeOutputTask);

        await Task.WhenAll(allTasks);

        return 0;
    }

    private static IEnumerable<string> Files(string directory, string filePattern, bool recursive)
    {
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = recursive,
            MatchCasing = MatchCasing.CaseInsensitive,
            MatchType = MatchType.Simple,
        };
        return Directory.EnumerateFiles(directory, filePattern, enumerationOptions);
    }

    private static async Task<DicomFile?> ToDicomFileAsync(string file)
    {
        if (string.IsNullOrEmpty(file))
            return null;

        try
        {
            var dicomFile = await DicomFile.OpenAsync(file);

            if (dicomFile == null || dicomFile.Format != DicomFileFormat.DICOM3) return null;

            if (dicomFile.Format == DicomFileFormat.Unknown) return null;

            return dicomFile;
        }
        catch (DicomFileException)
        {
            return null;
        }
    }

    private static async Task<bool> MatchesAsync(string file, List<IQuery> queries)
    {
        var dicomFile = await ToDicomFileAsync(file);

        if (dicomFile == null)
        {
            return false;
        }

        foreach (var query in queries)
        {
            if (!query.Matches(dicomFile.Dataset) && !query.Matches(dicomFile.FileMetaInfo))
            {
                return false;
            }
        }

        return true;
    }
}
