import { ShieldCheck } from "lucide-react";

import { ModulePlaceholder } from "@/components/module-placeholder";

export default function QualityPage() {
  return (
    <ModulePlaceholder
      title="Quality Trace"
      description="Batch traceability and defect-pattern workspace for quality teams."
      icon={ShieldCheck}
    />
  );
}
