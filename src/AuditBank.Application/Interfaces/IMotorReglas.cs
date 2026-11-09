namespace AuditBank.Application.Interfaces;

public interface IMotorReglas
{
    /// <summary>Evalúa la transacción con todas las reglas activas. Devuelve cuántas alertas creó.</summary>
    Task<int> EvaluarAsync(long transaccionId);
}