import { expect, test, type Page } from "@playwright/test"

// Local test logins from the root .env (CLAUDE.md "Verify setup").
type Role = "operator" | "admin" | "support"

function credentials(role: Role) {
  const prefix = `E2E_${role.toUpperCase()}`
  const email = process.env[`${prefix}_EMAIL`]
  const password = process.env[`${prefix}_PASSWORD`]
  test.skip(!email || !password, `${prefix}_EMAIL / _PASSWORD not set in .env`)
  return { email: email!, password: password! }
}

/** Sign in through /login. The operator's trusted-device token skips the Sign-in OTP. */
export async function signIn(page: Page, role: Role) {
  const { email, password } = credentials(role)
  const deviceToken = process.env.E2E_OPERATOR_DEVICE_TOKEN
  if (role === "operator" && deviceToken) {
    await page.addInitScript((token) => {
      localStorage.setItem("deviceToken", token)
    }, deviceToken)
  }

  await page.goto("/login")
  await page.getByRole("textbox", { name: "Work email" }).fill(email)
  await page.getByRole("textbox", { name: "Password" }).fill(password)
  await page.getByRole("button", { name: "Log in" }).click()
  await expect(page).not.toHaveURL(/\/login(\?|$)/)
}
