"use client";

import { useState } from "react";

/**
 * Portada de un servicio con fallback elegante.
 *
 * Render base: el bloque de gradiente con el texto "LEX" (el placeholder de
 * siempre). Si hay `src` y la imagen carga, se superpone con object-cover; si
 * `src` es null o la imagen falla al cargar (onError), se queda el placeholder.
 * Así una tarjeta nunca queda rota.
 *
 * Usamos <img> en vez de next/image a propósito (ver nota en el PR): las
 * imágenes son URLs externas de demo y queremos que el fallback dependa solo
 * del navegador, sin pasar por el optimizador de Next.
 */
export function ServiceCover({
  src,
  alt,
  className = "",
}: {
  src: string | null;
  alt: string;
  className?: string;
}) {
  const [error, setError] = useState(false);
  const mostrarImagen = Boolean(src) && !error;

  return (
    <div
      className={`relative flex items-center justify-center overflow-hidden bg-gradient-to-br from-accent-soft to-white ${className}`}
    >
      {mostrarImagen && (
        <img
          src={src!}
          alt={alt}
          loading="lazy"
          onError={() => setError(true)}
          className="absolute inset-0 h-full w-full object-cover"
        />
      )}
      {!mostrarImagen && (
        <span className="text-3xl font-extrabold tracking-tight text-accent/30 lg:text-4xl">
          LEX
        </span>
      )}
    </div>
  );
}
