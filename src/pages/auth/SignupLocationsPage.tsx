import { useCallback, useEffect, useState } from "react"
import { MapPinned } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"

import {
  getLocationDetail,
  getLocationsList,
  updateLocationDetail,
} from "@/api/dashboardApi"
import {
  activateOwnedLocation,
  createOwnedLocation,
  importOwnedLocations,
} from "@/api/locationsWriteApi"
import { LocationDetailEditDetailsDialog } from "@/components/dashboard/operator/Locations/LocationDetailEditDetailsDialog"
import { LocationsAddLocationDialog } from "@/components/dashboard/operator/Locations/LocationsAddLocationDialog"
import { LocationsImportLocationsDialog } from "@/components/dashboard/operator/Locations/LocationsImportLocationsDialog"
import { AuthSessionLoading } from "@/components/auth/AuthSessionLoading"
import { GuestLoopShell } from "@/components/guest-loop/GuestLoopShell"
import { SignupLocationsActionCards } from "@/components/signup/locations/SignupLocationsActionCards"
import { SignupLocationsCapacity } from "@/components/signup/locations/SignupLocationsCapacity"
import {
  SignupLocationsList,
  type SignupLocationListItem,
} from "@/components/signup/locations/SignupLocationsList"
import { fetchSignupLocationCapacity } from "@/components/signup/locations/fetchSignupLocationCapacity"
import { Button } from "@/components/ui/button"
import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"
import type { LocationDetailEditFields } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import type { PlanEntitlementLimit } from "@/lib/planEntitlements/planEntitlementsPresentation"
import {
  getMultiDashboardPath,
  getSelectedLocationId,
  updateAuthAccountType,
} from "@/pages/utils/authHelpers"
import { useAuthStore } from "@/stores/authStore"

function dashboardHref(accountType: string | null | undefined): string {
  if (accountType === "Single") {
    return "/single-dashboard"
  }
  return getMultiDashboardPath(getSelectedLocationId())
}

function SignupLocationsPage() {
  const navigate = useNavigate()
  const accountType = useAuthStore((state) => state.accountType)
  const hasHydrated = useAuthStore((state) => state._hasHydrated)

  const [capacity, setCapacity] = useState<PlanEntitlementLimit | null>(null)
  const [locations, setLocations] = useState<SignupLocationListItem[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const [addOpen, setAddOpen] = useState(false)
  const [addBusy, setAddBusy] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)

  const [importOpen, setImportOpen] = useState(false)
  const [importBusy, setImportBusy] = useState(false)

  const [editTargetId, setEditTargetId] = useState<number | null>(null)
  const [editInitial, setEditInitial] = useState<LocationDetailEditFields | null>(
    null
  )
  const [editBusy, setEditBusy] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)
  const [editLoading, setEditLoading] = useState(false)

  const goToDashboard = useCallback(() => {
    navigate(dashboardHref(accountType), { replace: true })
  }, [accountType, navigate])

  const refresh = useCallback(async () => {
    const [nextCapacity, list] = await Promise.all([
      fetchSignupLocationCapacity(),
      getLocationsList({ page: 1, pageSize: 100, sort: "name-asc" }),
    ])

    setCapacity(nextCapacity)
    setLocations(
      list.rows.map((row) => ({
        id: row.id,
        name: row.name,
        city: row.city,
        postcode: row.postcode,
      }))
    )

    if (nextCapacity.available && nextCapacity.cap <= 1) {
      navigate(dashboardHref(useAuthStore.getState().accountType), {
        replace: true,
      })
    }
  }, [navigate])

  useEffect(() => {
    if (!hasHydrated) {
      return
    }

    let cancelled = false
    void (async () => {
      setLoading(true)
      setLoadError(null)
      try {
        await refresh()
      } catch (error) {
        if (!cancelled) {
          setLoadError(
            getUserFacingApiErrorMessage(
              error,
              "Could not load location capacity."
            )
          )
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [hasHydrated, refresh])

  const atCap =
    capacity != null
    && capacity.available
    && (capacity.atCap || capacity.current >= capacity.cap)

  const openEdit = async (locationId: number) => {
    setEditError(null)
    setEditLoading(true)
    setEditTargetId(locationId)
    try {
      const detail = await getLocationDetail(locationId)
      setEditInitial({
        locationName: detail.header.name,
        address: detail.header.address,
        city: detail.header.city ?? "",
        postcode: detail.header.postcode ?? "",
        locationPhone: detail.header.locationPhone ?? "",
        localContact: detail.header.localContact ?? "",
      })
    } catch (error) {
      setEditTargetId(null)
      setEditInitial(null)
      toast.error(
        getUserFacingApiErrorMessage(error, "Could not load location.")
      )
    } finally {
      setEditLoading(false)
    }
  }

  if (!hasHydrated || loading) {
    return <AuthSessionLoading />
  }

  if (capacity?.available && capacity.cap <= 1) {
    return <AuthSessionLoading />
  }

  return (
    <GuestLoopShell
      contentAlign="start"
      contentMaxWidthClassName="max-w-[960px]"
      showBackButton={false}
    >
      <div className="flex w-full flex-col gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-16">
        <div className="flex min-w-0 flex-1 flex-col gap-8">
          <div className="flex flex-col gap-3">
            <p className="m-0 font-sans text-xs font-semibold tracking-[0.08em] text-[#14a74a]">
              CUSTOMISE YOUR SUBSCRIPTION
            </p>
            <h1 className="m-0 font-jakarta text-[clamp(1.75rem,3vw,2.25rem)] font-medium leading-tight tracking-[-0.04em] text-[#141414]">
              Add your restaurant Locations
            </h1>
            <p className="m-0 max-w-[520px] font-sans text-base font-normal leading-6 text-[#5c5c5c]">
              Set up the restaurants you want to manage with Tummly. Add them
              individually or upload multiple Locations at once. You can finish
              setting up additional Locations later.
            </p>
          </div>

          {loadError ? (
            <div
              className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
              role="alert"
            >
              {loadError}
            </div>
          ) : null}

          {capacity?.available ? (
            <SignupLocationsCapacity
              current={capacity.current}
              cap={capacity.cap}
            />
          ) : null}

          <SignupLocationsActionCards
            disabled={atCap}
            onAddIndividually={() => {
              setAddError(null)
              setAddOpen(true)
            }}
            onImportCsv={() => setImportOpen(true)}
          />

          <SignupLocationsList
            locations={locations}
            onEdit={(locationId) => {
              void openEdit(locationId)
            }}
          />
        </div>

        <div
          className="mx-auto hidden w-full max-w-[280px] shrink-0 items-center justify-center text-[#14a74a] lg:flex"
          aria-hidden
        >
          <MapPinned className="size-40 stroke-[1.25]" />
        </div>
      </div>

      <div className="mt-10 flex w-full flex-col gap-3 sm:flex-row sm:items-center">
        <Button
          type="button"
          onClick={goToDashboard}
          className="h-[45px] rounded-[4px] bg-[#14a74a] px-6 text-sm font-medium text-white shadow-none hover:bg-[#14a74a]/90 sm:min-w-[220px]"
        >
          Setup your locations
        </Button>
        <Button
          type="button"
          variant="outline"
          onClick={goToDashboard}
          className="h-[45px] rounded-[4px] border-[#d0d0d0] bg-transparent px-6 text-sm font-medium text-[#141414] shadow-none hover:bg-[#f3f3f3] sm:min-w-[200px]"
        >
          Add locations later
        </Button>
      </div>

      <LocationsAddLocationDialog
        open={addOpen}
        busy={addBusy}
        error={addError}
        onOpenChange={setAddOpen}
        onSubmit={async (input) => {
          setAddBusy(true)
          setAddError(null)
          try {
            const created = await createOwnedLocation(input)
            try {
              const activated = await activateOwnedLocation(created.locationId)
              if (activated.accountType != null) {
                updateAuthAccountType(activated.accountType)
              }
            } catch {
              // Create may already leave the location Active.
            }
            await refresh()
            toast.success("Location added.")
          } catch (error) {
            const message = getUserFacingApiErrorMessage(
              error,
              "Could not create location."
            )
            setAddError(message)
            throw error
          } finally {
            setAddBusy(false)
          }
        }}
      />

      <LocationsImportLocationsDialog
        open={importOpen}
        onOpenChange={setImportOpen}
        isSubmitting={importBusy}
        onConfirm={async (rows) => {
          setImportBusy(true)
          try {
            const result = await importOwnedLocations(
              rows.map((row) => ({
                locationName: row.locationName,
                address: row.address,
                city: row.city,
                postcode: row.postcode,
                locationPhone: row.locationPhone,
                localContact: row.localContact,
              }))
            )

            for (const created of result.created) {
              try {
                const activated = await activateOwnedLocation(created.locationId)
                if (activated.accountType != null) {
                  updateAuthAccountType(activated.accountType)
                }
              } catch {
                // Keep imported row even if activate fails; list refresh still runs.
              }
            }

            await refresh()

            if (result.created.length > 0 && result.errors.length === 0) {
              toast.success(
                result.created.length === 1
                  ? "1 location imported."
                  : `${result.created.length} locations imported.`
              )
            } else if (result.created.length > 0) {
              toast.success(
                `Imported ${result.created.length}; ${result.errors.length} row(s) need attention.`
              )
            }

            if (result.errors.length > 0) {
              toast.error(
                result.errors
                  .slice(0, 4)
                  .map((row) => {
                    const overflow =
                      row.code === "location_cap_reached"
                        ? " (over capacity)"
                        : ""
                    return `Row ${row.rowIndex + 1}: ${row.message}${overflow}`
                  })
                  .join(" ")
              )
            }

            if (result.created.length === 0 && result.errors.length === 0) {
              toast.error("No locations were imported.")
            }

            setImportOpen(false)
          } catch (error) {
            toast.error(
              getUserFacingApiErrorMessage(
                error,
                "Could not import locations."
              )
            )
            throw error
          } finally {
            setImportBusy(false)
          }
        }}
      />

      <LocationDetailEditDetailsDialog
        open={editTargetId != null && editInitial != null && !editLoading}
        initialValues={
          editInitial ?? {
            locationName: "",
            address: "",
            city: "",
            postcode: "",
            locationPhone: "",
            localContact: "",
          }
        }
        busy={editBusy}
        error={editError}
        onOpenChange={(open) => {
          if (!open && !editBusy) {
            setEditTargetId(null)
            setEditInitial(null)
            setEditError(null)
          }
        }}
        onSubmit={async (input) => {
          if (editTargetId == null) {
            return
          }
          setEditBusy(true)
          setEditError(null)
          try {
            await updateLocationDetail(editTargetId, input)
            await refresh()
            toast.success("Location updated.")
            setEditTargetId(null)
            setEditInitial(null)
          } catch (error) {
            const message = getUserFacingApiErrorMessage(
              error,
              "Could not update location."
            )
            setEditError(message)
            throw error
          } finally {
            setEditBusy(false)
          }
        }}
      />
    </GuestLoopShell>
  )
}

export default SignupLocationsPage
