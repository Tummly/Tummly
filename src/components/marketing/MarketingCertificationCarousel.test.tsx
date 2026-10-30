// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

import { MarketingCertificationCarousel } from "@/components/marketing/MarketingCertificationCarousel"
import { PRODUCT_TRUST } from "@/content/marketing/productPage"

const TRACK = { clientWidth: 400, scrollWidth: 1200 }

let scrollBy: ReturnType<typeof vi.fn>

beforeEach(() => {
  // jsdom has no layout: give the track a width and content wider than it.
  vi.stubGlobal(
    "ResizeObserver",
    class {
      observe() {}
      disconnect() {}
      unobserve() {}
    },
  )
  scrollBy = vi.fn()
  Object.defineProperty(HTMLElement.prototype, "clientWidth", {
    configurable: true,
    get: () => TRACK.clientWidth,
  })
  Object.defineProperty(HTMLElement.prototype, "scrollWidth", {
    configurable: true,
    get: () => TRACK.scrollWidth,
  })
  HTMLElement.prototype.scrollBy = scrollBy as unknown as typeof HTMLElement.prototype.scrollBy
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  Reflect.deleteProperty(HTMLElement.prototype, "clientWidth")
  Reflect.deleteProperty(HTMLElement.prototype, "scrollWidth")
  Reflect.deleteProperty(HTMLElement.prototype, "scrollBy")
})

function renderCarousel() {
  render(
    <MarketingCertificationCarousel
      certifications={PRODUCT_TRUST.certifications}
    />,
  )
  return {
    prev: screen.getByRole("button", { name: "Previous certifications" }),
    next: screen.getByRole("button", { name: "Next certifications" }),
    track: screen.getByRole("list"),
  }
}

describe("MarketingCertificationCarousel", () => {
  it("lists every certification label", () => {
    renderCarousel()
    for (const cert of PRODUCT_TRUST.certifications) {
      expect(screen.getByText(cert.label)).toBeTruthy()
    }
  })

  it("holds Previous at the start and keeps Next active", () => {
    const { prev, next } = renderCarousel()
    expect(prev.getAttribute("aria-disabled")).toBe("true")
    expect(next.getAttribute("aria-disabled")).toBe("false")
    // aria-disabled, not disabled: focus can stay on the button.
    expect(prev.hasAttribute("disabled")).toBe(false)
  })

  it("scrolls forward on Next and ignores Previous at the start", () => {
    const { prev, next } = renderCarousel()
    fireEvent.click(prev)
    expect(scrollBy).not.toHaveBeenCalled()
    fireEvent.click(next)
    expect(scrollBy).toHaveBeenCalledWith({ left: 300, behavior: "smooth" })
  })

  it("enables Previous after scrolling away from the start", () => {
    const { prev, track } = renderCarousel()
    act(() => {
      track.scrollLeft = 300
      fireEvent.scroll(track)
    })
    expect(prev.getAttribute("aria-disabled")).toBe("false")
    fireEvent.click(prev)
    expect(scrollBy).toHaveBeenCalledWith({ left: -300, behavior: "smooth" })
  })

  it("holds Next at the end without losing focus", () => {
    const { next, track } = renderCarousel()
    next.focus()
    act(() => {
      track.scrollLeft = TRACK.scrollWidth - TRACK.clientWidth
      fireEvent.scroll(track)
    })
    expect(next.getAttribute("aria-disabled")).toBe("true")
    expect(document.activeElement).toBe(next)
    fireEvent.click(next)
    expect(scrollBy).not.toHaveBeenCalled()
  })
})
