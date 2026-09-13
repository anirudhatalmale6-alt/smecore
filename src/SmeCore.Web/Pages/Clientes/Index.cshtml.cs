using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;
using SmeCore.Domain.Validacao;
using SmeCore.Infrastructure.Consultas;
using SmeCore.Infrastructure.Exportacao;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Clientes;

[Authorize(Policy = Permissoes.Politicas.ClientesVer)]
public class IndexModel : PageModel
{
    private readonly ClientesServico _clientes;
    private readonly IPermissoesServico _permissoes;
    private readonly IRelogio _relogio;

    public IndexModel(ClientesServico clientes, IPermissoesServico permissoes, IRelogio relogio)
    {
        _clientes = clientes;
        _permissoes = permissoes;
        _relogio = relogio;
    }

    // Name = "" faz o modelo ligar-se aos parâmetros da query string sem prefixo, para que os
    // endereços fiquem legíveis (/clientes?pesquisa=ana&tipo=2) e possam ser partilhados.
    [BindProperty(SupportsGet = true, Name = "")]
    public ClientesFiltro Filtro { get; set; } = new();

    public ResultadoPaginado<ClienteListagem> Pagina { get; private set; } = ResultadoPaginado<ClienteListagem>.Vazio();

    public List<string> Localidades { get; private set; } = new();

    public bool PodeCriar { get; private set; }
    public bool PodeEditar { get; private set; }
    public bool PodeEliminar { get; private set; }
    public bool PodeExportar { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await CarregarAsync(ct);
    }

    /// <summary>
    /// A listagem também responde a pedidos do HTMX, devolvendo apenas o corpo da tabela.
    /// É o que permite pesquisar sem recarregar a página inteira, mantendo exatamente o mesmo
    /// código de consulta do pedido normal.
    /// </summary>
    public async Task<IActionResult> OnGetTabelaAsync(CancellationToken ct)
    {
        await CarregarAsync(ct);

        // A barra de endereço passa a refletir os filtros, mas sem o handler: assim o endereço
        // que o utilizador copia (ou recarrega) devolve a página completa e não um fragmento.
        Response.Headers["HX-Push-Url"] = Tabelas.ConstruirUrl(Request.Path, Ligacoes.SemHandler(Request));

        return Partial("_Tabela", this);
    }

    public async Task<IActionResult> OnGetExportarAsync(string formato, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Exportar))
        {
            return Forbid();
        }

        // A exportação usa exatamente a mesma consulta da listagem, sem paginação: o que o
        // utilizador vê no ecrã é o que sai no ficheiro.
        var consulta = _clientes.Consultar(Filtro);
        var (itens, truncado) = await consulta.ParaExportacaoAsync(ct);

        var agora = _relogio.ParaLocal(_relogio.AgoraUtc);
        var subtitulo = truncado
            ? $"Clientes exportados em {Formatos.DataHora(agora)} — limitado aos primeiros {PaginacaoExtensions.LimiteExportacao:N0} registos"
            : $"Clientes exportados em {Formatos.DataHora(agora)} — {itens.Count} registo(s)";

        var tabela = new TabelaExportacao<ClienteListagem>(
            "Clientes",
            ExportacaoBuilder.Colunas<ClienteListagem>(
                ("N.º", c => c.Numero),
                ("Tipo", c => Formatos.Enumeracao(c.Tipo)),
                ("Nome", c => c.Nome),
                ("NIF", c => Nif.Formatar(c.Nif)),
                ("E-mail", c => c.Email),
                ("Telefone", c => c.Telefone),
                ("Telemóvel", c => c.Telemovel),
                ("Código postal", c => c.CodigoPostal),
                ("Localidade", c => c.Localidade),
                ("Veículos", c => c.TotalVeiculos),
                ("Ativo", c => c.Ativo),
                ("Criado em", c => _relogio.ParaLocal(c.CriadoEm).DateTime),
                ("Última alteração", c => c.AlteradoEm.HasValue ? _relogio.ParaLocal(c.AlteradoEm.Value).DateTime : null)),
            itens)
        {
            Subtitulo = subtitulo
        };

        var dataFicheiro = agora.ToString("yyyyMMdd-HHmm");

        if (string.Equals(formato, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return File(XlsxExportador.Gerar(tabela), XlsxExportador.TipoConteudo, $"clientes-{dataFicheiro}.xlsx");
        }

        return File(CsvExportador.Gerar(tabela), CsvExportador.TipoConteudo, $"clientes-{dataFicheiro}.csv");
    }

    public async Task<IActionResult> OnPostEliminarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Eliminar)) return Forbid();

        var resultado = await _clientes.EliminarAsync(id, ct);

        if (resultado.Sucesso) this.Sucesso("Cliente eliminado. O registo continua no histórico.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage(new { });
    }

    public async Task<IActionResult> OnPostReativarAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Eliminar)) return Forbid();

        var resultado = await _clientes.ReativarAsync(id, ct);

        if (resultado.Sucesso) this.Sucesso("Cliente reativado.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage(new { });
    }

    private async Task CarregarAsync(CancellationToken ct)
    {
        PodeCriar = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Criar);
        PodeEditar = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Editar);
        PodeEliminar = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Eliminar);
        PodeExportar = await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Exportar);

        // Só quem pode eliminar vê registos eliminados; caso contrário o filtro é ignorado.
        if (!PodeEliminar) Filtro.IncluirEliminados = false;

        Pagina = await _clientes.ObterPaginaAsync(Filtro, ct);
        Localidades = await _clientes.ObterLocalidadesAsync(ct);
    }

    public IEnumerable<(string Texto, string Url)> FiltrosAtivos()
    {
        if (!string.IsNullOrWhiteSpace(Filtro.Pesquisa))
        {
            yield return ($"Pesquisa: {Filtro.Pesquisa}", UrlSem("pesquisa"));
        }

        if (Filtro.Tipo.HasValue)
        {
            yield return ($"Tipo: {Formatos.Enumeracao(Filtro.Tipo.Value)}", UrlSem("tipo"));
        }

        if (Filtro.Ativo.HasValue)
        {
            yield return (Filtro.Ativo.Value ? "Apenas ativos" : "Apenas inativos", UrlSem("ativo"));
        }

        if (Filtro.ComVeiculos.HasValue)
        {
            yield return (Filtro.ComVeiculos.Value ? "Com veículos" : "Sem veículos", UrlSem("comVeiculos"));
        }

        if (!string.IsNullOrWhiteSpace(Filtro.Localidade))
        {
            yield return ($"Localidade: {Filtro.Localidade}", UrlSem("localidade"));
        }

        if (Filtro.IncluirEliminados)
        {
            yield return ("A incluir eliminados", UrlSem("incluirEliminados"));
        }
    }

    private string UrlSem(string chave)
    {
        var parametros = Ligacoes.Com(Request, (chave, null), ("pagina", null));
        return Tabelas.ConstruirUrl(Request.Path, parametros);
    }
}
