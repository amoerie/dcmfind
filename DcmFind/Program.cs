using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DcmFind;

public sealed record ProgramOptions(bool IgnoreRedirectedOutput, int ConsoleWindowWidth);

public class Program
{
    private readonly ProgramOptions _options;

    public Program()
    {
        _options = new ProgramOptions(false, Console.WindowWidth);
    }

    public Program(ProgramOptions options)
    {
        _options = options;
    }

    public static Task<int> Main(string[] args)
    {
        return new Program().MainAsync(args);
    }
    
    public async Task<int> MainAsync(string[] args)
    {
        var app = new CommandApp<FindCommand>();
        app.Configure(configurator => configurator.Settings.Registrar.RegisterInstance(_options));
        return await app.RunAsync(args);
    }
}

public class FindCommand : AsyncCommand<FindCommand.Settings>
{
    private readonly ProgramOptions _options;

    // Needed for testing
    public static bool IgnoreRedirectedConsoleOutput = false;

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

        [CommandOption("--no-progress")]
        [Description("Disables printing the file currently being inspected. Automatically disabled if the output is piped.")]
        public bool? NoProgress { get; init; }

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

    public FindCommand(ProgramOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
        var progress = settings.NoProgress != true && (_options.IgnoreRedirectedOutput || !Console.IsOutputRedirected);
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
        var fileEnumeratorTask = Task.Run(
            async () => await FileEnumerator.EnumerateAsync(filesChannel.Writer, directory, filePattern, recursive, cancellationToken), 
            cancellationToken
        );
        allTasks.Add(fileEnumeratorTask);

        // Match files against query
        var matchTasks = new List<Task>(parallelism);
        for (var i = 0; i < parallelism; i++)
        {
            var matchTask = Task.Run(
                async () => await DicomFileMatcher.MatchAsync(
                    filesChannel.Reader,
                    matchedFilesChannel.Writer,
                    progress, 
                    consoleOutputChannel.Writer,
                    queries, 
                    cancellationToken
                ),
                cancellationToken);
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
        var writeMatchedFilesToOutputTask = Task.Run(
            async () => await MatchedDicomFilesConsoleOutputWriter.WriteAsync(
                matchedFilesChannel.Reader, 
                consoleOutputChannel.Writer,
                limit,
                cancellationToken
            )
        , cancellationToken);
        allTasks.Add(writeMatchedFilesToOutputTask);

        // Write output to Console task
        var writeOutputTask = Task.Run(async () =>
        {
            await ConsoleOutputWriter.WriteAsync(consoleOutputChannel.Reader, _options, cancellationToken);
        }, cancellationToken);
        
        allTasks.Add(writeOutputTask);

        await Task.WhenAll(allTasks);

        return 0;
    }
}
