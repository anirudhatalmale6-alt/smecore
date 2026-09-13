using Microsoft.AspNetCore.Identity;

namespace SmeCore.Infrastructure.Identidade;

/// <summary>
/// Utilizador da aplicação. Assenta no ASP.NET Core Identity com chave Guid.
/// </summary>
public class Utilizador : IdentityUser<Guid>
{
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>Cargo/função apresentada no perfil (ex.: "Receção", "Oficina").</summary>
    public string? Cargo { get; set; }

    /// <summary>Um utilizador inativo mantém o histórico mas não consegue autenticar-se.</summary>
    public bool Ativo { get; set; } = true;

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }

    /// <summary>Obriga a definir uma nova palavra-passe no próximo início de sessão.</summary>
    public bool DeveAlterarPalavraPasse { get; set; }

    public string NomeApresentacao => string.IsNullOrWhiteSpace(NomeCompleto) ? (UserName ?? "—") : NomeCompleto;

    /// <summary>Iniciais para o avatar do cabeçalho.</summary>
    public string Iniciais
    {
        get
        {
            var origem = string.IsNullOrWhiteSpace(NomeCompleto) ? (UserName ?? "?") : NomeCompleto;
            var partes = origem.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "?";
            if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
            return (partes[0][..1] + partes[^1][..1]).ToUpperInvariant();
        }
    }
}

/// <summary>Perfil de acesso. As permissões são guardadas como claims do perfil.</summary>
public class Perfil : IdentityRole<Guid>
{
    public Perfil() { }

    public Perfil(string nome) : base(nome) { }

    public string? Descricao { get; set; }

    /// <summary>Perfis de sistema não podem ser eliminados nem renomeados.</summary>
    public bool DeSistema { get; set; }
}
