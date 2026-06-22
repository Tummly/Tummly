import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { imagetools } from "vite-imagetools";
import path from "path";

import { criticalPreloads } from "./vite-plugin-critical-preloads";

export default defineConfig({
  plugins: [react(), imagetools(), tailwindcss(), criticalPreloads()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
