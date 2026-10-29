import type { Page } from "@playwright/test"

import customUnitRates from "./fixtures/public-custom-unit-rates.json" with { type: "json" }

// Marketing Product page (Figma `5661:22625` … `6824:12527`).
// The plans strip prices come from the public catalog; the stub keeps the
// test independent of the local API and its CORS origin list.
export async function openProductPage(
  page: Page,
  { catalog = "ok" }: { catalog?: "ok" | "error" } = {},
) {
  await page.route("**/api/public/commercial-catalog/custom-unit-rates", (route) =>
    catalog === "ok"
      ? route.fulfill({
          json: customUnitRates,
          headers: { "Access-Control-Allow-Origin": "*" },
        })
      : route.fulfill({
          status: 500,
          json: { success: false, message: "Catalog not ready." },
          headers: { "Access-Control-Allow-Origin": "*" },
        }),
  )
  await page.goto("/product")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

// Hero, plans strip and sign-up CTA sections carry no accessible name, so
// find each section by its heading.
export const section = (page: Page, heading: string | RegExp) =>
  page
    .locator("section")
    .filter({ has: page.getByRole("heading", { name: heading, exact: typeof heading === "string" }) })
    .last()
