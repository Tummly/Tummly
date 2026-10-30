import type { Page } from "@playwright/test"

// Marketing About page (Figma file UP1DqyGGGrxx80Dp7jReTT): hero `5673:24144`,
// industries `5685:35427`, principles `5673:24211`, reality `5673:24194`,
// mission `7023:13862`, company `5673:24231`, sign-up `5661:24423`.
// The page is public and calls no API on load.
export async function openAboutPage(page: Page) {
  await page.goto("/about")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  await reject.waitFor({ timeout: 4000 }).catch(() => {})
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

// Sections carry no accessible name, so find each section by its heading.
export const section = (page: Page, heading: string | RegExp) =>
  page
    .locator("section")
    .filter({ has: page.getByRole("heading", { name: heading, exact: typeof heading === "string" }) })
    .last()

export const ABOUT_HEADINGS = {
  hero: "Built for the work that follows a customer’s visit.",
  industries: "For the people making the next service happen.",
  principles: "The principles behind Tummly.",
  reality: "Designed around the moments that matter.",
  mission: "Make listening part of running a better restaurant.",
  company: "The company behind Tummly",
  signUp: "See what that looks like in practice.",
} as const

/** Scroll the whole page so lazy images load. */
export async function scrollThrough(page: Page) {
  await page.evaluate(async () => {
    for (let y = 0; y < document.body.scrollHeight; y += 400) {
      window.scrollTo(0, y)
      await new Promise((resolve) => setTimeout(resolve, 50))
    }
    window.scrollTo(0, 0)
  })
}
