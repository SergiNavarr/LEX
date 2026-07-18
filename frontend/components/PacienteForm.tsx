"use client";

import { useState, type FormEvent } from "react";
import {
  crearPaciente,
  type PacienteResponse,
  type TipoPaciente,
} from "@/lib/pacientes";
import { ApiError } from "@/lib/api";
import { Field, Input } from "@/components/ui";

// Solo alta: el backend no expone edición ni borrado de pacientes (POST/GET únicamente).
export function PacienteForm({
  onCerrar,
  onExito,
}: {
  onCerrar: () => void;
  onExito: (p: PacienteResponse) => void;
}) {
  const [tipo, setTipo] = useState<TipoPaciente>("Humano");
  const [nombreCompleto, setNombreCompleto] = useState("");
  const [fechaNacimiento, setFechaNacimiento] = useState("");
  const [notas, setNotas] = useState("");

  // Humano
  const [esTitular, setEsTitular] = useState(false);
  const [dni, setDni] = useState("");
  const [contactoNombre, setContactoNombre] = useState("");
  const [contactoTelefono, setContactoTelefono] = useState("");

  // Animal
  const [especie, setEspecie] = useState("");
  const [raza, setRaza] = useState("");

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // La fecha del input date ("YYYY-MM-DD") se manda como ISO UTC para que Npgsql la
  // acepte en la columna timestamptz.
  function fechaIso(): string | undefined {
    return fechaNacimiento ? `${fechaNacimiento}T00:00:00Z` : undefined;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!nombreCompleto.trim()) return setError("Poné el nombre completo.");
    if (tipo === "Animal" && !especie.trim())
      return setError("Indicá la especie del animal.");

    setSubmitting(true);
    try {
      const nuevo =
        tipo === "Humano"
          ? await crearPaciente({
              tipo: "Humano",
              nombreCompleto: nombreCompleto.trim(),
              esTitular,
              fechaNacimiento: fechaIso(),
              dni: dni.trim() || undefined,
              contactoEmergenciaNombre: contactoNombre.trim() || undefined,
              contactoEmergenciaTelefono: contactoTelefono.trim() || undefined,
              notasRelevantes: notas.trim() || undefined,
            })
          : await crearPaciente({
              tipo: "Animal",
              nombreCompleto: nombreCompleto.trim(),
              esTitular: false,
              fechaNacimiento: fechaIso(),
              especie: especie.trim(),
              raza: raza.trim() || undefined,
              notasRelevantes: notas.trim() || undefined,
            });
      onExito(nuevo);
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "No pudimos guardar el paciente.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onCerrar}
    >
      <div
        className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-6 shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between">
          <h2 className="text-lg font-bold text-slate-900">Agregar paciente</h2>
          <button
            onClick={onCerrar}
            aria-label="Cerrar"
            className="rounded-lg p-1 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-5 space-y-4">
          {error && (
            <div className="rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-2.5 text-sm text-rose-700">
              {error}
            </div>
          )}

          {/* Toggle tipo */}
          <div className="grid grid-cols-2 gap-2 rounded-xl border border-slate-200 bg-slate-50 p-1">
            {(["Humano", "Animal"] as TipoPaciente[]).map((t) => (
              <button
                key={t}
                type="button"
                onClick={() => setTipo(t)}
                className={`rounded-lg px-3 py-2 text-sm font-semibold transition ${
                  tipo === t
                    ? "bg-white text-indigo-700 shadow-sm"
                    : "text-slate-500 hover:text-slate-700"
                }`}
              >
                {t === "Humano" ? "🧑 Humano" : "🐾 Animal"}
              </button>
            ))}
          </div>

          <Field label="Nombre completo" htmlFor="nombre">
            <Input
              id="nombre"
              required
              maxLength={150}
              value={nombreCompleto}
              onChange={(e) => setNombreCompleto(e.target.value)}
              placeholder={tipo === "Animal" ? "Ej: Firulais" : "Ej: Lucas Paz"}
            />
          </Field>

          {tipo === "Humano" ? (
            <>
              <label className="flex cursor-pointer items-center gap-2.5 text-sm text-slate-700">
                <input
                  type="checkbox"
                  className="accent-indigo-600"
                  checked={esTitular}
                  onChange={(e) => setEsTitular(e.target.checked)}
                />
                <span>Es titular (el paciente sos vos)</span>
              </label>
              <div className="grid grid-cols-2 gap-4">
                <Field label="DNI (opcional)" htmlFor="dni">
                  <Input
                    id="dni"
                    inputMode="numeric"
                    maxLength={20}
                    value={dni}
                    onChange={(e) => setDni(e.target.value)}
                    placeholder="30123456"
                  />
                </Field>
                <Field label="Fecha de nacimiento (opcional)" htmlFor="fnac">
                  <Input
                    id="fnac"
                    type="date"
                    value={fechaNacimiento}
                    onChange={(e) => setFechaNacimiento(e.target.value)}
                  />
                </Field>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <Field label="Contacto emergencia (opcional)" htmlFor="cnom">
                  <Input
                    id="cnom"
                    maxLength={150}
                    value={contactoNombre}
                    onChange={(e) => setContactoNombre(e.target.value)}
                    placeholder="Nombre"
                  />
                </Field>
                <Field label="Teléfono emergencia (opcional)" htmlFor="ctel">
                  <Input
                    id="ctel"
                    inputMode="tel"
                    maxLength={40}
                    value={contactoTelefono}
                    onChange={(e) => setContactoTelefono(e.target.value)}
                    placeholder="+54 9 …"
                  />
                </Field>
              </div>
            </>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-4">
                <Field label="Especie" htmlFor="especie">
                  <Input
                    id="especie"
                    required
                    maxLength={60}
                    value={especie}
                    onChange={(e) => setEspecie(e.target.value)}
                    placeholder="Perro, Gato, Ave…"
                  />
                </Field>
                <Field label="Raza (opcional)" htmlFor="raza">
                  <Input
                    id="raza"
                    maxLength={60}
                    value={raza}
                    onChange={(e) => setRaza(e.target.value)}
                    placeholder="Labrador…"
                  />
                </Field>
              </div>
              <Field label="Fecha de nacimiento (opcional)" htmlFor="fnac-a">
                <Input
                  id="fnac-a"
                  type="date"
                  value={fechaNacimiento}
                  onChange={(e) => setFechaNacimiento(e.target.value)}
                />
              </Field>
            </>
          )}

          <Field label="Notas relevantes (opcional)" htmlFor="notas">
            <textarea
              id="notas"
              rows={3}
              maxLength={2000}
              value={notas}
              onChange={(e) => setNotas(e.target.value)}
              placeholder="Información médica relevante…"
              className="w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-900 shadow-sm outline-none transition placeholder:text-slate-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
            />
          </Field>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onCerrar}
              className="flex-1 rounded-lg border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {submitting ? "Guardando…" : "Agregar"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
