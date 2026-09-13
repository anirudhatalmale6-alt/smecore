using FluentAssertions;
using SmeCore.Domain.Validacao;
using Xunit;

namespace SmeCore.Tests;

public class NifTestes
{
    [Theory]
    // Pessoas singulares
    [InlineData("123456789")]
    [InlineData("249123452")]
    // Pessoas coletivas
    [InlineData("501442600")]
    [InlineData("500000000")]
    // Séries de dois dígitos
    [InlineData("980000009")]
    [InlineData("700000003")]
    [InlineData("450000001")]
    [InlineData("910000000")]
    public void Aceita_nifs_validos(string valor)
        => Nif.EhValido(valor).Should().BeTrue();

    [Theory]
    [InlineData("123456780")]   // dígito de controlo errado
    [InlineData("12345678")]    // curto de mais
    [InlineData("1234567890")]  // comprido de mais
    [InlineData("400000000")]   // prefixo 4 isolado não existe
    [InlineData("000000000")]   // prefixo 0 não existe
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_nifs_invalidos(string? valor)
        => Nif.EhValido(valor).Should().BeFalse();

    [Theory]
    [InlineData("123 456 789")]
    [InlineData("123.456.789")]
    [InlineData("123-456-789")]
    [InlineData(" PT123456789 ")]
    [InlineData("123 456 789")] // espaço não separável, copiado de um PDF
    public void Ignora_separadores_e_prefixos_ao_normalizar(string valor)
        => Nif.EhValido(valor).Should().BeTrue();

    [Fact]
    public void Formatar_agrupa_em_tres()
        => Nif.Formatar("123456789").Should().Be("123 456 789");

    [Fact]
    public void Formatar_devolve_o_original_quando_nao_tem_nove_digitos()
        => Nif.Formatar("12345").Should().Be("12345");
}

public class MatriculaTestes
{
    [Theory]
    [InlineData("AA-00-AA")]  // desde 2020
    [InlineData("00-AA-00")]  // 2005-2020
    [InlineData("00-00-AA")]  // 1992-2005
    [InlineData("AA-00-00")]  // até 1992
    [InlineData("aa00aa")]    // minúsculas e sem separadores
    [InlineData("AA 00 AA")]
    [InlineData("AA.00.AA")]
    [InlineData("AA–00–AA")]  // travessão em vez de hífen
    public void Aceita_os_formatos_nacionais(string valor)
        => Matricula.EhValida(valor).Should().BeTrue();

    [Theory]
    [InlineData("A0-00-AA")]   // letra e dígito misturados no primeiro par
    [InlineData("AAA-00-A")]
    [InlineData("AA-0A-AA")]
    [InlineData("AA-00-A")]    // curta
    [InlineData("AA-00-AAA")]  // comprida
    [InlineData("000000")]     // só dígitos
    [InlineData("AAAAAA")]     // só letras
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_matriculas_invalidas(string? valor)
        => Matricula.EhValida(valor).Should().BeFalse();

    [Theory]
    [InlineData("aa00aa", "AA-00-AA")]
    [InlineData("00 aa 00", "00-AA-00")]
    [InlineData("AA-00-AA", "AA-00-AA")]
    public void Formata_sempre_com_hifenes_e_maiusculas(string entrada, string esperado)
        => Matricula.Formatar(entrada).Should().Be(esperado);
}

public class CodigoPostalTestes
{
    [Theory]
    [InlineData("4700-123")]
    [InlineData("4700123")]
    [InlineData("4700 123")]
    public void Aceita_codigos_validos(string valor)
        => CodigoPostal.EhValido(valor).Should().BeTrue();

    [Theory]
    [InlineData("4700-12")]
    [InlineData("470-123")]
    [InlineData("47001234")]
    [InlineData("")]
    public void Recusa_codigos_invalidos(string valor)
        => CodigoPostal.EhValido(valor).Should().BeFalse();

    [Fact]
    public void Formata_com_hifen()
        => CodigoPostal.Formatar("4700123").Should().Be("4700-123");
}

public class AtributosTestes
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]                 // vazio é aceite; use [Required] se for obrigatório
    [InlineData("123456789", true)]
    [InlineData("123456780", false)]
    public void Atributo_de_nif(string? valor, bool esperado)
        => new NifValidoAttribute().IsValid(valor).Should().Be(esperado);

    [Theory]
    [InlineData("WVWZZZ1JZ3W386752", true)]
    [InlineData("WVWZZZ1JZ3W38675", false)]   // 16 caracteres
    [InlineData("WVWZZZ1JZ3W386752X", false)] // 18 caracteres
    [InlineData("IVWZZZ1JZ3W386752", false)]  // contém I
    [InlineData("OVWZZZ1JZ3W386752", false)]  // contém O
    [InlineData("QVWZZZ1JZ3W386752", false)]  // contém Q
    [InlineData(null, true)]
    public void Atributo_de_vin(string? valor, bool esperado)
        => new VinValidoAttribute().IsValid(valor).Should().Be(esperado);
}
