using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace SmeCore.Web.Servicos;

public enum TipoMensagem
{
    Sucesso,
    Erro,
    Aviso,
    Informacao
}

/// <summary>
/// Mensagens de confirmação e de erro que sobrevivem a um redirecionamento (padrão
/// POST-redirect-GET, para que um F5 não repita a operação).
/// </summary>
public static class Mensagens
{
    private const string ChaveTexto = "Mensagem:Texto";
    private const string ChaveTipo = "Mensagem:Tipo";

    public static void Definir(this ITempDataDictionary tempData, TipoMensagem tipo, string texto)
    {
        tempData[ChaveTexto] = texto;
        tempData[ChaveTipo] = tipo.ToString();
    }

    public static void Sucesso(this PageModel pagina, string texto)
        => pagina.TempData.Definir(TipoMensagem.Sucesso, texto);

    public static void Erro(this PageModel pagina, string texto)
        => pagina.TempData.Definir(TipoMensagem.Erro, texto);

    public static void Aviso(this PageModel pagina, string texto)
        => pagina.TempData.Definir(TipoMensagem.Aviso, texto);

    public static void Informacao(this PageModel pagina, string texto)
        => pagina.TempData.Definir(TipoMensagem.Informacao, texto);

    public static (TipoMensagem Tipo, string Texto)? Ler(this ITempDataDictionary tempData)
    {
        if (tempData[ChaveTexto] is not string texto || string.IsNullOrWhiteSpace(texto)) return null;

        var tipo = tempData[ChaveTipo] is string nome && Enum.TryParse<TipoMensagem>(nome, out var lido)
            ? lido
            : TipoMensagem.Informacao;

        return (tipo, texto);
    }
}
