"use client";

// Botón "Vincular cuenta de LinkedIn" — SOLO VISUAL, sin integración real.
// Es honesto: muestra la etiqueta "Próximamente" y, al hacer clic, un aviso
// claro de que la función todavía no está disponible. No falla en silencio.

import { useState } from "react";

function LinkedInIcon({ className = "" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M20.45 20.45h-3.56v-5.57c0-1.33-.02-3.04-1.85-3.04-1.85 0-2.13 1.45-2.13 2.94v5.67H9.35V9h3.41v1.56h.05c.48-.9 1.64-1.85 3.37-1.85 3.6 0 4.27 2.37 4.27 5.46v6.28zM5.34 7.43a2.06 2.06 0 110-4.13 2.06 2.06 0 010 4.13zM7.12 20.45H3.56V9h3.56v11.45zM22.22 0H1.77C.79 0 0 .77 0 1.73v20.54C0 23.23.79 24 1.77 24h20.45c.98 0 1.78-.77 1.78-1.73V1.73C24 .77 23.2 0 22.22 0z" />
    </svg>
  );
}

export function LinkedInButton({ className = "" }: { className?: string }) {
  const [avisado, setAvisado] = useState(false);

  return (
    <div className={className}>
      <button
        type="button"
        onClick={() => setAvisado(true)}
        aria-label="Vincular cuenta de LinkedIn (próximamente)"
        className="inline-flex items-center gap-2 rounded-lg bg-[#0A66C2] px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-[#004182] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#0A66C2]/40"
      >
        <LinkedInIcon className="h-4 w-4" />
        Vincular cuenta de LinkedIn
        <span className="rounded-full bg-white/20 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide">
          Próximamente
        </span>
      </button>

      {avisado && (
        <p className="mt-2 text-xs text-gray-500" role="status">
          La vinculación con LinkedIn estará disponible próximamente.
        </p>
      )}
    </div>
  );
}
