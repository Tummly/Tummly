import type { Page } from "@playwright/test"

// Marketing QR materials (Figma file UP1DqyGGGrxx80Dp7jReTT, frames 5661:19336 … 5661:19499).
export async function openQrMaterials(page: Page) {
  await page.goto("/qr-materials")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  await reject.waitFor({ timeout: 4000 }).catch(() => {})
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

export { sectionOf } from "./marketing-home"
