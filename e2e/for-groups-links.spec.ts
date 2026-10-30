import { expect, test, type Page } from "@playwright/test"

async function openForGroups(page: Page) {
  await page.goto("/for-groups")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

test("section CTAs link to their pages", async ({ page }) => {
  await openForGroups(page)
  await expect(page.getByRole("link", { name: "Explore product features" })).toHaveAttribute("href", "/product")
  await expect(page.getByRole("link", { name: "Explore QR Materials" })).toHaveAttribute("href", "/qr-materials")
  await expect(page.getByRole("link", { name: "Explore Tummly AI" })).toHaveAttribute("href", "/ai-assistant")
  await expect(page.getByRole("link", { name: "Explore all FAQs" })).toHaveAttribute("href", "/faqs")
})

test("pricing CTAs link to signup, Pricing and contact", async ({ page }) => {
  await openForGroups(page)
  await expect(page.getByRole("link", { name: "Choose Group plan" })).toHaveAttribute(
    "href",
    "/signup?plan=Group&cadence=monthly",
  )
  await expect(page.getByRole("link", { name: "Compare all plans" })).toHaveAttribute("href", "/pricing")
  await expect(page.getByRole("link", { name: "Contact us" })).toHaveAttribute("href", "/help-center/contact")
})

test("hero and bottom CTA legal notes link to Terms and Privacy", async ({ page }) => {
  await openForGroups(page)
  for (const heading of [
    "See where your restaurant group needs attention.",
    "Make customer experience part of your group routine.",
  ]) {
    const form = page.locator("section").filter({ has: page.getByRole("heading", { name: heading }) })
    await expect(form.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
    await expect(form.getByRole("link", { name: "Privacy Notice" })).toHaveAttribute("href", "/privacy")
  }
})

test("Explore product features opens the Product page", async ({ page }) => {
  await openForGroups(page)
  await page.getByRole("link", { name: "Explore product features" }).click()
  await expect(page).toHaveURL(/\/product$/)
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible()
})
