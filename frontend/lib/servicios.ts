// Tipos de dominio + helpers de servicios y reseñas.
// Espejan exactamente los DTOs del backend (ServicioResponse / ResenaResponse).

import { apiFetch } from "./api";

export interface Servicio {
  idServicio: number;
  titulo: string;
  descripcion: string | null;
  imagenUrl: string | null;
  precio: number;
  tiempoEntregaDias: number | null;
  activo: boolean;
  fechaPublicacion: string;
  tipoServicioId: number;
  tipoServicioNombre: string;
  estudianteId: number;
  estudianteNombre: string;
  estudianteCalificacion: number;
}

export interface Resena {
  idResena: number;
  idTrabajo: number;
  autorUsuarioId: number;
  autorNombre: string;
  receptorUsuarioId: number;
  puntaje: number;
  comentario: string | null;
  fecha: string;
}

// Tipos de servicio del backend (tabla tipo_servicio).
export const TIPOS_SERVICIO = [
  { id: 1, nombre: "Digital" },
  { id: 2, nombre: "Clase" },
  { id: 3, nombre: "Salud" },
  { id: 4, nombre: "Otro" },
] as const;

/** Lista pública de servicios activos, con filtros opcionales. */
export function listarServicios(params: {
  tipoServicioId?: number | null;
  texto?: string;
}): Promise<Servicio[]> {
  const qs = new URLSearchParams();
  if (params.tipoServicioId) qs.set("tipoServicioId", String(params.tipoServicioId));
  if (params.texto?.trim()) qs.set("texto", params.texto.trim());
  const suffix = qs.toString() ? `?${qs.toString()}` : "";
  return apiFetch<Servicio[]>(`/api/servicios${suffix}`, { auth: false });
}

/** Detalle público de un servicio. */
export function obtenerServicio(id: number): Promise<Servicio> {
  return apiFetch<Servicio>(`/api/servicios/${id}`, { auth: false });
}

export interface ServicioInput {
  titulo: string;
  descripcion?: string | null;
  precio: number;
  tipoServicioId: number;
  tiempoEntregaDias?: number | null;
  imagenUrl?: string | null;
}

/** Publica un servicio nuevo (estudiante autenticado). */
export function crearServicio(input: ServicioInput): Promise<Servicio> {
  return apiFetch<Servicio>("/api/servicios", { method: "POST", body: input });
}

/** Edita un servicio propio. */
export function actualizarServicio(
  id: number,
  input: ServicioInput,
): Promise<Servicio> {
  return apiFetch<Servicio>(`/api/servicios/${id}`, {
    method: "PUT",
    body: input,
  });
}

/** Baja lógica de un servicio propio. */
export function eliminarServicio(id: number): Promise<void> {
  return apiFetch<void>(`/api/servicios/${id}`, { method: "DELETE" });
}

/** El área Salud (id 3) requiere supervisión: no se contrata por el flujo directo. */
export const TIPO_SALUD_ID = 3;

/** Reseñas recibidas por un usuario (reputación pública). */
export function listarResenasUsuario(idUsuario: number): Promise<Resena[]> {
  return apiFetch<Resena[]>(`/api/usuarios/${idUsuario}/resenas`, { auth: false });
}

export interface ResenaInput {
  puntaje: number;
  comentario?: string | null;
}

/** Reseñas de un trabajo puntual (ambas partes). Requiere ser parte del trabajo. */
export function listarResenasTrabajo(idTrabajo: number): Promise<Resena[]> {
  return apiFetch<Resena[]>(`/api/trabajos/${idTrabajo}/resenas`);
}

/** Deja una reseña sobre la otra parte de un trabajo Completado.
 *  El backend valida: estado Completado (400), ser parte (403) y una sola
 *  reseña por parte (400 "Ya dejaste una reseña para este trabajo"). */
export function crearResenaTrabajo(
  idTrabajo: number,
  input: ResenaInput,
): Promise<Resena> {
  return apiFetch<Resena>(`/api/trabajos/${idTrabajo}/resenas`, {
    method: "POST",
    body: input,
  });
}

// --- Helpers de presentación ---

/** Colores suaves del badge según el tipo de servicio. */
export function tipoBadgeClasses(tipoServicioId: number): string {
  switch (tipoServicioId) {
    case 1: // Digital
      return "bg-blue-50 text-blue-700 ring-blue-100";
    case 2: // Clase
      return "bg-amber-50 text-amber-700 ring-amber-100";
    case 3: // Salud
      return "bg-emerald-50 text-emerald-700 ring-emerald-100";
    default: // Otro
      return "bg-gray-100 text-gray-600 ring-gray-200";
  }
}

const formatoMoneda = new Intl.NumberFormat("es-AR", {
  maximumFractionDigits: 0,
});

export function formatPrecio(precio: number): string {
  return `$${formatoMoneda.format(precio)}`;
}

export function formatFecha(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toLocaleDateString("es-AR", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}
