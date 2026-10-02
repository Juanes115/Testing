using BaseTestingLab.Core.Common.Exceptions;
using BaseTestingLab.Core.Text;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.Text;

public class TextProcessorTests
{
    private readonly TextProcessor _sut = new();

    [Theory]
    [InlineData("   Hola    mundo   \t  con   espacios   ", "Hola mundo con espacios")]
    [InlineData("Línea 1\r\nLínea 2\rLínea 3", "Línea 1\nLínea 2\nLínea 3")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void SanitizeWhitespace_ShouldNormalizeWhitespacesAndNewlines(string? input, string expected)
    {
        var result = _sut.SanitizeWhitespace(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("usuarioNombreCompleto", "usuario-nombre-completo")]
    [InlineData("Factura_Electronica_V2", "factura-electronica-v2")]
    [InlineData("Hola Mundo Cruel", "hola-mundo-cruel")]
    [InlineData("APIClientService", "api-client-service")]
    public void ToKebabCase_ShouldTransformStringsProperly(string input, string expected)
    {
        var result = _sut.ToKebabCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Anita lava la tina", true)]
    [InlineData("A mamá Roma le aviva el amor a mamá", true)]
    [InlineData("Dábale arroz a la zorra el abad", true)]
    [InlineData("¿Acaso hubo búhos acá?", true)]
    [InlineData("Hola mundo", false)]
    [InlineData("x", true)]
    [InlineData("", false)]
    public void IsPalindrome_ShouldDetectPalindromes_IgnoringDiacriticsPunctuationAndCase(string input, bool expected)
    {
        var result = _sut.IsPalindrome(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("carlos.santana@empresa.com", "c***a@empresa.com")]
    [InlineData("ab@dominio.org", "a*@dominio.org")]
    public void MaskEmail_ShouldObfuscateUsernameProperly(string email, string expected)
    {
        var result = _sut.MaskEmail(email);
        result.Should().Be(expected);
    }

    [Fact]
    public void MaskEmail_WhenInvalidEmail_ShouldThrowValidationException()
    {
        var invalidEmail = "correo-sin-arroba.com";
        Action act = () => _sut.MaskEmail(invalidEmail);

        act.Should().Throw<ValidationException>()
           .WithMessage("*Formato de correo electrónico inválido*")
           .And.PropertyName.Should().Be("email");
    }

    [Theory]
    [InlineData("4532 1234 5678 9012", "****-****-****-9012")]
    [InlineData("4532-1234-5678-9012", "****-****-****-9012")]
    [InlineData("4532123456789012", "****-****-****-9012")]
    public void MaskCreditCard_ShouldLeaveOnlyLastFourDigits(string card, string expected)
    {
        var result = _sut.MaskCreditCard(card);
        result.Should().Be(expected);
        result.Should().StartWith("****-****-****-");
    }

    [Fact]
    public void Truncate_PreservingWords_ShouldNotCutWordsInHalf()
    {
        var text = "El veloz murciélago hindú comía feliz cardillo y escabeche";
        var result = _sut.Truncate(text, maxLength: 25, preserveWords: true, ellipsis: "...");

        result.Should().Be("El veloz murciélago...");
        result.Length.Should().BeLessThanOrEqualTo(25);
    }

    [Fact]
    public void RenderTemplate_WithValidPlaceholders_ShouldInterpolateValues()
    {
        var template = "Estimado {name}, su saldo pendiente en la cuenta {account} es de {amount}.";
        var values = new Dictionary<string, object?>
        {
            { "name", "Mariana" },
            { "account", "ACC-9981" },
            { "amount", "$150.00" }
        };

        var rendered = _sut.RenderTemplate(template, values);
        rendered.Should().Be("Estimado Mariana, su saldo pendiente en la cuenta ACC-9981 es de $150.00.");
    }

    [Fact]
    public void RenderTemplate_WhenStrictModeAndPlaceholderMissing_ShouldThrowKeyNotFoundException()
    {
        var template = "Bienvenido {user} a {company}!";
        var values = new Dictionary<string, object?> { { "user", "Andrés" } };

        Action act = () => _sut.RenderTemplate(template, values, strictMode: true);
        act.Should().Throw<KeyNotFoundException>().WithMessage("*'{company}'*");
    }

    [Fact]
    public void ParseKeyValuePairs_ShouldParseComplexDelimitedStringWithQuotes()
    {
        var connectionString = "Server=tcp:sql.acme.org; Port=1433; Database=\"Ventas Corporativas\"; Timeout=30";
        var result = _sut.ParseKeyValuePairs(connectionString);

        result.Should().HaveCount(4)
              .And.ContainKey("Server").WhoseValue.Should().Be("tcp:sql.acme.org");
        result["Port"].Should().Be("1433");
        result["Database"].Should().Be("Ventas Corporativas");
        result["Timeout"].Should().Be("30");
    }

    [Fact]
    public void ToSnakeCase_ShouldTransformStringsProperly()
    {
        var result = _sut.ToSnakeCase("UsuarioNombreCompleto");
        result.Should().Be("usuario_nombre_completo");
    }

    [Fact]
    public void ToTitleCase_ShouldCapitalizeWords()
    {
        var result = _sut.ToTitleCase("hola mundo desde pruebas unitarias");
        result.Should().Be("Hola Mundo Desde Pruebas Unitarias");
    }

    [Fact]
    public void Truncate_WithoutPreserveWords_ShouldCutAtExactLength()
    {
        var result = _sut.Truncate("1234567890", maxLength: 6, preserveWords: false, ellipsis: "...");
        result.Should().Be("123...");
        result.Length.Should().Be(6);
    }

    [Fact]
    public void Truncate_WhenMaxLengthShorterThanEllipsis_ShouldReturnTruncatedEllipsis()
    {
        var result = _sut.Truncate("texto largo", maxLength: 2, ellipsis: "...");
        result.Should().Be("..");
    }
}
