using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Consultas;
using SmeCore.Infrastructure.Exportacao;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Veiculos;

[Authorize(Policy = Permissoes.Politicas.VeiculosVer)]
public class IndexModel : PageModel
{
    private readonly VeiculosServico _veiculos;
    private readonly ClientesServico _clientes;
    private readonly IPermissoesServico _permissoes;
    private readonly IRelogio _relogio;

    public IndexModel(
        VeiculosServico veiculos,
        ClientesServico clientes,
        IPermissoesServico permissoes,
        IRelogio relogio)
    {
        _veiculos = veiculos;
        _clientes = clientes;
        _permissoes = permissoes;
        _relogio = relogio;
    }

    [BindProperty(SupportsGet = true, Name = "")]
    public VeiculosFiltro Filtro { get; set; } = new();

    public ResultadoPaginado<VeiculoListagem> Pagina { get; private set; } = ResultadoPaginado<VeiculoListagem>.Vazio();

    public List<string> Marcas { get; private set; } = new();

    /// <summary>Nome do cliente quando a lista está filtrada por um cliente concreto.</summary>
    public string? NomeCliente { get; private set; }

    public DateOnly Hoje { get; private set; }

    public bool PodeCriar { get; private set; }
    public bool PodeEditar { get; private set; }
    public bool PodeEliminar { get; private set; }
    public bool PodeExportar { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await CarregarAsync(ct);

    public async Task<IActionResult> OnGetTabelaAsync(CancellationToken ct)
    {
        await CarregarAsync(ct);
        Response.Headers["HX-Push-Url"] = Tabelas.ConstruirUrl(Request.Path, Ligacoes.SemHandler(Request));
        return Partial("_Tabela", this);
    }

    public async Task<IActionResult> OnGetExportarAsync(string formato, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Exportar)) return Forbid();

        var (itens, truncado) = await _veiculos.Consultar(Filtro).ParaExportacaoAsync(ct);

        var agora = _relogio.ParaLocal(_relogio.AgoraUtc);
        var subtitulo = truncado
            ? $"Veículos exportados em {Formatos.DataHora(agora)} — limitado aos primeiros {PaginacaoExtensions.LimiteExportacao:N0} registos"
            : $"Veículos exportados em {Formatos.DataHora(agora)} — {itens.Count} registo(s)";

        var tabela = new TabelaExportacao<VeiculoListagem>(
            "Veículos",
            ExportacaoBuilder.Colunas<VeiculoListagem>(
                ("Matrícula", v => v.Matricula),
                ("Marca", v => v.Marca),
                ("Modelo", v => v.Modelo),
                ("Versão", v => v.Versao),
                ("Ano", v => v.Ano),
                ("Combustível", v => Formatos.Enumeracao(v.Combustivel)),
                ("Quilometragem", v => v.Quilometragem),
                ("Próxima inspeção", v => v.ProximaInspecao),
                ("Cliente", v => v.ClienteNome),
                ("N.º cliente", v => v.ClienteNumero),
                ("Ativo", v => v.Ativo),
                ("Criado em", v => _relogio.ParaLocal(v.CriadoEm).DateTime)),
            itens)
        {
            Subtitulo = subtitulo
        };

        var dataFicheiro = agora.ToString("yyyyMMdd-HHmm");

        if (string.Equals(formato, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return File(XlsxExportador.Gerar(tabela), XlsxExportador.TipoConteudo, $"veiculos-{dataFicheiro}.xlsx");
        }

        return File(CsvExportador.Gerar(tabela), CsvExportador.TipoConteudo, $"veiculos-{dataFicheiro}.csv");
    }

    public async Task<IActionResult> OnPostEliminarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Eliminar)) return Forbid();

        var resultado = await _veiculos.EliminarAsync(id, ct);
        if (resultado.Sucesso) this.Sucesso("Veículo eliminado. O registo continua no histórico.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage(new { });
    }

    public async Task<IActionResult> OnPostReativarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Eliminar)) return Forbid();

        var resultado = await _veiculos.ReativarAsync(id, ct);
        if (resultado.Sucesso) this.Sucesso("Veículo reativado.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage(new { });
    }

    private async Task CarregarAsync(CancellationToken ct)
    {
        Hoje = _relogio.HojeLocal;

        PodeCriar = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Criar);
        PodeEditar = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Editar);
        PodeEliminar = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Eliminar);
        PodeExportar = await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Exportar);

        if (!PodeEliminar) Filtro.IncluirEliminados = false;

        Pagina = await _veiculos.ObterPaginaAsync(Filtro, ct);
        Marcas = await _veiculos.ObterMarcasAsync(ct);

        if (Filtro.ClienteId.HasValue)
        {
            var cliente = await _clientes.ObterPorIdAsync(Filtro.ClienteId.Value, ct);
            NomeCliente = cliente?.Nome;
        }
    }

    public IEnumerable<(string Texto, string Url)> FiltrosAtivos()
    {
        if (!string.IsNullOrWhiteSpace(Filtro.Pesquisa))
        {
            yield return ($"Pesquisa: {Filtro.Pesquisa}", UrlSem("pesquisa"));
        }

        if (Filtro.ClienteId.HasValue)
        {
            yield return ($"Cliente: {NomeCliente ?? "selecionado"}", UrlSem("clienteId"));
        }

        if (!string.IsNullOrWhiteSpace(Filtro.Marca))
        {
            yield return ($"Marca: {Filtro.Marca}", UrlSem("marca"));
        }

        if (Filtro.Combustivel.HasValue)
        {
            yield return ($"Combustível: {Formatos.Enumeracao(Filtro.Combustivel.Value)}", UrlSem("combustivel"));
        }

        if (Filtro.Ativo.HasValue)
        {
            yield return (Filtro.Ativo.Value ? "Apenas ativos" : "Apenas inativos", UrlSem("ativo"));
        }

        if (Filtro.AnoDe.HasValue)
        {
            yield return ($"A partir de {Filtro.AnoDe}", UrlSem("anoDe"));
        }

        if (Filtro.AnoAte.HasValue)
        {
            yield return ($"Até {Filtro.AnoAte}", UrlSem("anoAte"));
        }

        if (Filtro.InspecaoVencida)
        {
            yield return ("Inspeção vencida", UrlSem("inspecaoVencida"));
        }

        if (Filtro.InspecaoProximosDias.HasValue)
        {
            yield return ($"Inspeção nos próximos {Filtro.InspecaoProximosDias} dias", UrlSem("inspecaoProximosDias"));
        }

        if (Filtro.IncluirEliminados)
        {
            yield return ("A incluir eliminados", UrlSem("incluirEliminados"));
        }
    }

    private string UrlSem(string chave)
        => Tabelas.ConstruirUrl(Request.Path, Ligacoes.Com(Request, (chave, null), ("pagina", null)));
}
