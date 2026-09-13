using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SmeCore.Infrastructure.Identidade;

/// <summary>Requisito de autorização para uma permissão concreta.</summary>
public class PermissaoRequisito : IAuthorizationRequirement
{
    public string Permissao { get; }

    public PermissaoRequisito(string permissao) => Permissao = permissao;
}

/// <summary>
/// Cria políticas do tipo "perm:clientes.ver" em tempo de execução, para não ser necessário
/// registar uma política por cada permissão no arranque.
/// </summary>
public class PermissaoPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _predefinido;

    public PermissaoPolicyProvider(IOptions<AuthorizationOptions> opcoes)
    {
        _predefinido = new DefaultAuthorizationPolicyProvider(opcoes);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _predefinido.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _predefinido.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Permissoes.PrefixoPolitica, StringComparison.OrdinalIgnoreCase))
        {
            var permissao = policyName[Permissoes.PrefixoPolitica.Length..];
            var politica = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissaoRequisito(permissao))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(politica);
        }

        return _predefinido.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Decide se o utilizador tem a permissão pedida. As permissões vêm dos perfis, lidas através
/// de <see cref="IPermissoesServico"/> — nunca das claims gravadas no cookie, para que uma
/// alteração de permissões tenha efeito sem obrigar o utilizador a voltar a entrar.
/// </summary>
public class PermissaoHandler : AuthorizationHandler<PermissaoRequisito>
{
    private readonly IPermissoesServico _permissoes;

    public PermissaoHandler(IPermissoesServico permissoes) => _permissoes = permissoes;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissaoRequisito requisito)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        if (await _permissoes.TemPermissaoAsync(context.User, requisito.Permissao))
        {
            context.Succeed(requisito);
        }
    }
}
