"use client";

import { Cable, CircleStop, FlaskConical, LoaderCircle, Pencil, Play, Plus, Save, Trash2, X } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { type FormEvent, useMemo, useState } from "react";

import { StatusPill } from "@/components/operations/operations-page-shell";
import {
  type DeviceConnectionDto,
  type DeviceConnectionRequest,
  type DeviceProtocol,
  useDeleteDeviceConnection,
  useDeviceConnectionCommand,
  useDeviceConnections,
  useDeviceEquipment,
  useSaveDeviceConnection,
} from "@/lib/hooks/use-device-connections";

const protocolNames: Record<DeviceProtocol, string> = { 0: "MQTT", 1: "OPC UA", 2: "Modbus TCP" };

type FormState = DeviceConnectionRequest & { id?: number };

const emptyForm: FormState = {
  name: "",
  equipmentId: 0,
  protocol: 0,
  host: "",
  port: 1883,
  endpoint: "",
  username: "",
  password: "",
  certificateThumbprint: "",
  unitId: 1,
  registerAddress: 0,
  registerCount: 1,
  pollIntervalMilliseconds: 1000,
  useTls: false,
  allowInsecure: false,
};

export function DeviceConnectionsWorkspace() {
  const t = useTranslations("devices");
  const common = useTranslations("common");
  const locale = useLocale();
  const connections = useDeviceConnections();
  const equipment = useDeviceEquipment();
  const save = useSaveDeviceConnection();
  const remove = useDeleteDeviceConnection();
  const test = useDeviceConnectionCommand("test");
  const start = useDeviceConnectionCommand("start");
  const stop = useDeviceConnectionCommand("stop");
  const [form, setForm] = useState<FormState>(emptyForm);
  const [feedback, setFeedback] = useState<string | null>(null);

  const selected = useMemo(
    () => connections.data?.find((item) => item.id === form.id),
    [connections.data, form.id],
  );

  function edit(item: DeviceConnectionDto) {
    if (!item.id) return;
    setFeedback(null);
    setForm({
      id: item.id,
      name: item.name ?? "",
      equipmentId: item.equipmentId ?? 0,
      protocol: item.protocol ?? 0,
      host: item.host ?? "",
      port: item.port ?? defaultPort(item.protocol ?? 0),
      endpoint: item.endpoint ?? "",
      username: "",
      password: "",
      certificateThumbprint: "",
      unitId: 1,
      registerAddress: 0,
      registerCount: 1,
      pollIntervalMilliseconds: 1000,
      useTls: item.useTls,
      allowInsecure: item.allowInsecure,
    });
  }

  function reset() {
    setForm(emptyForm);
    setFeedback(null);
    save.reset();
  }

  function changeProtocol(protocol: DeviceProtocol) {
    setForm((current) => ({
      ...current,
      protocol,
      port: defaultPort(protocol),
      endpoint: "",
      useTls: false,
      allowInsecure: false,
    }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const { id, ...request } = form;
    save.mutate(
      id ? { id, request } : { request },
      { onSuccess: () => { setFeedback(id ? t("updated") : t("created")); reset(); } },
    );
  }

  function runCommand(command: "test" | "start" | "stop", item: DeviceConnectionDto) {
    if (!item.id) return;
    setFeedback(null);
    const mutation = command === "test" ? test : command === "start" ? start : stop;
    mutation.mutate(item.id, {
      onSuccess: () => setFeedback(command === "test"
        ? t("testCompleted", { name: item.name })
        : t("commandCompleted", { name: item.name, command: t(`commands.${command}`) })),
      onError: () => setFeedback(t("commandFailed", { name: item.name, command: t(`commands.${command}`) })),
    });
  }

  const commandPending = test.isPending || start.isPending || stop.isPending;

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
      <div className="min-w-0">
        {feedback ? <p role="status" className="mb-3 border-l-2 border-primary bg-primary/5 px-3 py-2 text-sm text-foreground">{feedback}</p> : null}
        {connections.isLoading ? (
          <p role="status" className="flex min-h-52 items-center justify-center gap-2 text-sm text-muted-foreground"><LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />{t("loading")}</p>
        ) : connections.isError ? (
          <p role="alert" className="py-12 text-center text-sm text-destructive">{t("loadError")}</p>
        ) : connections.data?.length ? (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[820px] border-collapse text-left text-sm">
              <thead className="text-xs uppercase text-muted-foreground"><tr className="border-b border-border">
                <th scope="col" className="px-2 py-3 font-medium">{t("columns.connection")}</th><th scope="col" className="px-2 py-3 font-medium">{t("columns.protocol")}</th><th scope="col" className="px-2 py-3 font-medium">{t("columns.equipment")}</th><th scope="col" className="px-2 py-3 font-medium">{t("columns.endpoint")}</th><th scope="col" className="px-2 py-3 font-medium">{t("columns.state")}</th><th scope="col" className="px-2 py-3 font-medium">{t("columns.lastContact")}</th><th scope="col" className="px-2 py-3 text-right font-medium">{t("columns.actions")}</th>
              </tr></thead>
              <tbody>{connections.data.map((item) => (
                <tr key={item.id} className="border-b border-border/70 last:border-0">
                  <td className="px-2 py-3"><p className="font-medium text-foreground">{item.name}</p><p className="mt-1 text-xs text-muted-foreground">{item.hasCredentials ? t("credentialsStored") : t("noCredentials")}</p></td>
                  <td className="px-2 py-3"><StatusPill tone="neutral">{protocolNames[item.protocol ?? 0]}</StatusPill></td>
                  <td className="px-2 py-3 font-mono text-xs">{item.equipmentCode}</td>
                  <td className="max-w-64 px-2 py-3"><p className="truncate font-mono text-xs">{item.host}:{item.port}</p><p className="mt-1 truncate text-xs text-muted-foreground">{item.endpoint}</p></td>
                  <td className="px-2 py-3"><StatusPill tone={item.lastErrorCode ? "danger" : item.isEnabled ? "good" : "neutral"}>{item.lastErrorCode ?? (item.isEnabled ? t("running") : t("stopped"))}</StatusPill></td>
                  <td className="whitespace-nowrap px-2 py-3 text-xs tabular-nums text-muted-foreground">{formatTimestamp(item.lastConnectedAt, locale, common("never"))}</td>
                  <td className="px-2 py-3"><div className="flex justify-end gap-1">
                    <IconButton label={t("editAction", { name: item.name })} onClick={() => edit(item)} icon={Pencil} />
                    <IconButton label={t("testAction", { name: item.name })} disabled={commandPending} onClick={() => runCommand("test", item)} icon={FlaskConical} />
                    {item.isEnabled ? <IconButton label={t("stopAction", { name: item.name })} disabled={commandPending} onClick={() => runCommand("stop", item)} icon={CircleStop} /> : <IconButton label={t("startAction", { name: item.name })} disabled={commandPending} onClick={() => runCommand("start", item)} icon={Play} />}
                    <IconButton label={t("deleteAction", { name: item.name })} disabled={remove.isPending} onClick={() => item.id && remove.mutate(item.id)} icon={Trash2} danger />
                  </div></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        ) : <p className="flex min-h-52 flex-col items-center justify-center gap-2 text-sm text-muted-foreground"><Cable className="h-5 w-5" aria-hidden="true" />{t("empty")}</p>}
      </div>

      <form onSubmit={submit} className="border-t border-border pt-4 xl:border-l xl:border-t-0 xl:pl-5 xl:pt-0">
        <div className="flex items-center justify-between"><div><h3 className="text-sm font-semibold text-foreground">{form.id ? t("editConnection") : t("newConnection")}</h3><p className="mt-1 text-xs text-muted-foreground">{t("settingsDescription")}</p></div>{form.id ? <IconButton label={t("cancelEditing")} onClick={reset} icon={X} /> : null}</div>
        <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-1">
          <TextField label={t("fields.name")} value={form.name} required onChange={(name) => setForm({ ...form, name })} />
          <SelectField label={t("fields.equipment")} value={String(form.equipmentId ?? 0)} onChange={(value) => setForm({ ...form, equipmentId: Number(value) })} options={[{ value: "0", label: t("selectEquipment") }, ...(equipment.data ?? []).map((item) => ({ value: String(item.id), label: `${item.code} - ${item.name}` }))]} />
          <SelectField label={t("fields.protocol")} value={String(form.protocol ?? 0)} onChange={(value) => changeProtocol(Number(value) as DeviceProtocol)} options={[{ value: "0", label: "MQTT" }, { value: "1", label: "OPC UA" }, { value: "2", label: "Modbus TCP" }]} />
          <div className="grid grid-cols-[minmax(0,1fr)_96px] gap-2"><TextField label={t("fields.host")} value={form.host} required onChange={(host) => setForm({ ...form, host })} /><NumberField label={t("fields.port")} value={form.port ?? 0} min={1} max={65535} onChange={(port) => setForm({ ...form, port })} /></div>
          <TextField label={form.protocol === 0 ? t("fields.topic") : form.protocol === 1 ? t("fields.nodeId") : t("fields.registerLabel")} value={form.endpoint} required placeholder={endpointPlaceholder(form.protocol ?? 0)} onChange={(endpoint) => setForm({ ...form, endpoint })} />
          {form.protocol === 2 ? <div className="grid grid-cols-3 gap-2"><NumberField label={t("fields.unitId")} value={form.unitId ?? 1} min={1} max={247} onChange={(unitId) => setForm({ ...form, unitId })} /><NumberField label={t("fields.register")} value={form.registerAddress ?? 0} min={0} max={65535} onChange={(registerAddress) => setForm({ ...form, registerAddress })} /><NumberField label={t("fields.count")} value={form.registerCount ?? 1} min={1} max={125} onChange={(registerCount) => setForm({ ...form, registerCount })} /></div> : null}
          {form.protocol === 0 ? <CheckField label={t("fields.useTls")} checked={form.useTls} onChange={(useTls) => setForm({ ...form, useTls })} /> : null}
          {form.protocol === 1 ? <><TextField label={t("fields.thumbprint")} value={form.certificateThumbprint ?? ""} placeholder={t("thumbprintPlaceholder")} onChange={(certificateThumbprint) => setForm({ ...form, certificateThumbprint })} /><CheckField label={t("fields.allowInsecure")} checked={form.allowInsecure} onChange={(allowInsecure) => setForm({ ...form, allowInsecure })} /></> : null}
          {form.protocol === 0 ? <TextField label={t("fields.username")} value={form.username ?? ""} autoComplete="off" onChange={(username) => setForm({ ...form, username })} /> : null}
          <TextField label={t("fields.password")} type="password" value={form.password ?? ""} autoComplete="new-password" placeholder={selected?.hasCredentials ? t("keepSecret") : t("optional")} onChange={(password) => setForm({ ...form, password })} />
          <NumberField label={t("fields.pollInterval")} value={form.pollIntervalMilliseconds ?? 1000} min={100} max={60000} onChange={(pollIntervalMilliseconds) => setForm({ ...form, pollIntervalMilliseconds })} />
        </div>
        {save.isError ? <p role="alert" className="mt-3 text-xs text-destructive">{t("saveError")}</p> : null}
        <button type="submit" disabled={save.isPending || !form.name || !form.equipmentId || !form.host || !form.endpoint} className="mt-4 flex h-10 w-full items-center justify-center gap-2 rounded-md bg-primary px-4 text-sm font-semibold text-primary-foreground transition-transform active:scale-[0.98] disabled:opacity-50">{save.isPending ? <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" /> : form.id ? <Save className="h-4 w-4" aria-hidden="true" /> : <Plus className="h-4 w-4" aria-hidden="true" />}{form.id ? t("saveChanges") : t("addConnection")}</button>
      </form>
    </div>
  );
}

function IconButton({ label, icon: Icon, onClick, disabled, danger }: { label: string; icon: typeof Pencil; onClick: () => void; disabled?: boolean; danger?: boolean }) {
  return <button type="button" aria-label={label} title={label} disabled={disabled} onClick={onClick} className={`flex h-9 w-9 items-center justify-center rounded-md transition-colors disabled:opacity-40 ${danger ? "text-destructive hover:bg-destructive/10" : "text-muted-foreground hover:bg-muted hover:text-foreground"}`}><Icon className="h-4 w-4" aria-hidden="true" /></button>;
}

function TextField({ label, value, onChange, type = "text", ...props }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean; placeholder?: string; autoComplete?: string }) {
  return <label className="text-xs font-medium text-muted-foreground">{label}<input {...props} type={type} value={value} onChange={(event) => onChange(event.target.value)} className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring" /></label>;
}

function NumberField({ label, value, min, max, onChange }: { label: string; value: number; min: number; max: number; onChange: (value: number) => void }) {
  return <label className="text-xs font-medium text-muted-foreground">{label}<input type="number" value={value} min={min} max={max} onChange={(event) => onChange(Number(event.target.value))} className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm tabular-nums text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring" /></label>;
}

function SelectField({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: Array<{ value: string; label: string }> }) {
  return <label className="text-xs font-medium text-muted-foreground">{label}<select value={value} onChange={(event) => onChange(event.target.value)} className="mt-1 h-10 w-full rounded-md bg-background px-3 text-sm text-foreground shadow-[0_0_0_1px_rgba(0,0,0,0.1)] outline-none focus-visible:ring-2 focus-visible:ring-ring">{options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>;
}

function CheckField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) {
  return <label className="flex min-h-10 items-center gap-2 text-xs font-medium text-foreground"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-4 w-4 accent-primary" />{label}</label>;
}

function defaultPort(protocol: DeviceProtocol) { return protocol === 0 ? 1883 : protocol === 1 ? 4840 : 502; }
function endpointPlaceholder(protocol: DeviceProtocol) { return protocol === 0 ? "mes/equipment/CNC-03/status" : protocol === 1 ? "ns=2;s=Equipment/State" : "holding-registers"; }
function formatTimestamp(value: string | null | undefined, locale: string, never: string) { return value ? new Intl.DateTimeFormat(locale, { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : never; }
