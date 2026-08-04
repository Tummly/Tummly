import { describe, expect, it } from "vitest"

import {
  RECOVERY_OFFER_PURPOSE_ID,
  RECOVERY_OFFER_PURPOSE_LABEL,
  RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS,
  RECOVERY_OFFER_TYPE_OPTIONS,
  autoTitleForRecoveryOffer,
  canContinueRecoveryOfferDetails,
  canContinueRespondWithRecoveryOfferSetup,
  emptyRecoveryOfferDetailsDraft,
  emptyRespondWithRecoveryOfferDraft,
  furthestRespondWithRecoveryOfferStep,
  toConfirmedRecoveryOfferPayload,
} from "./recoveryOfferPresentation"

describe("recoveryOfferPresentation", () => {
  it("locks purpose to Include a recovery offer", () => {
    expect(RECOVERY_OFFER_PURPOSE_ID).toBe("include_a_recovery_offer")
    expect(RECOVERY_OFFER_PURPOSE_LABEL).toBe("Include a recovery offer")
  })

  it("exposes Figma offer-type card labels and descriptions", () => {
    expect(RECOVERY_OFFER_TYPE_OPTIONS).toEqual([
      {
        id: "percentage_discount",
        label: "Percentage discount",
        description:
          "Give the guest a percentage off their next eligible purchase.",
      },
      {
        id: "fixed_discount",
        label: "Fixed discount",
        description:
          "Give the guest a fixed monetary amount off their next eligible purchase.",
      },
      {
        id: "free_item",
        label: "Free item",
        description:
          "Offer one specified item with or without a qualifying purchase.",
      },
      {
        id: "replacement_item",
        label: "Replacement item",
        description:
          "Allow the guest to receive a replacement for a specific item.",
      },
    ])
  })

  it("exposes purchase-requirement card labels for free item", () => {
    expect(RECOVERY_OFFER_PURCHASE_REQUIREMENT_OPTIONS.map((o) => o.label)).toEqual(
      [
        "No purchase required",
        "With any purchase",
        "With a minimum spend",
      ]
    )
  })

  it("gates Response setup Continue on channel and tone only", () => {
    expect(
      canContinueRespondWithRecoveryOfferSetup({
        channel: "email",
        tone: "warm_and_apologetic",
      })
    ).toBe(true)
    expect(
      canContinueRespondWithRecoveryOfferSetup({
        channel: null,
        tone: "warm_and_apologetic",
      })
    ).toBe(false)
    expect(
      canContinueRespondWithRecoveryOfferSetup({
        channel: "sms",
        tone: null,
      })
    ).toBe(false)
  })

  it("requires type fields, title, description, and validity for Offer details", () => {
    const base = emptyRecoveryOfferDetailsDraft()
    expect(canContinueRecoveryOfferDetails(base)).toBe(false)

    const percentage = {
      ...base,
      offerType: "percentage_discount" as const,
      discountPercentage: "20",
      title: "20% off",
      description: "A thank-you discount for your next visit.",
    }
    expect(canContinueRecoveryOfferDetails(percentage)).toBe(true)
    expect(
      canContinueRecoveryOfferDetails({
        ...percentage,
        discountPercentage: "0",
      })
    ).toBe(false)

    const freeItem = {
      ...base,
      offerType: "free_item" as const,
      freeItemText: "Dessert",
      purchaseRequirement: "with_minimum_spend" as const,
      minimumSpend: "15",
      title: "Free Dessert",
      description: "Enjoy a complimentary dessert.",
    }
    expect(canContinueRecoveryOfferDetails(freeItem)).toBe(true)
    expect(
      canContinueRecoveryOfferDetails({
        ...freeItem,
        minimumSpend: "",
      })
    ).toBe(false)

    const customExpiry = {
      ...percentage,
      validity: "choose_expiry_date" as const,
      expiryDate: "",
    }
    expect(canContinueRecoveryOfferDetails(customExpiry)).toBe(false)
    expect(
      canContinueRecoveryOfferDetails({
        ...customExpiry,
        expiryDate: "2026-09-01",
      })
    ).toBe(true)
  })

  it("auto-generates title from benefit and caps at 60", () => {
    expect(
      autoTitleForRecoveryOffer({
        offerType: "percentage_discount",
        discountPercentage: "15",
        discountAmount: "",
        freeItemText: "",
        replacementItemText: "",
      })
    ).toBe("15% off")

    expect(
      autoTitleForRecoveryOffer({
        offerType: "fixed_discount",
        discountPercentage: "",
        discountAmount: "10",
        freeItemText: "",
        replacementItemText: "",
      })
    ).toBe("£10 off")

    const longItem = "x".repeat(80)
    const title = autoTitleForRecoveryOffer({
      offerType: "free_item",
      discountPercentage: "",
      discountAmount: "",
      freeItemText: longItem,
      replacementItemText: "",
    })
    expect(title.length).toBeLessThanOrEqual(60)
    expect(title.startsWith("Free ")).toBe(true)
  })

  it("resumes at furthest incomplete step including Offer details", () => {
    const draft = emptyRespondWithRecoveryOfferDraft()
    expect(furthestRespondWithRecoveryOfferStep(draft)).toBe("setup")

    draft.setupComplete = true
    expect(furthestRespondWithRecoveryOfferStep(draft)).toBe("offer")

    draft.offer.offerComplete = true
    expect(furthestRespondWithRecoveryOfferStep(draft)).toBe("write")

    draft.messageComplete = true
    expect(furthestRespondWithRecoveryOfferStep(draft)).toBe("review")
  })

  it("builds confirmed offer payload for send and draft adapter", () => {
    const offer = {
      ...emptyRecoveryOfferDetailsDraft(),
      offerType: "percentage_discount" as const,
      discountPercentage: "25",
      title: "25% off",
      description: "Next visit discount.",
      validity: "14_days_after_issue" as const,
      staffInstructions: "Redeem once.",
    }
    expect(toConfirmedRecoveryOfferPayload(offer)).toEqual({
      offerType: "percentage_discount",
      title: "25% off",
      description: "Next visit discount.",
      validity: "14_days_after_issue",
      expiryDate: null,
      discountPercentage: 25,
      discountAmount: null,
      freeItemText: null,
      purchaseRequirement: null,
      minimumSpend: null,
      additionalExclusions: null,
      replacementItemText: null,
      staffInstructions: "Redeem once.",
    })
  })
})
