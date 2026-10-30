import { expect, test, type Locator, type Page } from "@playwright/test"

// Breakpoints 375 / 768 / 1024 / 1440 (CLAUDE.md); 1024 is Tailwind `lg`.
async function openForGroups(page: Page, width: number) {
  await page.setViewportSize({ width, height: 900 })
  await page.goto("/for-groups")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

const section = (page: Page, heading: string) =>
  page.locator("section").filter({ has: page.getByRole("heading", { name: heading }) }).last()

async function columns(items: Locator) {
  const boxes = await Promise.all((await items.all()).map((item) => item.boundingBox()))
  return new Set(boxes.map((box) => Math.round(box!.y))).size === 1
    ? boxes.length
    : new Set(boxes.map((box) => Math.round(box!.x))).size
}

for (const width of [375, 768, 1024, 1440]) {
  test(`no horizontal overflow at ${width}px`, async ({ page }) => {
    await openForGroups(page, width)
    await page.locator("footer").last().scrollIntoViewIfNeeded()
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth)
    expect(scrollWidth).toBeLessThanOrEqual(width)
  })
}

for (const [width, managing, loop] of [
  [375, 1, 1],
  [768, 1, 2],
  [1024, 3, 2],
  [1440, 3, 4],
] as const) {
  test(`card grids use ${managing} and ${loop} columns at ${width}px`, async ({ page }) => {
    await openForGroups(page, width)
    const managingCards = section(page, "A group view with the local detail that matters.").locator("article")
    const loopCards = section(page, "Keep the Guest Loop working across your restaurants.").locator("article")
    expect(await columns(managingCards)).toBe(managing)
    expect(await columns(loopCards)).toBe(loop)
  })
}

for (const [width, sideBySide] of [
  [768, false],
  [1024, true],
  [1440, true],
] as const) {
  test(`feature bands ${sideBySide ? "sit beside" : "stack under"} their media at ${width}px`, async ({ page }) => {
    await openForGroups(page, width)
    const band = section(page, "Know where to look closer.")
    const media = await band.getByRole("img", { name: /Group reporting dashboard/ }).boundingBox()
    const heading = await band.getByRole("heading", { level: 2 }).boundingBox()
    const overlapsVertically = heading!.y < media!.y + media!.height && media!.y < heading!.y + heading!.height
    expect(overlapsVertically).toBe(sideBySide)
  })
}

test("Locations stepper sits beside its label at 1440px", async ({ page }) => {
  await openForGroups(page, 1440)
  const pricing = section(page, "Build a plan around your group.")
  await pricing.scrollIntoViewIfNeeded()
  const label = await pricing.getByText("Restaurant Locations").boundingBox()
  const plus = await pricing.getByRole("button", { name: "Increase Locations" }).boundingBox()
  expect(plus!.y).toBeLessThan(label!.y + label!.height)
})
