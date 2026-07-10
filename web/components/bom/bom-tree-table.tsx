import { AlertTriangle, CheckCircle2 } from "lucide-react";
import { useTranslations } from "next-intl";

import type { BomExplosionResultDto } from "@/lib/hooks/use-bom";

export function BomTreeTable({ result }: { result: BomExplosionResultDto }) {
  const t = useTranslations("bom");
  const items = result.items ?? [];
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[760px] border-collapse text-left text-sm">
        <thead className="text-xs uppercase text-muted-foreground">
          <tr className="border-b border-border">
            <th scope="col" className="px-2 py-3 font-medium">{t("columns.path")}</th>
            <th scope="col" className="px-2 py-3 font-medium">{t("columns.material")}</th>
            <th scope="col" className="px-2 py-3 text-right font-medium">{t("columns.required")}</th>
            <th scope="col" className="px-2 py-3 text-right font-medium">{t("columns.available")}</th>
            <th scope="col" className="px-2 py-3 font-medium">{t("columns.coverage")}</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item, index) => {
            const shortage = item.shortageQuantity ?? 0;
            return (
              <tr key={`${item.materialId}-${index}`} className={`border-b border-border/70 last:border-0 ${shortage > 0 ? "bg-destructive/[0.035]" : ""}`}>
                <td className="max-w-96 px-2 py-3 font-mono text-xs text-muted-foreground">
                  <span className="inline-block" style={{ paddingLeft: `${Math.min(item.depth ?? 1, 10) * 8}px` }}>{(item.path ?? []).join(" / ")}</span>
                </td>
                <td className="px-2 py-3"><span className="font-medium text-foreground">{item.materialName}</span><span className="ml-2 font-mono text-xs text-muted-foreground">{item.materialCode}</span></td>
                <td className="px-2 py-3 text-right tabular-nums">{item.requiredQuantity} {item.unit}</td>
                <td className="px-2 py-3 text-right tabular-nums">{item.availableQuantity} {item.unit}</td>
                <td className="px-2 py-3">
                  {shortage > 0 ? (
                    <span className="inline-flex items-center gap-1 text-xs font-semibold text-destructive"><AlertTriangle className="h-4 w-4" aria-hidden="true" />{t("short", { quantity: shortage, unit: item.unit })}</span>
                  ) : (
                    <span className="inline-flex items-center gap-1 text-xs font-semibold text-primary"><CheckCircle2 className="h-4 w-4" aria-hidden="true" />{t("covered")}</span>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
