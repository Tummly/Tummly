import type { Page } from "@playwright/test"

// Marketing How it works (Figma file UP1DqyGGGrxx80Dp7jReTT, frames 5661:19524 … 6874:12985).
export async function openHowItWorks(page: Page) {
  await page.goto("/how-it-works")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  await reject.waitFor({ timeout: 4000 }).catch(() => {})
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

export { sectionOf } from "./marketing-home"
