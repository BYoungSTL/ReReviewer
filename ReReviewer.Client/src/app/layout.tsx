import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { SiteHeader } from "@/components/site-header";
import { Providers } from "./providers";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-sans",
  subsets: ["latin", "cyrillic"],
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
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col">
        <Providers>
          <SiteHeader />
          <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-8">{children}</main>
        </Providers>
      </body>
    </html>
  );
}
