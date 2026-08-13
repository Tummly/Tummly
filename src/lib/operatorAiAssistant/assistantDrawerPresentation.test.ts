import { describe, expect, it } from "vitest"

import { OPERATOR_RIGHT_DRAWER_CONTENT_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"

import {
  OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_OPEN_CLASS,
  OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS,
  OPERATOR_SIDENAV_COLLAPSED_PX,
  OPERATOR_SIDENAV_OPEN_PX,
  assistantDrawerContentClass,
  assistantDrawerMountsOverlay,
  paintsAssistantExpand,
} from "./assistantDrawerPresentation"

describe("assistantDrawerPresentation", () => {
  it("reuses the shared 620px token when collapsed or below lg", () => {
    expect(
      assistantDrawerContentClass({
        widthMode: "collapsed",
        viewportAtLeastLg: true,
        sidebarCollapsed: false,
      })
    ).toBe(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS)

    expect(
      assistantDrawerContentClass({
        widthMode: "expanded",
        viewportAtLeastLg: false,
        sidebarCollapsed: false,
      })
    ).toBe(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS)

    expect(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS).toContain(
      "w-[min(620px,100vw)]"
    )
  })

  it("uses a separate Expand width for the open SideNav and the collapsed rail", () => {
    expect(OPERATOR_SIDENAV_OPEN_PX).toBe(260)
    expect(OPERATOR_SIDENAV_COLLAPSED_PX).toBe(52)

    const openRail = assistantDrawerContentClass({
      widthMode: "expanded",
      viewportAtLeastLg: true,
      sidebarCollapsed: false,
    })
    expect(openRail).toContain(OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_OPEN_CLASS)
    expect(openRail).toContain("calc(100vw-260px)")
    expect(openRail).not.toContain("620px")
    expect(openRail).not.toBe(OPERATOR_RIGHT_DRAWER_CONTENT_CLASS)

    const collapsedRail = assistantDrawerContentClass({
      widthMode: "expanded",
      viewportAtLeastLg: true,
      sidebarCollapsed: true,
    })
    expect(collapsedRail).toContain(OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS)
    expect(collapsedRail).toContain("calc(100vw-52px)")
    expect(collapsedRail).not.toContain("620px")
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
