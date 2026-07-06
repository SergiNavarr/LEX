"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import {
  formatFecha,
  formatPrecio,
  listarResenasUsuario,
  obtenerServicio,
  TIPO_SALUD_ID,
  type Resena,
  type Servicio,
} from "@/lib/servicios";
import { contratarServicio } from "@/lib/trabajos";
import { ContratarSaludModal } from "@/components/ContratarSaludModal";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { RatingBadge, StarsRow } from "@/components/Stars";
import { TipoBadge } from "@/components/TipoBadge";
import { ServiceCover } from "@/components/ServiceCover";
import { ResenasList } from "@/components/ResenasList";
import { ErrorAlert } from "@/components/ui";

export default function ServicioDetallePage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);

  const [servicio, setServicio] = useState<Servicio | null>(null);
  const [resenas, setResenas] = useState<Resena[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!Number.isFinite(id)) {
      setError("Servicio no válido.");
      setLoading(false);
      return;
    }
    let cancelado = false;
    setLoading(true);
    setError(null);

    obtenerServicio(id)
      .then(async (s) => {
        if (cancelado) return;
        setServicio(s);
        // Reseñas del estudiante dueño del servicio (no bloquea el render del detalle).
        try {
          const r = await listarResenasUsuario(s.estudianteId);
          if (!cancelado) setResenas(r);
        } catch {
          // si fallan las reseñas, mostramos el detalle igual
        }
      })
      .catch((err) => {
        if (cancelado) return;
        setError(
          err instanceof ApiError
            ? err.message
            : "No pudimos cargar el servicio.",
        );
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });

    return () => {
      cancelado = true;
    };
  }, [id]);

  if (loading) return <DetalleSkeleton />;

  if (error || !servicio) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6">
        <ErrorAlert message={error ?? "Servicio no encontrado."} />
        <Link
          href="/"
          className="mt-6 inline-block text-sm font-semibold text-accent hover:underline"
        >
          ← Volver a la vidriera
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <Link
        href="/"
        className="mb-6 inline-block text-sm font-medium text-gray-500 transition hover:text-accent"
      >
        ← Volver a la vidriera
      </Link>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-[1fr_360px]">
        {/* Columna principal */}
        <div>
          {/* Portada: imagen del servicio o el placeholder "LEX" como fallback. */}
          <ServiceCover
            src={servicio.imagenUrl}
            alt={servicio.titulo}
            className="mb-6 aspect-[16/9] w-full rounded-xl border border-gray-100"
          />

          <TipoBadge
            tipoServicioId={servicio.tipoServicioId}
            nombre={servicio.tipoServicioNombre}
          />
          <h1 className="mt-3 text-2xl font-bold tracking-tight text-foreground sm:text-3xl">
            {servicio.titulo}
          </h1>

          <div className="mt-3 flex items-center gap-3 text-sm text-gray-500">
            <span>
              por{" "}
              <Link
                href={`/estudiantes/${servicio.estudianteId}`}
                className="font-semibold text-foreground transition hover:text-accent hover:underline"
              >
                {servicio.estudianteNombre}
              </Link>
            </span>
            <span className="text-gray-300">•</span>
            <RatingBadge value={servicio.estudianteCalificacion} />
          </div>

          <div className="mt-8">
            <h2 className="text-lg font-semibold text-foreground">
              Sobre este servicio
            </h2>
            <p className="mt-2 whitespace-pre-line text-gray-600">
              {servicio.descripcion?.trim() || "El estudiante no agregó una descripción."}
            </p>
          </div>

          {/* Reseñas */}
          <div className="mt-10">
            <h2 className="text-lg font-semibold text-foreground">
              Reseñas de {servicio.estudianteNombre}{" "}
              <span className="font-normal text-gray-400">
                ({resenas.length})
              </span>
            </h2>

            {resenas.length > 0 && (
              <div className="mt-2 flex items-center gap-2 text-sm text-gray-500">
                <StarsRow value={Math.round(servicio.estudianteCalificacion)} />
                <span className="font-semibold text-foreground">
                  {servicio.estudianteCalificacion.toFixed(1)}
                </span>
                <span>
                  · {resenas.length} reseña{resenas.length > 1 ? "s" : ""}
                </span>
              </div>
            )}

            <div className="mt-4">
              <ResenasList
                resenas={resenas}
                emptyText="Todavía no tiene reseñas. ¡Podés ser su primer cliente!"
              />
            </div>
          </div>
        </div>

        {/* Panel lateral de contratación (sticky en desktop) */}
        <aside className="lg:sticky lg:top-24 lg:self-start">
          <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm">
            <div className="flex items-baseline justify-between">
              <span className="text-sm text-gray-500">Precio</span>
              <span className="text-3xl font-extrabold text-foreground">
                {formatPrecio(servicio.precio)}
              </span>
            </div>

            <dl className="mt-5 space-y-3 border-t border-gray-100 pt-5 text-sm">
              <div className="flex justify-between">
                <dt className="text-gray-500">Entrega</dt>
                <dd className="font-medium text-foreground">
                  {servicio.tiempoEntregaDias
                    ? `${servicio.tiempoEntregaDias} día${
                        servicio.tiempoEntregaDias > 1 ? "s" : ""
                      }`
                    : "A coordinar"}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500">Tipo</dt>
                <dd className="font-medium text-foreground">
                  {servicio.tipoServicioNombre}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500">Publicado</dt>
                <dd className="font-medium text-foreground">
                  {formatFecha(servicio.fechaPublicacion)}
                </dd>
              </div>
            </dl>

            <div className="mt-6">
              <ContratarButton servicio={servicio} />
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}

/**
 * Botón de contratación. Estado según la sesión y el tipo de servicio:
 *  - Servicio de Salud → bloqueado (ese flujo requiere paciente + consentimiento;
 *    se hace en un paso posterior con /contratar-servicio-salud).
 *  - Sin sesión        → link a /login.
 *  - Cliente           → contrata de verdad (POST /api/trabajos/contratar-servicio),
 *    muestra confirmación y redirige a /panel/trabajos.
 *  - Otro rol          → deshabilitado con aclaración (solo los clientes contratan).
 */
function ContratarButton({ servicio }: { servicio: Servicio }) {
  const { isAuthenticated, hasRole, loading } = useAuth();
  const router = useRouter();
  const [enviando, setEnviando] = useState(false);
  const [ok, setOk] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [saludModal, setSaludModal] = useState(false);

  if (loading) {
    return <div className="h-11 w-full animate-pulse rounded-lg bg-gray-100" />;
  }

  const esSalud = servicio.tipoServicioId === TIPO_SALUD_ID;

  // Flujo de Salud (requiere paciente + consentimiento informado).
  if (esSalud) {
    if (!isAuthenticated) {
      return (
        <Link
          href="/login"
          className="flex w-full items-center justify-center rounded-lg bg-accent px-4 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-accent-hover"
        >
          Contratar
        </Link>
      );
    }
    if (!hasRole("Cliente")) {
      return (
        <>
          <button
            type="button"
            disabled
            className="flex w-full cursor-not-allowed items-center justify-center rounded-lg bg-gray-100 px-4 py-3 text-sm font-semibold text-gray-400"
          >
            Contratar
          </button>
          <p className="mt-2 text-center text-xs text-gray-400">
            Solo los clientes pueden contratar servicios.
          </p>
        </>
      );
    }
    return (
      <>
        <button
          type="button"
          onClick={() => setSaludModal(true)}
          className="flex w-full items-center justify-center rounded-lg bg-accent px-4 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-accent-hover"
        >
          Contratar
        </button>
        <p className="mt-2 text-center text-xs text-gray-500">
          Requiere elegir un paciente y aceptar el consentimiento informado.
        </p>
        {saludModal && (
          <ContratarSaludModal
            servicio={servicio}
            onClose={() => setSaludModal(false)}
            onContratado={() => router.push("/panel/trabajos")}
          />
        )}
      </>
    );
  }

  if (!isAuthenticated) {
    return (
      <Link
        href="/login"
        className="flex w-full items-center justify-center rounded-lg bg-accent px-4 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-accent-hover"
      >
        Contratar
      </Link>
    );
  }

  if (!hasRole("Cliente")) {
    return (
      <>
        <button
          type="button"
          disabled
          className="flex w-full cursor-not-allowed items-center justify-center rounded-lg bg-gray-100 px-4 py-3 text-sm font-semibold text-gray-400"
        >
          Contratar
        </button>
        <p className="mt-2 text-center text-xs text-gray-400">
          Solo los clientes pueden contratar servicios.
        </p>
      </>
    );
  }

  async function handleContratar() {
    setEnviando(true);
    setErrorMsg(null);
    try {
      await contratarServicio(servicio.idServicio);
      setOk(true);
      // Pequeña pausa para que se vea la confirmación antes de redirigir.
      setTimeout(() => router.push("/panel/trabajos"), 900);
    } catch (err) {
      setErrorMsg(
        err instanceof ApiError ? err.message : "No pudimos contratar el servicio.",
      );
      setEnviando(false);
    }
  }

  if (ok) {
    return (
      <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-center text-sm font-semibold text-emerald-700">
        ¡Contratado! Te llevamos a tus trabajos…
      </div>
    );
  }

  return (
    <>
      {errorMsg && (
        <div className="mb-3">
          <ErrorAlert message={errorMsg} />
        </div>
      )}
      <button
        type="button"
        onClick={handleContratar}
        disabled={enviando}
        className="flex w-full items-center justify-center rounded-lg bg-accent px-4 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-accent-hover disabled:cursor-not-allowed disabled:opacity-70"
      >
        {enviando ? "Contratando…" : "Contratar"}
      </button>
    </>
  );
}

function DetalleSkeleton() {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-[1fr_360px]">
        <div className="space-y-4">
          <div className="h-5 w-20 animate-pulse rounded-full bg-gray-100" />
          <div className="h-8 w-3/4 animate-pulse rounded bg-gray-100" />
          <div className="h-4 w-1/2 animate-pulse rounded bg-gray-100" />
          <div className="mt-8 space-y-2">
            <div className="h-4 w-full animate-pulse rounded bg-gray-100" />
            <div className="h-4 w-full animate-pulse rounded bg-gray-100" />
            <div className="h-4 w-2/3 animate-pulse rounded bg-gray-100" />
          </div>
        </div>
        <div className="h-64 animate-pulse rounded-xl bg-gray-100" />
      </div>
    </div>
  );
}
