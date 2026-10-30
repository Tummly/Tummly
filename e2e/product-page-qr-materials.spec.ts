import { expect, test } from "@playwright/test"

import { openProductPage, section } from "./product-page"

test("QR materials list switches the shown material", async ({ page }) => {
  await openProductPage(page)
  const qr = section(page, "Make it easy to tell you how it went.")
  const counter = qr.getByRole("button", { name: "QR Counter Display" })
  const cards = qr.getByRole("button", { name: "QR Guest Cards" })

  await expect(counter).toHaveAttribute("aria-pressed", "true")
  await expect(qr.getByText("Freestanding QR display for tills, counters and collection points.")).toBeVisible()

  await cards.click()
  await expect(cards).toHaveAttribute("aria-pressed", "true")
  await expect(counter).toHaveAttribute("aria-pressed", "false")
  await expect(qr.getByText("Compact cards for takeaway, delivery and counter handout.")).toBeVisible()

  await qr.getByRole("button", { name: "QR Tray Liner" }).focus()
  await page.keyboard.press("Enter")
  await expect(qr.getByRole("button", { name: "QR Tray Liner" })).toHaveAttribute("aria-pressed", "true")
})
