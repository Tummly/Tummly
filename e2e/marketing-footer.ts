import type { Page } from "@playwright/test"

// Marketing footer (Figma desktop 5839:73487, mobile 6243:19686).
// /terms is static: it renders the footer without the local API.
export async function openFooter(page: Page) {
  await page.goto("/terms")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
  const footer = page.locator("footer").last()
  await footer.scrollIntoViewIfNeeded()
  return footer
}
