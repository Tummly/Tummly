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

  test("follows Figma: button line heights and the campaign video", async ({ page }) => {
    await openHome(page)
    const hero = sectionOf(page, /Give your customers more reasons to come back/)
    // Figma Primary button label 22px, Secondary 20px.
    await expect(hero.getByRole("button", { name: "Start 30-day Pilot" })).toHaveCSS("line-height", "22px")
    const platform = sectionOf(page, /Make customer feedback useful/)
    await expect(platform.getByRole("link", { name: "Explore product features" })).toHaveCSS("line-height", "20px")
    // Campaign results loop; its first frame (the pizza still) is the poster.
    const video = hero.locator("video")
    await expect(video).toBeVisible()
    await expect(video).toHaveAttribute("poster", /hero-still/)
    await expect(video.locator("source")).toHaveAttribute(
      "src",
      /\/marketing\/home\/hero-campaign-results-loop-1080\.mp4$/,
    )
  })

  test("reduced motion shows the campaign still, not the video", async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "reduce" })
    await openHome(page)
    const hero = sectionOf(page, /Give your customers more reasons to come back/)
    await expect(hero.locator('img[src*="hero-still"]')).toBeVisible()
    await expect(hero.locator("video")).toHaveCount(0)
  })

  test("the fixed demo QR chip opens on hover and links to the product demo", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await openHome(page)
    const chip = page.getByRole("link", { name: "Scan to try on your phone" })
    await expect(chip).toBeVisible()
    await expect(chip).toHaveAttribute("href", "/product-demo")
    await expect(chip).toHaveAttribute("aria-expanded", "false")
    await chip.hover()
    await expect(chip).toHaveAttribute("aria-expanded", "true")
  })

  test("the fixed demo QR chip shows on marketing pages only", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    const chip = page.getByRole("link", { name: "Scan to try on your phone" })
    for (const path of ["/product", "/for-groups", "/about"]) {
      await page.goto(path)
      await expect(chip, path).toBeVisible()
    }
    for (const path of ["/signup", "/login", "/privacy"]) {
      await page.goto(path)
      // Wait for the page to render before checking the chip is absent.
      await expect(page.getByRole("heading").first()).toBeVisible()
      await expect(chip, path).toHaveCount(0)
    }
  })

  test("the fixed demo QR chip is hidden below lg", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 })
    await openHome(page)
    await expect(page.getByRole("link", { name: "Scan to try on your phone" })).toBeHidden()
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
