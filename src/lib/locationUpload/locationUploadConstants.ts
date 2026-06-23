export const LOCATION_UPLOAD_TEMPLATE_HEADERS = [
  "Location name",
  "Address",
  "Postcode",
  "Location phone",
  "Local contact",
] as const

export const LOCATION_UPLOAD_REQUIRED_HEADERS = [
  "Location name",
  "Address",
  "Postcode",
] as const

export const LOCATION_UPLOAD_TEMPLATE_FILENAME = "tummly-locations-template.csv"

export const LOCATION_UPLOAD_MAX_ROWS = 100

export const LOCATION_UPLOAD_ACCEPT =
  ".csv,.xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,text/csv"
