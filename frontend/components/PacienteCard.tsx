import type { PacienteResponse } from "@/lib/pacientes";

function fechaCorta(iso: string | null): string | null {
  if (!iso) return null;
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return null;
  // timeZone UTC para no correr la fecha por el offset del navegador.
  return d.toLocaleDateString("es-AR", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  });
}

export function PacienteCard({ paciente }: { paciente: PacienteResponse }) {
  const esAnimal = paciente.tipo === "Animal";
  const nacimiento = fechaCorta(paciente.fechaNacimiento);

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4">
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2">
          <span className="text-xl" aria-hidden="true">
            {esAnimal ? "🐾" : "🧑"}
          </span>
          <div>
            <p className="font-semibold text-slate-900">
              {paciente.nombreCompleto}
            </p>
            <p className="text-xs text-slate-400">{paciente.tipo}</p>
          </div>
        </div>
        {paciente.tipo === "Humano" && paciente.esTitular && (
          <span className="rounded-full bg-indigo-100 px-2 py-0.5 text-xs font-semibold text-indigo-700">
            Titular
          </span>
        )}
      </div>

      <dl className="mt-3 space-y-1 text-sm">
        {esAnimal ? (
          <>
            <Dato label="Especie" valor={paciente.especie} />
            <Dato label="Raza" valor={paciente.raza} />
          </>
        ) : (
          <>
            <Dato label="DNI" valor={paciente.dni} />
            <Dato
              label="Contacto de emergencia"
              valor={
                paciente.contactoEmergenciaNombre
                  ? `${paciente.contactoEmergenciaNombre}${paciente.contactoEmergenciaTelefono ? ` · ${paciente.contactoEmergenciaTelefono}` : ""}`
                  : null
              }
            />
          </>
        )}
        <Dato label="Nacimiento" valor={nacimiento} />
      </dl>

      {paciente.notasRelevantes && (
        <p className="mt-2 border-t border-slate-100 pt-2 text-sm text-slate-600">
          {paciente.notasRelevantes}
        </p>
      )}
    </div>
  );
}

function Dato({ label, valor }: { label: string; valor: string | null }) {
  if (!valor) return null;
  return (
    <div className="flex justify-between gap-2">
      <dt className="text-slate-500">{label}</dt>
      <dd className="font-medium text-slate-800">{valor}</dd>
    </div>
  );
}
