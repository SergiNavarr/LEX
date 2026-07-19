import { formatFecha } from "@/lib/servicios";
import type { DetalleTrabajoProyectoCerrado } from "@/lib/trabajos";

const FORMATO: Record<string, string> = {
  Archivos: "Archivos",
  Link: "Enlace",
  Ambos: "Archivos y enlace",
};

export function DetalleProyectoCerrado({
  detalle,
}: {
  detalle: DetalleTrabajoProyectoCerrado;
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">
        Detalles del proyecto
      </h2>
      <dl className="mt-3 grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Dato label="Fecha límite de entrega" valor={formatFecha(detalle.plazoEntregaFecha)} />
        <Dato
          label="Revisiones"
          valor={`${detalle.revisionesUsadas} / ${detalle.revisionesMaximas} usadas`}
        />
        <Dato
          label="Formato de entrega"
          valor={FORMATO[detalle.formatoEntregaSnapshot] ?? detalle.formatoEntregaSnapshot}
        />
      </dl>
    </div>
  );
}

function Dato({ label, valor }: { label: string; valor: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{valor}</dd>
    </div>
  );
}
