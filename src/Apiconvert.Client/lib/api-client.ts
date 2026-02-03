import { createClient } from "@/lib/supabase/client";

const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ||
  process.env.NEXT_PUBLIC_DOTNET_API_BASE_URL?.replace(/\/$/, "") ||
  "";

export async function apiFetch<T>(
  path: string,
  init: RequestInit & { body?: unknown } = {}
) {
  if (!apiBaseUrl) {
    throw new Error("NEXT_PUBLIC_API_BASE_URL is not configured.");
  }

  const supabase = createClient();
  const { data } = await supabase.auth.getSession();
  const accessToken = data.session?.access_token;
  if (!accessToken) {
    throw new Error("Not authenticated.");
  }

  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");
  headers.set("Authorization", `Bearer ${accessToken}`);

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers,
    body:
      init.body === undefined
        ? undefined
        : typeof init.body === "string"
        ? init.body
        : JSON.stringify(init.body),
  });

  const dataJson = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(dataJson?.error || "Request failed");
  }

  return dataJson as T;
}

export function getApiBaseUrl() {
  return apiBaseUrl;
}
