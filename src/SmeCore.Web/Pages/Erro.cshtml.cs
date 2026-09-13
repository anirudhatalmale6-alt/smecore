using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmeCore.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErroModel : PageModel
{
    public string Titulo { get; private set; } = "Ocorreu um erro";
    public string Descricao { get; private set; } = "Não foi possível concluir a operação.";

    /// <summary>
    /// Identificador do pedido. Aparece também nos registos do servidor, o que permite ligar
    /// o que o utilizador viu à linha de log correspondente.
    /// </summary>
    public string? Identificador { get; private set; }

    public void OnGet(int? codigo)
    {
        Identificador = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        switch (codigo)
        {
            case 404:
                Titulo = "Página não encontrada";
                Descricao = "O endereço que tentou abrir não existe ou o registo foi eliminado.";
                break;
            case 403:
                Titulo = "Sem permissão";
                Descricao = "A sua conta não tem permissão para abrir esta área.";
                break;
            case 400:
                Titulo = "Pedido inválido";
                Descricao = "Os dados enviados não foram aceites. Verifique o formulário e tente novamente.";
                break;
            case 500:
            case null:
                Titulo = "Ocorreu um erro";
                Descricao = "Houve uma falha inesperada. A ocorrência foi registada; tente novamente dentro de alguns instantes.";
                break;
            default:
                Titulo = $"Erro {codigo}";
                Descricao = "Não foi possível concluir a operação.";
                break;
        }
    }
}
