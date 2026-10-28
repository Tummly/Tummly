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
  lines: [{ key: "base:starter", label: "Starter", quantity: 1, netPence: 3900 }],
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
  const VAT_QUOTE: CustomPlanQuoteResult = {
    ...MONTHLY_QUOTE,
    cadence: "annual",
    periodNetPence: 10000,
    vatRateBps: 2000,
    vatAmountPence: 2000,
    grossTotalPence: 12000,
    recurringGrossPence: 12000,
    dueNowGrossPence: 12000,
  }

  function renderWith(showBreakdown: boolean) {
    return renderToStaticMarkup(
      createElement(
        MemoryRouter,
        null,
        createElement(CustomPlanSummaryCard, {
          quote: VAT_QUOTE,
          cadence: "annual",
          showBreakdown,
        }),
      ),
    )
  }

  it("shows the server VAT, gross, due now, recurring amount and start", () => {
    const html = renderWith(true)
    expect(html).toContain("Quote breakdown")
    expect(html).toContain("Annual")
    expect(html).toContain("Total before VAT")
    expect(html).toContain("£100")
    expect(html).toContain("£20")
    expect(html).toContain("Total including VAT")
    expect(html).toContain("£120")
    expect(html).toContain("Due now")
    expect(html).toContain("Each year after")
    expect(html).toContain("When payment is confirmed")
  })

  it("is off unless asked for", () => {
    expect(renderWith(false)).not.toContain("Quote breakdown")
  })
})
