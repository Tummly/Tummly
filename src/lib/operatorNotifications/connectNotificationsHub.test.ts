import { describe, expect, it } from "vitest"

import { notificationsHubUrl } from "./connectNotificationsHub"

describe("notificationsHubUrl", () => {
  it("maps /api base URL to /hubs/notifications on the same host", () => {
    expect(
      notificationsHubUrl("https://api.example.com/api")
    ).toBe("https://api.example.com/hubs/notifications")
    expect(
      notificationsHubUrl("https://api.example.com/api/")
    ).toBe("https://api.example.com/hubs/notifications")
  })
})
