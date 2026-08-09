import { CAMPAIGN_TEMPLATE_PREVIEW_COPY } from "@/lib/operatorCampaigns/campaignTemplatePreviewPresentation"
import type {
  CampaignTemplateDetail,
  CampaignTemplatePreviewChannelId,
  CampaignTemplatePreviewMessage,
  CampaignTemplatePreviewOfferLogicRow,
  CampaignTemplatePreviewPayload,
} from "@/types/operatorCampaigns"

export type CampaignTemplatePreviewAdapters = {
  loadTemplateDetail: (id: string) => Promise<CampaignTemplateDetail>
}

export type CampaignTemplatePreviewChannelTab = {
  id: CampaignTemplatePreviewChannelId
  label: string
}

export type CampaignTemplatePreviewViewModel = {
  templateId: string
  title: string
  subtitle: string
  summary: CampaignTemplatePreviewPayload["summary"]
  channelTabs: CampaignTemplatePreviewChannelTab[]
  selectedChannelId: CampaignTemplatePreviewChannelId
  activeMessage: CampaignTemplatePreviewMessage | null
  showOfferLogic: boolean
  offerLogic: CampaignTemplatePreviewOfferLogicRow[]
  eligibility: CampaignTemplatePreviewPayload["eligibility"]
  suggestedTiming: string
  footerDisclaimer: string
  useThisTemplateLabel: string
  closeLabel: string
}

export type CampaignTemplatePreviewSnapshot = {
  open: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  viewModel: CampaignTemplatePreviewViewModel | null
  /** Cleared after close / Use — Use returns the id synchronously. */
  useTemplateId: string | null
}

export type CampaignTemplatePreviewModule = {
  getSnapshot: () => CampaignTemplatePreviewSnapshot
  subscribe: (listener: () => void) => () => void
  open: (templateId: string) => Promise<void>
  close: () => void
  retryLoad: () => Promise<void>
  setSelectedChannel: (channelId: CampaignTemplatePreviewChannelId) => void
  /** Closes Preview and returns the loaded template id for the shared Use path. */
  useThisTemplate: () => string | null
}

type PreviewState = {
  open: boolean
  loadStatus: CampaignTemplatePreviewSnapshot["loadStatus"]
  loadError: string | null
  templateId: string | null
  detail: CampaignTemplateDetail | null
  selectedChannelId: CampaignTemplatePreviewChannelId | null
  loadGeneration: number
  useTemplateId: string | null
}

function channelTabLabel(id: CampaignTemplatePreviewChannelId): string {
  return id === "sms"
    ? CAMPAIGN_TEMPLATE_PREVIEW_COPY.smsTab
    : CAMPAIGN_TEMPLATE_PREVIEW_COPY.emailTab
}

function buildViewModel(
  detail: CampaignTemplateDetail,
  selectedChannelId: CampaignTemplatePreviewChannelId
): CampaignTemplatePreviewViewModel {
  const preview = detail.preview
  const channelTabs = preview.suggestedChannels.map((id) => ({
    id,
    label: channelTabLabel(id),
  }))
  const activeMessage =
    preview.messages.find((message) => message.channel === selectedChannelId)
    ?? preview.messages[0]
    ?? null

  return {
    templateId: detail.id,
    title: detail.title,
    subtitle: CAMPAIGN_TEMPLATE_PREVIEW_COPY.subtitle,
    summary: preview.summary,
    channelTabs,
    selectedChannelId,
    activeMessage,
    showOfferLogic: preview.offerLogic != null && preview.offerLogic.length > 0,
    offerLogic: preview.offerLogic ?? [],
    eligibility: preview.eligibility,
    suggestedTiming: preview.suggestedTiming,
    footerDisclaimer: preview.footerDisclaimer,
    useThisTemplateLabel: CAMPAIGN_TEMPLATE_PREVIEW_COPY.useThisTemplate,
    closeLabel: CAMPAIGN_TEMPLATE_PREVIEW_COPY.close,
  }
}

function resolveSelectedChannel(
  detail: CampaignTemplateDetail,
  preferred: CampaignTemplatePreviewChannelId | null
): CampaignTemplatePreviewChannelId {
  const channels = detail.preview.suggestedChannels
  if (channels.length === 0) {
    return "email"
  }
  if (preferred != null && channels.includes(preferred)) {
    return preferred
  }
  return channels[0]!
}

function projectSnapshot(state: PreviewState): CampaignTemplatePreviewSnapshot {
  const viewModel =
    state.detail != null && state.selectedChannelId != null
      ? buildViewModel(state.detail, state.selectedChannelId)
      : null

  return {
    open: state.open,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    viewModel,
    useTemplateId: state.useTemplateId,
  }
}

export function createCampaignTemplatePreviewModule(
  adapters: CampaignTemplatePreviewAdapters
): CampaignTemplatePreviewModule {
  let state: PreviewState = {
    open: false,
    loadStatus: "idle",
    loadError: null,
    templateId: null,
    detail: null,
    selectedChannelId: null,
    loadGeneration: 0,
    useTemplateId: null,
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

  const loadDetail = async (templateId: string) => {
    const generation = state.loadGeneration + 1
    setState({
      loadGeneration: generation,
      loadStatus: "loading",
      loadError: null,
      detail: null,
      selectedChannelId: null,
      templateId,
    })

    try {
      const detail = await adapters.loadTemplateDetail(templateId)
      if (generation !== state.loadGeneration) {
        return
      }

      const selectedChannelId = resolveSelectedChannel(detail, null)
      setState({
        loadStatus: "loaded",
        loadError: null,
        detail,
        selectedChannelId,
        templateId: detail.id,
      })
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      setState({
        loadStatus: "error",
        loadError: CAMPAIGN_TEMPLATE_PREVIEW_COPY.loadError,
        detail: null,
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
    open: async (templateId) => {
      setState({
        open: true,
        useTemplateId: null,
      })
      await loadDetail(templateId)
    },
    close: () => {
      setState({
        open: false,
        loadStatus: "idle",
        loadError: null,
        templateId: null,
        detail: null,
        selectedChannelId: null,
        useTemplateId: null,
        loadGeneration: state.loadGeneration + 1,
      })
    },
    retryLoad: async () => {
      if (!state.open || state.templateId == null) {
        return
      }
      await loadDetail(state.templateId)
    },
    setSelectedChannel: (channelId) => {
      if (state.detail == null) {
        return
      }
      if (!state.detail.preview.suggestedChannels.includes(channelId)) {
        return
      }
      setState({ selectedChannelId: channelId })
    },
    useThisTemplate: () => {
      const templateId = state.detail?.id ?? state.templateId
      if (templateId == null || state.loadStatus !== "loaded") {
        return null
      }
      setState({
        open: false,
        loadStatus: "idle",
        loadError: null,
        templateId: null,
        detail: null,
        selectedChannelId: null,
        useTemplateId: null,
        loadGeneration: state.loadGeneration + 1,
      })
      return templateId
    },
  }
}
