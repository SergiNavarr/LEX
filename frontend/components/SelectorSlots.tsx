"use client";

import { useEffect, useMemo, useState } from "react";
import { addDays, format, startOfWeek } from "date-fns";
import { es } from "date-fns/locale";
import {
  listarSlotsDisponibles,
  type SlotDisponibleResponse,
} from "@/lib/turnos";
import { ApiError } from "@/lib/api";

// --- Zona horaria (AR, UTC-3 fijo, igual que el backend) ---
//
// Los slots vienen del backend como instantes UTC. Para MOSTRARLOS en hora argentina
// desplazamos -3h y reconstruimos una Date "local" con esas componentes de reloj de pared:
// esa Date solo se usa para formatear (nunca como instante), así el string es correcto sea
// cual sea el timezone del navegador. Para MANDAR de vuelta reusamos el ISO UTC original del
// backend, sin reconvertir: es exacto por construcción.
function arDisplayDate(utcIso: string): Date {
  const s = new Date(new Date(utcIso).getTime() - 3 * 3600_000);
  return new Date(
    s.getUTCFullYear(),
    s.getUTCMonth(),
    s.getUTCDate(),
    s.getUTCHours(),
    s.getUTCMinutes(),
  );
}

// Fecha AR de hoy (a medianoche local), para navegar semanas sin depender del timezone.
function hoyAr(): Date {
  const s = new Date(Date.now() - 3 * 3600_000);
  return new Date(s.getUTCFullYear(), s.getUTCMonth(), s.getUTCDate());
}

const SEMANAS_MAX = 4;

export function SelectorSlots({
  estudianteId,
  duracionMinutos,
  cantidadRequerida,
  slotsElegidos,
  onCambiarSeleccion,
}: {
  estudianteId: number;
  duracionMinutos: number;
  cantidadRequerida: number;
  slotsElegidos: string[];
  onCambiarSeleccion: (slots: string[]) => void;
}) {
  const inicioSemanaHoy = useMemo(
    () => startOfWeek(hoyAr(), { weekStartsOn: 1 }),
    [],
  );
  const [semanaInicio, setSemanaInicio] = useState<Date>(inicioSemanaHoy);
  const [slots, setSlots] = useState<SlotDisponibleResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const dias = useMemo(
    () => Array.from({ length: 7 }, (_, i) => addDays(semanaInicio, i)),
    [semanaInicio],
  );

  useEffect(() => {
    let cancelado = false;
    setLoading(true);
    setError(null);
    const desde = format(semanaInicio, "yyyy-MM-dd");
    const hasta = format(addDays(semanaInicio, 6), "yyyy-MM-dd");
    listarSlotsDisponibles({ estudianteId, desde, hasta, duracionMinutos })
      .then((data) => {
        if (!cancelado) setSlots(data);
      })
      .catch((err) => {
        if (!cancelado)
          setError(
            err instanceof ApiError
              ? err.message
              : "No pudimos cargar la disponibilidad.",
          );
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, [estudianteId, duracionMinutos, semanaInicio]);

  // Slots por día (columna). Se filtran los que ya pasaron por las dudas.
  const ahora = Date.now();
  const slotsPorDia = useMemo(() => {
    const mapa = new Map<string, { iso: string; hora: string }[]>();
    for (const s of slots) {
      if (new Date(s.fechaHoraInicio).getTime() < ahora) continue;
      const display = arDisplayDate(s.fechaHoraInicio);
      const key = format(display, "yyyy-MM-dd");
      const lista = mapa.get(key) ?? [];
      lista.push({ iso: s.fechaHoraInicio, hora: format(display, "HH:mm") });
      mapa.set(key, lista);
    }
    return mapa;
  }, [slots, ahora]);

  const elegidos = new Set(slotsElegidos);

  function toggleSlot(iso: string) {
    if (elegidos.has(iso)) {
      onCambiarSeleccion(slotsElegidos.filter((s) => s !== iso));
      return;
    }
    if (cantidadRequerida === 1) {
      onCambiarSeleccion([iso]); // reemplaza
      return;
    }
    if (slotsElegidos.length < cantidadRequerida) {
      onCambiarSeleccion([...slotsElegidos, iso]);
    }
  }

  const puedeAtras = semanaInicio.getTime() > inicioSemanaHoy.getTime();
  const puedeAdelante =
    semanaInicio.getTime() <
    addDays(inicioSemanaHoy, SEMANAS_MAX * 7).getTime();

  const finSemana = addDays(semanaInicio, 6);
  const tituloSemana = `Semana del ${format(semanaInicio, "d", { locale: es })} al ${format(finSemana, "d 'de' MMMM", { locale: es })}`;

  // Slots elegidos ordenados para el resumen del sidebar.
  const resumen = [...slotsElegidos]
    .sort()
    .map((iso) => ({
      iso,
      etiqueta: format(arDisplayDate(iso), "EEE dd/MM HH:mm", { locale: es }),
    }));

  return (
    <div className="rounded-xl border border-slate-200 bg-white">
      {/* Navegación de semana */}
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
        <button
          type="button"
          onClick={() => setSemanaInicio(addDays(semanaInicio, -7))}
          disabled={!puedeAtras}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          ← Semana anterior
        </button>
        <span className="text-sm font-semibold capitalize text-slate-900">
          {tituloSemana}
        </span>
        <button
          type="button"
          onClick={() => setSemanaInicio(addDays(semanaInicio, 7))}
          disabled={!puedeAdelante}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Siguiente semana →
        </button>
      </div>

      {/* Grilla de días */}
      {loading ? (
        <div className="p-8 text-center text-sm text-slate-400">
          Cargando disponibilidad…
        </div>
      ) : error ? (
        <div className="p-6 text-center text-sm text-rose-600">{error}</div>
      ) : slotsPorDia.size === 0 ? (
        <div className="p-8 text-center text-sm text-slate-500">
          No hay disponibilidad en esta semana. Probá con otra.
        </div>
      ) : (
        <div className="grid grid-cols-7 gap-1 overflow-x-auto p-3">
          {dias.map((dia) => {
            const key = format(dia, "yyyy-MM-dd");
            const delDia = slotsPorDia.get(key) ?? [];
            return (
              <div key={key} className="min-w-[64px]">
                <div className="mb-2 text-center">
                  <div className="text-[11px] font-medium uppercase text-slate-400">
                    {format(dia, "EEE", { locale: es })}
                  </div>
                  <div className="text-sm font-semibold text-slate-700">
                    {format(dia, "d")}
                  </div>
                </div>
                <div className="space-y-1">
                  {delDia.length === 0 ? (
                    <div className="text-center text-[11px] text-slate-300">
                      —
                    </div>
                  ) : (
                    delDia.map((s) => {
                      const activo = elegidos.has(s.iso);
                      return (
                        <button
                          key={s.iso}
                          type="button"
                          onClick={() => toggleSlot(s.iso)}
                          className={`w-full rounded-md px-1 py-1.5 text-xs font-medium transition ${
                            activo
                              ? "bg-indigo-600 text-white"
                              : "border border-slate-200 text-slate-700 hover:border-indigo-400 hover:bg-indigo-50"
                          }`}
                        >
                          {s.hora}
                        </button>
                      );
                    })
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Resumen de selección */}
      <div className="border-t border-slate-200 px-4 py-3">
        <div className="text-sm font-semibold text-slate-700">
          Horarios elegidos: {slotsElegidos.length} / {cantidadRequerida}
        </div>
        {resumen.length > 0 && (
          <ul className="mt-2 space-y-1">
            {resumen.map((r) => (
              <li
                key={r.iso}
                className="flex items-center justify-between gap-2 text-sm text-slate-600"
              >
                <span className="capitalize">{r.etiqueta}</span>
                <button
                  type="button"
                  onClick={() =>
                    onCambiarSeleccion(slotsElegidos.filter((s) => s !== r.iso))
                  }
                  className="text-xs font-medium text-rose-600 hover:underline"
                >
                  Quitar
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
