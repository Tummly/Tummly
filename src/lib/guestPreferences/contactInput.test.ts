import { describe, expect, it } from "vitest"

import { classifyContact, maskEmailAddress } from "./contactInput"

describe("classifyContact", () => {
  it.each([
    ["a@b.co", "email"],
    ["  Sophie@Example.com ", "email"],
    ["07700 900123", "uk-mobile"],
    ["+447700900123", "uk-mobile"],
    ["+44 7700 900123", "uk-mobile"],
    ["+12025550123", null],
    ["", null],
    ["   ", null],
    ["not an email@", null],
    ["0770090012", null],
    ["07911 123456", "uk-mobile"],
    ["+447911123456", "uk-mobile"],
    ["+44 7911 123 456", "uk-mobile"],
    ["020 7946 0000", null],
    ["+442079460000", null],
    ["01632 960123", null],
  ])("classifies %j as %j", (raw, expected) => {
    expect(classifyContact(raw)).toBe(expected)
  })
})

describe("maskEmailAddress", () => {
  it("keeps two local-part characters, then five dots and the domain", () => {
    expect(maskEmailAddress("sophie@example.com")).toBe("so•••••@example.com")
  })

  it("keeps one character when the local part is two characters", () => {
    expect(maskEmailAddress("ab@example.com")).toBe("a•••••@example.com")
  })

  it("keeps no characters for a one-character local part", () => {
    expect(maskEmailAddress("a@b.co")).toBe("•••••@b.co")
  })

  it("lowercases and trims first, as the backend does", () => {
    expect(maskEmailAddress("  Sophie@Example.COM ")).toBe(
      "so•••••@example.com"
    )
  })
})
