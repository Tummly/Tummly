import type { SyntheticEvent } from "react"
import { describe, expect, it } from "vitest"

import { DemoMediaImage } from "@/components/marketing/demo/DemoMediaImage"
import { hideBrokenDemoMedia } from "@/lib/productDemo/demoMedia"

describe("DemoMediaImage", () => {
  it("hides a demo image that fails to load", () => {
    const target = { hidden: false }

    hideBrokenDemoMedia({
      currentTarget: target,
    } as unknown as SyntheticEvent<HTMLImageElement>)

    expect(target.hidden).toBe(true)
  })

  it("wires the broken-media fallback onto every demo image", () => {
    const element = DemoMediaImage({ src: "data:image/png;base64,AAAA", alt: "QR" })

    expect(element.props.src).toBe("data:image/png;base64,AAAA")
    expect(element.props.alt).toBe("QR")
    expect(element.props.onError).toBe(hideBrokenDemoMedia)
  })
})
