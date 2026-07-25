import type { CSSProperties } from "react"
import { Toaster as Sonner, type ToasterProps } from "sonner"
import { Loader2Icon, XIcon } from "lucide-react"

/**
 * Toast chrome — Figma "Success message" (3462:60100).
 * Light/dark surfaces use operator card tokens; success/error tints
 * match Badge `ready`/`positive` and `error`/`negative`.
 */
const Toaster = ({ ...props }: ToasterProps) => {
  return (
    <Sonner
      theme="light"
      className="toaster group"
      icons={{
        // Figma toast is message + close only (no status glyph).
        success: null,
        info: null,
        warning: null,
        error: null,
        loading: <Loader2Icon className="size-4 animate-spin" />,
        close: <XIcon className="size-[18px]" strokeWidth={1.75} />,
      }}
      style={
        {
          "--border-radius": "10px",
          // Inside right edge (Sonner defaults hang the close outside top-left).
          "--toast-close-button-start": "auto",
          "--toast-close-button-end": "18px",
          "--toast-close-button-transform": "translateY(-50%)",
        } as CSSProperties
      }
      toastOptions={{
        classNames: {
          toast:
            "cn-toast group gap-3 border px-[18px] py-[14px] text-base font-normal leading-5 shadow-none",
          title: "text-base font-normal leading-5",
          description: "text-sm font-normal leading-5 opacity-90",
          icon: "hidden group-data-[type=loading]:flex",
          closeButton:
            "!left-auto !right-[18px] !top-1/2 size-[18px] !-translate-y-1/2 border-0 bg-transparent p-0 shadow-none hover:bg-transparent hover:opacity-80",
        },
      }}
      {...props}
    />
  )
}

export { Toaster }
