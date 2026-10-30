import { expect, test, type Page, type Route } from "@playwright/test"

import customUnitRates from "./fixtures/public-custom-unit-rates.json" with { type: "json" }

// Figma For Groups pricing `5661:22463`. Plan prices come from the public
// catalog and the builder total from the server quote; both are stubbed.
const CORS = { "Access-Control-Allow-Origin": "*" }
const BASE_PENCE = { monthly: 19900, annual: 203000 }
const EXTRA_LOCATION_PENCE = { monthly: 3900, annual: 39800 }

type Cadence = keyof typeof BASE_PENCE

function quoteItem(locations: number, cadence: Cadence) {
  const extra = Math.max(0, locations - 5)
  const lines = [
    { key: "base:group", label: "Group · 5 included Locations", name: "Group", quantity: 1, netPence: BASE_PENCE[cadence] },
  ]
  if (extra > 0) {
    lines.push({
      key: "extra:location:band_1",
      label: `${extra} additional Location${extra === 1 ? "" : "s"}`,
      name: "Additional Locations",
      quantity: extra,
      netPence: extra * EXTRA_LOCATION_PENCE[cadence],
    })
  }
  const periodNetPence = lines.reduce((sum, line) => sum + line.netPence, 0)
  return {
    version: 2,
    cadence,
    basePlan: "group",
    locations,
    packs: {},
    quantities: { locations },
    monthlyNetPence: periodNetPence,
    periodNetPence,
    lines,
    storedAsPlan: "Group",
    vatRateBps: 0,
    quoteHash: `test-${locations}-${cadence}`,
  }
}

async function openForGroups(
  page: Page,
  { quote = "ok" }: { quote?: "ok" | "error" | ((route: Route) => Promise<void>) } = {},
) {
  await page.route("**/api/public/commercial-catalog/custom-unit-rates", (route) =>
    route.fulfill({ json: customUnitRates, headers: CORS }),
  )
  await page.route("**/api/public/commercial-catalog/custom-plan/quote", async (route) => {
    if (route.request().method() === "OPTIONS") {
      await route.fulfill({ status: 204, headers: { ...CORS, "Access-Control-Allow-Headers": "*" } })
      return
    }
    if (typeof quote === "function") {
      await quote(route)
      return
    }
    if (quote === "error") {
      await route.fulfill({ status: 500, json: { success: false, message: "Quote failed." }, headers: CORS })
      return
    }
    const body = route.request().postDataJSON() as { locations: number; cadence: Cadence }
    await route.fulfill({ json: { success: true, item: quoteItem(body.locations, body.cadence) }, headers: CORS })
  })
  await page.goto("/for-groups")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
  const pricing = page
    .locator("section")
    .filter({ has: page.getByRole("heading", { name: "Build a plan around your group." }) })
  await pricing.scrollIntoViewIfNeeded()
  return pricing
}

test("Group plan shows its price, allowances and an included-Locations quote", async ({ page }) => {
  const pricing = await openForGroups(page)

  await expect(pricing.getByText("£199", { exact: true }).first()).toBeVisible()
  await expect(pricing.getByText("£2,030/year")).toBeVisible()
  await expect(pricing.getByText("Up to 50 across your account")).toBeVisible()
  await expect(pricing.getByText("Priority + assisted onboarding")).toBeVisible()
  await expect(pricing.getByText("5 included in Group")).toBeVisible()
  await expect(pricing.getByText("Group · 5 included Locations")).toBeVisible()
  await expect(pricing.getByText("/ per month")).toBeVisible()
  await expect(pricing.getByRole("button", { name: "Decrease Locations" })).toBeDisabled()
  await expect(pricing.getByRole("link", { name: "Continue with Group" })).toHaveAttribute(
    "href",
    "/signup?plan=Group&cadence=monthly",
  )
})

test("adding a Location updates the quote and the Continue intent", async ({ page }) => {
  const pricing = await openForGroups(page)

  await pricing.getByRole("button", { name: "Increase Locations" }).click()
  await expect(pricing.getByText("6", { exact: true })).toBeVisible()
  await expect(pricing.getByRole("button", { name: "Decrease Locations" })).toBeEnabled()
  await expect(pricing.getByText("1 additional Location")).toBeVisible()
  await expect(pricing.getByText("£238", { exact: true })).toBeVisible()
  const continueLink = pricing.getByRole("link", { name: "Continue with Group" })
  await expect(continueLink).toHaveAttribute("href", /\/signup\?plan=Group&cadence=monthly&customIntent=/)

  await pricing.getByRole("button", { name: "Decrease Locations" }).click()
  await expect(pricing.getByText("1 additional Location")).toBeHidden()
  await expect(continueLink).toHaveAttribute("href", "/signup?plan=Group&cadence=monthly")
})

test("Annual billing shows the yearly total and keeps the cadence on Continue", async ({ page }) => {
  const pricing = await openForGroups(page)
  const annual = pricing.getByRole("button", { name: /Annual/ })

  await expect(annual).toContainText("SAVE 15%")
  await annual.click()
  await expect(annual).toHaveAttribute("aria-pressed", "true")
  await expect(pricing.getByRole("button", { name: "Monthly" })).toHaveAttribute("aria-pressed", "false")
  // The plan line and the estimated total.
  await expect(pricing.getByText("£2,030", { exact: true })).toHaveCount(2)
  await expect(pricing.getByText("/ per year")).toBeVisible()
  await expect(pricing.getByRole("link", { name: "Continue with Group" })).toHaveAttribute(
    "href",
    "/signup?plan=Group&cadence=annual",
  )
})

test("a failed quote shows the retry message and holds Continue", async ({ page }) => {
  const pricing = await openForGroups(page, { quote: "error" })

  await expect(pricing.getByText("We could not load a current quote.", { exact: false })).toBeVisible()
  await expect(pricing.getByRole("button", { name: "Try again" })).toBeVisible()
  await expect(pricing.getByRole("button", { name: "Continue with Group" })).toBeDisabled()
  await expect(pricing.getByRole("link", { name: "Contact us" })).toBeVisible()
})

test("a slow quote shows the updating message until it loads", async ({ page }) => {
  let release!: () => void
  const held = new Promise<void>((resolve) => {
    release = resolve
  })
  const pricing = await openForGroups(page, {
    quote: async (route) => {
      await held
      await route.fulfill({ json: { success: true, item: quoteItem(5, "monthly") }, headers: CORS })
    },
  })

  await expect(pricing.getByText("Updating your quote…")).toBeVisible()
  await expect(pricing.getByRole("button", { name: "Continue with Group" })).toBeDisabled()
  release()
  await expect(pricing.getByText("Updating your quote…")).toBeHidden()
  await expect(pricing.getByRole("link", { name: "Continue with Group" })).toBeVisible()
})
