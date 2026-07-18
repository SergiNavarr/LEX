import type { DetalleSalud, ServicioDetalleResponse } from "@/lib/servicios";
import type { PacienteResponse } from "@/lib/pacientes";

// Muestra el texto del consentimiento tal como lo generará el backend al firmar
// (espeja ConsentimientoTemplate.cs). Es informativo: la evidencia legal la arma
// el backend con su propia fecha al momento de firmar.
export function ConsentimientoTexto({
  servicio,
  paciente,
}: {
  servicio: ServicioDetalleResponse & { detalle: DetalleSalud };
  paciente: PacienteResponse;
}) {
  const d = servicio.detalle;
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-xs leading-relaxed text-slate-600">
      <p className="font-semibold text-slate-800">
        CONSENTIMIENTO INFORMADO PARA PRÁCTICA DE SALUD SUPERVISADA
      </p>
      <p className="mt-3">
        Por el presente, el/la abajo firmante manifiesta:
      </p>
      <ol className="mt-2 list-decimal space-y-2 pl-5">
        <li>
          Que ha sido debidamente informado/a acerca del servicio de{" "}
          <span className="font-medium">{d.catalogoServicioNombre}</span> a
          realizarse sobre el paciente{" "}
          <span className="font-medium">{paciente.nombreCompleto}</span> (
          {paciente.tipo}).
        </li>
        <li>
          Que el/la estudiante{" "}
          <span className="font-medium">{servicio.estudianteNombre}</span>{" "}
          realizará la práctica bajo supervisión del profesional{" "}
          <span className="font-medium">{d.supervisorNombre}</span>, Matrícula
          N° {d.supervisorMatricula}.
        </li>
        <li>
          Que comprende que se trata de una práctica estudiantil supervisada,
          con los alcances y limitaciones propios del año de cursada del
          estudiante.
        </li>
        <li>Que ha tenido oportunidad de hacer preguntas y aclarar dudas.</li>
        <li>
          Que acepta las condiciones y autoriza la realización de la práctica
          descrita.
        </li>
      </ol>
      <p className="mt-3 text-slate-400">
        Este consentimiento queda registrado en la plataforma LEX como evidencia
        de aceptación, con la fecha y hora de la firma.
      </p>
    </div>
  );
}
