import { FileText, Search, Upload } from "lucide-react";

import {
  OperationsPageShell,
  OperationsPanel,
  StatusPill,
} from "@/components/operations/operations-page-shell";

const documents = [
  { name: "A102 Alarm Handling SOP", type: "SOP", chunks: "18", status: "Indexed" },
  { name: "Line 2 Maintenance Manual", type: "Manual", chunks: "42", status: "Indexed" },
  { name: "Quality Inspection Standard", type: "Standard", chunks: "24", status: "Review" },
];

export default function KnowledgePage() {
  return (
    <OperationsPageShell
      title="Knowledge Base"
      eyebrow="RAG Knowledge"
      description="Manage SOPs, maintenance manuals, quality standards, and semantic search readiness for agent answers."
      icon={Search}
      metrics={[
        { label: "Documents", value: "36" },
        { label: "Chunks", value: "1,284" },
        { label: "Indexed", value: "94%" },
      ]}
    >
      <section className="grid gap-4 xl:grid-cols-[360px_minmax(0,1fr)]">
        <OperationsPanel
          title="Document Intake"
          description="Upload process documents and prepare them for chunking and retrieval."
        >
          <button className="flex min-h-11 w-full items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-medium text-primary-foreground transition-[background-color,scale] duration-150 hover:bg-primary/90 active:scale-[0.96]">
            <Upload className="h-4 w-4" aria-hidden="true" />
            Upload document
          </button>
          <label className="relative mt-3 block">
            <span className="sr-only">Search knowledge base</span>
            <Search
              className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden="true"
            />
            <input
              aria-label="Search knowledge base"
              defaultValue="A102 alarm recovery"
              className="min-h-11 w-full rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm outline-none transition-[border-color,box-shadow] duration-150 focus:border-ring focus:ring-2 focus:ring-ring/20"
            />
          </label>
          <div className="mt-4 rounded-md bg-muted p-3 text-sm text-muted-foreground">
            Query preview will use vector search with source citations once the
            backend contract is connected.
          </div>
        </OperationsPanel>

        <OperationsPanel
          title="Document Library"
          description="Current RAG corpus and indexing state."
        >
          <div className="grid gap-3">
            {documents.map((document) => (
              <article
                key={document.name}
                className="flex flex-col gap-3 rounded-md bg-muted p-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="flex items-start gap-3">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-background text-primary">
                    <FileText className="h-4 w-4" aria-hidden="true" />
                  </div>
                  <div>
                    <h3 className="text-sm font-medium text-foreground">
                      {document.name}
                    </h3>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {document.type} · {document.chunks} chunks
                    </p>
                  </div>
                </div>
                <StatusPill tone={document.status === "Indexed" ? "good" : "warning"}>
                  {document.status}
                </StatusPill>
              </article>
            ))}
          </div>
        </OperationsPanel>
      </section>
    </OperationsPageShell>
  );
}
