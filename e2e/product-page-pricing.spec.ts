import { expect, test } from "@playwright/test"

import { openProductPage, section } from "./product-page"

const HEADING = "Choose a plan for the way you run your restaurant."

test("Pricing shows plan prices and plan links", async ({ page }) => {
  await openProductPage(page)
  const pricing = section(page, HEADING)

  await expect(pricing.getByText("£0")).toBeVisible()
  await expect(pricing.getByText("£39")).toBeVisible()
  await expect(pricing.getByText("£99")).toBeVisible()
  await expect(pricing.getByText("£199")).toBeVisible()

  await expect(pricing.getByRole("link", { name: "Start 30-day Pilot" })).toHaveAttribute("href", "/30-day-pilot")
  await expect(pricing.getByRole("link", { name: "Choose Starter" })).toHaveAttribute("href", "/signup?plan=Starter&cadence=monthly")
  await expect(pricing.getByRole("link", { name: "Choose Growth" })).toHaveAttribute("href", "/signup?plan=Growth&cadence=monthly")
  await expect(pricing.getByRole("link", { name: "Choose Group" })).toHaveAttribute("href", "/signup?plan=Group&cadence=monthly")
  await expect(pricing.getByRole("link", { name: "Compare all plans" })).toHaveAttribute("href", "/pricing")
})

test("Pricing shows a retry message when the catalog fails", async ({ page }) => {
  await openProductPage(page, { catalog: "error" })
  const pricing = section(page, HEADING)

  await expect(pricing.getByText("We could not load a current quote.", { exact: false })).toBeVisible()
  await expect(pricing.getByRole("button", { name: "Try again" })).toBeVisible()
  await expect(pricing.getByRole("link", { name: "Choose Growth" })).toBeVisible()
})
