"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  listarMisTrabajos,
  ESTADO_META,
  type EstadoTrabajo,
  type TrabajoResponse,
} from "@/lib/trabajos";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { RequireRole } from "@/components/RequireRole";
import { TrabajoCard } from "@/components/TrabajoCard";
import { ErrorAlert } from "@/components/ui";

const FILTROS: { valor: EstadoTrabajo | "Todos"; etiqueta: string }[] = [
  { valor: "Todos", etiqueta: "Todos" },
  { valor: "Pendiente", etiqueta: "Pendientes" },
  { valor: "Aceptado", etiqueta: "Aceptados" },
  { valor: "EnCurso", etiqueta: "En curso" },
  { valor: "Entregado", etiqueta: "Entregados" },
  { valor: "Completado", etiqueta: "Completados" },
  { valor: "Cancelado", etiqueta: "Cancelados" },
  { valor: "Disputa", etiqueta: "En disputa" },
];

export default function MisTrabajosPage() {
  return (
    <RequireRole roles={["Cliente", "Estudiante"]}>
      <MisTrabajos />
    </RequireRole>
  );
}

function MisTrabajos() {
  const { user } = useAuth();
  const [trabajos, setTrabajos] = useState<TrabajoResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filtro, setFiltro] = useState<EstadoTrabajo | "Todos">("Todos");

  useEffect(() => {
    let cancelado = false;
    setLoading(true);
    listarMisTrabajos()
      .then((data) => {
        if (!cancelado) setTrabajos(data);
      })
      .catch((err) => {
        if (!cancelado)
          setError(
            err instanceof ApiError
              ? err.message
              : "No pudimos cargar tus trabajos.",
          );
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, []);

  const visibles =
    filtro === "Todos"
      ? trabajos
      : trabajos.filter((t) => t.estado === filtro);

  // Solo mostramos filtros que tienen al menos un trabajo (además de "Todos").
  const conteos = trabajos.reduce<Record<string, number>>((acc, t) => {
    acc[t.estado] = (acc[t.estado] ?? 0) + 1;
    return acc;
  }, {});

  return (
    <div className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl font-bold tracking-tight text-slate-900">
        Mis trabajos
      </h1>
      <p className="mt-1 text-sm text-slate-500">
        Las contrataciones donde participás, como cliente o estudiante.
      </p>

      {error && (
        <div className="mt-6">
          <ErrorAlert message={error} />
        </div>
      )}

      {/* Filtros por estado */}
      {trabajos.length > 0 && (
        <div className="mt-6 flex flex-wrap gap-2">
          {FILTROS.filter(
            (f) => f.valor === "Todos" || conteos[f.valor],
          ).map((f) => (
            <button
              key={f.valor}
              onClick={() => setFiltro(f.valor)}
              className={`rounded-full px-4 py-1.5 text-sm font-medium transition ${
                filtro === f.valor
                  ? "bg-indigo-600 text-white"
                  : "bg-slate-100 text-slate-700 hover:bg-slate-200"
              }`}
            >
              {f.etiqueta}
              {f.valor !== "Todos" && (
                <span className="ml-1.5 text-xs opacity-70">
                  {conteos[f.valor]}
                </span>
              )}
            </button>
          ))}
        </div>
      )}

      <div className="mt-8">
        {loading ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div
                key={i}
                className="h-40 animate-pulse rounded-xl border border-slate-200 bg-slate-50"
              />
            ))}
          </div>
        ) : trabajos.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 py-16 text-center">
            <p className="font-semibold text-slate-900">
              Todavía no tenés trabajos
            </p>
            <p className="mt-1 text-sm text-slate-500">
              Cuando contrates (o te contraten) un servicio, aparecerá acá.
            </p>
            <Link
              href="/"
              className="mt-5 inline-block rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-700"
            >
              Ver servicios
            </Link>
          </div>
        ) : visibles.length === 0 ? (
          <p className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 py-10 text-center text-sm text-slate-500">
            No tenés trabajos {ESTADO_META[filtro as EstadoTrabajo].etiqueta.toLowerCase()}.
          </p>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {visibles.map((t) => (
              <TrabajoCard key={t.id} trabajo={t} miUsuarioId={user?.usuarioId} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
