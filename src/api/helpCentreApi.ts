import axiosInstance from "./axiosInstance"
import { unwrapDataObject } from "@/lib/apiEnvelope"
import {
  parseHelpCentreAuthorKind,
  parseHelpCentreQueryStatus,
} from "@/lib/helpCentreApiNormalize"
import type {
  HelpCentreContactPrefill,
  HelpCentreQueryDetail,
  HelpCentreQueryListItem,
} from "@/types/helpCentre"

export interface CreateHelpCentreQueryPayload {
  topic: string
  businessName: string
  submitterName: string
  submitterEmail: string
  phone?: string
  restaurantLocationId?: number
  message: string
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
): Promise<{ id: number }> {
  const response = await axiosInstance.post(
    "/help-centre/queries",
    payload,
    { skipAuthRedirect: true }
  )
  const data = unwrapDataObject(response.data) ?? {}
  return { id: Number(data.id) }
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
): Promise<HelpCentreQueryDetail> {
  const response = await axiosInstance.post(
    `/help-centre/my-queries/${id}/replies`,
    { body }
  )
  const data = unwrapDataObject(response.data) ?? {}
  return normalizeDetail(data)
}
