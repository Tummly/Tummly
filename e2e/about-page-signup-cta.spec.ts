import { expect, test } from "@playwright/test"

import { ABOUT_HEADINGS, openAboutPage, section } from "./about-page"

test("About sign-up CTA shows the read legal note and its links", async ({ page }) => {
  await openAboutPage(page)
  const cta = section(page, ABOUT_HEADINGS.signUp)
  await expect(cta.getByText("Explore the Guest Loop and decide how it could fit the restaurant you run.")).toBeVisible()
  await expect(cta.getByRole("button", { name: "Continue with Google" })).toBeVisible()
  await expect(cta.getByRole("button", { name: "Continue with Microsoft" })).toBeVisible()
  await expect(
    cta.getByText("Read our Terms of Service and Privacy Notice. You’ll review and accept the required terms during account setup."),
  ).toBeVisible()
  await expect(cta.getByRole("link", { name: "Terms of Service" })).toHaveAttribute("href", "/terms")
  await expect(cta.getByRole("link", { name: "Privacy Notice" })).toHaveAttribute("href", "/privacy")

  await cta.getByRole("link", { name: "Terms of Service" }).click()
  await expect(page).toHaveURL(/\/terms$/)
  await page.goBack()
  await section(page, ABOUT_HEADINGS.signUp).getByRole("link", { name: "Privacy Notice" }).click()
  await expect(page).toHaveURL(/\/privacy$/)
})

test("About sign-up CTA blocks an invalid email and continues to signup with a valid one", async ({ page }) => {
  await openAboutPage(page)
  const cta = section(page, ABOUT_HEADINGS.signUp)
  const email = cta.getByLabel("Work email")

  await email.fill("not-an-email")
  await cta.getByRole("button", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/about$/)
  expect(await email.evaluate((el: HTMLInputElement) => el.validity.valid)).toBe(false)

  await email.fill("verify.about@tummly.test")
  await cta.getByRole("button", { name: "Get started" }).click()
  // Signup reads ?email=, prefills the field, then strips it from the URL.
  await expect(page).toHaveURL(/\/signup/)
  await expect(page.getByRole("heading", { name: "Create your Tummly account" })).toBeVisible()
  await expect(page.getByRole("textbox", { name: /email/i }).first()).toHaveValue("verify.about@tummly.test")
})

for (const provider of ["Google", "Microsoft"] as const) {
  test(`About sign-up CTA starts ${provider} OAuth with a signup return path`, async ({ page }) => {
    // Stub the API start endpoint: the test proves the redirect target, not the provider.
    await page.route("**/api/auth/external/*/start**", (route) =>
      route.fulfill({ status: 200, contentType: "text/html", body: "<p>oauth start stub</p>" }),
    )
    await openAboutPage(page)
    await section(page, ABOUT_HEADINGS.signUp).getByRole("button", { name: `Continue with ${provider}` }).click()
    await expect(page).toHaveURL(
      new RegExp(`/api/auth/external/${provider.toLowerCase()}/start\\?returnPath=%2Fsignup$`),
    )
  })
}
