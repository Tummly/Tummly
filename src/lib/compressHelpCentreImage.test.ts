import { afterEach, describe, expect, it, vi } from "vitest"

import {
  compressHelpCentreImage,
  helpCentreImageOutputFormat,
  isHelpCentreCompressibleImage,
  replaceFileExtension,
  scaleImageDimensions,
} from "@/lib/compressHelpCentreImage"

describe("isHelpCentreCompressibleImage", () => {
  it("accepts jpeg, png, and webp", () => {
    expect(
      isHelpCentreCompressibleImage(
        new File([], "a.jpg", { type: "image/jpeg" })
      )
    ).toBe(true)
    expect(
      isHelpCentreCompressibleImage(
        new File([], "a.png", { type: "image/png" })
      )
    ).toBe(true)
    expect(
      isHelpCentreCompressibleImage(
        new File([], "a.webp", { type: "image/webp" })
      )
    ).toBe(true)
  })

  it("skips gif and pdf", () => {
    expect(
      isHelpCentreCompressibleImage(
        new File([], "a.gif", { type: "image/gif" })
      )
    ).toBe(false)
    expect(
      isHelpCentreCompressibleImage(
        new File([], "a.pdf", { type: "application/pdf" })
      )
    ).toBe(false)
  })
})

describe("scaleImageDimensions", () => {
  it("leaves dimensions under the max edge unchanged", () => {
    expect(scaleImageDimensions(1200, 800, 2048)).toEqual({
      width: 1200,
      height: 800,
    })
  })

  it("scales the longest edge down to the max", () => {
    expect(scaleImageDimensions(4096, 2048, 2048)).toEqual({
      width: 2048,
      height: 1024,
    })
  })

  it("scales portrait images by height", () => {
    expect(scaleImageDimensions(1080, 4096, 2048)).toEqual({
      width: 540,
      height: 2048,
    })
  })
})

describe("replaceFileExtension", () => {
  it("replaces an existing extension", () => {
    expect(replaceFileExtension("shot.PNG", ".jpg")).toBe("shot.jpg")
  })

  it("appends when there is no extension", () => {
    expect(replaceFileExtension("shot", ".jpg")).toBe("shot.jpg")
  })
})

describe("helpCentreImageOutputFormat", () => {
  it("keeps webp as webp", () => {
    expect(helpCentreImageOutputFormat("image/webp")).toMatchObject({
      type: "image/webp",
      extension: ".webp",
    })
  })

  it("maps jpeg and png to jpeg", () => {
    expect(helpCentreImageOutputFormat("image/jpeg").type).toBe("image/jpeg")
    expect(helpCentreImageOutputFormat("image/png")).toMatchObject({
      type: "image/jpeg",
      extension: ".jpg",
    })
  })
})

describe("compressHelpCentreImage", () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it("returns gif and pdf unchanged", async () => {
    const gif = new File([new Uint8Array([1, 2, 3])], "a.gif", {
      type: "image/gif",
    })
    const pdf = new File([new Uint8Array([4, 5, 6])], "a.pdf", {
      type: "application/pdf",
    })

    await expect(compressHelpCentreImage(gif)).resolves.toBe(gif)
    await expect(compressHelpCentreImage(pdf)).resolves.toBe(pdf)
  })

  it("returns a smaller jpeg when canvas encode succeeds", async () => {
    const original = new File([new Uint8Array(20_000)], "shot.png", {
      type: "image/png",
    })

    const close = vi.fn()
    vi.stubGlobal(
      "createImageBitmap",
      vi.fn(async () => ({
        width: 4000,
        height: 2000,
        close,
      }))
    )

    const compressedBytes = new Uint8Array(500)
    const toBlob = vi.fn(
      (callback: BlobCallback, _type?: string, _quality?: number) => {
        callback(new Blob([compressedBytes], { type: "image/jpeg" }))
      }
    )

    const fillRect = vi.fn()
    const drawImage = vi.fn()
    const getContext = vi.fn(() => ({
      fillStyle: "",
      fillRect,
      drawImage,
    }))

    vi.stubGlobal("document", {
      createElement: vi.fn(() => ({
        width: 0,
        height: 0,
        getContext,
        toBlob,
      })),
    })

    const result = await compressHelpCentreImage(original)

    expect(result).not.toBe(original)
    expect(result.name).toBe("shot.jpg")
    expect(result.type).toBe("image/jpeg")
    expect(result.size).toBe(500)
    expect(close).toHaveBeenCalledOnce()
    expect(fillRect).toHaveBeenCalled()
    expect(drawImage).toHaveBeenCalled()
  })

  it("keeps the original when the encoded blob is not smaller", async () => {
    const original = new File([new Uint8Array(100)], "shot.jpg", {
      type: "image/jpeg",
    })

    vi.stubGlobal(
      "createImageBitmap",
      vi.fn(async () => ({
        width: 100,
        height: 100,
        close: vi.fn(),
      }))
    )

    vi.stubGlobal("document", {
      createElement: vi.fn(() => ({
        width: 0,
        height: 0,
        getContext: () => ({
          fillStyle: "",
          fillRect: vi.fn(),
          drawImage: vi.fn(),
        }),
        toBlob: (callback: BlobCallback) => {
          callback(new Blob([new Uint8Array(200)], { type: "image/jpeg" }))
        },
      })),
    })

    await expect(compressHelpCentreImage(original)).resolves.toBe(original)
  })

  it("keeps the original when createImageBitmap fails", async () => {
    const original = new File([new Uint8Array(100)], "shot.jpg", {
      type: "image/jpeg",
    })

    vi.stubGlobal(
      "createImageBitmap",
      vi.fn(async () => {
        throw new Error("decode failed")
      })
    )

    await expect(compressHelpCentreImage(original)).resolves.toBe(original)
  })
})
