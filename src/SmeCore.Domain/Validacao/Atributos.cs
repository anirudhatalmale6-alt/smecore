using System.ComponentModel.DataAnnotations;

namespace SmeCore.Domain.Validacao;

/// <summary>Valida um NIF/NIPC português. Campos vazios são aceites (use [Required] se for obrigatório).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NifValidoAttribute : ValidationAttribute
{
    public NifValidoAttribute()
        : base("O NIF indicado não é válido.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var texto = value.ToString();
        if (string.IsNullOrWhiteSpace(texto)) return true;
        return Nif.EhValido(texto);
    }
}

/// <summary>Valida uma matrícula portuguesa (LL-NN-NN, NN-NN-LL, NN-LL-NN ou LL-NN-LL).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MatriculaValidaAttribute : ValidationAttribute
{
    public MatriculaValidaAttribute()
        : base("A matrícula não corresponde a nenhum formato português (ex.: AA-00-AA, 00-AA-00, 00-00-AA ou AA-00-00).")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var texto = value.ToString();
        if (string.IsNullOrWhiteSpace(texto)) return true;
        return Matricula.EhValida(texto);
    }
}

/// <summary>Valida um código postal português (0000-000).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CodigoPostalValidoAttribute : ValidationAttribute
{
    public CodigoPostalValidoAttribute()
        : base("O código postal deve ter o formato 0000-000.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var texto = value.ToString();
        if (string.IsNullOrWhiteSpace(texto)) return true;
        return CodigoPostal.EhValido(texto);
    }
}

/// <summary>Valida um VIN: 17 caracteres, sem as letras I, O e Q.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class VinValidoAttribute : ValidationAttribute
{
    public VinValidoAttribute()
        : base("O VIN deve ter 17 caracteres e não pode conter as letras I, O ou Q.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var texto = value.ToString();
        if (string.IsNullOrWhiteSpace(texto)) return true;

        var limpo = texto.Trim().ToUpperInvariant();
        if (limpo.Length != 17) return false;

        foreach (var c in limpo)
        {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
            if (c is 'I' or 'O' or 'Q') return false;
        }
        return true;
    }
}
