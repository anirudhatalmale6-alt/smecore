namespace SmeCore.Infrastructure.Identidade;

/// <summary>
/// Catálogo de permissões. Um módulo futuro (orçamentos, obras, stock) acrescenta aqui a sua
/// entrada e passa a aparecer no ecrã de perfis sem tocar em mais nenhum ficheiro de autenticação.
/// </summary>
public static class Permissoes
{
    /// <summary>Tipo de claim onde as permissões são guardadas.</summary>
    public const string TipoClaim = "permissao";

    /// <summary>Prefixo das políticas de autorização geradas dinamicamente.</summary>
    public const string PrefixoPolitica = "perm:";

    public static class Clientes
    {
        public const string Ver = "clientes.ver";
        public const string Criar = "clientes.criar";
        public const string Editar = "clientes.editar";
        public const string Eliminar = "clientes.eliminar";
        public const string Exportar = "clientes.exportar";
    }

    public static class Veiculos
    {
        public const string Ver = "veiculos.ver";
        public const string Criar = "veiculos.criar";
        public const string Editar = "veiculos.editar";
        public const string Eliminar = "veiculos.eliminar";
        public const string Exportar = "veiculos.exportar";
    }

    public static class Utilizadores
    {
        public const string Ver = "utilizadores.ver";
        public const string Gerir = "utilizadores.gerir";
        public const string GerirPerfis = "utilizadores.perfis";
    }

    public static class Historico
    {
        public const string Ver = "historico.ver";
    }

    /// <summary>Perfil com acesso total. Nunca perde permissões, mesmo que as claims sejam apagadas.</summary>
    public const string PerfilAdministrador = "Administrador";
    public const string PerfilGestor = "Gestor";
    public const string PerfilUtilizador = "Utilizador";

    public record Entrada(string Chave, string Titulo, string Descricao);

    public record Modulo(string Nome, string Icone, IReadOnlyList<Entrada> Permissoes);

    /// <summary>Estrutura usada para desenhar o ecrã de permissões, por módulo.</summary>
    public static readonly IReadOnlyList<Modulo> Modulos = new List<Modulo>
    {
        new("Clientes", "utilizadores", new List<Entrada>
        {
            new(Clientes.Ver, "Consultar", "Ver a lista e a ficha de clientes."),
            new(Clientes.Criar, "Criar", "Registar novos clientes."),
            new(Clientes.Editar, "Editar", "Alterar dados de clientes existentes."),
            new(Clientes.Eliminar, "Eliminar", "Desativar clientes (o histórico é mantido)."),
            new(Clientes.Exportar, "Exportar", "Descarregar a lista em CSV ou Excel.")
        }),
        new("Veículos", "veiculo", new List<Entrada>
        {
            new(Veiculos.Ver, "Consultar", "Ver a lista e a ficha de veículos."),
            new(Veiculos.Criar, "Criar", "Registar novos veículos."),
            new(Veiculos.Editar, "Editar", "Alterar dados de veículos existentes."),
            new(Veiculos.Eliminar, "Eliminar", "Desativar veículos (o histórico é mantido)."),
            new(Veiculos.Exportar, "Exportar", "Descarregar a lista em CSV ou Excel.")
        }),
        new("Utilizadores e acessos", "escudo", new List<Entrada>
        {
            new(Utilizadores.Ver, "Consultar utilizadores", "Ver a lista de utilizadores."),
            new(Utilizadores.Gerir, "Gerir utilizadores", "Criar, editar, ativar e desativar utilizadores."),
            new(Utilizadores.GerirPerfis, "Gerir perfis", "Criar perfis e atribuir permissões.")
        }),
        new("Histórico", "historico", new List<Entrada>
        {
            new(Historico.Ver, "Consultar histórico", "Ver o registo de todas as alterações.")
        })
    };

    /// <summary>Todas as chaves de permissão existentes.</summary>
    public static IReadOnlyList<string> Todas { get; } = Modulos
        .SelectMany(m => m.Permissoes.Select(p => p.Chave))
        .ToList();

    /// <summary>Permissões atribuídas por omissão ao perfil Gestor.</summary>
    public static IReadOnlyList<string> PredefinidasGestor { get; } = new List<string>
    {
        Clientes.Ver, Clientes.Criar, Clientes.Editar, Clientes.Eliminar, Clientes.Exportar,
        Veiculos.Ver, Veiculos.Criar, Veiculos.Editar, Veiculos.Eliminar, Veiculos.Exportar,
        Utilizadores.Ver,
        Historico.Ver
    };

    /// <summary>Permissões atribuídas por omissão ao perfil Utilizador.</summary>
    public static IReadOnlyList<string> PredefinidasUtilizador { get; } = new List<string>
    {
        Clientes.Ver, Clientes.Criar, Clientes.Editar,
        Veiculos.Ver, Veiculos.Criar, Veiculos.Editar
    };

    public static string Politica(string permissao) => PrefixoPolitica + permissao;

    /// <summary>
    /// Nomes de política em constantes, para poderem ser usados em [Authorize(Policy = ...)] —
    /// um atributo só aceita constantes de compilação.
    /// </summary>
    public static class Politicas
    {
        public const string ClientesVer = PrefixoPolitica + Clientes.Ver;
        public const string ClientesCriar = PrefixoPolitica + Clientes.Criar;
        public const string ClientesEditar = PrefixoPolitica + Clientes.Editar;
        public const string ClientesEliminar = PrefixoPolitica + Clientes.Eliminar;
        public const string ClientesExportar = PrefixoPolitica + Clientes.Exportar;

        public const string VeiculosVer = PrefixoPolitica + Veiculos.Ver;
        public const string VeiculosCriar = PrefixoPolitica + Veiculos.Criar;
        public const string VeiculosEditar = PrefixoPolitica + Veiculos.Editar;
        public const string VeiculosEliminar = PrefixoPolitica + Veiculos.Eliminar;
        public const string VeiculosExportar = PrefixoPolitica + Veiculos.Exportar;

        public const string UtilizadoresVer = PrefixoPolitica + Utilizadores.Ver;
        public const string UtilizadoresGerir = PrefixoPolitica + Utilizadores.Gerir;
        public const string UtilizadoresGerirPerfis = PrefixoPolitica + Utilizadores.GerirPerfis;

        public const string HistoricoVer = PrefixoPolitica + Historico.Ver;
    }

    /// <summary>Título legível de uma chave, para mensagens de erro e histórico.</summary>
    public static string Titulo(string chave)
    {
        foreach (var modulo in Modulos)
        {
            foreach (var entrada in modulo.Permissoes)
            {
                if (entrada.Chave == chave) return $"{modulo.Nome} — {entrada.Titulo}";
            }
        }
        return chave;
    }
}
