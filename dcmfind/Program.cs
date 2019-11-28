using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Dicom;

namespace dcmfind
{
    public static class Program
    {
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
        
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Query)
                .WithNotParsed(Fail);
        }

        private static void Fail(IEnumerable<Error> errors)
        {
            Console.Error.WriteLine("Invalid arguments provided");
            foreach (var error in errors)
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
            if (!files.Any())
            {
                Console.WriteLine($"No files found that match {filePattern} in {directory}");
                return;
            }
            
            var query = options.Query;
            if (!query.Contains("="))
            {
                Console.Error.WriteLine($"Query '{query}' does not have a '=' in it");
                return;
            }

            var queryParts = query.Split('=');
            if (queryParts.Length != 2)
            {
                Console.Error.WriteLine($"Query '{query}' has more than 1 '=' in it, which is not allowed");
                return;
            }

            DicomTag queryDicomTag = ToDicomTag(queryParts[0]);
            if (queryDicomTag == null)
            {
                return;
            }

            var queryValue = queryParts[1];
            var limit = options.Limit;
            
            foreach(var result in Results(files, queryDicomTag, queryValue, limit))
                Console.WriteLine(result);
        }

        private static IEnumerable<string> Files(string directory, string filePattern, bool recursive)
        {
            return Directory.EnumerateFiles(directory, filePattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private static DicomTag ToDicomTag(string dicomTag)
        {
            try
            {
                if(dicomTag[0] == '(' || char.IsDigit(dicomTag[0]))
                    return DicomTag.Parse(dicomTag);

                var field = typeof(DicomTag).GetFields(BindingFlags.Static | BindingFlags.Public)
                    .Where(f => f.FieldType == typeof(DicomTag))
                    .FirstOrDefault(f => string.Equals(f.Name, dicomTag));
                if (field != null)
                {
                    return (DicomTag) field.GetValue(null);
                }

                return DicomTag.Parse(dicomTag);
            }
            catch (DicomDataException e)
            {
                Console.Error.WriteLine($"Invalid DICOM tag in query '{dicomTag}': " + e.Message);
                Console.Error.WriteLine(e);
                return null;
            }
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

        private static Func<DicomFile, bool> Matches(DicomTag queryDicomTag, string queryValue)
        {
            return dicomFile =>
            {
                try
                {
                    
                    var queryDicomTagValue = dicomFile.Dataset.GetString(queryDicomTag);
                    var matches = string.Equals(queryDicomTagValue, queryValue, StringComparison.OrdinalIgnoreCase);
                    //Console.WriteLine($"{queryDicomTagValue} matches {queryValue} ? {matches}");
                    return matches;
                }
                catch
                {
                    return false;
                }
            };
        }
        
        private static bool NotNull(DicomFile dicomFile)
        {
            return dicomFile != null;
        }
        
        private static string FileName(DicomFile dicomFile)
        {
            return dicomFile.File.Name;
        }

        private static IEnumerable<string> Results(IEnumerable<string> files, DicomTag queryDicomTag, string queryValue, int limit)
        {
            return files
                .Select(ToDicomFile)
                .Where(NotNull)
                .Where(Matches(queryDicomTag, queryValue))
                .Take(limit)
                .Select(FileName);
        }
    }
}