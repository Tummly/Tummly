import { describe, expect, it } from "vitest"

import {
  listGlobalSearchSelectables,
  resolveGlobalSearchHighlight,
} from "@/lib/operatorGlobalSearch/globalSearchCommandNav"
import type { OperatorGlobalSearchSnapshot } from "@/lib/operatorGlobalSearch/createOperatorGlobalSearchModule"

function makeSnapshot(
  overrides: Partial<OperatorGlobalSearchSnapshot> = {}
): OperatorGlobalSearchSnapshot {
  return {
    open: true,
    query: "mo",
    emptySuggestions: [],
    typedSuggestions: [
      {
        id: "ask-analyse-feedback",
        prompt: 'Analyse Feedback mentioning "mo"',
      },
      {
        id: "ask-what-changed",
        prompt: 'What changed about "mo"?',
      },
    ],
    shortcutModifierLabel: "Ctrl",
    hitsPending: false,
    guestHits: [],
    feedbackHits: [],
    campaignHits: [],
    offerHits: [],
    qrCodeHits: [],
    locationScope: "current",
    canWidenLocationScope: false,
    showWidenFromNoResults: false,
    showNoResults: false,
    showViewAllGuests: false,
    showViewAllFeedback: false,
    showViewAllCampaigns: false,
    showViewAllOffers: false,
    showViewAllQrCodes: false,
    searchStatus: "ready",
    showError: false,
    showOffline: false,
    showPartialWarning: false,
    failedGroupTypes: [],
    resultCountAnnouncement: "",
    ...overrides,
  }
}

const noopHandlers = {
  onSelectSuggestion: () => {},
  onSelectGuestHit: () => {},
  onSelectFeedbackHit: () => {},
  onSelectCampaignHit: () => {},
  onSelectOfferHit: () => {},
  onSelectQrCodeHit: () => {},
  onViewAllGuests: () => {},
  onViewAllFeedback: () => {},
  onViewAllCampaigns: () => {},
  onViewAllOffers: () => {},
  onViewAllQrCodes: () => {},
}

describe("resolveGlobalSearchHighlight", () => {
  it("keeps Ask Tummly highlight while only typed suggestions exist", () => {
    const values = listGlobalSearchSelectables(
      makeSnapshot(),
      noopHandlers
    ).map((item) => item.value)

    expect(values[0]).toBe("ask-analyse-feedback")
    expect(
      resolveGlobalSearchHighlight(values, "ask-analyse-feedback", {
        entityHitsJustArrived: false,
      })
    ).toBe("ask-analyse-feedback")
  })

  it("moves highlight to the first product hit when entity results arrive", () => {
    const before = listGlobalSearchSelectables(
      makeSnapshot(),
      noopHandlers
    ).map((item) => item.value)
    expect(before[0]).toBe("ask-analyse-feedback")

    const after = listGlobalSearchSelectables(
      makeSnapshot({
        guestHits: [
          {
            id: "42",
            title: "Morgan",
            subtitle: null,
            locationId: 1,
            initials: "MO",
          },
        ],
      }),
      noopHandlers
    ).map((item) => item.value)

    expect(after[0]).toBe("guest-42")
    expect(
      resolveGlobalSearchHighlight(after, "ask-analyse-feedback", {
        entityHitsJustArrived: true,
      })
    ).toBe("guest-42")
  })

  it("does not steal highlight after the user already moved off Ask Tummly", () => {
    const values = listGlobalSearchSelectables(
      makeSnapshot({
        guestHits: [
          {
            id: "42",
            title: "Morgan",
            subtitle: null,
            locationId: 1,
            initials: "MO",
          },
        ],
      }),
      noopHandlers
    ).map((item) => item.value)

    expect(
      resolveGlobalSearchHighlight(values, "ask-what-changed", {
        entityHitsJustArrived: false,
      })
    ).toBe("ask-what-changed")
  })
})
