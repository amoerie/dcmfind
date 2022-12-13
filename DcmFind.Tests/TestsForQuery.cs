using System;
using FellowOakDicom;
using FluentAssertions;
using Xunit;

namespace DcmFind.Tests;

[Collection("DcmFind")]
public class TestsForQuery
{
    private readonly DicomDataset _dicomDataset;

    public TestsForQuery()
    {
        _dicomDataset = new DicomDataset
        {
            { DicomTag.AccessionNumber, "Pineapple" },
            { DicomTag.Rows, "1000" } ,
            { DicomTag.StudyDate, new DateTime(2020, 1, 2) }
        };
    }

    public class TestsForEqualsQuery : TestsForQuery
    {
        [Fact]
        public void ShouldMatchWhenValuesAreEqual()
        {
            var query = new EqualsQuery(DicomTag.AccessionNumber, "Pineapple");

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenValuesAreNotEqual()
        {
            var query = new EqualsQuery(DicomTag.AccessionNumber, "Pineapple2");

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldNotMatchWhenDicomTagIsNotPresent()
        {
            var query = new EqualsQuery(DicomTag.AccessionNumber, "Pineapple");

            _dicomDataset.Clear();
                
            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Theory]
        [InlineData("%apple")]
        [InlineData("%Apple")]
        [InlineData("Pine%")]
        [InlineData("pine%")]
        [InlineData("Pine%apple")]
        public void ShouldMatchWhenValuesMatchesWildcardInBeginning(string value)
        {
            var query = new EqualsQuery(DicomTag.AccessionNumber, value);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
    }

    public class TestsForNotEqualsQuery : TestsForQuery
    {
        [Fact]
        public void ShouldNotMatchWhenValuesAreEqual()
        {
            var query = new NotEqualsQuery(DicomTag.AccessionNumber, "Pineapple");

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenValuesAreNotEqual()
        {
            var query = new NotEqualsQuery(DicomTag.AccessionNumber, "Pineapple2");

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenDicomTagIsNotPresent()
        {
            var query = new NotEqualsQuery(DicomTag.AccessionNumber, "Pineapple");

            _dicomDataset.Clear();
                
            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Theory]
        [InlineData("%apple")]
        [InlineData("%Apple")]
        [InlineData("Pine%")]
        [InlineData("pine%")]
        [InlineData("Pine%apple")]
        public void ShouldNotMatchWhenValuesMatchesWildcardInBeginning(string value)
        {
            var query = new NotEqualsQuery(DicomTag.AccessionNumber, value);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Theory]
        [InlineData("%banana")]
        [InlineData("%Banana")]
        [InlineData("Banana%")]
        [InlineData("bana%")]
        [InlineData("Bana%na")]
        public void ShouldMatchWhenValuesDoesNotMatchWildcard(string value)
        {
            var query = new NotEqualsQuery(DicomTag.AccessionNumber, value);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
    }

    public class TestsForLowerThanQuery : TestsForQuery
    {
        [Fact]
        public void ShouldMatchWhenNumberValueIsLowerThanNotInclusive()
        {
            var query = new LowerThanQuery(DicomTag.Rows, "1001", false);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenNumberValueIsLowerThanInclusive()
        {
            var query = new LowerThanQuery(DicomTag.Rows, "1001", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenNumberValueIsEqualAndNotInclusive()
        {
            var query = new LowerThanQuery(DicomTag.Rows, "1000", false);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenNumberValueIsEqualAndInclusive()
        {
            var query = new LowerThanQuery(DicomTag.Rows, "1000", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenStringValueIsLowerThanNotInclusive()
        {
            var query = new LowerThanQuery(DicomTag.AccessionNumber, "Pineapplf", false);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenStringValueIsLowerThanInclusive()
        {
            var query = new LowerThanQuery(DicomTag.AccessionNumber, "Pineapplf", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenStringValueIsEqualAndNotInclusive()
        {
            var query = new LowerThanQuery(DicomTag.AccessionNumber, "Pineapple", false);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenStringValueIsEqualAndInclusive()
        {
            var query = new LowerThanQuery(DicomTag.AccessionNumber, "Pineapple", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenDateValueIsLowerThanNotInclusive()
        {
            var query = new LowerThanQuery(DicomTag.StudyDate, "20200103", false);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenDateValueIsLowerThanInclusive()
        {
            var query = new LowerThanQuery(DicomTag.StudyDate, "20200103", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenDateValueIsEqualAndNotInclusive()
        {
            var query = new LowerThanQuery(DicomTag.StudyDate, "20200102", false);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenDateValueIsEqualAndInclusive()
        {
            var query = new LowerThanQuery(DicomTag.StudyDate, "20200102", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
    }
        
    public class TestsForGreaterThanQuery : TestsForQuery
    {
        [Fact]
        public void ShouldMatchWhenNumberValueIsGreaterThanNotInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.Rows, "999", false);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenNumberValueIsGreaterThanInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.Rows, "999", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenNumberValueIsEqualAndNotInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.Rows, "1000", false);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenNumberValueIsEqualAndInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.Rows, "1000", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenStringValueIsGreaterThanNotInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.AccessionNumber, "Pineappld", false);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenStringValueIsGreaterThanInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.AccessionNumber, "Pineappld", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenStringValueIsEqualAndNotInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.AccessionNumber, "Pineapple", false);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenStringValueIsEqualAndInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.AccessionNumber, "Pineapple", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenDateValueIsGreaterThanNotInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.StudyDate, "20200101", false);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldMatchWhenDateValueIsGreaterThanInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.StudyDate, "20200101", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
        [Fact]
        public void ShouldNotMatchWhenDateValueIsEqualAndNotInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.StudyDate, "20200102", false);

            query.Matches(_dicomDataset).Should().BeFalse();
        }
            
        [Fact]
        public void ShouldMatchWhenDateValueIsEqualAndInclusive()
        {
            var query = new GreaterThanQuery(DicomTag.StudyDate, "20200102", true);

            query.Matches(_dicomDataset).Should().BeTrue();
        }
            
    }

        
}
