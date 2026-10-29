import { test, expect } from "@playwright/test"

// Seed for the Playwright planner/generator agents: the app shell loads.
test.describe("App shell", () => {
  test("home page renders", async ({ page }) => {
    await page.goto("/")
    await expect(page).toHaveTitle("Tummly")
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible()
  })
})
