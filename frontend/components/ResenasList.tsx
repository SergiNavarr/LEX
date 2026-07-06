"use client";

// Lista de reseñas recibidas (estrellas + comentario + autor + fecha).
// Reutilizable: la usan el detalle del servicio y el portafolio del estudiante.

import { formatFecha, type Resena } from "@/lib/servicios";
import { StarsRow } from "@/components/Stars";

export function ResenasList({
  resenas,
  emptyText = "Todavía no tiene reseñas.",
}: {
  resenas: Resena[];
  emptyText?: string;
}) {
  if (resenas.length === 0) {
    return (
      <p className="rounded-lg border border-dashed border-gray-200 bg-gray-50/50 px-4 py-8 text-center text-sm text-gray-500">
        {emptyText}
      </p>
    );
  }

  return (
    <ul className="space-y-4">
      {resenas.map((r) => (
        <li
          key={r.idResena}
          className="rounded-xl border border-gray-200 bg-white p-4"
        >
          <div className="flex items-center justify-between gap-2">
            <span className="font-semibold text-foreground">{r.autorNombre}</span>
            <StarsRow value={r.puntaje} />
          </div>
          {r.comentario && (
            <p className="mt-2 text-sm text-gray-600">{r.comentario}</p>
          )}
          <p className="mt-2 text-xs text-gray-400">{formatFecha(r.fecha)}</p>
        </li>
      ))}
    </ul>
  );
}
