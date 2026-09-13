import type { NextConfig } from "next";

// Content-Security-Policy is set per request in src/proxy.ts (it needs a nonce).
const securityHeaders = [
  { key: "X-Frame-Options", value: "DENY" },
  { key: "X-Content-Type-Options", value: "nosniff" },
  // "same-origin", not "no-referrer": with "no-referrer" Chromium sends `Origin: null` on the
  // form POST, which fails the Server Actions same-origin check.
  { key: "Referrer-Policy", value: "same-origin" },
  { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
];

const nextConfig: NextConfig = {
  poweredByHeader: false,
  output: "standalone",
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default nextConfig;
