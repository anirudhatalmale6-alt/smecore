using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;
using SmeCore.Domain.Validacao;
using SmeCore.Infrastructure.Consultas;
using SmeCore.Infrastructure.Dados;

namespace SmeCore.Infrastructure.Servicos;

/// <summary>Filtros da listagem de clientes.</summary>
public class ClientesFiltro : ParametrosListagem
{
    public TipoCliente? Tipo { get; set; }

    /// <summary>null = todos; true = apenas ativos; false = apenas inativos.</summary>
    public bool? Ativo { get; set; }

    public string? Localidade { get; set; }

    /// <summary>Mostrar também clientes eliminados (apenas para quem pode eliminar).</summary>
    public bool IncluirEliminados { get; set; }

    /// <summary>Apenas clientes com pelo menos um veículo registado.</summary>
    public bool? ComVeiculos { get; set; }

    public bool TemFiltrosAtivos =>
        !string.IsNullOrWhiteSpace(Pesquisa)
        || Tipo.HasValue
        || Ativo.HasValue
        || ComVeiculos.HasValue
        || IncluirEliminados
        || !string.IsNullOrWhiteSpace(Localidade);
}

/// <summary>Linha da listagem. Projeção, para a tabela não arrastar entidades completas.</summary>
public class ClienteListagem
{
    public Guid Id { get; init; }
    public int Numero { get; init; }
    public TipoCliente Tipo { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Nif { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public string? Telemovel { get; init; }
    public string? Localidade { get; init; }
    public string? CodigoPostal { get; init; }
    public bool Ativo { get; init; }
    public bool Eliminado { get; init; }
    public int TotalVeiculos { get; init; }
    public DateTimeOffset CriadoEm { get; init; }
    public DateTimeOffset? AlteradoEm { get; init; }

    public string? Contacto => !string.IsNullOrWhiteSpace(Telemovel) ? Telemovel : Telefone;
}

public class ClientesServico
{
    private readonly AppDbContext _db;

    public ClientesServico(AppDbContext db) => _db = db;

    private static readonly MapaOrdenacao<ClienteListagem> Ordenacao =
        new MapaOrdenacao<ClienteListagem>("nome", q => q.ThenBy(c => c.Id))
            .Adicionar("numero", c => c.Numero)
            .Adicionar("nome", c => c.Nome)
            .Adicionar("nif", c => c.Nif)
            .Adicionar("email", c => c.Email)
            .Adicionar("localidade", c => c.Localidade)
            .Adicionar("tipo", c => c.Tipo)
            .Adicionar("veiculos", c => c.TotalVeiculos)
            .Adicionar("criado", c => c.CriadoEm)
            .Adicionar("alterado", c => c.AlteradoEm);

    public static bool OrdenacaoSuportada(string? campo) => Ordenacao.Suporta(campo);

    public IQueryable<ClienteListagem> Consultar(ClientesFiltro filtro)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();

        if (!filtro.IncluirEliminados)
        {
            query = query.Where(c => !c.Eliminado);
        }

        if (filtro.Tipo.HasValue)
        {
            query = query.Where(c => c.Tipo == filtro.Tipo.Value);
        }

        if (filtro.Ativo.HasValue)
        {
            query = query.Where(c => c.Ativo == filtro.Ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Localidade))
        {
            var padraoLocalidade = Pesquisa.ParaPadrao(filtro.Localidade);
            query = query.Where(c => c.Localidade != null && EF.Functions.ILike(c.Localidade, padraoLocalidade));
        }

        var termo = Pesquisa.Limpar(filtro.Pesquisa);
        if (termo is not null)
        {
            var padrao = Pesquisa.ParaPadrao(termo);
            var digitos = Nif.Normalizar(termo);
            var matricula = Matricula.Normalizar(termo);
            var numero = int.TryParse(termo, out var n) ? n : (int?)null;

            query = query.Where(c =>
                EF.Functions.ILike(c.Nome, padrao)
                || (c.Nif != null && digitos.Length > 0 && EF.Functions.ILike(c.Nif, "%" + digitos + "%"))
                || (c.Email != null && EF.Functions.ILike(c.Email, padrao))
                || (c.Telefone != null && EF.Functions.ILike(c.Telefone, padrao))
                || (c.Telemovel != null && EF.Functions.ILike(c.Telemovel, padrao))
                || (c.Localidade != null && EF.Functions.ILike(c.Localidade, padrao))
                || (numero != null && c.Numero == numero)
                // Pesquisar um cliente pela matrícula de um dos seus veículos: é o que a
                // receção tem à mão quando o carro entra na oficina.
                || (matricula.Length >= 4 && c.Veiculos.Any(v => !v.Eliminado
                        && EF.Functions.ILike(v.Matricula.Replace("-", ""), "%" + matricula + "%"))));
        }

        var projecao = query.Select(c => new ClienteListagem
        {
            Id = c.Id,
            Numero = c.Numero,
            Tipo = c.Tipo,
            Nome = c.Nome,
            Nif = c.Nif,
            Email = c.Email,
            Telefone = c.Telefone,
            Telemovel = c.Telemovel,
            Localidade = c.Localidade,
            CodigoPostal = c.CodigoPostal,
            Ativo = c.Ativo,
            Eliminado = c.Eliminado,
            TotalVeiculos = c.Veiculos.Count(v => !v.Eliminado),
            CriadoEm = c.CriadoEm,
            AlteradoEm = c.AlteradoEm
        });

        if (filtro.ComVeiculos.HasValue)
        {
            projecao = filtro.ComVeiculos.Value
                ? projecao.Where(c => c.TotalVeiculos > 0)
                : projecao.Where(c => c.TotalVeiculos == 0);
        }

        return Ordenacao.Aplicar(projecao, filtro.Ordenar, filtro.Descendente);
    }

    public Task<ResultadoPaginado<ClienteListagem>> ObterPaginaAsync(ClientesFiltro filtro, CancellationToken ct = default)
        => Consultar(filtro).ParaPaginaAsync(filtro, ct);

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => _db.Clientes
            .Include(c => c.Contactos.OrderByDescending(ct2 => ct2.Principal).ThenBy(ct2 => ct2.Nome))
            .Include(c => c.Veiculos.Where(v => !v.Eliminado))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>Lista curta para caixas de seleção (ex.: escolher o proprietário de um veículo).</summary>
    public async Task<List<(Guid Id, string Texto)>> ObterParaSelecaoAsync(string? termo, int limite = 20, CancellationToken ct = default)
    {
        var query = _db.Clientes.AsNoTracking().Where(c => !c.Eliminado && c.Ativo);

        var limpo = Pesquisa.Limpar(termo);
        if (limpo is not null)
        {
            var padrao = Pesquisa.ParaPadrao(limpo);
            var digitos = Nif.Normalizar(limpo);
            query = query.Where(c => EF.Functions.ILike(c.Nome, padrao)
                || (c.Nif != null && digitos.Length > 0 && EF.Functions.ILike(c.Nif, "%" + digitos + "%")));
        }

        var itens = await query
            .OrderBy(c => c.Nome)
            .Take(limite)
            .Select(c => new { c.Id, c.Nome, c.Nif })
            .ToListAsync(ct);

        return itens
            .Select(c => (c.Id, string.IsNullOrWhiteSpace(c.Nif) ? c.Nome : $"{c.Nome} — {Nif.Formatar(c.Nif)}"))
            .ToList();
    }

    public async Task<Resultado<Cliente>> CriarAsync(Cliente cliente, CancellationToken ct = default)
    {
        Normalizar(cliente);

        var erro = await ValidarAsync(cliente, ct);
        if (erro is not null) return erro;

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync(ct);
        return Resultado<Cliente>.Ok(cliente);
    }

    public async Task<Resultado<Cliente>> AtualizarAsync(Cliente cliente, CancellationToken ct = default)
    {
        Normalizar(cliente);

        var erro = await ValidarAsync(cliente, ct);
        if (erro is not null) return erro;

        await _db.SaveChangesAsync(ct);
        return Resultado<Cliente>.Ok(cliente);
    }

    /// <summary>
    /// Eliminação lógica. Um cliente com veículos ativos não é eliminado sem que os veículos
    /// sejam tratados primeiro — evita veículos órfãos numa listagem.
    /// </summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cliente is null) return Resultado.Erro("Cliente não encontrado.");
        if (cliente.Eliminado) return Resultado.Ok();

        var veiculosAtivos = await _db.Veiculos.CountAsync(v => v.ClienteId == id && !v.Eliminado, ct);
        if (veiculosAtivos > 0)
        {
            return Resultado.Erro(veiculosAtivos == 1
                ? "Este cliente tem 1 veículo registado. Transfira ou elimine o veículo antes de eliminar o cliente."
                : $"Este cliente tem {veiculosAtivos} veículos registados. Transfira ou elimine os veículos antes de eliminar o cliente.");
        }

        cliente.Eliminado = true;
        cliente.Ativo = false;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> ReativarAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cliente is null) return Resultado.Erro("Cliente não encontrado.");

        cliente.Eliminado = false;
        cliente.Ativo = true;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    /// <summary>Localidades já usadas, para alimentar o filtro sem inventar uma tabela de localidades.</summary>
    public Task<List<string>> ObterLocalidadesAsync(CancellationToken ct = default)
        => _db.Clientes.AsNoTracking()
            .Where(c => !c.Eliminado && c.Localidade != null && c.Localidade != "")
            .Select(c => c.Localidade!)
            .Distinct()
            .OrderBy(l => l)
            .Take(200)
            .ToListAsync(ct);

    private static void Normalizar(Cliente cliente)
    {
        cliente.Nome = cliente.Nome.Trim();
        cliente.Nif = string.IsNullOrWhiteSpace(cliente.Nif) ? null : Nif.Normalizar(cliente.Nif);
        cliente.Email = string.IsNullOrWhiteSpace(cliente.Email) ? null : cliente.Email.Trim().ToLowerInvariant();
        cliente.Telefone = Vazio(cliente.Telefone);
        cliente.Telemovel = Vazio(cliente.Telemovel);
        cliente.Morada = Vazio(cliente.Morada);
        cliente.Localidade = Vazio(cliente.Localidade);
        cliente.Observacoes = Vazio(cliente.Observacoes);
        cliente.CodigoPostal = string.IsNullOrWhiteSpace(cliente.CodigoPostal)
            ? null
            : CodigoPostal.Formatar(cliente.CodigoPostal);
        cliente.Pais = string.IsNullOrWhiteSpace(cliente.Pais) ? "Portugal" : cliente.Pais.Trim();

        foreach (var contacto in cliente.Contactos)
        {
            contacto.Nome = contacto.Nome.Trim();
            contacto.Funcao = Vazio(contacto.Funcao);
            contacto.Email = string.IsNullOrWhiteSpace(contacto.Email) ? null : contacto.Email.Trim().ToLowerInvariant();
            contacto.Telefone = Vazio(contacto.Telefone);
        }
    }

    private static string? Vazio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task<Resultado<Cliente>?> ValidarAsync(Cliente cliente, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nome))
        {
            return Resultado<Cliente>.Erro("O nome é obrigatório.", nameof(Cliente.Nome));
        }

        if (cliente.Nif is not null)
        {
            if (!Nif.EhValido(cliente.Nif))
            {
                return Resultado<Cliente>.Erro("O NIF indicado não é válido.", nameof(Cliente.Nif));
            }

            var duplicado = await _db.Clientes
                .AnyAsync(c => c.Nif == cliente.Nif && c.Id != cliente.Id && !c.Eliminado, ct);

            if (duplicado)
            {
                return Resultado<Cliente>.Erro("Já existe um cliente com este NIF.", nameof(Cliente.Nif));
            }
        }

        if (cliente.CodigoPostal is not null && !CodigoPostal.EhValido(cliente.CodigoPostal))
        {
            return Resultado<Cliente>.Erro("O código postal deve ter o formato 0000-000.", nameof(Cliente.CodigoPostal));
        }

        return null;
    }
}
