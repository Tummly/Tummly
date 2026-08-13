import axios, { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"
import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantAnalysisScope,
  OperatorAiAssistantConversationRow,
  OperatorAiAssistantMessage,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

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
  messages: AssistantMessageDto[]
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
    actions: (message.actions ?? []).map(fromActionDto),
  }
}

function fromActionDto(action: AssistantActionDto): OperatorAiAssistantAction {
  return {
    type: action.type,
    label: action.label,
    tab: action.tab,
    sentiment: action.sentiment,
    detectedTag: action.detectedTag,
    count: action.count,
    offerId: action.offerId,
    guestId: action.guestId,
    smartGroup: action.smartGroup,
    marketingEligible: action.marketingEligible,
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
    messages: conversation.messages.map(fromMessageDto),
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

export function isAssistantTurnAborted(error: unknown): boolean {
  return (
    axios.isCancel(error)
    || (isAxiosError(error) && error.code === "ERR_CANCELED")
  )
}
