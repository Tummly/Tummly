import axiosInstance from "./axiosInstance"
import { unwrapDataObject } from "@/lib/apiEnvelope"
import {
  parseHelpCentreAuthorKind,
  parseHelpCentreQueryStatus,
} from "@/lib/helpCentreApiNormalize"
import type { HelpCentreQueryStatus } from "@/types/helpCentre"
import type { SupportQueryDetail, SupportQueryListItem } from "@/types/support"

function normalizeListItem(raw: Record<string, unknown>): SupportQueryListItem {
  return {
    id: Number(raw.id),
    topic: String(raw.topic ?? ""),
    topicLabel: String(raw.topicLabel ?? ""),
    status: parseHelpCentreQueryStatus(raw.status),
    statusLabel: String(raw.statusLabel ?? ""),
    updatedAt: String(raw.updatedAt ?? ""),
    preview: (raw.preview as string | null | undefined) ?? null,
    submitterName: String(raw.submitterName ?? ""),
    submitterEmail: String(raw.submitterEmail ?? ""),
    businessName: String(raw.businessName ?? ""),
    queryLocationLabel:
      (raw.queryLocationLabel as string | null | undefined) ?? null,
    linkedOperator: Boolean(raw.linkedOperator),
    linkedOperatorEmail:
      (raw.linkedOperatorEmail as string | null | undefined) ?? null,
  }
}

function normalizeAttachment(raw: Record<string, unknown>) {
  return {
    id: Number(raw.id),
    fileName: String(raw.fileName ?? ""),
    contentType: String(raw.contentType ?? ""),
    sizeBytes: Number(raw.sizeBytes ?? 0),
    createdAt: String(raw.createdAt ?? ""),
  }
}

function normalizeDetail(raw: Record<string, unknown>): SupportQueryDetail {
  const messages = Array.isArray(raw.messages)
    ? raw.messages.map((message) => {
        const item = message as Record<string, unknown>
        return {
          id: Number(item.id),
          authorKind: parseHelpCentreAuthorKind(item.authorKind),
          body: String(item.body ?? ""),
          createdAt: String(item.createdAt ?? ""),
        }
      })
    : []

  const attachments = Array.isArray(raw.attachments)
    ? raw.attachments.map((attachment) =>
        normalizeAttachment(attachment as Record<string, unknown>)
      )
    : []

  return {
    ...normalizeListItem(raw),
    phone: (raw.phone as string | null | undefined) ?? null,
    queryLocation: raw.queryLocation as SupportQueryDetail["queryLocation"],
    escalationNote:
      (raw.escalationNote as string | null | undefined) ?? null,
    createdAt: String(raw.createdAt ?? ""),
    messages,
    attachments,
  }
}

export async function getSupportQueries(filters?: {
  status?: string
  topic?: string
}): Promise<SupportQueryListItem[]> {
  const response = await axiosInstance.get("/support/queries", {
    params: filters,
  })
  const data = unwrapDataObject(response.data) ?? {}
  const queries = Array.isArray(data.queries) ? data.queries : []

  return queries.map((query) =>
    normalizeListItem(query as Record<string, unknown>)
  )
}

export async function getSupportQuery(id: number): Promise<SupportQueryDetail> {
  const response = await axiosInstance.get(`/support/queries/${id}`)
  const data = unwrapDataObject(response.data) ?? {}
  return normalizeDetail(data)
}

export async function postSupportReply(
  id: number,
  body: string
): Promise<SupportQueryDetail> {
  const response = await axiosInstance.post(`/support/queries/${id}/replies`, {
    body,
  })
  const data = unwrapDataObject(response.data) ?? {}
  return normalizeDetail(data)
}

export async function patchSupportQueryStatus(
  id: number,
  status: HelpCentreQueryStatus,
  escalationNote?: string
): Promise<SupportQueryDetail> {
  const response = await axiosInstance.patch(`/support/queries/${id}/status`, {
    status,
    escalationNote,
  })
  const data = unwrapDataObject(response.data) ?? {}
  return normalizeDetail(data)
}

export async function downloadSupportQueryAttachment(
  queryId: number,
  attachmentId: number
): Promise<Blob> {
  const response = await axiosInstance.get(
    `/support/queries/${queryId}/attachments/${attachmentId}`,
    { responseType: "blob" }
  )

  return response.data as Blob
}
