/**
 * Free accounts may browse empty shells; product writes must re-open the
 * activate dialog instead of starting setup work.
 */
export function shouldGateFreeProductWrite(subscriptionPlan: string): boolean {
  return subscriptionPlan === "Free"
}
