using AuditBank.Domain.Enums;

namespace AuditBank.Application.DTOs;

public record CrearCuentaRequest(
    string NumeroCuenta,
    string Titular,
    TipoCuenta Tipo,
    Moneda Moneda,
    decimal SaldoInicial);

public record CuentaResponse(
    int Id,
    string NumeroCuenta,
    string Titular,
    TipoCuenta Tipo,
    Moneda Moneda,
    decimal Saldo,
    bool Activa,
    DateTime CreadaEn);