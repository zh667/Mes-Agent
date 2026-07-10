"use client";

import { Calculator, LoaderCircle } from "lucide-react";
import { useTranslations } from "next-intl";
import { type FormEvent, useState } from "react";

import { BomTreeTable } from "@/components/bom/bom-tree-table";
import { useBomExplosion, useBomProducts } from "@/lib/hooks/use-bom";

export function BomExplosionWorkspace() {
  const t = useTranslations("bom");
  const products = useBomProducts();
  const explosion = useBomExplosion();
  const [productId, setProductId] = useState("");
  const [quantity, setQuantity] = useState("1");

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsedProductId = Number(productId);
    const parsedQuantity = Number(quantity);
    if (parsedProductId > 0 && parsedQuantity > 0) {
      explosion.mutate({ productId: parsedProductId, quantity: parsedQuantity });
    }
  }

  const totals = explosion.data?.totalMaterials ?? [];
  const shortages = totals.filter((item) => (item.shortageQuantity ?? 0) > 0);

  return (
    <div className="space-y-5">
      <form onSubmit={submit} className="grid min-h-20 gap-3 border-b border-border pb-5 sm:grid-cols-[minmax(240px,1fr)_160px_auto] sm:items-end">
        <label className="text-xs font-medium text-muted-foreground">{t("product")}
          <select aria-label={t("product")} required value={productId} onChange={(event) => setProductId(event.target.value)} className="mt-1 block h-10 w-full rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring">
            <option value="">{t("selectBom")}</option>
            {(products.data ?? []).map((product) => <option key={product.productId} value={product.productId}>{product.code} - {product.name} ({t("version", { version: product.bomVersion })})</option>)}
          </select>
        </label>
        <label className="text-xs font-medium text-muted-foreground">{t("buildQuantity")}
          <input aria-label={t("buildQuantity")} type="number" min="0.0001" max="1000000" step="0.0001" required value={quantity} onChange={(event) => setQuantity(event.target.value)} className="mt-1 block h-10 w-full rounded-md bg-background px-3 text-sm tabular-nums text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring" />
        </label>
        <button type="submit" disabled={explosion.isPending || !productId} className="flex h-10 items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground transition-transform active:scale-[0.96] disabled:cursor-not-allowed disabled:opacity-50">
          {explosion.isPending ? <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" /> : <Calculator className="h-4 w-4" aria-hidden="true" />}
          {t("calculate")}
        </button>
      </form>

      {products.isLoading ? <p role="status" className="text-sm text-muted-foreground">{t("loading")}</p> : null}
      {explosion.isError ? <p role="alert" className="text-sm text-destructive">{t("error")}</p> : null}
      {explosion.data ? (
        <>
          <dl className="grid gap-2 sm:grid-cols-3">
            <Metric label={t("leafPaths")} value={String(explosion.data.items?.length ?? 0)} />
            <Metric label={t("uniqueMaterials")} value={String(totals.length)} />
            <Metric label={t("shortMaterials")} value={String(shortages.length)} alert={shortages.length > 0} />
          </dl>
          <BomTreeTable result={explosion.data} />
        </>
      ) : !products.isLoading ? (
        <div className="flex min-h-64 items-center justify-center border-y border-dashed border-border text-center text-sm text-muted-foreground">{t("empty")}</div>
      ) : null}
    </div>
  );
}

function Metric({ label, value, alert = false }: { label: string; value: string; alert?: boolean }) {
  return <div className="rounded-md bg-muted px-3 py-2"><dt className="text-xs text-muted-foreground">{label}</dt><dd className={`mt-1 text-lg font-semibold tabular-nums ${alert ? "text-destructive" : "text-foreground"}`}>{value}</dd></div>;
}
