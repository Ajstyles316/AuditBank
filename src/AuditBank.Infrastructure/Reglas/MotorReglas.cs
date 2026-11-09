using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuditBank.Infrastructure.Reglas;

public class MotorReglas(
    AppDbContext db,
    IEnumerable<IEvaluadorRegla> evaluadores,
    ILogger<MotorReglas> log) : IMotorReglas
{
    public async Task<int> EvaluarAsync(long transaccionId)
    {
        var transaccion = await db.Transacciones
            .Include(t => t.Cuenta)
            .FirstOrDefaultAsync(t => t.Id == transaccionId);
        if (transaccion is null) return 0;

        var reglas = await db.Reglas.AsNoTracking().Where(r => r.Activa).ToListAsync();
        var creadas = 0;

        foreach (var regla in reglas)
        {
            var evaluador = evaluadores.FirstOrDefault(e => e.Tipo == regla.Tipo);
            if (evaluador is null) continue;

            try
            {
                var detalle = await evaluador.EvaluarAsync(transaccion, regla.Parametros);
                if (detalle is null) continue;

                db.Alertas.Add(new Alerta
                {
                    ReglaId = regla.Id,
                    TransaccionId = transaccion.Id,
                    Severidad = regla.Severidad,
                    Detalle = detalle
                });
                creadas++;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error evaluando la regla {Regla} para la transacción {Id}",
                    regla.Codigo, transaccion.Id);
            }
        }

        if (creadas > 0) await db.SaveChangesAsync();
        return creadas;
    }
}