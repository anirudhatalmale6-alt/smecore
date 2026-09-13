using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Consultas;
using SmeCore.Infrastructure.Dados;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Infrastructure.Servicos;

public class UtilizadoresFiltro : ParametrosListagem
{
    public string? Perfil { get; set; }
    public bool? Ativo { get; set; }

    public bool TemFiltrosAtivos =>
        !string.IsNullOrWhiteSpace(Pesquisa) || !string.IsNullOrWhiteSpace(Perfil) || Ativo.HasValue;
}

public class UtilizadorListagem
{
    public Guid Id { get; init; }
    public string NomeCompleto { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Cargo { get; init; }
    public bool Ativo { get; init; }
    public DateTimeOffset CriadoEm { get; init; }
    public DateTimeOffset? UltimoAcessoEm { get; init; }
    public List<string> Perfis { get; init; } = new();
    public bool Bloqueado { get; init; }
}

public class UtilizadoresServico
{
    private readonly AppDbContext _db;
    private readonly UserManager<Utilizador> _utilizadores;
    private readonly RoleManager<Perfil> _perfis;
    private readonly IPermissoesServico _permissoes;
    private readonly IRelogio _relogio;

    public UtilizadoresServico(
        AppDbContext db,
        UserManager<Utilizador> utilizadores,
        RoleManager<Perfil> perfis,
        IPermissoesServico permissoes,
        IRelogio relogio)
    {
        _db = db;
        _utilizadores = utilizadores;
        _perfis = perfis;
        _permissoes = permissoes;
        _relogio = relogio;
    }

    private static readonly MapaOrdenacao<UtilizadorListagem> Ordenacao =
        new MapaOrdenacao<UtilizadorListagem>("nome", q => q.ThenBy(u => u.Id))
            .Adicionar("nome", u => u.NomeCompleto)
            .Adicionar("email", u => u.Email)
            .Adicionar("cargo", u => u.Cargo)
            .Adicionar("criado", u => u.CriadoEm)
            .Adicionar("acesso", u => u.UltimoAcessoEm);

    public async Task<ResultadoPaginado<UtilizadorListagem>> ObterPaginaAsync(
        UtilizadoresFiltro filtro,
        CancellationToken ct = default)
    {
        var agora = _relogio.AgoraUtc;

        // Os filtros são aplicados sobre a entidade, antes da projeção: filtrar sobre a lista
        // de perfis já projetada obrigaria a base de dados a materializar a subconsulta.
        var utilizadores = _db.Users.AsNoTracking().AsQueryable();

        if (filtro.Ativo.HasValue)
        {
            var ativo = filtro.Ativo.Value;
            utilizadores = utilizadores.Where(u => u.Ativo == ativo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Perfil))
        {
            var perfil = filtro.Perfil;
            utilizadores = utilizadores.Where(u => _db.UserRoles
                .Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == perfil)));
        }

        var termo = Pesquisa.Limpar(filtro.Pesquisa);
        if (termo is not null)
        {
            var padrao = Pesquisa.ParaPadrao(termo);
            utilizadores = utilizadores.Where(u =>
                EF.Functions.ILike(u.NomeCompleto, padrao)
                || (u.Email != null && EF.Functions.ILike(u.Email, padrao))
                || (u.Cargo != null && EF.Functions.ILike(u.Cargo, padrao)));
        }

        var query = utilizadores.Select(u => new UtilizadorListagem
        {
            Id = u.Id,
            NomeCompleto = u.NomeCompleto,
            Email = u.Email,
            Cargo = u.Cargo,
            Ativo = u.Ativo,
            CriadoEm = u.CriadoEm,
            UltimoAcessoEm = u.UltimoAcessoEm,
            Bloqueado = u.LockoutEnd != null && u.LockoutEnd > agora,
            Perfis = (from ur in _db.UserRoles
                      join r in _db.Roles on ur.RoleId equals r.Id
                      where ur.UserId == u.Id
                      orderby r.Name
                      select r.Name!).ToList()
        });

        var ordenada = Ordenacao.Aplicar(query, filtro.Ordenar, filtro.Descendente);
        return await ordenada.ParaPaginaAsync(filtro, ct);
    }

    public Task<Utilizador?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<List<Perfil>> ObterPerfisAsync(CancellationToken ct = default)
        => _db.Roles.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<List<string>> ObterPerfisDoUtilizadorAsync(Utilizador utilizador)
        => (await _utilizadores.GetRolesAsync(utilizador)).OrderBy(p => p).ToList();

    public async Task<Resultado<Utilizador>> CriarAsync(
        Utilizador utilizador,
        string palavraPasse,
        IEnumerable<string> perfis,
        CancellationToken ct = default)
    {
        utilizador.UserName = utilizador.Email;
        utilizador.NomeCompleto = utilizador.NomeCompleto.Trim();
        utilizador.CriadoEm = _relogio.AgoraUtc;

        var criacao = await _utilizadores.CreateAsync(utilizador, palavraPasse);
        if (!criacao.Succeeded) return ParaResultado<Utilizador>(criacao);

        var lista = perfis.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        if (lista.Count > 0)
        {
            var atribuicao = await _utilizadores.AddToRolesAsync(utilizador, lista);
            if (!atribuicao.Succeeded) return ParaResultado<Utilizador>(atribuicao);
        }

        return Resultado<Utilizador>.Ok(utilizador);
    }

    public async Task<Resultado> AtualizarAsync(
        Utilizador utilizador,
        IEnumerable<string> perfis,
        CancellationToken ct = default)
    {
        utilizador.UserName = utilizador.Email;
        utilizador.NomeCompleto = utilizador.NomeCompleto.Trim();

        var atualizacao = await _utilizadores.UpdateAsync(utilizador);
        if (!atualizacao.Succeeded) return ParaResultado<object>(atualizacao);

        var pretendidos = perfis.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        var atuais = await _utilizadores.GetRolesAsync(utilizador);

        var remover = atuais.Except(pretendidos).ToList();
        var acrescentar = pretendidos.Except(atuais).ToList();

        if (remover.Count > 0)
        {
            var r = await _utilizadores.RemoveFromRolesAsync(utilizador, remover);
            if (!r.Succeeded) return ParaResultado<object>(r);
        }

        if (acrescentar.Count > 0)
        {
            var r = await _utilizadores.AddToRolesAsync(utilizador, acrescentar);
            if (!r.Succeeded) return ParaResultado<object>(r);
        }

        return Resultado.Ok();
    }

    /// <summary>
    /// Desativar em vez de eliminar: o histórico continua a mostrar quem fez o quê, e o
    /// utilizador deixa de conseguir autenticar-se.
    /// </summary>
    public async Task<Resultado> DefinirEstadoAsync(Guid id, bool ativo, Guid? quemPede, CancellationToken ct = default)
    {
        var utilizador = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (utilizador is null) return Resultado.Erro("Utilizador não encontrado.");

        if (!ativo && quemPede == id)
        {
            return Resultado.Erro("Não pode desativar a sua própria conta.");
        }

        if (!ativo && await EhUltimoAdministradorAsync(utilizador, ct))
        {
            return Resultado.Erro("Esta é a única conta de administrador ativa. Crie outra antes de a desativar.");
        }

        utilizador.Ativo = ativo;

        // Invalidar o cookie de sessão do utilizador desativado, para não continuar a navegar.
        await _utilizadores.UpdateSecurityStampAsync(utilizador);
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> DefinirPalavraPasseAsync(
        Guid id,
        string novaPalavraPasse,
        bool obrigarAlteracao,
        CancellationToken ct = default)
    {
        var utilizador = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (utilizador is null) return Resultado.Erro("Utilizador não encontrado.");

        var token = await _utilizadores.GeneratePasswordResetTokenAsync(utilizador);
        var resultado = await _utilizadores.ResetPasswordAsync(utilizador, token, novaPalavraPasse);
        if (!resultado.Succeeded) return ParaResultado<object>(resultado);

        utilizador.DeveAlterarPalavraPasse = obrigarAlteracao;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> DesbloquearAsync(Guid id, CancellationToken ct = default)
    {
        var utilizador = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (utilizador is null) return Resultado.Erro("Utilizador não encontrado.");

        await _utilizadores.SetLockoutEndDateAsync(utilizador, null);
        await _utilizadores.ResetAccessFailedCountAsync(utilizador);
        return Resultado.Ok();
    }

    public async Task<Resultado> GuardarPerfilAsync(
        Perfil perfil,
        IEnumerable<string> permissoes,
        bool novo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(perfil.Name))
        {
            return Resultado.Erro("O nome do perfil é obrigatório.", nameof(Perfil.Name));
        }

        if (novo)
        {
            var criacao = await _perfis.CreateAsync(perfil);
            if (!criacao.Succeeded) return ParaResultado<object>(criacao);
        }
        else
        {
            var atualizacao = await _perfis.UpdateAsync(perfil);
            if (!atualizacao.Succeeded) return ParaResultado<object>(atualizacao);
        }

        // O perfil Administrador tem sempre tudo; as suas claims não são geridas à mão.
        if (!string.Equals(perfil.Name, Permissoes.PerfilAdministrador, StringComparison.OrdinalIgnoreCase))
        {
            await _permissoes.DefinirPermissoesDoPerfilAsync(perfil, permissoes);
        }

        return Resultado.Ok();
    }

    public async Task<Resultado> EliminarPerfilAsync(Guid id, CancellationToken ct = default)
    {
        var perfil = await _perfis.FindByIdAsync(id.ToString());
        if (perfil is null) return Resultado.Erro("Perfil não encontrado.");
        if (perfil.DeSistema) return Resultado.Erro("Os perfis de sistema não podem ser eliminados.");

        var emUso = await _db.UserRoles.CountAsync(ur => ur.RoleId == id, ct);
        if (emUso > 0)
        {
            return Resultado.Erro($"Este perfil está atribuído a {emUso} utilizador(es). Retire-o desses utilizadores primeiro.");
        }

        var resultado = await _perfis.DeleteAsync(perfil);
        if (!resultado.Succeeded) return ParaResultado<object>(resultado);

        _permissoes.InvalidarCache();
        return Resultado.Ok();
    }

    /// <summary>Quantos utilizadores estão a usar cada perfil, para o ecrã de perfis.</summary>
    public async Task<Dictionary<Guid, int>> ContarUtilizadoresPorPerfilAsync(CancellationToken ct = default)
        => await _db.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Total, ct);

    private async Task<bool> EhUltimoAdministradorAsync(Utilizador utilizador, CancellationToken ct)
    {
        var perfilAdmin = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == Permissoes.PerfilAdministrador, ct);

        if (perfilAdmin is null) return false;

        var ehAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == utilizador.Id && ur.RoleId == perfilAdmin.Id, ct);

        if (!ehAdmin) return false;

        var outrosAdmins = await (from ur in _db.UserRoles
                                  join u in _db.Users on ur.UserId equals u.Id
                                  where ur.RoleId == perfilAdmin.Id && u.Ativo && u.Id != utilizador.Id
                                  select u.Id).CountAsync(ct);

        return outrosAdmins == 0;
    }

    private static Resultado<T> ParaResultado<T>(IdentityResult resultado)
    {
        var r = new Resultado<T>();
        foreach (var erro in resultado.Errors)
        {
            r.Acrescentar(TraduzirErro(erro));
        }
        return r;
    }

    /// <summary>
    /// As mensagens do Identity vêm em inglês; o utilizador final nunca deve ver inglês.
    /// </summary>
    private static string TraduzirErro(IdentityError erro) => erro.Code switch
    {
        "DuplicateUserName" => "Já existe uma conta com este e-mail.",
        "DuplicateEmail" => "Já existe uma conta com este e-mail.",
        "DuplicateRoleName" => "Já existe um perfil com este nome.",
        "InvalidEmail" => "O e-mail indicado não é válido.",
        "InvalidUserName" => "O e-mail indicado não é válido.",
        "PasswordTooShort" => "A palavra-passe deve ter pelo menos 10 caracteres.",
        "PasswordRequiresDigit" => "A palavra-passe deve conter pelo menos um número.",
        "PasswordRequiresUpper" => "A palavra-passe deve conter pelo menos uma letra maiúscula.",
        "PasswordRequiresLower" => "A palavra-passe deve conter pelo menos uma letra minúscula.",
        "PasswordRequiresNonAlphanumeric" => "A palavra-passe deve conter pelo menos um símbolo.",
        "PasswordMismatch" => "A palavra-passe atual não está correta.",
        "UserAlreadyInRole" => "O utilizador já tem este perfil.",
        "UserNotInRole" => "O utilizador não tem este perfil.",
        _ => erro.Description
    };
}
