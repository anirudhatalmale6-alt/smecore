using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Validacao;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Clientes;

/// <summary>
/// Serve dois endereços: /clientes/novo e /clientes/{id}/editar (ver AddPageRoute no Program.cs).
/// O formulário é o mesmo, muda apenas o título e o destino da gravação.
/// </summary>
[Authorize(Policy = Permissoes.Politicas.ClientesVer)]
public class EditarModel : PageModel
{
    private readonly ClientesServico _clientes;
    private readonly IPermissoesServico _permissoes;

    public EditarModel(ClientesServico clientes, IPermissoesServico permissoes)
    {
        _clientes = clientes;
        _permissoes = permissoes;
    }

    public class DadosContacto
    {
        public Guid? Id { get; set; }

        [StringLength(200, ErrorMessage = "O nome não pode ter mais de 200 caracteres.")]
        [Display(Name = "Nome")]
        public string? Nome { get; set; }

        [StringLength(120)]
        [Display(Name = "Função")]
        public string? Funcao { get; set; }

        [EmailAddress(ErrorMessage = "O e-mail do contacto não é válido.")]
        [StringLength(200)]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }

        [StringLength(30)]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [Display(Name = "Contacto principal")]
        public bool Principal { get; set; }

        [Display(Name = "Remover")]
        public bool Remover { get; set; }

        public bool EstaVazio => string.IsNullOrWhiteSpace(Nome)
            && string.IsNullOrWhiteSpace(Email)
            && string.IsNullOrWhiteSpace(Telefone)
            && string.IsNullOrWhiteSpace(Funcao);
    }

    public class DadosCliente
    {
        [Display(Name = "Tipo de cliente")]
        public TipoCliente Tipo { get; set; } = TipoCliente.Particular;

        [Required(ErrorMessage = "Indique o nome ou a designação social.")]
        [StringLength(200, ErrorMessage = "O nome não pode ter mais de 200 caracteres.")]
        [Display(Name = "Nome / Designação social")]
        public string Nome { get; set; } = string.Empty;

        [NifValido]
        [StringLength(20)]
        [Display(Name = "NIF")]
        public string? Nif { get; set; }

        [EmailAddress(ErrorMessage = "O e-mail indicado não é válido.")]
        [StringLength(200)]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }

        [StringLength(30)]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(30)]
        [Display(Name = "Telemóvel")]
        public string? Telemovel { get; set; }

        [StringLength(300)]
        [Display(Name = "Morada")]
        public string? Morada { get; set; }

        [CodigoPostalValido]
        [StringLength(12)]
        [Display(Name = "Código postal")]
        public string? CodigoPostal { get; set; }

        [StringLength(120)]
        [Display(Name = "Localidade")]
        public string? Localidade { get; set; }

        [StringLength(80)]
        [Display(Name = "País")]
        public string Pais { get; set; } = "Portugal";

        [StringLength(4000, ErrorMessage = "As observações não podem ter mais de 4000 caracteres.")]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Display(Name = "Cliente ativo")]
        public bool Ativo { get; set; } = true;

        public List<DadosContacto> Contactos { get; set; } = new();
    }

    [BindProperty]
    public DadosCliente Dados { get; set; } = new();

    public Guid? Id { get; private set; }
    public int? Numero { get; private set; }
    public bool Novo => Id is null;

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        Id = id;

        if (id is null)
        {
            if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Criar)) return Forbid();

            // Duas linhas em branco: chega para o caso comum e não obriga a usar JavaScript
            // para acrescentar contactos.
            Dados.Contactos.Add(new DadosContacto());
            Dados.Contactos.Add(new DadosContacto());
            return Page();
        }

        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Clientes.Editar)) return Forbid();

        var cliente = await _clientes.ObterPorIdAsync(id.Value, ct);
        if (cliente is null) return NotFound();

        Numero = cliente.Numero;
        Dados = new DadosCliente
        {
            Tipo = cliente.Tipo,
            Nome = cliente.Nome,
            Nif = cliente.Nif,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Telemovel = cliente.Telemovel,
            Morada = cliente.Morada,
            CodigoPostal = cliente.CodigoPostal,
            Localidade = cliente.Localidade,
            Pais = cliente.Pais,
            Observacoes = cliente.Observacoes,
            Ativo = cliente.Ativo,
            Contactos = cliente.Contactos.Select(c => new DadosContacto
            {
                Id = c.Id,
                Nome = c.Nome,
                Funcao = c.Funcao,
                Email = c.Email,
                Telefone = c.Telefone,
                Principal = c.Principal
            }).ToList()
        };

        Dados.Contactos.Add(new DadosContacto());

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, CancellationToken ct)
    {
        Id = id;

        var permissao = id is null ? Permissoes.Clientes.Criar : Permissoes.Clientes.Editar;
        if (!await _permissoes.TemPermissaoAsync(User, permissao)) return Forbid();

        if (!ModelState.IsValid) return Page();

        Cliente cliente;

        if (id is null)
        {
            cliente = new Cliente();
        }
        else
        {
            var existente = await _clientes.ObterPorIdAsync(id.Value, ct);
            if (existente is null) return NotFound();
            cliente = existente;
            Numero = existente.Numero;
        }

        cliente.Tipo = Dados.Tipo;
        cliente.Nome = Dados.Nome;
        cliente.Nif = Dados.Nif;
        cliente.Email = Dados.Email;
        cliente.Telefone = Dados.Telefone;
        cliente.Telemovel = Dados.Telemovel;
        cliente.Morada = Dados.Morada;
        cliente.CodigoPostal = Dados.CodigoPostal;
        cliente.Localidade = Dados.Localidade;
        cliente.Pais = Dados.Pais;
        cliente.Observacoes = Dados.Observacoes;
        cliente.Ativo = Dados.Ativo;

        AplicarContactos(cliente);

        var resultado = id is null
            ? await _clientes.CriarAsync(cliente, ct)
            : await _clientes.AtualizarAsync(cliente, ct);

        if (!resultado.Sucesso)
        {
            foreach (var erro in resultado.Erros)
            {
                // Os erros de negócio são colocados no campo respetivo, para aparecerem junto
                // ao campo errado e não numa caixa genérica no topo.
                var chave = erro.Campo is null ? string.Empty : $"Dados.{erro.Campo}";
                ModelState.AddModelError(chave, erro.Mensagem);
            }
            return Page();
        }

        this.Sucesso(id is null
            ? $"Cliente \"{cliente.Nome}\" criado."
            : $"Cliente \"{cliente.Nome}\" atualizado.");

        return Redirect($"/clientes/{cliente.Id}");
    }

    /// <summary>
    /// Reconcilia a lista de contactos do formulário com a que está na base de dados:
    /// atualiza os existentes, acrescenta os novos e remove os marcados.
    /// </summary>
    private void AplicarContactos(Cliente cliente)
    {
        var enviados = Dados.Contactos.Where(c => !c.EstaVazio || c.Id is not null).ToList();

        foreach (var contacto in cliente.Contactos.ToList())
        {
            var enviado = enviados.FirstOrDefault(c => c.Id == contacto.Id);

            if (enviado is null || enviado.Remover || string.IsNullOrWhiteSpace(enviado.Nome))
            {
                cliente.Contactos.Remove(contacto);
                continue;
            }

            contacto.Nome = enviado.Nome!;
            contacto.Funcao = enviado.Funcao;
            contacto.Email = enviado.Email;
            contacto.Telefone = enviado.Telefone;
            contacto.Principal = enviado.Principal;
        }

        foreach (var novo in enviados.Where(c => c.Id is null && !c.Remover && !string.IsNullOrWhiteSpace(c.Nome)))
        {
            cliente.Contactos.Add(new ClienteContacto
            {
                Nome = novo.Nome!,
                Funcao = novo.Funcao,
                Email = novo.Email,
                Telefone = novo.Telefone,
                Principal = novo.Principal
            });
        }

        // No máximo um contacto principal: se vierem vários marcados, fica o primeiro.
        var principais = cliente.Contactos.Where(c => c.Principal).ToList();
        for (var i = 1; i < principais.Count; i++)
        {
            principais[i].Principal = false;
        }
    }
}
