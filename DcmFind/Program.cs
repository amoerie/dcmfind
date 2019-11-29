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
            
            public string Directory { get; set; }
            
            [Option('f', "filePattern", Default = "*.dcm", HelpText = "Only query files that satisfy this file pattern")]
            public string FilePattern { get; set; }
            
            [Option('r', "recursive", Default = true, HelpText = "Search recursively in nested directories")]
            public bool Recursive { get; set; }
            
            [Option('l', "limit", Default = 100, HelpText = "Limit results and stop finding after this many results")]
            public int Limit { get; set; }
            
            [Option(shortName: 'q', longName: "query", Default = "", Required = true, HelpText = "The query that should be applied")]
            public string Query { get; set; }
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
            
            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Invalid directory: {directory} does not exist");
                return;
            }

            var filePattern = options.FilePattern;
            var recursive = options.Recursive;
            var files = Files(directory, filePattern, recursive);
            // ReSharper disable PossibleMultipleEnumeration
            if (!files.Any())
            {
                Console.WriteLine($"No files found that match {filePattern} in {directory}");
                return;
            }

            if (!QueryParser.TryParse(options.Query, out var query))
            {
                Console.Error.WriteLine($"Invalid query: {query}");
                return;
            }

            foreach(var result in Results(files, query, options.Limit))
                Console.WriteLine(result);
            // ReSharper restore PossibleMultipleEnumeration
        }

        private static IEnumerable<string> Files(string directory, string filePattern, bool recursive)
        {
            return Directory.EnumerateFiles(directory, filePattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private static DicomFile ToDicomFile(string file)
        {
            try
            {
                return DicomFile.Open(file);
            }
            catch (DicomFileException e)
            {
                Console.Error.WriteLine($"{file} could not be opened as a DICOM file: " + e.Message);
                Console.Error.WriteLine(e);
                return null;
            }
        }
        
        private static bool NotNull(DicomFile dicomFile)
        {
            return dicomFile != null;
        }
        
        private static string FileName(DicomFile dicomFile)
        {
            return dicomFile.File.Name;
        }

        private static IEnumerable<string> Results(IEnumerable<string> files, IQuery query, int limit)
        {
            return files
                .Select(ToDicomFile)
                .Where(NotNull)
                .Where(f => query.Matches(f.Dataset))
                .Take(limit)
                .Select(FileName);
        }
    }
}