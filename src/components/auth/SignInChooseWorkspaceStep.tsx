import type { FormEvent } from "react"

import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type { WorkspaceLocation } from "@/lib/workspaceSetupFlow"
import { cn } from "@/lib/utils"

const cardShadow =
  "shadow-[2px_6px_14px_rgba(0,0,0,0.04),9px_25px_26px_rgba(0,0,0,0.03),20px_55px_35px_rgba(0,0,0,0.02)]"

type SignInChooseWorkspaceStepProps = {
  workspaces: WorkspaceLocation[]
  selectedLocationId: number | null
  loading: boolean
  submitting: boolean
  error: string | null
  onSelect: (locationId: number) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}

function getWorkspaceOptionLabel(workspace: WorkspaceLocation) {
  const locationName = workspace.locationName.trim()
  if (locationName) {
    return locationName
  }

  const restaurantName = workspace.restaurantName.trim()
  if (restaurantName) {
    return restaurantName
  }

  return `Location ${workspace.locationId}`
}

export function SignInChooseWorkspaceStep({
  workspaces,
  selectedLocationId,
  loading,
  submitting,
  error,
  onSelect,
  onSubmit,
}: SignInChooseWorkspaceStepProps) {
  const controlsDisabled = loading || submitting
  const canSubmit =
    !loading && !submitting && selectedLocationId != null && workspaces.length > 0

  return (
    <form
      onSubmit={onSubmit}
      noValidate
      className={`flex w-full max-w-[490px] shrink-0 flex-col gap-6 rounded-[6px] border border-[#d2d2d2] bg-white px-[clamp(1.25rem,4vw,1.875rem)] py-[clamp(1.25rem,4vw,2.375rem)] sm:gap-7 lg:gap-16 ${cardShadow}`}
    >
      <header className="flex flex-col gap-4 text-[#232323]">
        <h1 className="m-0 text-[clamp(1.625rem,4vw,2rem)] font-bold leading-normal tracking-[-0.64px]">
          Choose workspace
        </h1>
        <p className="m-0 text-sm leading-normal">
          Select the Tummly workspace you want to open.
        </p>
      </header>

      <Select
        disabled={controlsDisabled || workspaces.length === 0}
        value={
          selectedLocationId != null ? String(selectedLocationId) : undefined
        }
        onValueChange={(value) => onSelect(Number(value))}
      >
        <SelectTrigger
          aria-label="Workspace"
          className={cn(
            "h-[50px] min-h-[50px] w-full rounded-[4px] border border-[rgba(74,74,76,0.4)] bg-white px-[13px] py-[15px] text-sm leading-5 shadow-none",
            "hover:bg-white dark:bg-white dark:hover:bg-white",
            "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
            "data-[size=default]:!h-[50px]",
            "[&_[data-slot=select-value]]:text-[#141414]",
            "[&_[data-slot=select-value][data-placeholder]]:text-[#7d7d7d]",
            "[&_svg]:size-4 [&_svg]:text-[#7d7d7d]"
          )}
        >
          <SelectValue placeholder="Workspace" />
        </SelectTrigger>

        <SelectContent
          position="popper"
          side="bottom"
          align="start"
          className="w-(--radix-select-trigger-width) min-w-(--radix-select-trigger-width) max-w-(--radix-select-trigger-width)"
        >
          {workspaces.map((workspace) => (
            <SelectItem
              key={workspace.locationId}
              value={String(workspace.locationId)}
            >
              {getWorkspaceOptionLabel(workspace)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {error ? (
        <p className="m-0 text-sm font-medium leading-5 text-destructive" role="alert">
          {error}
        </p>
      ) : null}

      {!loading && workspaces.length === 0 && !error ? (
        <p className="m-0 text-sm leading-normal text-[#232323]">
          No workspaces are available yet. Contact support if this looks wrong.
        </p>
      ) : null}

      <Button
        type="submit"
        disabled={!canSubmit}
        className="h-auto min-h-0 w-full rounded-[4px] bg-[#14a74a] px-[17px] py-[15px] text-base font-medium leading-5 text-white hover:bg-[#129641] disabled:opacity-50"
      >
        {submitting ? "Please wait..." : "Select"}
      </Button>
    </form>
  )
}
