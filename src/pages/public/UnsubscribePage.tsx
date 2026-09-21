import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react"
import { useSearchParams } from "react-router-dom"

import {
  confirmUnsubscribe,
  fetchUnsubscribePreview,
  submitUnsubscribeForm,
} from "@/api/publicUnsubscribeApi"
import Footer from "@/components/home/Footer"
import { Button } from "@/components/ui/button"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import {
  UNSUBSCRIBE_INVALID_LINK_MESSAGE,
  UNSUBSCRIBE_MISSING_RESTAURANT_MESSAGE,
  UNSUBSCRIBE_PREVIEW_NETWORK_ERROR_MESSAGE,
  UNSUBSCRIBE_SUCCESS_MESSAGE,
  resolveUnsubscribePageMode,
} from "@/lib/publicUnsubscribe/unsubscribePresentation"

type TokenPhase = "loading" | "ready" | "invalid" | "error" | "success"
type FormPhase = "ready" | "success"

function isValidEmail(value: string): boolean {
  const trimmed = value.trim()
  if (trimmed.length === 0) {
    return false
  }
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)
}

export default function UnsubscribePage() {
  const [searchParams] = useSearchParams()
  const mode = useMemo(
    () => resolveUnsubscribePageMode(searchParams),
    [searchParams]
  )

  return (
    <>
      <main className="w-full bg-white text-[#141414]">
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-8 px-4 py-10 sm:px-6 sm:py-12 md:px-10 lg:px-16 lg:py-16">
          <header className="flex flex-col gap-3">
            <h1 className="m-0 text-[clamp(2rem,5vw,2.875rem)] font-bold leading-normal">
              Unsubscribe
            </h1>
            <p className="m-0 text-base font-medium leading-6 sm:text-lg">
              Stop marketing emails from this restaurant.
            </p>
          </header>

          {mode.kind === "token" ? (
            <TokenUnsubscribeFlow token={mode.token} />
          ) : null}

          {mode.kind === "form" ? (
            <FormUnsubscribeFlow restaurantId={mode.restaurantId} />
          ) : null}

          {mode.kind === "missing-restaurant" ? (
            <p className="m-0 text-base leading-6">
              {UNSUBSCRIBE_MISSING_RESTAURANT_MESSAGE}
            </p>
          ) : null}
        </div>
      </main>
      <Footer />
    </>
  )
}

function TokenUnsubscribeFlow({ token }: { token: string }) {
  const [phase, setPhase] = useState<TokenPhase>("loading")
  const [restaurantName, setRestaurantName] = useState("")
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [previewAttempt, setPreviewAttempt] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function loadPreview() {
      setPhase("loading")
      setSubmitError(null)

      try {
        const preview = await fetchUnsubscribePreview(token)
        if (cancelled) {
          return
        }

        if (!preview.valid) {
          setPhase("invalid")
          return
        }

        setRestaurantName(preview.restaurantName)
        setPhase("ready")
      } catch {
        if (cancelled) {
          return
        }
        setPhase("error")
      }
    }

    void loadPreview()

    return () => {
      cancelled = true
    }
  }, [token, previewAttempt])

  const handleConfirm = useCallback(async () => {
    setIsSubmitting(true)
    setSubmitError(null)

    try {
      await confirmUnsubscribe(token)
      setPhase("success")
    } catch {
      setSubmitError("We couldn't complete your unsubscribe request. Please try again.")
    } finally {
      setIsSubmitting(false)
    }
  }, [token])

  if (phase === "loading") {
    return <p className="m-0 text-base leading-6">Loading…</p>
  }

  if (phase === "invalid") {
    return (
      <p className="m-0 text-base leading-6" role="status">
        {UNSUBSCRIBE_INVALID_LINK_MESSAGE}
      </p>
    )
  }

  if (phase === "error") {
    return (
      <div className="flex max-w-md flex-col gap-4">
        <p className="m-0 text-base leading-6" role="alert">
          {UNSUBSCRIBE_PREVIEW_NETWORK_ERROR_MESSAGE}
        </p>
        <Button
          type="button"
          size="responsive"
          onClick={() => {
            setPreviewAttempt((current) => current + 1)
          }}
        >
          Try again
        </Button>
      </div>
    )
  }

  if (phase === "success") {
    return (
      <p className="m-0 text-base leading-6" role="status">
        {UNSUBSCRIBE_SUCCESS_MESSAGE}
      </p>
    )
  }

  return (
    <div className="flex max-w-md flex-col gap-6">
      <p className="m-0 text-base leading-6">
        Confirm you want to unsubscribe from marketing emails for{" "}
        <span className="font-semibold">{restaurantName}</span>.
      </p>
      <Button
        type="button"
        size="responsive"
        disabled={isSubmitting}
        onClick={() => {
          void handleConfirm()
        }}
      >
        {isSubmitting ? "Confirming…" : "Confirm unsubscribe"}
      </Button>
      {submitError ? (
        <p role="alert" className="m-0 text-sm text-destructive">
          {submitError}
        </p>
      ) : null}
    </div>
  )
}

function FormUnsubscribeFlow({ restaurantId }: { restaurantId: number }) {
  const [phase, setPhase] = useState<FormPhase>("ready")
  const [email, setEmail] = useState("")
  const [emailError, setEmailError] = useState<string | undefined>()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      setSubmitError(null)

      if (!isValidEmail(email)) {
        setEmailError("Enter a valid email address.")
        return
      }

      setEmailError(undefined)
      setIsSubmitting(true)

      try {
        await submitUnsubscribeForm(email.trim(), restaurantId)
        setPhase("success")
      } catch {
        setSubmitError(
          "We couldn't complete your unsubscribe request. Please try again."
        )
      } finally {
        setIsSubmitting(false)
      }
    },
    [email, restaurantId]
  )

  if (phase === "success") {
    return (
      <p className="m-0 text-base leading-6" role="status">
        {UNSUBSCRIBE_SUCCESS_MESSAGE}
      </p>
    )
  }

  return (
    <form
      className="flex max-w-md flex-col gap-4"
      onSubmit={(event) => {
        void handleSubmit(event)
      }}
      noValidate
    >
      <p className="m-0 text-base leading-6">
        Enter the email address used for this restaurant&apos;s marketing
        emails.
      </p>
      <FloatingLabelInput
        label="Email"
        type="email"
        autoComplete="email"
        value={email}
        error={emailError}
        onChange={(event) => {
          setEmail(event.target.value)
          if (emailError) {
            setEmailError(undefined)
          }
        }}
      />
      <Button type="submit" size="responsive" disabled={isSubmitting}>
        {isSubmitting ? "Submitting…" : "Unsubscribe"}
      </Button>
      {submitError ? (
        <p role="alert" className="m-0 text-sm text-destructive">
          {submitError}
        </p>
      ) : null}
    </form>
  )
}
