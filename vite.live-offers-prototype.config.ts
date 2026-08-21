import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"
import { imagetools } from "vite-imagetools"
import path from "path"
import { fileURLToPath } from "url"

const __dirname = path.dirname(fileURLToPath(import.meta.url))

/** Throwaway ungated UI review for Home Live offers — port 5174. */
export default defineConfig({
  plugins: [react(), imagetools(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5174,
    strictPort: true,
    fs: {
      // Worktree may symlink node_modules to the main checkout.
      allow: [
        path.resolve(__dirname),
        path.resolve(__dirname, "../Tummly"),
        path.resolve(__dirname, "../Tummly/node_modules"),
      ],
    },
  },
  build: {
    rollupOptions: {
      input: path.resolve(__dirname, "prototype-live-offers.html"),
    },
  },
})
