import type { DetalleTrabajoClase } from "@/lib/trabajos";

export function DetalleClase({
  detalle,
}: {
  detalle: DetalleTrabajoClase;
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">Detalles de la clase</h2>
      <dl className="mt-3 grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Dato label="Materia" valor={detalle.materiaSnapshot} />
        <Dato label="Nivel" valor={detalle.nivelSnapshot} />
        <Dato label="Modalidad" valor={detalle.modalidadSnapshot} />
        <Dato
          label="Sesiones"
          valor={`${detalle.sesionesCompletadas} / ${detalle.cantidadSesionesTotales}`}
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
