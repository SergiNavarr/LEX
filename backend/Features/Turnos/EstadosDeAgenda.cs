using Lex.Api.Domain.Enums;

namespace Lex.Api.Features.Turnos;

// Que estados de turno ocupan la agenda del estudiante. Es una sola regla y vive en un
// solo lugar: la consulta de huecos libres y la validacion de una reserva tienen que
// coincidir, o el sistema ofrece un horario que despues rechaza.
public static class EstadosDeAgenda
{
    // Reservado y Confirmado son compromisos vigentes. Realizado tambien ocupa: un turno
    // se puede marcar como dado antes de su hora (el estudiante cierra la sesion apenas
    // termina, o el seed arma historia), asi que hay Realizados con fecha futura y el
    // horario sigue tomado. Cancelado y Ausente devuelven el hueco a la agenda.
    public static readonly EstadoTurno[] Ocupan =
        { EstadoTurno.Reservado, EstadoTurno.Confirmado, EstadoTurno.Realizado };
}
