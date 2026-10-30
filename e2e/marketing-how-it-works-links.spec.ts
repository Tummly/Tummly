import { expect, test } from "@playwright/test"

import { openHowItWorks, sectionOf } from "./marketing-how-it-works"

test("steps buttons go to Pilot signup and the Pilot page (Figma 6865:35312)", async ({ page }) => {
  await openHowItWorks(page)
  const section = sectionOf(page, /Get the Guest Loop started/)
  await expect(section.getByRole("link", { name: "Get started" })).toHaveAttribute("href", "/signup?plan=Pilot")
  await expect(section.getByRole("link", { name: "Explore 30-day pilot" })).toHaveAttribute("href", "/30-day-pilot")
})

test("feature rows link to signup and Tummly AI (Figma 6870:35827 … 6871:35965)", async ({ page }) => {
  await openHowItWorks(page)
  for (const heading of [/Read the experience/, /Keep the customer context/, /relevant invitation/, /Learn from the response/, /Let AI help/]) {
    const section = sectionOf(page, heading)
    await expect(section.getByRole("link", { name: "Get started" })).toHaveAttribute("href", "/signup?plan=Pilot")
  }
  await expect(sectionOf(page, /Let AI help/).getByRole("link", { name: "See Tummly AI" })).toHaveAttribute("href", "/ai-assistant")
})

test("plan cards show prices and plan links (Figma 5661:19770)", async ({ page }) => {
  await openHowItWorks(page)
  const section = sectionOf(page, /Choose how you would like to start/)
  await section.scrollIntoViewIfNeeded()
  for (const price of ["£0", "£39", "£99", "£199"]) {
    await expect(section.getByText(price, { exact: true })).toBeVisible()
  }
  await expect(section.getByRole("link", { name: "Choose Starter" })).toHaveAttribute("href", /\/signup\?plan=Starter/)
  await expect(section.getByRole("link", { name: "Choose Growth" })).toHaveAttribute("href", /\/signup\?plan=Growth/)
  await expect(section.getByRole("link", { name: "Choose Group" })).toHaveAttribute("href", /\/signup\?plan=Group/)
  await expect(section.getByRole("link", { name: "Compare all plans" })).toHaveAttribute("href", "/pricing")
})

test("plan price error offers a retry", async ({ page }) => {
  await page.route("**/commercial-catalog/custom-unit-rates", (route) => route.fulfill({ status: 500, body: "{}" }))
  await openHowItWorks(page)
  const section = sectionOf(page, /Choose how you would like to start/)
  await section.scrollIntoViewIfNeeded()
  await expect(section.getByRole("button", { name: "Try again" })).toBeVisible()
})
