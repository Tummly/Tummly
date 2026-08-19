import axios, { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"
import type { GuestSttResult } from "@/lib/guestFeedback/createGuestMicSttModule"
import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantActionType,
  OperatorAiAssistantAnalysisScope,
  OperatorAiAssistantConversationRow,
  OperatorAiAssistantListItem,
  OperatorAiAssistantMessage,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { CreateCampaignDraftRequest } from "@/types/operatorCampaigns"
import type { CreateCatalogOfferRequestBody } from "@/lib/operatorOffers/offerCatalogPresentation"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"
import { parseRecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"

type AssistantReportingPeriodDto = {
  kind: string
  presetId?: string | null
  startDate?: string | null
  endDate?: string | null
}

type AssistantAnalysisScopeDto = {
  ownedLocationId: number
  ownedLocationName: string
  reportingPeriod: AssistantReportingPeriodDto
}

type AssistantActionDto = {
  type: string
  label: string
  tab?: string | null
  sentiment?: string | null
  detectedTag?: string | null
  count?: number | null
  offerId?: number | null
  guestId?: number | null
  smartGroup?: string | null
  marketingEligible?: boolean | null
  feedbackId?: number | null
  intent?: string | null
  campaignId?: number | null
}

type AssistantMessageDto = {
  id: number
  role: string
  class?: string | null
  title?: string | null
  body: string
  analysisScope?: AssistantAnalysisScopeDto | null
  actions?: AssistantActionDto[] | null
}

type AssistantConversationDto = {
  id: number
  title: string
  analysisScope: AssistantAnalysisScopeDto
  lastActivityAt: string
  isArchived?: boolean
  messages: AssistantMessageDto[]
  pendingCampaignDraft?: CreateCampaignDraftRequest | null
  pendingOfferDraft?: CreateCatalogOfferRequestBody | null
  pendingRecoveryDraft?: {
    feedbackId: number
    intent: string
    channel?: string | null
    purpose?: string | null
    tone?: string | null
    includeNotes?: string | null
    subject?: string | null
    message?: string | null
    category?: string | null
    note?: string | null
    offerId?: number | null
    useConfirmedActionForGuestResponse?: boolean
  } | null
  draftInterviewActive?: boolean
}

type AssistantConversationListItemDto = {
  id: number
  title: string
  ownedLocationName: string
  lastActivityAt: string
  isArchived: boolean
}

type AssistantConversationResponse = {
  success: boolean
  conversation: AssistantConversationDto
}

function toReportingPeriodDto(
  range: HomePerformanceDateRange
): AssistantReportingPeriodDto {
  if (range.kind === "custom") {
    return {
      kind: "custom",
      startDate: range.startDate,
      endDate: range.endDate,
    }
  }
  return { kind: "preset", presetId: range.presetId }
}

function fromReportingPeriodDto(
  period: AssistantReportingPeriodDto
): HomePerformanceDateRange {
  if (period.kind === "custom" && period.startDate && period.endDate) {
    return {
      kind: "custom",
      startDate: period.startDate,
      endDate: period.endDate,
    }
  }
  const presetId =
    period.presetId === "last30" || period.presetId === "thisMonth"
      ? period.presetId
      : "last7"
  return { kind: "preset", presetId }
}

export function toAssistantAnalysisScopeDto(
  scope: OperatorAiAssistantAnalysisScope
): AssistantAnalysisScopeDto {
  return {
    ownedLocationId: scope.ownedLocationId,
    ownedLocationName: scope.ownedLocationName,
    reportingPeriod: toReportingPeriodDto(scope.reportingPeriod),
  }
}

function fromAnalysisScopeDto(
  scope: AssistantAnalysisScopeDto
): OperatorAiAssistantAnalysisScope {
  return {
    ownedLocationId: scope.ownedLocationId,
    ownedLocationName: scope.ownedLocationName,
    reportingPeriod: fromReportingPeriodDto(scope.reportingPeriod),
  }
}

function fromMessageDto(message: AssistantMessageDto): OperatorAiAssistantMessage {
  const classValue = message.class
  const answerClass =
    classValue === "grounded"
    || classValue === "refusal"
    || classValue === "failure"
    || classValue === "clarify"
      ? classValue
      : undefined

  return {
    id: String(message.id),
    role: message.role === "assistant" ? "assistant" : "user",
    class: answerClass,
    title: message.title,
    body: message.body,
    analysisScope: message.analysisScope
      ? fromAnalysisScopeDto(message.analysisScope)
      : undefined,
    actions: (message.actions ?? [])
      .map(fromActionDto)
      .filter((action): action is OperatorAiAssistantAction => action != null),
  }
}

const KNOWN_ASSISTANT_ACTION_TYPES = new Set<OperatorAiAssistantActionType>([
  "review-campaign",
  "draft-campaign",
  "draft-offer",
  "open-recovery",
  "view-feedback-set",
  "prepare-recovery",
  "view-campaigns",
  "view-offers",
  "view-offer",
  "view-guests",
  "view-guest",
  "view-capture",
])

function fromActionDto(
  action: AssistantActionDto
): OperatorAiAssistantAction | null {
  if (!KNOWN_ASSISTANT_ACTION_TYPES.has(action.type as OperatorAiAssistantActionType)) {
    return null
  }
  return {
    type: action.type as OperatorAiAssistantActionType,
    label: action.label,
    tab: action.tab,
    sentiment: action.sentiment,
    detectedTag: action.detectedTag,
    count: action.count,
    offerId: action.offerId,
    guestId: action.guestId,
    smartGroup: action.smartGroup,
    marketingEligible: action.marketingEligible,
    feedbackId: action.feedbackId,
    intent: action.intent,
    campaignId: action.campaignId,
  }
}

function fromPendingRecoveryDraft(
  draft: AssistantConversationDto["pendingRecoveryDraft"]
): RecoveryDraftActionPayload | null {
  return parseRecoveryDraftActionPayload(draft)
}

export function fromConversationDto(
  conversation: AssistantConversationDto
): OperatorAiAssistantConversationRow {
  return {
    id: String(conversation.id),
    title: conversation.title,
    analysisScope: fromAnalysisScopeDto(conversation.analysisScope),
    lastActivityAt: conversation.lastActivityAt,
    isArchived: conversation.isArchived === true,
    messages: conversation.messages.map(fromMessageDto),
    pendingCampaignDraft: conversation.pendingCampaignDraft ?? null,
    pendingOfferDraft: conversation.pendingOfferDraft ?? null,
    pendingRecoveryDraft: fromPendingRecoveryDraft(
      conversation.pendingRecoveryDraft
    ),
    draftInterviewActive: conversation.draftInterviewActive === true,
  }
}

export function fromConversationListItemDto(
  item: AssistantConversationListItemDto
): OperatorAiAssistantListItem {
  return {
    id: String(item.id),
    title: item.title,
    ownedLocationName: item.ownedLocationName,
    lastActivityAt: item.lastActivityAt,
    isArchived: item.isArchived,
  }
}

export async function sendAssistantTurn(input: {
  conversationId: string | null
  message: string
  analysisScope: OperatorAiAssistantAnalysisScope
  signal?: AbortSignal
}): Promise<OperatorAiAssistantConversationRow> {
  const conversationId =
    input.conversationId == null || input.conversationId === ""
      ? null
      : Number.parseInt(input.conversationId, 10)

  const response = await axiosInstance.post<AssistantConversationResponse>(
    "/assistant/turns",
    {
      conversationId: Number.isFinite(conversationId) ? conversationId : null,
      message: input.message,
      analysisScope: toAssistantAnalysisScopeDto(input.analysisScope),
    },
    { signal: input.signal }
  )
  return fromConversationDto(response.data.conversation)
}

export async function getAssistantConversation(
  conversationId: string
): Promise<OperatorAiAssistantConversationRow | null> {
  try {
    const response = await axiosInstance.get<AssistantConversationResponse>(
      `/assistant/conversations/${conversationId}`
    )
    return fromConversationDto(response.data.conversation)
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 404) {
      return null
    }
    throw error
  }
}

export async function retryAssistantTurn(
  conversationId: string,
  signal?: AbortSignal
): Promise<OperatorAiAssistantConversationRow> {
  const response = await axiosInstance.post<AssistantConversationResponse>(
    `/assistant/conversations/${conversationId}/retry`,
    {},
    { signal }
  )
  return fromConversationDto(response.data.conversation)
}

export async function applyAssistantScope(
  conversationId: string,
  analysisScope: OperatorAiAssistantAnalysisScope
): Promise<OperatorAiAssistantConversationRow> {
  const response = await axiosInstance.patch<AssistantConversationResponse>(
    `/assistant/conversations/${conversationId}/scope`,
    { analysisScope: toAssistantAnalysisScopeDto(analysisScope) }
  )
  return fromConversationDto(response.data.conversation)
}

export async function listAssistantConversations(
  archived: boolean
): Promise<OperatorAiAssistantListItem[]> {
  const response = await axiosInstance.get<{
    success: boolean
    conversations: AssistantConversationListItemDto[]
  }>("/assistant/conversations", { params: { archived } })
  return response.data.conversations.map(fromConversationListItemDto)
}

export async function archiveAssistantConversation(
  conversationId: string
): Promise<void> {
  await axiosInstance.patch(`/assistant/conversations/${conversationId}/archive`)
}

export async function unarchiveAssistantConversation(
  conversationId: string
): Promise<void> {
  await axiosInstance.patch(
    `/assistant/conversations/${conversationId}/unarchive`
  )
}

export async function deleteAssistantConversation(
  conversationId: string
): Promise<void> {
  await axiosInstance.delete(`/assistant/conversations/${conversationId}`)
}

export async function clearAssistantDraftInterview(
  conversationId: string
): Promise<void> {
  await axiosInstance.post(
    `/assistant/conversations/${conversationId}/draft-interview/clear`
  )
}

type AssistantSttResponse = {
  success: boolean
  text?: string
  code?: string
  message?: string
}

export async function transcribeOperatorAudio(
  audio: Blob
): Promise<GuestSttResult> {
  const formData = new FormData()
  const extension = audio.type.includes("ogg")
    ? "ogg"
    : audio.type.includes("mp4")
      ? "mp4"
      : "webm"
  formData.append("audio", audio, `clip.${extension}`)

  try {
    const response = await axiosInstance.post<AssistantSttResponse>(
      "/assistant/stt",
      formData,
      {
        transformRequest: [
          (data, headers) => {
            if (data instanceof FormData) {
              delete headers["Content-Type"]
            }
            return data
          },
        ],
      }
    )

    if (!response.data.success || typeof response.data.text !== "string") {
      return { ok: false, reason: "stt_failure" }
    }

    return { ok: true, text: response.data.text }
  } catch (error) {
    if (isAxiosError(error)) {
      if (error.response?.status === 429) {
        return { ok: false, reason: "rate_limit" }
      }

      const code = error.response?.data?.code
      if (error.response?.status === 422 && code === "empty_speech") {
        return { ok: false, reason: "empty_speech" }
      }
    }

    return { ok: false, reason: "stt_failure" }
  }
}

export function isAssistantTurnAborted(error: unknown): boolean {
  return (
    axios.isCancel(error)
    || (isAxiosError(error) && error.code === "ERR_CANCELED")
  )
}
