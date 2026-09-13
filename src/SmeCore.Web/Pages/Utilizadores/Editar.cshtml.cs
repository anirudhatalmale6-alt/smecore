using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Utilizadores;

/// <summary>Serve /utilizadores/novo e /utilizadores/{id}/editar.</summary>
[Authorize(Policy = Permissoes.Politicas.UtilizadoresGerir)]
public class EditarModel : PageModel
{
    private readonly UtilizadoresServico _utilizadores;

    public EditarModel(UtilizadoresServico utilizadores) => _utilizadores = utilizadores;

    public class DadosUtilizador
    {
        [Required(ErrorMessage = "Indique o nome completo.")]
        [StringLength(200)]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indique o e-mail.")]
        [EmailAddress(ErrorMessage = "O e-mail indicado não é válido.")]
        [StringLength(200)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Cargo")]
        public string? Cargo { get; set; }

        [Phone(ErrorMessage = "O número de telefone não é válido.")]
        [StringLength(30)]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [Display(Name = "Conta ativa")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Perfis")]
        public List<string> Perfis { get; set; } = new();

        [StringLength(100, MinimumLength = 10, ErrorMessage = "A palavra-passe deve ter pelo menos 10 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Palavra-passe")]
        public string? PalavraPasse { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar palavra-passe")]
        [Compare(nameof(PalavraPasse), ErrorMessage = "As palavras-passe não coincidem.")]
        public string? ConfirmarPalavraPasse { get; set; }

        [Display(Name = "Obrigar a alterar a palavra-passe no próximo início de sessão")]
        public bool ObrigarAlteracao { get; set; } = true;
    }

    [BindProperty]
    public DadosUtilizador Dados { get; set; } = new();

    public Guid? Id { get; private set; }
    public bool Novo => Id is null;

    public List<Perfil> PerfisDisponiveis { get; private set; } = new();

    public DateTimeOffset? UltimoAcesso { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        Id = id;
        PerfisDisponiveis = await _utilizadores.ObterPerfisAsync(ct);

        if (id is null)
        {
            Dados.Perfis.Add(Permissoes.PerfilUtilizador);
            return Page();
        }

        var utilizador = await _utilizadores.ObterPorIdAsync(id.Value, ct);
        if (utilizador is null) return NotFound();

        UltimoAcesso = utilizador.UltimoAcessoEm;

        Dados = new DadosUtilizador
        {
            NomeCompleto = utilizador.NomeCompleto,
            Email = utilizador.Email ?? string.Empty,
            Cargo = utilizador.Cargo,
            Telefone = utilizador.PhoneNumber,
            Ativo = utilizador.Ativo,
            Perfis = await _utilizadores.ObterPerfisDoUtilizadorAsync(utilizador),
            ObrigarAlteracao = false
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        Id = id;
        PerfisDisponiveis = await _utilizadores.ObterPerfisAsync(ct);

        if (id is null && string.IsNullOrWhiteSpace(Dados.PalavraPasse))
        {
            ModelState.AddModelError("Dados.PalavraPasse", "Defina uma palavra-passe inicial para a nova conta.");
        }

        if (!ModelState.IsValid) return Page();

        if (id is null)
        {
            var novo = new Utilizador
            {
                Email = Dados.Email.Trim(),
                UserName = Dados.Email.Trim(),
                EmailConfirmed = true,
                NomeCompleto = Dados.NomeCompleto,
                Cargo = Dados.Cargo,
                PhoneNumber = Dados.Telefone,
                Ativo = Dados.Ativo,
                DeveAlterarPalavraPasse = Dados.ObrigarAlteracao
            };

            var criacao = await _utilizadores.CriarAsync(novo, Dados.PalavraPasse!, Dados.Perfis, ct);

            if (!criacao.Sucesso)
            {
                foreach (var erro in criacao.Erros) ModelState.AddModelError(string.Empty, erro.Mensagem);
                return Page();
            }

            this.Sucesso($"Conta de {novo.NomeApresentacao} criada.");
            return Redirect("/utilizadores");
        }

        var utilizador = await _utilizadores.ObterPorIdAsync(id.Value, ct);
        if (utilizador is null) return NotFound();

        utilizador.NomeCompleto = Dados.NomeCompleto;
        utilizador.Email = Dados.Email.Trim();
        utilizador.Cargo = Dados.Cargo;
        utilizador.PhoneNumber = Dados.Telefone;
        utilizador.Ativo = Dados.Ativo;

        var atualizacao = await _utilizadores.AtualizarAsync(utilizador, Dados.Perfis, ct);

        if (!atualizacao.Sucesso)
        {
            foreach (var erro in atualizacao.Erros) ModelState.AddModelError(string.Empty, erro.Mensagem);
            return Page();
        }

        // A palavra-passe só é mexida se o campo tiver sido preenchido: gravar o formulário
        // sem tocar nesse campo não deve redefinir nada.
        if (!string.IsNullOrWhiteSpace(Dados.PalavraPasse))
        {
            var senha = await _utilizadores.DefinirPalavraPasseAsync(
                id.Value, Dados.PalavraPasse, Dados.ObrigarAlteracao, ct);

            if (!senha.Sucesso)
            {
                foreach (var erro in senha.Erros) ModelState.AddModelError("Dados.PalavraPasse", erro.Mensagem);
                return Page();
            }
        }

        this.Sucesso($"Conta de {utilizador.NomeApresentacao} atualizada.");
        return Redirect("/utilizadores");
    }
}
