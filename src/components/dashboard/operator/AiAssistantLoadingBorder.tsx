import type { ReactNode } from "react"
import { motion, useReducedMotion } from "framer-motion"

import { cn } from "@/lib/utils"

const LOADING_BORDER_GRADIENT =
  "linear-gradient(90deg, var(--op-color-blue-400), var(--op-color-green-500), var(--op-color-blue-400))"

type AiAssistantLoadingBorderProps = {
  loading: boolean
  className?: string
  /** Fill / radius for the inner surface (clips over the gradient ring). */
  contentClassName?: string
  children: ReactNode
}

/**
 * Idle: passes className through to a single wrapper.
 * Loading: 1px moving linear blue → green ring (#4984F2 → #14A74A).
 */
export function AiAssistantLoadingBorder({
  loading,
  className,
  contentClassName,
  children,
}: AiAssistantLoadingBorderProps) {
  const shouldReduceMotion = useReducedMotion()

  if (!loading) {
    return <div className={cn(className, contentClassName)}>{children}</div>
  }

  return (
    <div
      className={cn("relative overflow-hidden p-px", className)}
      aria-busy="true"
    >
      <motion.div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          backgroundImage: LOADING_BORDER_GRADIENT,
          backgroundSize: "200% 100%",
          backgroundPosition: "0% 0%",
        }}
        animate={
          shouldReduceMotion
            ? undefined
            : { backgroundPosition: ["0% 0%", "200% 0%"] }
        }
        transition={
          shouldReduceMotion
            ? undefined
            : { duration: 1.6, repeat: Infinity, ease: "linear" }
        }
      />
      <div
        className={cn(
          "relative z-[1] h-full min-h-0 overflow-hidden",
          contentClassName
        )}
      >
        {children}
      </div>
    </div>
  )
}
