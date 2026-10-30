import { expect, test, type Locator, type Page } from "@playwright/test"

import { openFooter } from "./marketing-footer"
import { openProductPage, section } from "./product-page"
import {
  CORS_HEADERS,
  TRUST_BOGUS_TITLE,
  TRUST_COMPANY_RECORD_URL,
  TRUST_ICO_REGISTRATION_URL,
  TRUST_INDICATORS_FIXTURE,
  TRUST_INDICATORS_WITHOUT_ICO,
  TRUST_TOOLTIPS,
} from "./fixtures/publicTrustIndicators"

// Public trust indicators (CONTRACT §8, §9). Every test stubs the API with
// `page.route`; the real API is never needed. The trust block renders only when
// the dev preview key is set (`PUBLIC_TRUST_CLAIMS_APPROVED` stays false).
// The owner's API-level checks (approval, roles, evidence links, rollback) are
// backend integration tests, not browser tests; see the task report.

const TRUST_API = "**/api/public/trust-indicators"
const PREVIEW_KEY = "tummly.trustPreview"

type Tile = { title: string; statusLabel: string }

const ICO: Tile = { title: "ICO Data Protection Registration", statusLabel: "Registered" }
const ROADMAP: Tile = { title: "Cyber Essentials", statusLabel: "On our roadmap" }
const GDPR: Tile = { title: "UK GDPR & Data Protection", statusLabel: "Under review" }
const COOKIES: Tile = { title: "Cookie & Privacy Choices", statusLabel: "Verified" }
const COMPANY: Tile = { title: "UK Registered Company", statusLabel: "Verified public record" }
const TILES = [ICO, ROADMAP, GDPR, COOKIES, COMPANY]

const WIDTHS = [320, 375, 390, 768, 1024, 1440] as const
/** Footer tile columns: 2 below 360px, 3 from 360px, 5 from `sm` (640px). */
const FOOTER_COLUMNS: Record<(typeof WIDTHS)[number], number> = {
  320: 2,
  375: 3,
  390: 3,
  768: 5,
  1024: 5,
  1440: 5,
}
const TOUCH_WIDTHS = [320, 375, 390, 768, 1024] as const
const FOOTER_COLUMN_NAVS = ["Product", "Restaurants", "Find us on social", "Company", "Company & Legal"]

type TrustApi = { status: number; body: unknown; calls: number }

const published = (body: unknown): TrustApi => ({ status: 200, body, calls: 0 })

/** Stubs the endpoint. `api` is read on each call, so a test can change it before a reload. */
async function stubTrustApi(page: Page, api: TrustApi) {
  await page.route(TRUST_API, (route) => {
    api.calls += 1
    return route.fulfill({
      status: api.status,
      json: api.body,
      headers: CORS_HEADERS,
    })
  })
}

/** Dev preview on for every navigation in this page. */
async function previewOn(page: Page) {
  await page.addInitScript((key: string) => {
    window.localStorage.setItem(key, "1")
  }, PREVIEW_KEY)
}

async function rejectCookies(page: Page) {
  const reject = page.getByRole("button", { name: "Reject non-essential" })
  if (await reject.isVisible().catch(() => false)) {
    await reject.click()
  }
}

async function openHome(page: Page) {
  await page.goto("/")
  await rejectCookies(page)
  return page.locator("footer").last()
}

const trustList = (footer: Locator) =>
  footer.getByRole("list", { name: "Trust indicators", exact: true })

const tileButton = (scope: Locator, tile: Tile) =>
  scope.getByRole("button", { name: `${tile.title}, ${tile.statusLabel}`, exact: true })

const bubbleOf = (page: Page, tile: Tile) =>
  page.getByRole("dialog", { name: tile.title, exact: true })

/** Moves keyboard focus onto the tile. Shift+Tab then Tab keeps `:focus-visible`. */
async function tabToTile(page: Page, tile: Locator) {
  await tile.focus()
  await page.keyboard.press("Shift+Tab")
  await page.keyboard.press("Tab")
  await expect(tile).toBeFocused()
}

/** Throws unless the element has a box. Keeps the callers free of non-null assertions. */
async function boxOf(locator: Locator) {
  const box = await locator.boundingBox()
  if (box === null) throw new Error("element has no bounding box")
  return box
}

/** The toggletip arrow: the only SVG in the bubble with a polygon (Radix wraps it in a span). */
const arrowOf = (bubble: Locator) => bubble.locator("svg:has(polygon)")

/**
 * The bubble sits just above its tile (side offset 8px plus the 12px arrow, so the gap
 * is at most about 24px) and overlaps it horizontally. A bubble pinned to a page corner
 * fails this.
 */
async function expectBubbleAboveTile(bubble: Locator, trigger: Locator) {
  const box = await boxOf(bubble)
  const tile = await boxOf(trigger)
  const gap = tile.y - (box.y + box.height)
  expect(gap).toBeGreaterThanOrEqual(0)
  expect(gap).toBeLessThanOrEqual(24)
  expect(box.x).toBeLessThan(tile.x + tile.width)
  expect(box.x + box.width).toBeGreaterThan(tile.x)
}

async function expectInsideViewport(page: Page, locator: Locator) {
  const viewportWidth = await page.evaluate(() => document.documentElement.clientWidth)
  const box = await boxOf(locator)
  expect(box.x).toBeGreaterThanOrEqual(0)
  expect(box.x + box.width).toBeLessThanOrEqual(viewportWidth)
}

/** Top edge of each footer tile, to count tiles per row and rows. */
async function tileRowTops(tiles: Locator) {
  return tiles.evaluateAll((nodes) =>
    nodes.map((node) => Math.round(node.getBoundingClientRect().top)),
  )
}

/** Document-relative boxes, so two pages with different heights compare directly. */
async function footerLayout(footer: Locator) {
  const docBox = (locator: Locator) =>
    locator.evaluate((el) => {
      const r = el.getBoundingClientRect()
      return {
        x: Math.round(r.x + window.scrollX),
        y: Math.round(r.y + window.scrollY),
        width: Math.round(r.width),
        height: Math.round(r.height),
      }
    })

  const layout: Record<string, unknown> = {
    logo: await docBox(footer.locator('a[href="/"]').first()),
    pilot: await docBox(footer.getByRole("link", { name: "Start 30-day Pilot" })),
  }
  for (const name of FOOTER_COLUMN_NAVS) {
    layout[name] = await docBox(footer.getByRole("navigation", { name, exact: true }))
  }
  return layout
}

test.describe("footer trust list", () => {
  for (const width of WIDTHS) {
    test(`lists in item order with visible status; bogus item absent at ${width}px`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 900 })
      await previewOn(page)
      await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
      const footer = await openFooter(page)

      const items = trustList(footer).getByRole("listitem")
      await expect(items).toHaveCount(TILES.length)
      for (const [index, tile] of TILES.entries()) {
        const item = items.nth(index)
        await expect(item.getByRole("button")).toHaveAccessibleName(
          `${tile.title}, ${tile.statusLabel}`,
        )
        await expect(item.getByText(tile.statusLabel, { exact: true })).toBeVisible()
      }
      await expect(footer.getByText(TRUST_BOGUS_TITLE)).toHaveCount(0)
    })

    test(`column count is ${FOOTER_COLUMNS[width]} at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 })
      await previewOn(page)
      await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
      const footer = await openFooter(page)

      const tiles = trustList(footer).getByRole("button")
      await expect(tiles).toHaveCount(TILES.length)
      const tops = await tileRowTops(tiles)
      const columns = FOOTER_COLUMNS[width]
      expect(tops.filter((top) => top === tops[0])).toHaveLength(columns)
      expect(new Set(tops).size).toBe(Math.ceil(TILES.length / columns))
    })

    test(`tooltip sits above its tile and inside the viewport for the first and last tile at ${width}px`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 900 })
      await previewOn(page)
      await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
      const footer = await openFooter(page)

      for (const tile of [ICO, COMPANY]) {
        const trigger = tileButton(trustList(footer), tile)
        await trigger.hover()
        const bubble = bubbleOf(page, tile)
        await expect(bubble).toBeVisible()
        await expectBubbleAboveTile(bubble, trigger)
        await expectInsideViewport(page, bubble)
        await expect(arrowOf(bubble)).toBeVisible()
        await expectInsideViewport(page, arrowOf(bubble))
        await page.mouse.move(1, 1)
        await expect(bubble).toBeHidden()
      }
    })
  }

  test("hover opens the tooltip, leaving closes it", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const trigger = tileButton(trustList(footer), ICO)
    await trigger.hover()
    await expect(trigger).toHaveAttribute("aria-expanded", "true")
    await expect(bubbleOf(page, ICO)).toContainText(TRUST_TOOLTIPS.ico)

    await page.mouse.move(1, 1)
    await expect(bubbleOf(page, ICO)).toBeHidden()
    await expect(trigger).toHaveAttribute("aria-expanded", "false")
  })

  test("focus opens the tooltip without moving focus", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const trigger = tileButton(trustList(footer), ICO)
    await tabToTile(page, trigger)
    await expect(bubbleOf(page, ICO)).toBeVisible()
    await expect(trigger).toBeFocused()
  })

  test("Enter moves focus into the CTA; Escape returns focus to the tile and keeps the tooltip closed", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const trigger = tileButton(trustList(footer), ICO)
    await tabToTile(page, trigger)
    await expect(bubbleOf(page, ICO)).toBeVisible()

    await page.keyboard.press("Enter")
    const cta = bubbleOf(page, ICO).getByRole("link", { name: /View registration/ })
    await expect(cta).toBeFocused()

    await page.keyboard.press("Escape")
    await expect(trigger).toBeFocused()
    await expect(bubbleOf(page, ICO)).toHaveCount(0)
  })

  test("Escape on a focus-opened tile closes the tooltip and it stays closed", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const trigger = tileButton(trustList(footer), ICO)
    await tabToTile(page, trigger)
    await expect(bubbleOf(page, ICO)).toBeVisible()

    await page.keyboard.press("Escape")
    await expect(bubbleOf(page, ICO)).toHaveCount(0)
    await expect(trigger).toBeFocused()
  })

  test("ExternalProof CTAs carry the published URL, open in a new tab and say so", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const cases: { tile: Tile; label: RegExp; href: string }[] = [
      { tile: ICO, label: /View registration/, href: TRUST_ICO_REGISTRATION_URL },
      { tile: COMPANY, label: /View company record/, href: TRUST_COMPANY_RECORD_URL },
    ]
    for (const { tile, label, href } of cases) {
      await tileButton(trustList(footer), tile).hover()
      const link = bubbleOf(page, tile).getByRole("link", { name: label })
      await expect(link).toHaveAttribute("href", href)
      await expect(link).toHaveAttribute("target", "_blank")
      await expect(link).toHaveAttribute("rel", "noopener noreferrer")
      await expect(link).toHaveAccessibleName(/opens in a new tab/)
      await page.mouse.move(1, 1)
      await expect(bubbleOf(page, tile)).toBeHidden()
    }
  })

  test("roadmap and under-review tooltips have no link", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    await tileButton(trustList(footer), ROADMAP).hover()
    const roadmap = bubbleOf(page, ROADMAP)
    await expect(roadmap).toContainText("has not yet been assessed")
    await expect(roadmap.getByRole("link")).toHaveCount(0)
    await expect(roadmap.getByRole("button")).toHaveCount(0)

    await tileButton(trustList(footer), GDPR).hover()
    const gdpr = bubbleOf(page, GDPR)
    await expect(gdpr).toContainText(TRUST_TOOLTIPS.ukGdpr)
    await expect(gdpr.getByRole("link")).toHaveCount(0)
    await expect(gdpr.getByRole("button")).toHaveCount(0)
  })

  test("Manage cookie choices opens the real cookie preferences manager", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    await tileButton(trustList(footer), COOKIES).hover()
    await bubbleOf(page, COOKIES).getByRole("button", { name: "Manage cookie choices" }).click()

    const dialog = page.getByRole("dialog", { name: "Cookie settings", exact: true })
    await expect(dialog).toBeVisible()
    await expect(dialog.getByRole("checkbox", { name: "Analytics cookies" })).toBeVisible()
    await expect(dialog.getByRole("button", { name: "Save preferences" })).toBeVisible()
  })

  test("screen-reader names: each item is \"{title}, {status}\" and the list is Trust indicators", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const list = trustList(footer)
    await expect(list).toBeVisible()
    await expect(list).toHaveAccessibleName("Trust indicators")
    for (const [index, tile] of TILES.entries()) {
      await expect(list.getByRole("listitem").nth(index).getByRole("button")).toHaveAccessibleName(
        `${tile.title}, ${tile.statusLabel}`,
      )
    }
  })

  for (const width of TOUCH_WIDTHS) {
    test.describe(`touch at ${width}px`, () => {
      test.use({ hasTouch: true })

      test(`tap opens, outside tap closes at ${width}px`, async ({ page }) => {
        await page.setViewportSize({ width, height: 900 })
        await previewOn(page)
        await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
        const footer = await openFooter(page)

        const trigger = tileButton(trustList(footer), ICO)
        await trigger.tap()
        await expect(trigger).toHaveAttribute("aria-expanded", "true")
        await expect(bubbleOf(page, ICO)).toBeVisible()

        await footer.getByText("© 2026 Tummly.com Ltd. All rights reserved.").tap()
        await expect(bubbleOf(page, ICO)).toHaveCount(0)
      })
    })
  }

  test("200% zoom keeps the tooltip above its tile inside the viewport and the status visible", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 768, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    await page.evaluate(() => {
      document.documentElement.style.zoom = "2"
    })
    const trigger = tileButton(trustList(footer), ICO)
    await trigger.hover()
    const bubble = bubbleOf(page, ICO)
    await expect(bubble).toBeVisible()
    await expectBubbleAboveTile(bubble, trigger)
    await expectInsideViewport(page, bubble)
    await expect(arrowOf(bubble)).toBeVisible()
    await expectInsideViewport(page, arrowOf(bubble))
    const status = trigger.getByText(ICO.statusLabel, { exact: true })
    await expect(status).toBeVisible()
    await expectInsideViewport(page, status)
  })
})

test.describe("trust surfaces share the published titles", () => {
  test("Home, Product and For groups show the same titles as the footer", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openHome(page)
    await expect(trustList(footer).getByRole("button")).toHaveCount(TILES.length)

    const surfaces: { name: string; open: () => Promise<Locator> }[] = [
      {
        name: "Home",
        open: async () => {
          await openHome(page)
          return page.getByRole("region", { name: "Trust and privacy credentials" })
        },
      },
      {
        name: "Product",
        open: async () => {
          await openProductPage(page)
          return section(page, /Guest trust\./)
        },
      },
      {
        name: "For groups",
        open: async () => {
          await page.goto("/for-groups")
          await rejectCookies(page)
          return page
            .locator("section")
            .filter({ has: page.getByText("Built with trust at the heart of Tummly.") })
        },
      },
    ]

    for (const surface of surfaces) {
      const scope = await surface.open()
      for (const tile of TILES) {
        await expect(tileButton(scope, tile), `${surface.name}: ${tile.title}`).toBeVisible()
      }
      await expect(
        scope.getByRole("button", { name: TRUST_BOGUS_TITLE }),
        `${surface.name}: bogus item`,
      ).toHaveCount(0)
    }
  })
})

test.describe("no trust block: empty array, 500 and gate closed", () => {
  test("Home shows no carousel for a closed gate, an empty array or a 500", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    const api = published(TRUST_INDICATORS_FIXTURE)
    await stubTrustApi(page, api)

    // Gate closed: no preview key, so no request and no trust block.
    const plain = await page.context().newPage()
    await plain.setViewportSize({ width: 1440, height: 900 })
    await stubTrustApi(plain, api)
    await openHome(plain)
    await expect(
      plain.getByRole("region", { name: "Trust and privacy credentials" }),
    ).toHaveCount(0)
    expect(api.calls).toBe(0)
    await plain.close()

    await previewOn(page)
    const cases: { name: string; status: number; body: unknown }[] = [
      { name: "empty array", status: 200, body: [] },
      { name: "500", status: 500, body: { success: false, message: "Unavailable." } },
    ]
    for (const testCase of cases) {
      api.status = testCase.status
      api.body = testCase.body
      const footer = await openHome(page)
      await expect(footer.getByText("Built with trust in mind"), testCase.name).toHaveCount(0)
      await expect(
        page.getByRole("region", { name: "Trust and privacy credentials" }),
        testCase.name,
      ).toHaveCount(0)
    }
  })

  for (const width of WIDTHS) {
    test(`footer keeps the brand and link positions with no trust block at ${width}px`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 900 })
      const api = published(TRUST_INDICATORS_FIXTURE)
      await stubTrustApi(page, api)

      // Baseline: gate closed, so the trust block is never rendered.
      const plain = await page.context().newPage()
      await plain.setViewportSize({ width, height: 900 })
      await stubTrustApi(plain, api)
      const baselineFooter = await openFooter(plain)
      await expect(baselineFooter.getByText("Proud to be a company of")).toBeVisible()
      const baseline = await footerLayout(baselineFooter)
      await plain.close()

      const cases: { name: string; preview: boolean; status: number; body: unknown }[] = [
        { name: "empty array", preview: true, status: 200, body: [] },
        { name: "500", preview: true, status: 500, body: { success: false, message: "Unavailable." } },
      ]
      await previewOn(page)
      for (const testCase of cases) {
        api.status = testCase.status
        api.body = testCase.body
        const footer = await openFooter(page)
        await expect(footer.getByText("Proud to be a company of")).toBeVisible()
        await expect(footer.getByText("Built with trust in mind"), testCase.name).toHaveCount(0)
        await expect(trustList(footer), testCase.name).toHaveCount(0)
        expect(await footerLayout(footer), testCase.name).toEqual(baseline)
      }
    })
  }
})

test.describe("cache", () => {
  test("SPA navigation keeps the list; a full reload shows the new published set", async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await previewOn(page)
    const api = published(TRUST_INDICATORS_FIXTURE)
    await stubTrustApi(page, api)

    await openHome(page)
    await expect(tileButton(page.locator("footer").last(), ICO)).toBeVisible()

    await page
      .getByRole("banner")
      .getByRole("navigation", { name: "Main" })
      .getByRole("link", { name: "Product", exact: true })
      .click()
    await expect(page).toHaveURL(/\/product$/)
    await expect(tileButton(page.locator("footer").last(), ICO)).toBeVisible()
    expect(api.calls).toBe(1)

    api.body = TRUST_INDICATORS_WITHOUT_ICO
    await page.reload()
    const footer = page.locator("footer").last()
    await expect(tileButton(footer, ROADMAP)).toBeVisible()
    await expect(tileButton(footer, ICO)).toHaveCount(0)
    expect(api.calls).toBe(2)
  })
})

test.describe("footer corrections", () => {
  test("footer Start 30-day Pilot is the one Pilot CTA and opens signup with Pilot", async ({
    page,
  }) => {
    const pilotPosts: string[] = []
    page.on("request", (request) => {
      if (request.method() === "POST" && /pilot/i.test(request.url())) {
        pilotPosts.push(request.url())
      }
    })
    await previewOn(page)
    await stubTrustApi(page, published(TRUST_INDICATORS_FIXTURE))
    const footer = await openFooter(page)

    const pilot = footer.getByRole("link", { name: "Start 30-day Pilot" })
    await expect(pilot).toHaveCount(1)
    await pilot.click()

    await expect(page).toHaveURL(/\/signup\?plan=Pilot$/)
    // The signup page keeps the chosen plan in sessionStorage and shows no plan label.
    await expect
      .poll(() => page.evaluate(() => sessionStorage.getItem("tummly.signupPlanIntent")))
      .toContain('"plan":"Pilot"')
    expect(pilotPosts).toEqual([])
  })

  test("disclosure sits above the copyright line", async ({ page }) => {
    const footer = await openFooter(page)
    const operator = footer.getByText(
      "Tummly is operated by TUMMLY.COM LIMITED, registered in England and Wales (Company No. 16236040).",
    )
    const office = footer.getByText(
      "Registered office: 71-75 Shelton Street, Covent Garden, London, WC2H 9JQ.",
    )
    const copyright = footer.getByText("© 2026 Tummly.com Ltd. All rights reserved.")
    await expect(operator).toBeVisible()
    await expect(office).toBeVisible()

    const operatorBox = await boxOf(operator)
    const officeBox = await boxOf(office)
    const copyrightBox = await boxOf(copyright)
    expect(operatorBox.y).toBeLessThan(officeBox.y)
    expect(officeBox.y).toBeLessThan(copyrightBox.y)
  })

  test("Manage marketing preferences sits directly below Privacy for Guests and opens /preferences", async ({
    page,
  }) => {
    const footer = await openFooter(page)
    const legal = footer.getByRole("navigation", { name: "Company & Legal", exact: true })
    const names = await legal.getByRole("link").allTextContents()
    const guest = names.indexOf("Privacy for Guests")
    const preferences = names.indexOf("Manage marketing preferences")
    expect(guest).toBeGreaterThanOrEqual(0)
    expect(preferences).toBe(guest + 1)

    const guestBox = await boxOf(legal.getByRole("link", { name: "Privacy for Guests", exact: true }))
    const preferencesBox = await boxOf(
      legal.getByRole("link", { name: "Manage marketing preferences", exact: true }),
    )
    expect(preferencesBox.y).toBeGreaterThan(guestBox.y)

    await legal.getByRole("link", { name: "Manage marketing preferences", exact: true }).click()
    await expect(page).toHaveURL(/\/preferences$/)
  })

  test("the header and the footer both show For groups", async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    const footer = await openHome(page)

    await expect(
      page
        .getByRole("banner")
        .getByRole("navigation", { name: "Main" })
        .getByRole("link", { name: "For groups", exact: true }),
    ).toBeVisible()
    await expect(
      footer
        .getByRole("navigation", { name: "Restaurants", exact: true })
        .getByRole("link", { name: "For groups", exact: true }),
    ).toBeVisible()
  })

  test("the footer shows no VAT", async ({ page }) => {
    const footer = await openFooter(page)
    await expect(footer.getByText("Proud to be a company of")).toBeVisible()
    await expect(footer).not.toContainText(/\bVAT\b/)
  })
})
