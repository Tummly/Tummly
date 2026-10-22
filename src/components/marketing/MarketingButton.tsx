import {
  Children,
  cloneElement,
  isValidElement,
  useEffect,
  useRef,
  useState,
  type ComponentPropsWithoutRef,
  type ReactElement,
  type ReactNode,
} from "react"
import { animate, motion, useReducedMotion } from "framer-motion"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

/**
 * Figma Button hover (`5661:14507` / Primary `4128:13978` → `4128:13980`,
 * Secondary `4129:14593` → `4129:14592`): SMART_ANIMATE, ease-out, 300ms.
 * Width stays at the hover size; label shifts left and the arrow fades in.
 */
const HOVER_MS = 0.3
const ARROW_W = 15

type MarketingButtonVariant =
  | "primary"
  | "dark"
  | "secondary"
  | "secondary-inverse"

type MarketingButtonDensity = "default" | "compact"

type MarketingButtonProps = Omit<
  ComponentPropsWithoutRef<"button">,
  "children"
> & {
  children: ReactNode
  variant?: MarketingButtonVariant
  asChild?: boolean
  /** Figma arrow reveal on hover. Default true. */
  showArrow?: boolean
  /** Tighter padding for dense chrome (e.g. mobile header). */
  density?: MarketingButtonDensity
}

const baseClass =
  "inline-flex h-auto min-h-0 cursor-pointer items-center justify-center rounded-[4px] border text-sm font-inter font-medium whitespace-nowrap no-underline shadow-none outline-none select-none focus-visible:ring-2 focus-visible:ring-[#141414]/30 disabled:pointer-events-none disabled:opacity-50"

const variantClass: Record<MarketingButtonVariant, string> = {
  primary: "border-transparent text-white",
  dark: "border-transparent text-white",
  secondary: "border-[#4e4e4e] text-[#141414]",
  "secondary-inverse": "border-[#4e4e4e] text-white",
}

type SurfaceStyle = {
  backgroundColor: string
  paddingLeft: number
  paddingRight: number
  paddingTop: number
  paddingBottom: number
  gap: number
}

function restSurface(
  variant: MarketingButtonVariant,
  density: MarketingButtonDensity,
): SurfaceStyle {
  const shared =
    density === "compact"
      ? {
          paddingLeft: 10,
          paddingRight: 10,
          paddingTop: 8,
          paddingBottom: 8,
          gap: 4,
        }
      : {
          paddingLeft: 18,
          paddingRight: 18,
          paddingTop: 12,
          paddingBottom: 12,
          gap: 6,
        }
  switch (variant) {
    case "primary":
      return { ...shared, backgroundColor: "#14a74a" }
    case "dark":
      return { ...shared, backgroundColor: "#141414" }
    case "secondary":
      return { ...shared, backgroundColor: "rgba(241, 241, 241, 0)" }
    case "secondary-inverse":
      return { ...shared, backgroundColor: "rgba(255, 255, 255, 0)" }
  }
}

function hoverSurface(
  variant: MarketingButtonVariant,
  density: MarketingButtonDensity,
): SurfaceStyle {
  const shared =
    density === "compact"
      ? {
          paddingLeft: 8,
          paddingRight: 8,
          paddingTop: 8,
          paddingBottom: 8,
          gap: 2,
        }
      : {
          paddingLeft: 16,
          paddingRight: 16,
          paddingTop: 12,
          paddingBottom: 12,
          gap: 4,
        }
  switch (variant) {
    case "primary":
      return { ...shared, backgroundColor: "#28bb5e" }
    case "dark":
      return { ...shared, backgroundColor: "#2e2e2e" }
    case "secondary":
      return { ...shared, backgroundColor: "#f1f1f1" }
    case "secondary-inverse":
      return { ...shared, backgroundColor: "rgba(255, 255, 255, 0.08)" }
  }
}

function arrowStroke(variant: MarketingButtonVariant) {
  return variant === "primary" ||
    variant === "dark" ||
    variant === "secondary-inverse"
    ? "#fafafa"
    : "#141414"
}

function MarketingButtonArrow({
  variant,
  hovered,
  reduceMotion,
}: {
  variant: MarketingButtonVariant
  hovered: boolean
  reduceMotion: boolean | null
}) {
  const stroke = arrowStroke(variant)

  return (
    <motion.span
      aria-hidden
      initial={false}
      animate={{
        opacity: hovered ? 1 : 0,
      }}
      transition={{
        duration: reduceMotion ? 0 : HOVER_MS,
        ease: "easeOut",
      }}
      className="relative block h-2.5 w-3.75 shrink-0"
      style={{ width: ARROW_W }}
    >
      <svg
        width={ARROW_W}
        height={10}
        viewBox="0 0 15 10"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className="absolute top-0 left-0 block size-auto h-2.5 w-3.75"
      >
        <path
          d="M9.12598 0.916677L13.2093 5.00001L9.12598 9.08334"
          stroke={stroke}
          strokeWidth="1.16667"
        />
        <path
          d="M12.2086 5L1.51855 5"
          stroke={stroke}
          strokeWidth="1.16667"
        />
      </svg>
    </motion.span>
  )
}

/**
 * Marketing CTA button — Figma Primary / Secondary hover smart-animate.
 * Prefer this over raw `Button` on marketing pages.
 */
export function MarketingButton({
  children,
  className,
  variant = "primary",
  asChild = false,
  showArrow = true,
  density = "default",
  type = "button",
  disabled,
  onMouseEnter,
  onMouseLeave,
  ...props
}: MarketingButtonProps) {
  const reduceMotion = useReducedMotion()
  const [hovered, setHovered] = useState(false)
  const surfaceRef = useRef<HTMLElement | null>(null)
  const rest = restSurface(variant, density)
  const hover = hoverSurface(variant, density)
  const active = !disabled && hovered
  /** Hover layout locks width; rest only changes fill color + label/arrow motion. */
  const layout = showArrow ? hover : rest
  const labelShift = showArrow ? (ARROW_W + hover.gap) / 2 : 0

  useEffect(() => {
    const el = surfaceRef.current
    if (el == null) return

    const to = active ? hover : rest
    const controls = animate(
      el,
      showArrow
        ? { backgroundColor: to.backgroundColor }
        : {
            backgroundColor: to.backgroundColor,
            paddingLeft: to.paddingLeft,
            paddingRight: to.paddingRight,
            paddingTop: to.paddingTop,
            paddingBottom: to.paddingBottom,
            gap: to.gap,
          },
      {
        duration: reduceMotion ? 0 : HOVER_MS,
        ease: "easeOut",
      },
    )

    return () => {
      controls.stop()
    }
  }, [active, hover, rest, reduceMotion, showArrow])

  const composedClass = cn(baseClass, variantClass[variant], className)
  const surfaceStyle = showArrow
    ? {
        ...layout,
        backgroundColor: rest.backgroundColor,
      }
    : rest

  const setHover = (next: boolean) => {
    if (disabled) return
    setHovered(next)
  }

  const arrow = showArrow ? (
    <MarketingButtonArrow
      variant={variant}
      hovered={active}
      reduceMotion={reduceMotion}
    />
  ) : null

  const inner = (label: ReactNode) =>
    showArrow ? (
      <motion.span
        className="inline-flex items-center"
        style={{ gap: hover.gap }}
        initial={false}
        animate={{ x: active ? 0 : labelShift }}
        transition={{
          duration: reduceMotion ? 0 : HOVER_MS,
          ease: "easeOut",
        }}
      >
        {label}
        {arrow}
      </motion.span>
    ) : (
      <>
        {label}
        {arrow}
      </>
    )

  if (asChild) {
    const child = Children.only(children)
    if (!isValidElement(child)) {
      return null
    }

    const element = child as ReactElement<{
      children?: ReactNode
      className?: string
    }>

    return (
      <Slot.Root
        ref={surfaceRef}
        data-slot="marketing-button"
        data-variant={variant}
        className={cn(composedClass, element.props.className)}
        style={surfaceStyle}
        onMouseEnter={(event) => {
          setHover(true)
          onMouseEnter?.(event as never)
        }}
        onMouseLeave={(event) => {
          setHover(false)
          onMouseLeave?.(event as never)
        }}
        {...props}
      >
        {cloneElement(element, {
          className: undefined,
          children: inner(element.props.children),
        })}
      </Slot.Root>
    )
  }

  return (
    <button
      ref={surfaceRef as React.RefObject<HTMLButtonElement>}
      data-slot="marketing-button"
      data-variant={variant}
      type={type}
      disabled={disabled}
      className={composedClass}
      style={surfaceStyle}
      onMouseEnter={(event) => {
        setHover(true)
        onMouseEnter?.(event)
      }}
      onMouseLeave={(event) => {
        setHover(false)
        onMouseLeave?.(event)
      }}
      {...props}
    >
      {inner(children)}
    </button>
  )
}

export type { MarketingButtonProps, MarketingButtonVariant }
