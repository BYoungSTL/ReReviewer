import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Сборка с минимальным набором node_modules для Docker (см. Dockerfile).
  output: "standalone",
  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
};

export default nextConfig;
