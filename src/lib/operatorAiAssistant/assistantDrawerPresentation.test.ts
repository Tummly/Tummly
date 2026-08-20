import { describe, expect, it } from "vitest"

import {
  ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS,
  ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS,
  OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS,
  OPERATOR_SIDENAV_COLLAPSED_PX,
  assistantComposerDockClass,
  assistantComposerRailClass,
  assistantConversationStageClass,
  assistantDrawerContentClass,
  assistantDrawerMountsOverlay,
  assistantDrawerOverlayClass,
  assistantThreadBodyClass,
  assistantThreadRailClass,
  assistantThreadStickAnchor,
  paintsAssistantExpand,
  stickAssistantThreadToBottom,
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

  it("puts the Expand thread scrollbar on the pane bleed and centers an 800px rail", () => {
    expect(ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS).toContain("max-w-[800px]")
    expect(ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS).toContain("mx-auto")
    expect(ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS).toContain("px-[30px]")

    const expandedStage = assistantConversationStageClass(true)
    expect(expandedStage).toContain("gap-[30px]")
    expect(expandedStage).toContain("pb-4")
    expect(expandedStage).not.toContain("px-[30px]")
    expect(expandedStage).not.toContain("pb-[50px]")
    expect(assistantConversationStageClass(false)).not.toContain("gap-[30px]")
    expect(assistantConversationStageClass(false)).not.toContain("pb-4")

    expect(assistantThreadBodyClass(true)).toContain("overflow-y-auto")
    expect(assistantThreadBodyClass(true)).not.toContain("px-[30px]")
    expect(assistantThreadBodyClass(true)).not.toContain("max-w-[800px]")
    expect(assistantThreadBodyClass(false)).toContain("px-[30px]")
    expect(assistantThreadBodyClass(false)).toContain("pb-[30px]")

    expect(assistantThreadRailClass(true)).toContain("max-w-[800px]")
    expect(assistantThreadRailClass(true)).toContain("px-[30px]")
    expect(assistantThreadRailClass(true)).not.toContain("flex-1")
    expect(assistantThreadRailClass(false)).not.toContain("max-w-[800px]")

    expect(assistantComposerDockClass(true)).not.toContain("px-[30px]")
    expect(assistantComposerDockClass(false)).toContain("px-[30px]")
    expect(assistantComposerDockClass(false)).toContain("pb-4")
    expect(assistantComposerDockClass(false)).not.toContain("pb-[30px]")

    expect(assistantComposerRailClass(true)).toContain("max-w-[800px]")
    expect(assistantComposerRailClass(true)).toContain("px-[30px]")
    expect(assistantComposerRailClass(false)).toBe("contents")
  })

  it("sticks the thread to the latest user, wait, or assistant row", () => {
    expect(
      assistantThreadStickAnchor({
        showList: true,
        showGreeting: false,
        messages: [{ id: "1", role: "user", body: "Hi" }],
      })
    ).toBeNull()
    expect(
      assistantThreadStickAnchor({
        showList: false,
        showGreeting: true,
        messages: [],
      })
    ).toBeNull()

    const afterSend = assistantThreadStickAnchor({
      showList: false,
      showGreeting: false,
      messages: [{ id: "u1", role: "user", body: "What needs attention?" }],
    })
    expect(afterSend).toBe("1:u1:user:What needs attention?")

    const whileWait = assistantThreadStickAnchor({
      showList: false,
      showGreeting: false,
      messages: [
        { id: "u1", role: "user", body: "What needs attention?" },
        { id: "w1", role: "wait", body: "Retrieving data…" },
      ],
    })
    expect(whileWait).toBe("2:w1:wait:Retrieving data…")

    const waitProgress = assistantThreadStickAnchor({
      showList: false,
      showGreeting: false,
      messages: [
        { id: "u1", role: "user", body: "What needs attention?" },
        { id: "w1", role: "wait", body: "Preparing answer…" },
      ],
    })
    expect(waitProgress).not.toBe(whileWait)

    const afterAnswer = assistantThreadStickAnchor({
      showList: false,
      showGreeting: false,
      messages: [
        { id: "u1", role: "user", body: "What needs attention?" },
        {
          id: "a1",
          role: "assistant",
          body: "Slow service is the main issue.",
        },
      ],
    })
    expect(afterAnswer).toBe(
      "2:a1:assistant:Slow service is the main issue."
    )
  })

  it("sets the thread scrollTop to the end of the body", () => {
    const body = { scrollTop: 120, scrollHeight: 960 }
    stickAssistantThreadToBottom(body as HTMLElement)
    expect(body.scrollTop).toBe(960)
  })
})
