import { expect, test } from "@playwright/test"

import { openHome, sectionOf } from "./marketing-home"

test.describe("Home hero signup (Figma 5433:12691)", () => {
  test("shows the new copy, OAuth buttons and legal links", async ({ page }) => {
    await openHome(page)
    const hero = sectionOf(page, /Give your customers more reasons to come back/)
    await expect(hero.getByText("Customer feedback and marketing for restaurants", { exact: false })).toBeVisible()
    await expect(hero.getByText("No payment card required")).toBeVisible()
    await expect(hero.getByRole("button", { name: /Continue with Google/ })).toBeVisible()
    await expect(hero.getByRole("button", { name: /Continue with Microsoft/ })).toBeVisible()
    await expect(hero.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
    await expect(hero.getByRole("link", { name: "Privacy Notice" })).toHaveAttribute("href", "/privacy")
  })

  test("follows Figma: button line heights, campaign still, no extra chrome", async ({ page }) => {
    await openHome(page)
    const hero = sectionOf(page, /Give your customers more reasons to come back/)
    // Figma Primary button label 22px, Secondary 20px.
    await expect(hero.getByRole("button", { name: "Start 30-day Pilot" })).toHaveCSS("line-height", "22px")
    const platform = sectionOf(page, /Make customer feedback useful/)
    await expect(platform.getByRole("link", { name: "Explore product features" })).toHaveCSS("line-height", "20px")
    // Hero visual is the Figma still until the new video is uploaded.
    await expect(hero.locator("video")).toHaveCount(0)
    await expect(hero.locator('img[src*="hero-still"]')).toBeVisible()
    // The fixed demo QR chip is not in the Figma frames.
    await expect(page.getByRole("link", { name: "Scan to try on your phone" })).toHaveCount(0)
  })

  test("a valid work email continues to Pilot signup", async ({ page }) => {
    await openHome(page)
    const hero = sectionOf(page, /Give your customers more reasons to come back/)
    await hero.getByPlaceholder("Work email").fill("verify.home@tummly.test")
    await hero.getByRole("button", { name: "Start 30-day Pilot" }).click()
    await expect(page).toHaveURL(/\/signup\?plan=Pilot/)
    await expect(page.getByRole("heading", { name: "Create your Tummly account" })).toBeVisible()
  })

  test("an invalid email stays on the page", async ({ page }) => {
    await openHome(page)
    const hero = sectionOf(page, /Give your customers more reasons to come back/)
    await hero.getByPlaceholder("Work email").fill("not-an-email")
    await hero.getByRole("button", { name: "Start 30-day Pilot" }).click()
    await expect(page).toHaveURL(/\/$/)
  })
})

test("bottom sign-up CTA has the same signup controls (Figma 6788:42946)", async ({ page }) => {
  await openHome(page)
  const cta = sectionOf(page, /Your next customer conversation starts here/)
  await cta.scrollIntoViewIfNeeded()
  await expect(cta.getByRole("button", { name: "Get started" })).toBeVisible()
  await expect(cta.getByRole("button", { name: /Continue with Google/ })).toBeVisible()
  await expect(cta.getByRole("button", { name: /Continue with Microsoft/ })).toBeVisible()
  await expect(cta.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
  await expect(cta.locator("video")).toHaveCount(1)
  // Figma legal note colour #7c7c7c.
  await expect(cta.getByText(/Read our/)).toHaveCSS("color", "rgb(124, 124, 124)")
})
