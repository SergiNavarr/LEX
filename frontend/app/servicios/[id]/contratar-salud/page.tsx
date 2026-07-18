"use client";

import { use, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  obtenerServicio,
  esSalud,
  formatPrecio,
  type ServicioDetalleResponse,
  type DetalleSalud,
} from "@/lib/servicios";
import { contratarSalud, firmarConsentimiento } from "@/lib/trabajos";
import {
  listarMisPacientes,
  type PacienteResponse,
} from "@/lib/pacientes";
import { ApiError } from "@/lib/api";
import { RequireRole } from "@/components/RequireRole";
import { SelectorSlots } from "@/components/SelectorSlots";
import { PacienteForm } from "@/components/PacienteForm";
import { ConsentimientoTexto } from "@/components/ConsentimientoTexto";
import { ErrorAlert } from "@/components/ui";

type SaludServicio = ServicioDetalleResponse & { detalle: DetalleSalud };

export default function ContratarSaludPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  return (
    <RequireRole roles={["Cliente"]} vista="Cliente">
      <ContratarSalud servicioId={Number(id)} />
    </RequireRole>
  );
}

function ContratarSalud({ servicioId }: { servicioId: number }) {
  const router = useRouter();
  const [servicio, setServicio] = useState<SaludServicio | null>(null);
  const [cargando, setCargando] = useState(true);
  const [cargaError, setCargaError] = useState<string | null>(null);

  const [pacientes, setPacientes] = useState<PacienteResponse[]>([]);
  const [pacienteId, setPacienteId] = useState<number>(0);
  const [formPaciente, setFormPaciente] = useState(false);

  const [slotsElegidos, setSlotsElegidos] = useState<string[]>([]);
  const [acepta, setAcepta] = useState(false);
  const [notas, setNotas] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const cargarPacientes = useCallback(async () => {
    try {
      setPacientes(await listarMisPacientes());
    } catch {
      // no bloquea: se puede agregar uno nuevo
    }
  }, []);

  useEffect(() => {
    let cancelado = false;
    obtenerServicio(servicioId)
      .then((s) => {
        if (cancelado) return;
        if (!esSalud(s)) {
          setCargaError("Este servicio no es de tipo Salud.");
          return;
        }
        setServicio(s);
      })
      .catch((err) => {
        if (!cancelado)
          setCargaError(
            err instanceof ApiError
              ? err.message
              : "No pudimos cargar el servicio.",
          );
      })
      .finally(() => {
        if (!cancelado) setCargando(false);
      });
    cargarPacientes();
    return () => {
      cancelado = true;
    };
  }, [servicioId, cargarPacientes]);

  if (cargando) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-10">
        <div className="h-40 animate-pulse rounded-xl bg-slate-100" />
      </div>
    );
  }

  if (cargaError || !servicio) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-16">
        <ErrorAlert message={cargaError ?? "Servicio no encontrado."} />
        <Link
          href="/"
          className="mt-6 inline-block text-sm font-semibold text-indigo-700 hover:underline"
        >
          ← Volver al catálogo
        </Link>
      </div>
    );
  }

  const paciente = pacientes.find((p) => p.id === pacienteId) ?? null;
  const listo = pacienteId > 0 && slotsElegidos.length === 1 && acepta;

  function onPacienteCreado(p: PacienteResponse) {
    setPacientes((prev) => [p, ...prev]);
    setPacienteId(p.id);
    setFormPaciente(false);
  }

  async function handleConfirmar() {
    if (!listo || !servicio) return;
    setEnviando(true);
    setError(null);
    try {
      const trabajo = await contratarSalud({
        servicioId: servicio.id,
        pacienteId,
        slotElegido: slotsElegidos[0],
        notasCliente: notas.trim() || undefined,
      });
      // El trabajo ya se creó; si la firma falla, el cliente puede firmar desde el detalle.
      try {
        await firmarConsentimiento(trabajo.id);
      } catch {
        // no bloquea el redirect
      }
      router.push(`/panel/trabajos/${trabajo.id}`);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "No pudimos confirmar la contratación.",
      );
      setEnviando(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-10">
      <Link
        href={`/servicios/${servicio.id}`}
        className="mb-4 inline-block text-sm text-slate-500 transition hover:text-indigo-700"
      >
        ← Volver al servicio
      </Link>

      <h1 className="text-2xl font-bold tracking-tight text-slate-900">
        Contratar servicio de salud
      </h1>

      <div className="mt-4 rounded-xl border border-slate-200 bg-white p-5">
        <h2 className="font-semibold text-slate-900">{servicio.titulo}</h2>
        <p className="mt-1 text-sm text-slate-500">
          por {servicio.estudianteNombre} · {servicio.detalle.catalogoServicioNombre}
        </p>
        <p className="mt-1 text-sm text-slate-500">
          Supervisa: {servicio.detalle.supervisorNombre} (Mat.{" "}
          {servicio.detalle.supervisorMatricula}) · {servicio.detalle.modalidad}{" "}
          · {servicio.detalle.duracionMinutosSesion} min
        </p>
        <p className="mt-2 text-lg font-bold text-slate-900">
          {formatPrecio(servicio.precio)}
        </p>
      </div>

      {/* Paso 1: paciente */}
      <section className="mt-6">
        <h3 className="text-sm font-semibold text-slate-900">
          1. Elegí el paciente
        </h3>
        <div className="mt-2 flex flex-col gap-2 sm:flex-row sm:items-center">
          <select
            value={pacienteId}
            onChange={(e) => setPacienteId(Number(e.target.value))}
            className="w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 sm:max-w-sm"
          >
            <option value={0}>Elegí un paciente…</option>
            {pacientes.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nombreCompleto} ({p.tipo})
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => setFormPaciente(true)}
            className="shrink-0 rounded-lg border border-slate-300 px-3 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            + Agregar nuevo paciente
          </button>
        </div>
      </section>

      {/* Paso 2: horario */}
      <section className="mt-6">
        <h3 className="mb-2 text-sm font-semibold text-slate-900">
          2. Elegí el horario
        </h3>
        <SelectorSlots
          estudianteId={servicio.estudianteId}
          duracionMinutos={servicio.detalle.duracionMinutosSesion}
          cantidadRequerida={1}
          slotsElegidos={slotsElegidos}
          onCambiarSeleccion={setSlotsElegidos}
        />
      </section>

      {/* Paso 3: consentimiento */}
      <section className="mt-6">
        <h3 className="mb-2 text-sm font-semibold text-slate-900">
          3. Consentimiento informado
        </h3>
        {paciente ? (
          <ConsentimientoTexto servicio={servicio} paciente={paciente} />
        ) : (
          <p className="rounded-lg border border-dashed border-slate-200 bg-slate-50/50 px-4 py-6 text-center text-sm text-slate-500">
            Elegí un paciente para ver el consentimiento.
          </p>
        )}
        <label className="mt-3 flex cursor-pointer items-start gap-2.5 text-sm text-slate-700">
          <input
            type="checkbox"
            className="mt-0.5 accent-indigo-600"
            checked={acepta}
            disabled={!paciente}
            onChange={(e) => setAcepta(e.target.checked)}
          />
          <span>
            Acepto el consentimiento informado y autorizo la realización de la
            práctica.
          </span>
        </label>
      </section>

      <div className="mt-6">
        <label
          htmlFor="notas"
          className="mb-1.5 block text-sm font-medium text-slate-700"
        >
          Notas para el estudiante (opcional)
        </label>
        <textarea
          id="notas"
          rows={3}
          maxLength={500}
          value={notas}
          onChange={(e) => setNotas(e.target.value)}
          className="w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
        />
      </div>

      {error && (
        <div className="mt-4">
          <ErrorAlert message={error} />
        </div>
      )}

      <button
        onClick={handleConfirmar}
        disabled={!listo || enviando}
        className="mt-6 w-full rounded-lg bg-indigo-600 py-3 font-medium text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {enviando ? "Confirmando…" : "Confirmar contratación"}
      </button>

      {formPaciente && (
        <PacienteForm
          onCerrar={() => setFormPaciente(false)}
          onExito={onPacienteCreado}
        />
      )}
    </div>
  );
}
