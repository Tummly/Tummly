import { describe, expect, it } from "vitest"

import { isAccountAlreadyProvisionedMessage } from "@/lib/setupAccountErrors"

describe("isAccountAlreadyProvisionedMessage", () => {
  it("recognises account-already-created responses", () => {
    expect(isAccountAlreadyProvisionedMessage("Account already created.")).toBe(
      true
    )
  })

  it("recognises user-already-exists responses", () => {
    expect(isAccountAlreadyProvisionedMessage("User already exists.")).toBe(
      true
    )
  })

  it("rejects unrelated setup errors", () => {
    expect(
      isAccountAlreadyProvisionedMessage("Invalid invite token.")
    ).toBe(false)
  })
})
