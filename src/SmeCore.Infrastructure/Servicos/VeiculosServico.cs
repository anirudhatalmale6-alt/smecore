using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Common;
using SmeCore.Domain.Validacao;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Consultas;
using SmeCore.Infrastructure.Dados;

namespace SmeCore.Infrastructure.Servicos;

public class VeiculosFiltro : ParametrosListagem
{
    public Guid? ClienteId { get; set; }
    public TipoCombustivel? Combustivel { get; set; }
    public string? Marca { get; set; }
    public bool? Ativo { get; set; }
    public bool IncluirEliminados { get; set; }

    public int? AnoDe { get; set; }
    public int? AnoAte { get; set; }

    /// <summary>Só veículos com inspeção já vencida.</summary>
    public bool InspecaoVencida { get; set; }

    /// <summary>Só veículos com inspeção nos próximos N dias.</summary>
    public int? InspecaoProximosDias { get; set; }

    public bool TemFiltrosAtivos =>
        !string.IsNullOrWhiteSpace(Pesquisa)
        || ClienteId.HasValue
        || Combustivel.HasValue
        || !string.IsNullOrWhiteSpace(Marca)
        || Ativo.HasValue
        || IncluirEliminados
        || AnoDe.HasValue
        || AnoAte.HasValue
        || InspecaoVencida
        || InspecaoProximosDias.HasValue;
}

public class VeiculoListagem
{
    public Guid Id { get; init; }
    public string Matricula { get; init; } = string.Empty;
    public string Marca { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string? Versao { get; init; }
    public int? Ano { get; init; }
    public TipoCombustivel Combustivel { get; init; }
    public int? Quilometragem { get; init; }
    public DateOnly? ProximaInspecao { get; init; }
    public bool Ativo { get; init; }
    public bool Eliminado { get; init; }
    public Guid ClienteId { get; init; }
    public string ClienteNome { get; init; } = string.Empty;
    public int ClienteNumero { get; init; }
    public DateTimeOffset CriadoEm { get; init; }
    public DateTimeOffset? AlteradoEm { get; init; }

    public string Designacao => $"{Marca} {Modelo}".Trim();
}

public class VeiculosServico
{
    private readonly AppDbContext _db;
    private readonly IRelogio _relogio;

    public VeiculosServico(AppDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    private static readonly MapaOrdenacao<VeiculoListagem> Ordenacao =
        new MapaOrdenacao<VeiculoListagem>("matricula", q => q.ThenBy(v => v.Id))
            .Adicionar("matricula", v => v.Matricula)
            .Adicionar("marca", v => v.Marca, v => v.Modelo)
            .Adicionar("modelo", v => v.Modelo)
            .Adicionar("ano", v => v.Ano)
            .Adicionar("cliente", v => v.ClienteNome)
            .Adicionar("combustivel", v => v.Combustivel)
            .Adicionar("quilometragem", v => v.Quilometragem)
            .Adicionar("inspecao", v => v.ProximaInspecao)
            .Adicionar("criado", v => v.CriadoEm)
            .Adicionar("alterado", v => v.AlteradoEm);

    public static bool OrdenacaoSuportada(string? campo) => Ordenacao.Suporta(campo);

    public IQueryable<VeiculoListagem> Consultar(VeiculosFiltro filtro)
    {
        var query = _db.Veiculos.AsNoTracking().AsQueryable();

        if (!filtro.IncluirEliminados) query = query.Where(v => !v.Eliminado);
        if (filtro.ClienteId.HasValue) query = query.Where(v => v.ClienteId == filtro.ClienteId.Value);
        if (filtro.Combustivel.HasValue) query = query.Where(v => v.Combustivel == filtro.Combustivel.Value);
        if (filtro.Ativo.HasValue) query = query.Where(v => v.Ativo == filtro.Ativo.Value);
        if (filtro.AnoDe.HasValue) query = query.Where(v => v.Ano >= filtro.AnoDe.Value);
        if (filtro.AnoAte.HasValue) query = query.Where(v => v.Ano <= filtro.AnoAte.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Marca))
        {
            var padraoMarca = Pesquisa.ParaPadrao(filtro.Marca);
            query = query.Where(v => EF.Functions.ILike(v.Marca, padraoMarca));
        }

        // A data de referência vem do relógio injetado, nunca de DateTime.Today, para que o
        // comportamento seja o mesmo em testes e em produção (e no fuso certo).
        var hoje = _relogio.HojeLocal;

        if (filtro.InspecaoVencida)
        {
            query = query.Where(v => v.ProximaInspecao != null && v.ProximaInspecao < hoje);
        }

        if (filtro.InspecaoProximosDias is int dias && dias > 0)
        {
            var limite = hoje.AddDays(dias);
            query = query.Where(v => v.ProximaInspecao != null
                && v.ProximaInspecao >= hoje
                && v.ProximaInspecao <= limite);
        }

        var termo = Pesquisa.Limpar(filtro.Pesquisa);
        if (termo is not null)
        {
            var padrao = Pesquisa.ParaPadrao(termo);
            var matricula = Matricula.Normalizar(termo);

            query = query.Where(v =>
                (matricula.Length > 0 && EF.Functions.ILike(v.Matricula.Replace("-", ""), "%" + matricula + "%"))
                || EF.Functions.ILike(v.Marca, padrao)
                || EF.Functions.ILike(v.Modelo, padrao)
                || (v.Versao != null && EF.Functions.ILike(v.Versao, padrao))
                || (v.Vin != null && EF.Functions.ILike(v.Vin, padrao))
                || (v.Cliente != null && EF.Functions.ILike(v.Cliente.Nome, padrao)));
        }

        var projecao = query.Select(v => new VeiculoListagem
        {
            Id = v.Id,
            Matricula = v.Matricula,
            Marca = v.Marca,
            Modelo = v.Modelo,
            Versao = v.Versao,
            Ano = v.Ano,
            Combustivel = v.Combustivel,
            Quilometragem = v.Quilometragem,
            ProximaInspecao = v.ProximaInspecao,
            Ativo = v.Ativo,
            Eliminado = v.Eliminado,
            ClienteId = v.ClienteId,
            ClienteNome = v.Cliente!.Nome,
            ClienteNumero = v.Cliente!.Numero,
            CriadoEm = v.CriadoEm,
            AlteradoEm = v.AlteradoEm
        });

        return Ordenacao.Aplicar(projecao, filtro.Ordenar, filtro.Descendente);
    }

    public Task<ResultadoPaginado<VeiculoListagem>> ObterPaginaAsync(VeiculosFiltro filtro, CancellationToken ct = default)
        => Consultar(filtro).ParaPaginaAsync(filtro, ct);

    public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => _db.Veiculos
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    /// <summary>Marcas já registadas, para o filtro e para sugestões no formulário.</summary>
    public Task<List<string>> ObterMarcasAsync(CancellationToken ct = default)
        => _db.Veiculos.AsNoTracking()
            .Where(v => !v.Eliminado)
            .Select(v => v.Marca)
            .Distinct()
            .OrderBy(m => m)
            .Take(300)
            .ToListAsync(ct);

    public async Task<Resultado<Veiculo>> CriarAsync(Veiculo veiculo, CancellationToken ct = default)
    {
        Normalizar(veiculo);

        var erro = await ValidarAsync(veiculo, ct);
        if (erro is not null) return erro;

        _db.Veiculos.Add(veiculo);
        await _db.SaveChangesAsync(ct);
        return Resultado<Veiculo>.Ok(veiculo);
    }

    public async Task<Resultado<Veiculo>> AtualizarAsync(Veiculo veiculo, CancellationToken ct = default)
    {
        Normalizar(veiculo);

        var erro = await ValidarAsync(veiculo, ct);
        if (erro is not null) return erro;

        await _db.SaveChangesAsync(ct);
        return Resultado<Veiculo>.Ok(veiculo);
    }

    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var veiculo = await _db.Veiculos.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (veiculo is null) return Resultado.Erro("Veículo não encontrado.");
        if (veiculo.Eliminado) return Resultado.Ok();

        veiculo.Eliminado = true;
        veiculo.Ativo = false;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> ReativarAsync(Guid id, CancellationToken ct = default)
    {
        var veiculo = await _db.Veiculos.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (veiculo is null) return Resultado.Erro("Veículo não encontrado.");

        veiculo.Eliminado = false;
        veiculo.Ativo = true;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    /// <summary>Contagens usadas no painel inicial. Uma única data de referência para todas.</summary>
    public async Task<(int Total, int Vencidos, int Proximos30) > ObterResumoInspecoesAsync(CancellationToken ct = default)
    {
        var hoje = _relogio.HojeLocal;
        var limite = hoje.AddDays(30);

        var total = await _db.Veiculos.CountAsync(v => !v.Eliminado, ct);
        var vencidos = await _db.Veiculos.CountAsync(v => !v.Eliminado && v.ProximaInspecao != null && v.ProximaInspecao < hoje, ct);
        var proximos = await _db.Veiculos.CountAsync(v => !v.Eliminado && v.ProximaInspecao != null
            && v.ProximaInspecao >= hoje && v.ProximaInspecao <= limite, ct);

        return (total, vencidos, proximos);
    }

    private static void Normalizar(Veiculo veiculo)
    {
        veiculo.Matricula = Matricula.Formatar(veiculo.Matricula);
        veiculo.Vin = string.IsNullOrWhiteSpace(veiculo.Vin) ? null : veiculo.Vin.Trim().ToUpperInvariant();
        veiculo.Marca = veiculo.Marca.Trim();
        veiculo.Modelo = veiculo.Modelo.Trim();
        veiculo.Versao = Vazio(veiculo.Versao);
        veiculo.Cor = Vazio(veiculo.Cor);
        veiculo.Observacoes = Vazio(veiculo.Observacoes);
    }

    private static string? Vazio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task<Resultado<Veiculo>?> ValidarAsync(Veiculo veiculo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(veiculo.Matricula))
        {
            return Resultado<Veiculo>.Erro("A matrícula é obrigatória.", nameof(Veiculo.Matricula));
        }

        if (!Matricula.EhValida(veiculo.Matricula))
        {
            return Resultado<Veiculo>.Erro(
                "A matrícula não corresponde a nenhum formato português (AA-00-AA, 00-AA-00, 00-00-AA ou AA-00-00).",
                nameof(Veiculo.Matricula));
        }

        var duplicada = await _db.Veiculos
            .AnyAsync(v => v.Matricula == veiculo.Matricula && v.Id != veiculo.Id && !v.Eliminado, ct);

        if (duplicada)
        {
            return Resultado<Veiculo>.Erro("Já existe um veículo ativo com esta matrícula.", nameof(Veiculo.Matricula));
        }

        if (veiculo.Vin is not null)
        {
            var vinDuplicado = await _db.Veiculos
                .AnyAsync(v => v.Vin == veiculo.Vin && v.Id != veiculo.Id && !v.Eliminado, ct);

            if (vinDuplicado)
            {
                return Resultado<Veiculo>.Erro("Já existe um veículo ativo com este VIN.", nameof(Veiculo.Vin));
            }
        }

        if (string.IsNullOrWhiteSpace(veiculo.Marca))
        {
            return Resultado<Veiculo>.Erro("A marca é obrigatória.", nameof(Veiculo.Marca));
        }

        if (string.IsNullOrWhiteSpace(veiculo.Modelo))
        {
            return Resultado<Veiculo>.Erro("O modelo é obrigatório.", nameof(Veiculo.Modelo));
        }

        // O limite superior vem do relógio: um ano futuro é aceitável até ao ano seguinte
        // (modelos lançados no fim do ano), qualquer coisa acima disso é erro de digitação.
        var anoMaximo = _relogio.HojeLocal.Year + 1;
        if (veiculo.Ano.HasValue && (veiculo.Ano < 1900 || veiculo.Ano > anoMaximo))
        {
            return Resultado<Veiculo>.Erro($"O ano deve estar entre 1900 e {anoMaximo}.", nameof(Veiculo.Ano));
        }

        var clienteExiste = await _db.Clientes.AnyAsync(c => c.Id == veiculo.ClienteId && !c.Eliminado, ct);
        if (!clienteExiste)
        {
            return Resultado<Veiculo>.Erro("Escolha o cliente proprietário do veículo.", nameof(Veiculo.ClienteId));
        }

        return null;
    }
}
