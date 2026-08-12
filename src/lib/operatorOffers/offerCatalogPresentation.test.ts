import { describe, expect, it } from "vitest"

import {
  autoTitleForCatalogOffer,
  canConfirmCampaignCatalogOfferDetails,
  catalogOfferDetailToDraft,
  emptyCampaignCatalogOfferDetailsDraft,
  isDirtyBenefitOrValidity,
  mergeCampaignCatalogOfferDraftPatch,
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

  it("auto-generates catalog title from benefit fields", () => {
    expect(
      autoTitleForCatalogOffer({
        offerType: "percentage_discount",
        discountPercentage: "10",
        discountAmount: "",
        freeItemText: "",
        replacementItemText: "",
      })
    ).toBe("10% off your next visit")

    expect(
      autoTitleForCatalogOffer({
        offerType: "fixed_discount",
        discountPercentage: "",
        discountAmount: "5",
        freeItemText: "",
        replacementItemText: "",
      })
    ).toBe("£5 off your next order")

    expect(
      autoTitleForCatalogOffer({
        offerType: "free_item",
        discountPercentage: "",
        discountAmount: "",
        freeItemText: "side",
        replacementItemText: "",
      })
    ).toBe("Enjoy a free side")

    expect(
      autoTitleForCatalogOffer({
        offerType: "replacement_item",
        discountPercentage: "",
        discountAmount: "",
        freeItemText: "",
        replacementItemText: "chicken wrap meal",
      })
    ).toBe("Replacement chicken wrap meal")
  })

  it("mergeCampaignCatalogOfferDraftPatch refreshes title until touched", () => {
    const draft = emptyCampaignCatalogOfferDetailsDraft()
    const withType = mergeCampaignCatalogOfferDraftPatch(draft, {
      offerType: "percentage_discount",
    })
    expect(withType.title).toBe("Percentage discount")
    expect(withType.titleTouched).toBe(false)

    const withPct = mergeCampaignCatalogOfferDraftPatch(withType, {
      discountPercentage: "15",
    })
    expect(withPct.title).toBe("15% off your next visit")

    const manual = mergeCampaignCatalogOfferDraftPatch(withPct, {
      title: "Custom lunch deal",
    })
    expect(manual.title).toBe("Custom lunch deal")
    expect(manual.titleTouched).toBe(true)

    const afterBenefit = mergeCampaignCatalogOfferDraftPatch(manual, {
      discountPercentage: "20",
    })
    expect(afterBenefit.title).toBe("Custom lunch deal")

    const afterTypeChange = mergeCampaignCatalogOfferDraftPatch(manual, {
      offerType: "fixed_discount",
      discountAmount: "5",
    })
    expect(afterTypeChange.titleTouched).toBe(false)
    expect(afterTypeChange.title).toBe("£5 off your next order")
  })
})
