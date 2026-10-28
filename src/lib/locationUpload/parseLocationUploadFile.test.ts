import * as XLSX from "xlsx"
import { describe, expect, it } from "vitest"

import { parseLocationUploadFile } from "@/lib/locationUpload/parseLocationUploadFile"

function createUploadFile(
  rows: string[][],
  filename: string,
  bookType: XLSX.BookType
) {
  const worksheet = XLSX.utils.aoa_to_sheet(rows)
  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, "Locations")
  const buffer = XLSX.write(workbook, { bookType, type: "array" })

  return new File([buffer], filename, {
    type:
      bookType === "csv"
        ? "text/csv"
        : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  })
}

describe("parseLocationUploadFile", () => {
  it("parses valid xlsx uploads", async () => {
    const file = createUploadFile(
      [
        [
          "Location name",
          "Address",
          "Postcode",
          "Location phone",
          "Local contact name",
          "Local contact email",
        ],
        [
          "The Willow & Oak Bistro",
          "125 High Street",
          "M1 4AB",
          "+44 161 555 1234",
          "Jane Smith",
          "jane@bistro.com",
        ],
      ],
      "locations.xlsx",
      "xlsx"
    )

    const result = await parseLocationUploadFile(file)

    expect(result).toEqual({
      locations: [
        {
          locationName: "The Willow & Oak Bistro",
          address: "125 High Street",
          city: "",
          postcode: "M1 4AB",
          addressOverridden: false,
          locationPhone: "+44 161 555 1234",
          localContact: "Jane Smith — jane@bistro.com",
        },
      ],
    })
  })

  it("rejects files without required columns", async () => {
    const file = createUploadFile(
      [["Location name", "Address"], ["Bistro", "125 High Street"]],
      "locations.xlsx",
      "xlsx"
    )

    const result = await parseLocationUploadFile(file)

    expect(result).toEqual({
      error: "Missing required columns: Postcode.",
    })
  })

  it("rejects unsupported file types", async () => {
    const file = new File(["{}"], "locations.json", {
      type: "application/json",
    })

    const result = await parseLocationUploadFile(file)

    expect(result).toEqual({
      error: "Upload a CSV or XLSX file only.",
    })
  })

  it("reads an optional FSA establishment id column", async () => {
    const file = createUploadFile(
      [
        ["Location name", "Address", "Postcode", "FSA establishment id"],
        ["Cafe One", "1 High Street", "M1 4AB", " 123456 "],
        ["Cafe Two", "2 High Street", "M1 4AB", ""],
      ],
      "locations.csv",
      "csv"
    )

    const result = await parseLocationUploadFile(file)

    expect(result).toMatchObject({
      locations: [
        { locationName: "Cafe One", fsaEstablishmentId: "123456" },
        { locationName: "Cafe Two" },
      ],
    })
    if ("locations" in result) {
      expect(result.locations[1]).not.toHaveProperty("fsaEstablishmentId")
    }
  })

  it("does not require the FSA column", async () => {
    const file = createUploadFile(
      [
        ["Location name", "Address", "Postcode"],
        ["Cafe One", "1 High Street", "M1 4AB"],
      ],
      "locations.csv",
      "csv"
    )

    const result = await parseLocationUploadFile(file)

    expect("locations" in result).toBe(true)
  })
})
