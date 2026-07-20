import type { BuildOperatorShellPresentationInput } from "@/lib/operatorHome/buildShellPresentation"

/** Fixture workspace session for the responsive shell + hero prototype. */
export const OPERATOR_HOME_RESPONSIVE_PROTOTYPE_FIXTURE: BuildOperatorShellPresentationInput =
  {
    operatorDisplayName: "Alex Morgan",
    activationExpiresAt: "2026-08-13T23:59:59.000Z",
    selfRole: "Owner",
    locations: [
      { id: 1, name: "The Ivy Soho Brasserie", address: "Soho, London" },
      { id: 2, name: "Camden High Street", address: "Camden, London" },
    ],
    selectedLocationId: 1,
    locationSwitcherInteractive: true,
    activeNavId: "home",
  }

export const OPERATOR_HOME_RESPONSIVE_VIEWPORTS = [
  { id: "320", label: "320px", width: 320 },
  { id: "768", label: "768px", width: 768 },
  { id: "1024", label: "1024px", width: 1024 },
  { id: "full", label: "Full width", width: null },
] as const

export type OperatorHomeResponsiveViewportId =
  (typeof OPERATOR_HOME_RESPONSIVE_VIEWPORTS)[number]["id"]

export function parseOperatorHomeResponsiveViewport(
  value: string | null
): OperatorHomeResponsiveViewportId {
  if (value === "320" || value === "768" || value === "1024" || value === "full") {
    return value
  }
  return "320"
}

export function getOperatorHomeResponsiveViewportWidth(
  viewportId: OperatorHomeResponsiveViewportId
): number | null {
  return (
    OPERATOR_HOME_RESPONSIVE_VIEWPORTS.find((item) => item.id === viewportId)
      ?.width ?? null
  )
}
