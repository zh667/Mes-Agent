import { Gauge } from "lucide-react";

import { ModulePlaceholder } from "@/components/module-placeholder";

export default function EquipmentPage() {
  return (
    <ModulePlaceholder
      title="Equipment OEE"
      description="Realtime equipment status, downtime, alarm, and OEE dashboard entry point."
      icon={Gauge}
    />
  );
}
