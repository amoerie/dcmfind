namespace DcmFind;

public readonly struct ConsoleOutput
{
    public readonly string StringToWrite;
    public readonly bool Overwrite;
    
    public ConsoleOutput(string stringToWrite, bool overwrite)
    {
        StringToWrite = stringToWrite;
        Overwrite = overwrite;
    }
} 
