using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Veiculos;

[Authorize(Policy = Permissoes.Politicas.VeiculosVer)]
public class DetalhesModel : PageModel
{
    private readonly VeiculosServico _veiculos;
    private readonly HistoricoServico _historico;
    private readonly IPermissoesServico _permissoes;
    private readonly IRelogio _relogio;

    public DetalhesModel(
        VeiculosServico veiculos,
        HistoricoServico historico,
        IPermissoesServico permissoes,
        IRelogio relogio)
    {
        _veiculos = veiculos;
        _historico = historico;
        _permissoes = permissoes;
        _relogio = relogio;
    }

    public Veiculo Veiculo { get; private set; } = null!;

    public IReadOnlyList<HistoricoLinha> Historico { get; private set; } = Array.Empty<HistoricoLinha>();

    [FromQuery(Name = "ver")]
    public string Separador { get; set; } = "dados";

    public DateOnly Hoje { get; private set; }

    public bool PodeEditar { get; private set; }
    public bool PodeEliminar { get; private set; }
    public bool PodeVerClientes { get; private set; }
    public bool PodeVerHistorico { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var veiculo = await _veiculos.ObterPorIdAsync(id, ct);
        if (veiculo is null) return NotFound();

        Veiculo = veiculo;
        Hoje = _relogio.HojeLocal;

        PodeEditar = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Editar);
        PodeEliminar = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Eliminar);
        PodeVerClientes = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Ver);
        PodeVerHistorico = await _permissoes.TemPermissaoAsync(User, Permissoes.Historico.Ver);

        if (Separador != "historico" || !PodeVerHistorico) Separador = "dados";

        if (Separador == "historico")
        {
            Historico = await _historico.ObterDoRegistoAsync(nameof(Veiculo), id.ToString(), 200, ct);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEliminarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Eliminar)) return Forbid();

        var resultado = await _veiculos.EliminarAsync(id, ct);

        if (!resultado.Sucesso)
        {
            this.Erro(resultado.Erros[0].Mensagem);
            return Redirect($"/veiculos/{id}");
        }

        this.Sucesso("Veículo eliminado. O registo continua no histórico.");
        return Redirect("/veiculos");
    }

    public async Task<IActionResult> OnPostReativarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Eliminar)) return Forbid();

        var resultado = await _veiculos.ReativarAsync(id, ct);

        if (resultado.Sucesso) this.Sucesso("Veículo reativado.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return Redirect($"/veiculos/{id}");
    }
}
