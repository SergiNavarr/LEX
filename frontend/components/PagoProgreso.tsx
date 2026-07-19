"use client";

import { useEffect, useState } from "react";
import { addHours, format } from "date-fns";
import {
  listarMisPagos,
  obtenerPagoDetalle,
  type MovimientoPagoResponse,
  type PagoDetalleResponse,
  type TipoMovimientoPago,
} from "@/lib/pagos";
import type { RolEnTrabajo } from "@/lib/transicionesTrabajo";
import { ApiError } from "@/lib/api";
import { formatPrecio } from "@/lib/servicios";

const EMOJI_ESTADO: Record<string, string> = {
  Retenido: "bg-blue-100 text-blue-800",
  ParcialmenteLiberado: "bg-amber-100 text-amber-800",
  Liberado: "bg-emerald-100 text-emerald-800",
  Reembolsado: "bg-rose-100 text-rose-800",
  EnDisputa: "bg-purple-100 text-purple-800",
};

const MOV_META: Record<
  TipoMovimientoPago,
  { signo: string; clase: string; etiqueta: string }
> = {
  Retencion: { signo: "+", clase: "text-blue-700", etiqueta: "Retención inicial" },
  LiberacionEstudiante: {
    signo: "→",
    clase: "text-emerald-700",
    etiqueta: "Liberación al estudiante",
  },
  ComisionLex: { signo: "→", clase: "text-slate-500", etiqueta: "Comisión LEX" },
  Reembolso: { signo: "↩", clase: "text-rose-700", etiqueta: "Reembolso al cliente" },
  Ajuste: { signo: "±", clase: "text-amber-700", etiqueta: "Ajuste" },
};

function fechaMov(iso: string): string {
  return format(addHours(new Date(iso), -3), "dd/MM/yyyy HH:mm");
}

export function PagoProgreso({
  trabajoId,
  rolUsuario,
}: {
  trabajoId: number;
  rolUsuario: RolEnTrabajo;
}) {
  const [pago, setPago] = useState<PagoDetalleResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [sinPago, setSinPago] = useState(false);

  useEffect(() => {
    let cancelado = false;
    setLoading(true);
    listarMisPagos()
      .then(async (resumenes) => {
        const r = resumenes.find((p) => p.trabajoId === trabajoId);
        if (!r) {
          if (!cancelado) setSinPago(true);
          return;
        }
        const detalle = await obtenerPagoDetalle(r.id);
        if (!cancelado) setPago(detalle);
      })
      .catch(() => {
        if (!cancelado) setSinPago(true);
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, [trabajoId]);

  if (loading) {
    return <div className="h-32 animate-pulse rounded-xl bg-slate-100" />;
  }
  if (sinPago || !pago) return null;

  const liberadoEstudiante = suma(pago.movimientos, "LiberacionEstudiante");
  const progreso =
    pago.montoAEstudiante > 0
      ? Math.round((liberadoEstudiante / pago.montoAEstudiante) * 100)
      : 0;

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">Pago</h2>

      <div className="mt-3 flex items-center justify-between">
        <div>
          <div className="text-xs text-slate-500">Monto total</div>
          <div className="text-2xl font-bold text-slate-900">
            {formatPrecio(pago.montoTotal)}
          </div>
        </div>
        <span
          className={`rounded-full px-3 py-1 text-sm font-medium ${EMOJI_ESTADO[pago.estado] ?? "bg-slate-100 text-slate-700"}`}
        >
          {pago.estado === "ParcialmenteLiberado"
            ? "Parcialmente liberado"
            : pago.estado === "EnDisputa"
              ? "En disputa"
              : pago.estado}
        </span>
      </div>

      {/* Barra de progreso de liberación al estudiante */}
      <div className="mt-4">
        <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100">
          <div
            className="h-full rounded-full bg-emerald-500 transition-all"
            style={{ width: `${progreso}%` }}
          />
        </div>
        <div className="mt-2 flex justify-between text-xs text-slate-500">
          <span>
            Liberado al estudiante: {formatPrecio(liberadoEstudiante)} /{" "}
            {formatPrecio(pago.montoAEstudiante)}
          </span>
          <span>Comisión LEX: {formatPrecio(pago.montoComisionCalculada)}</span>
        </div>
      </div>

      {/* Movimientos */}
      {pago.movimientos.length > 0 && (
        <div className="mt-5">
          <h3 className="text-xs font-semibold uppercase text-slate-400">
            Movimientos
          </h3>
          <ul className="mt-2 divide-y divide-slate-100">
            {pago.movimientos.map((m) => {
              const meta = MOV_META[m.tipo];
              const propio =
                rolUsuario === "Estudiante" &&
                m.tipo === "LiberacionEstudiante";
              return (
                <li
                  key={m.id}
                  className={`flex items-center justify-between gap-3 py-2 text-sm ${propio ? "rounded-md bg-emerald-50/60 px-2" : ""}`}
                >
                  <div className="min-w-0">
                    <span className={`font-medium ${meta.clase}`}>
                      {meta.etiqueta}
                    </span>
                    {propio && (
                      <span className="ml-2 text-xs font-semibold text-emerald-600">
                        tu cobro
                      </span>
                    )}
                    <div className="text-xs text-slate-400">
                      {fechaMov(m.fechaMovimiento)}
                    </div>
                  </div>
                  <span className={`shrink-0 font-semibold ${meta.clase}`}>
                    {meta.signo} {formatPrecio(m.monto)}
                  </span>
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}

function suma(
  movimientos: MovimientoPagoResponse[],
  tipo: TipoMovimientoPago,
): number {
  return movimientos
    .filter((m) => m.tipo === tipo)
    .reduce((acc, m) => acc + m.monto, 0);
}
