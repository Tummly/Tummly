import { afterEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/axiosInstance", () => ({
  default: {
    post: vi.fn(),
  },
}))

import axiosInstance from "@/api/axiosInstance"
import { confirmShopOrderPayment } from "@/api/shopOrdersApi"

const mockedPost = vi.mocked(axiosInstance.post)

describe("confirmShopOrderPayment", () => {
  afterEach(() => {
    mockedPost.mockReset()
  })

  it("posts to the confirm-payment path with locationId and a 15 s timeout", async () => {
    mockedPost.mockResolvedValue({ data: { id: "ord-1", paymentStatus: "paid" } })

    const result = await confirmShopOrderPayment({
      orderId: "ord-1",
      locationId: 9,
    })

    expect(mockedPost).toHaveBeenCalledWith(
      "/shop/orders/ord-1/confirm-payment",
      {},
      { params: { locationId: 9 }, timeout: 15000 }
    )
    expect(result).toEqual({ id: "ord-1", paymentStatus: "paid" })
  })
})
