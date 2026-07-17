import { describe, expect, it } from "vitest"

import { feedbackHomeHubUrl } from "./connectFeedbackHomeHub"

describe("feedbackHomeHubUrl", () => {
  it("maps /api base URL to /hubs/feedback-home on the same host", () => {
    expect(feedbackHomeHubUrl("https://api.example.com/api")).toBe(
      "https://api.example.com/hubs/feedback-home"
    )
    expect(feedbackHomeHubUrl("https://api.example.com/api/")).toBe(
      "https://api.example.com/hubs/feedback-home"
    )
  })
})
