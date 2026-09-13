using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Clientes;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Clientes;

[Authorize(Policy = Permissoes.Politicas.ClientesVer)]
public class DetalhesModel : PageModel
{
    private readonly ClientesServico _clientes;
    private readonly HistoricoServico _historico;
    private readonly IPermissoesServico _permissoes;

    public DetalhesModel(ClientesServico clientes, HistoricoServico historico, IPermissoesServico permissoes)
    {
        _clientes = clientes;
        _historico = historico;
        _permissoes = permissoes;
    }

    public Cliente Cliente { get; private set; } = null!;

    public IReadOnlyList<HistoricoLinha> Historico { get; private set; } = Array.Empty<HistoricoLinha>();

    /// <summary>Separador ativo: "dados", "veiculos" ou "historico".</summary>
    [FromQuery(Name = "ver")]
    public string Separador { get; set; } = "dados";

    public bool PodeEditar { get; private set; }
    public bool PodeEliminar { get; private set; }
    public bool PodeVerVeiculos { get; private set; }
    public bool PodeCriarVeiculos { get; private set; }
    public bool PodeVerHistorico { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var cliente = await _clientes.ObterPorIdAsync(id, ct);
        if (cliente is null) return NotFound();

        Cliente = cliente;

        PodeEditar = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Editar);
        PodeEliminar = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Eliminar);
        PodeVerVeiculos = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Ver);
        PodeCriarVeiculos = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Criar);
        PodeVerHistorico = await _permissoes.TemPermissaoAsync(User, Permissoes.Historico.Ver);

        if (Separador is not ("dados" or "veiculos" or "historico")) Separador = "dados";
        if (Separador == "historico" && !PodeVerHistorico) Separador = "dados";
        if (Separador == "veiculos" && !PodeVerVeiculos) Separador = "dados";

        if (Separador == "historico")
        {
            Historico = await _historico.ObterDoRegistoAsync(nameof(Cliente), id.ToString(), 200, ct);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEliminarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Eliminar)) return Forbid();

        var resultado = await _clientes.EliminarAsync(id, ct);

        if (!resultado.Sucesso)
        {
            this.Erro(resultado.Erros[0].Mensagem);
            return Redirect($"/clientes/{id}");
        }

        this.Sucesso("Cliente eliminado. O registo continua no histórico.");
        return Redirect("/clientes");
    }

    public async Task<IActionResult> OnPostReativarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Eliminar)) return Forbid();

        var resultado = await _clientes.ReativarAsync(id, ct);

        if (resultado.Sucesso) this.Sucesso("Cliente reativado.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return Redirect($"/clientes/{id}");
    }
}
