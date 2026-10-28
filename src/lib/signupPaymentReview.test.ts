import { describe, expect, it } from "vitest"

import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  signupPaymentFailureView,
  signupPlanIntentFromSavedPlan,
  signupPlanUpdatePayload,
  signupReviewSubmitOutcome,
} from "@/lib/signupPaymentReview"

const savedCustom = (packs: Record<string, number>) => ({
  chosenPlan: "Custom",
  chosenCadence: "monthly",
  customPlanConfigJson: JSON.stringify({
    version: 2,
    cadence: "monthly",
    basePlan: "growth",
    locations: 3,
    packs,
    quoteHash: "stalehash",
    periodNetPence: 1000,
  }),
})

describe("signupPaymentFailureView", () => {
  it("sends a stale quote to a fresh review with the changed-quote copy", () => {
    expect(
      signupPaymentFailureView({
        status: "PaymentFailed",
        paymentFailureCode: "quote_changed",
        savedPlan: savedCustom({ offer: 2 }),
      }),
    ).toEqual({ kind: "review", message: CUSTOM_PLAN_QUOTE_COPY.changed })
  })

  it("sends a retired pack to a new choice with the unavailable copy", () => {
    expect(
      signupPaymentFailureView({
        status: "PaymentFailed",
        paymentFailureCode: "legacy_pack_key",
        savedPlan: savedCustom({ qr_10: 1 }),
      }),
    ).toEqual({ kind: "review", message: CUSTOM_PLAN_QUOTE_COPY.optionUnavailable })
  })

  it("keeps Retry for other failures", () => {
    expect(
      signupPaymentFailureView({
        status: "PaymentFailed",
        paymentFailureCode: "revolut_http_error",
        savedPlan: null,
      }).kind,
    ).toBe("retry")
  })

  it("keeps Retry when the review code has no saved plan to restore", () => {
    expect(
      signupPaymentFailureView({
        status: "PaymentFailed",
        paymentFailureCode: "quote_changed",
        savedPlan: null,
      }).kind,
    ).toBe("retry")
  })
})

describe("signupPlanIntentFromSavedPlan", () => {
  it("restores the saved Custom choices, hash included", () => {
    const intent = signupPlanIntentFromSavedPlan(savedCustom({ offer: 2 }))
    expect(intent?.plan).toBe("Custom")
    expect(intent?.cadence).toBe("monthly")
    expect(intent?.config?.basePlan).toBe("growth")
    expect(intent?.config?.locations).toBe(3)
    expect(intent?.config?.packs.offer).toBe(2)
    expect(intent?.config?.quoteHash).toBe("stalehash")
    expect(intent?.capacityOptionRetired).toBeUndefined()
  })

  it("flags a retired pack, drops its config and keeps the saved base plan card", () => {
    const intent = signupPlanIntentFromSavedPlan(savedCustom({ qr_10: 1 }))
    expect(intent?.capacityOptionRetired).toBe(true)
    expect(intent?.config).toBeUndefined()
    expect(intent?.plan).toBe("Growth")
  })

  it("restores a plain card plan without a config", () => {
    expect(
      signupPlanIntentFromSavedPlan({
        chosenPlan: "Starter",
        chosenCadence: "annual",
        customPlanConfigJson: null,
      }),
    ).toEqual({ plan: "Starter", cadence: "annual" })
  })

  it("restores nothing for no plan or a plan that is not paid", () => {
    expect(signupPlanIntentFromSavedPlan(null)).toBeNull()
    expect(
      signupPlanIntentFromSavedPlan({
        chosenPlan: "Pilot",
        chosenCadence: "monthly",
        customPlanConfigJson: null,
      }),
    ).toBeNull()
  })
})

describe("signupPlanUpdatePayload", () => {
  it("sends the plan and the reviewed config with its hash", () => {
    const payload = signupPlanUpdatePayload({
      plan: "Custom",
      cadence: "monthly",
      config: {
        version: 2,
        cadence: "monthly",
        basePlan: "growth",
        locations: 3,
        packs: { offer: 2 },
        quoteHash: "freshhash",
      },
    })
    expect(payload?.chosenPlan).toBe("Custom")
    expect(payload?.chosenCadence).toBe("monthly")
    expect(JSON.parse(payload?.customPlanConfigJson ?? "{}")).toMatchObject({
      basePlan: "growth",
      locations: 3,
      quoteHash: "freshhash",
    })
  })

  it("sends nothing without a plan or for Pilot", () => {
    expect(signupPlanUpdatePayload(null)).toBeNull()
    expect(signupPlanUpdatePayload({ plan: "Pilot" })).toBeNull()
  })

  it("sends nothing when the intent carries the retired-option flag", () => {
    expect(
      signupPlanUpdatePayload({ plan: "Growth", cadence: "monthly", capacityOptionRetired: true }),
    ).toBeNull()
  })
})

describe("signupReviewSubmitOutcome", () => {
  it("maps the server codes to the next step", () => {
    expect(signupReviewSubmitOutcome("quote_changed")).toBe("review_again")
    expect(signupReviewSubmitOutcome("legacy_pack_key")).toBe("choose_again")
    expect(signupReviewSubmitOutcome("invalid_pack_quantity")).toBe("show_error")
    expect(signupReviewSubmitOutcome(null)).toBe("show_error")
  })
})
