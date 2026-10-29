import { defineConfig, devices } from "@playwright/test"

// E2E_* logins live in the gitignored root .env (see CLAUDE.md "Verify setup").
try {
  process.loadEnvFile(".env")
} catch {
  // No .env: flows that need sign-in skip themselves.
}

const baseURL = "http://localhost:5173"

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: "npm run dev -- --port 5173 --strictPort",
    url: baseURL,
    reuseExistingServer: true,
    timeout: 120_000,
  },
})
