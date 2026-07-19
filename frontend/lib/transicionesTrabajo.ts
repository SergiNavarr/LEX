// State machine del trabajo, duplicada en el cliente para una UX mejor (los botones se
// derivan del estado + rol sin round-trip). El backend es la fuente de verdad y valida
// cada transición; esto solo decide qué ofrecer. Espeja README_ESTADOS_TRABAJO.md.

import type { EstadoTrabajo } from "./trabajos";
import type { TipoServicio } from "./servicios";

export type AccionTrabajo =
  | "aceptar"
  | "iniciar"
  | "entregar"
  | "completar"
  | "cancelar"
  | "disputar";

export type RolEnTrabajo = "Cliente" | "Estudiante";

interface AccionMeta {
  etiqueta: string;
  descripcion: string;
  variante: "primary" | "secondary" | "danger" | "warning";
  requiereConfirmacion: boolean;
  mensajeConfirmacion?: string;
  requiereMotivo?: boolean; // para disputar
}

export const ACCIONES_META: Record<AccionTrabajo, AccionMeta> = {
  aceptar: {
    etiqueta: "Aceptar trabajo",
    descripcion: "Confirmá que aceptás realizar este trabajo",
    variante: "primary",
    requiereConfirmacion: false,
  },
  iniciar: {
    etiqueta: "Iniciar trabajo",
    descripcion: "Marcar el trabajo como iniciado",
    variante: "primary",
    requiereConfirmacion: false,
  },
  entregar: {
    etiqueta: "Marcar como entregado",
    descripcion: "Notificar al cliente que el trabajo está listo",
    variante: "primary",
    requiereConfirmacion: true,
    mensajeConfirmacion:
      "¿Confirmás que el trabajo está entregado? El cliente deberá revisarlo antes de que se libere el pago.",
  },
  completar: {
    etiqueta: "Completar y liberar pago",
    descripcion: "Aprobar la entrega y liberar el pago al estudiante",
    variante: "primary",
    requiereConfirmacion: true,
    mensajeConfirmacion:
      "¿Confirmás la aprobación? Esto liberará el pago al estudiante.",
  },
  cancelar: {
    etiqueta: "Cancelar trabajo",
    descripcion: "Cancelar el trabajo",
    variante: "danger",
    requiereConfirmacion: true,
    mensajeConfirmacion:
      "¿Estás seguro que querés cancelar? Esta acción no se puede deshacer.",
  },
  disputar: {
    etiqueta: "Iniciar disputa",
    descripcion: "Reportar un conflicto que requiere resolución",
    variante: "warning",
    requiereConfirmacion: true,
    requiereMotivo: true,
    mensajeConfirmacion:
      "La disputa congelará el pago hasta que un admin resuelva. Describí brevemente el motivo.",
  },
};

// Matriz de transiciones válidas por estado y rol (común a las 3 verticales).
const TRANSICIONES: Record<EstadoTrabajo, Record<RolEnTrabajo, AccionTrabajo[]>> = {
  Pendiente: {
    Estudiante: ["aceptar", "cancelar"],
    Cliente: ["cancelar"],
  },
  Aceptado: {
    Estudiante: ["iniciar", "cancelar"],
    Cliente: ["cancelar"],
  },
  EnCurso: {
    Estudiante: ["entregar", "cancelar", "disputar"],
    Cliente: ["cancelar", "disputar"],
  },
  Entregado: {
    Estudiante: ["disputar"],
    Cliente: ["completar", "disputar"],
  },
  Disputa: {
    Estudiante: [],
    Cliente: [],
  },
  Completado: {
    Estudiante: [],
    Cliente: [],
  },
  Cancelado: {
    Estudiante: [],
    Cliente: [],
  },
};

export function accionesDisponibles(
  estado: EstadoTrabajo,
  rol: RolEnTrabajo,
  tipoTrabajo: TipoServicio,
): AccionTrabajo[] {
  const acciones = [...TRANSICIONES[estado][rol]];

  // Clase y Salud no se entregan ni completan a mano: el trabajo avanza (y se completa)
  // marcando las sesiones. Ambas quedan fuera de las acciones ofrecidas.
  if (tipoTrabajo === "Clase" || tipoTrabajo === "Salud") {
    return acciones.filter((a) => a !== "completar" && a !== "entregar");
  }

  return acciones;
}
