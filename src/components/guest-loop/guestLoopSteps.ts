export type GuestLoopProgressStep = {
  number: number
  label: string
}

export const GUEST_LOOP_SINGLE_STEPS: readonly GuestLoopProgressStep[] = [
  { number: 1, label: "Account" },
  { number: 2, label: "Restaurant" },
  { number: 3, label: "Ready" },
] as const
