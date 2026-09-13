using System.Security.Claims;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Web.Servicos;

/// <summary>
/// Liga o histórico ao pedido HTTP em curso: quem está autenticado e de que endereço.
/// </summary>
public class UtilizadorAtualHttp : IUtilizadorAtual
{
    private readonly IHttpContextAccessor _acessor;

    public UtilizadorAtualHttp(IHttpContextAccessor acessor) => _acessor = acessor;

    public Guid? Id
    {
        get
        {
            var valor = _acessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(valor, out var id) ? id : null;
        }
    }

    public string? Nome
    {
        get
        {
            var utilizador = _acessor.HttpContext?.User;
            if (utilizador?.Identity?.IsAuthenticated != true) return "Sistema";

            // O nome legível é gravado como claim no início de sessão; o e-mail serve de recurso.
            return utilizador.FindFirstValue("nome_apresentacao")
                   ?? utilizador.FindFirstValue(ClaimTypes.Email)
                   ?? utilizador.Identity.Name;
        }
    }

    public string? Origem
    {
        get
        {
            var contexto = _acessor.HttpContext;
            if (contexto is null) return null;

            // Com UseForwardedHeaders ativo, RemoteIpAddress já é o IP real do cliente.
            var ip = contexto.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrWhiteSpace(ip) ? null : ip;
        }
    }
}
