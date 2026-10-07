import { describe, expect, it } from "vitest"

import {
  ASSISTANT_WAIT_BODY,
  ASSISTANT_WAIT_PREPARING_BODY,
  ASSISTANT_WAIT_RETRIEVING_BODY,
  assistantWaitPhraseAt,
  classifyAssistantWaitGate,
  classifyAssistantWaitRetrieveFocus,
  isAssistantCheckingWaitBody,
  isAssistantWaitPhraseBody,
  planAssistantWaitPhrases,
} from "./assistantWaitPhrases"

describe("assistantWaitPhrases", () => {
  it("classifies create, offer, recovery, and retrieve gates", () => {
    expect(classifyAssistantWaitGate("Create a campaign for new guests")).toBe(
      "create-campaign-draft"
    )
    expect(
      classifyAssistantWaitGate("Create a campaign with a 20% off offer")
    ).toBe("create-campaign-with-offer")
    expect(classifyAssistantWaitGate("Attach it to a campaign")).toBe(
      "create-campaign-with-offer"
    )
    expect(
      classifyAssistantWaitGate("Attach Happy Hour to Summer campaign")
    ).toBe("create-campaign-with-offer")
    expect(classifyAssistantWaitGate("Draft an offer for free dessert")).toBe(
      "offer-path"
    )
    expect(
      classifyAssistantWaitGate("Help me recover this negative feedback")
    ).toBe("recovery-path")
    expect(classifyAssistantWaitGate("Summarise recent feedback")).toBe(
      "retrieve"
    )
    expect(classifyAssistantWaitGate("Tell me a joke")).toBe("refuse")
    expect(classifyAssistantWaitGate("How do I create a campaign?")).toBe(
      "refuse"
    )
    expect(classifyAssistantWaitGate("Buy AI credits with Revolut")).toBe(
      "refuse"
    )
    expect(
      classifyAssistantWaitGate(
        "What's billing Does my account have at the moment?"
      )
    ).toBe("retrieve")
  })

  it("classifies retrieve focus from the ask", () => {
    expect(
      classifyAssistantWaitRetrieveFocus("Summarise recent feedback")
    ).toBe("feedback")
    expect(classifyAssistantWaitRetrieveFocus("How are campaigns doing?")).toBe(
      "campaigns"
    )
    expect(
      classifyAssistantWaitRetrieveFocus("Show offer redemptions")
    ).toBe("offers")
    expect(classifyAssistantWaitRetrieveFocus("What needs attention?")).toBe(
      "attention"
    )
    expect(
      classifyAssistantWaitRetrieveFocus(
        "What's billing Does my account have at the moment?"
      )
    ).toBe("billing")
  })

  it("refuse and out-of-scope waits never rotate retrieve summary copy", () => {
    const retrievePreparing = [
      "Writing the summary…",
      "Drafting the reply…",
      "Checking named facts…",
    ]
    for (const ask of [
      "Tell me a joke",
      "How do I create a campaign?",
      "Buy AI credits",
      "who are you",
    ]) {
      const plan = planAssistantWaitPhrases(ask)
      expect(plan.gate).toBe("refuse")
      for (const step of ["checking", "retrieving", "preparing"] as const) {
        for (let index = 0; index < 8; index += 1) {
          const body = assistantWaitPhraseAt(plan, step, index)
          expect(retrievePreparing).not.toContain(body)
        }
      }
    }
  })

  it("billing retrieve waits use plan and credit phrases", () => {
    const plan = planAssistantWaitPhrases(
      "What's billing Does my account have at the moment?"
    )
    expect(plan.gate).toBe("retrieve")
    expect(plan.retrieveFocus).toBe("billing")
    expect(assistantWaitPhraseAt(plan, "retrieving", 0)).toBe(
      "Loading plan and billing status…"
    )
    expect(assistantWaitPhraseAt(plan, "preparing", 0)).toBe(
      "Writing the plan and credits answer…"
    )
    expect(assistantWaitPhraseAt(plan, "preparing", 0)).not.toBe(
      "Writing the summary…"
    )
  })

  it("returns truthful rotating phrases per gate and step", () => {
    const campaign = planAssistantWaitPhrases("Create a campaign for SMS")
    expect(assistantWaitPhraseAt(campaign, "checking", 0)).toBe(
      "Matching the campaign location…"
    )
    expect(assistantWaitPhraseAt(campaign, "preparing", 0)).toBe(
      "Drafting the campaign message…"
    )

    const offer = planAssistantWaitPhrases("Create an offer draft")
    expect(assistantWaitPhraseAt(offer, "checking", 0)).toBe(
      "Matching offer type and value…"
    )
    expect(assistantWaitPhraseAt(offer, "preparing", 2)).toBe(
      "Writing offer title and terms…"
    )

    const feedback = planAssistantWaitPhrases("Summarise recent feedback")
    expect(assistantWaitPhraseAt(feedback, "retrieving", 0)).toBe(
      "Loading feedback scores…"
    )
    expect(assistantWaitPhraseAt(feedback, "preparing", 0)).toBe(
      "Summarising feedback…"
    )
  })

  it("exports default retrieve bodies and recognises wait phrases", () => {
    expect(ASSISTANT_WAIT_BODY).toBe("Matching your question to venue data…")
    expect(ASSISTANT_WAIT_RETRIEVING_BODY).toBe(
      "Loading campaigns, feedback, and offers…"
    )
    expect(ASSISTANT_WAIT_PREPARING_BODY).toBe("Writing the summary…")
    expect(isAssistantCheckingWaitBody(ASSISTANT_WAIT_BODY)).toBe(true)
    expect(isAssistantCheckingWaitBody(ASSISTANT_WAIT_RETRIEVING_BODY)).toBe(
      false
    )
    expect(isAssistantWaitPhraseBody(ASSISTANT_WAIT_PREPARING_BODY)).toBe(true)
  })
})
