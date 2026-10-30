import { expect, test } from "@playwright/test"

import { openQrMaterials, sectionOf } from "./marketing-qr-materials"

test("sections follow the Figma frame order (5661:19336 … 5661:19499)", async ({ page }) => {
  await openQrMaterials(page)
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Put a simple invitation where customers will see it.",
  )
  const headings = page.getByRole("heading", { level: 2 })
  const order = [
    "Find the right place to start the conversation.",
    "From the right QR to useful customer feedback.",
    "A plan that fits your restaurant.",
    "A Counter Display to help each new Location get started.",
    "Keep each restaurant connected to its customers.",
    "Frequently asked questions",
    "Give customers an easy way to tell you.",
  ]
  const texts = (await headings.allTextContents()).map((t) => t.trim())
  expect(texts.filter((t) => order.includes(t))).toEqual(order)
})

test("hero and Built-for links (Figma 5661:19336, 6853:12456)", async ({ page }) => {
  await openQrMaterials(page)
  const hero = sectionOf(page, /Put a simple invitation/)
  await expect(hero.getByRole("link", { name: "See how it works" })).toHaveAttribute("href", "/how-it-works")
  await expect(hero.getByRole("link", { name: "Get started" })).toBeVisible()
  const built = sectionOf(page, /Keep each restaurant connected/)
  await expect(built.getByRole("link", { name: "See Tummly for groups" })).toHaveAttribute("href", "/for-groups")
  const counter = sectionOf(page, /A Counter Display to help/)
  await expect(counter.getByRole("link", { name: "Explore 30-day Pilot" })).toHaveAttribute("href", "/30-day-pilot")
})

test("four feedback steps (Figma 5661:19391)", async ({ page }) => {
  await openQrMaterials(page)
  const steps = sectionOf(page, /From the right QR to useful customer feedback/)
  for (const title of [
    "Choose your Location",
    "Preview your Guest Form",
    "Place and test your QR",
    "Review customer feedback",
  ]) {
    await expect(steps.getByRole("heading", { name: title })).toBeVisible()
  }
})
