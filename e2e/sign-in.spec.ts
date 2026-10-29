import { expect, test } from "@playwright/test"

import { signIn } from "./auth"

const landing = {
  operator: "/single-dashboard",
  admin: "/admin-dashboard/admin",
  support: "/admin-dashboard/support",
} as const

test.describe("Sign-in with the local test logins", () => {
  for (const [role, path] of Object.entries(landing) as [keyof typeof landing, string][]) {
    test(`${role} lands on ${path}`, async ({ page }) => {
      await signIn(page, role)
      await expect(page).toHaveURL(new RegExp(`${path}(\\?|$)`))
    })
  }
})
