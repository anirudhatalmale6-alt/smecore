using System.ComponentModel.DataAnnotations;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;

namespace SmeCore.Domain.Clientes;

public enum TipoCliente
{
    [Display(Name = "Particular")]
    Particular = 1,

    [Display(Name = "Empresa")]
    Empresa = 2
}

public class Cliente : EntidadeAuditavel
{
    /// <summary>Número sequencial legível, atribuído pela base de dados (ex.: 1, 2, 3...).</summary>
    public int Numero { get; set; }

    public TipoCliente Tipo { get; set; } = TipoCliente.Particular;

    /// <summary>Nome da pessoa ou designação social da empresa.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>NIF/NIPC português (9 dígitos) ou número fiscal estrangeiro.</summary>
    public string? Nif { get; set; }

    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Telemovel { get; set; }

    public string? Morada { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Localidade { get; set; }
    public string Pais { get; set; } = "Portugal";

    public string? Observacoes { get; set; }

    public bool Ativo { get; set; } = true;

    public List<ClienteContacto> Contactos { get; set; } = new();
    public List<Veiculo> Veiculos { get; set; } = new();

    /// <summary>Etiqueta usada em listagens e caixas de pesquisa.</summary>
    public string Designacao => string.IsNullOrWhiteSpace(Nif) ? Nome : $"{Nome} ({Nif})";
}

/// <summary>Pessoa de contacto. Usado sobretudo em clientes do tipo Empresa.</summary>
public class ClienteContacto : EntidadeAuditavel
{
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public bool Principal { get; set; }
}
