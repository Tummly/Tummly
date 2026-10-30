import { expect, test } from "@playwright/test"

import { openFooter } from "./marketing-footer"

// ENG01 / E3: the badge grid stays hidden until the Director approves the claims.
test("Marketing footer withholds the trust badge grid", async ({ page }) => {
  const footer = await openFooter(page)
  await expect(footer.getByText("Built with trust in mind")).toHaveCount(0)
  await expect(footer.getByRole("list", { name: "Trust badges" })).toHaveCount(0)
})
