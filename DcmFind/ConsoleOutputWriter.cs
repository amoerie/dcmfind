using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DcmFind;

public static class ConsoleOutputWriter
{
    public static async Task WriteAsync(ChannelReader<ConsoleOutput> input, ProgramOptions options, CancellationToken cancellationToken)
    {
        try
        {
            ConsoleOutput? previous = null;
            var outputToWrite = new StringBuilder();
            
            while (await input.WaitToReadAsync(cancellationToken))
            {
                while (input.TryRead(out var current))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (current.Overwrite && current.StringToWrite.Length > options.ConsoleWindowWidth)
                    {
                        current = new ConsoleOutput(
                            current.StringToWrite.Substring(0, options.ConsoleWindowWidth),
                            current.Overwrite);
                    }

                    // Overwrite previous string?
                    if (previous is { Overwrite: true })
                    {
                        // Move cursor back to beginning
                        outputToWrite.Append('\r');

                        // Write new output
                        outputToWrite.Append(current.StringToWrite);
                        
                        // If new string is shorter than previous string, overwrite with spaces
                        if (current.StringToWrite.Length < previous.Value.StringToWrite.Length)
                        {
                            outputToWrite.Append(' ', previous.Value.StringToWrite.Length - current.StringToWrite.Length);    
                        }
                    }
                    else
                    {
                        outputToWrite.Append(current.StringToWrite);
                    }

                    if (!current.Overwrite)
                    {
                        // If we're not going to overwrite this string, immediately append a new line
                        outputToWrite.Append(Environment.NewLine);
                    }

                    Console.ForegroundColor = current.Overwrite
                        ? ConsoleColor.Yellow
                        : ConsoleColor.Green;

                    Console.Write(outputToWrite.ToString());

                    outputToWrite.Clear();

                    previous = current;
                }
            }

            // If last output needs to be overwritten, overwrite with empty spaces
            if (previous is { Overwrite: true })
            {
                outputToWrite.Append('\r');
                outputToWrite.Append(' ', previous.Value.StringToWrite.Length);
                Console.Write(outputToWrite.ToString());
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            Console.ResetColor();
        }
    }
}
