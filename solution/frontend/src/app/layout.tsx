import type { Metadata } from "next";
import { connection } from "next/server";
import "./globals.css";

export const metadata: Metadata = {
  title: "Fundo Loans",
  description: "Apply for a business loan.",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  // Render per request: the CSP nonce set in proxy.ts is only applied to dynamically rendered pages.
  await connection();

  return (
    <html lang="en">
      <body className="min-h-screen antialiased">{children}</body>
    </html>
  );
}
