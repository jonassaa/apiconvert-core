import { isIP } from "node:net";

function isPrivateIpv4(host: string) {
  const parts = host.split(".").map((part) => Number(part));
  if (parts.length !== 4 || parts.some((part) => Number.isNaN(part))) {
    return false;
  }
  if (parts[0] === 10) return true;
  if (parts[0] === 127) return true;
  if (parts[0] === 169 && parts[1] === 254) return true;
  if (parts[0] === 192 && parts[1] === 168) return true;
  if (parts[0] === 172 && parts[1] >= 16 && parts[1] <= 31) return true;
  return false;
}

function isPrivateIpv6(host: string) {
  const normalized = host.toLowerCase();
  if (normalized === "::1") return true;
  const firstBlock = normalized.split(":")[0] ?? "";
  if (firstBlock.startsWith("fc") || firstBlock.startsWith("fd")) return true;
  if (
    firstBlock.startsWith("fe8") ||
    firstBlock.startsWith("fe9") ||
    firstBlock.startsWith("fea") ||
    firstBlock.startsWith("feb")
  ) {
    return true;
  }
  return false;
}

export function validateForwardUrl(input: string) {
  let url: URL;
  try {
    url = new URL(input);
  } catch {
    return { ok: false, error: "Forward URL is invalid." };
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    return { ok: false, error: "Forward URL must use http or https." };
  }

  const hostname = url.hostname.toLowerCase();
  if (
    hostname === "localhost" ||
    hostname.endsWith(".localhost") ||
    hostname.endsWith(".local")
  ) {
    return { ok: false, error: "Forward URL cannot target local hosts." };
  }

  const ipType = isIP(hostname);
  if (ipType === 4 && isPrivateIpv4(hostname)) {
    return { ok: false, error: "Forward URL cannot target private IPv4 ranges." };
  }
  if (ipType === 6 && isPrivateIpv6(hostname)) {
    return { ok: false, error: "Forward URL cannot target private IPv6 ranges." };
  }

  return { ok: true, error: null as string | null };
}
