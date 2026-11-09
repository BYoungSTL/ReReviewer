import type { ReviewerViewModel } from "@/lib/api/schema";

type Outlet = ReviewerViewModel["outlets"][number];

// Издание без даты окончания — текущее место работы.
export const currentOutlets = (reviewer: ReviewerViewModel) =>
  reviewer.outlets.filter((outlet) => outlet.endedAt === null);

export const pastOutlets = (reviewer: ReviewerViewModel) =>
  reviewer.outlets.filter((outlet) => outlet.endedAt !== null);

// Подпись под именем в карточке: где работает сейчас.
export const outletLine = (reviewer: ReviewerViewModel) =>
  currentOutlets(reviewer).map((outlet) => outlet.outletName).join(", ") || "Независимый автор";

const LINK_LABELS: Record<string, string> = {
  "youtube.com": "YouTube",
  "youtu.be": "YouTube",
  "t.me": "Telegram",
  "twitch.tv": "Twitch",
  "vk.com": "ВКонтакте",
  "x.com": "X",
  "twitter.com": "X",
  "boosty.to": "Boosty",
  "dzen.ru": "Дзен",
};

// Подпись ссылки обзорщика: название известного сервиса или домен без www.
export function linkLabel(url: string) {
  try {
    const host = new URL(url).hostname.replace(/^www\./, "");

    return LINK_LABELS[host] ?? host;
  } catch {
    return url;
  }
}

// «Игровой вестник, 2016–2021». Даты приходят как «2016-01-15».
export const outletPeriod = (outlet: Outlet) =>
  [outlet.startedAt?.slice(0, 4), outlet.endedAt?.slice(0, 4)].filter(Boolean).join("–");
