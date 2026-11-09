using AuditBank.Application.DTOs;
using AuditBank.Application.Interfaces;
using AuditBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuditBank.Infrastructure.Services;

public class AuditoriaService(AppDbContext db) : IAuditoriaService
{
    public async Task<PagedResult<RegistroAuditoriaResponse>> ListarAsync(FiltroAuditoria f)
    {
        var pagina = Math.Max(f.Pagina, 1);
        var tamano = Math.Clamp(f.TamanoPagina, 1, 100);

        var q = db.RegistrosAuditoria.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Entidad)) q = q.Where(r => r.Entidad == f.Entidad);
        if (!string.IsNullOrWhiteSpace(f.EntidadId)) q = q.Where(r => r.EntidadId == f.EntidadId);
        if (f.UsuarioId is not null) q = q.Where(r => r.UsuarioId == f.UsuarioId);
        if (f.Accion is not null) q = q.Where(r => r.Accion == f.Accion);
        if (f.Desde is not null) q = q.Where(r => r.FechaUtc >= f.Desde);
        if (f.Hasta is not null) q = q.Where(r => r.FechaUtc <= f.Hasta);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(r => r.FechaUtc).ThenByDescending(r => r.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(r => new RegistroAuditoriaResponse(
                r.Id, r.Accion, r.Entidad, r.EntidadId, r.ValorAnterior, r.ValorNuevo,
                r.FechaUtc, r.DireccionIp, r.UsuarioId,
                r.Usuario != null ? r.Usuario.NombreUsuario : null))
            .ToListAsync();

        return new PagedResult<RegistroAuditoriaResponse>(items, total, pagina, tamano);
    }
}