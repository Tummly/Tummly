import { expect, test } from "@playwright/test"

// Figma For Groups "Keep the Guest Loop working" `6405:36122`: four loop videos
// that carry the Figma still as `poster`; reduced motion shows the still only.
const cards = (page: import("@playwright/test").Page) =>
  page
    .locator("section")
    .filter({ has: page.getByRole("heading", { name: /Keep the Guest Loop working/ }) })
    .locator("article")

test("Guest Loop cards use the Figma still as the video poster", async ({ page }) => {
  await page.goto("/for-groups")
  const section = cards(page)
  await section.first().scrollIntoViewIfNeeded()
  await expect(section).toHaveCount(4)
  const videos = section.locator("video")
  await expect(videos).toHaveCount(4)
  for (let i = 0; i < 4; i++) {
    await expect(videos.nth(i)).toHaveAttribute("poster", /\S/)
  }
})

test.describe("reduced motion", () => {
  test.use({ reducedMotion: "reduce" })

  test("Guest Loop cards show the still and load no video", async ({ page }) => {
    await page.goto("/for-groups")
    const section = cards(page)
    await section.first().scrollIntoViewIfNeeded()
    await expect(section.locator("img")).toHaveCount(4)
    await expect(section.locator("video")).toHaveCount(0)
  })
})
