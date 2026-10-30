import { expect, test } from "@playwright/test"

import { openHowItWorks } from "./marketing-how-it-works"

const STEPS = ["Share your experience", "Add your details", "Choose whether to hear more", "See the thank-you"]

for (const [width, height] of [[375, 812], [768, 1024], [1440, 900]]) {
  test(`guest steps select by click and keyboard at ${width} (Figma 5887:20699)`, async ({ page }) => {
    await page.setViewportSize({ width, height })
    await openHowItWorks(page)
    const list = page.getByRole("tablist", { name: "Guest Form steps" })
    await list.scrollIntoViewIfNeeded()
    const tabs = list.getByRole("tab")
    await expect(tabs).toHaveCount(4)
    for (const index of [1, 2, 3, 0]) {
      await tabs.nth(index).click()
      await expect(tabs.nth(index)).toHaveAttribute("aria-selected", "true")
      await expect(tabs.nth(index)).toContainText(STEPS[index])
      const panelId = await tabs.nth(index).getAttribute("aria-controls")
      await expect(page.locator(`[id="${panelId}"]`)).toBeVisible()
      await expect(page.locator(`img[src*="guest-form-step-0${index + 1}"]`).last()).toBeVisible()
    }
    await tabs.nth(0).focus()
    await page.keyboard.press("ArrowDown")
    await expect(tabs.nth(1)).toHaveAttribute("aria-selected", "true")
    await expect(tabs.nth(1)).toBeFocused()
    await page.keyboard.press("End")
    await expect(tabs.nth(3)).toHaveAttribute("aria-selected", "true")
    await page.keyboard.press("Home")
    await expect(tabs.nth(0)).toHaveAttribute("aria-selected", "true")
  })
}
