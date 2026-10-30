import { expect, test } from "@playwright/test"

import { openQrMaterials, sectionOf } from "./marketing-qr-materials"

test("FAQ rows load open and toggle (Figma 6858:12612)", async ({ page }) => {
  await openQrMaterials(page)
  const section = sectionOf(page, /Frequently asked questions/)
  const rows = section.locator("[aria-expanded]")
  await expect(rows).toHaveCount(7)
  for (const row of await rows.all()) {
    await expect(row).toHaveAttribute("aria-expanded", "true")
  }
  const website = section.getByRole("button", { name: "Do I need a website or printer?" })
  await website.click()
  await expect(website).toHaveAttribute("aria-expanded", "false")
  await expect(section.getByRole("link", { name: "Explore all FAQs" })).toHaveAttribute("href", "/faqs")
})

test("sign-up CTA sends the email to Pilot signup (Figma 5661:19499)", async ({ page }) => {
  await openQrMaterials(page)
  const section = sectionOf(page, /Give customers an easy way to tell you/)
  await expect(section.getByRole("button", { name: /Continue with Google/ })).toBeVisible()
  await expect(section.getByRole("button", { name: /Continue with Microsoft/ })).toBeVisible()
  await expect(section.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
  await expect(section.getByRole("link", { name: "Privacy Notice" })).toHaveAttribute("href", "/privacy")
  await section.getByPlaceholder("Work email").fill("qa.verify@tummly.test")
  await section.getByRole("button", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/signup\?plan=Pilot/)
})
