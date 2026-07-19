"use client";

import { useEffect, useState } from "react";
import { addHours, format } from "date-fns";
import {
  listarHistorialTrabajo,
  ESTADO_META,
  type TrabajoHistorialResponse,
} from "@/lib/trabajos";
import { ApiError } from "@/lib/api";

function fechaHora(iso: string): string {
  return format(addHours(new Date(iso), -3), "dd/MM/yyyy HH:mm");
}

export function HistorialTrabajo({
  trabajoId,
  estudianteId,
  estudianteNombre,
  clienteId,
  clienteNombre,
}: {
  trabajoId: number;
  estudianteId: number;
  estudianteNombre: string;
  clienteId: number;
  clienteNombre: string;
}) {
  const [historial, setHistorial] = useState<TrabajoHistorialResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelado = false;
    listarHistorialTrabajo(trabajoId)
      .then((data) => {
        if (!cancelado)
          setHistorial(
            [...data].sort((a, b) => a.fecha.localeCompare(b.fecha)),
          );
      })
      .catch((err) => {
        if (!cancelado && !(err instanceof ApiError)) {
          // silencioso: el historial no es crítico
        }
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, [trabajoId]);

  function nombreDe(usuarioId: number | null): string {
    if (usuarioId === estudianteId) return estudianteNombre;
    if (usuarioId === clienteId) return clienteNombre;
    return "—";
  }

  if (loading) {
    return <div className="h-24 animate-pulse rounded-xl bg-slate-100" />;
  }
  if (historial.length === 0) return null;

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">Historial</h2>
      <ol className="mt-4 space-y-4 border-l-2 border-slate-100 pl-5">
        {historial.map((h) => (
          <li key={h.id} className="relative">
            <span className="absolute -left-[1.65rem] top-1 h-3 w-3 rounded-full border-2 border-white bg-indigo-500" />
            <div className="flex flex-wrap items-center gap-2">
              {h.estadoAnterior && (
                <>
                  <span className="text-xs text-slate-400">
                    {ESTADO_META[h.estadoAnterior].etiqueta}
                  </span>
                  <span className="text-slate-300">→</span>
                </>
              )}
              <span
                className={`rounded-full px-2 py-0.5 text-xs font-medium ${ESTADO_META[h.estadoNuevo].clases}`}
              >
                {ESTADO_META[h.estadoNuevo].etiqueta}
              </span>
            </div>
            <p className="mt-1 text-xs text-slate-500">
              {fechaHora(h.fecha)}
              {h.usuarioId != null && ` · por ${nombreDe(h.usuarioId)}`}
            </p>
          </li>
        ))}
      </ol>
    </div>
  );
}
