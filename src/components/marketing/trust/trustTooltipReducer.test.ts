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

const CLOSED = { open: false, source: null, closePending: false }

describe("trustTooltipReducer", () => {
  it("uses a 120 ms mouse-leave close delay", () => {
    expect(TRUST_TOOLTIP_CLOSE_DELAY_MS).toBe(120)
  })

  it("opens on mouse enter with the hover source", () => {
    expect(run([{ type: "hover-enter" }])).toEqual({
      open: true,
      source: "hover",
      closePending: false,
    })
  })

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

  it("opens on keyboard focus with the focus source", () => {
    expect(run([{ type: "focus-visible" }])).toEqual({
      open: true,
      source: "focus",
      closePending: false,
    })
  })

  it("does not close a focus-opened tooltip on pointer leave", () => {
    const opened = run([{ type: "focus-visible" }])
    expect(run([{ type: "pointer-leave" }], opened)).toBe(opened)
  })

  it("does not close a click-opened tooltip on pointer leave", () => {
    const opened = run([{ type: "click" }])
    expect(run([{ type: "pointer-leave" }], opened)).toBe(opened)
  })

  it("keeps the first source when a later hover or focus arrives while open", () => {
    expect(run([{ type: "click" }, { type: "hover-enter" }]).source).toBe("click")
    expect(run([{ type: "focus-visible" }, { type: "hover-enter" }]).source).toBe("focus")
  })

  it("click opens with the click source when closed", () => {
    expect(run([{ type: "click" }])).toEqual({
      open: true,
      source: "click",
      closePending: false,
    })
  })

  it("a second click closes, whatever opened it", () => {
    expect(run([{ type: "click" }, { type: "click" }])).toEqual(CLOSED)
    expect(run([{ type: "hover-enter" }, { type: "click" }])).toEqual(CLOSED)
    expect(run([{ type: "focus-visible" }, { type: "click" }])).toEqual(CLOSED)
  })

  it("a click on a tooltip with a pending hover close closes it", () => {
    const state = run([{ type: "hover-enter" }, { type: "pointer-leave" }, { type: "click" }])
    expect(state).toEqual(CLOSED)
  })

  it("escape or an outside pointer or focus leaving closes the tooltip", () => {
    expect(run([{ type: "click" }, { type: "close" }])).toEqual(CLOSED)
    expect(run([{ type: "focus-visible" }, { type: "close" }])).toEqual(CLOSED)
    expect(run([{ type: "hover-enter" }, { type: "close" }])).toEqual(CLOSED)
  })

  it("close on a closed tooltip returns the same state object", () => {
    expect(run([{ type: "close" }])).toBe(TRUST_TOOLTIP_INITIAL_STATE)
  })
})
