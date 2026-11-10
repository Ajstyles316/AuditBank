using System.Linq.Expressions;
using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Domain.Entities;
using AuditBank.Domain.Enums;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Services;

public class AlertaService(AppDbContext db, IUsuarioActual actual) : IAlertaService
{
    private static readonly Expression<Func<Alerta, AlertaResponse>> Proyeccion = a =>
        new AlertaResponse(
            a.Id, a.Regla.Codigo, a.Regla.Nombre, a.Severidad, a.Estado, a.Detalle, a.CreadaEn,
            a.TransaccionId, a.Transaccion.CuentaId, a.Transaccion.Cuenta.NumeroCuenta,
            a.Transaccion.Monto, a.Transaccion.Tipo,
            a.AsignadaAId, a.AsignadaA != null ? a.AsignadaA.NombreUsuario : null, a.CerradaEn);

    // ---------------- Consultas ----------------
    public async Task<PagedResult<AlertaResponse>> ListarAsync(FiltroAlertas f)
    {
        var pagina = Math.Max(f.Pagina, 1);
        var tamano = Math.Clamp(f.TamanoPagina, 1, 100);

        var q = db.Alertas.AsNoTracking().AsQueryable();

        if (f.Estado is not null) q = q.Where(a => a.Estado == f.Estado);
        if (f.Severidad is not null) q = q.Where(a => a.Severidad == f.Severidad);
        if (f.ReglaId is not null) q = q.Where(a => a.ReglaId == f.ReglaId);
        if (f.CuentaId is not null) q = q.Where(a => a.Transaccion.CuentaId == f.CuentaId);
        if (f.Desde is not null) q = q.Where(a => a.CreadaEn >= f.Desde);
        if (f.Hasta is not null) q = q.Where(a => a.CreadaEn <= f.Hasta);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(a => a.Severidad).ThenByDescending(a => a.CreadaEn).ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(Proyeccion)
            .ToListAsync();

        return new PagedResult<AlertaResponse>(items, total, pagina, tamano);
    }

    public async Task<AlertaResponse?> ObtenerAsync(long id) =>
        await db.Alertas.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

    public async Task<List<HallazgoResponse>?> ListarHallazgosAsync(long alertaId)
    {
        if (!await db.Alertas.AnyAsync(a => a.Id == alertaId)) return null;

        return await db.Hallazgos.AsNoTracking()
            .Where(h => h.AlertaId == alertaId)
            .OrderBy(h => h.CreadoEn).ThenBy(h => h.Id)
            .Select(h => new HallazgoResponse(
                h.Id, h.AlertaId, h.Tipo, h.Descripcion, h.CreadoEn, h.AutorId, h.Autor.NombreUsuario))
            .ToListAsync();
    }

    // ---------------- Flujo ----------------
    private bool PuedeOperar(Alerta a) =>
        actual.Rol == "Administrador" || (actual.Id is not null && a.AsignadaAId == actual.Id);

    public async Task<Resultado> AsignarAsync(long alertaId, int usuarioId)
    {
        var alerta = await db.Alertas.FirstOrDefaultAsync(a => a.Id == alertaId);
        if (alerta is null) return Resultado.NoEncontrado("La alerta no existe.");
        if (alerta.Estado == EstadoAlerta.Cerrada) return Resultado.Invalido("La alerta ya está cerrada.");

        var auditor = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (auditor is null || !auditor.Activo || auditor.Rol.Nombre != "Auditor")
            return Resultado.Invalido("El usuario debe existir, estar activo y tener el rol Auditor.");

        alerta.AsignadaAId = auditor.Id;
        await db.SaveChangesAsync();
        return Resultado.Exito();
    }

    public async Task<Resultado> TomarAsync(long alertaId)
    {
        if (actual.Id is not int miId) return Resultado.Prohibido("Sesión inválida.");

        var alerta = await db.Alertas.FirstOrDefaultAsync(a => a.Id == alertaId);
        if (alerta is null) return Resultado.NoEncontrado("La alerta no existe.");
        if (alerta.Estado == EstadoAlerta.Cerrada) return Resultado.Invalido("La alerta ya está cerrada.");
        if (alerta.AsignadaAId is not null && alerta.AsignadaAId != miId)
            return Resultado.Invalido("La alerta ya está asignada a otro auditor.");

        alerta.AsignadaAId = miId;
        await db.SaveChangesAsync();
        return Resultado.Exito();
    }

    public async Task<Resultado> IniciarRevisionAsync(long alertaId)
    {
        var alerta = await db.Alertas.FirstOrDefaultAsync(a => a.Id == alertaId);
        if (alerta is null) return Resultado.NoEncontrado("La alerta no existe.");
        if (!PuedeOperar(alerta))
            return Resultado.Prohibido("Solo el auditor asignado o un administrador puede operar esta alerta.");
        if (alerta.AsignadaAId is null)
            return Resultado.Invalido("Primero hay que asignar la alerta a un auditor.");
        if (alerta.Estado != EstadoAlerta.Abierta)
            return Resultado.Invalido("Solo se puede iniciar la revisión de una alerta abierta.");

        alerta.Estado = EstadoAlerta.EnRevision;
        await db.SaveChangesAsync();
        return Resultado.Exito();
    }

    public async Task<Resultado> ComentarAsync(long alertaId, string texto)
    {
        if (actual.Id is not int autorId) return Resultado.Prohibido("Sesión inválida.");
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > 2000)
            return Resultado.Invalido("El comentario es obligatorio y no puede superar 2000 caracteres.");

        var alerta = await db.Alertas.FirstOrDefaultAsync(a => a.Id == alertaId);
        if (alerta is null) return Resultado.NoEncontrado("La alerta no existe.");
        if (!PuedeOperar(alerta))
            return Resultado.Prohibido("Solo el auditor asignado o un administrador puede operar esta alerta.");
        if (alerta.Estado == EstadoAlerta.Cerrada) return Resultado.Invalido("La alerta ya está cerrada.");

        db.Hallazgos.Add(new Hallazgo
        {
            AlertaId = alerta.Id,
            AutorId = autorId,
            Tipo = TipoHallazgo.Comentario,
            Descripcion = texto.Trim()
        });
        await db.SaveChangesAsync();
        return Resultado.Exito();
    }

    public async Task<Resultado> CerrarAsync(long alertaId, CerrarAlertaRequest r)
    {
        if (actual.Id is not int autorId) return Resultado.Prohibido("Sesión inválida.");

        var alerta = await db.Alertas.FirstOrDefaultAsync(a => a.Id == alertaId);
        if (alerta is null) return Resultado.NoEncontrado("La alerta no existe.");
        if (!PuedeOperar(alerta))
            return Resultado.Prohibido("Solo el auditor asignado o un administrador puede operar esta alerta.");
        if (alerta.Estado != EstadoAlerta.EnRevision)
            return Resultado.Invalido("La alerta debe estar En revisión para poder cerrarla.");

        if (r.Conclusion is not (TipoHallazgo.FalsoPositivo or TipoHallazgo.IrregularidadConfirmada))
            return Resultado.Invalido("La conclusión debe ser FalsoPositivo o IrregularidadConfirmada.");
        if (string.IsNullOrWhiteSpace(r.Descripcion) || r.Descripcion.Trim().Length < 10 || r.Descripcion.Length > 2000)
            return Resultado.Invalido("La descripción debe tener entre 10 y 2000 caracteres.");

        db.Hallazgos.Add(new Hallazgo
        {
            AlertaId = alerta.Id,
            AutorId = autorId,
            Tipo = r.Conclusion,
            Descripcion = r.Descripcion.Trim()
        });
        alerta.Estado = EstadoAlerta.Cerrada;
        alerta.CerradaEn = DateTime.UtcNow;

        await db.SaveChangesAsync(); // hallazgo + cierre, todo o nada
        return Resultado.Exito();
    }
}