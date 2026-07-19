import { ESTADO_META, type EstadoTrabajo } from "@/lib/trabajos";

export default function EstadoBadge({
  estado,
  size = "md",
}: {
  estado: EstadoTrabajo;
  size?: "sm" | "md";
}) {
  const meta = ESTADO_META[estado];
  const sizeClasses =
    size === "sm" ? "text-xs px-2 py-0.5" : "text-sm px-3 py-1";
  return (
    <span
      className={`inline-flex items-center rounded-full font-medium ${meta.clases} ${sizeClasses}`}
    >
      {meta.etiqueta}
    </span>
  );
}
