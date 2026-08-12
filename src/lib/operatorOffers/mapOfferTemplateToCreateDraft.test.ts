import { describe, expect, it } from "vitest"

import { OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS } from "@/lib/operatorOffers/offerCatalogPresentation"
import { mapOfferTemplateToCreateDraft } from "@/lib/operatorOffers/mapOfferTemplateToCreateDraft"
import {
  OFFER_TEMPLATE_SEED,
  getOfferTemplateById,
} from "@/lib/operatorOffers/offerTemplateSeed"

describe("offerTemplateSeed", () => {
  it("ships the seven catalogue ids from ticket 08", () => {
    expect(OFFER_TEMPLATE_SEED.map((item) => item.id)).toEqual([
      "welcome-new-guests",
      "thank-recent-guests",
      "encourage-quieter-time",
      "reconnect-with-guests",
      "completed-recovery-offer",
      "promote-new-item",
      "custom-offer",
    ])
  })

  it("exposes card fields and soft draft keys without writing source to draft helpers", () => {
    const welcome = getOfferTemplateById("welcome-new-guests")
    expect(welcome).toMatchObject({
      title: "Welcome new guests",
      summary:
        "Give newly joined guest-club members a simple reason to return.",
      suggestedBenefit: "Percentage discount or free item",
      softOfferType: "percentage_discount",
      suggestedValidity: "30 days after issue",
      softValidity: "30_days_after_issue",
      suggestedSource: "Guest form signup",
      offerTitlePlaceholder: "Welcome to {{restaurant_name}}",
      startingDescription:
        "Enjoy a welcome offer on your next eligible visit to {{restaurant_name}}.",
    })

    const quieter = getOfferTemplateById("encourage-quieter-time")
    expect(quieter?.suggestedSource).toBeUndefined()
    expect(quieter?.softValidity).toBe("choose_expiry_date")

    const custom = getOfferTemplateById("custom-offer")
    expect(custom).toMatchObject({
      softOfferType: null,
      softValidity: "30_days_after_issue",
    })
    expect(custom).not.toHaveProperty("offerTitlePlaceholder")
    expect(custom).not.toHaveProperty("suggestedBenefit")
    expect(custom).not.toHaveProperty("suggestedValidity")
    expect(custom).not.toHaveProperty("suggestedSource")
  })
})

describe("mapOfferTemplateToCreateDraft", () => {
  it("prefills title and description with restaurant_name substituted", () => {
    const template = getOfferTemplateById("welcome-new-guests")!
    const draft = mapOfferTemplateToCreateDraft(template, "Camden Kitchen")

    expect(draft.title).toBe("Welcome to Camden Kitchen")
    expect(draft.description).toBe(
      "Enjoy a welcome offer on your next eligible visit to Camden Kitchen."
    )
    expect(draft.offerType).toBe("percentage_discount")
    expect(draft.validity).toBe("30_days_after_issue")
    expect(draft.expiryDate).toBe("")
    expect(draft.discountPercentage).toBe("")
    expect(draft.discountAmount).toBe("")
    expect(draft.freeItemText).toBe("")
    expect(draft.replacementItemText).toBe("")
    expect(draft.purchaseRequirement).toBeNull()
    expect(draft.staffInstructions).toBe(OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS)
  })

  it("keeps day_or_time and item_name literals for operator edit", () => {
    const quieter = getOfferTemplateById("encourage-quieter-time")!
    const quieterDraft = mapOfferTemplateToCreateDraft(quieter, "Camden")
    expect(quieterDraft.title).toBe(
      "A little extra for your next {{day_or_time}} visit"
    )
    expect(quieterDraft.validity).toBe("choose_expiry_date")
    expect(quieterDraft.expiryDate).toBe("")

    const promote = getOfferTemplateById("promote-new-item")!
    const promoteDraft = mapOfferTemplateToCreateDraft(promote, "Camden")
    expect(promoteDraft.offerType).toBe("free_item")
    expect(promoteDraft.description).toBe(
      "Discover {{item_name}} at Camden and use this offer during the available period."
    )
    expect(promoteDraft.validity).toBe("choose_expiry_date")
  })

  it("maps recovery and reconnect soft types and validity", () => {
    const recovery = mapOfferTemplateToCreateDraft(
      getOfferTemplateById("completed-recovery-offer")!,
      "Camden"
    )
    expect(recovery.offerType).toBe("replacement_item")
    expect(recovery.validity).toBe("14_days_after_issue")
    expect(recovery.title).toBe("A recovery offer from Camden")

    const reconnect = mapOfferTemplateToCreateDraft(
      getOfferTemplateById("reconnect-with-guests")!,
      "Camden"
    )
    expect(reconnect.offerType).toBe("percentage_discount")
    expect(reconnect.validity).toBe("14_days_after_issue")
  })

  it("treats custom offer as blank-ish create with soft description", () => {
    const draft = mapOfferTemplateToCreateDraft(
      getOfferTemplateById("custom-offer")!,
      "Camden Kitchen"
    )

    expect(draft.offerType).toBeNull()
    expect(draft.title).toBe("")
    expect(draft.description).toBe(
      "Enjoy this offer on your next eligible visit to Camden Kitchen."
    )
    expect(draft.validity).toBe("30_days_after_issue")
    expect(draft.staffInstructions).toBe(OFFER_CATALOG_DEFAULT_STAFF_INSTRUCTIONS)
  })

  it("never writes suggested source into the draft", () => {
    const template = getOfferTemplateById("welcome-new-guests")!
    expect(template.suggestedSource).toBe("Guest form signup")
    const draft = mapOfferTemplateToCreateDraft(template, "Camden")
    expect(JSON.stringify(draft)).not.toContain("Guest form signup")
  })
})
