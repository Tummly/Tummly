import { expect, test } from "@playwright/test"

import { openQrMaterials, sectionOf } from "./marketing-qr-materials"

test("QR format list changes the photo and caption (Figma 5661:19364)", async ({ page }) => {
  await openQrMaterials(page)
  const section = sectionOf(page, /Find the right place to start the conversation/)
  await section.scrollIntoViewIfNeeded()
  const counter = section.getByRole("button", { name: "QR Counter Display" })
  await expect(counter).toHaveAttribute("aria-pressed", "true")
  const tent = section.getByRole("button", { name: "QR Table Tent" })
  await tent.click()
  await expect(tent).toHaveAttribute("aria-pressed", "true")
  await expect(counter).toHaveAttribute("aria-pressed", "false")
  await expect(section.getByText("Table display that makes private Feedback easy to share while dining.").filter({ visible: true })).toBeVisible()
})
