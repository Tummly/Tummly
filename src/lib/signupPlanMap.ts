export type SignupPlanCard = "essential" | "pro"
export type SignupManagePlanId = "Pilot" | "Growth"

export function mapSignupCardToPlanId(
  card: SignupPlanCard
): SignupManagePlanId {
  if (card === "essential") return "Pilot"
  return "Growth"
}
