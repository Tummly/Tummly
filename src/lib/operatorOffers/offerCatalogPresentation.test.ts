import { describe, expect, it } from "vitest"

import {
  canConfirmCampaignCatalogOfferDetails,
  catalogOfferDetailToDraft,
  emptyCampaignCatalogOfferDetailsDraft,
  isDirtyBenefitOrValidity,
  OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS,
  shouldConfirmEditOfferSave,
  toCreateCatalogOfferRequestBody,
} from "@/lib/operatorOffers/offerCatalogPresentation"

describe("offerCatalogPresentation draft helpers", () => {
  it("empty draft starts with Figma default staff instructions and no type", () => {
    const draft = emptyCampaignCatalogOfferDetailsDraft()

    expect(draft.offerType).toBeNull()
    expect(draft.staffInstructions).toBe(OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS)
    expect(draft.validity).toBe("30_days_after_issue")
    expect(canConfirmCampaignCatalogOfferDetails(draft)).toBe(false)
  })

  it("toCreateCatalogOfferRequestBody maps a valid percentage draft", () => {
    const draft = emptyCampaignCatalogOfferDetailsDraft()
    draft.offerType = "percentage_discount"
    draft.discountPercentage = "10"
    draft.title = "10% off"
    draft.description = "Ten percent off your next visit."

    expect(canConfirmCampaignCatalogOfferDetails(draft)).toBe(true)
    expect(
      toCreateCatalogOfferRequestBody({ locationId: 42, draft })
    ).toEqual(
      expect.objectContaining({
        locationId: 42,
        offerType: "percentage_discount",
        title: "10% off",
        discountPercentage: 10,
      })
    )
  })

  it("catalogOfferDetailToDraft hydrates type and benefit fields", () => {
    const draft = catalogOfferDetailToDraft({
      id: 9,
      locationId: 42,
      status: "active",
      offerType: "fixed_discount",
      title: "£5 off",
      description: "Five pounds off.",
      validity: "14_days_after_issue",
      expiryDate: null,
      discountPercentage: null,
      discountAmount: 5,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: "Staff note",
      issueCount: 0,
      createdAt: "2026-08-09T00:00:00Z",
      updatedAt: "2026-08-09T00:00:00Z",
    })

    expect(draft).toMatchObject({
      offerType: "fixed_discount",
      discountAmount: "5",
      title: "£5 off",
      validity: "14_days_after_issue",
      staffInstructions: "Staff note",
    })
  })

  it("isDirtyBenefitOrValidity detects benefit and validity changes only", () => {
    const baseline = emptyCampaignCatalogOfferDetailsDraft()
    baseline.offerType = "percentage_discount"
    baseline.discountPercentage = "10"
    baseline.title = "10% off"
    baseline.description = "Ten percent."
    baseline.validity = "30_days_after_issue"

    expect(
      isDirtyBenefitOrValidity(baseline, {
        ...baseline,
        title: "Renamed only",
      })
    ).toBe(false)
    expect(
      isDirtyBenefitOrValidity(baseline, {
        ...baseline,
        discountPercentage: "15",
      })
    ).toBe(true)
    expect(
      isDirtyBenefitOrValidity(baseline, {
        ...baseline,
        validity: "14_days_after_issue",
      })
    ).toBe(true)
    expect(
      isDirtyBenefitOrValidity(baseline, {
        ...baseline,
        purchaseRequirement: "with_any_purchase",
      })
    ).toBe(false)
  })

  it("shouldConfirmEditOfferSave requires issues and dirty benefit/validity", () => {
    expect(
      shouldConfirmEditOfferSave({
        issueCount: 0,
        dirtyBenefitOrValidity: true,
      })
    ).toBe(false)
    expect(
      shouldConfirmEditOfferSave({
        issueCount: 2,
        dirtyBenefitOrValidity: false,
      })
    ).toBe(false)
    expect(
      shouldConfirmEditOfferSave({
        issueCount: 1,
        dirtyBenefitOrValidity: true,
      })
    ).toBe(true)
  })
})
