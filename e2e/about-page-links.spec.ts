import { expect, test } from "@playwright/test"

import { ABOUT_HEADINGS, openAboutPage, section } from "./about-page"

test("About hero CTAs link to the Pilot and How it works pages", async ({ page }) => {
  await openAboutPage(page)
  const hero = section(page, ABOUT_HEADINGS.hero)
  // Pilot intent opens the marketing Pilot page (site-wide RequestTrialLink pattern).
  await expect(hero.getByRole("link", { name: "Get started" })).toHaveAttribute("href", "/30-day-pilot")
  await expect(hero.getByRole("link", { name: "See how it works" })).toHaveAttribute("href", "/how-it-works")

  await hero.getByRole("link", { name: "See how it works" }).click()
  await expect(page).toHaveURL(/\/how-it-works$/)
})

test("About hero Get started opens the 30-day Pilot page", async ({ page }) => {
  await openAboutPage(page)
  await section(page, ABOUT_HEADINGS.hero).getByRole("link", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/30-day-pilot$/)
})

test("Contact Tummly opens the Help Centre contact page", async ({ page }) => {
  await openAboutPage(page)
  const company = section(page, ABOUT_HEADINGS.company)
  const contact = company.getByRole("link", { name: "Contact Tummly" })
  await expect(contact).toHaveAttribute("href", "/help-center/contact")
  await contact.click()
  await expect(page).toHaveURL(/\/help-center\/contact$/)
  await expect(page.getByRole("heading", { level: 1 }).first()).toHaveText("Tell us what you need.")
})

for (const width of [1280, 1440]) {
  test(`Header Resources menu reaches the About page at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 })
    await openAboutPage(page) // dismisses the cookie banner for this context
    await page.goto("/")
    // Resources opens on hover (Radix menu); a click would toggle it shut again.
    await page.locator("header").getByRole("button", { name: "Resources" }).hover()
    const about = page.getByRole("menu").getByRole("menuitem", { name: "About", exact: true })
    await expect(about).toBeVisible()
    await about.click()
    await expect(page).toHaveURL(/\/about$/)
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(ABOUT_HEADINGS.hero)
  })
}
