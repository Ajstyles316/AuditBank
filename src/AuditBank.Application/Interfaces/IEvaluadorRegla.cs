using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;

namespace AuditBank.Application.Interfaces;

public interface IEvaluadorRegla
{
    TipoRegla Tipo { get; }

    /// <summary>Devuelve el detalle de la alerta si la transacción infringe la regla; null si no.</summary>
    Task<string?> EvaluarAsync(Transaccion transaccion, string parametrosJson);
}