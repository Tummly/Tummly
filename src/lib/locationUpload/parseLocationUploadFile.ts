import * as XLSX from "xlsx"

import {
  LOCATION_UPLOAD_MAX_ROWS,
  LOCATION_UPLOAD_REQUIRED_HEADERS,
  LOCATION_UPLOAD_TEMPLATE_FILENAME,
  LOCATION_UPLOAD_TEMPLATE_HEADERS,
} from "@/lib/locationUpload/locationUploadConstants"
import {
  combineLocalContact,
  type UploadedLocationDraft,
} from "@/lib/locationUpload/locationUploadValidation"

type ParsedSheetRow = Record<string, unknown>

function normalizeHeader(value: string) {
  return value.trim().toLowerCase().replace(/\s+/g, " ")
}

const HEADER_ALIASES: Record<string, keyof UploadedLocationDraft | "localContactName" | "localContactEmail"> = {
  "location name": "locationName",
  address: "address",
  postcode: "postcode",
  "location phone": "locationPhone",
  "local contact name": "localContactName",
  "local contact email": "localContactEmail",
}

function getFileKind(file: File) {
  const extension = file.name.split(".").pop()?.toLowerCase()

  if (extension === "csv") {
    return "csv" as const
  }

  if (extension === "xlsx") {
    return "xlsx" as const
  }

  return null
}

function mapSheetRow(
  row: ParsedSheetRow,
  columnMap: Map<string, keyof UploadedLocationDraft | "localContactName" | "localContactEmail">
): UploadedLocationDraft | null {
  const values = {
    locationName: "",
    address: "",
    postcode: "",
    locationPhone: "",
    localContactName: "",
    localContactEmail: "",
  }

  for (const [header, field] of columnMap.entries()) {
    const rawValue = row[header]
    const value = rawValue == null ? "" : String(rawValue).trim()

    if (field === "localContactName" || field === "localContactEmail") {
      values[field] = value
      continue
    }

    if (
      field === "locationName" ||
      field === "address" ||
      field === "postcode" ||
      field === "locationPhone"
    ) {
      values[field] = value
    }
  }

  const hasContent = Object.values(values).some((value) => value.length > 0)
  if (!hasContent) {
    return null
  }

  return {
    locationName: values.locationName,
    address: values.address,
    postcode: values.postcode,
    addressOverridden: false,
    locationPhone: values.locationPhone,
    localContact: combineLocalContact(
      values.localContactName,
      values.localContactEmail
    ),
  }
}

function buildColumnMap(headers: string[]) {
  const columnMap = new Map<
    string,
    keyof UploadedLocationDraft | "localContactName" | "localContactEmail"
  >()

  for (const header of headers) {
    const normalized = normalizeHeader(header)
    const field = HEADER_ALIASES[normalized]

    if (field) {
      columnMap.set(header, field)
    }
  }

  return columnMap
}

function getMissingRequiredHeaders(columnMap: Map<string, unknown>) {
  const presentFields = new Set(columnMap.values())

  return LOCATION_UPLOAD_REQUIRED_HEADERS.filter((header) => {
    const normalized = normalizeHeader(header)
    const field = HEADER_ALIASES[normalized]
    return field ? !presentFields.has(field) : true
  })
}

export async function parseLocationUploadFile(
  file: File
): Promise<{ locations: UploadedLocationDraft[] } | { error: string }> {
  const fileKind = getFileKind(file)

  if (!fileKind) {
    return { error: "Upload a CSV or XLSX file only." }
  }

  try {
    const buffer = await file.arrayBuffer()
    const workbook = XLSX.read(buffer, {
      type: "array",
      raw: false,
    })

    const sheetName = workbook.SheetNames[0]
    if (!sheetName) {
      return { error: "The uploaded file is empty." }
    }

    const sheet = workbook.Sheets[sheetName]
    const rows = XLSX.utils.sheet_to_json<ParsedSheetRow>(sheet, {
      defval: "",
    })

    if (rows.length === 0) {
      return { error: "Add at least one location row to the template." }
    }

    if (rows.length > LOCATION_UPLOAD_MAX_ROWS) {
      return {
        error: `Upload up to ${LOCATION_UPLOAD_MAX_ROWS} locations at a time. You can add more from your workspace later.`,
      }
    }

    const headers = Object.keys(rows[0] ?? {})
    const columnMap = buildColumnMap(headers)
    const missingHeaders = getMissingRequiredHeaders(columnMap)

    if (missingHeaders.length > 0) {
      return {
        error: `Missing required columns: ${missingHeaders.join(", ")}.`,
      }
    }

    const locations = rows
      .map((row) => mapSheetRow(row, columnMap))
      .filter((location): location is UploadedLocationDraft => location !== null)

    if (locations.length === 0) {
      return { error: "Add at least one location row to the template." }
    }

    return { locations }
  } catch {
    return {
      error: "We couldn't read that file. Try a different CSV or XLSX file.",
    }
  }
}

export function downloadLocationUploadTemplate() {
  const worksheet = XLSX.utils.aoa_to_sheet([
    [...LOCATION_UPLOAD_TEMPLATE_HEADERS],
    [
      "The Willow & Oak Bistro",
      "125 High Street",
      "M1 4AB",
      "+44 161 555 1234",
      "Jane Smith",
      "jane@bistro.com",
    ],
  ])

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, worksheet, "Locations")
  XLSX.writeFile(workbook, LOCATION_UPLOAD_TEMPLATE_FILENAME)
}
