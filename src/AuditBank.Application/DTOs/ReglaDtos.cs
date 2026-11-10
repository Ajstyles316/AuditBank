using System.Text.Json;
using AuditBank.Domain.Enums;

namespace AuditBank.Application.DTOs;

public record ReglaResponse(
    int Id,
    string Codigo,
    string Nombre,
    string Descripcion,
    TipoRegla Tipo,
    SeveridadAlerta Severidad,
    bool Activa,
    JsonElement Parametros);

public record ActualizarReglaRequest(bool Activa, SeveridadAlerta Severidad, JsonElement Parametros);