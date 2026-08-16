import { describe, expect, it } from "vitest"

import {
  ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS,
  OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS,
  OPERATOR_SIDENAV_COLLAPSED_PX,
  assistantDrawerContentClass,
  assistantDrawerMountsOverlay,
  assistantDrawerOverlayClass,
  paintsAssistantExpand,
} from "./assistantDrawerPresentation"

describe("assistantDrawerPresentation", () => {
  it("uses Conversations fill and the shared 620px width when collapsed or below lg", () => {
    expect(
      assistantDrawerContentClass({
        widthMode: "collapsed",
        viewportAtLeastLg: true,
        sidebarCollapsed: false,
      })
    ).toBe(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS)

    expect(
      assistantDrawerContentClass({
        widthMode: "expanded",
        viewportAtLeastLg: false,
        sidebarCollapsed: false,
      })
    ).toBe(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS)

    expect(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS).toContain(
      "bg-op-assistant-list-background"
    )
    expect(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS).toContain("!select-text")
    expect(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS).toContain("!touch-pan-y")
    expect(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS).toContain(
      "w-[min(620px,100vw)]"
    )
  })

  it("always subtracts the collapsed SideNav rail from Expand width", () => {
    expect(OPERATOR_SIDENAV_COLLAPSED_PX).toBe(52)

    const fromOpenSideNav = assistantDrawerContentClass({
      widthMode: "expanded",
      viewportAtLeastLg: true,
      sidebarCollapsed: false,
    })
    expect(fromOpenSideNav).toContain(OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS)
    expect(fromOpenSideNav).toContain("calc(100vw-52px)")
    expect(fromOpenSideNav).not.toContain("calc(100vw-260px)")
    expect(fromOpenSideNav).toContain("bg-op-assistant-list-background")
    expect(fromOpenSideNav).not.toContain("620px")
    expect(fromOpenSideNav).not.toBe(ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS)

    const collapsedRail = assistantDrawerContentClass({
      widthMode: "expanded",
      viewportAtLeastLg: true,
      sidebarCollapsed: true,
    })
    expect(collapsedRail).toContain(OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS)
    expect(collapsedRail).toContain("calc(100vw-52px)")
    expect(collapsedRail).toContain("bg-op-assistant-list-background")
    expect(collapsedRail).not.toContain("620px")
  })

  it("uses the Assistant overlay token and removes backdrop blur", () => {
    expect(assistantDrawerOverlayClass()).toContain("bg-op-assistant-overlay")
    expect(assistantDrawerOverlayClass()).toContain(
      "supports-backdrop-filter:backdrop-blur-none"
    )
  })

  it("unmounts the vaul Overlay in Expand so modal=false does not skip a hook", () => {
    expect(
      assistantDrawerMountsOverlay({
        widthMode: "expanded",
        viewportAtLeastLg: true,
      })
    ).toBe(false)
    expect(
      assistantDrawerMountsOverlay({
        widthMode: "collapsed",
        viewportAtLeastLg: true,
      })
    ).toBe(true)
    expect(
      assistantDrawerMountsOverlay({
        widthMode: "expanded",
        viewportAtLeastLg: false,
      })
    ).toBe(true)
  })

  it("paints Expand only when width mode is expanded and the viewport is at least lg", () => {
    expect(
      paintsAssistantExpand({
        widthMode: "expanded",
        viewportAtLeastLg: true,
      })
    ).toBe(true)
    expect(
      paintsAssistantExpand({
        widthMode: "expanded",
        viewportAtLeastLg: false,
      })
    ).toBe(false)
    expect(
      paintsAssistantExpand({
        widthMode: "collapsed",
        viewportAtLeastLg: true,
      })
    ).toBe(false)
  })
})
