import axiosInstance from "./axiosInstance"
import { unwrapDataObject } from "@/lib/apiEnvelope"
import {
  parseHelpCentreAuthorKind,
  parseHelpCentreQueryStatus,
} from "@/lib/helpCentreApiNormalize"
import type {
  HelpCentreContactPrefill,
  HelpCentreQueryAttachment,
  HelpCentreQueryDetail,
  HelpCentreQueryListItem,
} from "@/types/helpCentre"
import {
  parseEmailDispatchMeta,
  type EmailDispatchMeta,
} from "@/lib/emailDispatch"

export interface CreateHelpCentreQueryPayload {
  topic: string
  businessName: string
  submitterName: string
  submitterEmail: string
  phone?: string
  restaurantLocationId?: number
  message: string
  attachments?: File[]
}

function normalizeAttachment(
  raw: Record<string, unknown>
): HelpCentreQueryAttachment {
  return {
    id: Number(raw.id),
    fileName: String(raw.fileName ?? ""),
    contentType: String(raw.contentType ?? ""),
    sizeBytes: Number(raw.sizeBytes ?? 0),
    createdAt: String(raw.createdAt ?? ""),
  }
}

function normalizeListItem(raw: Record<string, unknown>): HelpCentreQueryListItem {
  return {
    id: Number(raw.id),
    topic: String(raw.topic ?? ""),
    topicLabel: String(raw.topicLabel ?? ""),
    status: parseHelpCentreQueryStatus(raw.status),
    statusLabel: String(raw.statusLabel ?? ""),
    updatedAt: String(raw.updatedAt ?? ""),
    preview: (raw.preview as string | null | undefined) ?? null,
  }
}

function normalizeDetail(raw: Record<string, unknown>): HelpCentreQueryDetail {
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
    submitterName: String(raw.submitterName ?? ""),
    submitterEmail: String(raw.submitterEmail ?? ""),
    phone: (raw.phone as string | null | undefined) ?? null,
    businessName: String(raw.businessName ?? ""),
    queryLocation: raw.queryLocation as HelpCentreQueryDetail["queryLocation"],
    canReply: Boolean(raw.canReply),
    createdAt: String(raw.createdAt ?? ""),
    messages,
    attachments,
  }
}

export async function getHelpCentreContactPrefill(): Promise<HelpCentreContactPrefill> {
  const response = await axiosInstance.get("/help-centre/contact-prefill")
  const data = unwrapDataObject(response.data) ?? {}

  return {
    businessName: String(data.businessName ?? ""),
    submitterName: String(data.submitterName ?? ""),
    submitterEmail: String(data.submitterEmail ?? ""),
    locations: Array.isArray(data.locations)
      ? data.locations.map((location) => {
          const item = location as Record<string, unknown>
          return {
            id: Number(item.id),
            label: String(item.label ?? ""),
          }
        })
      : [],
  }
}

export async function createHelpCentreQuery(
  payload: CreateHelpCentreQueryPayload
): Promise<{ id: number } & EmailDispatchMeta> {
  const formData = new FormData()
  formData.append("topic", payload.topic)
  formData.append("businessName", payload.businessName)
  formData.append("submitterName", payload.submitterName)
  formData.append("submitterEmail", payload.submitterEmail)
  formData.append("message", payload.message)

  if (payload.phone?.trim()) {
    formData.append("phone", payload.phone.trim())
  }

  if (payload.restaurantLocationId) {
    formData.append(
      "restaurantLocationId",
      String(payload.restaurantLocationId)
    )
  }

  payload.attachments?.forEach((file) => {
    formData.append("attachments", file)
  })

  const response = await axiosInstance.post("/help-centre/queries", formData, {
    skipAuthRedirect: true,
    // Let the browser set multipart boundary; the axios default is application/json.
    transformRequest: [(data, headers) => {
      if (data instanceof FormData) {
        delete headers["Content-Type"]
      }

      return data
    }],
  })
  const data = unwrapDataObject(response.data) ?? {}
  return {
    id: Number(data.id),
    ...parseEmailDispatchMeta(data),
  }
}

export async function getMyHelpCentreQueries(): Promise<HelpCentreQueryListItem[]> {
  const response = await axiosInstance.get("/help-centre/my-queries")
  const data = unwrapDataObject(response.data) ?? {}
  const queries = Array.isArray(data.queries) ? data.queries : []

  return queries.map((query) =>
    normalizeListItem(query as Record<string, unknown>)
  )
}

export async function getMyHelpCentreQuery(
  id: number
): Promise<HelpCentreQueryDetail> {
  const response = await axiosInstance.get(`/help-centre/my-queries/${id}`)
  const data = unwrapDataObject(response.data) ?? {}
  return normalizeDetail(data)
}

export async function postMyHelpCentreReply(
  id: number,
  body: string
): Promise<HelpCentreQueryDetail & EmailDispatchMeta> {
  const response = await axiosInstance.post(
    `/help-centre/my-queries/${id}/replies`,
    { body }
  )
  const data = unwrapDataObject(response.data) ?? {}
  return {
    ...normalizeDetail(data),
    ...parseEmailDispatchMeta(data),
  }
}

export async function downloadMyHelpCentreQueryAttachment(
  queryId: number,
  attachmentId: number
): Promise<Blob> {
  const response = await axiosInstance.get(
    `/help-centre/my-queries/${queryId}/attachments/${attachmentId}`,
    { responseType: "blob" }
  )

  return response.data as Blob
}
