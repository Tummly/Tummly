import { useEffect, type ReactNode } from "react"
import { createPortal } from "react-dom"
import { MonitorIcon, SmartphoneIcon } from "lucide-react"
import { RemoveScroll } from "react-remove-scroll"

import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import {
  CAPTURE_GUEST_PREVIEW_CANVAS_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE,
  CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_COPY_CLASS,
  CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

export type OperatorGuestPreviewDevice =
  (typeof CAPTURE_GUEST_PREVIEW_DEVICE)[keyof typeof CAPTURE_GUEST_PREVIEW_DEVICE]

export type OperatorGuestPreviewShellProps = {
  open: boolean
  onClose: () => void
  titleId: string
  lead: ReactNode
  meta?: ReactNode
  headerActions: ReactNode
  toolbarLeading?: ReactNode
  device: OperatorGuestPreviewDevice
  onDeviceChange: (device: OperatorGuestPreviewDevice) => void
  desktopLabel: string
  mobileLabel: string
  overlayClassName: string
  bodyClassName: string
  children: ReactNode
  /** Feedback/Campaigns portal above Operator wizard Dialog. */
  portaled?: boolean
  /** Nested scroll lock so wheel/touch still works inside a parent Dialog. */
  removeScroll?: boolean
  /** Capture Escape before a parent Dialog closes itself. */
  trapEscape?: boolean
}

/**
 * Shared Operator Guest preview chrome — header, device toggle, canvas.
 * Capture and Feedback/Campaigns supply their own lead/meta/actions/canvas.
 */
export function OperatorGuestPreviewShell({
  open,
  onClose,
  titleId,
  lead,
  meta,
  headerActions,
  toolbarLeading,
  device,
  onDeviceChange,
  desktopLabel,
  mobileLabel,
  overlayClassName,
  bodyClassName,
  children,
  portaled = false,
  removeScroll = false,
  trapEscape = false,
}: OperatorGuestPreviewShellProps) {
  useEffect(() => {
    if (!open) {
      return
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return
      }
      if (trapEscape) {
        event.preventDefault()
        event.stopImmediatePropagation()
      }
      onClose()
    }

    window.addEventListener("keydown", onKeyDown, trapEscape)
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = "hidden"

    return () => {
      window.removeEventListener("keydown", onKeyDown, trapEscape)
      document.body.style.overflow = previousOverflow
    }
  }, [open, onClose, trapEscape])

  if (!open) {
    return null
  }

  const overlay = (
    <div
      className={overlayClassName}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
    >
      <header className={CAPTURE_GUEST_PREVIEW_HEADER_CLASS}>
        <div className={CAPTURE_GUEST_PREVIEW_HEADER_COPY_CLASS}>
          {lead}
          {meta}
        </div>
        {headerActions}
      </header>

      <div className={bodyClassName}>
        <div className={CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS}>
          {toolbarLeading ?? <div className="flex-1" />}
          <ToggleGroup
            type="single"
            value={device}
            onValueChange={(value) => {
              if (
                value === CAPTURE_GUEST_PREVIEW_DEVICE.desktop
                || value === CAPTURE_GUEST_PREVIEW_DEVICE.mobile
              ) {
                onDeviceChange(value)
              }
            }}
            variant="default"
            spacing={0}
            className={CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS}
            aria-label="Preview device"
          >
            <ToggleGroupItem
              value={CAPTURE_GUEST_PREVIEW_DEVICE.desktop}
              className={CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS}
            >
              <MonitorIcon data-icon="inline-start" aria-hidden />
              {desktopLabel}
            </ToggleGroupItem>
            <ToggleGroupItem
              value={CAPTURE_GUEST_PREVIEW_DEVICE.mobile}
              className={CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS}
            >
              <SmartphoneIcon data-icon="inline-start" aria-hidden />
              {mobileLabel}
            </ToggleGroupItem>
          </ToggleGroup>
          {toolbarLeading == null ? <div className="flex-1" /> : null}
        </div>

        <div className={CAPTURE_GUEST_PREVIEW_CANVAS_CLASS}>{children}</div>
      </div>
    </div>
  )

  if (!portaled) {
    return overlay
  }

  return createPortal(
    removeScroll ? (
      <RemoveScroll enabled removeScrollBar={false} forwardProps>
        {overlay}
      </RemoveScroll>
    ) : (
      overlay
    ),
    document.body
  )
}
