using AuditBank.Domain.Enums;

namespace AuditBank.Domain.Entities;

public class Hallazgo
{
    public long Id { get; set; }
    public TipoHallazgo Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public long AlertaId { get; set; }
    public Alerta Alerta { get; set; } = null!;

    public int AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;
}