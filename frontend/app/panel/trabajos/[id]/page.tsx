"use client";

import { use, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import {
  obtenerTrabajo,
  esTrabajoProyectoCerrado,
  esTrabajoClase,
  esTrabajoSalud,
  type TrabajoDetalleResponse,
} from "@/lib/trabajos";
import { formatFecha, formatPrecio } from "@/lib/servicios";
import type { RolEnTrabajo } from "@/lib/transicionesTrabajo";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { RequireRole } from "@/components/RequireRole";
import EstadoBadge from "@/components/EstadoBadge";
import { TipoBadge } from "@/components/TipoBadge";
import { AccionesTrabajo } from "@/components/AccionesTrabajo";
import { DetalleProyectoCerrado } from "@/components/DetalleProyectoCerrado";
import { DetalleClase } from "@/components/DetalleClase";
import { DetalleSalud } from "@/components/DetalleSalud";
import { SesionesLista } from "@/components/SesionesLista";
import { PagoProgreso } from "@/components/PagoProgreso";
import { HistorialTrabajo } from "@/components/HistorialTrabajo";
import { ResenasTrabajo } from "@/components/ResenasTrabajo";
import { ErrorAlert } from "@/components/ui";

export default function TrabajoDetallePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  return (
    <RequireRole roles={["Cliente", "Estudiante"]}>
      <TrabajoDetalle trabajoId={Number(id)} />
    </RequireRole>
  );
}

function TrabajoDetalle({ trabajoId }: { trabajoId: number }) {
  const { user } = useAuth();
  const [trabajo, setTrabajo] = useState<TrabajoDetalleResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setTrabajo(await obtenerTrabajo(trabajoId));
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos cargar el trabajo.",
      );
    } finally {
      setLoading(false);
    }
  }, [trabajoId]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl px-6 py-10">
        <div className="h-40 animate-pulse rounded-xl bg-slate-100" />
      </div>
    );
  }

  if (error || !trabajo) {
    return (
      <div className="mx-auto max-w-4xl px-6 py-16">
        <ErrorAlert message={error ?? "Trabajo no encontrado."} />
        <Link
          href="/panel/trabajos"
          className="mt-6 inline-block text-sm font-semibold text-indigo-700 hover:underline"
        >
          ← Volver a mis trabajos
        </Link>
      </div>
    );
  }

  const rolUsuario: RolEnTrabajo | null =
    user?.usuarioId === trabajo.clienteId
      ? "Cliente"
      : user?.usuarioId === trabajo.estudianteId
        ? "Estudiante"
        : null;

  if (!rolUsuario) {
    return (
      <div className="mx-auto max-w-4xl px-6 py-16 text-center">
        <p className="text-slate-600">No tenés acceso a este trabajo.</p>
        <Link
          href="/panel/trabajos"
          className="mt-4 inline-block text-sm font-semibold text-indigo-700 hover:underline"
        >
          ← Volver a mis trabajos
        </Link>
      </div>
    );
  }

  const tieneSesiones = trabajo.tipo === "Clase" || trabajo.tipo === "Salud";

  return (
    <div className="mx-auto max-w-4xl space-y-6 px-4 py-10 sm:px-6 lg:px-8">
      <Link
        href="/panel/trabajos"
        className="inline-block text-sm text-slate-500 transition hover:text-indigo-700"
      >
        ← Volver a mis trabajos
      </Link>

      {/* Encabezado */}
      <div className="rounded-xl border border-slate-200 bg-white p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <span className="text-xs font-medium text-slate-400">
              Trabajo #{trabajo.id}
            </span>
            <h1 className="mt-1 text-2xl font-bold tracking-tight text-slate-900">
              {trabajo.tituloSnapshot}
            </h1>
            <div className="mt-2">
              <TipoBadge tipo={trabajo.tipo} />
            </div>
          </div>
          <EstadoBadge estado={trabajo.estado} />
        </div>

        <dl className="mt-6 grid grid-cols-1 gap-4 border-t border-slate-100 pt-5 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-slate-500">Cliente</dt>
            <dd className="font-medium text-slate-900">
              {trabajo.clienteNombre}
              {rolUsuario === "Cliente" && (
                <span className="ml-1 text-xs text-indigo-600">(vos)</span>
              )}
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Estudiante</dt>
            <dd className="font-medium text-slate-900">
              {trabajo.estudianteNombre}
              {rolUsuario === "Estudiante" && (
                <span className="ml-1 text-xs text-indigo-600">(vos)</span>
              )}
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Precio</dt>
            <dd className="font-bold text-slate-900">
              {formatPrecio(trabajo.precioAcordado)}
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Creado</dt>
            <dd className="font-medium text-slate-900">
              {formatFecha(trabajo.fechaCreacion)}
            </dd>
          </div>
        </dl>
      </div>

      {/* Acciones */}
      <AccionesTrabajo
        trabajo={trabajo}
        rolUsuario={rolUsuario}
        onAccionCompletada={cargar}
      />

      {/* Detalle por vertical */}
      {esTrabajoProyectoCerrado(trabajo) && (
        <DetalleProyectoCerrado detalle={trabajo.detalle} />
      )}
      {esTrabajoClase(trabajo) && <DetalleClase detalle={trabajo.detalle} />}
      {esTrabajoSalud(trabajo) && (
        <DetalleSalud
          trabajo={trabajo}
          detalle={trabajo.detalle}
          rolUsuario={rolUsuario}
          onCambio={cargar}
        />
      )}

      {/* Sesiones (Clase y Salud) */}
      {tieneSesiones && (
        <SesionesLista
          trabajoId={trabajo.id}
          rolUsuario={rolUsuario}
          onCambio={cargar}
        />
      )}

      {/* Pago */}
      <PagoProgreso trabajoId={trabajo.id} rolUsuario={rolUsuario} />

      {/* Historial */}
      <HistorialTrabajo
        trabajoId={trabajo.id}
        estudianteId={trabajo.estudianteId}
        estudianteNombre={trabajo.estudianteNombre}
        clienteId={trabajo.clienteId}
        clienteNombre={trabajo.clienteNombre}
      />

      {/* Reseñas (solo si Completado) */}
      {trabajo.estado === "Completado" && <ResenasTrabajo trabajo={trabajo} />}
    </div>
  );
}
