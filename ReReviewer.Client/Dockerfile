# Сайт ReReviewer на Next.js. Сборка — из папки ReReviewer.Client (контекст в docker-compose.yml: ./ReReviewer.Client).
# API при сборке не нужен: страницы запрашивают его только во время запроса.

FROM node:24-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:24-alpine AS build
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# Шрифты next/font/google скачиваются здесь, поэтому сборке нужен интернет.
# Папки public в репозитории может не быть (git не хранит пустые папки), а итоговый образ копирует её.
RUN mkdir -p public && npm run build

# output: "standalone" (next.config.ts) — сервер и только нужные ему node_modules.
FROM node:24-alpine AS final
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
COPY --from=build /app/public ./public
# Next.js пишет кэш страниц в .next/cache, поэтому файлы принадлежат пользователю, от которого он работает.
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
USER node
EXPOSE 3000
CMD ["node", "server.js"]
