"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { obtenerPortafolio, type Portafolio } from "@/lib/portafolio";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { ServiceCard } from "@/components/ServiceCard";
import { ResenasList } from "@/components/ResenasList";
import { LinkedInButton } from "@/components/LinkedInButton";
import { StarsRow } from "@/components/Stars";
import { ErrorAlert } from "@/components/ui";

export default function PortafolioPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const { user } = useAuth();

  const [portafolio, setPortafolio] = useState<Portafolio | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!Number.isFinite(id)) {
      setError("Estudiante no válido.");
      setLoading(false);
      return;
    }
    let cancelado = false;
    setLoading(true);
    setError(null);

    obtenerPortafolio(id)
      .then((p) => {
        if (!cancelado) setPortafolio(p);
      })
      .catch((err) => {
        if (cancelado) return;
        setError(
          err instanceof ApiError
            ? err.message
            : "No pudimos cargar el portafolio.",
        );
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });

    return () => {
      cancelado = true;
    };
  }, [id]);

  if (loading) return <PortafolioSkeleton />;

  if (error || !portafolio) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6">
        <ErrorAlert message={error ?? "Portafolio no encontrado."} />
        <Link
          href="/"
          className="mt-6 inline-block text-sm font-semibold text-accent hover:underline"
        >
          ← Volver a la vidriera
        </Link>
      </div>
    );
  }

  const esPropio = user?.usuarioId === portafolio.usuarioId;
  const inicial = portafolio.nombreCompleto.trim().charAt(0).toUpperCase() || "?";

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      {/* Aviso para el dueño: así te ven los demás + vincular LinkedIn */}
      {esPropio && (
        <div className="mb-6 flex flex-col gap-3 rounded-xl border border-accent/20 bg-accent-soft/40 p-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-gray-600">
            Estás viendo <span className="font-semibold text-foreground">tu portafolio público</span>, tal como lo ven los demás.
          </p>
          <LinkedInButton />
        </div>
      )}

      {/* Hero */}
      <section className="flex flex-col items-center gap-5 rounded-2xl border border-gray-200 bg-white p-8 text-center shadow-sm sm:flex-row sm:items-center sm:gap-7 sm:text-left">
        <div className="flex h-24 w-24 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-accent to-accent-hover text-4xl font-extrabold text-white shadow-inner">
          {inicial}
        </div>
        <div className="min-w-0 flex-1">
          <h1 className="text-2xl font-bold tracking-tight text-foreground sm:text-3xl">
            {portafolio.nombreCompleto}
          </h1>
          {portafolio.anioCursado != null && (
            <p className="mt-1 text-sm font-medium text-accent">
              {portafolio.anioCursado}° año
            </p>
          )}
          {portafolio.bio && (
            <p className="mt-3 max-w-2xl whitespace-pre-line text-sm text-gray-600">
              {portafolio.bio}
            </p>
          )}
          <div className="mt-4 flex items-center justify-center gap-2 sm:justify-start">
            {portafolio.calificacionPromedio > 0 ? (
              <>
                <StarsRow value={Math.round(portafolio.calificacionPromedio)} />
                <span className="text-lg font-bold text-foreground">
                  {portafolio.calificacionPromedio.toFixed(1)}
                </span>
                <span className="text-sm text-gray-400">
                  ({portafolio.resenas.length} reseña
                  {portafolio.resenas.length !== 1 ? "s" : ""})
                </span>
              </>
            ) : (
              <span className="text-sm text-gray-400">Todavía sin calificación</span>
            )}
          </div>
        </div>
      </section>

      {/* Sello de verificación institucional (el diferenciador de confianza) */}
      {portafolio.carreras.length > 0 && (
        <section className="mt-6 grid gap-3 sm:grid-cols-2">
          {portafolio.carreras.map((c) => {
            const verificado = c.estadoVerificacion === "Verificado";
            return (
              <div
                key={c.carreraId}
                className={`flex items-start gap-3 rounded-xl border p-4 ${
                  verificado
                    ? "border-emerald-200 bg-emerald-50/60"
                    : "border-amber-200 bg-amber-50/60"
                }`}
              >
                <VerifIcon verificado={verificado} />
                <div className="min-w-0">
                  <p className="font-semibold text-foreground">{c.carrera}</p>
                  <p className="text-sm text-gray-600">{c.institucion}</p>
                  <span
                    className={`mt-1.5 inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ${
                      verificado
                        ? "bg-emerald-100 text-emerald-700"
                        : "bg-amber-100 text-amber-700"
                    }`}
                  >
                    {verificado ? "Verificado" : "Pendiente de verificación"}
                  </span>
                </div>
              </div>
            );
          })}
        </section>
      )}

      {/* Estadísticas */}
      <section className="mt-6 grid grid-cols-3 gap-3">
        <StatCard valor={portafolio.trabajosCompletados} label="Trabajos completados" />
        <StatCard
          valor={
            portafolio.calificacionPromedio > 0
              ? portafolio.calificacionPromedio.toFixed(1)
              : "—"
          }
          label="Calificación"
        />
        <StatCard valor={portafolio.resenas.length} label="Reseñas" />
      </section>

      {/* Servicios */}
      <section className="mt-12">
        <h2 className="text-lg font-bold text-foreground">
          Servicios{" "}
          <span className="font-normal text-gray-400">
            ({portafolio.servicios.length})
          </span>
        </h2>
        {portafolio.servicios.length === 0 ? (
          <p className="mt-3 rounded-lg border border-dashed border-gray-200 bg-gray-50/50 px-4 py-8 text-center text-sm text-gray-500">
            Este estudiante todavía no publicó servicios.
          </p>
        ) : (
          <div className="mt-4 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {portafolio.servicios.map((s) => (
              <ServiceCard key={s.idServicio} servicio={s} />
            ))}
          </div>
        )}
      </section>

      {/* Reseñas */}
      <section className="mt-12">
        <h2 className="text-lg font-bold text-foreground">
          Reseñas{" "}
          <span className="font-normal text-gray-400">
            ({portafolio.resenas.length})
          </span>
        </h2>
        <div className="mt-4">
          <ResenasList
            resenas={portafolio.resenas}
            emptyText="Todavía no tiene reseñas. ¡Podés ser su primer cliente!"
          />
        </div>
      </section>
    </div>
  );
}

function StatCard({ valor, label }: { valor: number | string; label: string }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-4 text-center shadow-sm">
      <p className="text-2xl font-extrabold text-foreground sm:text-3xl">{valor}</p>
      <p className="mt-1 text-xs text-gray-500 sm:text-sm">{label}</p>
    </div>
  );
}

function VerifIcon({ verificado }: { verificado: boolean }) {
  if (verificado) {
    return (
      <svg
        className="h-6 w-6 shrink-0 text-emerald-500"
        viewBox="0 0 20 20"
        fill="currentColor"
        aria-hidden="true"
      >
        <path
          fillRule="evenodd"
          d="M16.403 12.652a3 3 0 000-5.304 3 3 0 00-3.75-3.751 3 3 0 00-5.305 0 3 3 0 00-3.751 3.75 3 3 0 000 5.305 3 3 0 003.75 3.751 3 3 0 005.305 0 3 3 0 003.751-3.75zm-2.546-4.46a.75.75 0 00-1.214-.883l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z"
          clipRule="evenodd"
        />
      </svg>
    );
  }
  return (
    <svg
      className="h-6 w-6 shrink-0 text-amber-500"
      viewBox="0 0 20 20"
      fill="currentColor"
      aria-hidden="true"
    >
      <path
        fillRule="evenodd"
        d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.515 2.625H3.72c-1.345 0-2.188-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z"
        clipRule="evenodd"
      />
    </svg>
  );
}

function PortafolioSkeleton() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex flex-col items-center gap-5 rounded-2xl border border-gray-200 bg-white p-8 sm:flex-row">
        <div className="h-24 w-24 shrink-0 animate-pulse rounded-full bg-gray-100" />
        <div className="w-full space-y-3">
          <div className="h-7 w-1/2 animate-pulse rounded bg-gray-100" />
          <div className="h-4 w-3/4 animate-pulse rounded bg-gray-100" />
          <div className="h-4 w-1/3 animate-pulse rounded bg-gray-100" />
        </div>
      </div>
      <div className="mt-6 grid grid-cols-3 gap-3">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-20 animate-pulse rounded-xl bg-gray-100" />
        ))}
      </div>
    </div>
  );
}
