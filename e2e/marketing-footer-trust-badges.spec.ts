import { expect, test } from "@playwright/test"

import { CORS_HEADERS, TRUST_INDICATORS_FIXTURE } from "./fixtures/publicTrustIndicators"
import { openFooter } from "./marketing-footer"

// ENG01 / E3 and CONTRACT §9: with the release gate closed (no dev preview key)
// no trust block renders, even when the API returns published items, and the
// public endpoint is never requested.
test("Gate closed: no trust block in the footer or on Home, and no trust request", async ({
  page,
}) => {
  let trustRequests = 0
  await page.route("**/api/public/trust-indicators", (route) => {
    trustRequests += 1
    return route.fulfill({ json: TRUST_INDICATORS_FIXTURE, headers: CORS_HEADERS })
  })

  const footer = await openFooter(page)
  await expect(footer.getByText("Proud to be a company of")).toBeVisible()
  await expect(footer.getByText("Built with trust in mind")).toHaveCount(0)
  await expect(footer.getByRole("list", { name: "Trust indicators" })).toHaveCount(0)

  await page.goto("/")
  await expect(page.locator("footer").last().getByText("Proud to be a company of")).toBeVisible()
  await expect(page.getByRole("region", { name: "Trust and privacy credentials" })).toHaveCount(0)

  expect(trustRequests).toBe(0)
})
