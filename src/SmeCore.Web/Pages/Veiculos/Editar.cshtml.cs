using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Common;
using SmeCore.Domain.Validacao;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Veiculos;

/// <summary>Serve /veiculos/novo e /veiculos/{id}/editar (ver AddPageRoute no Program.cs).</summary>
[Authorize(Policy = Permissoes.Politicas.VeiculosVer)]
public class EditarModel : PageModel
{
    private readonly VeiculosServico _veiculos;
    private readonly ClientesServico _clientes;
    private readonly IPermissoesServico _permissoes;
    private readonly IRelogio _relogio;

    public EditarModel(
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

    public class DadosVeiculo
    {
        [Required(ErrorMessage = "Indique a matrícula.")]
        [MatriculaValida]
        [StringLength(20)]
        [Display(Name = "Matrícula")]
        public string Matricula { get; set; } = string.Empty;

        [VinValido]
        [StringLength(20)]
        [Display(Name = "VIN / N.º de chassis")]
        public string? Vin { get; set; }

        [Required(ErrorMessage = "Indique a marca.")]
        [StringLength(80)]
        [Display(Name = "Marca")]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indique o modelo.")]
        [StringLength(120)]
        [Display(Name = "Modelo")]
        public string Modelo { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Versão")]
        public string? Versao { get; set; }

        [Range(1900, 2100, ErrorMessage = "O ano deve estar entre 1900 e 2100.")]
        [Display(Name = "Ano")]
        public int? Ano { get; set; }

        [Display(Name = "Data da 1.ª matrícula")]
        [DataType(DataType.Date)]
        public DateOnly? DataPrimeiraMatricula { get; set; }

        [Display(Name = "Combustível")]
        public TipoCombustivel Combustivel { get; set; } = TipoCombustivel.Gasolina;

        [Display(Name = "Caixa")]
        public TipoCaixa Caixa { get; set; } = TipoCaixa.NaoDefinida;

        [Range(0, 10000, ErrorMessage = "A cilindrada deve estar entre 0 e 10 000 cm³.")]
        [Display(Name = "Cilindrada (cm³)")]
        public int? Cilindrada { get; set; }

        [Range(0, 2000, ErrorMessage = "A potência deve estar entre 0 e 2000 cv.")]
        [Display(Name = "Potência (cv)")]
        public int? Potencia { get; set; }

        [StringLength(60)]
        [Display(Name = "Cor")]
        public string? Cor { get; set; }

        [Range(0, 3000000, ErrorMessage = "A quilometragem deve estar entre 0 e 3 000 000 km.")]
        [Display(Name = "Quilometragem (km)")]
        public int? Quilometragem { get; set; }

        [Display(Name = "Próxima inspeção")]
        [DataType(DataType.Date)]
        public DateOnly? ProximaInspecao { get; set; }

        [StringLength(4000)]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Display(Name = "Veículo ativo")]
        public bool Ativo { get; set; } = true;

        [Required(ErrorMessage = "Escolha o cliente proprietário.")]
        [Display(Name = "Cliente proprietário")]
        public Guid ClienteId { get; set; }
    }

    [BindProperty]
    public DadosVeiculo Dados { get; set; } = new();

    public Guid? Id { get; private set; }
    public bool Novo => Id is null;

    public List<(Guid Id, string Texto)> Clientes { get; private set; } = new();

    /// <summary>Nome do proprietário atual, para não desaparecer da caixa ao filtrar a lista.</summary>
    public string? NomeClienteSelecionado { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? clienteId, CancellationToken ct)
    {
        Id = id;

        if (id is null)
        {
            if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Criar)) return Forbid();

            if (clienteId.HasValue) Dados.ClienteId = clienteId.Value;

            // A próxima inspeção sai em branco: é um dado que vem do documento do veículo,
            // não algo que a aplicação possa adivinhar.
            await CarregarClientesAsync(null, ct);
            return Page();
        }

        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Veiculos.Editar)) return Forbid();

        var veiculo = await _veiculos.ObterPorIdAsync(id.Value, ct);
        if (veiculo is null) return NotFound();

        Dados = new DadosVeiculo
        {
            Matricula = veiculo.Matricula,
            Vin = veiculo.Vin,
            Marca = veiculo.Marca,
            Modelo = veiculo.Modelo,
            Versao = veiculo.Versao,
            Ano = veiculo.Ano,
            DataPrimeiraMatricula = veiculo.DataPrimeiraMatricula,
            Combustivel = veiculo.Combustivel,
            Caixa = veiculo.Caixa,
            Cilindrada = veiculo.Cilindrada,
            Potencia = veiculo.Potencia,
            Cor = veiculo.Cor,
            Quilometragem = veiculo.Quilometragem,
            ProximaInspecao = veiculo.ProximaInspecao,
            Observacoes = veiculo.Observacoes,
            Ativo = veiculo.Ativo,
            ClienteId = veiculo.ClienteId
        };

        await CarregarClientesAsync(null, ct);
        return Page();
    }

    /// <summary>
    /// Devolve apenas as opções da caixa de clientes, filtradas pelo texto escrito. É o que
    /// permite ter uma lista pesquisável sem carregar a base de clientes inteira na página.
    /// </summary>
    public async Task<IActionResult> OnGetClientesAsync(string? termo, CancellationToken ct)
    {
        // A caixa envia o seu próprio valor no pedido; assim o cliente já escolhido continua
        // selecionado depois de filtrar a lista.
        Dados.ClienteId = Guid.TryParse(Request.Query["Dados.ClienteId"], out var atual) ? atual : Guid.Empty;

        await CarregarClientesAsync(termo, ct);
        return Partial("_OpcoesClientes", this);
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        Id = id;

        var permissao = id is null ? Permissoes.Veiculos.Criar : Permissoes.Veiculos.Editar;
        if (!await _permissoes.TemPermissaoAsync(User, permissao)) return Forbid();

        if (!ModelState.IsValid)
        {
            await CarregarClientesAsync(null, ct);
            return Page();
        }

        Veiculo veiculo;

        if (id is null)
        {
            veiculo = new Veiculo();
        }
        else
        {
            var existente = await _veiculos.ObterPorIdAsync(id.Value, ct);
            if (existente is null) return NotFound();
            veiculo = existente;
        }

        veiculo.Matricula = Dados.Matricula;
        veiculo.Vin = Dados.Vin;
        veiculo.Marca = Dados.Marca;
        veiculo.Modelo = Dados.Modelo;
        veiculo.Versao = Dados.Versao;
        veiculo.Ano = Dados.Ano;
        veiculo.DataPrimeiraMatricula = Dados.DataPrimeiraMatricula;
        veiculo.Combustivel = Dados.Combustivel;
        veiculo.Caixa = Dados.Caixa;
        veiculo.Cilindrada = Dados.Cilindrada;
        veiculo.Potencia = Dados.Potencia;
        veiculo.Cor = Dados.Cor;
        veiculo.Quilometragem = Dados.Quilometragem;
        veiculo.ProximaInspecao = Dados.ProximaInspecao;
        veiculo.Observacoes = Dados.Observacoes;
        veiculo.Ativo = Dados.Ativo;
        veiculo.ClienteId = Dados.ClienteId;

        var resultado = id is null
            ? await _veiculos.CriarAsync(veiculo, ct)
            : await _veiculos.AtualizarAsync(veiculo, ct);

        if (!resultado.Sucesso)
        {
            foreach (var erro in resultado.Erros)
            {
                var chave = erro.Campo is null ? string.Empty : $"Dados.{erro.Campo}";
                ModelState.AddModelError(chave, erro.Mensagem);
            }

            await CarregarClientesAsync(null, ct);
            return Page();
        }

        this.Sucesso(id is null
            ? $"Veículo {veiculo.Matricula} criado."
            : $"Veículo {veiculo.Matricula} atualizado.");

        return Redirect($"/veiculos/{veiculo.Id}");
    }

    private async Task CarregarClientesAsync(string? termo, CancellationToken ct)
    {
        Clientes = await _clientes.ObterParaSelecaoAsync(termo, 50, ct);

        if (Dados.ClienteId != Guid.Empty && Clientes.All(c => c.Id != Dados.ClienteId))
        {
            // O proprietário atual entra sempre na lista, mesmo que não corresponda ao filtro:
            // de outro modo, gravar o formulário mudaria o proprietário sem ninguém pedir.
            var cliente = await _clientes.ObterPorIdAsync(Dados.ClienteId, ct);
            if (cliente is not null)
            {
                NomeClienteSelecionado = cliente.Nome;
                Clientes.Insert(0, (cliente.Id, cliente.Designacao));
            }
        }
    }

    /// <summary>Ano máximo aceitável, para o campo do formulário.</summary>
    public int AnoMaximo => _relogio.HojeLocal.Year + 1;
}
