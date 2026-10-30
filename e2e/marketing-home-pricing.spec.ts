import { expect, test } from "@playwright/test"

import { openHome, sectionOf } from "./marketing-home"

// Figma 4902:19866. Prices come from the published catalogue (local API).
test("pricing strip shows four plans with their signup links", async ({ page }) => {
  await openHome(page)
  const section = sectionOf(page, /Start with a Pilot/)
  await section.scrollIntoViewIfNeeded()
  await expect(section.getByRole("link", { name: "Compare all plans" })).toHaveAttribute("href", "/pricing")
  await expect(section.getByRole("link", { name: "Start 30-day Pilot" })).toHaveAttribute("href", "/30-day-pilot")
  for (const plan of ["Starter", "Growth", "Group"]) {
    await expect(section.getByRole("link", { name: `Choose ${plan}` })).toHaveAttribute("href", `/signup?plan=${plan}&cadence=monthly`)
  }
  await expect(section.getByText("£39")).toBeVisible()
})

test("pricing shows an error state when the catalogue fails", async ({ page }) => {
  await page.route("**/public/commercial-catalog/**", (route) =>
    route.fulfill({ status: 500, contentType: "application/json", body: "{}" }),
  )
  await openHome(page)
  const section = sectionOf(page, /Start with a Pilot/)
  await section.scrollIntoViewIfNeeded()
  await expect(section.getByText("£39")).toHaveCount(0)
  await expect(section.getByRole("button", { name: /retry|try again/i })).toBeVisible()
})
