using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DcmFind.Tests;

[Collection("DcmFind")]
public class TestsForDcmFind : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly StringBuilder _output;
    private readonly StringBuilder _errorOutput;
    private readonly StringWriter _outputWriter;
    private readonly StringWriter _errorOutputWriter;
    private readonly DirectoryInfo _testFilesDirectory;
    private readonly FileInfo _testFile0;
    private readonly FileInfo _testFile1;
    private readonly FileInfo _testFile2;

    public TestsForDcmFind(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        _output = new StringBuilder();
        _outputWriter = new StringWriter(_output);
        _errorOutput = new StringBuilder();
        _errorOutputWriter = new StringWriter(_errorOutput);
        _originalOut = Console.Out;
        _originalError = Console.Error;
        Console.SetOut(_outputWriter);
        Console.SetError(_errorOutputWriter);
        
        _testFilesDirectory = new DirectoryInfo("./TestFiles");
        _testFile0 = new FileInfo(Path.Join(_testFilesDirectory.Name, "0.jpg"));
        _testFile1 = new FileInfo(Path.Join(_testFilesDirectory.Name, "1.dcm"));
        _testFile2 = new FileInfo(Path.Join(_testFilesDirectory.Name, "2.dcm"));
    }
    
    public void Dispose()
    {
        _testOutputHelper.WriteLine(_output.ToString());
        _outputWriter.Dispose();
        _errorOutputWriter.Dispose();
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
    }

    [Fact]
    public async Task ShouldFindAllTestFiles()
    {
        // Arrange
        var expected = new[] { _testFile1.FullName, _testFile2.FullName };
        
        // Act
        var options = new ProgramOptions(false, 400);
        var statusCode = await new Program(options).MainAsync(Array.Empty<string>());
        
        // Assert
        var actual = _output.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        actual.Should().BeEquivalentTo(expected, c => c.WithoutStrictOrdering());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task ShouldFindWithDirectory()
    {
        // Arrange
        var expected = new[] { _testFile1.FullName, _testFile2.FullName };
        
        // Act
        var options = new ProgramOptions(false, 400);
        var statusCode = await new Program(options).MainAsync(new []
        {
            "--directory", _testFilesDirectory.FullName
        });
        
        // Assert
        var actual = _output.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        actual.Should().BeEquivalentTo(expected, c => c.WithoutStrictOrdering());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task ShouldFindWithQuery()
    {
        // Arrange
        var expected = new[] { _testFile2.FullName };
        
        // Act
        var options = new ProgramOptions(false, 400);
        var statusCode = await new Program(options).MainAsync(new []
        {
            "--directory", _testFilesDirectory.FullName,
            "--query", "AccessionNumber=CR2022062117111"
        });
        
        // Assert
        var actual = _output.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        actual.Should().BeEquivalentTo(expected, c => c.WithoutStrictOrdering());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task ShouldFindWithQueryWithLimit()
    {
        // Arrange
        var expected = new[] { _testFile2.FullName };
        
        // Act
        var options = new ProgramOptions(false, 400);
        var statusCode = await new Program(options).MainAsync(new []
        {
            "--directory", _testFilesDirectory.FullName,
            "--query", "AccessionNumber=CR2022062117111",
            "--limit", "1",
        });
        
        // Assert
        var actual = _output.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        actual.Should().BeEquivalentTo(expected, c => c.WithoutStrictOrdering());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task ShouldFindWithDirectoryAndProgress()
    {
        // Arrange
        var expected = new[]
        {
            $"{_testFile0.FullName}\r{_testFile1.FullName}\r{_testFile2.FullName}\r{_testFile1.FullName}",
            _testFile2.FullName
        };
        
        // Act
        var options = new ProgramOptions(true, 400);
        var statusCode = await new Program(options).MainAsync(new []
        {
            "--directory", _testFilesDirectory.FullName,
            "--parallelism", "1"
        });
        
        // Assert
        var actual = _output.ToString().Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        actual.Should().BeEquivalentTo(expected, c => c.WithoutStrictOrdering());
        Assert.Equal(string.Empty, _errorOutput.ToString());
        Assert.Equal(0, statusCode);
    }

}
