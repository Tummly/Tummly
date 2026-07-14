import type { CSSProperties } from "react"
import { Toaster as Sonner, type ToasterProps } from "sonner"
import {
  CircleCheckIcon,
  InfoIcon,
  Loader2Icon,
  OctagonXIcon,
  TriangleAlertIcon,
} from "lucide-react"

const Toaster = ({ ...props }: ToasterProps) => {
  return (
    <Sonner
      theme="light"
      className="toaster group"
      icons={{
        success: <CircleCheckIcon className="size-4" />,
        info: <InfoIcon className="size-4" />,
        warning: <TriangleAlertIcon className="size-4" />,
        error: <OctagonXIcon className="size-4" />,
        loading: <Loader2Icon className="size-4 animate-spin" />,
      }}
      style={
        {
          "--normal-bg": "var(--popover)",
          "--normal-text": "var(--popover-foreground)",
          "--normal-border": "var(--border)",
          "--border-radius": "var(--radius)",
          // Inside top-right (Sonner defaults hang the close control outside top-left).
          "--toast-close-button-start": "auto",
          "--toast-close-button-end": "10px",
          "--toast-close-button-transform": "translateY(10px)",
        } as CSSProperties
      }
      toastOptions={{
        classNames: {
          toast: "cn-toast pr-8",
          closeButton: "!left-auto !right-2.5 !top-2.5 !transform-none",
        },
      }}
      {...props}
    />
  )
}

export { Toaster }
