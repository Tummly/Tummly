import { createElement } from "react"
import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { CustomPlanSummaryCard } from "@/components/marketing/custom-plan/CustomPlanSummaryCard"

const MONTHLY_QUOTE: CustomPlanQuoteResult = {
  version: 2,
  catalogPublicId: "cat-1",
  cadence: "monthly",
  basePlan: "starter",
  locations: 1,
  packs: {},
  quantities: {
    locations: 1,
    teamUsers: 3,
    qrPlacements: 10,
    qrPlacementsAccountExtra: 0,
    offers: 3,
    emailCredits: 2500,
    smsCredits: 100,
    aiCredits: 100,
  },
  workspaceQrPool: 10,
  monthlyNetPence: 3900,
  periodNetPence: 3900,
  lines: [
    {
      key: "base:starter",
      label: "Starter · 1 included Location",
      name: "Starter",
      quantity: 1,
      unitNetPence: 3900,
      netPence: 3900,
    },
  ],
  basePlanChanged: false,
  requestedBasePlan: null,
  storedAsPlan: "Starter",
  quoteHash: "0123456789abcdef0123456789abcdef",
  vatRateBps: 0,
  vatAmountPence: 0,
  grossTotalPence: 3900,
  recurringGrossPence: 3900,
  dueNowGrossPence: 3900,
  effectiveTiming: "on_payment_confirmed",
}

function renderCard(cadence: "monthly" | "annual", quote = MONTHLY_QUOTE) {
  return renderToStaticMarkup(
    createElement(
      MemoryRouter,
      null,
      createElement(CustomPlanSummaryCard, {
        quote,
        cadence,
        // The cadence toggle renders only with a change handler.
        onCadenceChange: () => undefined,
      }),
    ),
  )
}

function cadenceButton(html: string, label: string): string {
  const buttons = html.match(/<button[^>]*>[\s\S]*?<\/button>/g) ?? []
  return buttons.find((button) => button.includes(label)) ?? ""
}

describe("CustomPlanSummaryCard total", () => {
  it("uses the quote cadence for the total suffix, not the selected toggle", () => {
    const html = renderCard("annual")
    expect(html).toContain("£39")
    expect(html).toContain("/ per month")
    expect(html).not.toContain("/ per year")
  })

  it("keeps the selected toggle highlight on the chosen cadence", () => {
    const html = renderCard("annual")
    expect(cadenceButton(html, "Annual")).toContain('aria-pressed="true"')
    expect(cadenceButton(html, "Monthly")).toContain('aria-pressed="false"')
  })
})

describe("CustomPlanSummaryCard Active QR", () => {
  it("shows the server Workspace pool, not a per-Location figure", () => {
    const html = renderCard("monthly", {
      ...MONTHLY_QUOTE,
      basePlan: "group",
      locations: 6,
      quantities: {
        ...MONTHLY_QUOTE.quantities,
        qrPlacements: 50,
        qrPlacementsAccountExtra: 2,
      },
      workspaceQrPool: 62,
    })
    expect(html).toContain("62 in your Workspace")
    expect(html).not.toContain("per Location")
  })
})

describe("CustomPlanSummaryCard breakdown", () => {
  const UNIT_LINES: CustomPlanQuoteResult["lines"] = [
    ...MONTHLY_QUOTE.lines,
    {
      key: "pack:team_user",
      label: "Additional team users × 7",
      name: "Additional team users",
      quantity: 7,
      unitNetPence: 300,
      netPence: 2100,
    },
  ]
  const VAT_QUOTE: CustomPlanQuoteResult = {
    ...MONTHLY_QUOTE,
    cadence: "annual",
    lines: UNIT_LINES,
    periodNetPence: 10000,
    vatRateBps: 2000,
    vatAmountPence: 2000,
    grossTotalPence: 12000,
    recurringGrossPence: 12000,
    dueNowGrossPence: 12000,
  }
  const NO_VAT_QUOTE: CustomPlanQuoteResult = {
    ...MONTHLY_QUOTE,
    lines: UNIT_LINES,
    periodNetPence: 6000,
    grossTotalPence: 6000,
    recurringGrossPence: 6000,
    dueNowGrossPence: 6000,
  }

  function renderWith(quote: CustomPlanQuoteResult, showBreakdown: boolean) {
    return renderToStaticMarkup(
      createElement(
        MemoryRouter,
        null,
        createElement(CustomPlanSummaryCard, {
          quote,
          cadence: quote.cadence === "annual" ? "annual" : "monthly",
          showBreakdown,
        }),
      ),
    )
  }

  /** Every `<dt>`/`<dd>` pair in order, as plain text. */
  function pairs(html: string): [string, string][] {
    return [...html.matchAll(/<dt[^>]*>(.*?)<\/dt><dd[^>]*>(.*?)<\/dd>/g)].map(
      (match) => [match[1], match[2]],
    )
  }

  it("shows each summary row with its exact label and server value", () => {
    expect(pairs(renderWith(VAT_QUOTE, true))).toEqual([
      ["Quantity", "1"],
      ["Price per unit", "£39"],
      ["Line total", "£39"],
      ["Quantity", "7"],
      ["Price per unit", "£3"],
      ["Line total", "£21"],
      ["Billing period", "Annual"],
      ["Total before VAT", "£100"],
      ["VAT", "£20"],
      ["Updated total", "£120"],
      ["Effective date", "When payment is confirmed"],
      ["New recurring amount", "£120 / per year"],
      ["Amount due today", "£120"],
    ])
  })

  it("shows name, quantity, price per unit and line total for each line", () => {
    const html = renderWith(NO_VAT_QUOTE, true)
    expect(html).toContain("Additional team users")
    expect(pairs(html).slice(3, 6)).toEqual([
      ["Quantity", "7"],
      ["Price per unit", "£3"],
      ["Line total", "£21"],
    ])
  })

  it("leaves out the VAT rows and all VAT wording when VAT is off", () => {
    const html = renderWith(NO_VAT_QUOTE, true)
    const labels = pairs(html).map(([label]) => label)
    expect(labels).not.toContain("VAT")
    expect(labels).not.toContain("Total before VAT")
    expect(html).not.toContain("VAT")
    expect(pairs(html).find(([label]) => label === "Updated total")).toEqual([
      "Updated total",
      "£60",
    ])
  })

  it("shows a net figure only as Total before VAT, never as a second total", () => {
    const html = renderWith(VAT_QUOTE, true)
    expect(html).not.toContain("Estimated total")
    const totals = pairs(html).filter(([label]) => /total/i.test(label) && label !== "Line total")
    expect(totals).toEqual([
      ["Total before VAT", "£100"],
      ["Updated total", "£120"],
    ])
    expect(renderWith(NO_VAT_QUOTE, true)).not.toContain("Estimated total")
  })

  it("keeps the Estimated total when there is no breakdown", () => {
    const html = renderWith(VAT_QUOTE, false)
    expect(html).toContain("Estimated total")
  })

  it("is off unless asked for", () => {
    expect(renderWith(VAT_QUOTE, false)).not.toContain("Quote breakdown")
  })
})
