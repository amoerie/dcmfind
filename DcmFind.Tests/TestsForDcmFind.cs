using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DcmFind.Tests;

public class TestsForDcmFind : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly StringBuilder _output;
    private readonly StringBuilder _errorOutput;
    private readonly StringWriter _outputWriter;
    private readonly StringWriter _errorOutputWriter;
    private readonly DirectoryInfo _testFilesDirectory;
    private readonly FileInfo _testFile1;
    private readonly FileInfo _testFile2;

    public TestsForDcmFind(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        _output = new StringBuilder();
        _outputWriter = new StringWriter(_output);
        _errorOutput = new StringBuilder();
        _errorOutputWriter = new StringWriter(_errorOutput);
        Console.SetOut(_outputWriter);
        Console.SetError(_errorOutputWriter);
        
        _testFilesDirectory = new DirectoryInfo("./TestFiles");
        _testFile1 = new FileInfo(Path.Join(_testFilesDirectory.Name, "1.dcm"));
        _testFile2 = new FileInfo(Path.Join(_testFilesDirectory.Name, "2.dcm"));
    }

    [Fact]
    public async Task ShouldFindAllTestFiles()
    {
        // Arrange
        var expected = $"{_testFile1.FullName}{Environment.NewLine}"
                       + $"{_testFile2.FullName }{Environment.NewLine}";
        
        // Act
        var statusCode = await Program.Main(Array.Empty<string>());
        
        // Assert
        Assert.Equal(expected, _output.ToString());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task ShouldFindWithDirectory()
    {
        // Arrange
        var expected = $"{_testFile1.FullName}{Environment.NewLine}"
                       + $"{_testFile2.FullName }{Environment.NewLine}";
        
        // Act
        var statusCode = await Program.Main(new []
        {
            "--directory", _testFilesDirectory.FullName
        });
        
        // Assert
        Assert.Equal(expected, _output.ToString());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task ShouldFindWithQuery()
    {
        // Arrange
        var expected = $"{_testFile2.FullName }{Environment.NewLine}";
        
        // Act
        var statusCode = await Program.Main(new []
        {
            "--directory", _testFilesDirectory.FullName,
            "--query", "AccessionNumber=CR2022062117111"
        });
        
        // Assert
        Assert.Equal(expected, _output.ToString());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }
    

    public void Dispose()
    {
        _testOutputHelper.WriteLine(_output.ToString());
        _outputWriter.Dispose();
        _errorOutputWriter.Dispose();
    }
}
