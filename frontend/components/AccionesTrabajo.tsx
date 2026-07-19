"use client";

import { useState } from "react";
import {
  aceptarTrabajo,
  iniciarTrabajo,
  entregarTrabajo,
  completarTrabajo,
  cancelarTrabajo,
  disputarTrabajo,
  type TrabajoResponse,
} from "@/lib/trabajos";
import {
  ACCIONES_META,
  accionesDisponibles,
  type AccionTrabajo,
  type RolEnTrabajo,
} from "@/lib/transicionesTrabajo";
import { ApiError } from "@/lib/api";
import { ErrorAlert } from "@/components/ui";

const VARIANTE_CLASES: Record<string, string> = {
  primary: "bg-indigo-600 text-white hover:bg-indigo-700",
  secondary: "border border-slate-300 text-slate-700 hover:bg-slate-50",
  danger: "border border-rose-300 text-rose-600 hover:bg-rose-50",
  warning: "border border-amber-300 text-amber-700 hover:bg-amber-50",
};

export function AccionesTrabajo({
  trabajo,
  rolUsuario,
  onAccionCompletada,
}: {
  trabajo: TrabajoResponse;
  rolUsuario: RolEnTrabajo;
  onAccionCompletada: () => void;
}) {
  const [ejecutando, setEjecutando] = useState<AccionTrabajo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [disputaAbierta, setDisputaAbierta] = useState(false);
  const [motivo, setMotivo] = useState("");

  const acciones = accionesDisponibles(
    trabajo.estado,
    rolUsuario,
    trabajo.tipo,
  );
  if (acciones.length === 0) return null;

  async function ejecutar(accion: AccionTrabajo, motivoDisputa?: string) {
    setEjecutando(accion);
    setError(null);
    try {
      switch (accion) {
        case "aceptar":
          await aceptarTrabajo(trabajo.id);
          break;
        case "iniciar":
          await iniciarTrabajo(trabajo.id);
          break;
        case "entregar":
          await entregarTrabajo(trabajo.id);
          break;
        case "completar":
          await completarTrabajo(trabajo.id);
          break;
        case "cancelar":
          await cancelarTrabajo(trabajo.id);
          break;
        case "disputar":
          await disputarTrabajo(trabajo.id, motivoDisputa ?? "");
          break;
      }
      onAccionCompletada();
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "No pudimos ejecutar la acción.",
      );
    } finally {
      setEjecutando(null);
    }
  }

  function onClick(accion: AccionTrabajo) {
    const meta = ACCIONES_META[accion];
    if (meta.requiereMotivo) {
      setDisputaAbierta(true);
      return;
    }
    if (meta.requiereConfirmacion) {
      if (!confirm(meta.mensajeConfirmacion ?? "¿Confirmás la acción?")) return;
    }
    ejecutar(accion);
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">Acciones</h2>
      {error && (
        <div className="mt-3">
          <ErrorAlert message={error} />
        </div>
      )}
      <div className="mt-3 flex flex-wrap gap-2">
        {acciones.map((a) => {
          const meta = ACCIONES_META[a];
          return (
            <button
              key={a}
              onClick={() => onClick(a)}
              disabled={ejecutando !== null}
              className={`rounded-lg px-4 py-2 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-60 ${VARIANTE_CLASES[meta.variante]}`}
              title={meta.descripcion}
            >
              {ejecutando === a ? "Procesando…" : meta.etiqueta}
            </button>
          );
        })}
      </div>

      {disputaAbierta && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onClick={() => setDisputaAbierta(false)}
        >
          <div
            className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-bold text-slate-900">Iniciar disputa</h3>
            <p className="mt-1 text-sm text-slate-500">
              {ACCIONES_META.disputar.mensajeConfirmacion}
            </p>
            <textarea
              rows={3}
              maxLength={500}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
              placeholder="Describí el motivo…"
              className="mt-3 w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
            />
            <div className="mt-4 flex gap-3">
              <button
                onClick={() => setDisputaAbierta(false)}
                className="flex-1 rounded-lg border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancelar
              </button>
              <button
                onClick={() => {
                  setDisputaAbierta(false);
                  ejecutar("disputar", motivo.trim());
                }}
                disabled={!motivo.trim()}
                className="flex-1 rounded-lg bg-amber-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-amber-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Confirmar disputa
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
