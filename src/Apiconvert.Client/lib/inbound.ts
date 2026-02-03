export function normalizeInboundPath(value: string) {
  return value.trim().replace(/^\/+|\/+$/g, "");
}
