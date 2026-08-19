import { useEffect, useRef, useState, type ReactNode, type RefObject } from "react"

import { GuestFeedbackForm } from "@/components/guest-feedback/GuestFeedbackForm"
import { GuestFeedbackShell } from "@/components/guest-feedback/GuestFeedbackShell"
import {
  OPERATOR_HOME_HERO_PHONE_CANVAS_CLASS,
  OPERATOR_HOME_HERO_PHONE_CANVAS_WIDTH,
  OPERATOR_HOME_HERO_PHONE_CLASS,
  OPERATOR_HOME_HERO_PHONE_FADE_CLASS,
  OPERATOR_HOME_HERO_PHONE_GUEST_CONTENT_CLASS,
  OPERATOR_HOME_HERO_PHONE_GUEST_SHELL_CLASS,
  OPERATOR_HOME_HERO_PHONE_SCREEN_CLASS,
  OPERATOR_HOME_HERO_PHONE_SHELL_CLASS,
} from "@/lib/operatorHome/heroPresentation"

const LG_VIEWPORT_QUERY = "(min-width: 1024px)"

type HomeGuestFormPhoneProps = {
  locationName: string
  address: string
}

function readLargeViewport(): boolean {
  return typeof window !== "undefined"
    && window.matchMedia(LG_VIEWPORT_QUERY).matches
}

function useLargeViewport(): boolean {
  const [isLargeViewport, setIsLargeViewport] = useState(readLargeViewport)

  useEffect(() => {
    const media = window.matchMedia(LG_VIEWPORT_QUERY)
    const sync = () => setIsLargeViewport(media.matches)

    sync()
    media.addEventListener("change", sync)
    return () => media.removeEventListener("change", sync)
  }, [])

  return isLargeViewport
}

function useElementWidth(ref: RefObject<HTMLDivElement | null>): number {
  const [width, setWidth] = useState(0)

  useEffect(() => {
    const node = ref.current
    if (node == null) {
      return
    }

    const measure = () => setWidth(node.getBoundingClientRect().width)
    measure()

    if (typeof ResizeObserver === "undefined") {
      window.addEventListener("resize", measure)
      return () => window.removeEventListener("resize", measure)
    }

    const observer = new ResizeObserver((entries) => {
      setWidth(entries[0]?.contentRect.width ?? 0)
    })
    observer.observe(node)
    return () => observer.disconnect()
  }, [ref])

  return width
}

function HomePhoneShell({
  screenRef,
  children,
}: {
  screenRef: RefObject<HTMLDivElement | null>
  children: ReactNode
}) {
  return (
    <div className={OPERATOR_HOME_HERO_PHONE_CLASS}>
      <div className={OPERATOR_HOME_HERO_PHONE_SHELL_CLASS}>
        <div ref={screenRef} className={OPERATOR_HOME_HERO_PHONE_SCREEN_CLASS}>
          {children}
        </div>
      </div>
    </div>
  )
}

function FixedGuestFormCanvas({
  locationName,
  address,
  scale,
}: HomeGuestFormPhoneProps & { scale: number }) {
  return (
    <div
      inert
      className={OPERATOR_HOME_HERO_PHONE_CANVAS_CLASS}
      style={{ transform: `scale(${scale})` }}
    >
      <GuestFeedbackShell
        className={OPERATOR_HOME_HERO_PHONE_GUEST_SHELL_CLASS}
        contentClassName={OPERATOR_HOME_HERO_PHONE_GUEST_CONTENT_CLASS}
      >
        <GuestFeedbackForm
          token=""
          locationName={locationName}
          address={address}
          isSubmitting={false}
          submitError={null}
          onSubmit={async () => {}}
          onRetry={() => {}}
        />
      </GuestFeedbackShell>
    </div>
  )
}

function HomeGuestFormPhoneComposition({
  locationName,
  address,
}: HomeGuestFormPhoneProps) {
  const screenRef = useRef<HTMLDivElement>(null)
  const screenWidth = useElementWidth(screenRef)

  return (
    <>
      <HomePhoneShell screenRef={screenRef}>
        {screenWidth > 0 ? (
          <FixedGuestFormCanvas
            locationName={locationName}
            address={address}
            scale={screenWidth / OPERATOR_HOME_HERO_PHONE_CANVAS_WIDTH}
          />
        ) : null}
      </HomePhoneShell>
      <div className={OPERATOR_HOME_HERO_PHONE_FADE_CLASS} aria-hidden />
    </>
  )
}

/** Decorative, location-aware guest form shown in the Operator Home hero. */
export function HomeGuestFormPhone(props: HomeGuestFormPhoneProps) {
  const isLargeViewport = useLargeViewport()

  return isLargeViewport ? <HomeGuestFormPhoneComposition {...props} /> : null
}
