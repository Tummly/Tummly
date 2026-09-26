import { describe, expect, it } from "vitest"

import {
  campaignSmsOfferClaimPreviewFields,
  ensureCampaignSmsOfferClaimInBody,
  formatCampaignSmsOfferClaimBlock,
  stripCampaignSmsOfferClaimFooter,
} from "@/lib/operatorCampaigns/campaignSmsOfferClaimPresentation"
import { GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER } from "@/lib/operatorFeedback/guestPreviewPresentation"

describe("campaignSmsOfferClaimPresentation", () => {
  it("formats a Claim-code footer for SMS", () => {
    expect(
      formatCampaignSmsOfferClaimBlock({
        title: "10% off",
        redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
        expiryLabel: "Expires: 30 days after issue",
      })
    ).toBe(
      `\n\n10% off\nClaim code: ${GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER}\nExpires: 30 days after issue`
    )
  })

  it("appends Claim code to body and replaces a prior footer", () => {
    const withSample = ensureCampaignSmsOfferClaimInBody("Thanks for visiting.", {
      title: "Welcome back",
      redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
      expiryLabel: "Expires: —",
    })
    expect(withSample).toContain(
      `Claim code: ${GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER}`
    )
    expect(withSample.startsWith("Thanks for visiting.")).toBe(true)

    const replaced = ensureCampaignSmsOfferClaimInBody(withSample, {
      title: "Welcome back",
      redemptionCode: "TUM-ABC123",
      expiryLabel: "Expires: 31 August 2026",
    })
    expect(replaced).toContain("Claim code: TUM-ABC123")
    expect(replaced).not.toContain(GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER)
    expect(replaced.match(/Claim code:/g)?.length).toBe(1)
  })

  it("strips Claim footer when offer is cleared", () => {
    const withOffer = ensureCampaignSmsOfferClaimInBody("Hi", {
      title: "Offer",
      redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
      expiryLabel: "Expires: —",
    })
    expect(stripCampaignSmsOfferClaimFooter(withOffer)).toBe("Hi")
    expect(ensureCampaignSmsOfferClaimInBody(withOffer, null)).toBe("Hi")
  })

  it("builds preview fields with the sample Claim code", () => {
    expect(
      campaignSmsOfferClaimPreviewFields({
        title: "Free dessert",
        expiryLabel: "Expires: 7 days after issue",
      })
    ).toEqual({
      title: "Free dessert",
      redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
      expiryLabel: "Expires: 7 days after issue",
    })
  })
})
