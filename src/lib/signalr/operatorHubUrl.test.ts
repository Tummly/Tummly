import { describe, expect, it } from "vitest"

import { operatorHubUrl } from "./operatorHubUrl"

describe("operatorHubUrl", () => {
  it("strips a trailing /api segment and appends the hub path", () => {
    expect(
      operatorHubUrl("https://api.example.com/api", "/hubs/notifications")
    ).toBe("https://api.example.com/hubs/notifications")
    expect(
      operatorHubUrl("https://api.example.com/api/", "/hubs/feedback-home")
    ).toBe("https://api.example.com/hubs/feedback-home")
  })

  it("leaves bases without /api unchanged before appending the hub path", () => {
    expect(
      operatorHubUrl("https://api.example.com", "/hubs/notifications")
    ).toBe("https://api.example.com/hubs/notifications")
  })
})
