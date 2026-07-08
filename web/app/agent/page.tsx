import { Bot } from "lucide-react";

import { ModulePlaceholder } from "@/components/module-placeholder";

export default function AgentPage() {
  return (
    <ModulePlaceholder
      title="Agent Console"
      description="Chat workspace for production, quality, OEE, and knowledge-base tools."
      icon={Bot}
    />
  );
}
