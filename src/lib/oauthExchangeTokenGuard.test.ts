import { afterEach, describe, expect, it, vi } from "vitest"

import {
  resetOAuthExchangeTokenGuardForTests,
  runOAuthExchangeOnce,
} from "./oauthExchangeTokenGuard"

describe("runOAuthExchangeOnce", () => {
  afterEach(() => {
    resetOAuthExchangeTokenGuardForTests()
  })

  it("runs the runner only once for the same token", async () => {
    const runner = vi.fn(async () => "ok")

    const first = runOAuthExchangeOnce("ticket-a", runner)
    const second = runOAuthExchangeOnce("ticket-a", runner)

    await expect(first).resolves.toBe("ok")
    await expect(second).resolves.toBe("ok")
    expect(runner).toHaveBeenCalledTimes(1)
  })

  it("runs again for a different token", async () => {
    const runner = vi.fn(async () => "ok")

    await runOAuthExchangeOnce("ticket-a", runner)
    await runOAuthExchangeOnce("ticket-b", runner)

    expect(runner).toHaveBeenCalledTimes(2)
  })

  it("rejects when token is empty", async () => {
    await expect(
      runOAuthExchangeOnce("  ", async () => "ok")
    ).rejects.toThrow(/Missing OAuth exchange token/)
  })

  it("allows the same token to retry after rejection", async () => {
    const runner = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce("ok")

    await expect(
      runOAuthExchangeOnce("ticket-retry", runner)
    ).rejects.toThrow(/network/)

    await expect(runOAuthExchangeOnce("ticket-retry", runner)).resolves.toBe(
      "ok"
    )
    expect(runner).toHaveBeenCalledTimes(2)
  })
})
