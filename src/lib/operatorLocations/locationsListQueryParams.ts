import {
  getMultiSelectIds,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import {
  LOCATIONS_DEFAULT_SORT_ID,
  LOCATIONS_PAGE_SIZE,
  type LocationLifecycleStatus,
  type LocationSetupStatus,
  type LocationsSortId,
} from "@/lib/operatorLocations/locationsPresentation"

export type LocationsListQueryParams = {
  q?: string
  lifecycle?: LocationLifecycleStatus[]
  setup?: LocationSetupStatus[]
  city?: string[]
  sort?: LocationsSortId
  page?: number
  pageSize?: number
}

export type LocationsListApiRow = {
  id: number
  name: string
  lifecycleStatus: LocationLifecycleStatus
  setupStatus: LocationSetupStatus
  managerName: string | null
  managerUserId?: number | null
  city: string | null
  postcode: string | null
  cityId: string
  cityPostcode: string
  lastActivityAt: string | null
  searchText?: string
}

export type LocationsListKpis = {
  active: number
  draft: number
  paused: number
  setupNeedsAttention: number
}

export type LocationsListCityFacet = {
  id: string
  label: string
}

export type LocationsListResponse = {
  success: boolean
  rows: LocationsListApiRow[]
  totalCount: number
  page: number
  pageSize: number
  kpis: LocationsListKpis
  cityFacets: LocationsListCityFacet[]
}

export function buildLocationsListQueryParams(input: {
  searchQuery: string
  sortId: LocationsSortId
  page: number
  pageSize?: number
  applied?: OperatorFilterSelection | null
}): LocationsListQueryParams {
  const selection = input.applied ?? {}
  const lifecycle = getMultiSelectIds(selection, "lifecycle")
  const setup = getMultiSelectIds(selection, "setup")
  const city = getMultiSelectIds(selection, "city")
  const q = input.searchQuery.trim()

  return {
    ...(q.length > 0 ? { q } : {}),
    ...(lifecycle.length > 0
      ? { lifecycle: lifecycle as LocationLifecycleStatus[] }
      : {}),
    ...(setup.length > 0 ? { setup: setup as LocationSetupStatus[] } : {}),
    ...(city.length > 0 ? { city } : {}),
    sort: input.sortId ?? LOCATIONS_DEFAULT_SORT_ID,
    page: input.page,
    pageSize: input.pageSize ?? LOCATIONS_PAGE_SIZE,
  }
}
