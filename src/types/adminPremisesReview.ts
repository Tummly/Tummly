/** Admin-only view of a Location saved as Draft for a possible duplicate premises. */
export type AdminPremisesReviewMatch = {
  locationId: number
  restaurantId: number
  workspaceName: string
  locationName: string | null
  address: string | null
  city: string | null
  postcode: string | null
  lifecycleStatus: string | null
  sameWorkspace: boolean
  premisesKeyMatch: boolean
  fsaMatch: boolean
  priorPilot: boolean
  priorStarterKit: boolean
}

export type AdminPremisesReviewItem = {
  locationId: number
  restaurantId: number
  workspaceName: string
  locationName: string
  address: string
  city: string | null
  postcode: string | null
  fsaEstablishmentId: string | null
  createdAtUtc: string
  matches: AdminPremisesReviewMatch[]
}

export type AdminPremisesReviewAction = "clear" | "reject"
