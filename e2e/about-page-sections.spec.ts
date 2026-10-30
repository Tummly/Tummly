import { expect, test } from "@playwright/test"

import { ABOUT_HEADINGS, openAboutPage, scrollThrough, section } from "./about-page"

test("About page sections follow the Figma order", async ({ page }) => {
  await openAboutPage(page)
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(ABOUT_HEADINGS.hero)
  await expect(page.locator("section h2")).toHaveText([
    ABOUT_HEADINGS.industries,
    ABOUT_HEADINGS.principles,
    ABOUT_HEADINGS.reality,
    ABOUT_HEADINGS.mission,
    ABOUT_HEADINGS.company,
    ABOUT_HEADINGS.signUp,
  ])
  // The old two-column ambition block is gone; "Our ambition" now sits under the mission.
  await expect(page.getByRole("heading", { name: "Our ambition" })).toHaveCount(1)
})

test("About page shows the Figma card copy", async ({ page }) => {
  await openAboutPage(page)
  await expect(section(page, ABOUT_HEADINGS.industries).locator("h3")).toHaveText([
    "Cafés & bakeries",
    "Takeaways & QSRs",
    "Casual dining",
    "Growing restaurant groups",
  ])
  await expect(section(page, ABOUT_HEADINGS.principles).locator("h3")).toHaveText([
    "Every guest deserves to be heard",
    "Listening leads to improvement",
    "Guest trust comes first",
    "Give guests a reason to return",
    "Every Location matters",
  ])
  await expect(section(page, ABOUT_HEADINGS.reality).locator("h3")).toHaveText([
    "After an order or visit",
    "When something needs attention",
    "Before the next invitation",
    "At the management review",
  ])
  await expect(section(page, ABOUT_HEADINGS.company).locator("h3")).toHaveText([
    "Company",
    "Registered office",
    "Company and product enquiries",
  ])
  await expect(section(page, ABOUT_HEADINGS.company).getByText("Registered office: 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ.")).toBeVisible()
})

test("About page loads with no console errors or failed requests", async ({ page }) => {
  const problems: string[] = []
  page.on("console", (message) => {
    if (message.type() === "error") problems.push(`console: ${message.text()}`)
  })
  page.on("pageerror", (error) => problems.push(`pageerror: ${error.message}`))
  page.on("requestfailed", (request) => problems.push(`failed: ${request.url()}`))
  page.on("response", (response) => {
    if (response.status() >= 400) problems.push(`${response.status()}: ${response.url()}`)
  })

  await openAboutPage(page)
  await scrollThrough(page)
  await page.waitForLoadState("networkidle")
  expect(problems).toEqual([])
})

test("About page images all load", async ({ page }) => {
  await openAboutPage(page)
  await scrollThrough(page)
  await page.waitForLoadState("networkidle")
  const broken = await page
    .locator("section img")
    .evaluateAll((images: HTMLImageElement[]) =>
      images.filter((image) => !image.complete || image.naturalWidth === 0).map((image) => image.src),
    )
  expect(broken).toEqual([])
})
