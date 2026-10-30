import { expect, test, type Page } from "@playwright/test"

// Figma For Groups hero `6392:18184`: the hero loop (`for-groups/hero.mp4`)
// plays in the 1548×763 frame; its first frame is the Figma still, which is
// the poster and the reduced-motion image.
const heroMedia = (page: Page) =>
  page
    .locator("section")
    .filter({ has: page.getByRole("heading", { level: 1 }) })
    .locator(".aspect-\\[1548\\/763\\]")

test("hero plays the For Groups loop with the Figma still as poster", async ({ page }) => {
  await page.goto("/for-groups")
  const media = heroMedia(page)
  const video = media.locator("video")
  await expect(video).toHaveCount(1)
  await expect(video.locator("source")).toHaveAttribute(
    "src",
    /\/for-groups\/hero\.mp4$/,
  )
  await expect(video).toHaveAttribute("poster", /\S/)
  await expect
    .poll(() => video.evaluate((v: HTMLVideoElement) => v.currentTime), {
      timeout: 30_000,
    })
    .toBeGreaterThan(0)
})

test.describe("reduced motion", () => {
  test.use({ reducedMotion: "reduce" })

  test("hero shows the still and loads no video", async ({ page }) => {
    await page.goto("/for-groups")
    const media = heroMedia(page)
    await expect(media.locator("img")).toHaveAttribute("src", /\S/)
    await expect(media.locator("video")).toHaveCount(0)
  })
})
