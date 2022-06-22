using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
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

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        await foreach (var file in FindAsync(settings))
        {
            Console.WriteLine(file);
        }

        return 0;
    }

    private static async IAsyncEnumerable<string> FindAsync(Settings settings)
    {
        var directory = new DirectoryInfo(settings.Directory!).FullName;
        var filePattern = settings.FilePattern!;
        var recursive = settings.Recursive ?? true;
        var query = settings.Query;
        var limit = settings.Limit ?? int.MaxValue;
        
        var files = Files(directory, filePattern, recursive);

        var queries = new List<IQuery>();
        foreach (var q in settings.Query ?? Array.Empty<string>())
        {
            if (!QueryParser.TryParse(q, out var parsedQuery) || parsedQuery == null)
            {
                throw new ArgumentException($"Query cannot be parsed: {query}");
            }

            queries.Add(parsedQuery);
        }


        int numberOfResults = 0;
        await foreach(var dicomFile in ResultsAsync(files, queries))
        {
            yield return dicomFile.File.Name;
            
            numberOfResults++;
            if (numberOfResults >= limit)
                yield break;
        }
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

            if (dicomFile == null) return null;

            if (dicomFile.Format == DicomFileFormat.Unknown) return null;

            return dicomFile;
        }
        catch (DicomFileException)
        {
            return null;
        }
    }

    private static async IAsyncEnumerable<DicomFile> ResultsAsync(IEnumerable<string> files, List<IQuery> queries)
    {
        foreach (var file in files)
        {
            var dicomFile = await ToDicomFileAsync(file);

            if (dicomFile == null)
            {
                continue;
            }

            if (queries.All(q => q.Matches(dicomFile.Dataset) || q.Matches(dicomFile.FileMetaInfo)))
            {
                yield return dicomFile;
            }
        }
    }
}
