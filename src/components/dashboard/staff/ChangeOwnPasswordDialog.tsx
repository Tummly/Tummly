import { useState } from "react"
import { toast } from "sonner"

import { changeOwnStaffPassword, StaffApiError } from "@/api/adminStaffApi"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import {
  staffErrorMessage,
  validateStaffPassword,
} from "@/lib/admin/adminStaff"

type ChangeOwnPasswordDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

type PasswordFields = {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

const EMPTY_FIELDS: PasswordFields = {
  currentPassword: "",
  newPassword: "",
  confirmPassword: "",
}

/**
 * Staff change their own password. The server repeats every rule and also
 * checks the new password against the account email, so the client rule
 * here is only a fast preview.
 */
export function ChangeOwnPasswordDialog({
  open,
  onOpenChange,
}: ChangeOwnPasswordDialogProps) {
  const [fields, setFields] = useState<PasswordFields>(EMPTY_FIELDS)
  const [errors, setErrors] = useState<Partial<Record<keyof PasswordFields, string>>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  function handleOpenChange(next: boolean) {
    if (!next) {
      setFields(EMPTY_FIELDS)
      setErrors({})
      setFormError(null)
    }
    onOpenChange(next)
  }

  function setField(key: keyof PasswordFields, value: string) {
    setFields((current) => ({ ...current, [key]: value }))
  }

  function validate(): Partial<Record<keyof PasswordFields, string>> {
    const next: Partial<Record<keyof PasswordFields, string>> = {}
    if (fields.currentPassword === "") {
      next.currentPassword = "Enter your current password."
    }
    const passwordError = validateStaffPassword(fields.newPassword)
    if (passwordError) {
      next.newPassword = passwordError
    } else if (fields.newPassword === fields.currentPassword) {
      next.newPassword = "Choose a password that is different from the current one."
    }
    if (fields.confirmPassword !== fields.newPassword) {
      next.confirmPassword = "The new passwords do not match."
    }
    return next
  }

  async function submit() {
    const next = validate()
    setErrors(next)
    setFormError(null)
    if (Object.keys(next).length > 0) return

    setBusy(true)
    try {
      await changeOwnStaffPassword({
        currentPassword: fields.currentPassword,
        newPassword: fields.newPassword,
      })
      toast.success("Password changed.")
      handleOpenChange(false)
    } catch (error) {
      const apiError = error instanceof StaffApiError ? error : null
      const message = staffErrorMessage(
        apiError?.code ?? null,
        apiError?.status ?? null,
        "Could not change the password."
      )
      if (apiError?.code === "current_password_invalid") {
        setErrors({ currentPassword: message })
      } else {
        setFormError(message)
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="gap-6 bg-white sm:max-w-[440px]">
        <DialogHeader className="gap-2">
          <DialogTitle>Change password</DialogTitle>
          <DialogDescription>
            Enter your current password, then choose a new one of at least 12
            characters.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <FloatingLabelInput
            label="Current password"
            type="password"
            autoComplete="current-password"
            value={fields.currentPassword}
            onChange={(event) => setField("currentPassword", event.target.value)}
            error={errors.currentPassword}
          />
          <FloatingLabelInput
            label="New password"
            type="password"
            autoComplete="new-password"
            value={fields.newPassword}
            onChange={(event) => setField("newPassword", event.target.value)}
            error={errors.newPassword}
          />
          <FloatingLabelInput
            label="Confirm new password"
            type="password"
            autoComplete="new-password"
            value={fields.confirmPassword}
            onChange={(event) => setField("confirmPassword", event.target.value)}
            error={errors.confirmPassword}
          />
        </div>

        {formError !== null ? (
          <p role="alert" className="text-sm text-destructive">
            {formError}
          </p>
        ) : null}

        <DialogFooter>
          <Button type="button" variant="ghost" onClick={() => handleOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" disabled={busy} onClick={() => void submit()}>
            {busy ? "Saving…" : "Change password"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
