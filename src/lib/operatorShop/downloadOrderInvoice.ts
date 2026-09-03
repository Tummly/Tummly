import {
  downloadInvoicePdfBlob,
  fetchBillingCreditsInvoicePdf,
} from "@/api/billingCreditsApi"

/**
 * Downloads the minted TM VAT invoice PDF for a Shop order.
 * Returns false when the order has no invoice document number yet.
 */
export async function downloadShopOrderInvoicePdf(
  invoiceDocumentNumber: string | null | undefined
): Promise<boolean> {
  const trimmed = invoiceDocumentNumber?.trim() ?? ""
  if (trimmed.length === 0) {
    return false
  }

  const blob = await fetchBillingCreditsInvoicePdf(trimmed)
  downloadInvoicePdfBlob(blob, trimmed)
  return true
}
