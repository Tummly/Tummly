import { expect, test } from "@playwright/test"

import { openHome } from "./marketing-home"

const STEPS = [
  ["Connect with customers", "Hear how the order or visit went."],
  ["Get to know your guests", "Give each response the context it deserves."],
  ["Invite them back", "Make the next invitation relevant."],
  ["Understand the response", "See what customers say and respond to."],
] as const

test.describe("Home How it works tabs (Figma 5754:22093)", () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test("each tab click selects its step without a page error", async ({ page }) => {
    const errors: string[] = []
    page.on("pageerror", (e) => errors.push(e.message))
    await openHome(page)
    await page.getByRole("tab", { name: STEPS[0][0] }).scrollIntoViewIfNeeded()
    for (const [tab, title] of STEPS) {
      await page.getByRole("tab", { name: tab }).click()
      await expect(page.getByRole("tab", { name: tab })).toHaveAttribute("aria-selected", "true")
      await expect(page.getByRole("tabpanel").getByRole("heading", { name: title })).toBeVisible()
    }
    expect(errors).toEqual([])
  })

  test("arrow keys move between tabs", async ({ page }) => {
    await openHome(page)
    const first = page.getByRole("tab", { name: STEPS[0][0] })
    await first.scrollIntoViewIfNeeded()
    await first.focus()
    await page.keyboard.press("ArrowRight")
    const second = page.getByRole("tab", { name: STEPS[1][0] })
    await expect(second).toHaveAttribute("aria-selected", "true")
    await expect(second).toBeFocused()
    await page.keyboard.press("End")
    await expect(page.getByRole("tab", { name: STEPS[3][0] })).toBeFocused()
  })

  test("step buttons go to the Pilot and How it works pages", async ({ page }) => {
    await openHome(page)
    const panel = page.getByRole("tabpanel")
    await expect(panel.getByRole("link", { name: "Get started" })).toHaveAttribute("href", "/30-day-pilot")
    await expect(panel.getByRole("link", { name: "See how it works" })).toHaveAttribute("href", "/how-it-works")
  })
})
