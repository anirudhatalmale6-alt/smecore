using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Auditoria;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Dados;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Web.Pages.Conta;

public class EntrarModel : PageModel
{
    private readonly SignInManager<Utilizador> _sessoes;
    private readonly UserManager<Utilizador> _utilizadores;
    private readonly AppDbContext _db;
    private readonly IRelogio _relogio;
    private readonly ILogger<EntrarModel> _log;

    public EntrarModel(
        SignInManager<Utilizador> sessoes,
        UserManager<Utilizador> utilizadores,
        AppDbContext db,
        IRelogio relogio,
        ILogger<EntrarModel> log)
    {
        _sessoes = sessoes;
        _utilizadores = utilizadores;
        _db = db;
        _relogio = relogio;
        _log = log;
    }

    public class DadosEntrada
    {
        [Required(ErrorMessage = "Indique o e-mail.")]
        [EmailAddress(ErrorMessage = "O e-mail indicado não é válido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indique a palavra-passe.")]
        [DataType(DataType.Password)]
        [Display(Name = "Palavra-passe")]
        public string PalavraPasse { get; set; } = string.Empty;

        [Display(Name = "Manter a sessão iniciada")]
        public bool Memorizar { get; set; }
    }

    [BindProperty]
    public DadosEntrada Entrada { get; set; } = new();

    public string? Mensagem { get; private set; }

    [FromQuery(Name = "regressar")]
    public string? Regressar { get; set; }

    public IActionResult OnGet()
    {
        // Quem já tem sessão não precisa do formulário.
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect("/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var utilizador = await _utilizadores.FindByEmailAsync(Entrada.Email.Trim());

        // A mensagem é sempre a mesma, exista a conta ou não: dizer "este e-mail não existe"
        // permitiria descobrir quem tem conta na aplicação.
        const string erroGenerico = "E-mail ou palavra-passe incorretos.";

        if (utilizador is null)
        {
            _log.LogInformation("Tentativa de sessão com um e-mail inexistente.");
            Mensagem = erroGenerico;
            return Page();
        }

        var resultado = await _sessoes.PasswordSignInAsync(
            utilizador,
            Entrada.PalavraPasse,
            Entrada.Memorizar,
            lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            Mensagem = "A conta está temporariamente bloqueada por tentativas falhadas. Tente novamente dentro de 15 minutos.";
            return Page();
        }

        if (resultado.IsNotAllowed)
        {
            // É o caso de uma conta desativada, recusada pelo GestorSessao.
            Mensagem = "Esta conta está desativada. Contacte um administrador.";
            return Page();
        }

        if (!resultado.Succeeded)
        {
            Mensagem = erroGenerico;
            return Page();
        }

        utilizador.UltimoAcessoEm = _relogio.AgoraUtc;

        _db.RegistosAuditoria.Add(new RegistoAuditoria
        {
            Instante = _relogio.AgoraUtc,
            Entidade = nameof(Utilizador),
            EntidadeId = utilizador.Id.ToString(),
            EntidadeDescricao = $"{utilizador.NomeApresentacao} ({utilizador.Email})",
            Acao = AcaoAuditoria.Autenticacao,
            UtilizadorId = utilizador.Id,
            UtilizadorNome = utilizador.NomeApresentacao,
            Origem = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _db.SaveChangesAsync();

        if (utilizador.DeveAlterarPalavraPasse)
        {
            return LocalRedirect("/a-minha-conta?alterar=1");
        }

        // LocalRedirect recusa endereços absolutos: impede que um link preparado
        // (?regressar=https://...) envie o utilizador para fora depois de autenticar.
        if (!string.IsNullOrWhiteSpace(Regressar) && Url.IsLocalUrl(Regressar))
        {
            return LocalRedirect(Regressar);
        }

        return LocalRedirect("/");
    }
}
