import type { FilterSheetSchema, FieldSchema } from "@/lib/operatorFilterSheet"
import { PermissionRoles } from "@/lib/operatorTeamPermissions/permissionRoles"

export function teamPermissionsFilterSheetSchema(options: {
  isSingleLocation: boolean
  locations: ReadonlyArray<{ id: number; name: string }>
}): FilterSheetSchema {
  const fields: FieldSchema[] = [
    {
      id: "status",
      kind: "multi-select",
      label: "Status",
      chipKind: "status",
      options: [
        { id: "active", label: "Active" },
        { id: "deactivated", label: "Deactivated" },
      ],
    },
    {
      id: "role",
      kind: "multi-select",
      label: "Role",
      chipKind: "role",
      options: PermissionRoles.map((role) => ({ id: role, label: role })),
    },
  ]

  if (!options.isSingleLocation) {
    fields.push({
      id: "location",
      kind: "multi-select",
      label: "Location",
      chipKind: "location",
      options: options.locations.map((location) => ({
        id: String(location.id),
        label: location.name,
      })),
    })
  }

  return { fields }
}
