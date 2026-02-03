import type { NextConfig } from "next";

const normalizeOrigin = (value?: string) =>
  value?.replace(/\/$/, "").toLowerCase() ?? "";

const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL ||
  process.env.NEXT_PUBLIC_DOTNET_API_BASE_URL ||
  process.env.DOTNET_API_BASE_URL ||
  "";

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || "";
const shouldProxyApi =
  Boolean(apiBaseUrl) && normalizeOrigin(apiBaseUrl) !== normalizeOrigin(siteUrl);

const nextConfig: NextConfig = {
  turbopack: {
    root: __dirname,
  },
  async rewrites() {
    if (!shouldProxyApi) return [];
    return [
      {
        source: "/api/:path*",
        destination: `${apiBaseUrl.replace(/\/$/, "")}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
