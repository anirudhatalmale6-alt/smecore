using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace SmeCore.Domain.Common;

/// <summary>
/// Apresentação de valores ao utilizador. Nenhuma vista escreve datas directamente:
/// tudo passa por aqui, para que nunca apareça uma data no formato ISO (2026-09-13) no ecrã.
/// </summary>
public static class Formatos
{
    public static readonly CultureInfo CulturaPt = CultureInfo.GetCultureInfo("pt-PT");

    /// <summary>Data curta: 13/09/2026. Devolve "—" quando não há valor.</summary>
    public static string Data(DateOnly? valor)
        => valor.HasValue ? valor.Value.ToString("dd/MM/yyyy", CulturaPt) : "—";

    /// <summary>Data e hora locais: 13/09/2026 21:04.</summary>
    public static string DataHora(DateTimeOffset? valor, IRelogio? relogio = null)
    {
        if (!valor.HasValue) return "—";
        var local = relogio?.ParaLocal(valor.Value) ?? valor.Value.ToLocalTime();
        return local.ToString("dd/MM/yyyy HH:mm", CulturaPt);
    }

    /// <summary>Data e hora com segundos, para o histórico.</summary>
    public static string DataHoraSegundos(DateTimeOffset? valor, IRelogio? relogio = null)
    {
        if (!valor.HasValue) return "—";
        var local = relogio?.ParaLocal(valor.Value) ?? valor.Value.ToLocalTime();
        return local.ToString("dd/MM/yyyy HH:mm:ss", CulturaPt);
    }

    /// <summary>Número inteiro com separador de milhares: 125 400.</summary>
    public static string Inteiro(int? valor)
        => valor.HasValue ? valor.Value.ToString("N0", CulturaPt) : "—";

    /// <summary>Quilometragem: 125 400 km.</summary>
    public static string Quilometros(int? valor)
        => valor.HasValue ? $"{valor.Value.ToString("N0", CulturaPt)} km" : "—";

    /// <summary>Valor monetário em euros: 1 250,00 €.</summary>
    public static string Euros(decimal? valor)
        => valor.HasValue ? valor.Value.ToString("C2", CulturaPt) : "—";

    /// <summary>Texto ou travessão quando vazio, para tabelas não ficarem com células em branco.</summary>
    public static string Texto(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim();

    /// <summary>Nome legível de um valor de enum, lido do atributo [Display].</summary>
    public static string Enumeracao(Enum? valor)
    {
        if (valor is null) return "—";

        var campo = valor.GetType().GetField(valor.ToString());
        var display = campo?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? valor.ToString();
    }

    /// <summary>Diferença em dias face a hoje, em texto ("faltam 12 dias", "há 3 dias", "hoje").</summary>
    public static string PrazoRelativo(DateOnly? data, DateOnly hoje)
    {
        if (!data.HasValue) return "—";

        var dias = data.Value.DayNumber - hoje.DayNumber;
        return dias switch
        {
            0 => "hoje",
            1 => "amanhã",
            -1 => "ontem",
            > 1 => $"faltam {dias} dias",
            _ => $"há {-dias} dias"
        };
    }
}
