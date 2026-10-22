import axiosInstance from "@/api/axiosInstance"

export type BulkRecoveryBatchMember = {
  id: number
  feedbackId: number
  guestName: string
  maskedDestination: string
  feedbackComment: string | null
  status: string
  feedbackGuestResponseId: number | null
  failureReason: string | null
}

export type BulkRecoveryBatch = {
  id: number
  restaurantId: number
  locationId: number
  channel: "email" | "sms" | string
  intent: string
  subject: string | null
  bodyTemplate: string
  purpose: string | null
  tone: string | null
  includeNotes: string | null
  status: string
  memberCount: number
  createdAt: string
  completedAt: string | null
  members: BulkRecoveryBatchMember[]
}

export type SendBulkRecoveryBatchResult = {
  batchId: number
  status: string
  sent: number
  skipped: number
  failed: number
  total: number
}

type BatchEnvelope = {
  success: boolean
  batch: BulkRecoveryBatch
}

type SendEnvelope = {
  success: boolean
  result: SendBulkRecoveryBatchResult
}

export async function getBulkRecoveryBatch(
  batchId: number
): Promise<BulkRecoveryBatch> {
  const response = await axiosInstance.get<BatchEnvelope>(
    `/feedback/bulk-recovery-batches/${batchId}`
  )
  return response.data.batch
}

export async function updateBulkRecoveryBatchDraft(
  batchId: number,
  body: {
    subject?: string | null
    bodyTemplate?: string | null
    purpose?: string | null
    tone?: string | null
    includeNotes?: string | null
  }
): Promise<BulkRecoveryBatch> {
  const response = await axiosInstance.patch<BatchEnvelope>(
    `/feedback/bulk-recovery-batches/${batchId}`,
    body
  )
  return response.data.batch
}

export async function sendBulkRecoveryBatch(
  batchId: number
): Promise<SendBulkRecoveryBatchResult> {
  const response = await axiosInstance.post<SendEnvelope>(
    `/feedback/bulk-recovery-batches/${batchId}/send`
  )
  return response.data.result
}
