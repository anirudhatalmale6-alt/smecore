using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;

namespace SmeCore.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ClientesServico _clientes;
    private readonly VeiculosServico _veiculos;
    private readonly HistoricoServico _historico;
    private readonly IPermissoesServico _permissoes;
    private readonly IRelogio _relogio;

    public IndexModel(
        ClientesServico clientes,
        VeiculosServico veiculos,
        HistoricoServico historico,
        IPermissoesServico permissoes,
        IRelogio relogio)
    {
        _clientes = clientes;
        _veiculos = veiculos;
        _historico = historico;
        _permissoes = permissoes;
        _relogio = relogio;
    }

    public int TotalClientes { get; private set; }
    public int ClientesInativos { get; private set; }
    public int TotalVeiculos { get; private set; }
    public int InspecoesVencidas { get; private set; }
    public int InspecoesProximas { get; private set; }

    public IReadOnlyList<VeiculoListagem> ProximasInspecoes { get; private set; } = Array.Empty<VeiculoListagem>();
    public IReadOnlyList<ClienteListagem> UltimosClientes { get; private set; } = Array.Empty<ClienteListagem>();
    public IReadOnlyList<HistoricoLinha> Atividade { get; private set; } = Array.Empty<HistoricoLinha>();

    public bool PodeVerClientes { get; private set; }
    public bool PodeVerVeiculos { get; private set; }
    public bool PodeVerHistorico { get; private set; }

    public DateOnly Hoje { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Hoje = _relogio.HojeLocal;

        PodeVerClientes = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Ver);
        PodeVerVeiculos = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Ver);
        PodeVerHistorico = await _permissoes.TemPermissaoAsync(User, Permissoes.Historico.Ver);

        if (PodeVerClientes)
        {
            var todos = await _clientes.ObterPaginaAsync(new ClientesFiltro { TamanhoPagina = 1 }, ct);
            TotalClientes = todos.TotalItens;

            var inativos = await _clientes.ObterPaginaAsync(new ClientesFiltro { Ativo = false, TamanhoPagina = 1 }, ct);
            ClientesInativos = inativos.TotalItens;

            var recentes = await _clientes.ObterPaginaAsync(new ClientesFiltro
            {
                Ordenar = "criado",
                Direcao = "desc",
                TamanhoPagina = 5
            }, ct);
            UltimosClientes = recentes.Itens;
        }

        if (PodeVerVeiculos)
        {
            var resumo = await _veiculos.ObterResumoInspecoesAsync(ct);
            TotalVeiculos = resumo.Total;
            InspecoesVencidas = resumo.Vencidos;
            InspecoesProximas = resumo.Proximos30;

            var proximas = await _veiculos.ObterPaginaAsync(new VeiculosFiltro
            {
                Ordenar = "inspecao",
                Direcao = "asc",
                TamanhoPagina = 8,
                // Inclui as vencidas e as dos próximos 60 dias: é a lista de trabalho de quem
                // está na receção, não um relatório.
                InspecaoProximosDias = 60
            }, ct);

            var vencidas = await _veiculos.ObterPaginaAsync(new VeiculosFiltro
            {
                Ordenar = "inspecao",
                Direcao = "asc",
                TamanhoPagina = 8,
                InspecaoVencida = true
            }, ct);

            ProximasInspecoes = vencidas.Itens.Concat(proximas.Itens).Take(8).ToList();
        }

        if (PodeVerHistorico)
        {
            var atividade = await _historico.ObterPaginaAsync(new HistoricoFiltro { TamanhoPagina = 8 }, ct);
            Atividade = atividade.Itens;
        }
    }
}
