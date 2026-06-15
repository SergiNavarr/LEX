namespace Lex.Api.Common;

// Excepciones de dominio que el middleware traduce a codigos HTTP claros.
// Asi los services expresan el "por que" y los controllers quedan finos.

/// <summary>Recurso inexistente -> 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Accion no permitida para el usuario autenticado -> 403.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>Entrada invalida / regla de negocio violada -> 400.</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
