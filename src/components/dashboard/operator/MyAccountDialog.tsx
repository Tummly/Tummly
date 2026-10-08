import { useEffect, useState } from "react"
import { isAxiosError } from "axios"
import { XIcon } from "lucide-react"

import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"

import {
  changeMyAccountPassword,
  getMyAccount,
  updateMyAccountProfile,
  type MyAccountSnapshot,
} from "@/api/accountApi"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Spinner } from "@/components/ui/spinner"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  formatPhoneForDisplay,
  tryNormalizePhoneToE164,
} from "@/lib/phoneNumber"
import { cn } from "@/lib/utils"

type MyAccountTab = "profile" | "security" | "access"

type MyAccountDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onProfileSaved?: (profile: {
    fullName: string
    jobTitle: string | null
  }) => void
}

/** Figma 6583:34681 — Main Bg white / #171717 (`--op-surface-primary`). */
const DIALOG_CONTENT_CLASS =
  "max-h-[min(90dvh,840px)] gap-5 overflow-y-auto border-0 bg-op-surface-primary p-8 text-op-text-primary shadow-lg sm:max-w-[720px]"

const FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

/**
 * Operator input chrome — light `#b2b2b3` / dark `rgba(74,74,76,0.8)` via
 * `--op-input-border`; placeholder via `--op-input-placeholder`.
 */
const FIELD_INPUT_CLASS =
  "h-auto min-h-12 rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder disabled:cursor-not-allowed disabled:bg-[var(--op-input-disabled-background)] disabled:opacity-100 dark:bg-transparent dark:disabled:bg-[var(--op-input-disabled-background)]"

/** Main Bg / Subtitle `#7c7c7c` — same token in light and dark. */
const MUTED_TEXT_CLASS = "text-[var(--op-color-gray-550)]"

function emptySnapshot(): MyAccountSnapshot {
  return {
    fullName: "",
    email: "",
    jobTitle: null,
    phoneNumber: "",
    role: "",
    organisation: "",
    locationAccess: [],
    twoFactorEnabled: false,
  }
}

export function MyAccountDialog({
  open,
  onOpenChange,
  onProfileSaved,
}: MyAccountDialogProps) {
  const [tab, setTab] = useState<MyAccountTab>("profile")
  const [loadStatus, setLoadStatus] = useState<"idle" | "loading" | "error">(
    "idle"
  )
  const [loadError, setLoadError] = useState<string | null>(null)
  const [snapshot, setSnapshot] = useState<MyAccountSnapshot>(emptySnapshot)

  const [fullName, setFullName] = useState("")
  const [jobTitle, setJobTitle] = useState("")
  const [phoneNumber, setPhoneNumber] = useState("")
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileSaving, setProfileSaving] = useState(false)

  const [currentPassword, setCurrentPassword] = useState("")
  const [newPassword, setNewPassword] = useState("")
  const [confirmNewPassword, setConfirmNewPassword] = useState("")
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null)
  const [passwordSaving, setPasswordSaving] = useState(false)

  useEffect(() => {
    if (!open) {
      return
    }

    let cancelled = false
    setTab("profile")
    setLoadStatus("loading")
    setLoadError(null)
    setProfileError(null)
    setPasswordError(null)
    setPasswordSuccess(null)
    setCurrentPassword("")
    setNewPassword("")
    setConfirmNewPassword("")

    void getMyAccount()
      .then((data) => {
        if (cancelled) {
          return
        }
        setSnapshot(data)
        setFullName(data.fullName)
        setJobTitle(data.jobTitle ?? "")
        setPhoneNumber(
          data.phoneNumber.trim().length > 0
            ? formatPhoneForDisplay(data.phoneNumber)
            : ""
        )
        setLoadStatus("idle")
      })
      .catch((error: unknown) => {
        if (cancelled) {
          return
        }
        const message = isAxiosError<{ message?: string }>(error)
          ? error.response?.data?.message
          : null
        setLoadError(
          typeof message === "string" && message.trim().length > 0
            ? message
            : "Unable to load account details."
        )
        setLoadStatus("error")
      })

    return () => {
      cancelled = true
    }
  }, [open])

  const handleClose = () => {
    onOpenChange(false)
  }

  const handleSaveProfile = async () => {
    setProfileError(null)
    const trimmedName = fullName.trim()
    if (trimmedName.length === 0) {
      setProfileError("Full name is required.")
      return
    }

    const trimmedPhone = phoneNumber.trim()
    let phoneE164: string | null = null
    if (trimmedPhone.length > 0) {
      phoneE164 = tryNormalizePhoneToE164(trimmedPhone)
      if (phoneE164 == null) {
        setProfileError("Please enter a valid UK phone number.")
        return
      }
    }

    const nextJobTitle =
      jobTitle.trim().length > 0 ? jobTitle.trim() : null

    // Eager shell update so the account trigger shows the new Job title
    // before the PATCH round-trip finishes.
    onProfileSaved?.({
      fullName: trimmedName,
      jobTitle: nextJobTitle,
    })

    setProfileSaving(true)
    try {
      const updated = await updateMyAccountProfile({
        fullName: trimmedName,
        jobTitle: nextJobTitle,
        phoneNumber: phoneE164,
      })
      setSnapshot(updated)
      setFullName(updated.fullName)
      setJobTitle(updated.jobTitle ?? "")
      setPhoneNumber(
        updated.phoneNumber.trim().length > 0
          ? formatPhoneForDisplay(updated.phoneNumber)
          : ""
      )
      onProfileSaved?.({
        fullName: updated.fullName,
        jobTitle: updated.jobTitle,
      })
      onOpenChange(false)
    } catch (error) {
      // Revert shell chrome to the last loaded snapshot on failed save.
      onProfileSaved?.({
        fullName: snapshot.fullName,
        jobTitle: snapshot.jobTitle,
      })
      setProfileError(
        getUserFacingApiErrorMessage(
          error,
          "Unable to save profile changes."
        )
      )
    } finally {
      setProfileSaving(false)
    }
  }

  const handleChangePassword = async () => {
    setPasswordError(null)
    setPasswordSuccess(null)

    if (newPassword !== confirmNewPassword) {
      setPasswordError("Passwords do not match.")
      return
    }
    if (newPassword.trim().length === 0) {
      setPasswordError("New password is required.")
      return
    }

    setPasswordSaving(true)
    try {
      await changeMyAccountPassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      })
      setCurrentPassword("")
      setNewPassword("")
      setConfirmNewPassword("")
      setPasswordSuccess("Password changed successfully.")
    } catch (error) {
      setPasswordError(
        getUserFacingApiErrorMessage(error, "Unable to change password.")
      )
    } finally {
      setPasswordSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent showCloseButton={false} className={DIALOG_CONTENT_CLASS}>
        <div className="flex items-start justify-between gap-5">
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 font-sans text-2xl font-bold tracking-normal text-op-text-primary">
              My account
            </DialogTitle>
            <DialogDescription
              className={cn(
                "max-w-none font-sans text-base font-medium leading-normal",
                MUTED_TEXT_CLASS
              )}
            >
              Manage your personal details, security and account access.
            </DialogDescription>
          </DialogHeader>
          <DialogClose asChild>
            <Button
              type="button"
              variant="op-collapse"
              aria-label="Close"
              className="shrink-0"
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        {loadStatus === "loading" ? (
          <div className="flex items-center justify-center py-16">
            <Spinner className="size-6" />
          </div>
        ) : loadStatus === "error" ? (
          <div className="flex flex-col items-start gap-3 py-8">
            <p className="m-0 text-sm text-destructive">{loadError}</p>
            <Button type="button" variant="op-secondary" onClick={handleClose}>
              Close
            </Button>
          </div>
        ) : (
          <Tabs
            value={tab}
            onValueChange={(value) => setTab(value as MyAccountTab)}
            className="gap-5"
          >
            <div className="border-b border-op-card-border">
              <TabsList
                variant="line"
                className={cn(ACCOUNT_WORKSPACE_TAB_LIST_CLASS, "gap-5")}
              >
                <TabsTrigger
                  value="profile"
                  className={cn(
                    ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
                    "px-[18px] py-3.5 text-base tracking-[-0.4px]"
                  )}
                >
                  Profile
                </TabsTrigger>
                <TabsTrigger
                  value="security"
                  className={cn(
                    ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
                    "px-[18px] py-3.5 text-base tracking-[-0.4px]"
                  )}
                >
                  Security
                </TabsTrigger>
                <TabsTrigger
                  value="access"
                  className={cn(
                    ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
                    "px-[18px] py-3.5 text-base tracking-[-0.4px]"
                  )}
                >
                  Access
                </TabsTrigger>
              </TabsList>
            </div>

            <TabsContent value="profile" className="mt-0 flex flex-col gap-5">
              <div className="grid gap-5 md:grid-cols-2">
                <Field
                  id="my-account-full-name"
                  label="Full name"
                  value={fullName}
                  onChange={setFullName}
                  autoComplete="name"
                />
                <Field
                  id="my-account-email"
                  label="Email"
                  value={snapshot.email}
                  readOnly
                  autoComplete="email"
                />
              </div>
              <div className="h-px w-full bg-op-card-border" />
              <div className="grid gap-5 md:grid-cols-2">
                <Field
                  id="my-account-job-title"
                  label="Job title"
                  value={jobTitle}
                  onChange={setJobTitle}
                  placeholder="Enter your job title"
                  autoComplete="organization-title"
                />
                <Field
                  id="my-account-phone"
                  label="Phone number"
                  value={phoneNumber}
                  onChange={setPhoneNumber}
                  placeholder="Enter your phone number"
                  autoComplete="tel"
                />
              </div>
              {profileError ? (
                <p className="m-0 text-sm text-destructive">{profileError}</p>
              ) : null}
              <div className="flex flex-wrap gap-3 pt-[60px]">
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={profileSaving}
                  onClick={() => void handleSaveProfile()}
                >
                  {profileSaving ? "Saving…" : "Save changes"}
                </Button>
                <Button
                  type="button"
                  variant="op-tertiary"
                  disabled={profileSaving}
                  onClick={handleClose}
                >
                  Cancel
                </Button>
              </div>
            </TabsContent>

            <TabsContent value="security" className="mt-0 flex flex-col gap-5">
              <Field
                id="my-account-current-password"
                label="Current password"
                value={currentPassword}
                onChange={setCurrentPassword}
                type="password"
                autoComplete="current-password"
                placeholder="Enter current password"
              />
              <div className="grid gap-5 md:grid-cols-2">
                <Field
                  id="my-account-new-password"
                  label="New password"
                  value={newPassword}
                  onChange={setNewPassword}
                  type="password"
                  autoComplete="new-password"
                  placeholder="Enter new password"
                />
                <Field
                  id="my-account-confirm-password"
                  label="Confirm new password"
                  value={confirmNewPassword}
                  onChange={setConfirmNewPassword}
                  type="password"
                  autoComplete="new-password"
                  placeholder="Confirm new password"
                />
              </div>
              {passwordError ? (
                <p className="m-0 text-sm text-destructive">{passwordError}</p>
              ) : null}
              {passwordSuccess ? (
                <p className="m-0 text-sm text-primary">{passwordSuccess}</p>
              ) : null}
              <div>
                <Button
                  type="button"
                  variant="op-secondary"
                  disabled={passwordSaving}
                  onClick={() => void handleChangePassword()}
                >
                  {passwordSaving ? "Changing…" : "Change password"}
                </Button>
              </div>

              <div className="h-px w-full bg-op-card-border" />

              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-2">
                  <div className="flex flex-wrap items-center gap-3">
                    <p className="m-0 text-base font-semibold leading-[22px] text-op-text-primary">
                      Two-factor authentication
                    </p>
                    <Badge variant="soft">
                      {snapshot.twoFactorEnabled ? "Enabled" : "Not enabled"}
                    </Badge>
                  </div>
                  <p
                    className={cn(
                      "m-0 text-sm font-medium leading-5",
                      MUTED_TEXT_CLASS
                    )}
                  >
                    Add an extra layer of security to your account.
                  </p>
                </div>
                <div>
                  <Button type="button" variant="op-secondary" disabled>
                    Set up 2FA
                  </Button>
                </div>
              </div>
            </TabsContent>

            <TabsContent value="access" className="mt-0 flex flex-col gap-5">
              <Field
                id="my-account-role"
                label="Your role"
                value={snapshot.role}
                readOnly
              />
              <Field
                id="my-account-organisation"
                label="Organisation"
                value={snapshot.organisation}
                readOnly
              />
              <div className="h-px w-full bg-op-card-border" />
              <div className="flex flex-col gap-5">
                <p className="m-0 text-base font-semibold leading-[22px] text-op-text-primary">
                  Location access
                </p>
                {snapshot.locationAccess.length === 0 ? (
                  <p className={cn("m-0 text-sm", MUTED_TEXT_CLASS)}>
                    No locations available.
                  </p>
                ) : (
                  <ul className="m-0 flex list-none flex-col p-0">
                    {snapshot.locationAccess.map((row, index) => (
                      <li
                        key={`${row.locationName}-${index}`}
                        className={cn(
                          "flex max-w-xs items-start justify-between gap-4 py-3.5",
                          index > 0 ? "border-t border-op-card-border" : null
                        )}
                      >
                        <span
                          className={cn(
                            "text-sm font-semibold leading-5",
                            MUTED_TEXT_CLASS
                          )}
                        >
                          {row.locationName}
                        </span>
                        <Badge variant="soft">{row.accessLabel}</Badge>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </TabsContent>
          </Tabs>
        )}
      </DialogContent>
    </Dialog>
  )
}

function Field({
  id,
  label,
  value,
  onChange,
  readOnly = false,
  type = "text",
  placeholder,
  autoComplete,
}: {
  id: string
  label: string
  value: string
  onChange?: (value: string) => void
  readOnly?: boolean
  type?: "text" | "password"
  placeholder?: string
  autoComplete?: string
}) {
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <Label htmlFor={id} className={FIELD_LABEL_CLASS}>
        {label}
      </Label>
      <Input
        id={id}
        type={type}
        value={value}
        readOnly={readOnly}
        disabled={readOnly}
        placeholder={placeholder}
        autoComplete={autoComplete}
        className={cn(
          FIELD_INPUT_CLASS,
          readOnly ? MUTED_TEXT_CLASS : null
        )}
        onChange={
          onChange
            ? (event) => {
                onChange(event.target.value)
              }
            : undefined
        }
      />
    </div>
  )
}
