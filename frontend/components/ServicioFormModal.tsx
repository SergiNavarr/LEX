"use client";

import { useState, type FormEvent } from "react";
import {
  actualizarServicio,
  crearServicio,
  TIPOS_SERVICIO,
  type Servicio,
  type ServicioInput,
} from "@/lib/servicios";
import { ApiError } from "@/lib/api";
import { ErrorAlert, Field, Input, Select, SubmitButton } from "@/components/ui";

export function ServicioFormModal({
  servicio,
  onClose,
  onSaved,
}: {
  /** Si viene, es edición; si no, es alta. */
  servicio?: Servicio;
  onClose: () => void;
  onSaved: (s: Servicio) => void;
}) {
  const editando = Boolean(servicio);

  const [titulo, setTitulo] = useState(servicio?.titulo ?? "");
  const [descripcion, setDescripcion] = useState(servicio?.descripcion ?? "");
  const [precio, setPrecio] = useState(
    servicio ? String(servicio.precio) : "",
  );
  const [tipoServicioId, setTipoServicioId] = useState(
    servicio?.tipoServicioId ?? TIPOS_SERVICIO[0].id,
  );
  const [tiempoEntregaDias, setTiempoEntregaDias] = useState(
    servicio?.tiempoEntregaDias != null
      ? String(servicio.tiempoEntregaDias)
      : "",
  );
  const [imagenUrl, setImagenUrl] = useState(servicio?.imagenUrl ?? "");

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    const precioNum = Number(precio);
    if (!Number.isFinite(precioNum) || precioNum <= 0) {
      setError("El precio debe ser un número mayor a 0.");
      return;
    }

    const diasNum = tiempoEntregaDias.trim() ? Number(tiempoEntregaDias) : null;
    if (diasNum !== null && (!Number.isInteger(diasNum) || diasNum <= 0)) {
      setError("El tiempo de entrega debe ser un número entero de días.");
      return;
    }

    const input: ServicioInput = {
      titulo: titulo.trim(),
      descripcion: descripcion.trim() || null,
      precio: precioNum,
      tipoServicioId,
      tiempoEntregaDias: diasNum,
      imagenUrl: imagenUrl.trim() || null,
    };

    setSubmitting(true);
    try {
      const guardado = servicio
        ? await actualizarServicio(servicio.idServicio, input)
        : await crearServicio(input);
      onSaved(guardado);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos guardar el servicio.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onClose}
    >
      <div
        className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between">
          <h2 className="text-lg font-bold text-foreground">
            {editando ? "Editar servicio" : "Publicar servicio"}
          </h2>
          <button
            onClick={onClose}
            className="rounded-lg p-1 text-gray-400 transition hover:bg-gray-100 hover:text-gray-600"
            aria-label="Cerrar"
          >
            <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-5 space-y-4">
          {error && <ErrorAlert message={error} />}

          <Field label="Título" htmlFor="titulo">
            <Input
              id="titulo"
              required
              maxLength={150}
              value={titulo}
              onChange={(e) => setTitulo(e.target.value)}
              placeholder="Ej: Diseño de logo profesional"
            />
          </Field>

          <Field label="Descripción" htmlFor="descripcion">
            <textarea
              id="descripcion"
              rows={3}
              maxLength={1000}
              value={descripcion}
              onChange={(e) => setDescripcion(e.target.value)}
              placeholder="Contá qué incluye el servicio…"
              className="w-full rounded-lg border border-gray-200 bg-white px-3.5 py-2.5 text-sm text-foreground shadow-sm outline-none transition placeholder:text-gray-400 focus:border-accent focus:ring-2 focus:ring-accent/20"
            />
          </Field>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Precio ($)" htmlFor="precio">
              <Input
                id="precio"
                type="number"
                min="1"
                step="any"
                required
                value={precio}
                onChange={(e) => setPrecio(e.target.value)}
                placeholder="5000"
              />
            </Field>

            <Field label="Entrega (días)" htmlFor="dias">
              <Input
                id="dias"
                type="number"
                min="1"
                step="1"
                value={tiempoEntregaDias}
                onChange={(e) => setTiempoEntregaDias(e.target.value)}
                placeholder="Opcional"
              />
            </Field>
          </div>

          <Field label="Tipo de servicio" htmlFor="tipo">
            <Select
              id="tipo"
              value={tipoServicioId}
              onChange={(e) => setTipoServicioId(Number(e.target.value))}
            >
              {TIPOS_SERVICIO.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.nombre}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Imagen de portada (URL)" htmlFor="imagenUrl">
            <Input
              id="imagenUrl"
              type="url"
              value={imagenUrl}
              onChange={(e) => setImagenUrl(e.target.value)}
              placeholder="https://… (opcional)"
            />
          </Field>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-lg border border-gray-200 px-4 py-2.5 text-sm font-semibold text-gray-700 transition hover:bg-gray-50"
            >
              Cancelar
            </button>
            <div className="flex-1">
              <SubmitButton loading={submitting}>
                {editando ? "Guardar cambios" : "Publicar"}
              </SubmitButton>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
