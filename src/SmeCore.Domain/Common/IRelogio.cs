namespace SmeCore.Domain.Common;

/// <summary>
/// Fonte única do tempo. Tudo o que precise da data atual recebe este serviço em vez de
/// chamar DateTime.Now — é o que permite testar regras de datas sem alterar o relógio da máquina.
/// </summary>
public interface IRelogio
{
    /// <summary>Instante atual em UTC. É o valor que vai para a base de dados.</summary>
    DateTimeOffset AgoraUtc { get; }

    /// <summary>Data de hoje no fuso horário de Portugal continental (Europe/Lisbon).</summary>
    DateOnly HojeLocal { get; }

    /// <summary>Converte um instante UTC para a hora local portuguesa, para apresentação.</summary>
    DateTimeOffset ParaLocal(DateTimeOffset instante);

    /// <summary>
    /// Instante UTC correspondente à meia-noite local do dia indicado. É o que permite que um
    /// filtro "de 13/09 a 13/09" apanhe exatamente o dia 13 em Portugal, e não das 23:00 do
    /// dia 12 às 23:00 do dia 13 como aconteceria comparando em UTC.
    /// </summary>
    DateTimeOffset InicioDoDiaUtc(DateOnly dia);
}

public sealed class RelogioSistema : IRelogio
{
    private readonly TimeZoneInfo _fuso;

    public RelogioSistema()
    {
        _fuso = ObterFusoPortugal();
    }

    public DateTimeOffset AgoraUtc => DateTimeOffset.UtcNow;

    public DateOnly HojeLocal => DateOnly.FromDateTime(ParaLocal(AgoraUtc).DateTime);

    public DateTimeOffset ParaLocal(DateTimeOffset instante)
        => TimeZoneInfo.ConvertTime(instante, _fuso);

    public DateTimeOffset InicioDoDiaUtc(DateOnly dia)
    {
        var meiaNoiteLocal = dia.ToDateTime(TimeOnly.MinValue);
        var deslocamento = _fuso.GetUtcOffset(meiaNoiteLocal);
        return new DateTimeOffset(meiaNoiteLocal, deslocamento).ToUniversalTime();
    }

    /// <summary>
    /// O identificador do fuso difere entre Linux (IANA) e Windows, e o servidor pode estar
    /// em qualquer dos dois. Tenta ambos antes de desistir para UTC.
    /// </summary>
    internal static TimeZoneInfo ObterFusoPortugal()
    {
        foreach (var id in new[] { "Europe/Lisbon", "GMT Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }
        return TimeZoneInfo.Utc;
    }
}
