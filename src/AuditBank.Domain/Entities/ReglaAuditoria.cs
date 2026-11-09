using AuditBank.Domain.Enums;

namespace AuditBank.Domain.Entities;

public class ReglaAuditoria
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public TipoRegla Tipo { get; set; }
    public SeveridadAlerta Severidad { get; set; }
    public bool Activa { get; set; } = true;
    public string Parametros { get; set; } = "{}"; // JSON con los umbrales
}