import Link from "next/link";
import { formatPrecio, type Servicio } from "@/lib/servicios";
import { RatingBadge } from "@/components/Stars";
import { TipoBadge } from "@/components/TipoBadge";
import { ServiceCover } from "@/components/ServiceCover";

export function ServiceCard({ servicio }: { servicio: Servicio }) {
  return (
    <Link
      href={`/servicios/${servicio.idServicio}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm transition hover:-translate-y-0.5 hover:border-accent/30 hover:shadow-md"
    >
      {/* Portada: imagen del servicio o, si no hay / falla, el placeholder "LEX". */}
      <ServiceCover
        src={servicio.imagenUrl}
        alt={servicio.titulo}
        className="h-28 lg:h-36"
      />

      <div className="flex flex-1 flex-col p-4 lg:p-5">
        <div className="mb-2 flex items-center justify-between gap-2">
          <TipoBadge
            tipoServicioId={servicio.tipoServicioId}
            nombre={servicio.tipoServicioNombre}
          />
          <RatingBadge value={servicio.estudianteCalificacion} />
        </div>

        <h3 className="line-clamp-2 font-semibold text-foreground transition group-hover:text-accent lg:text-lg">
          {servicio.titulo}
        </h3>

        <p className="mt-1 text-sm text-gray-500">
          por <span className="font-medium">{servicio.estudianteNombre}</span>
        </p>

        <div className="mt-4 flex items-end justify-between border-t border-gray-100 pt-3">
          <span className="text-xs text-gray-500">
            {servicio.tiempoEntregaDias
              ? `Entrega en ${servicio.tiempoEntregaDias} día${
                  servicio.tiempoEntregaDias > 1 ? "s" : ""
                }`
              : "Tiempo a coordinar"}
          </span>
          <span className="text-lg font-bold text-foreground lg:text-xl">
            {formatPrecio(servicio.precio)}
          </span>
        </div>
      </div>
    </Link>
  );
}
