import { ESTADO_SESION_META, type EstadoSesion } from "@/lib/sesiones";

export default function EstadoSesionBadge({ estado }: { estado: EstadoSesion }) {
  const meta = ESTADO_SESION_META[estado];
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${meta.clases}`}
    >
      {meta.etiqueta}
    </span>
  );
}
