import { expect, test, type Page } from "@playwright/test"

async function openForGroups(page: Page) {
  await page.goto("/for-groups")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

const hero = (page: Page) =>
  page.locator("section").filter({ has: page.getByRole("heading", { level: 1 }) })

// Figma Sign up `6405:36178`: editorial plate (dark type), "read" legal note.
const bottomCta = (page: Page) =>
  page
    .locator("section")
    .filter({ has: page.getByRole("heading", { name: "Make customer experience part of your group routine." }) })

test("hero form rejects an invalid email and continues to Pilot signup", async ({ page }) => {
  await openForGroups(page)
  const form = hero(page)
  const email = form.getByLabel(/email/i)

  await expect(form.getByRole("button", { name: "Continue with Google" })).toBeVisible()
  await expect(form.getByRole("button", { name: "Continue with Microsoft" })).toBeVisible()
  await expect(form.getByText("Read our", { exact: false })).toBeVisible()

  await email.fill("not-an-email")
  await form.getByRole("button", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/for-groups$/)
  expect(await email.evaluate((el: HTMLInputElement) => el.validity.valid)).toBe(false)

  await email.fill("owner@example.com")
  await form.getByRole("button", { name: "Get started" }).click()
  await expect(page).toHaveURL(/\/signup\?.*plan=Pilot/)
})

test("bottom CTA uses the editorial variant and continues to Group signup", async ({ page }) => {
  await openForGroups(page)
  const cta = bottomCta(page)
  const submit = cta.getByRole("button", { name: "Get started" })
  await submit.scrollIntoViewIfNeeded()

  await expect(cta.getByText("You’ll review and accept the required terms during account setup.", { exact: false })).toBeVisible()
  await expect(submit).toHaveCSS("background-color", "rgb(20, 20, 20)")
  await expect(cta.getByRole("button", { name: "Continue with Google" })).toHaveCSS("background-color", "rgb(250, 250, 250)")
  await expect(cta.locator("> div")).toHaveCSS("border-radius", "12px")

  const email = cta.getByLabel(/email/i)
  await email.fill("not-an-email")
  await submit.click()
  await expect(page).toHaveURL(/\/for-groups$/)

  await email.fill("owner@example.com")
  await submit.click()
  await expect(page).toHaveURL(/\/signup\?.*plan=Group/)
  await expect(page).toHaveURL(/cadence=monthly/)
})
