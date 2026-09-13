using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Utilizadores;

/// <summary>Serve /perfis/novo e /perfis/{id}/editar.</summary>
[Authorize(Policy = Permissoes.Politicas.UtilizadoresGerirPerfis)]
public class EditarPerfilModel : PageModel
{
    private readonly RoleManager<Perfil> _perfis;
    private readonly UtilizadoresServico _utilizadores;
    private readonly IPermissoesServico _permissoes;

    public EditarPerfilModel(
        RoleManager<Perfil> perfis,
        UtilizadoresServico utilizadores,
        IPermissoesServico permissoes)
    {
        _perfis = perfis;
        _utilizadores = utilizadores;
        _permissoes = permissoes;
    }

    public class DadosPerfil
    {
        [Required(ErrorMessage = "Indique o nome do perfil.")]
        [StringLength(80)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        public List<string> Permissoes { get; set; } = new();
    }

    [BindProperty]
    public DadosPerfil Dados { get; set; } = new();

    public Guid? Id { get; private set; }
    public bool Novo => Id is null;
    public bool DeSistema { get; private set; }

    /// <summary>O perfil Administrador tem sempre acesso total e não é editável.</summary>
    public bool EhAdministrador { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        Id = id;

        if (id is null) return Page();

        var perfil = await _perfis.FindByIdAsync(id.Value.ToString());
        if (perfil is null) return NotFound();

        DeSistema = perfil.DeSistema;
        EhAdministrador = string.Equals(perfil.Name, Permissoes.PerfilAdministrador, StringComparison.OrdinalIgnoreCase);

        Dados = new DadosPerfil
        {
            Nome = perfil.Name ?? string.Empty,
            Descricao = perfil.Descricao,
            Permissoes = (await _permissoes.ObterPermissoesDoPerfilAsync(perfil.Name!)).ToList()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        Id = id;

        if (!ModelState.IsValid) return Page();

        Perfil perfil;

        if (id is null)
        {
            perfil = new Perfil(Dados.Nome.Trim()) { Descricao = Dados.Descricao };
        }
        else
        {
            var existente = await _perfis.FindByIdAsync(id.Value.ToString());
            if (existente is null) return NotFound();

            perfil = existente;
            DeSistema = perfil.DeSistema;
            EhAdministrador = string.Equals(perfil.Name, Permissoes.PerfilAdministrador, StringComparison.OrdinalIgnoreCase);

            // Um perfil de sistema conserva o nome: há código e configuração que dependem dele.
            if (!DeSistema) perfil.Name = Dados.Nome.Trim();
            perfil.Descricao = Dados.Descricao;
        }

        var resultado = await _utilizadores.GuardarPerfilAsync(perfil, Dados.Permissoes, id is null, ct);

        if (!resultado.Sucesso)
        {
            foreach (var erro in resultado.Erros)
            {
                ModelState.AddModelError(erro.Campo is null ? string.Empty : $"Dados.{erro.Campo}", erro.Mensagem);
            }
            return Page();
        }

        this.Sucesso(id is null ? $"Perfil \"{perfil.Name}\" criado." : $"Perfil \"{perfil.Name}\" atualizado.");
        return Redirect("/perfis");
    }
}
