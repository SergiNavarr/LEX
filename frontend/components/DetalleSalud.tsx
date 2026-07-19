"use client";

import { useState } from "react";
import Link from "next/link";
import {
  firmarConsentimiento,
  type DetalleTrabajoSalud,
  type TrabajoResponse,
} from "@/lib/trabajos";
import type { RolEnTrabajo } from "@/lib/transicionesTrabajo";
import { ApiError } from "@/lib/api";
import { formatFecha } from "@/lib/servicios";
import { ErrorAlert } from "@/components/ui";

export function DetalleSalud({
  trabajo,
  detalle,
  rolUsuario,
  onCambio,
}: {
  trabajo: TrabajoResponse;
  detalle: DetalleTrabajoSalud;
  rolUsuario: RolEnTrabajo;
  onCambio: () => void;
}) {
  const [modal, setModal] = useState(false);
  const [acepta, setAcepta] = useState(false);
  const [firmando, setFirmando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const pendienteFirma = !detalle.consentimientoFirmado;

  async function firmar() {
    setFirmando(true);
    setError(null);
    try {
      await firmarConsentimiento(trabajo.id);
      setModal(false);
      onCambio();
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "No pudimos firmar el consentimiento.",
      );
    } finally {
      setFirmando(false);
    }
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5">
      <div className="flex items-center gap-2">
        <span className="rounded-full bg-rose-100 px-2.5 py-0.5 text-xs font-semibold text-rose-700">
          Salud
        </span>
        <h2 className="text-sm font-semibold text-slate-900">
          Detalles de la práctica
        </h2>
      </div>

      <dl className="mt-3 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Dato label="Práctica" valor={detalle.catalogoServicioNombreSnapshot} />
        <Dato label="Paciente" valor={detalle.pacienteNombre} />
        <Dato
          label="Supervisor"
          valor={`${detalle.supervisorNombreSnapshot} (Mat. ${detalle.supervisorMatriculaSnapshot})`}
        />
        <Dato label="Modalidad" valor={detalle.modalidadSaludSnapshot} />
      </dl>

      {/* Consentimiento */}
      <div className="mt-4 border-t border-slate-100 pt-4">
        {detalle.consentimientoFirmado && detalle.consentimiento ? (
          <div className="rounded-lg border border-emerald-200 bg-emerald-50/60 px-4 py-3 text-sm text-emerald-800">
            ✓ Consentimiento firmado el{" "}
            {formatFecha(detalle.consentimiento.fechaAceptacion)}.
          </div>
        ) : pendienteFirma && rolUsuario === "Cliente" ? (
          <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
            <p className="text-sm font-medium text-amber-800">
              Falta firmar el consentimiento informado.
            </p>
            <p className="mt-1 text-xs text-amber-700">
              El estudiante no puede iniciar la práctica hasta que lo firmes.
            </p>
            <button
              onClick={() => setModal(true)}
              className="mt-3 rounded-lg bg-amber-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-amber-700"
            >
              Firmar ahora
            </button>
          </div>
        ) : (
          <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            Consentimiento pendiente de firma por el cliente.
          </div>
        )}
      </div>

      {modal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onClick={() => setModal(false)}
        >
          <div
            className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-6 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-bold text-slate-900">
              Consentimiento informado
            </h3>
            {error && (
              <div className="mt-3">
                <ErrorAlert message={error} />
              </div>
            )}
            <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-4 text-xs leading-relaxed text-slate-600">
              <p className="font-semibold text-slate-800">
                CONSENTIMIENTO INFORMADO PARA PRÁCTICA DE SALUD SUPERVISADA
              </p>
              <p className="mt-2">
                El/la firmante autoriza la práctica de{" "}
                <span className="font-medium">
                  {detalle.catalogoServicioNombreSnapshot}
                </span>{" "}
                sobre el paciente{" "}
                <span className="font-medium">{detalle.pacienteNombre}</span>,
                realizada por el/la estudiante{" "}
                <span className="font-medium">{trabajo.estudianteNombre}</span>{" "}
                bajo la supervisión del profesional{" "}
                <span className="font-medium">
                  {detalle.supervisorNombreSnapshot}
                </span>{" "}
                (Matrícula N° {detalle.supervisorMatriculaSnapshot}). Comprende
                que se trata de una práctica estudiantil supervisada y acepta sus
                condiciones. Quedará registrado en LEX con la fecha de la firma.
              </p>
            </div>
            <label className="mt-3 flex cursor-pointer items-start gap-2.5 text-sm text-slate-700">
              <input
                type="checkbox"
                className="mt-0.5 accent-indigo-600"
                checked={acepta}
                onChange={(e) => setAcepta(e.target.checked)}
              />
              <span>Acepto el consentimiento y autorizo la práctica.</span>
            </label>
            <div className="mt-4 flex gap-3">
              <button
                onClick={() => setModal(false)}
                className="flex-1 rounded-lg border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
              >
                Cancelar
              </button>
              <button
                onClick={firmar}
                disabled={!acepta || firmando}
                className="flex-1 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {firmando ? "Firmando…" : "Firmar consentimiento"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Link al paciente para el cliente (gestiona sus pacientes) */}
      {rolUsuario === "Cliente" && (
        <p className="mt-3 text-xs text-slate-400">
          <Link href="/panel/pacientes" className="hover:underline">
            Gestionar mis pacientes
          </Link>
        </p>
      )}
    </div>
  );
}

function Dato({ label, valor }: { label: string; valor: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{valor}</dd>
    </div>
  );
}
