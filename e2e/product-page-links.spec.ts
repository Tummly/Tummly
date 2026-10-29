import { expect, test } from "@playwright/test"

import { openProductPage, section } from "./product-page"

test("Product page sections follow the Figma order", async ({ page }) => {
  await openProductPage(page)
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Run the whole Guest Loop from one place.",
  )
  await expect(page.locator("main section h2, section h2")).toHaveText([
    "Make it easy to tell you how it went.",
    "Let customers explain in their own words.",
    "Give service concerns the attention they deserve.",
    "See the person behind the latest comment.",
    "Give customers a reason to order or visit again.",
    "Know what deserves your attention.",
    "From a restaurant question to a useful draft.",
    "More useful work from the feedback you collect.",
    "Keep the local detail as your group grows.",
    /Guest trust\.\s*Restaurant control\./,
    "Choose a plan for the way you run your restaurant.",
    "Frequently asked questions",
    "Put the Guest Loop to work in your restaurant.",
  ])
})

test("Product page CTAs link to their pages", async ({ page }) => {
  await openProductPage(page)
  const hero = section(page, "Run the whole Guest Loop from one place.")
  await expect(hero.getByRole("link", { name: "Get started" })).toHaveAttribute("href", "/30-day-pilot")
  await expect(hero.getByRole("link", { name: "See how it works" })).toHaveAttribute("href", "/how-it-works")
  await expect(page.getByRole("link", { name: "Explore Tummly AI" })).toHaveAttribute("href", "/ai-assistant")
  await expect(page.getByRole("link", { name: "Explore For Groups" })).toHaveAttribute("href", "/for-groups")
  await expect(page.getByRole("link", { name: "Explore Trust & Privacy" })).toHaveAttribute("href", "/trust-privacy")
  await expect(page.getByRole("link", { name: "Explore all FAQs" })).toHaveAttribute("href", "/faqs")

  await page.getByRole("link", { name: "See how it works" }).click()
  await expect(page).toHaveURL(/\/how-it-works$/)
})
