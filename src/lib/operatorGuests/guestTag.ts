/** Restaurant Guest tag catalog entry for Add Tag / Filters pickers. */
export type GuestTag = {
  id: string
  name: string
  guestCount: number
}

export type GuestTagApiRow = {
  id: number
  name: string
  guestCount: number
  aiSourced?: boolean
}

export function mapGuestTagApiRowToGuestTag(row: GuestTagApiRow): GuestTag {
  return {
    id: String(row.id),
    name: row.name,
    guestCount: row.guestCount,
  }
}

export function mapCreatedGuestTagApiToGuestTag(row: {
  id: number
  name: string
}): GuestTag {
  return {
    id: String(row.id),
    name: row.name,
    guestCount: 0,
  }
}
