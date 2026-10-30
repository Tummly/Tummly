import { describe, expect, it } from "vitest"

import {
  TRUST_TOOLTIP_CLOSE_DELAY_MS,
  TRUST_TOOLTIP_INITIAL_STATE,
  trustTooltipReducer,
  type TrustTooltipEvent,
  type TrustTooltipState,
} from "./trustTooltipReducer"

function run(events: TrustTooltipEvent[], from: TrustTooltipState = TRUST_TOOLTIP_INITIAL_STATE) {
  return events.reduce(trustTooltipReducer, from)
}

const CLOSED: TrustTooltipState = {
  open: false,
  source: null,
  closePending: false,
  suppressFocus: false,
}

describe("trustTooltipReducer", () => {
  it("uses a 120 ms mouse-leave close delay", () => {
    expect(TRUST_TOOLTIP_CLOSE_DELAY_MS).toBe(120)
  })

  describe("opening", () => {
    it("opens on mouse enter with the hover source", () => {
      expect(run([{ type: "hover-enter" }])).toEqual({
        open: true,
        source: "hover",
        closePending: false,
        suppressFocus: false,
      })
    })

    it("opens on keyboard focus with the focus source", () => {
      expect(run([{ type: "focus-visible" }])).toEqual({
        open: true,
        source: "focus",
        closePending: false,
        suppressFocus: false,
      })
    })

    it("click opens with the click source when closed", () => {
      expect(run([{ type: "click" }])).toEqual({
        open: true,
        source: "click",
        closePending: false,
        suppressFocus: false,
      })
    })
  })

  describe("mouse hover", () => {
    it("schedules a close when the pointer leaves a hover-opened tooltip", () => {
      const state = run([{ type: "hover-enter" }, { type: "pointer-leave" }])
      expect(state.open).toBe(true)
      expect(state.closePending).toBe(true)
    })

    it("closes when the close timer fires after a hover leave", () => {
      expect(run([{ type: "hover-enter" }, { type: "pointer-leave" }, { type: "close-timer" }])).toEqual(CLOSED)
    })

    it("keeps the tooltip open when the pointer enters the content before the timer fires", () => {
      const state = run([
        { type: "hover-enter" },
        { type: "pointer-leave" },
        { type: "content-enter" },
        { type: "close-timer" },
      ])
      expect(state.open).toBe(true)
      expect(state.closePending).toBe(false)
    })

    it("schedules a close again when the pointer leaves the content", () => {
      const state = run([
        { type: "hover-enter" },
        { type: "pointer-leave" },
        { type: "content-enter" },
        { type: "pointer-leave" },
      ])
      expect(state.closePending).toBe(true)
      expect(run([{ type: "close-timer" }], state)).toEqual(CLOSED)
    })

    it("cancels a pending close when the pointer re-enters the trigger", () => {
      const state = run([
        { type: "hover-enter" },
        { type: "pointer-leave" },
        { type: "hover-enter" },
        { type: "close-timer" },
      ])
      expect(state.open).toBe(true)
    })

    it("ignores a stray close timer when nothing is pending", () => {
      const opened = run([{ type: "focus-visible" }])
      expect(run([{ type: "close-timer" }], opened)).toBe(opened)
    })

    it("does not close a focus-opened tooltip on pointer leave", () => {
      const opened = run([{ type: "focus-visible" }])
      expect(run([{ type: "pointer-leave" }], opened)).toBe(opened)
    })

    it("does not close a click-opened tooltip on pointer leave", () => {
      const opened = run([{ type: "click" }])
      expect(run([{ type: "pointer-leave" }], opened)).toBe(opened)
    })

    it("a focus event during a hover open switches the source to focus and clears the pending close", () => {
      const state = run([{ type: "hover-enter" }, { type: "pointer-leave" }, { type: "focus-visible" }])
      expect(state.open).toBe(true)
      expect(state.source).toBe("focus")
      expect(state.closePending).toBe(false)
      expect(run([{ type: "close-timer" }], state).open).toBe(true)
    })
  })

  describe("click", () => {
    it("a click on a hover-opened tooltip keeps it open with the click source", () => {
      const state = run([{ type: "hover-enter" }, { type: "click" }])
      expect(state.open).toBe(true)
      expect(state.source).toBe("click")
      expect(state.closePending).toBe(false)
    })

    it("a click on a focus-opened tooltip keeps it open with the click source", () => {
      const state = run([{ type: "focus-visible" }, { type: "click" }])
      expect(state.open).toBe(true)
      expect(state.source).toBe("click")
    })

    it("a click cancels a pending hover close and keeps the tooltip open", () => {
      const state = run([{ type: "hover-enter" }, { type: "pointer-leave" }, { type: "click" }])
      expect(state.open).toBe(true)
      expect(state.source).toBe("click")
      expect(state.closePending).toBe(false)
      expect(run([{ type: "close-timer" }], state).open).toBe(true)
    })

    it("a click on a click-opened tooltip closes it", () => {
      expect(run([{ type: "click" }, { type: "click" }])).toEqual(CLOSED)
    })

    it("hover then click then click closes", () => {
      expect(run([{ type: "hover-enter" }, { type: "click" }, { type: "click" }])).toEqual(CLOSED)
    })

    it("focus then click then click closes", () => {
      expect(run([{ type: "focus-visible" }, { type: "click" }, { type: "click" }])).toEqual(CLOSED)
    })

    it("keeps the first source when a later hover or focus arrives while open", () => {
      expect(run([{ type: "click" }, { type: "hover-enter" }]).source).toBe("click")
      expect(run([{ type: "focus-visible" }, { type: "hover-enter" }]).source).toBe("focus")
    })
  })

  describe("closing", () => {
    it("close (focus left) closes the tooltip", () => {
      expect(run([{ type: "click" }, { type: "close" }])).toEqual(CLOSED)
      expect(run([{ type: "focus-visible" }, { type: "close" }])).toEqual(CLOSED)
      expect(run([{ type: "hover-enter" }, { type: "close" }])).toEqual(CLOSED)
    })

    it("close on a closed tooltip returns the same state object", () => {
      expect(run([{ type: "close" }])).toBe(TRUST_TOOLTIP_INITIAL_STATE)
    })
  })

  describe("no reopen after Escape or an outside pointer", () => {
    it("dismiss (Escape) closes and suppresses the next focus", () => {
      const state = run([{ type: "click" }, { type: "dismiss" }])
      expect(state.open).toBe(false)
      expect(state.suppressFocus).toBe(true)
    })

    it("the focus Radix returns after Escape does not reopen", () => {
      const state = run([{ type: "click" }, { type: "dismiss" }, { type: "focus-visible" }])
      expect(state.open).toBe(false)
      expect(state.suppressFocus).toBe(false)
    })

    it("only the first focus after a dismiss is suppressed", () => {
      const state = run([
        { type: "dismiss" },
        { type: "focus-visible" },
        { type: "focus-visible" },
      ])
      expect(state.open).toBe(true)
      expect(state.source).toBe("focus")
    })

    it("an input event ends the suppression, so a later focus opens", () => {
      const state = run([{ type: "dismiss" }, { type: "input" }, { type: "focus-visible" }])
      expect(state.open).toBe(true)
      expect(state.source).toBe("focus")
    })

    it("a hover after a dismiss still opens", () => {
      expect(run([{ type: "dismiss" }, { type: "hover-enter" }]).open).toBe(true)
    })

    it("a focus-out close does not suppress a later focus", () => {
      const state = run([{ type: "focus-visible" }, { type: "close" }, { type: "focus-visible" }])
      expect(state.open).toBe(true)
    })

    it("input on a state without suppression returns the same state object", () => {
      expect(run([{ type: "input" }])).toBe(TRUST_TOOLTIP_INITIAL_STATE)
    })
  })
})
