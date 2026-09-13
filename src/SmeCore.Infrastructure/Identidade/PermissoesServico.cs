using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace SmeCore.Infrastructure.Identidade;

public interface IPermissoesServico
{
    /// <summary>Permissões efetivas de um utilizador, já com a soma de todos os seus perfis.</summary>
    Task<ISet<string>> ObterPermissoesAsync(ClaimsPrincipal utilizador);

    Task<bool> TemPermissaoAsync(ClaimsPrincipal utilizador, string permissao);

    /// <summary>Permissões atribuídas a um perfil.</summary>
    Task<ISet<string>> ObterPermissoesDoPerfilAsync(string nomePerfil);

    /// <summary>Substitui as permissões de um perfil pelo conjunto indicado.</summary>
    Task DefinirPermissoesDoPerfilAsync(Perfil perfil, IEnumerable<string> permissoes);

    /// <summary>Limpa a cache — chamado sempre que perfis ou atribuições mudam.</summary>
    void InvalidarCache();
}

/// <summary>
/// Resolve permissões a partir dos perfis, com uma cache curta em memória. O perfil
/// Administrador tem sempre acesso total, mesmo que as claims sejam apagadas por engano.
/// </summary>
public class PermissoesServico : IPermissoesServico
{
    private const string ChaveCache = "permissoes:perfis";
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromSeconds(30);

    private readonly RoleManager<Perfil> _perfis;
    private readonly IMemoryCache _cache;

    public PermissoesServico(RoleManager<Perfil> perfis, IMemoryCache cache)
    {
        _perfis = perfis;
        _cache = cache;
    }

    public async Task<ISet<string>> ObterPermissoesAsync(ClaimsPrincipal utilizador)
    {
        var resultado = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (utilizador.Identity?.IsAuthenticated != true) return resultado;

        var nomesPerfis = utilizador.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (nomesPerfis.Any(n => string.Equals(n, Permissoes.PerfilAdministrador, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var todas in Permissoes.Todas) resultado.Add(todas);
            return resultado;
        }

        var mapa = await ObterMapaAsync();
        foreach (var nome in nomesPerfis)
        {
            if (mapa.TryGetValue(nome, out var permissoes))
            {
                foreach (var p in permissoes) resultado.Add(p);
            }
        }

        return resultado;
    }

    public async Task<bool> TemPermissaoAsync(ClaimsPrincipal utilizador, string permissao)
    {
        var permissoes = await ObterPermissoesAsync(utilizador);
        return permissoes.Contains(permissao);
    }

    public async Task<ISet<string>> ObterPermissoesDoPerfilAsync(string nomePerfil)
    {
        if (string.Equals(nomePerfil, Permissoes.PerfilAdministrador, StringComparison.OrdinalIgnoreCase))
        {
            return new HashSet<string>(Permissoes.Todas, StringComparer.OrdinalIgnoreCase);
        }

        var mapa = await ObterMapaAsync();
        return mapa.TryGetValue(nomePerfil, out var permissoes)
            ? new HashSet<string>(permissoes, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task DefinirPermissoesDoPerfilAsync(Perfil perfil, IEnumerable<string> permissoes)
    {
        var pretendidas = permissoes
            .Where(p => Permissoes.Todas.Contains(p, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var atuais = (await _perfis.GetClaimsAsync(perfil))
            .Where(c => c.Type == Permissoes.TipoClaim)
            .ToList();

        foreach (var claim in atuais.Where(c => !pretendidas.Contains(c.Value)))
        {
            await _perfis.RemoveClaimAsync(perfil, claim);
        }

        var existentes = atuais.Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var nova in pretendidas.Where(p => !existentes.Contains(p)))
        {
            await _perfis.AddClaimAsync(perfil, new Claim(Permissoes.TipoClaim, nova));
        }

        InvalidarCache();
    }

    public void InvalidarCache() => _cache.Remove(ChaveCache);

    private async Task<Dictionary<string, HashSet<string>>> ObterMapaAsync()
    {
        if (_cache.TryGetValue(ChaveCache, out Dictionary<string, HashSet<string>>? emCache) && emCache is not null)
        {
            return emCache;
        }

        var mapa = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var perfil in _perfis.Roles.ToList())
        {
            var claims = await _perfis.GetClaimsAsync(perfil);
            mapa[perfil.Name!] = claims
                .Where(c => c.Type == Permissoes.TipoClaim)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        _cache.Set(ChaveCache, mapa, DuracaoCache);
        return mapa;
    }
}
