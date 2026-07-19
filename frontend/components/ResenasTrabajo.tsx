"use client";

// Reseñas de un trabajo: lista las reseñas mutuas y, si corresponde, deja que el
// usuario califique. El backend valida (solo Completado, solo partes, una vez cada una);
// lo reflejamos en la UI.

import { useEffect, useState, type FormEvent } from "react";
import {
  crearResenaTrabajo,
  listarResenasTrabajo,
  type ResenaResponse,
} from "@/lib/resenas";
import { formatFecha } from "@/lib/servicios";
import type { TrabajoResponse } from "@/lib/trabajos";
import { ApiError } from "@/lib/api";
import { useAuth } from "@/contexts/AuthContext";
import { StarsInput, StarsRow } from "@/components/Stars";
import { ErrorAlert } from "@/components/ui";

export function ResenasTrabajo({ trabajo }: { trabajo: TrabajoResponse }) {
  const { user } = useAuth();

  const [resenas, setResenas] = useState<ResenaResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [puntaje, setPuntaje] = useState(0);
  const [comentario, setComentario] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [exito, setExito] = useState(false);

  const idTrabajo = trabajo.id;

  async function cargarResenas() {
    setResenas(await listarResenasTrabajo(idTrabajo));
  }

  useEffect(() => {
    let cancelado = false;
    setLoading(true);
    setLoadError(null);
    listarResenasTrabajo(idTrabajo)
      .then((data) => {
        if (!cancelado) setResenas(data);
      })
      .catch((err) => {
        if (!cancelado)
          setLoadError(
            err instanceof ApiError
              ? err.message
              : "No pudimos cargar las reseñas.",
          );
      })
      .finally(() => {
        if (!cancelado) setLoading(false);
      });
    return () => {
      cancelado = true;
    };
  }, [idTrabajo]);

  function nombreReceptor(r: ResenaResponse): string {
    if (r.receptorUsuarioId === trabajo.estudianteId)
      return trabajo.estudianteNombre;
    if (r.receptorUsuarioId === trabajo.clienteId) return trabajo.clienteNombre;
    return "la otra parte";
  }

  const completado = trabajo.estado === "Completado";
  const esParte =
    user?.usuarioId === trabajo.estudianteId ||
    user?.usuarioId === trabajo.clienteId;
  const miResena = resenas.find((r) => r.autorUsuarioId === user?.usuarioId);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    if (puntaje < 1 || puntaje > 5) {
      setSubmitError("Elegí un puntaje de 1 a 5 estrellas.");
      return;
    }
    setSubmitting(true);
    try {
      await crearResenaTrabajo(idTrabajo, {
        puntaje,
        comentario: comentario.trim() || undefined,
      });
      await cargarResenas();
      setExito(true);
      setPuntaje(0);
      setComentario("");
    } catch (err) {
      setSubmitError(
        err instanceof ApiError ? err.message : "No pudimos enviar la reseña.",
      );
      if (err instanceof ApiError && err.status === 400) {
        try {
          await cargarResenas();
        } catch {
          // si el refresh falla, igual mostramos el error
        }
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <h2 className="text-sm font-semibold text-slate-900">Reseñas</h2>

      {esParte && (
        <div className="mt-3">
          {!completado ? (
            <div className="rounded-lg border border-dashed border-slate-200 bg-slate-50/50 px-4 py-4 text-sm text-slate-500">
              Podrás calificar cuando el trabajo esté completado.
            </div>
          ) : miResena ? (
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="flex items-center justify-between gap-2">
                <h3 className="text-sm font-semibold text-slate-900">Tu reseña</h3>
                <StarsRow value={miResena.puntaje} />
              </div>
              {miResena.comentario && (
                <p className="mt-2 text-sm text-slate-600">
                  {miResena.comentario}
                </p>
              )}
              <p className="mt-2 text-xs text-slate-400">
                {formatFecha(miResena.fecha)} · calificaste a{" "}
                {nombreReceptor(miResena)}
              </p>
            </div>
          ) : exito ? (
            <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">
              ¡Gracias por tu reseña!
            </div>
          ) : (
            <form
              onSubmit={handleSubmit}
              className="rounded-lg border border-slate-200 bg-white p-4"
            >
              <h3 className="font-semibold text-slate-900">
                Calificá esta experiencia
              </h3>
              <p className="mt-1 text-sm text-slate-500">
                Tu reseña es para{" "}
                <span className="font-medium text-slate-900">
                  {user?.usuarioId === trabajo.clienteId
                    ? trabajo.estudianteNombre
                    : trabajo.clienteNombre}
                </span>
                .
              </p>
              <div className="mt-4 space-y-4">
                {submitError && <ErrorAlert message={submitError} />}
                <div>
                  <span className="mb-1.5 block text-sm font-medium text-slate-700">
                    Puntaje
                  </span>
                  <StarsInput value={puntaje} onChange={setPuntaje} />
                </div>
                <div>
                  <label
                    htmlFor="comentario"
                    className="mb-1.5 block text-sm font-medium text-slate-700"
                  >
                    Comentario (opcional)
                  </label>
                  <textarea
                    id="comentario"
                    rows={3}
                    maxLength={1000}
                    value={comentario}
                    onChange={(e) => setComentario(e.target.value)}
                    placeholder="Contá cómo fue tu experiencia…"
                    className="w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                  />
                </div>
                <button
                  type="submit"
                  disabled={submitting}
                  className="rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-70"
                >
                  {submitting ? "Enviando…" : "Enviar reseña"}
                </button>
              </div>
            </form>
          )}
        </div>
      )}

      <div className="mt-5">
        {loading ? (
          <div className="h-16 animate-pulse rounded-lg bg-slate-100" />
        ) : loadError ? (
          <ErrorAlert message={loadError} />
        ) : resenas.length === 0 ? (
          <p className="rounded-lg border border-dashed border-slate-200 bg-slate-50/50 px-4 py-5 text-center text-sm text-slate-500">
            Este trabajo todavía no tiene reseñas.
          </p>
        ) : (
          <ul className="space-y-3">
            {resenas.map((r) => (
              <li
                key={r.id}
                className="rounded-lg border border-slate-200 bg-white p-4"
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="text-sm text-slate-600">
                    <span className="font-semibold text-slate-900">
                      {r.autorNombre}
                    </span>{" "}
                    calificó a{" "}
                    <span className="font-semibold text-slate-900">
                      {nombreReceptor(r)}
                    </span>
                  </span>
                  <StarsRow value={r.puntaje} />
                </div>
                {r.comentario && (
                  <p className="mt-2 text-sm text-slate-600">{r.comentario}</p>
                )}
                <p className="mt-2 text-xs text-slate-400">
                  {formatFecha(r.fecha)}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
