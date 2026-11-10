namespace AuditBank.Application.DTOs;

public enum CodigoResultado { Ok, NoEncontrado, Prohibido, Invalido }

public record Resultado(CodigoResultado Codigo, string? Mensaje = null)
{
    public bool Ok => Codigo == CodigoResultado.Ok;

    public static Resultado Exito() => new(CodigoResultado.Ok);
    public static Resultado NoEncontrado(string mensaje) => new(CodigoResultado.NoEncontrado, mensaje);
    public static Resultado Prohibido(string mensaje) => new(CodigoResultado.Prohibido, mensaje);
    public static Resultado Invalido(string mensaje) => new(CodigoResultado.Invalido, mensaje);
}