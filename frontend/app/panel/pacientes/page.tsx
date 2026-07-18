"use client";

import { useCallback, useEffect, useState } from "react";
import {
  listarMisPacientes,
  type PacienteResponse,
} from "@/lib/pacientes";
import { ApiError } from "@/lib/api";
import { RequireRole } from "@/components/RequireRole";
import { PacienteCard } from "@/components/PacienteCard";
import { PacienteForm } from "@/components/PacienteForm";
import { ErrorAlert } from "@/components/ui";

export default function MisPacientesPage() {
  return (
    <RequireRole roles={["Cliente"]} vista="Cliente">
      <MisPacientes />
    </RequireRole>
  );
}

function MisPacientes() {
  const [pacientes, setPacientes] = useState<PacienteResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formAbierto, setFormAbierto] = useState(false);

  const cargar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setPacientes(await listarMisPacientes());
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos cargar tus pacientes.",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    cargar();
  }, [cargar]);

  function onExito(nuevo: PacienteResponse) {
    setPacientes((prev) => [nuevo, ...prev]);
    setFormAbierto(false);
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">
            Mis pacientes
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Las personas o animales a tu cargo para los que podés contratar
            servicios de salud.
          </p>
        </div>
        <button
          onClick={() => setFormAbierto(true)}
          className="shrink-0 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700"
        >
          Agregar paciente
        </button>
      </div>

      {error && (
        <div className="mt-6">
          <ErrorAlert message={error} />
        </div>
      )}

      <div className="mt-8">
        {loading ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div
                key={i}
                className="h-32 animate-pulse rounded-xl border border-slate-200 bg-slate-50"
              />
            ))}
          </div>
        ) : pacientes.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 py-16 text-center">
            <p className="font-semibold text-slate-900">
              Todavía no registraste pacientes
            </p>
            <p className="mt-1 text-sm text-slate-500">
              Agregá uno para poder contratar servicios de salud.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {pacientes.map((p) => (
              <PacienteCard key={p.id} paciente={p} />
            ))}
          </div>
        )}
      </div>

      {formAbierto && (
        <PacienteForm
          onCerrar={() => setFormAbierto(false)}
          onExito={onExito}
        />
      )}
    </div>
  );
}
