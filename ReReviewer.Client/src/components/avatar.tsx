import { cn } from "cn";

// Фотографий в API нет, поэтому аватар — инициалы на цветном круге. Цвет зависит от id, чтобы у человека он был всегда один.
const PALETTE = [
  { background: "#3b2f5c", color: "#d9ccff" },
  { background: "#1f4a45", color: "#b8f0e4" },
  { background: "#2b3f5c", color: "#c7dcff" },
  { background: "#4a2b2b", color: "#ffd0c7" },
  { background: "#4a4220", color: "#f3e6a8" },
  { background: "#2e4a26", color: "#cff2c2" },
];

// Псевдоним в кавычках («PixelHunter») в инициалы не входит: «Иван «PixelHunter» Петров» → «ИП».
// Слова не с буквы (цифры, знаки) тоже пропускаются: «Тест настроек 2» → «ТН».
export function initials(name: string) {
  const all = name.replace(/«[^»]*»/g, " ").split(/\s+/).filter(Boolean);
  const lettered = all.filter((word) => /^\p{L}/u.test(word));
  const words = lettered.length > 0 ? lettered : all;
  const letters = words.length > 1 ? [words[0], words[words.length - 1]] : words;

  return letters.map((word) => word[0]).join("").toUpperCase();
}

function colorsOf(id: string) {
  let hash = 0;
  for (const char of id) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;

  return PALETTE[hash % PALETTE.length];
}

// src — загруженная аватарка (lib/avatar.ts), без неё рисуются инициалы. Картинка декоративная: имя всегда написано рядом.
export function Avatar({ id, name, src, size = "md", className }: {
  id: string;
  name: string;
  src?: string | null;
  size?: "xs" | "sm" | "md" | "lg";
  className?: string;
}) {
  const sizeClass = cn(
    size === "xs" && "size-8 text-[11px]",
    size === "sm" && "size-9 text-xs",
    size === "md" && "size-12 text-sm",
    size === "lg" && "size-20 text-2xl sm:size-22",
  );

  if (src)
    // eslint-disable-next-line @next/next/no-img-element -- аватарка уже 256×256 и отдаётся через BFF, оптимизатор ей не нужен
    return <img src={src} alt="" width={256} height={256} className={cn("shrink-0 rounded-full object-cover", sizeClass, className)} />;

  return (
    <span
      aria-hidden="true"
      style={colorsOf(id)}
      className={cn("inline-flex shrink-0 items-center justify-center rounded-full font-display font-bold", sizeClass, className)}
    >
      {initials(name)}
    </span>
  );
}
