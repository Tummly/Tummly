import { CAMPAIGN_AUDIENCE_OPTIONS } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { CAMPAIGN_CHANNEL_OPTIONS } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import { CAMPAIGN_DETAIL_PREVIEW_COPY } from "@/lib/operatorCampaigns/campaignDetailPreviewPresentation"
import { CAMPAIGN_OFFER_OPTIONS } from "@/lib/operatorCampaigns/campaignOfferPresentation"
import {
  labelForCampaignGoalId,
  type CampaignGoalId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type { CampaignTemplatePreviewChannelId } from "@/types/operatorCampaigns"

export type CampaignDetailPreviewSource = {
  id: number
  status: string
  name: string
  goalId: string | null
  audienceKey: string | null
  channel: string | null
  offerStance: string | null
  offerId: number | null
  messageSubject: string | null
  messageBody: string | null
}

export type CampaignDetailPreviewAdapters = {
  loadCampaign: (id: number) => Promise<CampaignDetailPreviewSource>
}

export type CampaignDetailPreviewChannelTab = {
  id: CampaignTemplatePreviewChannelId
  label: string
}

export type CampaignDetailPreviewMessage = {
  channel: CampaignTemplatePreviewChannelId
  body: string
  subject: string | null
  offerBlock: {
    title: string
    description: string
    redemptionCode: string
    expiryLabel: string
  } | null
}

export type CampaignDetailPreviewOfferLogicRow = {
  label: string
  value: string
}

export type CampaignDetailPreviewViewModel = {
  campaignId: number
  title: string
  subtitle: string
  summary: {
    goal: string
    audience: string
    channel: string
    offer: string
  }
  channelTabs: CampaignDetailPreviewChannelTab[]
  selectedChannelId: CampaignTemplatePreviewChannelId
  activeMessage: CampaignDetailPreviewMessage | null
  showOfferLogic: boolean
  offerLogic: CampaignDetailPreviewOfferLogicRow[]
  sendLogicLabel: string
  footerDisclaimer: string
  closeLabel: string
}

export type CampaignDetailPreviewSnapshot = {
  open: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  viewModel: CampaignDetailPreviewViewModel | null
}

export type CampaignDetailPreviewModule = {
  getSnapshot: () => CampaignDetailPreviewSnapshot
  subscribe: (listener: () => void) => () => void
  open: (campaignId: number) => Promise<void>
  close: () => void
  retryLoad: () => Promise<void>
  setSelectedChannel: (channelId: CampaignTemplatePreviewChannelId) => void
}

type PreviewState = {
  open: boolean
  loadStatus: CampaignDetailPreviewSnapshot["loadStatus"]
  loadError: string | null
  campaignId: number | null
  campaign: CampaignDetailPreviewSource | null
  selectedChannelId: CampaignTemplatePreviewChannelId | null
  loadGeneration: number
}

function emptyLabel(value: string | null | undefined): string {
  if (value == null || value.trim().length === 0) {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  }
  return value
}

function goalLabel(goalId: string | null): string {
  if (goalId == null || goalId.trim().length === 0) {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  }
  return (
    labelForCampaignGoalId(goalId as CampaignGoalId)
    ?? CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  )
}

function audienceLabel(audienceKey: string | null): string {
  if (audienceKey == null || audienceKey.trim().length === 0) {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  }
  return (
    CAMPAIGN_AUDIENCE_OPTIONS.find((option) => option.id === audienceKey)
      ?.title ?? audienceKey
  )
}

function channelLabel(channel: string | null): string {
  if (channel == null || channel.trim().length === 0) {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  }
  return (
    CAMPAIGN_CHANNEL_OPTIONS.find((option) => option.id === channel)?.title
    ?? channel
  )
}

function offerLabel(offerStance: string | null): string {
  if (offerStance == null || offerStance.trim().length === 0) {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  }
  return (
    CAMPAIGN_OFFER_OPTIONS.find((option) => option.id === offerStance)?.title
    ?? offerStance
  )
}

function resolveChannelId(
  channel: string | null
): CampaignTemplatePreviewChannelId {
  if (channel === "sms") {
    return "sms"
  }
  return "email"
}

function channelTabLabel(id: CampaignTemplatePreviewChannelId): string {
  return id === "sms"
    ? CAMPAIGN_DETAIL_PREVIEW_COPY.smsTab
    : CAMPAIGN_DETAIL_PREVIEW_COPY.emailTab
}

function sendLogicLabelForStatus(status: string): string {
  if (status === "draft") {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.notScheduled
  }
  if (status.length === 0) {
    return CAMPAIGN_DETAIL_PREVIEW_COPY.emptyValue
  }
  return status.charAt(0).toUpperCase() + status.slice(1)
}

function buildViewModel(
  campaign: CampaignDetailPreviewSource,
  selectedChannelId: CampaignTemplatePreviewChannelId
): CampaignDetailPreviewViewModel {
  const channelId = resolveChannelId(campaign.channel)
  const channelTabs: CampaignDetailPreviewChannelTab[] = [
    { id: channelId, label: channelTabLabel(channelId) },
  ]
  const body = campaign.messageBody?.trim() ?? ""
  const activeMessage: CampaignDetailPreviewMessage | null =
    body.length > 0
      ? {
          channel: channelId,
          body: campaign.messageBody ?? "",
          subject: campaign.messageSubject,
          offerBlock: null,
        }
      : null

  return {
    campaignId: campaign.id,
    title: campaign.name,
    subtitle: CAMPAIGN_DETAIL_PREVIEW_COPY.subtitle,
    summary: {
      goal: goalLabel(campaign.goalId),
      audience: audienceLabel(campaign.audienceKey),
      channel: channelLabel(campaign.channel),
      offer: offerLabel(campaign.offerStance),
    },
    channelTabs,
    selectedChannelId,
    activeMessage,
    showOfferLogic: false,
    offerLogic: [],
    sendLogicLabel: sendLogicLabelForStatus(campaign.status),
    footerDisclaimer: CAMPAIGN_DETAIL_PREVIEW_COPY.footerDisclaimer,
    closeLabel: CAMPAIGN_DETAIL_PREVIEW_COPY.close,
  }
}

function projectSnapshot(state: PreviewState): CampaignDetailPreviewSnapshot {
  const viewModel =
    state.campaign != null && state.selectedChannelId != null
      ? buildViewModel(state.campaign, state.selectedChannelId)
      : null

  return {
    open: state.open,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    viewModel,
  }
}

export function createCampaignDetailPreviewModule(
  adapters: CampaignDetailPreviewAdapters
): CampaignDetailPreviewModule {
  let state: PreviewState = {
    open: false,
    loadStatus: "idle",
    loadError: null,
    campaignId: null,
    campaign: null,
    selectedChannelId: null,
    loadGeneration: 0,
  }

  let snapshot = projectSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const setState = (patch: Partial<PreviewState>) => {
    state = { ...state, ...patch }
    snapshot = projectSnapshot(state)
    emit()
  }

  const loadDetail = async (campaignId: number) => {
    const generation = state.loadGeneration + 1
    setState({
      loadGeneration: generation,
      loadStatus: "loading",
      loadError: null,
      campaign: null,
      selectedChannelId: null,
      campaignId,
    })

    try {
      const campaign = await adapters.loadCampaign(campaignId)
      if (generation !== state.loadGeneration) {
        return
      }

      const selectedChannelId = resolveChannelId(campaign.channel)
      setState({
        loadStatus: "loaded",
        loadError: null,
        campaign,
        selectedChannelId,
        campaignId: campaign.id,
      })
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      setState({
        loadStatus: "error",
        loadError: CAMPAIGN_DETAIL_PREVIEW_COPY.loadError,
        campaign: null,
        selectedChannelId: null,
      })
    }
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    open: async (campaignId) => {
      setState({ open: true })
      await loadDetail(campaignId)
    },
    close: () => {
      setState({
        open: false,
        loadStatus: "idle",
        loadError: null,
        campaignId: null,
        campaign: null,
        selectedChannelId: null,
        loadGeneration: state.loadGeneration + 1,
      })
    },
    retryLoad: async () => {
      if (!state.open || state.campaignId == null) {
        return
      }
      await loadDetail(state.campaignId)
    },
    setSelectedChannel: (channelId) => {
      if (state.campaign == null) {
        return
      }
      if (resolveChannelId(state.campaign.channel) !== channelId) {
        return
      }
      setState({ selectedChannelId: channelId })
    },
  }
}
