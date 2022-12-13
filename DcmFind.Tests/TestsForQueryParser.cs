using FluentAssertions;
using Xunit;

namespace DcmFind.Tests;

[Collection("DcmFind")]
public class TestsForQueryParser
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldReturnFalseWhenQueryValueIsNullOrEmpty(string queryValue)
    {
        QueryParser.TryParse(queryValue, out _).Should().BeFalse();
    }
        
    [Fact]
    public void ShouldReturnFalseWhenOperatorIsMissing()
    {
        QueryParser.TryParse("AccessionNumberPineapple", out _).Should().BeFalse();
    }
        
    [Fact]
    public void ShouldReturnFalseWhenOperatorIsUnknown()
    {
        QueryParser.TryParse("AccessionNumber~Pineapple", out _).Should().BeFalse();
    }
        
    [Fact]
    public void ShouldReturnFalseWhenDicomTagIsUnknown()
    {
        QueryParser.TryParse("Banana=Pineapple", out _).Should().BeFalse();
    }
        
    [Fact]
    public void ShouldReturnEqualsQuery()
    {
        QueryParser.TryParse("AccessionNumber=Pineapple", out var query).Should().BeTrue();
        query.Should().BeOfType<EqualsQuery>();
    }
        
    [Fact]
    public void ShouldReturnEqualsQueryEvenIfQueryValueContainsLowerThan()
    {
        QueryParser.TryParse("AccessionNumber=Pineapple<=3", out var query).Should().BeTrue();
        query.Should().BeOfType<EqualsQuery>();
    }
        
    [Fact]
    public void ShouldReturnLowerThanQuery()
    {
        QueryParser.TryParse("AccessionNumber<=Pineapple", out var query).Should().BeTrue();
        query.Should().BeOfType<LowerThanQuery>();
    }
        
    [Fact]
    public void ShouldReturnLowerThanQueryEvenIfQueryValueContainsEquals()
    {
        QueryParser.TryParse("AccessionNumber<=Pineapple=3", out var query).Should().BeTrue();
        query.Should().BeOfType<LowerThanQuery>();
    }
        
    [Fact]
    public void ShouldReturnLowerThanOrEqualsQuery()
    {
        QueryParser.TryParse("AccessionNumber<Pineapple", out var query).Should().BeTrue();
        query.Should().BeOfType<LowerThanQuery>();
    }
        
    [Fact]
    public void ShouldReturnNotEqualsQuery()
    {
        QueryParser.TryParse("AccessionNumber!=\"\"", out var query).Should().BeTrue();
        query.Should().BeOfType<NotEqualsQuery>();
    }
}
