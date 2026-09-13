using Microsoft.AspNetCore.Html;

namespace SmeCore.Web.Servicos;

/// <summary>
/// Ícones em SVG embutido. Ficam no código, e não num tipo de letra de ícones externo, para a
/// aplicação não depender de nenhum CDN — no App Platform isso é uma chamada externa a menos.
/// </summary>
public static class Icones
{
    private static readonly Dictionary<string, string> Formas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["painel"] = "<rect x='3' y='3' width='7' height='9' rx='1'/><rect x='14' y='3' width='7' height='5' rx='1'/><rect x='14' y='12' width='7' height='9' rx='1'/><rect x='3' y='16' width='7' height='5' rx='1'/>",
        ["utilizadores"] = "<path d='M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2'/><circle cx='9' cy='7' r='4'/><path d='M23 21v-2a4 4 0 0 0-3-3.87'/><path d='M16 3.13a4 4 0 0 1 0 7.75'/>",
        ["utilizador"] = "<path d='M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2'/><circle cx='12' cy='7' r='4'/>",
        ["veiculo"] = "<path d='M5 17h14'/><path d='M3 17v-4.5L5.2 7A2 2 0 0 1 7.1 5.6h9.8A2 2 0 0 1 18.8 7L21 12.5V17'/><path d='M3 12.5h18'/><circle cx='7.5' cy='17.5' r='1.6'/><circle cx='16.5' cy='17.5' r='1.6'/>",
        ["escudo"] = "<path d='M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'/>",
        ["historico"] = "<circle cx='12' cy='12' r='9'/><path d='M12 7v5l4 2'/>",
        ["pesquisa"] = "<circle cx='11' cy='11' r='7'/><path d='M20 20l-3.5-3.5'/>",
        ["mais"] = "<path d='M12 5v14'/><path d='M5 12h14'/>",
        ["lapis"] = "<path d='M17 3a2.83 2.83 0 0 1 4 4L7.5 20.5 2 22l1.5-5.5z'/>",
        ["lixo"] = "<path d='M3 6h18'/><path d='M8 6V4h8v2'/><path d='M19 6l-1 14H6L5 6'/><path d='M10 11v5'/><path d='M14 11v5'/>",
        ["descarregar"] = "<path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><path d='M7 10l5 5 5-5'/><path d='M12 15V3'/>",
        ["folha"] = "<path d='M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z'/><path d='M14 2v6h6'/><path d='M8 13h8'/><path d='M8 17h8'/>",
        ["regressar"] = "<path d='M19 12H5'/><path d='M12 19l-7-7 7-7'/>",
        ["menu"] = "<path d='M3 6h18'/><path d='M3 12h18'/><path d='M3 18h18'/>",
        ["sair"] = "<path d='M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4'/><path d='M16 17l5-5-5-5'/><path d='M21 12H9'/>",
        ["aviso"] = "<path d='M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z'/><path d='M12 9v4'/><path d='M12 17h.01'/>",
        ["ok"] = "<circle cx='12' cy='12' r='9'/><path d='M8.5 12.5l2.5 2.5 4.5-5'/>",
        ["info"] = "<circle cx='12' cy='12' r='9'/><path d='M12 11v5'/><path d='M12 8h.01'/>",
        ["erro"] = "<circle cx='12' cy='12' r='9'/><path d='M15 9l-6 6'/><path d='M9 9l6 6'/>",
        ["telefone"] = "<path d='M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2 4.2 2 2 0 0 1 4 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.1a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2z'/>",
        ["email"] = "<rect x='2' y='4' width='20' height='16' rx='2'/><path d='m2 7 10 6 10-6'/>",
        ["morada"] = "<path d='M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 0 1 16 0z'/><circle cx='12' cy='10' r='3'/>",
        ["calendario"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M8 3v4'/><path d='M16 3v4'/><path d='M3 11h18'/>",
        ["filtro"] = "<path d='M22 3H2l8 9.5V19l4 2v-8.5z'/>",
        ["chave"] = "<circle cx='7.5' cy='15.5' r='4.5'/><path d='m10.7 12.3 8.8-8.8'/><path d='m17 5 3 3'/><path d='m14 8 3 3'/>",
        ["reativar"] = "<path d='M3 12a9 9 0 1 0 3-6.7'/><path d='M3 4v5h5'/>",
        ["caixa-vazia"] = "<path d='M22 12h-6l-2 3h-4l-2-3H2'/><path d='M5.5 5.1 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.5-6.9A2 2 0 0 0 16.7 4H7.3a2 2 0 0 0-1.8 1.1z'/>",
        ["guardar"] = "<path d='M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z'/><path d='M17 21v-8H7v8'/><path d='M7 3v5h8'/>",
        ["olho"] = "<path d='M1 12s4-7 11-7 11 7 11 7-4 7-11 7S1 12 1 12z'/><circle cx='12' cy='12' r='3'/>",
        ["etiqueta"] = "<path d='M20.6 13.6 13.6 20.6a2 2 0 0 1-2.8 0l-8-8V3h9.6l8.2 8.2a2 2 0 0 1 0 2.4z'/><path d='M7 7h.01'/>",
        ["empresa"] = "<rect x='3' y='7' width='18' height='14' rx='2'/><path d='M8 7V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v3'/><path d='M3 13h18'/>",
        ["fechar"] = "<path d='M18 6 6 18'/><path d='M6 6l12 12'/>"
    };

    /// <summary>Devolve o SVG do ícone pedido. Um nome desconhecido não desenha nada.</summary>
    public static IHtmlContent Desenhar(string nome, int tamanho = 18, string? classe = null)
    {
        if (!Formas.TryGetValue(nome, out var forma)) return HtmlString.Empty;

        var atributoClasse = string.IsNullOrWhiteSpace(classe) ? string.Empty : $" class=\"{classe}\"";

        return new HtmlString(
            $"<svg{atributoClasse} width=\"{tamanho}\" height=\"{tamanho}\" viewBox=\"0 0 24 24\" fill=\"none\" " +
            "stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\" " +
            $"aria-hidden=\"true\" focusable=\"false\">{forma}</svg>");
    }
}
