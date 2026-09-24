export const PENDING_PAYMENT_INTERSTITIAL_DISMISS_KEY =
  "tummly.operator.pending-payment-interstitial.dismissed"

export function clearPendingPaymentInterstitialDismissed(
  storage: Pick<Storage, "removeItem"> = sessionStorage
): void {
  storage.removeItem(PENDING_PAYMENT_INTERSTITIAL_DISMISS_KEY)
}
