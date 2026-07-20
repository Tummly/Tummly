import { useMemo, useState } from "react"

import { OperatorDashboardShell } from "@/components/dashboard/operator/OperatorDashboardShell"
import { OperatorHomeHero } from "@/components/dashboard/operator/OperatorHomeHero"
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
      <OperatorDashboardShell
        presentation={presentation}
        onSelectLocation={setSelectedLocationId}
        onSignOut={() => undefined}
      >
        <OperatorHomeHero
          activationPeriodBadge={presentation.activationPeriodBadge}
          canPreviewGuestForm
          canCopySmartGuestLink
          onPreviewGuestForm={() => undefined}
          onCopySmartGuestLink={() => undefined}
        />
      </OperatorDashboardShell>
    </div>
  )
}
