import { useCallback, useEffect, useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { useParams } from "react-router-dom"

import {
  fetchScanLocationMetadata,
  getScanApiErrorMessage,
  submitGuestFeedback,
  type ScanLocationMetadata,
} from "@/api/scanApi"
import { GuestFeedbackForm } from "@/components/guest-feedback/GuestFeedbackForm"
import { GuestFeedbackLoading } from "@/components/guest-feedback/GuestFeedbackLoading"
import { GuestFeedbackNotFound } from "@/components/guest-feedback/GuestFeedbackNotFound"
import { GuestFeedbackShell } from "@/components/guest-feedback/GuestFeedbackShell"
import { GuestFeedbackSuccess } from "@/components/guest-feedback/GuestFeedbackSuccess"
import type { GuestFeedbackFormValues } from "@/schemas/guestFeedback"

type PagePhase = "loading" | "ready" | "not-found" | "success"

const fadeTransition = {
  duration: 0.28,
  ease: [0.25, 0.1, 0.25, 1] as const,
}

export default function GuestFeedbackPage() {
  const { token = "" } = useParams()
  const shouldReduceMotion = useReducedMotion()
  const [phase, setPhase] = useState<PagePhase>("loading")
  const [metadata, setMetadata] = useState<ScanLocationMetadata | null>(null)
  const [notFoundMessage, setNotFoundMessage] = useState<string | undefined>()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadMetadata() {
      setPhase("loading")
      setNotFoundMessage(undefined)

      if (!token.trim()) {
        setPhase("not-found")
        return
      }

      try {
        const result = await fetchScanLocationMetadata(token)
        if (cancelled) {
          return
        }

        setMetadata(result)
        setPhase("ready")
      } catch (error) {
        if (cancelled) {
          return
        }

        setNotFoundMessage(
          getScanApiErrorMessage(
            error,
            "This link was not found or is no longer active."
          )
        )
        setPhase("not-found")
      }
    }

    void loadMetadata()

    return () => {
      cancelled = true
    }
  }, [token])

  const handleSubmit = useCallback(
    async (values: GuestFeedbackFormValues) => {
      setIsSubmitting(true)
      setSubmitError(null)

      try {
        await submitGuestFeedback(token, values)
        setPhase("success")
      } catch (error) {
        setSubmitError(
          getScanApiErrorMessage(
            error,
            "We couldn't submit your feedback. Please try again."
          )
        )
      } finally {
        setIsSubmitting(false)
      }
    },
    [token]
  )

  const handleRetry = useCallback(() => {
    setSubmitError(null)
  }, [])

  return (
    <GuestFeedbackShell
        contentClassName={
          phase === "success"
            ? "justify-center pb-6 pt-[clamp(3rem,10vw,4rem)]"
            : undefined
        }
    >
      <AnimatePresence mode="wait">
        {phase === "loading" ? (
          <motion.div
            key="loading"
            initial={shouldReduceMotion ? false : { opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={shouldReduceMotion ? undefined : { opacity: 0 }}
            transition={shouldReduceMotion ? { duration: 0 } : fadeTransition}
            className="w-full"
          >
            <GuestFeedbackLoading />
          </motion.div>
        ) : null}

        {phase === "not-found" ? (
          <motion.div
            key="not-found"
            initial={shouldReduceMotion ? false : { opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            exit={shouldReduceMotion ? undefined : { opacity: 0, y: -8 }}
            transition={shouldReduceMotion ? { duration: 0 } : fadeTransition}
            className="w-full"
          >
            <GuestFeedbackNotFound message={notFoundMessage} />
          </motion.div>
        ) : null}

        {phase === "ready" && metadata ? (
          <motion.div
            key="form"
            initial={shouldReduceMotion ? false : { opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            exit={shouldReduceMotion ? undefined : { opacity: 0, y: -12 }}
            transition={shouldReduceMotion ? { duration: 0 } : fadeTransition}
            className="w-full"
          >
            <GuestFeedbackForm
              token={token}
              restaurantName={metadata.restaurantName}
              locationName={metadata.locationName}
              isSubmitting={isSubmitting}
              submitError={submitError}
              onSubmit={handleSubmit}
              onRetry={handleRetry}
            />
          </motion.div>
        ) : null}

        {phase === "success" ? (
          <motion.div
            key="success"
            initial={shouldReduceMotion ? false : { opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={shouldReduceMotion ? undefined : { opacity: 0 }}
            transition={shouldReduceMotion ? { duration: 0 } : fadeTransition}
            className="w-full"
          >
            <GuestFeedbackSuccess />
          </motion.div>
        ) : null}
      </AnimatePresence>
    </GuestFeedbackShell>
  )
}
