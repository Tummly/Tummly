import { useCallback, useEffect, useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { useNavigate, useParams, useSearchParams } from "react-router-dom"

import { createDemoSession } from "@/api/demoSessionApi"
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
import { DemoGuestThankYou } from "@/components/marketing/demo/DemoGuestThankYou"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import { executeGuestFeedbackRecaptcha } from "@/lib/guestFeedback/executeGuestFeedbackRecaptcha"
import { guestFeedbackMajorStateTransition } from "@/lib/guestFeedback/guestFeedbackMotionTokens"
import {
  startDemoTourSession,
  writeGuestSubmission,
} from "@/lib/productDemo/demoTourSession"
import { productDemoPath } from "@/lib/productDemo/productDemoPaths"
import {
  toIssuedGuestOfferCoupon,
  type GuestPreviewOfferCouponView,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import {
  completeUserSession,
  persistSelectedLocation,
} from "@/pages/utils/authHelpers"
import type { GuestFeedbackFormValues } from "@/schemas/guestFeedback"

type PagePhase =
  | "loading"
  | "ready"
  | "not-found"
  | "success"
  | "demo-thanks"

const fadeTransition = guestFeedbackMajorStateTransition

export default function GuestFeedbackPage() {
  const { token = "" } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const isProductDemo = searchParams.get("productDemo") === "1"
  const shouldReduceMotion = useReducedMotion()
  const [phase, setPhase] = useState<PagePhase>("loading")
  const [metadata, setMetadata] = useState<ScanLocationMetadata | null>(null)
  const [notFoundMessage, setNotFoundMessage] = useState<string | undefined>()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [issuedOffer, setIssuedOffer] =
    useState<GuestPreviewOfferCouponView | null>(null)
  const [demoTransitionError, setDemoTransitionError] = useState<string | null>(
    null
  )

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
        setIssuedOffer(null)
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

  const enterRestaurantFromGuest = useCallback(async () => {
    setDemoTransitionError(null)
    try {
      const session = await createDemoSession()
      completeUserSession(session)
      if (session.selectedLocationId != null) {
        persistSelectedLocation(session.selectedLocationId)
      }
      startDemoTourSession({
        entryPath: "guest",
        hasGuestSubmission: true,
        phase: "welcome",
      })
      navigate(productDemoPath("feedback"), { replace: true })
    } catch {
      setDemoTransitionError(
        "Unable to open the restaurant demo. Try again or start a Pilot."
      )
    }
  }, [navigate])

  const handleSubmit = useCallback(
    async (values: GuestFeedbackFormValues) => {
      setIsSubmitting(true)
      setSubmitError(null)

      try {
        if (isProductDemo) {
          // Session-bound only — do not write visitor Feedback into the shared seed.
          writeGuestSubmission({
            commentPreview: values.comment.trim().slice(0, 160),
            submittedAt: new Date().toISOString(),
          })
          setPhase("demo-thanks")
          return
        }

        const recaptchaToken = await executeGuestFeedbackRecaptcha()
        const result = await submitGuestFeedback(token, values, {
          recaptchaToken,
        })
        if (result.offer != null) {
          setIssuedOffer(toIssuedGuestOfferCoupon(result.offer))
          setPhase("success")
          return
        }

        setIssuedOffer(null)
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
    [token, isProductDemo]
  )

  const handleRetry = useCallback(() => {
    setSubmitError(null)
  }, [])

  const centeredSuccessChrome =
    phase === "success" ? "min-h-0 flex-1 justify-stretch pb-6 pt-6 sm:pt-8" : undefined

  const hideShellPoweredBy = phase === "ready" || phase === "success"

  const shellGlow = phase === "success" ? "accent" : "none"

  return (
    <GuestFeedbackShell
      contentClassName={centeredSuccessChrome}
      showPoweredBy={!hideShellPoweredBy}
      glow={shellGlow}
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
            {isProductDemo ? (
              <p className="font-sans mb-4 text-center text-xs font-medium tracking-[0.06em] text-[#7c7c7c] uppercase">
                Sample demo · No messages are sent
              </p>
            ) : null}
            <GuestFeedbackForm
              token={token}
              locationName={metadata.locationName}
              restaurantName={metadata.restaurantName}
              address={metadata.address}
              brandLogoPublicUrl={metadata.brandLogoPublicUrl}
              guestFormConsent={metadata.guestFormConsent}
              isSubmitting={isSubmitting}
              submitError={submitError}
              onSubmit={handleSubmit}
              onRetry={handleRetry}
            />
          </motion.div>
        ) : null}

        {phase === "demo-thanks" && metadata ? (
          <motion.div
            key="demo-thanks"
            initial={shouldReduceMotion ? false : { opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={shouldReduceMotion ? undefined : { opacity: 0 }}
            transition={shouldReduceMotion ? { duration: 0 } : fadeTransition}
            className="w-full"
          >
            <DemoGuestThankYou
              restaurantName={metadata.restaurantName}
              locationName={metadata.locationName}
              onSeeInTummly={() => {
                void enterRestaurantFromGuest()
              }}
              onStartPilot={() => {
                window.location.assign(PRODUCT_DEMO.exit.primaryTo)
              }}
            />
            {demoTransitionError ? (
              <p
                role="alert"
                className="font-sans mt-3 text-center text-sm text-[#b42318]"
              >
                {demoTransitionError}
              </p>
            ) : null}
          </motion.div>
        ) : null}

        {phase === "success" && metadata ? (
          <motion.div
            key="success"
            initial={shouldReduceMotion ? false : { opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={shouldReduceMotion ? undefined : { opacity: 0 }}
            transition={shouldReduceMotion ? { duration: 0 } : fadeTransition}
            className="flex min-h-full w-full flex-1 flex-col"
          >
            <GuestFeedbackSuccess
              restaurantName={metadata.restaurantName}
              locationName={metadata.locationName}
              brandLogoPublicUrl={metadata.brandLogoPublicUrl}
              offer={issuedOffer}
            />
          </motion.div>
        ) : null}
      </AnimatePresence>
    </GuestFeedbackShell>
  )
}
