export const PermissionRoles = [
  "Owner",
  "Admin",
  "Area Manager",
  "Location Manager",
  "Marketing",
  "Staff",
  "Billing Admin",
  "Reporting Only",
] as const

export type PermissionRole = (typeof PermissionRoles)[number]

export const ASSIGNABLE_PERMISSION_ROLES = PermissionRoles.filter(
  (role) => role !== "Owner"
)

export function assignableRolesForActor(actorPermissionRole: string): string[] {
  if (actorPermissionRole === "Admin") {
    return ASSIGNABLE_PERMISSION_ROLES.filter((role) => role !== "Admin")
  }
  return [...ASSIGNABLE_PERMISSION_ROLES]
}
