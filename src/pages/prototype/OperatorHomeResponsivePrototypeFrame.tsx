import { useMemo, useState } from "react"

import { DashboardShell } from "@/components/dashboard/operator/DashboardShell"
import { HomeHero } from "@/components/dashboard/operator/Home/HomeHero"
import { buildOperatorShellPresentation } from "@/lib/operatorHome/buildShellPresentation"

import { OPERATOR_HOME_RESPONSIVE_PROTOTYPE_FIXTURE } from "./operatorHomeResponsiveFixtures"

/**
 * PROTOTYPE — inner frame document for responsive preview.
 * Loaded inside an iframe so Tailwind breakpoints match the chosen width.
 */
export default function OperatorHomeResponsivePrototypeFrame() {
  const [selectedLocationId, setSelectedLocationId] = useState(
    OPERATOR_HOME_RESPONSIVE_PROTOTYPE_FIXTURE.selectedLocationId
  )

  const presentation = useMemo(
    () =>
      buildOperatorShellPresentation({
        ...OPERATOR_HOME_RESPONSIVE_PROTOTYPE_FIXTURE,
        selectedLocationId,
      }),
    [selectedLocationId]
  )

  return (
    <div
      data-prototype-shell-root
      className="h-dvh min-h-0 overflow-hidden bg-[var(--operator-shell-chrome)]"
    >
      <DashboardShell
        presentation={presentation}
        onSelectLocation={setSelectedLocationId}
        onSignOut={() => undefined}
      >
        <HomeHero
          activationPeriodBadge={presentation.activationPeriodBadge}
          canPreviewGuestForm
          canCopySmartGuestLink
          onPreviewGuestForm={() => undefined}
          onCopySmartGuestLink={() => undefined}
        />
      </DashboardShell>
    </div>
  )
}
