export type GuestLoopProgressStep = {
  number: number
  label: string
}

export const GUEST_LOOP_SINGLE_STEPS: readonly GuestLoopProgressStep[] = [
  { number: 1, label: "Account" },
  { number: 2, label: "Restaurant" },
  { number: 3, label: "Ready" },
] as const

export const GUEST_LOOP_MULTI_STEPS: readonly GuestLoopProgressStep[] = [
  { number: 1, label: "Account" },
  { number: 2, label: "Group" },
  { number: 3, label: "Locations" },
  { number: 4, label: "Ready" },
] as const
