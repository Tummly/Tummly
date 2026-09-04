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
  AssistantSendScheduleRoute,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import {
  ALL_LOCATIONS_CHROME_LABEL,
  analysisScopeKind,
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
  scopeKind?: "all" | "single"
  ownedLocationId?: number | null
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
  recommendedNextStep?: string | null
  meta?: string | null
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
    locationId?: number | null
    useConfirmedActionForGuestResponse?: boolean
  } | null
  draftInterviewActive?: boolean
  sendScheduleRoute?: {
    kind: string
    campaignId?: number | null
    step?: string | null
    scheduleMode?: string | null
    dateLocal?: string | null
    timeLocal?: string | null
    feedbackId?: number | null
    intent?: string | null
  } | null
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
    period.presetId === "last30"
    || period.presetId === "thisMonth"
    || period.presetId === "previousMonth"
      ? period.presetId
      : "last7"
  return { kind: "preset", presetId }
}

export function toAssistantAnalysisScopeDto(
  scope: OperatorAiAssistantAnalysisScope
): AssistantAnalysisScopeDto {
  if (analysisScopeKind(scope) === "all") {
    return {
      scopeKind: "all",
      ownedLocationName: ALL_LOCATIONS_CHROME_LABEL,
      reportingPeriod: toReportingPeriodDto(scope.reportingPeriod),
    }
  }
  return {
    scopeKind: "single",
    ownedLocationId: scope.ownedLocationId,
    ownedLocationName: scope.ownedLocationName,
    reportingPeriod: toReportingPeriodDto(scope.reportingPeriod),
  }
}

function fromAnalysisScopeDto(
  scope: AssistantAnalysisScopeDto
): OperatorAiAssistantAnalysisScope {
  if (scope.scopeKind === "all") {
    return {
      scopeKind: "all",
      ownedLocationId: null,
      ownedLocationName: scope.ownedLocationName || ALL_LOCATIONS_CHROME_LABEL,
      reportingPeriod: fromReportingPeriodDto(scope.reportingPeriod),
    }
  }
  return {
    scopeKind: "single",
    ownedLocationId: scope.ownedLocationId ?? null,
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
      || classValue === "gap"
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
    recommendedNextStep: message.recommendedNextStep ?? null,
    meta: message.meta ?? null,
  }
}

const KNOWN_ASSISTANT_ACTION_TYPES = new Set<OperatorAiAssistantActionType>([
  "review-campaign",
  "change-audience",
  "add-offer",
  "review-offer",
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

function fromSendScheduleRoute(
  route: AssistantConversationDto["sendScheduleRoute"]
): AssistantSendScheduleRoute | null {
  if (route == null) {
    return null
  }
  if (route.kind !== "campaign" && route.kind !== "recovery") {
    return null
  }
  const step =
    route.step === "review" || route.step === "schedule" ? route.step : null
  const scheduleMode =
    route.scheduleMode === "send-now" || route.scheduleMode === "schedule-later"
      ? route.scheduleMode
      : null
  return {
    kind: route.kind,
    campaignId: route.campaignId,
    step,
    scheduleMode,
    dateLocal: route.dateLocal,
    timeLocal: route.timeLocal,
    feedbackId: route.feedbackId,
    intent: route.intent,
  }
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
    sendScheduleRoute: fromSendScheduleRoute(conversation.sendScheduleRoute),
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

const FIGMA_MOCK_CONVERSATIONS: AssistantConversationListItemDto[] = [
  {
    id: 101,
    title: "Weekly feedback themes",
    ownedLocationName: "Camden",
    lastActivityAt: new Date(Date.now() - 12 * 60 * 1000).toISOString(),
    isArchived: false,
  },
  {
    id: 102,
    title: "Camden service issues",
    ownedLocationName: "Camden",
    lastActivityAt: new Date(Date.now() - 25 * 60 * 60 * 1000).toISOString(),
    isArchived: false,
  },
  {
    id: 103,
    title: "Thank-you campaign draft",
    ownedLocationName: "Camden",
    lastActivityAt: new Date(Date.now() - 27 * 60 * 60 * 1000).toISOString(),
    isArchived: false,
  },
  {
    id: 104,
    title: "August offer idea",
    ownedLocationName: "Camden",
    lastActivityAt: new Date(Date.now() - 29 * 60 * 60 * 1000).toISOString(),
    isArchived: false,
  },
  {
    id: 105,
    title: "August offer idea",
    ownedLocationName: "Camden",
    lastActivityAt: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000).toISOString(),
    isArchived: false,
  },
]

function createMockConversationDto(
  id: number,
  title: string
): AssistantConversationDto {
  const isWeeklyOrComplaints =
    title.toLowerCase().includes("weekly")
    || title.toLowerCase().includes("complain")
    || title.toLowerCase().includes("feedback")

  return {
    id,
    title: isWeeklyOrComplaints ? "What are guests complaining about this week?" : title,
    analysisScope: {
      scopeKind: "single",
      ownedLocationId: 1,
      ownedLocationName: "Camden",
      reportingPeriod: {
        kind: "preset",
        presetId: "last-7-days",
      },
    },
    lastActivityAt: new Date().toISOString(),
    isArchived: false,
    messages: [
      {
        id: 1,
        role: "user",
        body: isWeeklyOrComplaints
          ? "What are guests complaining about this week?"
          : title,
      },
      {
        id: 2,
        role: "assistant",
        class: "grounded",
        title: isWeeklyOrComplaints
          ? "Slow service is the main issue this week"
          : `Summary for ${title}`,
        body: isWeeklyOrComplaints
          ? "Slow service appeared in 6 of 24 feedback submissions, mainly between 7:00 PM and 9:00 PM."
          : `Here is the summary for **${title}** (Mehmet’s Grill · Camden · Last 7 days):\n\n• **Feedback Themes**: Overall satisfaction is strong. Speed of service and delivery time are the main areas guests mentioned.\n• **Guest Engagement**: 54 scans and 19 feedback submissions were recorded.`,
        meta: "Camden · Last 7 days · 24 submissions",
        recommendedNextStep:
          "Review the six affected submissions and prepare follow-up responses where contact and permissions allow.",
        actions: [
          {
            type: "view-feedback-set",
            label: "View 6 feedback items",
            clickable: true,
          },
          {
            type: "prepare-recovery",
            label: "Prepare recovery responses",
            clickable: true,
          },
          {
            type: "view-capture",
            label: "Show issue breakdown",
            clickable: true,
          },
        ],
      },
    ],
    pendingCampaignDraft: null,
    pendingOfferDraft: null,
    pendingRecoveryDraft: null,
    draftInterviewActive: false,
    sendScheduleRoute: null,
  }
}

export async function getAssistantConversation(
  conversationId: string
): Promise<OperatorAiAssistantConversationRow | null> {
  const matchingMock = FIGMA_MOCK_CONVERSATIONS.find(
    (item) => String(item.id) === conversationId
  )

  try {
    const response = await axiosInstance.get<AssistantConversationResponse>(
      `/assistant/conversations/${conversationId}`
    )
    return fromConversationDto(response.data.conversation)
  } catch (error) {
    if (matchingMock) {
      return fromConversationDto(
        createMockConversationDto(matchingMock.id, matchingMock.title)
      )
    }
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
  try {
    const response = await axiosInstance.get<{
      success: boolean
      conversations: AssistantConversationListItemDto[]
    }>("/assistant/conversations", { params: { archived } })
    if (response.data?.conversations && response.data.conversations.length > 0) {
      return response.data.conversations.map(fromConversationListItemDto)
    }
    if (!archived) {
      return FIGMA_MOCK_CONVERSATIONS.map(fromConversationListItemDto)
    }
    return []
  } catch {
    if (!archived) {
      return FIGMA_MOCK_CONVERSATIONS.map(fromConversationListItemDto)
    }
    return []
  }
}

export async function archiveAssistantConversation(
  conversationId: string
): Promise<void> {
  try {
    await axiosInstance.patch(`/assistant/conversations/${conversationId}/archive`)
  } catch (error) {
    const isMock = FIGMA_MOCK_CONVERSATIONS.some(
      (item) => String(item.id) === conversationId
    )
    if (!isMock) throw error
  }
}

export async function unarchiveAssistantConversation(
  conversationId: string
): Promise<void> {
  try {
    await axiosInstance.patch(
      `/assistant/conversations/${conversationId}/unarchive`
    )
  } catch (error) {
    const isMock = FIGMA_MOCK_CONVERSATIONS.some(
      (item) => String(item.id) === conversationId
    )
    if (!isMock) throw error
  }
}

export async function deleteAssistantConversation(
  conversationId: string
): Promise<void> {
  try {
    await axiosInstance.delete(`/assistant/conversations/${conversationId}`)
  } catch (error) {
    const isMock =
      FIGMA_MOCK_CONVERSATIONS.some(
        (item) => String(item.id) === String(conversationId)
      ) ||
      String(conversationId).startsWith("conv-") ||
      import.meta.env.DEV
    if (!isMock) throw error
  }
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
