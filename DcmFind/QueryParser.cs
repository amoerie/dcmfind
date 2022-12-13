using System;
using System.Linq;
using FellowOakDicom;

namespace DcmFind
{
    public static class QueryParser
    {
        private static readonly string[] SupportedOperators = {"<=", ">=", "!=", "=", "<", ">" };

        public static bool TryParse(string queryAsString, out IQuery? query)
        {
            query = null;
            
            if (string.IsNullOrWhiteSpace(queryAsString))
                return false;
            
            var matchedOperator = SupportedOperators
                .Select(o => new { Operator = o, Index = queryAsString.IndexOf(o, StringComparison.OrdinalIgnoreCase)})
                .Where(match => match.Index != -1)
                .MinBy(match => match.Index);

            DicomTag? dicomTag;
            if (matchedOperator == null)
            {
                if (DicomTagParser.TryParse(queryAsString, out dicomTag) && dicomTag != null)
                {
                    query = new ContainsTagQuery(dicomTag!);
                    return true;
                }
                
                var supportedOperatorsAsString = string.Join(" or ", SupportedOperators.Select(c => $"'{c}'"));
                Console.Error.WriteLine($"Query '{queryAsString}' is not recognized as a DICOM tag and does not contain any of the supported operators: {supportedOperatorsAsString}");
                return false;
            }

            var @operator = matchedOperator.Operator;
            var dicomTagAsString = queryAsString.Substring(0, matchedOperator.Index).Trim();
            if (!DicomTagParser.TryParse(dicomTagAsString, out dicomTag) || dicomTag == null)
            {
                return false;
            }

            var queryValue = queryAsString.Substring(matchedOperator.Index + matchedOperator.Operator.Length).Trim();

            switch (@operator)
            {
                case "=":
                    query = new EqualsQuery(dicomTag, queryValue);
                    return true;
                case "!=":
                    query = new NotEqualsQuery(dicomTag, queryValue);
                    return true;
                case "<":
                    query = new LowerThanQuery(dicomTag, queryValue, false);
                    return true;
                case "<=":
                    query = new LowerThanQuery(dicomTag, queryValue, true);
                    return true;
                case ">":
                    query = new GreaterThanQuery(dicomTag, queryValue, false);
                    return true;
                case ">=":
                    query = new GreaterThanQuery(dicomTag, queryValue, true);
                    return true;
            }

            return false;
        }
    }
}
