import { describe, expect, it } from "vitest"

import {
  buildOfferDetailsClaimsRowActions,
  buildOfferDetailsDefinitionFields,
  buildOfferDetailsHeaderMenuItems,
  buildOfferDetailsLifecycleEmptyState,
  buildOfferDetailsMetaRows,
  buildOfferDetailsOverviewKpis,
  buildOfferDetailsRedemptionsRowActions,
  DEFAULT_OFFER_DETAILS_DATE_RANGE,
  formatOfferDetailsSourceLabel,
  isVisibleOfferDetailsRowAction,
  labelForOfferDetailsDateRange,
  OFFER_DETAILS_CAMPAIGNS_SUB_TAB_IDS,
  OFFER_DETAILS_CAMPAIGNS_SUB_TAB_LABELS,
  OFFER_DETAILS_CLAIMS_COLUMN_LABELS,
  OFFER_DETAILS_COPY,
  OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS,
  OFFER_DETAILS_TAB_LABELS,
  OFFER_DETAILS_VOID_REQUESTS_COLUMN_LABELS,
  offerDetailsHeaderActionConfirmCopy,
  offerDetailsStatusLabel,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

function sampleOffer(
  overrides: Partial<CatalogOfferDetail> = {}
): CatalogOfferDetail {
  return {
    id: 10,
    locationId: 42,
    status: "active",
    offerType: "percentage_discount",
    title: "10% off next visit",
    description: "Camden thank-you offer · August",
    validity: "14_days_after_issue",
    expiryDate: null,
    discountPercentage: 10,
    discountAmount: null,
    freeItemText: null,
    purchaseRequirement: null,
    minimumSpend: null,
    additionalExclusions: null,
    replacementItemText: null,
    staffInstructions: null,
    issueCount: 0,
    createdAt: "2026-07-01T12:00:00.000Z",
    updatedAt: "2026-07-01T12:00:00.000Z",
    ...overrides,
  }
}

describe("buildOfferDetailsHeaderMenuItems", () => {
  it("ships Draft matrix without Delete draft or View activity", () => {
    expect(buildOfferDetailsHeaderMenuItems("draft")).toEqual([
      { id: "rename", label: OFFER_DETAILS_COPY.rename },
      { id: "duplicate", label: OFFER_DETAILS_COPY.duplicate },
    ])
  })

  it("ships Active matrix with Archive offer and without navigate-only items", () => {
    expect(buildOfferDetailsHeaderMenuItems("active")).toEqual([
      { id: "pause-issuance", label: OFFER_DETAILS_COPY.pauseIssuance },
      { id: "duplicate", label: OFFER_DETAILS_COPY.duplicate },
      { id: "archive-offer", label: OFFER_DETAILS_COPY.archiveOffer },
    ])
  })

  it("ships Paused matrix with Resume and Archive offer", () => {
    expect(buildOfferDetailsHeaderMenuItems("paused")).toEqual([
      { id: "resume-issuance", label: OFFER_DETAILS_COPY.resumeIssuance },
      { id: "archive-offer", label: OFFER_DETAILS_COPY.archiveOffer },
      { id: "duplicate", label: OFFER_DETAILS_COPY.duplicate },
    ])
  })

  it("ships Expired matrix with Duplicate as new Draft and Archive offer", () => {
    expect(buildOfferDetailsHeaderMenuItems("expired")).toEqual([
      {
        id: "duplicate",
        label: OFFER_DETAILS_COPY.duplicateAsNewDraft,
      },
      { id: "archive-offer", label: OFFER_DETAILS_COPY.archiveOffer },
    ])
  })

  it("ships Archived matrix without View historical record", () => {
    expect(buildOfferDetailsHeaderMenuItems("archived")).toEqual([
      {
        id: "duplicate",
        label: OFFER_DETAILS_COPY.duplicateAsNewDraft,
      },
    ])
  })
})

describe("offerDetailsHeaderActionConfirmCopy", () => {
  it("uses one-line confirm copy for Archive offer", () => {
    expect(offerDetailsHeaderActionConfirmCopy("archive-offer")).toEqual({
      title: OFFER_DETAILS_COPY.archiveConfirmTitle,
      description: OFFER_DETAILS_COPY.archiveConfirmDescription,
    })
  })
})

describe("buildOfferDetailsOverviewKpis", () => {
  it("uses ticket Overview KPI labels and zero defaults", () => {
    expect(buildOfferDetailsOverviewKpis()).toEqual([
      {
        id: "claims",
        label: "Claims",
        primaryText: "0",
        helperText: OFFER_DETAILS_COPY.kpiClaimsHelper,
      },
      {
        id: "redemptions",
        label: "Redemptions",
        primaryText: "0",
        helperText: OFFER_DETAILS_COPY.kpiRedemptionsHelper,
      },
      {
        id: "redemption-rate",
        label: "Redemption rate",
        primaryText: "0%",
        helperText: OFFER_DETAILS_COPY.kpiRedemptionRateHelper,
      },
      {
        id: "expired-unused",
        label: "Expired unused",
        primaryText: "0",
        helperText: OFFER_DETAILS_COPY.kpiExpiredUnusedHelper,
      },
      {
        id: "failed-attempts",
        label: "Failed attempts",
        primaryText: "0",
        helperText: OFFER_DETAILS_COPY.kpiFailedAttemptsHelper,
      },
    ])
  })
})

describe("Overview copy helpers", () => {
  it("keeps definition title and five-tab labels from ticket 10", () => {
    expect(OFFER_DETAILS_COPY.definitionTitle).toBe(
      "Claims and redemptions over time"
    )
    expect(OFFER_DETAILS_TAB_LABELS).toEqual({
      overview: "Overview",
      claims: "Claims",
      redemptions: "Redemptions",
      campaigns: "Campaigns",
      "void-requests": "Void requests",
    })
  })

  it("defaults Overview date range to Last 7 days", () => {
    expect(DEFAULT_OFFER_DETAILS_DATE_RANGE).toEqual({
      kind: "preset",
      presetId: "last7",
    })
    expect(labelForOfferDetailsDateRange(DEFAULT_OFFER_DETAILS_DATE_RANGE)).toBe(
      "Last 7 days"
    )
    expect(
      labelForOfferDetailsDateRange({ kind: "preset", presetId: "last90" })
    ).toBe("Last 90 days")
  })

  it("builds meta and definition rows from catalog get-by-id facts", () => {
    const offer = sampleOffer()
    expect(
      buildOfferDetailsMetaRows({
        locationName: "Camden",
        createdAt: offer.createdAt,
        sourceLabel: formatOfferDetailsSourceLabel(offer.attachKinds),
        createdByLabel: offer.createdByDisplayName,
      })
    ).toEqual([
      { label: "Source", value: "Manual" },
      { label: "Locations", value: "Camden" },
      { label: "Created by", value: "—" },
      { label: "Created", value: "1 Jul 2026" },
    ])

    expect(
      buildOfferDetailsMetaRows({
        locationName: "Camden",
        createdAt: offer.createdAt,
        sourceLabel: formatOfferDetailsSourceLabel([
          "guest-form-thank-you",
        ]),
        createdByLabel: "Sarah M.",
      })
    ).toEqual([
      { label: "Source", value: "Guest form thank-you" },
      { label: "Locations", value: "Camden" },
      { label: "Created by", value: "Sarah M." },
      { label: "Created", value: "1 Jul 2026" },
    ])

    const fields = buildOfferDetailsDefinitionFields({
      offer,
      locationName: "Camden",
    })
    expect(fields.find((row) => row.label === "Offer value")?.value).toBe(
      "10% off"
    )
    expect(fields.find((row) => row.label === "Expiry")?.value).toBe(
      "14 days after issue"
    )
    expect(
      fields.find((row) => row.label === "Redemption method")?.value
    ).toBe("—")
    expect(fields.find((row) => row.label === "Usage")?.value).toBe("—")
    expect(
      fields.find((row) => row.label === "Manager override")?.value
    ).toBe("—")
    expect(
      fields.find((row) => row.label === "Staff verification")?.value
    ).toBe("—")
  })

  it("maps staff verification from staffInstructions and fixed discount value", () => {
    const fields = buildOfferDetailsDefinitionFields({
      offer: sampleOffer({
        offerType: "fixed_discount",
        discountPercentage: null,
        discountAmount: 5,
        staffInstructions: "Ask for ID",
      }),
      locationName: "Camden",
    })
    expect(fields.find((row) => row.label === "Offer value")?.value).toBe(
      "£5 off"
    )
    expect(
      fields.find((row) => row.label === "Staff verification")?.value
    ).toBe("Required")
  })

  it("maps status badge labels for Details status set", () => {
    expect(offerDetailsStatusLabel("active")).toBe("Active")
    expect(offerDetailsStatusLabel("archived")).toBe("Archived")
  })
})

describe("buildOfferDetailsLifecycleEmptyState", () => {
  it("ships Claims empty Figma title helper and Share CTA", () => {
    expect(buildOfferDetailsLifecycleEmptyState("claims")).toEqual({
      title: "No one has claimed this offer yet",
      helper:
        "Once guests claim this offer from a feedback form, campaign or manual link, they'll appear here.",
      primaryCtaLabel: "Share offer in a campaign",
    })
  })

  it("adapts empty chrome copy for other lifecycle tabs without inventing extra CTAs", () => {
    expect(buildOfferDetailsLifecycleEmptyState("redemptions")).toEqual({
      title: "No redemptions yet",
      helper:
        "When guests redeem this offer, staff redemptions will appear here.",
      primaryCtaLabel: null,
    })
    expect(buildOfferDetailsLifecycleEmptyState("campaigns-linked")).toEqual({
      title: "No linked campaigns yet",
      helper:
        "Campaigns that use this offer will appear here once you attach it.",
      primaryCtaLabel: null,
    })
    expect(
      buildOfferDetailsLifecycleEmptyState("campaigns-issuance")
    ).toEqual({
      title: "No issuance sources yet",
      helper:
        "Attach paths that issued passes for this offer will appear here.",
      primaryCtaLabel: null,
    })
    expect(buildOfferDetailsLifecycleEmptyState("void-requests")).toEqual({
      title: "No void requests yet",
      helper:
        "Void requests for passes on this offer will appear here.",
      primaryCtaLabel: null,
    })
  })
})

describe("lifecycle column labels", () => {
  it("locks Claims columns from ticket 10", () => {
    expect(OFFER_DETAILS_CLAIMS_COLUMN_LABELS).toEqual({
      guest: "Guest",
      claimCode: "Claim code",
      claimed: "Claimed",
      source: "Source",
      location: "Location",
      expiry: "Expiry",
      status: "Status",
      actions: "Actions",
    })
  })

  it("locks Redemptions columns without Override or Offer", () => {
    expect(OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS).toEqual({
      dateTime: "Date/time",
      guest: "Guest",
      passReference: "Pass reference",
      location: "Location",
      staffMember: "Staff member",
      outcome: "Outcome",
      reason: "Reason",
      offerVersion: "Offer version",
      actions: "Actions",
    })
    expect(OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS).not.toHaveProperty(
      "override"
    )
    expect(OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS).not.toHaveProperty("offer")
  })

  it("locks Void requests columns from Figma", () => {
    expect(OFFER_DETAILS_VOID_REQUESTS_COLUMN_LABELS).toEqual({
      dateTime: "Date/time",
      requestedBy: "Requested by",
      guest: "Guest",
      offerPass: "Offer pass",
      reason: "Reason",
      location: "Location",
      currentState: "Current state",
      requestedCorrection: "Requested correction",
      status: "Status",
      actions: "Actions",
    })
  })
})

describe("lifecycle row action builders", () => {
  it("hides gated navigate-only actions but keeps confirm actions", () => {
    expect(
      buildOfferDetailsRedemptionsRowActions().filter(
        isVisibleOfferDetailsRowAction
      ).map((action) => action.id)
    ).toEqual(["request-void"])
    expect(
      buildOfferDetailsClaimsRowActions().filter(
        isVisibleOfferDetailsRowAction
      ).map((action) => action.id)
    ).toEqual([
      "view-guest-profile",
      "resend-offer",
      "cancel-claim",
      "copy-code",
    ])
  })

  it("ships Claims ⋮ with Resend gated and Copy code live when data exists", () => {
    expect(buildOfferDetailsClaimsRowActions()).toEqual([
      { id: "view-guest-profile", label: "View guest profile", gated: false },
      { id: "resend-offer", label: "Resend offer", gated: true },
      { id: "cancel-claim", label: "Cancel claim", gated: true },
      { id: "copy-code", label: "Copy code", gated: false },
    ])
  })

  it("ships Redemptions ⋮ without Export; request-void is live", () => {
    const actions = buildOfferDetailsRedemptionsRowActions()
    expect(actions.map((action) => action.id)).toEqual([
      "view-redemption",
      "view-pass",
      "view-guest",
      "view-issued-terms",
      "request-void",
    ])
    expect(actions.map((action) => action.id)).not.toContain("export-record")
    expect(
      actions.find((action) => action.id === "request-void")?.gated
    ).toBe(false)
    expect(
      actions
        .filter((action) => action.id !== "request-void")
        .every((action) => action.gated)
    ).toBe(true)
  })
})

describe("Campaigns sub-tabs", () => {
  it("ships Linked campaigns and Issuance sources sub-tab ids", () => {
    expect(OFFER_DETAILS_CAMPAIGNS_SUB_TAB_IDS).toEqual([
      "linked",
      "issuance-sources",
    ])
    expect(OFFER_DETAILS_CAMPAIGNS_SUB_TAB_LABELS).toEqual({
      linked: "Linked campaigns",
      "issuance-sources": "Issuance sources",
    })
  })
})
