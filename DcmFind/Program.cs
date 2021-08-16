using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Dicom;

namespace DcmFind
{
    public static class Program
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable ClassNeverInstantiated.Global
        public class Options
        {
            [Option('d', "directory", Default = ".", HelpText = "Search for *.dcm files in this directory")]

            public string? Directory { get; set; }

            [Option('f', "filePattern", Default = "*", HelpText = "Only query files that satisfy this file pattern")]
            public string? FilePattern { get; set; }

            [Option('r', "recursive", Default = true, HelpText = "Search recursively in nested directories")]
            public bool Recursive { get; set; }

            [Option('l', "limit", HelpText = "Limit results and stop finding after this many results")]
            public int? Limit { get; set; }

            [Option(shortName: 'q', longName: "query", Required = false,  HelpText = "The query that should be applied")]
            public IEnumerable<string>? Query { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore ClassNeverInstantiated.Global

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Query)
                .WithNotParsed(Fail);
        }

        private static void Fail(IEnumerable<Error> errors)
        {
            Console.Error.WriteLine("Invalid arguments provided");
            foreach (var error in errors.Where(e => e.Tag != ErrorType.HelpRequestedError))
            {
                Console.Error.WriteLine(error.ToString());
            }
        }

        private static void Query(Options options)
        {
            var directory = new DirectoryInfo(options.Directory).FullName;
            var filePattern = options.FilePattern;
            var recursive = options.Recursive;
            var query = options.Query;
            var limit = options.Limit;
            if (filePattern == null || query == null)
                return;

            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Invalid directory: {directory} does not exist");
                return;
            }

            var files = Files(directory, filePattern, recursive);
            // ReSharper disable PossibleMultipleEnumeration
            if (!files.Any())
            {
                Console.WriteLine($"No files found that match {filePattern} in {directory}");
                return;
            }

            var queries = new List<IQuery>();
            foreach (var q in options.Query ?? Array.Empty<string>())
            {
                if (!QueryParser.TryParse(q, out var parsedQuery) || parsedQuery == null)
                {
                    Console.Error.WriteLine($"Invalid query: {query}");
                    return;
                }

                queries.Add(parsedQuery);
            }

            foreach (var result in Results(files, queries, limit))
                Console.WriteLine(result);
            // ReSharper restore PossibleMultipleEnumeration
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

        private static DicomFile? ToDicomFile(string file)
        {
            if (string.IsNullOrEmpty(file))
                return null;
            
            try
            {
                var dicomFile = DicomFile.Open(file);

                if (dicomFile.Format == DicomFileFormat.Unknown) return null;

                return dicomFile;
            }
            catch (DicomFileException)
            {
                return null;
            }
        }

        private static IEnumerable<string?> Results(IEnumerable<string> files, List<IQuery> queries, int? limit)
        {
            var results = files
                .Select(ToDicomFile)
                .Where(f => f != null)
                .Where(f => queries.All(q => q.Matches(f.Dataset) || q.Matches(f.FileMetaInfo)))
                .Select(f => f?.File?.Name)
                .Where(fileName => fileName != null);

            if (limit != null)
                results = results.Take(limit.Value);

            return results;
        }
    }
}