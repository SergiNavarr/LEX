"use client";

import { use, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  obtenerServicio,
  esClase,
  formatPrecio,
  type ServicioDetalleResponse,
  type DetalleClase,
} from "@/lib/servicios";
import { contratarClase } from "@/lib/trabajos";
import { ApiError } from "@/lib/api";
import { RequireRole } from "@/components/RequireRole";
import { SelectorSlots } from "@/components/SelectorSlots";
import { ErrorAlert } from "@/components/ui";

export default function ContratarClasePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  return (
    <RequireRole roles={["Cliente"]} vista="Cliente">
      <ContratarClase servicioId={Number(id)} />
    </RequireRole>
  );
}

function ContratarClase({ servicioId }: { servicioId: number }) {
  const router = useRouter();
  const [servicio, setServicio] = useState<
    (ServicioDetalleResponse & { detalle: DetalleClase }) | null
  >(null);
  const [cargando, setCargando] = useState(true);
  const [cargaError, setCargaError] = useState<string | null>(null);

  const [slotsElegidos, setSlotsElegidos] = useState<string[]>([]);
  const [notas, setNotas] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    let cancelado = false;
    obtenerServicio(servicioId)
      .then((s) => {
        if (cancelado) return;
        if (!esClase(s)) {
          setCargaError("Este servicio no es de tipo Clase.");
          return;
        }
        setServicio(s);
      })
      .catch((err) => {
        if (!cancelado)
          setCargaError(
            err instanceof ApiError
              ? err.message
              : "No pudimos cargar el servicio.",
          );
      })
      .finally(() => {
        if (!cancelado) setCargando(false);
      });
    return () => {
      cancelado = true;
    };
  }, [servicioId]);

  if (cargando) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-10">
        <div className="h-40 animate-pulse rounded-xl bg-slate-100" />
      </div>
    );
  }

  if (cargaError || !servicio) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-16">
        <ErrorAlert message={cargaError ?? "Servicio no encontrado."} />
        <Link
          href="/"
          className="mt-6 inline-block text-sm font-semibold text-indigo-700 hover:underline"
        >
          ← Volver al catálogo
        </Link>
      </div>
    );
  }

  const cantidad = servicio.detalle.esPaquete
    ? (servicio.detalle.cantidadSesionesPaquete ?? 1)
    : 1;
  const completo = slotsElegidos.length === cantidad;

  async function handleConfirmar() {
    if (!completo || !servicio) return;
    setEnviando(true);
    setError(null);
    try {
      const trabajo = await contratarClase({
        servicioId: servicio.id,
        slotsElegidos,
        notasCliente: notas.trim() || undefined,
      });
      router.push(`/panel/trabajos/${trabajo.id}`);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "No pudimos confirmar la contratación.",
      );
      setEnviando(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-10">
      <Link
        href={`/servicios/${servicio.id}`}
        className="mb-4 inline-block text-sm text-slate-500 transition hover:text-indigo-700"
      >
        ← Volver al servicio
      </Link>

      <h1 className="text-2xl font-bold tracking-tight text-slate-900">
        Contratar servicio de tutoría
      </h1>

      <div className="mt-4 rounded-xl border border-slate-200 bg-white p-5">
        <h2 className="font-semibold text-slate-900">{servicio.titulo}</h2>
        <p className="mt-1 text-sm text-slate-500">
          por {servicio.estudianteNombre} · {servicio.detalle.materia} ·{" "}
          {servicio.detalle.duracionMinutosSesion} min por sesión
        </p>
        <p className="mt-2 text-lg font-bold text-slate-900">
          {formatPrecio(servicio.precio)}
          {servicio.detalle.esPaquete && (
            <span className="ml-2 text-sm font-normal text-slate-500">
              paquete de {cantidad} sesiones
            </span>
          )}
        </p>
      </div>

      <p className="mt-6 text-sm text-slate-600">
        {servicio.detalle.esPaquete
          ? `Elegí los ${cantidad} horarios para tus sesiones.`
          : "Elegí el horario para tu sesión."}
      </p>

      <div className="mt-3">
        <SelectorSlots
          estudianteId={servicio.estudianteId}
          duracionMinutos={servicio.detalle.duracionMinutosSesion}
          cantidadRequerida={cantidad}
          slotsElegidos={slotsElegidos}
          onCambiarSeleccion={setSlotsElegidos}
        />
      </div>

      <div className="mt-6">
        <label
          htmlFor="notas"
          className="mb-1.5 block text-sm font-medium text-slate-700"
        >
          Notas para el estudiante (opcional)
        </label>
        <textarea
          id="notas"
          rows={3}
          maxLength={500}
          value={notas}
          onChange={(e) => setNotas(e.target.value)}
          placeholder="Contale qué querés reforzar…"
          className="w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
        />
      </div>

      {error && (
        <div className="mt-4">
          <ErrorAlert message={error} />
        </div>
      )}

      <button
        onClick={handleConfirmar}
        disabled={!completo || enviando}
        className="mt-6 w-full rounded-lg bg-indigo-600 py-3 font-medium text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {enviando
          ? "Confirmando…"
          : completo
            ? "Confirmar contratación"
            : `Elegí ${cantidad - slotsElegidos.length} horario${cantidad - slotsElegidos.length !== 1 ? "s" : ""} más`}
      </button>
    </div>
  );
}
