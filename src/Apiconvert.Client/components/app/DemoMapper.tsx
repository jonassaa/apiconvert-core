"use client";

import { useMemo, useState } from "react";
import { applyMapping, type MappingConfig } from "@/lib/mapping/engine";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";

type MappingExample = {
  id: string;
  label: string;
  input: Record<string, unknown>;
  mapping: MappingConfig;
};

const examples: MappingExample[] = [
  {
    id: "rename",
    label: "Rename + nesting",
    input: { first_name: "Ada", last_name: "Lovelace" },
    mapping: {
      rows: [
        { outputPath: "user.name.first", sourceType: "path", sourceValue: "first_name" },
        { outputPath: "user.name.last", sourceType: "path", sourceValue: "last_name" },
      ],
    },
  },
  {
    id: "constants",
    label: "Constants + defaults",
    input: { ticketId: "123" },
    mapping: {
      rows: [
        { outputPath: "ticket.id", sourceType: "path", sourceValue: "ticketId" },
        { outputPath: "source", sourceType: "constant", sourceValue: "zendesk" },
        { outputPath: "ticket.priority", sourceType: "path", sourceValue: "priority", defaultValue: "normal" },
      ],
    },
  },
  {
    id: "transforms",
    label: "Transforms",
    input: { email: "USER@EXAMPLE.COM", amount: "42", first: "Ada", last: "Lovelace" },
    mapping: {
      rows: [
        { outputPath: "user.email", sourceType: "transform", sourceValue: "email", transformType: "toLowerCase" },
        { outputPath: "order.amount", sourceType: "transform", sourceValue: "amount", transformType: "number" },
        { outputPath: "user.fullName", sourceType: "transform", sourceValue: "first,const: ,last", transformType: "concat" },
      ],
    },
  },
];

export function DemoMapper() {
  const [selectedId, setSelectedId] = useState(examples[0].id);
  const example = useMemo(
    () => examples.find((item) => item.id === selectedId) ?? examples[0],
    [selectedId]
  );
  const [inputJson, setInputJson] = useState(
    JSON.stringify(example.input, null, 2)
  );

  const mapping = example.mapping;

  let output = "{}";
  try {
    const parsed = JSON.parse(inputJson);
    output = JSON.stringify(applyMapping(parsed, mapping).output, null, 2);
  } catch {
    output = "Invalid JSON";
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        {examples.map((item) => (
          <Button
            key={item.id}
            size="sm"
            variant={item.id === selectedId ? "default" : "secondary"}
            onClick={() => {
              setSelectedId(item.id);
              setInputJson(JSON.stringify(item.input, null, 2));
            }}
          >
            {item.label}
          </Button>
        ))}
      </div>
      <div className="grid gap-4 md:grid-cols-2 md:items-stretch">
        <div className="rounded-2xl border border-border/70 bg-white/80 p-3 shadow-[0_12px_25px_-20px_rgba(15,15,15,0.5)] dark:bg-zinc-900/70">
          <p className="mb-2 text-[11px] font-semibold uppercase tracking-[0.3em] text-muted-foreground">
            Incoming payload
          </p>
          <Textarea
            rows={9}
            value={inputJson}
            readOnly
            className="resize-none border-border/60 bg-white/90 font-mono text-xs dark:bg-zinc-950/40"
          />
        </div>
        <div className="rounded-2xl border border-border/70 bg-white/80 p-3 shadow-[0_12px_25px_-20px_rgba(15,15,15,0.5)] dark:bg-zinc-900/70">
          <p className="mb-2 text-[11px] font-semibold uppercase tracking-[0.3em] text-muted-foreground">
            Clean request
          </p>
          <Textarea
            rows={9}
            value={output}
            readOnly
            className="resize-none border-border/60 bg-white/90 font-mono text-xs dark:bg-zinc-950/40"
          />
        </div>
      </div>
    </div>
  );
}
