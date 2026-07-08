import { Search } from "lucide-react";

import { ModulePlaceholder } from "@/components/module-placeholder";

export default function KnowledgePage() {
  return (
    <ModulePlaceholder
      title="Knowledge Base"
      description="SOP, maintenance manual, and process-document management entry point for RAG."
      icon={Search}
    />
  );
}
