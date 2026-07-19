// Tipos de dominio + helpers del motor de trabajos. Espejan TrabajoResponse del backend.
// Los enums viajan como string (JsonStringEnumConverter en el backend).
//
// Nota: GET /api/trabajos/{id} devuelve ademas un bloque `detalle` polimorfico por vertical
// (snapshots, sesiones, consentimiento en Salud). Ese detalle se tipa en la Parte 5, cuando
// se rehaga la pantalla de detalle de trabajo. Aca solo va la base comun.

import { apiFetch } from "./api";
import type {
  FormatoEntrega,
  ModalidadClase,
  ModalidadSalud,
  NivelClase,
  TipoServicio,
} from "./servicios";

export type EstadoTrabajo =
  | "Pendiente"
  | "Aceptado"
  | "EnCurso"
  | "Entregado"
  | "Completado"
  | "Cancelado"
  | "Disputa";

export type TipoTrabajo = TipoServicio; // los mismos 3 verticales

export interface TrabajoResponse {
  id: number;
  servicioId: number;
  clienteId: number;
  clienteNombre: string;
  estudianteId: number;
  estudianteNombre: string;
  tituloSnapshot: string;
  descripcionSnapshot: string;
  precioAcordado: number;
  estado: EstadoTrabajo;
  tipo: TipoTrabajo;
  fechaCreacion: string; // ISO UTC
  fechaInicio: string | null;
  fechaFin: string | null;
}

export interface TrabajoHistorialResponse {
  id: number;
  estadoAnterior: EstadoTrabajo | null;
  estadoNuevo: EstadoTrabajo;
  fecha: string;
  usuarioId: number | null;
}

// --- Detalle polimórfico ---
// GET /api/trabajos/{id} devuelve la base + un bloque `detalle` con los campos
// snapshot de la vertical concreta (espeja TrabajoDetalleResponse del backend).

export interface DetalleTrabajoProyectoCerrado {
  plazoEntregaFecha: string; // ISO
  revisionesMaximas: number;
  revisionesUsadas: number;
  formatoEntregaSnapshot: FormatoEntrega;
}

export interface DetalleTrabajoClase {
  materiaSnapshot: string;
  nivelSnapshot: NivelClase;
  modalidadSnapshot: ModalidadClase;
  duracionMinutosSesionSnapshot: number;
  esPaqueteSnapshot: boolean;
  cantidadSesionesTotales: number;
  sesionesCompletadas: number;
}

export interface ConsentimientoTrabajo {
  id: number;
  textoCompleto: string;
  aceptadoPorUsuarioId: number;
  fechaAceptacion: string;
  ipAceptacion: string | null;
}

export interface DetalleTrabajoSalud {
  catalogoServicioIdSnapshot: number;
  catalogoServicioNombreSnapshot: string;
  catalogoServicioAnioMinimoSnapshot: number;
  supervisorIdSnapshot: number;
  supervisorNombreSnapshot: string;
  supervisorMatriculaSnapshot: string;
  pacienteId: number;
  pacienteNombre: string;
  modalidadSaludSnapshot: ModalidadSalud;
  duracionMinutosSesionSnapshot: number;
  consentimientoId: number | null;
  consentimientoFirmado: boolean;
  consentimiento: ConsentimientoTrabajo | null;
}

export type DetalleTrabajo =
  | DetalleTrabajoProyectoCerrado
  | DetalleTrabajoClase
  | DetalleTrabajoSalud;

export type TrabajoDetalleResponse = TrabajoResponse & {
  detalle: DetalleTrabajo | null;
};

export function esTrabajoProyectoCerrado(
  t: TrabajoDetalleResponse,
): t is TrabajoDetalleResponse & { detalle: DetalleTrabajoProyectoCerrado } {
  return t.tipo === "ProyectoCerrado" && t.detalle !== null;
}

export function esTrabajoClase(
  t: TrabajoDetalleResponse,
): t is TrabajoDetalleResponse & { detalle: DetalleTrabajoClase } {
  return t.tipo === "Clase" && t.detalle !== null;
}

export function esTrabajoSalud(
  t: TrabajoDetalleResponse,
): t is TrabajoDetalleResponse & { detalle: DetalleTrabajoSalud } {
  return t.tipo === "Salud" && t.detalle !== null;
}

// --- Metadata de estados (presentacion) ---

export interface EstadoMeta {
  etiqueta: string;
  descripcion: string;
  clases: string; // clases Tailwind para el badge
}

export const ESTADO_META: Record<EstadoTrabajo, EstadoMeta> = {
  Pendiente: {
    etiqueta: "Pendiente",
    descripcion: "Esperando aceptación del estudiante",
    clases: "bg-amber-100 text-amber-800",
  },
  Aceptado: {
    etiqueta: "Aceptado",
    descripcion: "El estudiante confirmó, aún no arrancó",
    clases: "bg-blue-100 text-blue-800",
  },
  EnCurso: {
    etiqueta: "En curso",
    descripcion: "Trabajo activo",
    clases: "bg-indigo-100 text-indigo-800",
  },
  Entregado: {
    etiqueta: "Entregado",
    descripcion: "Estudiante entregó, cliente debe confirmar",
    clases: "bg-purple-100 text-purple-800",
  },
  Completado: {
    etiqueta: "Completado",
    descripcion: "Trabajo finalizado, pago liberado",
    clases: "bg-emerald-100 text-emerald-800",
  },
  Cancelado: {
    etiqueta: "Cancelado",
    descripcion: "Cancelado antes de completar",
    clases: "bg-slate-100 text-slate-800",
  },
  Disputa: {
    etiqueta: "En disputa",
    descripcion: "Conflicto activo, requiere resolución",
    clases: "bg-rose-100 text-rose-800",
  },
};

// --- Listado y detalle ---

export function listarMisTrabajos(): Promise<TrabajoResponse[]> {
  return apiFetch<TrabajoResponse[]>("/api/trabajos/mios");
}

export function obtenerTrabajo(id: number): Promise<TrabajoDetalleResponse> {
  return apiFetch<TrabajoDetalleResponse>(`/api/trabajos/${id}`);
}

export function listarHistorialTrabajo(
  id: number,
): Promise<TrabajoHistorialResponse[]> {
  return apiFetch<TrabajoHistorialResponse[]>(`/api/trabajos/${id}/historial`);
}

// --- Contratacion por vertical ---
// ProyectoCerrado no reserva turnos. Clase reserva N slots (uno por sesion del paquete).
// Salud reserva un unico slot y requiere paciente; el consentimiento se firma aparte.

export interface ContratarProyectoCerradoRequest {
  servicioId: number;
  notasCliente?: string;
}

export function contratarProyectoCerrado(
  req: ContratarProyectoCerradoRequest,
): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>("/api/trabajos/proyecto-cerrado", {
    method: "POST",
    body: req,
  });
}

export interface ContratarClaseRequest {
  servicioId: number;
  slotsElegidos: string[]; // ISO UTC datetimes
  notasCliente?: string;
}

export function contratarClase(
  req: ContratarClaseRequest,
): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>("/api/trabajos/clase", {
    method: "POST",
    body: req,
  });
}

export interface ContratarSaludRequest {
  servicioId: number;
  pacienteId: number;
  slotElegido: string; // ISO UTC datetime
  notasCliente?: string;
}

export function contratarSalud(
  req: ContratarSaludRequest,
): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>("/api/trabajos/salud", {
    method: "POST",
    body: req,
  });
}

// --- Transiciones de estado (un endpoint POST por accion) ---

export function aceptarTrabajo(id: number): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>(`/api/trabajos/${id}/aceptar`, {
    method: "POST",
  });
}

export function iniciarTrabajo(id: number): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>(`/api/trabajos/${id}/iniciar`, {
    method: "POST",
  });
}

export function entregarTrabajo(id: number): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>(`/api/trabajos/${id}/entregar`, {
    method: "POST",
  });
}

export function completarTrabajo(id: number): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>(`/api/trabajos/${id}/completar`, {
    method: "POST",
  });
}

export function cancelarTrabajo(
  id: number,
  motivo?: string,
): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>(`/api/trabajos/${id}/cancelar`, {
    method: "POST",
    body: motivo ? { motivo } : undefined,
  });
}

export function disputarTrabajo(
  id: number,
  motivo: string,
): Promise<TrabajoResponse> {
  return apiFetch<TrabajoResponse>(`/api/trabajos/${id}/disputar`, {
    method: "POST",
    body: { motivo },
  });
}

// Firma de consentimiento (solo Salud). Habilita pasar de Aceptado a EnCurso.
export function firmarConsentimiento(trabajoId: number): Promise<void> {
  return apiFetch<void>(`/api/trabajos/salud/${trabajoId}/consentimiento`, {
    method: "POST",
  });
}
