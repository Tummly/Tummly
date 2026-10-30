import { expect, test } from "@playwright/test"

import { openFooter } from "./marketing-footer"

test("Marketing footer shows the Figma link columns with working targets", async ({ page }) => {
  const footer = await openFooter(page)

  const column = (name: string) => footer.getByRole("navigation", { name, exact: true })

  await expect(column("Product").getByRole("link")).toHaveText([
    "Guest Loop",
    "How it works",
    "QR Materials",
    "AI Assistant",
    "Try demo",
  ])
  await expect(column("Restaurants").getByRole("link")).toHaveText([
    "Pricing",
    "Build your own plan",
    "30-day Pilot",
    "For groups",
    "FAQs",
  ])
  await expect(column("Find us on social").getByRole("link")).toHaveText([
    "Instagram",
    "LinkedIn",
    "Facebook",
  ])
  await expect(column("Company").getByRole("link")).toHaveText([
    "About us",
    "Contact",
    "View company record",
  ])
  await expect(column("Company & Legal").getByRole("link")).toHaveText([
    "Trust and privacy",
    "Privacy Notice",
    "Privacy for Guests",
    "Manage marketing preferences",
    "Accessibility",
    "Terms of Service",
    "Acceptable Use",
    "Shop and Print Terms",
  ])
  await expect(column("Legal").getByRole("link")).toHaveText(["Privacy", "Cookies", "Terms"])

  const companyRecord = column("Company").getByRole("link", { name: "View company record" })
  await expect(companyRecord).toHaveAttribute(
    "href",
    "https://find-and-update.company-information.service.gov.uk/company/16236040"
  )
  await expect(companyRecord).toHaveAttribute("target", "_blank")
  await expect(companyRecord).toHaveAttribute("rel", "noopener noreferrer")

  await expect(footer.getByText("Proud to be a company of")).toBeVisible()
  await expect(
    footer.getByText("Registered office: 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ.")
  ).toBeVisible()
})

test("Marketing footer Start 30-day Pilot opens the Pilot page", async ({ page }) => {
  const footer = await openFooter(page)
  await footer.getByRole("link", { name: "Start 30-day Pilot" }).click()
  await expect(page).toHaveURL(/\/30-day-pilot$/)
})
