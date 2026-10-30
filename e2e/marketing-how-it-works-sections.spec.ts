import { expect, test } from "@playwright/test"

import { openHowItWorks } from "./marketing-how-it-works"

const HEADINGS = [
  "From the first scan to a reason to return.",
  "Get the Guest Loop started in three steps.",
  "A short form. Space for an honest answer.",
  "Read the experience. Make the reply personal.",
  "Keep the customer context for next time.",
  "Give customers a relevant invitation.",
  "Learn from the response. Plan what comes next.",
  "Let AI help with the first draft.",
  "Choose how you would like to start.",
  "Frequently asked questions",
  "Start with your restaurant. Build from there.",
]

test("sections follow the Figma frame order", async ({ page }) => {
  await openHowItWorks(page)
  await expect(page.getByRole("heading", { level: 1 })).toHaveCount(1)
  await expect(page.getByText("Built for the way UK restaurants work", { exact: false })).toBeVisible()
  const tops: number[] = []
  for (const name of HEADINGS) {
    const heading = page.getByRole("heading", { name, exact: true }).first()
    await expect(heading).toBeAttached()
    tops.push(await heading.evaluate((el) => el.getBoundingClientRect().top + window.scrollY))
  }
  expect(tops).toEqual([...tops].sort((a, b) => a - b))
})

test("page has no overflow or errors while scrolling at each breakpoint", async ({ page }) => {
  const errors: string[] = []
  page.on("pageerror", (e) => errors.push(e.message))
  for (const [width, height] of [[375, 812], [768, 1024], [1024, 768], [1440, 900]]) {
    await page.setViewportSize({ width, height })
    await openHowItWorks(page)
    for (let y = 0; y < 20000; y += 600) {
      await page.mouse.wheel(0, 600)
      await page.waitForTimeout(40)
    }
    await expect(page.getByRole("heading", { level: 1 })).toBeAttached()
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)
    expect(overflow, `width ${width}`).toBe(0)
  }
  expect(errors).toEqual([])
})
