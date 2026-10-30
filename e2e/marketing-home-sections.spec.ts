import { expect, test } from "@playwright/test"

import { openHome, sectionOf } from "./marketing-home"

test("serve strip scrolls, hides its duplicate list and can pause (Figma 6757:45771)", async ({ page }) => {
  await openHome(page)
  const strip = page.locator(".home-serve-marquee")
  await expect(strip.getByText("Kebab shops").first()).toBeVisible()
  const track = page.locator(".home-serve-marquee-track")
  await expect(track).toHaveCSS("animation-name", "home-serve-marquee")
  await expect(track.locator("ul")).toHaveCount(2)
  await expect(track.locator("ul").nth(1)).toHaveAttribute("aria-hidden", "true")
  await expect(strip.getByRole("listitem")).toHaveCount(await track.locator("ul").first().locator("li").count())
  const pause = page.getByRole("button", { name: "Pause animation" })
  await pause.focus()
  await pause.click()
  await expect(page.getByRole("button", { name: "Play animation" })).toBeAttached()
  await expect(track).toHaveCSS("animation-play-state", "paused")
})

test("platform cards and product link (Figma 6761:46375)", async ({ page }) => {
  await openHome(page)
  const section = sectionOf(page, /Make customer feedback useful to your business/)
  for (const title of ["Hear what matters", "Know the customer context", "Create a reason to return", "Learn from the response"]) {
    await expect(section.getByRole("heading", { name: title })).toBeVisible()
  }
  await section.scrollIntoViewIfNeeded()
  await expect(section.locator("video")).toHaveCount(3)
  // Small 864×1080 loops; the 2160×2700 masters made the cards stutter.
  await expect(section.locator('video source[src$="-864.mp4"]')).toHaveCount(3)
  await expect(section.getByRole("link", { name: "Explore product features" })).toHaveAttribute("href", "/product")
})

test("platform videos start to load before the section scrolls in", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await openHome(page)
  const section = sectionOf(page, /Make customer feedback useful to your business/)
  // No scroll: the cards are below the fold, but the loops mount and preload.
  await expect(section.locator("video")).toHaveCount(3)
  for (const video of await section.locator("video").all()) {
    await expect(video).toHaveAttribute("preload", "auto")
  }
})

test("QR format list changes the photo (Figma 4902:19649)", async ({ page }) => {
  await openHome(page)
  const qr = sectionOf(page, /Meet customers where the experience happens/)
  await qr.scrollIntoViewIfNeeded()
  const photo = qr.locator("img").first()
  const before = await photo.getAttribute("src")
  await qr.getByRole("button", { name: /QR Guest Cards/ }).or(qr.getByRole("tab", { name: /QR Guest Cards/ })).first().click()
  await expect.poll(() => photo.getAttribute("src")).not.toBe(before)
})

test("industries show four cards (Figma 6792:13613)", async ({ page }) => {
  await openHome(page)
  const section = sectionOf(page, /Built for real restaurant days/)
  for (const title of ["Takeaways & QSRs", "Cafés & bakeries", "Casual dining", "Growing restaurant groups"]) {
    await expect(section.getByRole("heading", { name: title })).toBeVisible()
  }
})

test("trust accordion toggles and the badge carousel stays gated (Figma 4942:19219)", async ({ page }) => {
  await openHome(page)
  const section = sectionOf(page, /Good communication starts with trust/)
  const trigger = section.getByRole("button", { name: "A private way to be heard" })
  await expect(trigger).toHaveAttribute("aria-expanded", "true")
  await trigger.click()
  await expect(trigger).toHaveAttribute("aria-expanded", "false")
  await expect(section.getByRole("link", { name: "Explore Trust & Privacy" })).toHaveAttribute("href", "/trust-privacy")
  await expect(section.getByRole("img", { name: "ISO/IEC 27001" })).toHaveCount(0)
})
