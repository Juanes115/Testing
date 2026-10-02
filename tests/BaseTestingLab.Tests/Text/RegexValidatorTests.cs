using BaseTestingLab.Core.Text;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.Text;

public class RegexValidatorTests
{
    private readonly RegexValidator _validator = new();

    [Theory]
    [InlineData("usuario@ejemplo.com", true)]
    [InlineData("nombre.apellido+tag@sub.dominio.org", true)]
    [InlineData("invalido@", false)]
    [InlineData("@sinusuario.com", false)]
    [InlineData("espacios en@correo.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_ShouldValidateStandardEmailFormats(string? email, bool expected)
    {
        _validator.IsValidEmail(email).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://www.google.com", true)]
    [InlineData("http://api.midominio.com:8080/v1/users?page=1", true)]
    [InlineData("ftp://invalido.com", false)]
    [InlineData("javascript:alert(1)", false)]
    public void IsValidUrl_ShouldValidateHttpAndHttpsUrls(string url, bool expected)
    {
        _validator.IsValidUrl(url).Should().Be(expected);
    }

    [Theory]
    [InlineData("+573001234567", true)]
    [InlineData("+14155552671", true)]
    [InlineData("3001234567", false)]
    [InlineData("+0123456", false)]
    public void IsValidE164Phone_ShouldValidateInternationalPhoneNumbers(string phone, bool expected)
    {
        _validator.IsValidE164Phone(phone).Should().Be(expected);
    }

    [Theory]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8", true)]
    [InlineData("not-a-valid-guid-string", false)]
    public void IsValidGuid_ShouldValidateGuidStrings(string guid, bool expected)
    {
        _validator.IsValidGuid(guid).Should().Be(expected);
    }

    [Theory]
    [InlineData("articulo-de-prueba-2026", true)]
    [InlineData("articulo_de_prueba", false)]
    [InlineData("Articulo-Mayusculas", false)]
    public void IsValidSlug_ShouldValidateUrlSlugs(string slug, bool expected)
    {
        _validator.IsValidSlug(slug).Should().Be(expected);
    }

    [Fact]
    public void ValidatePasswordStrength_WhenStrong_ShouldPass()
    {
        var password = "Secure#Password2026!";
        var (isValid, errors) = _validator.ValidatePasswordStrength(password, minLength: 8);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePasswordStrength_WhenWeak_ShouldReturnSpecificErrors()
    {
        var password = "pass";
        var (isValid, errors) = _validator.ValidatePasswordStrength(password, minLength: 8);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("8 caracteres"))
              .And.Contain(e => e.Contains("mayúscula"))
              .And.Contain(e => e.Contains("número"))
              .And.Contain(e => e.Contains("carácter especial"));
    }

    [Fact]
    public void ExtractNamedGroups_ShouldExtractMatchingTokens()
    {
        var pattern = @"^HOST=(?<host>[^:]+):PORT=(?<port>\d+)$";
        var input = "HOST=db.server.internal:PORT=5432";
        var groups = _validator.ExtractNamedGroups(input, pattern);

        groups.Should().ContainKey("host").WhoseValue.Should().Be("db.server.internal");
        groups.Should().ContainKey("port").WhoseValue.Should().Be("5432");
    }
}
