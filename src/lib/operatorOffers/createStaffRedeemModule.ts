import {
  staffRedeemErrorMessage,
  type StaffRedeemAdapters,
  type StaffRedeemConfirmPreview,
} from "@/lib/operatorOffers/staffRedeemAdapters"

export type StaffRedeemStep = "enter-code" | "confirm"

export type StaffRedeemSnapshot = {
  open: boolean
  step: StaffRedeemStep
  locationId: number | null
  code: string
  checkError: string | null
  confirmPreview: StaffRedeemConfirmPreview | null
  checkBusy: boolean
  redeemBusy: boolean
}

export type StaffRedeemMarkResult = "redeemed" | "failed" | "noop"

export type StaffRedeemModule = {
  getSnapshot: () => StaffRedeemSnapshot
  subscribe: (listener: () => void) => () => void
  open: (locationId: number) => void
  close: () => void
  setCode: (code: string) => void
  checkOffer: () => Promise<void>
  cancelConfirm: () => void
  markAsRedeemed: () => Promise<StaffRedeemMarkResult>
  /** QR success path — fills code and runs Check offer only (not Mark). */
  applyScannedCode: (code: string) => Promise<void>
}

type StaffRedeemState = {
  open: boolean
  step: StaffRedeemStep
  locationId: number | null
  code: string
  checkError: string | null
  confirmPreview: StaffRedeemConfirmPreview | null
  checkBusy: boolean
  redeemBusy: boolean
  checkGeneration: number
  redeemGeneration: number
}

function projectSnapshot(state: StaffRedeemState): StaffRedeemSnapshot {
  return {
    open: state.open,
    step: state.step,
    locationId: state.locationId,
    code: state.code,
    checkError: state.checkError,
    confirmPreview: state.confirmPreview,
    checkBusy: state.checkBusy,
    redeemBusy: state.redeemBusy,
  }
}

function initialState(): StaffRedeemState {
  return {
    open: false,
    step: "enter-code",
    locationId: null,
    code: "",
    checkError: null,
    confirmPreview: null,
    checkBusy: false,
    redeemBusy: false,
    checkGeneration: 0,
    redeemGeneration: 0,
  }
}

export function createStaffRedeemModule(
  adapters: StaffRedeemAdapters
): StaffRedeemModule {
  let state = initialState()
  let snapshot = projectSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const setState = (patch: Partial<StaffRedeemState>) => {
    state = { ...state, ...patch }
    snapshot = projectSnapshot(state)
    emit()
  }

  const runCheck = async () => {
    if (!state.open || state.locationId == null) {
      return
    }

    const code = state.code.trim()
    if (code.length === 0) {
      setState({
        checkError: staffRedeemErrorMessage("empty_code"),
        confirmPreview: null,
        step: "enter-code",
      })
      return
    }

    const locationId = state.locationId
    const generation = state.checkGeneration + 1
    setState({
      checkGeneration: generation,
      checkBusy: true,
      checkError: null,
    })

    try {
      const result = await adapters.checkCode(locationId, code)
      if (generation !== state.checkGeneration || !state.open) {
        return
      }

      if (!result.ok) {
        setState({
          checkBusy: false,
          checkError: staffRedeemErrorMessage(result.reason),
          confirmPreview: null,
          step: "enter-code",
        })
        return
      }

      setState({
        checkBusy: false,
        checkError: null,
        confirmPreview: result.preview,
        step: "confirm",
      })
    } catch {
      if (generation !== state.checkGeneration || !state.open) {
        return
      }
      setState({
        checkBusy: false,
        checkError: staffRedeemErrorMessage("invalid"),
        confirmPreview: null,
        step: "enter-code",
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
    open: (locationId) => {
      setState({
        ...initialState(),
        open: true,
        locationId,
        checkGeneration: state.checkGeneration + 1,
        redeemGeneration: state.redeemGeneration + 1,
      })
    },
    close: () => {
      setState({
        ...initialState(),
        checkGeneration: state.checkGeneration + 1,
        redeemGeneration: state.redeemGeneration + 1,
      })
    },
    setCode: (code) => {
      setState({
        code,
        checkError: null,
      })
    },
    checkOffer: runCheck,
    cancelConfirm: () => {
      if (state.step !== "confirm") {
        return
      }
      setState({
        step: "enter-code",
        confirmPreview: null,
        checkError: null,
      })
    },
    markAsRedeemed: async () => {
      if (
        !state.open
        || state.step !== "confirm"
        || state.locationId == null
        || state.confirmPreview == null
      ) {
        return "noop"
      }

      const locationId = state.locationId
      const code = state.code.trim()
      const issueId = state.confirmPreview.issueId
      const generation = state.redeemGeneration + 1
      setState({
        redeemGeneration: generation,
        redeemBusy: true,
        checkError: null,
      })

      try {
        const result = await adapters.redeem(locationId, code, issueId)
        if (generation !== state.redeemGeneration || !state.open) {
          return "noop"
        }

        if (!result.ok) {
          setState({
            redeemBusy: false,
            checkError: staffRedeemErrorMessage("redeem_failed"),
          })
          return "failed"
        }

        setState({
          ...initialState(),
          checkGeneration: state.checkGeneration + 1,
          redeemGeneration: state.redeemGeneration + 1,
        })
        return "redeemed"
      } catch {
        if (generation !== state.redeemGeneration || !state.open) {
          return "noop"
        }
        setState({
          redeemBusy: false,
          checkError: staffRedeemErrorMessage("redeem_failed"),
        })
        return "failed"
      }
    },
    applyScannedCode: async (code) => {
      if (!state.open) {
        return
      }
      setState({
        code,
        checkError: null,
        step: "enter-code",
        confirmPreview: null,
      })
      await runCheck()
    },
  }
}
