import type { Page } from "@playwright/test"

// Marketing homepage (Figma file UP1DqyGGGrxx80Dp7jReTT, frames 5433:12691 … 6788:42946).
export async function openHome(page: Page) {
  await page.goto("/")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  await reject.waitFor({ timeout: 4000 }).catch(() => {})
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

/** The <section> that holds the heading with this name. */
export function sectionOf(page: Page, heading: RegExp) {
  return page
    .getByRole("heading", { name: heading })
    .first()
    .locator("xpath=ancestor::section[1]")
}
