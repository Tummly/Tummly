export const defaultFormValidationOptions = {
  mode: "onSubmit" as const,
  reValidateMode: "onChange" as const,
}

const crossFieldGroups = [
  ["password", "confirmPassword"],
  ["newPassword", "confirmPassword"],
] as const

export function getCrossFieldPeers(fieldName: string): readonly string[] {
  const peers = new Set<string>()

  for (const group of crossFieldGroups) {
    if ((group as readonly string[]).includes(fieldName)) {
      for (const name of group) {
        if (name !== fieldName) {
          peers.add(name)
        }
      }
    }
  }

  return [...peers]
}

export function addAttemptedFields<T extends string>(
  current: ReadonlySet<T>,
  fields: readonly T[]
): Set<T> {
  return new Set([...current, ...fields])
}
