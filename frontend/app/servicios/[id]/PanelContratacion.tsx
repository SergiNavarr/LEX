"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/contexts/AuthContext";
import { contratarProyectoCerrado } from "@/lib/trabajos";
import { ApiError } from "@/lib/api";
import { formatPrecio, type ServicioDetalleResponse } from "@/lib/servicios";

export default function PanelContratacion({
  servicio,
}: {
  servicio: ServicioDetalleResponse;
}) {
  const { user, vistaActiva } = useAuth();
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  // El id del usuario vive en `user`; la identidad (roles/vistas) en `vistaActiva`.
  const esServicioPropio = user?.usuarioId === servicio.estudianteId;
  const puedeContratar = vistaActiva === "Cliente" && !esServicioPropio;

  async function handleContratarProyectoCerrado() {
    setEnviando(true);
    setError(null);
    try {
      const trabajo = await contratarProyectoCerrado({ servicioId: servicio.id });
      router.push(`/panel/trabajos/${trabajo.id}`);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Error al contratar. Intentá de nuevo.",
      );
      setEnviando(false);
    }
  }

  return (
    <aside className="lg:sticky lg:top-24 bg-white border border-slate-200 rounded-xl p-6 shadow-sm">
      <div className="mb-4">
        <div className="text-sm text-slate-500 mb-1">Precio</div>
        <div className="text-3xl font-bold text-slate-900">
          {formatPrecio(servicio.precio)}
        </div>
      </div>

      {!user && (
        <div className="mb-4 p-3 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-800">
          <Link href="/login" className="font-medium underline">
            Iniciá sesión
          </Link>{" "}
          para contratar.
        </div>
      )}

      {esServicioPropio && (
        <div className="mb-4 p-3 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-700">
          Este es tu propio servicio.
        </div>
      )}

      {user && vistaActiva !== "Cliente" && !esServicioPropio && (
        <div className="mb-4 p-3 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-700">
          Cambiá a la vista Cliente para contratar servicios.
        </div>
      )}

      {puedeContratar && servicio.tipo === "ProyectoCerrado" && (
        <button
          onClick={handleContratarProyectoCerrado}
          disabled={enviando}
          className="w-full py-3 bg-indigo-600 text-white rounded-lg font-medium hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {enviando ? "Enviando..." : "Contratar servicio"}
        </button>
      )}

      {puedeContratar && servicio.tipo === "Clase" && (
        <Link
          href={`/servicios/${servicio.id}/contratar-clase`}
          className="block w-full rounded-lg bg-indigo-600 py-3 text-center font-medium text-white transition-colors hover:bg-indigo-700"
        >
          Elegir horarios y contratar
        </Link>
      )}

      {puedeContratar && servicio.tipo === "Salud" && (
        <Link
          href={`/servicios/${servicio.id}/contratar-salud`}
          className="block w-full rounded-lg bg-indigo-600 py-3 text-center font-medium text-white transition-colors hover:bg-indigo-700"
        >
          Contratar con paciente
        </Link>
      )}

      {error && (
        <div className="mt-3 p-3 bg-rose-50 border border-rose-200 rounded-lg text-sm text-rose-700">
          {error}
        </div>
      )}
    </aside>
  );
}
