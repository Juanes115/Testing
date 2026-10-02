using BaseTestingLab.Core.Text;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.Text;

public class WildcardMatcherTests
{
    [Theory]
    [InlineData("reporte_2026.pdf", "*.pdf", true)]
    [InlineData("factura.xlsx", "*.pdf", false)]
    [InlineData("archivo_final_v2.docx", "archivo*v2.docx", true)]
    [InlineData("archivo_v2.docx", "archivo*v2.docx", true)]
    [InlineData("archivo_v3.docx", "archivo*v2.docx", false)]
    [InlineData("log1.txt", "log?.txt", true)]
    [InlineData("log12.txt", "log?.txt", false)]
    [InlineData("IMG_01.png", "IMG_??.png", true)]
    [InlineData("IMG_001.png", "IMG_??.png", false)]
    [InlineData("itemA.dat", "item[A-Z].dat", true)]
    [InlineData("item5.dat", "item[0-9].dat", true)]
    [InlineData("itemZ.dat", "item[0-9].dat", false)]
    public void IsMatch_ShouldEvaluateWildcardsAccurately(string input, string pattern, bool expected)
    {
        var matcher = new WildcardMatcher(ignoreCase: true);
        var result = matcher.IsMatch(input, pattern);
        result.Should().Be(expected);
    }

    [Fact]
    public void Filter_ShouldReturnOnlyMatchingElements()
    {
        var matcher = new WildcardMatcher(ignoreCase: true);
        var files = new[]
        {
            "reporte_enero.csv",
            "reporte_febrero.csv",
            "balance_anual.xlsx",
            "datos_enero.csv",
            "resumen_mensual.pdf"
        };

        var matches = matcher.Filter(files, "reporte_*.csv").ToList();

        matches.Should().HaveCount(2)
               .And.ContainInOrder("reporte_enero.csv", "reporte_febrero.csv")
               .And.NotContain("balance_anual.xlsx");
    }

    [Fact]
    public void WildcardToRegex_ShouldProduceCorrectRegexAnchorPattern()
    {
        var pattern = "doc_?_*.txt";
        var regex = WildcardMatcher.WildcardToRegex(pattern);
        regex.Should().Be(@"^doc_._.*\.txt$");
    }
}
