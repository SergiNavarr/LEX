"use client";

import { useCallback, useEffect, useState } from "react";
import { addHours, format } from "date-fns";
import { es } from "date-fns/locale";
import {
  listarSesionesDeTrabajo,
  marcarSesionRealizada,
  marcarSesionNoAsistio,
  type SesionResponse,
} from "@/lib/sesiones";
import type { RolEnTrabajo } from "@/lib/transicionesTrabajo";
import { ApiError } from "@/lib/api";
import EstadoSesionBadge from "@/components/EstadoSesionBadge";
import { ErrorAlert } from "@/components/ui";

// Instante UTC -> Date con componentes de reloj de pared AR (solo para display, nunca
// como instante). Idéntico enfoque tz-seguro que SelectorSlots.
function arDisplay(utcIso: string): Date {
  const s = addHours(new Date(utcIso), -3);
  return new Date(
    s.getUTCFullYear(),
    s.getUTCMonth(),
    s.getUTCDate(),
    s.getUTCHours(),
    s.getUTCMinutes(),
  );
}

type Marca = "realizar" | "no-asistio";

export function SesionesLista({
  trabajoId,
  rolUsuario,
  onCambio,
}: {
  trabajoId: number;
  rolUsuario: RolEnTrabajo;
  onCambio: () => void;
}) {
  const [sesiones, setSesiones] = useState<SesionResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<{ sesion: SesionResponse; marca: Marca } | null>(
    null,
  );

  const cargar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSesiones(await listarSesionesDeTrabajo(trabajoId));
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos cargar las sesiones.",
      );
    } finally {
      setLoading(false);
    }
  }, [trabajoId]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  if (loading) {
    return <div className="h-24 animate-pulse rounded-xl bg-slate-100" />;
  }
  if (error) return <ErrorAlert message={error} />;
  if (sesiones.length === 0) return null;

  const realizadas = sesiones.filter(
    (s) => s.estado === "Realizada" || s.estado === "NoAsistio",
  ).length;

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">
        Sesiones{" "}
        <span className="font-normal text-slate-400">
          ({realizadas}/{sesiones.length} completadas)
        </span>
      </h2>

      <ul className="mt-3 space-y-3">
        {sesiones.map((s) => {
          const d = arDisplay(s.fechaHoraInicio);
          const puedeMarcar =
            rolUsuario === "Estudiante" && s.estado === "Pendiente";
          return (
            <li
              key={s.id}
              className="rounded-lg border border-slate-200 p-3"
            >
              <div className="flex items-center justify-between gap-2">
                <div>
                  <span className="text-sm font-semibold text-slate-900">
                    Sesión {s.numeroSesion}
                  </span>
                  <span className="ml-2 text-sm capitalize text-slate-500">
                    {format(d, "EEE dd/MM HH:mm", { locale: es })} ·{" "}
                    {s.duracionMinutos} min
                  </span>
                </div>
                <EstadoSesionBadge estado={s.estado} />
              </div>

              {s.observaciones && (
                <p className="mt-1.5 text-sm text-slate-600">
                  {s.observaciones}
                </p>
              )}

              {puedeMarcar && (
                <div className="mt-2 flex gap-2">
                  <button
                    onClick={() => setModal({ sesion: s, marca: "realizar" })}
                    className="rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-indigo-700"
                  >
                    Marcar realizada
                  </button>
                  <button
                    onClick={() => setModal({ sesion: s, marca: "no-asistio" })}
                    className="rounded-md border border-rose-300 px-3 py-1.5 text-xs font-semibold text-rose-600 transition hover:bg-rose-50"
                  >
                    No asistió
                  </button>
                </div>
              )}
            </li>
          );
        })}
      </ul>

      {modal && (
        <MarcarModal
          sesion={modal.sesion}
          marca={modal.marca}
          onCerrar={() => setModal(null)}
          onExito={async () => {
            setModal(null);
            await cargar();
            onCambio();
          }}
        />
      )}
    </div>
  );
}

function MarcarModal({
  sesion,
  marca,
  onCerrar,
  onExito,
}: {
  sesion: SesionResponse;
  marca: Marca;
  onCerrar: () => void;
  onExito: () => void;
}) {
  const [observaciones, setObservaciones] = useState("");
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const esRealizar = marca === "realizar";
  const d = arDisplay(sesion.fechaHoraInicio);

  async function confirmar() {
    setEnviando(true);
    setError(null);
    try {
      const obs = observaciones.trim() || undefined;
      if (esRealizar) await marcarSesionRealizada(sesion.id, obs);
      else await marcarSesionNoAsistio(sesion.id, obs);
      onExito();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos marcar la sesión.",
      );
      setEnviando(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onCerrar}
    >
      <div
        className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="text-lg font-bold text-slate-900">
          {esRealizar ? "Marcar sesión realizada" : "Registrar inasistencia"}
        </h3>
        <p className="mt-1 text-sm capitalize text-slate-500">
          Sesión {sesion.numeroSesion} ·{" "}
          {format(d, "EEE dd/MM HH:mm", { locale: es })}
        </p>
        {!esRealizar && (
          <p className="mt-2 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
            El pago se libera igual: reservaste el horario y pusiste tu tiempo.
          </p>
        )}
        {error && (
          <div className="mt-3">
            <ErrorAlert message={error} />
          </div>
        )}
        <label className="mt-3 block text-sm font-medium text-slate-700">
          Observaciones (opcional)
        </label>
        <textarea
          rows={3}
          maxLength={1000}
          value={observaciones}
          onChange={(e) => setObservaciones(e.target.value)}
          className="mt-1 w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
        />
        <div className="mt-4 flex gap-3">
          <button
            onClick={onCerrar}
            className="flex-1 rounded-lg border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            Cancelar
          </button>
          <button
            onClick={confirmar}
            disabled={enviando}
            className="flex-1 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {enviando ? "Guardando…" : "Confirmar"}
          </button>
        </div>
      </div>
    </div>
  );
}
