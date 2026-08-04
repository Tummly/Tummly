import { describe, expect, it } from "vitest"

import {
  RESPONSE_SETUP_INCLUDE_NOTES_HELPER,
  RESPONSE_SETUP_INCLUDE_NOTES_LABEL,
  RESPONSE_SETUP_INCLUDE_NOTES_PLACEHOLDER,
  RESPONSE_SETUP_PURPOSE_LABEL,
  RESPONSE_SETUP_STEP_DESCRIPTION,
  RESPONSE_SETUP_STEP_HEADING,
  RESPONSE_SETUP_TONE_LABEL,
  buildResponseSetupChannelCards,
  mapResponseSetupSummaryChrome,
} from "./responseSetupPresentation"

describe("responseSetupPresentation", () => {
  it("keeps Response setup heading and field labels as in Figma", () => {
    expect(RESPONSE_SETUP_STEP_HEADING).toBe(
      "How should the guest be contacted?"
    )
    expect(RESPONSE_SETUP_STEP_DESCRIPTION).toBe(
      "Choose how the guest should be contacted and what the response should achieve."
    )
    expect(RESPONSE_SETUP_PURPOSE_LABEL).toBe(
      "What should the response achieve?"
    )
    expect(RESPONSE_SETUP_TONE_LABEL).toBe("Response tone")
    expect(RESPONSE_SETUP_INCLUDE_NOTES_LABEL).toBe(
      "Anything the response should include?"
    )
    expect(RESPONSE_SETUP_INCLUDE_NOTES_PLACEHOLDER).toBe(
      "Add any facts or actions the team has confirmed…"
    )
    expect(RESPONSE_SETUP_INCLUDE_NOTES_HELPER).toBe(
      "Only include information the restaurant has verified. Tummly will not invent refunds, compensation or operational actions."
    )
  })

  it("builds Email/SMS channel cards with masked destination and availability line", () => {
    expect(
      buildResponseSetupChannelCards({
        availableChannels: ["email"],
        selectedChannel: "email",
        maskedDestination: "m••••@email.com",
      })
    ).toEqual([
      {
        channel: "email",
        title: "Email m••••@email.com",
        availabilityLine: "Available · No email credits required",
        selected: true,
      },
    ])

    expect(
      buildResponseSetupChannelCards({
        availableChannels: ["sms"],
        selectedChannel: "sms",
        maskedDestination: "••••4821",
      })
    ).toEqual([
      {
        channel: "sms",
        title: "SMS ••••4821",
        availabilityLine: "Available · Estimated usage: 1 SMS credit",
        selected: true,
      },
    ])

    expect(
      buildResponseSetupChannelCards({
        availableChannels: ["email", "sms"],
        selectedChannel: "sms",
        maskedDestinationByChannel: {
          email: "m••••@email.com",
          sms: "••••4821",
        },
      })
    ).toEqual([
      {
        channel: "email",
        title: "Email m••••@email.com",
        availabilityLine: "Available · No email credits required",
        selected: false,
      },
      {
        channel: "sms",
        title: "SMS ••••4821",
        availabilityLine: "Available · Estimated usage: 1 SMS credit",
        selected: true,
      },
    ])
  })

  it("maps Feedback summary Classification, Contact, and Issue tags", () => {
    expect(
      mapResponseSetupSummaryChrome(
        {
          classificationStatus: "Succeeded",
          sentiment: "negative",
          detectedTags: ["FoodQuality", "WaitTime"],
        },
        "email_available"
      )
    ).toEqual({
      classificationStatus: "Succeeded",
      classificationSentiment: "negative",
      contactLabel: "Email available",
      issueTagLabels: ["Food quality", "Wait time"],
    })

    expect(
      mapResponseSetupSummaryChrome(
        {
          classificationStatus: "Pending",
          sentiment: null,
          detectedTags: ["FoodQuality"],
        },
        "sms_available"
      )
    ).toEqual({
      classificationStatus: "Pending",
      classificationSentiment: null,
      contactLabel: "SMS available",
      issueTagLabels: null,
    })
  })
})
