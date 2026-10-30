import { expect, test, type Page } from "@playwright/test"

// Figma For Groups `5661:20525` … `6405:36178` (file UP1DqyGGGrxx80Dp7jReTT).
async function openForGroups(page: Page) {
  await page.goto("/for-groups")
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

const section = (page: Page, heading: string | RegExp) =>
  page
    .locator("section")
    .filter({ has: page.getByRole("heading", { name: heading }) })
    .last()

test("sections follow the Figma frame order", async ({ page }) => {
  await openForGroups(page)
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "See where your restaurant group needs attention.",
  )
  await expect(page.locator("section h2")).toHaveText([
    "A group view with the local detail that matters.",
    "Keep the Guest Loop working across your restaurants.",
    "Know where to look closer.",
    "Bring your restaurants together, one at a time.",
    "Give people access that fits their responsibility.",
    "Make the invitation fit the restaurant.",
    "Ask a group question. Get closer to the detail.",
    "Build a plan around your group.",
    "Frequently asked questions",
    "Make customer experience part of your group routine.",
  ])
})

test("eyebrows and intro copy match Figma", async ({ page }) => {
  await openForGroups(page)
  for (const eyebrow of [
    "Tummly for restaurant groups",
    "MANAGING MULTIPLE RESTAURANTS",
    "GROUP-WIDE VISIBILITY",
    "ADDING YOUR RESTAURANTS",
    "TEAM ACCESS",
    "OFFERS & CAMPAIGNS",
    "AI ASSISTANT",
    "GROUP PRICING",
  ]) {
    await expect(page.getByText(eyebrow, { exact: true })).toBeVisible()
  }
  await expect(
    page.getByText(
      "Spot recurring service issues without losing sight of the restaurant behind them.",
      { exact: false },
    ),
  ).toBeVisible()
  await expect(
    page.getByText(
      "Give local managers a clear view of today’s concerns and the next invitation. Build a group plan around the restaurants you run.",
    ),
  ).toBeVisible()
})

test("Managing and Guest Loop cards show their titles and copy", async ({ page }) => {
  await openForGroups(page)
  const managing = section(page, "A group view with the local detail that matters.")
  await expect(managing.getByRole("heading", { level: 3 })).toHaveText([
    "Spot recurring issues",
    "Keep managers focused",
    "Grow at your own pace",
  ])

  const loop = section(page, "Keep the Guest Loop working across your restaurants.")
  await expect(loop.getByRole("heading", { level: 3 })).toHaveText([
    "Local feedback",
    "Considered follow-up",
    "Relevant invitations",
    "Shared visibility",
  ])
  await expect(loop.getByText("Know which restaurant a customer is talking about.")).toBeVisible()
  await expect(loop.getByText("Bring feedback and engagement into a consistent group review.")).toBeVisible()
})

test("feature bands list their three points", async ({ page }) => {
  await openForGroups(page)
  const bands: [string, string[]][] = [
    ["Know where to look closer.", ["Compare restaurants", "Look behind a change", "Focus the review"]],
    [
      "Bring your restaurants together, one at a time.",
      ["Start with one restaurant", "Add more when you're ready", "Keep every restaurant distinct"],
    ],
    [
      "Give people access that fits their responsibility.",
      ["Assigned restaurants", "A focused staff task", "Restricted billing"],
    ],
    [
      "Make the invitation fit the restaurant.",
      ["Choose the restaurant scope", "Match the audience", "Confirm the details"],
    ],
    [
      "Ask a group question. Get closer to the detail.",
      ["Explore a recurring concern", "Check the local detail", "Prepare the follow-up"],
    ],
  ]
  for (const [heading, points] of bands) {
    const band = section(page, heading)
    await expect(band.getByRole("heading", { level: 3 })).toHaveText(points)
    await expect(band.getByRole("img", { name: /./ })).toHaveCount(1)
  }
})

test("FAQ answers load open and toggle", async ({ page }) => {
  await openForGroups(page)
  const faqs = section(page, "Frequently asked questions")
  const questions = faqs.getByRole("button", { name: /\?$/ })
  await expect(questions).toHaveCount(9)
  for (let i = 0; i < 9; i++) {
    await expect(questions.nth(i)).toHaveAttribute("aria-expanded", "true")
  }

  const first = faqs.getByRole("button", { name: "Can I manage all my restaurants in one account?" })
  await first.click()
  await expect(first).toHaveAttribute("aria-expanded", "false")
  await expect(faqs.getByText("Growth includes up to three Locations", { exact: false })).toBeHidden()
  await first.click()
  await expect(first).toHaveAttribute("aria-expanded", "true")
  await expect(faqs.getByText("Have another question?")).toBeVisible()
})

test("20px gaps above Managing and above the bottom CTA plate", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await openForGroups(page)
  const hero = page.locator("section", { has: page.getByRole("heading", { level: 1 }) })
  const managing = section(page, "A group view with the local detail that matters.")
  const faqs = section(page, "Frequently asked questions")
  const plate = section(page, "Make customer experience part of your group routine.").locator("> div")

  const [heroBox, managingBox, faqsBox, plateBox] = await Promise.all(
    [hero, managing, faqs, plate].map((l) => l.boundingBox()),
  )
  expect(Math.round(managingBox!.y - (heroBox!.y + heroBox!.height))).toBe(20)
  expect(Math.round(plateBox!.y - (faqsBox!.y + faqsBox!.height))).toBe(20)
})
