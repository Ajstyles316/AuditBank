using AuditBank.Domain.Enums;

namespace AuditBank.Application.DTOs;

public record AsignarAlertaRequest(int UsuarioId);

public record ComentarioRequest(string Texto);

public record CerrarAlertaRequest(TipoHallazgo Conclusion, string Descripcion);

public record HallazgoResponse(
    long Id,
    long AlertaId,
    TipoHallazgo Tipo,
    string Descripcion,
    DateTime CreadoEn,
    int AutorId,
    string Autor);