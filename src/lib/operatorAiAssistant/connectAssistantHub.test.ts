import { describe, expect, it } from "vitest"

import { assistantHubUrl } from "./connectAssistantHub"

describe("assistantHubUrl", () => {
  it("maps the API base URL to the Assistant hub on the same host", () => {
    expect(assistantHubUrl("https://api.example.com/api")).toBe(
      "https://api.example.com/hubs/assistant"
    )
    expect(assistantHubUrl("https://api.example.com/api/")).toBe(
      "https://api.example.com/hubs/assistant"
    )
  })
})
