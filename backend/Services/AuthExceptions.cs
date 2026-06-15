using Lex.Api.DTOs;

namespace Lex.Api.Services;

// Excepciones de dominio de autenticacion. El AuthController las mapea a
// codigos HTTP claros (400 / 401) sin filtrar detalles internos.

public class EmailYaRegistradoException : Exception
{
    public EmailYaRegistradoException(string email)
        : base($"Ya existe un usuario registrado con el email '{email}'.") { }
}

public class TipoRegistroInvalidoException : Exception
{
    public TipoRegistroInvalidoException(TipoRegistro tipo)
        : base($"El tipo de registro '{tipo}' no es valido. Use 'ClienteParticular', 'ClienteEmpresa' o 'Agencia'.") { }
}

public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException()
        : base("Email o contraseña incorrectos.") { }
}
