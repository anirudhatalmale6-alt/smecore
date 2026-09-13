using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Dados;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages;

/// <summary>
/// Fica fora da pasta /Conta de propósito: essa pasta é pública (formulário de entrada) e esta
/// página exige sessão iniciada.
/// </summary>
public class MinhaContaModel : PageModel
{
    private readonly UserManager<Utilizador> _utilizadores;
    private readonly SignInManager<Utilizador> _sessoes;
    private readonly IPermissoesServico _permissoes;
    private readonly AppDbContext _db;

    public MinhaContaModel(
        UserManager<Utilizador> utilizadores,
        SignInManager<Utilizador> sessoes,
        IPermissoesServico permissoes,
        AppDbContext db)
    {
        _utilizadores = utilizadores;
        _sessoes = sessoes;
        _permissoes = permissoes;
        _db = db;
    }

    public class DadosPerfil
    {
        [Required(ErrorMessage = "Indique o nome completo.")]
        [StringLength(200)]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Phone(ErrorMessage = "O número de telefone não é válido.")]
        [StringLength(30)]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }
    }

    public class DadosPalavraPasse
    {
        [Required(ErrorMessage = "Indique a palavra-passe atual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Palavra-passe atual")]
        public string Atual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indique a nova palavra-passe.")]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "A palavra-passe deve ter pelo menos 10 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova palavra-passe")]
        public string Nova { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova palavra-passe")]
        [Compare(nameof(Nova), ErrorMessage = "As palavras-passe não coincidem.")]
        public string Confirmar { get; set; } = string.Empty;
    }

    [BindProperty]
    public DadosPerfil Perfil { get; set; } = new();

    [BindProperty]
    public DadosPalavraPasse Senha { get; set; } = new();

    public string Email { get; private set; } = string.Empty;
    public string? Cargo { get; private set; }
    public List<string> Perfis { get; private set; } = new();
    public IReadOnlyList<string> PermissoesEfetivas { get; private set; } = Array.Empty<string>();
    public DateTimeOffset? UltimoAcesso { get; private set; }
    public bool TemDeAlterarPalavraPasse { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var utilizador = await ObterAsync();
        if (utilizador is null) return RedirectToPage("/Conta/Entrar");

        Perfil = new DadosPerfil
        {
            NomeCompleto = utilizador.NomeCompleto,
            Telefone = utilizador.PhoneNumber
        };

        await PreencherAsync(utilizador);
        return Page();
    }

    public async Task<IActionResult> OnPostPerfilAsync(CancellationToken ct)
    {
        var utilizador = await ObterAsync();
        if (utilizador is null) return RedirectToPage("/Conta/Entrar");

        // O bloco da palavra-passe não é submetido neste formulário: os seus erros de
        // obrigatoriedade não devem impedir a gravação do nome.
        LimparErros(nameof(Senha));

        if (!ModelState.IsValid)
        {
            await PreencherAsync(utilizador);
            return Page();
        }

        utilizador.NomeCompleto = Perfil.NomeCompleto.Trim();
        utilizador.PhoneNumber = Perfil.Telefone;

        await _db.SaveChangesAsync(ct);

        // Atualiza o cookie para o cabeçalho passar a mostrar o nome novo de imediato.
        await _sessoes.RefreshSignInAsync(utilizador);

        this.Sucesso("Dados atualizados.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPalavraPasseAsync(CancellationToken ct)
    {
        var utilizador = await ObterAsync();
        if (utilizador is null) return RedirectToPage("/Conta/Entrar");

        LimparErros(nameof(Perfil));

        if (!ModelState.IsValid)
        {
            await PreencherAsync(utilizador);
            return Page();
        }

        var resultado = await _utilizadores.ChangePasswordAsync(utilizador, Senha.Atual, Senha.Nova);

        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
            {
                var campo = erro.Code == "PasswordMismatch" ? "Senha.Atual" : "Senha.Nova";
                ModelState.AddModelError(campo, Traduzir(erro));
            }

            await PreencherAsync(utilizador);
            return Page();
        }

        utilizador.DeveAlterarPalavraPasse = false;
        await _db.SaveChangesAsync(ct);

        await _sessoes.RefreshSignInAsync(utilizador);

        this.Sucesso("Palavra-passe alterada.");
        return RedirectToPage();
    }

    private async Task<Utilizador?> ObterAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is null ? null : await _utilizadores.FindByIdAsync(id);
    }

    private async Task PreencherAsync(Utilizador utilizador)
    {
        Email = utilizador.Email ?? string.Empty;
        Cargo = utilizador.Cargo;
        UltimoAcesso = utilizador.UltimoAcessoEm;
        TemDeAlterarPalavraPasse = utilizador.DeveAlterarPalavraPasse;
        Perfis = (await _utilizadores.GetRolesAsync(utilizador)).OrderBy(p => p).ToList();

        var permissoes = await _permissoes.ObterPermissoesAsync(User);
        PermissoesEfetivas = permissoes
            .Select(Permissoes.Titulo)
            .OrderBy(t => t, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>Descarta os erros do bloco que não foi submetido neste POST.</summary>
    private void LimparErros(string prefixo)
    {
        foreach (var chave in ModelState.Keys.Where(k => k.StartsWith(prefixo + ".", StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(chave);
        }
    }

    private static string Traduzir(IdentityError erro) => erro.Code switch
    {
        "PasswordMismatch" => "A palavra-passe atual não está correta.",
        "PasswordTooShort" => "A palavra-passe deve ter pelo menos 10 caracteres.",
        "PasswordRequiresDigit" => "A palavra-passe deve conter pelo menos um número.",
        "PasswordRequiresUpper" => "A palavra-passe deve conter pelo menos uma letra maiúscula.",
        "PasswordRequiresLower" => "A palavra-passe deve conter pelo menos uma letra minúscula.",
        _ => erro.Description
    };
}
