using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DcmFind;

public static class FileEnumerator
{
    public static async Task EnumerateAsync(ChannelWriter<string> output, string directory, string filePattern,
        bool recursive, CancellationToken cancellationToken)
    {
        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = recursive,
            MatchCasing = MatchCasing.CaseInsensitive,
            MatchType = MatchType.Simple,
        };
        var files = Directory.EnumerateFiles(directory, filePattern, enumerationOptions);
        var now = DateTime.Now;
        try
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileInfo = new FileInfo(file);

                // Exclude files that were created after starting DcmFind
                if (fileInfo.CreationTimeUtc >= now)
                {
                    continue;
                }

                await output.WriteAsync(file, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
        finally
        {
            // Close files channel when all files were enumerated, or when the operation is canceled
            output.Complete();
        }
    }
}
