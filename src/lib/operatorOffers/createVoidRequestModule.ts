import {
  validateVoidCreateForm,
  voidRequestFormErrorMessage,
  type VoidCreatePreview,
  type VoidRequestAdapters,
  type VoidReviewDetail,
} from "@/lib/operatorOffers/voidRequestAdapters"
import type {
  VoidRequestCorrectionId,
  VoidRequestReasonId,
} from "@/lib/operatorOffers/voidRequestPresentation"

export type VoidRequestStep =
  | "closed"
  | "create"
  | "review"
  | "confirm-approve"
  | "confirm-reject"

export type VoidRequestSnapshot = {
  open: boolean
  step: VoidRequestStep
  createPreview: VoidCreatePreview | null
  reviewDetail: VoidReviewDetail | null
  reasonId: VoidRequestReasonId | null
  explanation: string
  correctionId: VoidRequestCorrectionId | null
  formError: string | null
  activeRequestId: string | null
  busy: boolean
}

export type VoidSendResult =
  | "sent"
  | "validation_failed"
  | "pending_exists"
  | "failed"
  | "noop"

export type VoidOutcomeActionResult =
  | "approved"
  | "rejected"
  | "failed"
  | "noop"

export type VoidRequestModule = {
  getSnapshot: () => VoidRequestSnapshot
  subscribe: (listener: () => void) => () => void
  openCreate: (preview: VoidCreatePreview) => void
  openReview: (detail: VoidReviewDetail) => void
  openApproveConfirm: (requestId: string) => Promise<void>
  openRejectConfirm: (requestId: string) => Promise<void>
  setReason: (reasonId: VoidRequestReasonId) => void
  setExplanation: (explanation: string) => void
  setCorrection: (correctionId: VoidRequestCorrectionId) => void
  sendRequest: () => Promise<VoidSendResult>
  requestApprove: () => void
  requestReject: () => void
  confirmApprove: () => Promise<VoidOutcomeActionResult>
  confirmReject: () => Promise<VoidOutcomeActionResult>
  goBack: () => void
  close: () => void
}

type VoidRequestState = {
  open: boolean
  step: VoidRequestStep
  createPreview: VoidCreatePreview | null
  reviewDetail: VoidReviewDetail | null
  reasonId: VoidRequestReasonId | null
  explanation: string
  correctionId: VoidRequestCorrectionId | null
  formError: string | null
  activeRequestId: string | null
  busy: boolean
  actionGeneration: number
}

function projectSnapshot(state: VoidRequestState): VoidRequestSnapshot {
  return {
    open: state.open,
    step: state.step,
    createPreview: state.createPreview,
    reviewDetail: state.reviewDetail,
    reasonId: state.reasonId,
    explanation: state.explanation,
    correctionId: state.correctionId,
    formError: state.formError,
    activeRequestId: state.activeRequestId,
    busy: state.busy,
  }
}

function initialState(): VoidRequestState {
  return {
    open: false,
    step: "closed",
    createPreview: null,
    reviewDetail: null,
    reasonId: null,
    explanation: "",
    correctionId: null,
    formError: null,
    activeRequestId: null,
    busy: false,
    actionGeneration: 0,
  }
}

export function createVoidRequestModule(
  adapters: VoidRequestAdapters
): VoidRequestModule {
  let state = initialState()
  let snapshot = projectSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const setState = (patch: Partial<VoidRequestState>) => {
    state = { ...state, ...patch }
    snapshot = projectSnapshot(state)
    emit()
  }

  const resetClosed = () => {
    setState({
      ...initialState(),
      actionGeneration: state.actionGeneration + 1,
    })
  }

  const openConfirmFromId = async (
    requestId: string,
    step: "confirm-approve" | "confirm-reject"
  ) => {
    const generation = state.actionGeneration + 1
    setState({
      actionGeneration: generation,
      busy: true,
      formError: null,
    })
    try {
      const detail = await adapters.getRequest(requestId)
      if (generation !== state.actionGeneration) {
        return
      }
      if (detail == null) {
        setState({
          busy: false,
          formError: voidRequestFormErrorMessage("requestNotFound"),
          open: false,
          step: "closed",
        })
        return
      }
      setState({
        ...initialState(),
        open: true,
        step,
        reviewDetail: detail,
        activeRequestId: detail.requestId,
        actionGeneration: state.actionGeneration + 1,
      })
    } catch {
      if (generation !== state.actionGeneration) {
        return
      }
      setState({
        busy: false,
        formError: voidRequestFormErrorMessage("requestNotFound"),
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
    openCreate: (preview) => {
      setState({
        ...initialState(),
        open: true,
        step: "create",
        createPreview: preview,
        actionGeneration: state.actionGeneration + 1,
      })
    },
    openReview: (detail) => {
      setState({
        ...initialState(),
        open: true,
        step: "review",
        reviewDetail: detail,
        activeRequestId: detail.requestId,
        actionGeneration: state.actionGeneration + 1,
      })
    },
    openApproveConfirm: (requestId) =>
      openConfirmFromId(requestId, "confirm-approve"),
    openRejectConfirm: (requestId) =>
      openConfirmFromId(requestId, "confirm-reject"),
    setReason: (reasonId) => {
      setState({
        reasonId,
        formError: null,
      })
    },
    setExplanation: (explanation) => {
      setState({
        explanation,
        formError: null,
      })
    },
    setCorrection: (correctionId) => {
      setState({
        correctionId,
        formError: null,
      })
    },
    sendRequest: async () => {
      if (!state.open || state.step !== "create" || state.createPreview == null) {
        return "noop"
      }

      const validation = validateVoidCreateForm({
        reasonId: state.reasonId,
        explanation: state.explanation,
        correctionId: state.correctionId,
      })
      if (!validation.ok) {
        setState({
          formError: voidRequestFormErrorMessage(validation.errorKey),
        })
        return "validation_failed"
      }

      const preview = state.createPreview
      const reasonId = state.reasonId!
      const correctionId = state.correctionId!
      const generation = state.actionGeneration + 1
      setState({
        actionGeneration: generation,
        busy: true,
        formError: null,
      })

      try {
        const result = await adapters.createRequest({
          passId: preview.passId,
          redemptionId: preview.redemptionId,
          offerId: preview.offerId,
          locationId: preview.locationId,
          reasonId,
          explanation: validation.explanation,
          correctionId,
          summary: {
            offerTitle: preview.offerTitle,
            guestName: preview.guestName,
            passCodeMasked: preview.passCodeMasked,
            currentStateText: preview.currentStateText,
            expiresText: preview.expiresText,
            locationName: preview.locationName,
            linkedCampaignText: preview.linkedCampaignText,
          },
        })

        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }

        if (!result.ok) {
          setState({
            busy: false,
            formError: voidRequestFormErrorMessage(
              result.reason === "pending_exists"
                ? "pendingExists"
                : "createFailed"
            ),
          })
          return result.reason === "pending_exists"
            ? "pending_exists"
            : "failed"
        }

        await adapters.notifyApprovers(result.requestId)
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }

        resetClosed()
        return "sent"
      } catch {
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        setState({
          busy: false,
          formError: voidRequestFormErrorMessage("createFailed"),
        })
        return "failed"
      }
    },
    requestApprove: () => {
      if (!state.open || state.step !== "review" || state.activeRequestId == null) {
        return
      }
      setState({
        step: "confirm-approve",
        formError: null,
      })
    },
    requestReject: () => {
      if (!state.open || state.step !== "review" || state.activeRequestId == null) {
        return
      }
      setState({
        step: "confirm-reject",
        formError: null,
      })
    },
    confirmApprove: async () => {
      if (
        !state.open
        || state.step !== "confirm-approve"
        || state.activeRequestId == null
      ) {
        return "noop"
      }

      const requestId = state.activeRequestId
      const generation = state.actionGeneration + 1
      setState({
        actionGeneration: generation,
        busy: true,
        formError: null,
      })

      try {
        const result = await adapters.approveRequest(requestId)
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        if (!result.ok) {
          setState({
            busy: false,
            formError: voidRequestFormErrorMessage("approveFailed"),
          })
          return "failed"
        }
        await adapters.notifySubmitter(requestId, "approved")
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        resetClosed()
        return "approved"
      } catch {
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        setState({
          busy: false,
          formError: voidRequestFormErrorMessage("approveFailed"),
        })
        return "failed"
      }
    },
    confirmReject: async () => {
      if (
        !state.open
        || state.step !== "confirm-reject"
        || state.activeRequestId == null
      ) {
        return "noop"
      }

      const requestId = state.activeRequestId
      const generation = state.actionGeneration + 1
      setState({
        actionGeneration: generation,
        busy: true,
        formError: null,
      })

      try {
        const result = await adapters.rejectRequest(requestId)
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        if (!result.ok) {
          setState({
            busy: false,
            formError: voidRequestFormErrorMessage("rejectFailed"),
          })
          return "failed"
        }
        await adapters.notifySubmitter(requestId, "rejected")
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        resetClosed()
        return "rejected"
      } catch {
        if (generation !== state.actionGeneration || !state.open) {
          return "noop"
        }
        setState({
          busy: false,
          formError: voidRequestFormErrorMessage("rejectFailed"),
        })
        return "failed"
      }
    },
    goBack: () => {
      if (
        !state.open
        || (state.step !== "confirm-approve" && state.step !== "confirm-reject")
      ) {
        return
      }
      setState({
        step: "review",
        formError: null,
      })
    },
    close: () => {
      resetClosed()
    },
  }
}
