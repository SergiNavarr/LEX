"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  eliminarServicio,
  formatPrecio,
  listarServicios,
  type Servicio,
} from "@/lib/servicios";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { RequireRole } from "@/components/RequireRole";
import { ServicioFormModal } from "@/components/ServicioFormModal";
import { TipoBadge } from "@/components/TipoBadge";
import { LinkedInButton } from "@/components/LinkedInButton";
import { ErrorAlert } from "@/components/ui";

export default function MisServiciosPage() {
  return (
    <RequireRole roles={["Estudiante"]} vista="Estudiante">
      <MisServicios />
    </RequireRole>
  );
}

function MisServicios() {
  const { user } = useAuth();
  const [servicios, setServicios] = useState<Servicio[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Estado del modal: null = cerrado, "nuevo" = alta, Servicio = edición.
  const [modal, setModal] = useState<"nuevo" | Servicio | null>(null);
  const [bajaId, setBajaId] = useState<number | null>(null);

  useEffect(() => {
    if (!user) return;
    let cancelado = false;
    setLoading(true);
    // No hay endpoint "mis servicios": filtramos el listado público por el estudiante logueado.
    listarServicios({})
      .then((data) => {
        if (!cancelado)
          setServicios(data.filter((s) => s.estudianteId === user.usuarioId));
      })
      .catch((err) => {
        if (!cancelado)
          setError(
            err instanceof ApiError
              ? err.message
              : "No pudimos cargar tus servicios.",
          );
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, [user]);

  function handleSaved(guardado: Servicio) {
    setServicios((prev) => {
      const existe = prev.some((s) => s.idServicio === guardado.idServicio);
      return existe
        ? prev.map((s) => (s.idServicio === guardado.idServicio ? guardado : s))
        : [guardado, ...prev];
    });
    setModal(null);
  }

  async function handleBaja(s: Servicio) {
    if (!confirm(`¿Dar de baja "${s.titulo}"? Dejará de mostrarse en la vidriera.`))
      return;
    setBajaId(s.idServicio);
    setError(null);
    try {
      await eliminarServicio(s.idServicio);
      setServicios((prev) => prev.filter((x) => x.idServicio !== s.idServicio));
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos dar de baja el servicio.",
      );
    } finally {
      setBajaId(null);
    }
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">
            Mis servicios
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            Gestioná tu oferta en LEX.
          </p>
        </div>
        <button
          onClick={() => setModal("nuevo")}
          className="rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-accent-hover"
        >
          + Publicar servicio
        </button>
      </div>

      {/* Accesos de perfil público: ver el portafolio como lo ven los demás +
          vinculación con LinkedIn (función futura). */}
      <div className="mt-6 flex flex-col gap-3 rounded-xl border border-gray-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          {user && (
            <Link
              href={`/estudiantes/${user.usuarioId}`}
              className="inline-flex items-center gap-1.5 rounded-lg border border-accent/30 bg-accent-soft/50 px-4 py-2.5 text-sm font-semibold text-accent transition hover:bg-accent-soft"
            >
              Ver mi portafolio
              <span aria-hidden="true">→</span>
            </Link>
          )}
          <span className="hidden text-xs text-gray-400 sm:inline">
            Así te ven los clientes.
          </span>
        </div>
        <LinkedInButton />
      </div>

      {error && (
        <div className="mt-6">
          <ErrorAlert message={error} />
        </div>
      )}

      <div className="mt-8">
        {loading ? (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div
                key={i}
                className="h-20 animate-pulse rounded-xl border border-gray-200 bg-gray-50"
              />
            ))}
          </div>
        ) : servicios.length === 0 ? (
          <div className="rounded-xl border border-dashed border-gray-200 bg-gray-50/50 py-16 text-center">
            <p className="font-semibold text-foreground">
              Todavía no publicaste ningún servicio
            </p>
            <p className="mt-1 text-sm text-gray-500">
              Creá el primero para que los clientes te encuentren.
            </p>
          </div>
        ) : (
          <ul className="space-y-3">
            {servicios.map((s) => (
              <li
                key={s.idServicio}
                className="flex flex-col gap-4 rounded-xl border border-gray-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="min-w-0">
                  <div className="mb-1 flex items-center gap-2">
                    <TipoBadge
                      tipoServicioId={s.tipoServicioId}
                      nombre={s.tipoServicioNombre}
                    />
                    <span className="text-sm font-bold text-foreground">
                      {formatPrecio(s.precio)}
                    </span>
                  </div>
                  <Link
                    href={`/servicios/${s.idServicio}`}
                    className="font-semibold text-foreground hover:text-accent"
                  >
                    {s.titulo}
                  </Link>
                  {s.tiempoEntregaDias && (
                    <p className="text-xs text-gray-500">
                      Entrega en {s.tiempoEntregaDias} día
                      {s.tiempoEntregaDias > 1 ? "s" : ""}
                    </p>
                  )}
                </div>
                <div className="flex shrink-0 gap-2">
                  <button
                    onClick={() => setModal(s)}
                    className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50"
                  >
                    Editar
                  </button>
                  <button
                    onClick={() => handleBaja(s)}
                    disabled={bajaId === s.idServicio}
                    className="rounded-lg border border-red-200 px-3 py-1.5 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:opacity-50"
                  >
                    {bajaId === s.idServicio ? "Dando de baja…" : "Dar de baja"}
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {modal && (
        <ServicioFormModal
          servicio={modal === "nuevo" ? undefined : modal}
          onClose={() => setModal(null)}
          onSaved={handleSaved}
        />
      )}
    </div>
  );
}
