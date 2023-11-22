using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FellowOakDicom;

namespace DcmFind;

public static class DicomFileMatcher
{
    public static async Task MatchAsync(
        ChannelReader<string> input, 
        ChannelWriter<string> output,
        bool writeToConsoleOutput,
        ChannelWriter<ConsoleOutput> consoleOutput,
        List<IQuery> queries,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await input.WaitToReadAsync(cancellationToken))
            {
                while (input.TryRead(out string? file))
                {
                    if (writeToConsoleOutput)
                    {
                        consoleOutput.TryWrite(new ConsoleOutput(file, true));
                    }
                            
                    cancellationToken.ThrowIfCancellationRequested();

                    DicomFile dicomFile;
                    try
                    {
                        dicomFile = await DicomFile.OpenAsync(file);

                        if (dicomFile == null)
                        {
                            continue;
                        }

                        if (dicomFile.Format != DicomFileFormat.DICOM3)
                        {
                            continue;
                        }
                    }
                    catch (DicomFileException)
                    {
                        continue;
                    }

                    var matches = true;
                    for (var i = 0; i < queries.Count; i++)
                    {
                        var query = queries[i];
                        if (!query.Matches(dicomFile.Dataset) && !query.Matches(dicomFile.FileMetaInfo))
                        {
                            matches = false;
                            break;
                        }
                    }
                    
                    if (matches)
                    {
                        await output.WriteAsync(file, cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
    }
}
