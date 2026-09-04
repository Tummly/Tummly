import type { CSSProperties } from "react"
import { Toaster as Sonner, type ToasterProps } from "sonner"
import { Loader2Icon, XIcon } from "lucide-react"

/**
 * Toast chrome — Figma "Success message" (3462:60099).
 * Operator surfaces (`html.op`) resolve `--op-toast-*`; layout vars fall back
 * outside Operator so Admin keeps usable padding/radius.
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
        close: (
          <XIcon
            className="size-[var(--op-toast-close-size,18px)]"
            strokeWidth={1.75}
          />
        ),
      }}
      style={
        {
          "--border-radius": "var(--op-toast-radius, 10px)",
          "--toast-close-button-start": "auto",
          "--toast-close-button-end": "var(--op-toast-padding-x, 18px)",
          "--toast-close-button-transform": "translateY(-50%)",
          zIndex: 9999,
        } as CSSProperties
      }
      toastOptions={{
        classNames: {
          toast:
            "cn-toast group gap-3 border p-[var(--op-toast-padding-y,14px)_var(--op-toast-padding-x,18px)] text-[length:var(--op-toast-font-size,16px)] font-normal leading-[var(--op-toast-line-height,20px)] shadow-none",
          title:
            "text-[length:var(--op-toast-font-size,16px)] font-normal leading-[var(--op-toast-line-height,20px)]",
          description: "text-sm font-normal leading-5 opacity-90",
          icon: "hidden group-data-[type=loading]:flex",
          closeButton:
            "!absolute !top-1/2 !right-[var(--op-toast-padding-x,18px)] !left-auto size-[var(--op-toast-close-size,18px)] border-0 bg-transparent p-0 shadow-none hover:bg-transparent hover:opacity-80",
        },
      }}
      {...props}
    />
  )
}

export { Toaster }
