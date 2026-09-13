using FluentAssertions;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using Xunit;

namespace SmeCore.Tests;

public class FormatosTestes
{
    [Fact]
    public void Data_sai_no_formato_portugues()
        => Formatos.Data(new DateOnly(2026, 9, 13)).Should().Be("13/09/2026");

    [Fact]
    public void Data_ausente_sai_como_travessao()
        => Formatos.Data(null).Should().Be("—");

    [Fact]
    public void Quilometros_usa_separador_de_milhares_portugues()
    {
        // Em pt-PT o separador de milhares é um espaço não separável (U+00A0), não um espaço normal.
        Formatos.Quilometros(125400).Should().Be("125\u00A0400 km");
    }

    [Fact]
    public void Enumeracao_usa_o_nome_apresentavel()
        => Formatos.Enumeracao(TipoCombustivel.Gasoleo).Should().Be("Gasóleo");

    [Theory]
    [InlineData(0, "hoje")]
    [InlineData(1, "amanhã")]
    [InlineData(-1, "ontem")]
    [InlineData(12, "faltam 12 dias")]
    [InlineData(-30, "há 30 dias")]
    public void Prazo_relativo_e_lido_a_partir_da_data_recebida(int dias, string esperado)
    {
        // A data de referência é um argumento, nunca DateTime.Today: é o que faz o teste
        // valer em qualquer dia do ano.
        var hoje = new DateOnly(2026, 3, 1);
        Formatos.PrazoRelativo(hoje.AddDays(dias), hoje).Should().Be(esperado);
    }
}

public class RelogioTestes
{
    private readonly RelogioSistema _relogio = new();

    [Fact]
    public void Inicio_do_dia_no_verao_e_uma_hora_antes_em_utc()
    {
        // Em julho Portugal continental está em UTC+1: a meia-noite local são as 23:00 do dia
        // anterior em UTC. É esta a conversão que faz um filtro "de 13/07 a 13/07" apanhar o
        // dia 13 inteiro e não parte do 12.
        var inicio = _relogio.InicioDoDiaUtc(new DateOnly(2026, 7, 13));

        inicio.UtcDateTime.Should().Be(new DateTime(2026, 7, 12, 23, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Inicio_do_dia_no_inverno_coincide_com_utc()
    {
        var inicio = _relogio.InicioDoDiaUtc(new DateOnly(2026, 1, 13));

        inicio.UtcDateTime.Should().Be(new DateTime(2026, 1, 13, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Hoje_local_deriva_do_instante_utc()
    {
        var agora = _relogio.AgoraUtc;
        var local = _relogio.ParaLocal(agora);

        _relogio.HojeLocal.Should().Be(DateOnly.FromDateTime(local.DateTime));
    }
}

public class PaginacaoTestes
{
    [Fact]
    public void Tamanho_de_pagina_invalido_volta_ao_predefinido()
    {
        var parametros = new ParametrosListagem { TamanhoPagina = 0 };
        parametros.TamanhoPagina.Should().Be(ParametrosListagem.TamanhoPaginaPredefinido);
    }

    [Fact]
    public void Tamanho_de_pagina_excessivo_e_limitado()
    {
        var parametros = new ParametrosListagem { TamanhoPagina = 100_000 };
        parametros.TamanhoPagina.Should().Be(ParametrosListagem.TamanhoPaginaMaximo);
    }

    [Fact]
    public void Pagina_zero_ou_negativa_passa_a_um()
    {
        new ParametrosListagem { Pagina = 0 }.Pagina.Should().Be(1);
        new ParametrosListagem { Pagina = -5 }.Pagina.Should().Be(1);
    }

    [Fact]
    public void Direcao_alternada_comeca_ascendente_e_inverte_no_mesmo_campo()
    {
        var parametros = new ParametrosListagem { Ordenar = "nome", Direcao = "asc" };

        parametros.DirecaoAlternada("nome").Should().Be("desc");
        parametros.DirecaoAlternada("nif").Should().Be("asc");
    }

    [Fact]
    public void Resultado_calcula_o_intervalo_apresentado()
    {
        var pagina = new ResultadoPaginado<int>(new[] { 1, 2, 3 }, totalItens: 53, pagina: 3, tamanhoPagina: 25);

        pagina.TotalPaginas.Should().Be(3);
        pagina.PrimeiroItem.Should().Be(51);
        pagina.UltimoItem.Should().Be(53);
        pagina.TemAnterior.Should().BeTrue();
        pagina.TemSeguinte.Should().BeFalse();
    }

    [Fact]
    public void Resultado_vazio_tem_uma_pagina_e_intervalo_zero()
    {
        var pagina = ResultadoPaginado<int>.Vazio();

        pagina.TotalPaginas.Should().Be(1);
        pagina.PrimeiroItem.Should().Be(0);
        pagina.UltimoItem.Should().Be(0);
    }
}
