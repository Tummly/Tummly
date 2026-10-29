import type { ReactElement } from "react"
import { describe, expect, it, vi } from "vitest"
import { Navigate } from "react-router-dom"

vi.mock("react-router-dom", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router-dom")>()
  return {
    ...actual,
    useLocation: () => ({ search: "?t=signed-token" }),
  }
})

import { UnsubscribeRedirect } from "./UnsubscribeRedirect"

describe("UnsubscribeRedirect", () => {
  it("redirects /unsubscribe?t= to /preferences?t= with replace", () => {
    const element = UnsubscribeRedirect() as ReactElement<{
      to: { pathname: string; search: string }
      replace: boolean
    }>

    expect(element.type).toBe(Navigate)
    expect(element.props.to).toEqual({
      pathname: "/preferences",
      search: "?t=signed-token",
    })
    expect(element.props.replace).toBe(true)
  })
})
