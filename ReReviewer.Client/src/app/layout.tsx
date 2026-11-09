import type { Metadata } from "next";
import { Geist_Mono, Manrope, Unbounded } from "next/font/google";
import { SiteHeader } from "@/components/site-header";
import { Providers } from "./providers";
import "./globals.css";

const manrope = Manrope({
  variable: "--font-sans",
  subsets: ["latin", "cyrillic"],
});

// Только для заголовков страниц и крупных оценок (класс font-display).
const unbounded = Unbounded({
  variable: "--font-unbounded",
  subsets: ["latin", "cyrillic"],
  weight: ["500", "700"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin", "cyrillic"],
});

export const metadata: Metadata = {
  metadataBase: new URL(process.env.SITE_URL ?? "http://localhost:3000"),
  title: {
    default: "ReReviewer — отзывы об игровых обзорщиках",
    template: "%s — ReReviewer",
  },
  description: "Отзывы об игровых журналистах и блогерах: насколько честны, глубоки и полезны их обзоры.",
  openGraph: {
    siteName: "ReReviewer",
    locale: "ru_RU",
    type: "website",
  },
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="ru"
      className={`dark ${manrope.variable} ${unbounded.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col">
        <Providers>
          <SiteHeader />
          <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6">{children}</main>
          <footer className="border-t">
            <div className="mx-auto w-full max-w-6xl px-4 py-6 text-sm text-muted-foreground sm:px-6">
              ReReviewer — отзывы об игровых обзорщиках
            </div>
          </footer>
        </Providers>
      </body>
    </html>
  );
}
