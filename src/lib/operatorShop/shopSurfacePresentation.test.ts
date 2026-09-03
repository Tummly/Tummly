import { describe, expect, it } from "vitest"

import {
  SHOP_CHECKOUT_INPUT_CLASS,
  SHOP_CHECKOUT_OPTION_DEFAULT_CLASS,
  SHOP_CHECKOUT_OPTION_SELECTED_CLASS,
  SHOP_CHECKOUT_REVIEW_CARD_CLASS,
  SHOP_CHECKOUT_SECTION_CARD_CLASS,
  SHOP_CHECKOUT_TEXTAREA_CLASS,
  SHOP_PAGE_BACKGROUND_CLASS,
  SHOP_PRODUCT_CARD_PRIMARY_CLASS,
  SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS,
  SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS,
  SHOP_PRODUCT_CARD_RECOMMENDATION_IMAGE_CLASS,
  SHOP_PRODUCT_CARD_SECONDARY_CLASS,
  SHOP_PRODUCT_CARD_SECONDARY_IMAGE_CLASS,
  SHOP_PRODUCT_DETAIL_CARD_CLASS,
  SHOP_RECOMMENDATION_INNER_CLASS,
  SHOP_RECOMMENDATION_PARENT_CLASS,
  SHOP_RECOMMENDATION_TAG_CLASS,
} from "./shopSurfacePresentation"

describe("shopSurfacePresentation", () => {
  it("uses sidenav / shell chrome for the Shop page background", () => {
    expect(SHOP_PAGE_BACKGROUND_CLASS).toBe("bg-op-shell-chrome")
  })

  it("uses shell chrome #141414 and border on the recommendation parent", () => {
    expect(SHOP_RECOMMENDATION_PARENT_CLASS).toContain("bg-op-shell-chrome")
    expect(SHOP_RECOMMENDATION_PARENT_CLASS).toContain("border-op-border-default")
    expect(SHOP_RECOMMENDATION_PARENT_CLASS).toContain("rounded-op-lg")
  })

  it("uses #202020 for the nested recommendation panel", () => {
    expect(SHOP_RECOMMENDATION_INNER_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-990)]"
    )
  })

  it("keeps recommendation tags content-width", () => {
    expect(SHOP_RECOMMENDATION_TAG_CLASS).toContain("w-fit")
  })

  it("uses recommendation product card 562×241 with #171717 fill", () => {
    expect(SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS).toContain("h-[241px]")
    expect(SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS).toContain("w-[562px]")
    expect(SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS).toContain("rounded-op-lg")
    expect(SHOP_PRODUCT_CARD_RECOMMENDATION_IMAGE_CLASS).toContain("w-[228px]")
    expect(SHOP_PRODUCT_CARD_RECOMMENDATION_IMAGE_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
  })

  it("uses primary body #171717 and image well #212121", () => {
    expect(SHOP_PRODUCT_CARD_PRIMARY_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(SHOP_PRODUCT_CARD_PRIMARY_CLASS).toContain("border-0")
    expect(SHOP_PRODUCT_CARD_PRIMARY_CLASS).toContain("rounded-op-lg")
    expect(SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-985)]"
    )
  })

  it("uses secondary body and image #202020 for related products", () => {
    expect(SHOP_PRODUCT_CARD_SECONDARY_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-990)]"
    )
    expect(SHOP_PRODUCT_CARD_SECONDARY_IMAGE_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-990)]"
    )
  })

  it("uses #202020 and 6px radius on product detail config cards", () => {
    expect(SHOP_PRODUCT_DETAIL_CARD_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-990)]"
    )
    expect(SHOP_PRODUCT_DETAIL_CARD_CLASS).toContain("rounded-op-lg")
  })

  it("uses checkout review #171717 without border and section card with border", () => {
    expect(SHOP_CHECKOUT_REVIEW_CARD_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(SHOP_CHECKOUT_REVIEW_CARD_CLASS).toContain("border-0")
    expect(SHOP_CHECKOUT_REVIEW_CARD_CLASS).toContain("rounded-op-lg")
    expect(SHOP_CHECKOUT_SECTION_CARD_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(SHOP_CHECKOUT_SECTION_CARD_CLASS).toContain("border-op-border-default")
  })

  it("uses checkout option #202020 default and #7c7c7c selected border", () => {
    expect(SHOP_CHECKOUT_OPTION_DEFAULT_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-990)]"
    )
    expect(SHOP_CHECKOUT_OPTION_SELECTED_CLASS).toContain(
      "border-[var(--op-color-gray-550)]"
    )
    expect(SHOP_CHECKOUT_OPTION_SELECTED_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
  })

  it("uses Operator input border tokens on checkout fields", () => {
    expect(SHOP_CHECKOUT_INPUT_CLASS).toContain("border-op-input-border")
    expect(SHOP_CHECKOUT_INPUT_CLASS).toContain("h-[50px]")
    expect(SHOP_CHECKOUT_TEXTAREA_CLASS).toContain("border-op-input-border")
  })
})
