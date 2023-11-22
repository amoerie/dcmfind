using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DcmFind;

public static class MatchedDicomFilesConsoleOutputWriter
{
    public static async Task WriteAsync(ChannelReader<string> input, ChannelWriter<ConsoleOutput> output, int limit, CancellationToken cancellationToken)
    {
        var numberOfMatches = 0;
        try
        {
            while (await input.WaitToReadAsync(cancellationToken))
            {
                while (input.TryRead(out string? file))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    numberOfMatches++;
                    if (numberOfMatches <= limit)
                    {
                        await output.WriteAsync(new ConsoleOutput(file, false), cancellationToken);
                    }
                    else
                    {
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
            output.Complete();
        }
    }
}
