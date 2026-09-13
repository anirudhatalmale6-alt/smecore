using System.ComponentModel.DataAnnotations;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;

namespace SmeCore.Domain.Veiculos;

public enum TipoCombustivel
{
    [Display(Name = "Gasolina")]
    Gasolina = 1,

    [Display(Name = "Gasóleo")]
    Gasoleo = 2,

    [Display(Name = "GPL")]
    Gpl = 3,

    [Display(Name = "Elétrico")]
    Eletrico = 4,

    [Display(Name = "Híbrido (gasolina)")]
    HibridoGasolina = 5,

    [Display(Name = "Híbrido (gasóleo)")]
    HibridoGasoleo = 6,

    [Display(Name = "Híbrido plug-in")]
    HibridoPlugIn = 7,

    [Display(Name = "Outro")]
    Outro = 99
}

public enum TipoCaixa
{
    [Display(Name = "Manual")]
    Manual = 1,

    [Display(Name = "Automática")]
    Automatica = 2,

    [Display(Name = "Não definida")]
    NaoDefinida = 0
}

public class Veiculo : EntidadeAuditavel
{
    /// <summary>Matrícula normalizada em maiúsculas com hífenes (ex.: AA-00-AA).</summary>
    public string Matricula { get; set; } = string.Empty;

    /// <summary>Número de chassis / VIN (17 caracteres quando presente).</summary>
    public string? Vin { get; set; }

    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Versao { get; set; }

    public int? Ano { get; set; }

    /// <summary>Data da primeira matrícula, quando conhecida.</summary>
    public DateOnly? DataPrimeiraMatricula { get; set; }

    public TipoCombustivel Combustivel { get; set; } = TipoCombustivel.Gasolina;
    public TipoCaixa Caixa { get; set; } = TipoCaixa.NaoDefinida;

    /// <summary>Cilindrada em cm³.</summary>
    public int? Cilindrada { get; set; }

    /// <summary>Potência em cv.</summary>
    public int? Potencia { get; set; }

    public string? Cor { get; set; }

    /// <summary>Quilometragem registada na última visita.</summary>
    public int? Quilometragem { get; set; }

    /// <summary>Data da próxima inspeção periódica obrigatória.</summary>
    public DateOnly? ProximaInspecao { get; set; }

    public string? Observacoes { get; set; }

    public bool Ativo { get; set; } = true;

    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string Designacao => $"{Marca} {Modelo}".Trim();
}
