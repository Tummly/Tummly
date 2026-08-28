import axiosInstance from "@/api/axiosInstance"
import type { BillingCreditsPageData } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"

type PaymentMethodResponse = BillingCreditsPageData["paymentMethod"] extends infer T
  ? T extends { kind: string }
    ? {
        kind: string
        brand?: string
        last4?: string
        expiryLabel?: string
        walletName?: string
      }
    : never
  : never

type BillingCreditsPageResponse = {
  actorPermissionRole: string
  actorCanManage: boolean
  planSubscription: BillingCreditsPageData["planSubscription"]
  paymentMethod: PaymentMethodResponse | null
  invoices: BillingCreditsPageData["invoices"]
}

function mapPaymentMethod(
  raw: PaymentMethodResponse | null | undefined
): BillingCreditsPageData["paymentMethod"] {
  if (raw == null) {
    return null
  }

  if (raw.kind === "wallet") {
    return {
      kind: "wallet",
      walletName: raw.walletName,
    }
  }

  return {
    kind: "card",
    brand: raw.brand,
    last4: raw.last4,
    expiryLabel: raw.expiryLabel,
  }
}

export async function getBillingCreditsPage(): Promise<BillingCreditsPageData> {
  const { data } = await axiosInstance.get<BillingCreditsPageResponse>(
    "/billing-credits"
  )
  return {
    actorPermissionRole: data.actorPermissionRole,
    actorCanManage: data.actorCanManage,
    planSubscription: data.planSubscription,
    paymentMethod: mapPaymentMethod(data.paymentMethod),
    invoices: data.invoices ?? [],
  }
}

export async function createPaymentMethodUpdateSession(): Promise<{
  redirectUrl: string
}> {
  const { data } = await axiosInstance.post<{ redirectUrl: string }>(
    "/billing-credits/payment-method/update"
  )
  return { redirectUrl: data.redirectUrl }
}

export async function fetchBillingCreditsInvoicePdf(
  invoiceNo: string
): Promise<Blob> {
  const encoded = encodeURIComponent(invoiceNo)
  const { data } = await axiosInstance.get<Blob>(
    `/billing-credits/invoices/${encoded}/pdf`,
    { responseType: "blob" }
  )
  return data
}

export function openInvoicePdfBlob(blob: Blob): void {
  const url = URL.createObjectURL(blob)
  window.open(url, "_blank", "noopener,noreferrer")
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export function downloadInvoicePdfBlob(blob: Blob, invoiceNo: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement("a")
  anchor.href = url
  anchor.download = `${invoiceNo}.pdf`
  anchor.rel = "noopener noreferrer"
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}
