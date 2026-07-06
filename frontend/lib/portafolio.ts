// Portafolio público del estudiante: una sola llamada que trae perfil,
// verificación institucional, servicios y reseñas (GET /api/estudiantes/{id}/portafolio).

import { apiFetch } from "./api";
import type { Resena, Servicio } from "./servicios";

export type EstadoVerificacion = "Verificado" | "Pendiente";

export interface CarreraPortafolio {
  carreraId: number;
  carrera: string;
  institucion: string;
  estadoVerificacion: EstadoVerificacion;
}

export interface Portafolio {
  usuarioId: number;
  nombreCompleto: string;
  bio: string | null;
  anioCursado: number | null;
  calificacionPromedio: number;
  trabajosCompletados: number;
  carreras: CarreraPortafolio[];
  servicios: Servicio[];
  resenas: Resena[];
}

/** Portafolio público de un estudiante. No requiere sesión. */
export function obtenerPortafolio(id: number): Promise<Portafolio> {
  return apiFetch<Portafolio>(`/api/estudiantes/${id}/portafolio`, {
    auth: false,
  });
}
