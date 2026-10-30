import { expect, test } from "@playwright/test"

import { openHowItWorks, sectionOf } from "./marketing-how-it-works"

test("FAQ rows load open and toggle (Figma 6874:12909)", async ({ page }) => {
  await openHowItWorks(page)
  const section = sectionOf(page, /Frequently asked questions/)
  const rows = section.locator("[aria-expanded]")
  await expect(rows).toHaveCount(9)
  const website = section.getByRole("button", { name: "Do I need a website?" })
  await expect(website).toHaveAttribute("aria-expanded", "true")
  await website.click()
  await expect(website).toHaveAttribute("aria-expanded", "false")
  await expect(section.getByRole("link", { name: "Explore all FAQs" })).toHaveAttribute("href", "/faqs")
})

for (const heading of [/From the first scan/, /Start with your restaurant/]) {
  test(`sign-up form ${heading.source} sends the email to Pilot signup`, async ({ page }) => {
    await openHowItWorks(page)
    const section = sectionOf(page, heading)
    await expect(section.getByRole("button", { name: /Continue with Google/ })).toBeVisible()
    await expect(section.getByRole("button", { name: /Continue with Microsoft/ })).toBeVisible()
    await expect(section.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
    await expect(section.getByRole("link", { name: "Privacy Notice" })).toHaveAttribute("href", "/privacy")
    const email = section.getByPlaceholder("Work email")
    await email.fill("not-an-email")
    await section.getByRole("button", { name: "Get started" }).click()
    await expect(page).toHaveURL(/\/how-it-works/)
    expect(await email.evaluate((el: HTMLInputElement) => el.validity.valid)).toBe(false)
    await email.fill("qa.verify@tummly.test")
    await section.getByRole("button", { name: "Get started" }).click()
    await expect(page).toHaveURL(/\/signup\?plan=Pilot/)
  })
}
