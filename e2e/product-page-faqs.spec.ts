import { expect, test } from "@playwright/test"

import { openProductPage, section } from "./product-page"

test("FAQ answers open and close", async ({ page }) => {
  await openProductPage(page)
  const faqs = section(page, "Frequently asked questions")
  const question = faqs.getByRole("button", { name: "Do I need a website?" })
  const answer = faqs.getByText("No. Your restaurant’s Tummly feedback form has its own link", { exact: false })

  await expect(faqs.getByRole("button")).toHaveCount(8)
  await expect(question).toHaveAttribute("aria-expanded", "true")
  await expect(answer).toBeVisible()
  await question.click()
  await expect(question).toHaveAttribute("aria-expanded", "false")
  await expect(answer).toBeHidden()
})
