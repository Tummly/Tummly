import { describe, expect, it } from "vitest"

import { buildExternalAuthStartUrl } from "./externalAuthStart"

describe("buildExternalAuthStartUrl", () => {
  it("builds google start url with returnPath", () => {
    expect(
      buildExternalAuthStartUrl("google", "/login", "https://api.example.com/api")
    ).toBe(
      "https://api.example.com/api/auth/external/google/start?returnPath=%2Flogin"
    )
  })

  it("builds microsoft start url and strips trailing slash on apiBase", () => {
    expect(
      buildExternalAuthStartUrl(
        "microsoft",
        "/signup",
        "https://api.example.com/api/"
      )
    ).toBe(
      "https://api.example.com/api/auth/external/microsoft/start?returnPath=%2Fsignup"
    )
  })
})
