import { describe, expect, it } from "vitest"

import {
  bindExclusiveAssistantCloser,
  closeExclusiveAssistantDrawer,
  closeExclusivePeerRightDrawers,
  registerExclusivePeerCloser,
} from "./assistantExclusiveOpen"

describe("assistantExclusiveOpen", () => {
  it("closes registered peer Drawers and the Assistant from the exclusive helpers", () => {
    const closed: string[] = []
    const unbindAssistant = bindExclusiveAssistantCloser(() => {
      closed.push("assistant")
    })
    const unbindPeer = registerExclusivePeerCloser(() => {
      closed.push("peer")
    })

    closeExclusivePeerRightDrawers()
    expect(closed).toEqual(["peer"])

    closeExclusiveAssistantDrawer()
    expect(closed).toEqual(["peer", "assistant"])

    unbindPeer()
    unbindAssistant()
    closeExclusivePeerRightDrawers()
    closeExclusiveAssistantDrawer()
    expect(closed).toEqual(["peer", "assistant"])
  })
})
