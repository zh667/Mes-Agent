import { ClipboardList } from "lucide-react";

import { ModulePlaceholder } from "@/components/module-placeholder";

export default function WorkOrdersPage() {
  return (
    <ModulePlaceholder
      title="Work Orders"
      description="Operational view for dispatching, reporting, and completing shop-floor work orders."
      icon={ClipboardList}
    />
  );
}
