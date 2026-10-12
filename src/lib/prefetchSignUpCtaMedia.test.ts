import { afterEach, describe, expect, it, vi } from "vitest"

type FakeLink = {
  rel: string
  as: string
  type: string
  href: string
  setAttribute: (name: string, value: string) => void
  getAttribute: (name: string) => string | null
}

describe("prefetchSignUpCtaMedia", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.resetModules()
  })

  it("appends poster and video prefetch links once", async () => {
    const appended: FakeLink[] = []
    const head = {
      appendChild: vi.fn((node: FakeLink) => {
        appended.push(node)
        return node
      }),
    }

    vi.stubGlobal("document", {
      createElement: vi.fn((tag: string) => {
        if (tag !== "link") {
          throw new Error(`unexpected tag: ${tag}`)
        }
        const attrs = new Map<string, string>()
        const link: FakeLink = {
          rel: "",
          as: "",
          type: "",
          href: "",
          setAttribute: (name, value) => {
            attrs.set(name, value)
          },
          getAttribute: (name) => attrs.get(name) ?? null,
        }
        return link
      }),
      head,
    })

    const { warmSignUpCtaMedia } = await import("./prefetchSignUpCtaMedia")
    const { signUpCtaVideoUrl } = await import("./marketingMedia")

    warmSignUpCtaMedia()
    warmSignUpCtaMedia()

    expect(head.appendChild).toHaveBeenCalledTimes(2)

    const image = appended.find((node) => node.as === "image")
    const video = appended.find((node) => node.as === "video")

    expect(image?.rel).toBe("prefetch")
    expect(image?.type).toBe("image/avif")
    expect(image?.getAttribute("imagesrcset")).toContain(
      "images/cta/signup-cta-bg-",
    )

    expect(video?.rel).toBe("prefetch")
    expect(video?.type).toBe("video/mp4")
    expect(video?.href).toBe(signUpCtaVideoUrl())
  })
})
