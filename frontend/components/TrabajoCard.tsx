import Link from "next/link";
import { formatFecha, formatPrecio } from "@/lib/servicios";
import { type TrabajoResponse } from "@/lib/trabajos";
import { TipoBadge } from "@/components/TipoBadge";
import EstadoBadge from "@/components/EstadoBadge";

// `miUsuarioId` permite marcar "vos" en la parte que corresponde al usuario logueado.
export function TrabajoCard({
  trabajo,
  miUsuarioId,
}: {
  trabajo: TrabajoResponse;
  miUsuarioId: number | undefined;
}) {
  const soyCliente = trabajo.clienteId === miUsuarioId;
  const otraParte = soyCliente
    ? `${trabajo.estudianteNombre} (estudiante)`
    : `${trabajo.clienteNombre} (cliente)`;

  return (
    <Link
      href={`/panel/trabajos/${trabajo.id}`}
      className="flex h-full flex-col rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:-translate-y-0.5 hover:border-indigo-300 hover:shadow-md"
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-medium text-slate-400">
          Trabajo #{trabajo.id}
        </span>
        <EstadoBadge estado={trabajo.estado} size="sm" />
      </div>

      <h3 className="mt-2 line-clamp-2 font-semibold text-slate-900">
        {trabajo.tituloSnapshot}
      </h3>

      <div className="mt-2 flex items-center gap-2">
        <TipoBadge tipo={trabajo.tipo} />
      </div>

      <p className="mt-2 text-sm text-slate-500">
        Con <span className="font-medium text-slate-700">{otraParte}</span>
      </p>

      <div className="mt-4 flex items-end justify-between border-t border-slate-100 pt-3">
        <span className="text-xs text-slate-400">
          {formatFecha(trabajo.fechaCreacion)}
        </span>
        <span className="text-lg font-bold text-slate-900">
          {formatPrecio(trabajo.precioAcordado)}
        </span>
      </div>
    </Link>
  );
}
