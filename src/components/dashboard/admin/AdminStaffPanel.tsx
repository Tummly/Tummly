import { useCallback, useEffect, useState } from "react"
import { MoreVertical } from "lucide-react"
import { toast } from "sonner"

import {
  createStaffMember,
  disableStaffMember,
  enableStaffMember,
  fetchStaffMembers,
  resetStaffPassword,
  STAFF_ROLES,
  StaffApiError,
  unlockStaffMember,
  type AdminStaffMember,
  type StaffRole,
} from "@/api/adminStaffApi"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { FloatingLabelTextarea } from "@/components/ui/floating-label-textarea"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  staffErrorMessage,
  staffStatusLabel,
  validateStaffPassword,
  validateStaffReason,
  type StaffStatusLabel,
} from "@/lib/admin/adminStaff"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_SEPARATOR_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

/** Keeps the role menu above the Add staff dialog. */
const DIALOG_SELECT_CONTENT_CLASS = "z-[150]"

const STATUS_BADGE_VARIANT: Record<StaffStatusLabel, "ready" | "error" | "outline"> = {
  Active: "ready",
  Locked: "error",
  Disabled: "outline",
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

type StaffAction = "disable" | "enable" | "unlock" | "reset"

type PendingAction = {
  member: AdminStaffMember
  action: StaffAction
}

const ACTION_TITLES: Record<StaffAction, string> = {
  disable: "Disable staff account",
  enable: "Enable staff account",
  unlock: "Unlock staff account",
  reset: "Reset password",
}

const ACTION_SUCCESS: Record<StaffAction, string> = {
  disable: "Staff account disabled. Its sessions stop on the next request.",
  enable: "Staff account enabled.",
  unlock: "Staff account unlocked.",
  reset: "Password reset. The account is unlocked.",
}

function messageFor(error: unknown, fallback: string): string {
  if (error instanceof StaffApiError) {
    return staffErrorMessage(error.code, error.status, error.message || fallback)
  }
  return fallback
}

export function AdminStaffPanel() {
  const [members, setMembers] = useState<AdminStaffMember[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [createOpen, setCreateOpen] = useState(false)
  const [createBusy, setCreateBusy] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const [createFields, setCreateFields] = useState({
    fullName: "",
    email: "",
    role: "Support" as StaffRole,
    initialPassword: "",
    reason: "",
  })
  const [createErrors, setCreateErrors] = useState<Record<string, string>>({})

  const [pending, setPending] = useState<PendingAction | null>(null)
  const [actionBusy, setActionBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionReason, setActionReason] = useState("")
  const [actionPassword, setActionPassword] = useState("")
  const [actionErrors, setActionErrors] = useState<Record<string, string>>({})

  const loadMembers = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      setMembers(await fetchStaffMembers())
    } catch (error) {
      setLoadError(messageFor(error, "Could not load staff accounts."))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    fetchStaffMembers()
      .then((next) => {
        if (!cancelled) setMembers(next)
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setLoadError(messageFor(error, "Could not load staff accounts."))
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  function openCreate() {
    setCreateFields({
      fullName: "",
      email: "",
      role: "Support",
      initialPassword: "",
      reason: "",
    })
    setCreateErrors({})
    setCreateError(null)
    setCreateOpen(true)
  }

  function validateCreate(): Record<string, string> {
    const errors: Record<string, string> = {}
    const fullName = createFields.fullName.trim()
    if (fullName.length < 1 || fullName.length > 150) {
      errors.fullName = "Enter a full name of 1 to 150 characters."
    }
    const email = createFields.email.trim()
    if (!EMAIL_SHAPE.test(email)) {
      errors.email = "Enter a valid email address."
    }
    const passwordError = validateStaffPassword(
      createFields.initialPassword,
      email
    )
    if (passwordError) errors.initialPassword = passwordError
    const reasonError = validateStaffReason(createFields.reason)
    if (reasonError) errors.reason = reasonError
    return errors
  }

  async function submitCreate() {
    const errors = validateCreate()
    setCreateErrors(errors)
    if (Object.keys(errors).length > 0) return

    setCreateBusy(true)
    setCreateError(null)
    try {
      await createStaffMember({
        fullName: createFields.fullName.trim(),
        email: createFields.email.trim(),
        role: createFields.role,
        initialPassword: createFields.initialPassword,
        reason: createFields.reason.trim(),
      })
      await loadMembers()
      toast.success("Staff account added.")
      setCreateOpen(false)
    } catch (error) {
      setCreateError(messageFor(error, "Could not add the staff account."))
    } finally {
      setCreateBusy(false)
    }
  }

  function openAction(member: AdminStaffMember, action: StaffAction) {
    setPending({ member, action })
    setActionReason("")
    setActionPassword("")
    setActionErrors({})
    setActionError(null)
  }

  function closeAction() {
    setPending(null)
  }

  function validateAction(): Record<string, string> {
    if (!pending) return {}
    const errors: Record<string, string> = {}
    const reasonError = validateStaffReason(actionReason)
    if (reasonError) errors.reason = reasonError
    if (pending.action === "reset") {
      const passwordError = validateStaffPassword(
        actionPassword,
        pending.member.email
      )
      if (passwordError) errors.newPassword = passwordError
    }
    return errors
  }

  async function submitAction() {
    if (!pending) return
    const errors = validateAction()
    setActionErrors(errors)
    if (Object.keys(errors).length > 0) return

    const reason = actionReason.trim()
    const { member, action } = pending
    setActionBusy(true)
    setActionError(null)
    try {
      if (action === "disable") await disableStaffMember(member.id, reason)
      if (action === "enable") await enableStaffMember(member.id, reason)
      if (action === "unlock") await unlockStaffMember(member.id, reason)
      if (action === "reset") {
        await resetStaffPassword(member.id, {
          newPassword: actionPassword,
          reason,
        })
      }
      await loadMembers()
      toast.success(ACTION_SUCCESS[action])
      setPending(null)
    } catch (error) {
      setActionError(messageFor(error, "The change was not saved."))
    } finally {
      setActionBusy(false)
    }
  }

  if (loading && members === null) {
    return (
      <div className={GUESTS_PAGE_STACK_CLASS} aria-busy="true">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  if (loadError !== null || members === null) {
    return (
      <div className={GUESTS_PAGE_STACK_CLASS} role="alert">
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{loadError}</p>
        <div>
          <Button type="button" variant="outline" onClick={() => void loadMembers()}>
            Try again
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>Staff accounts</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            Add Admin and Support sign-ins, disable or unlock them, and reset
            passwords. Every change needs a reason and is written to the audit log.
          </p>
        </div>
        <Button type="button" onClick={openCreate}>
          Add staff
        </Button>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Name</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Email</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Role</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Status</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>Password</TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  <span className="sr-only">Actions</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {members.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell colSpan={6} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    No staff accounts yet.
                  </TableCell>
                </TableRow>
              ) : (
                members.map((member) => {
                  const status = staffStatusLabel(member)
                  return (
                    <TableRow
                      key={member.id}
                      className={GUESTS_TABLE_BODY_ROW_CLASS}
                      data-testid={`staff-row-${member.id}`}
                    >
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {member.fullName}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {member.email}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant="secondary">{member.role}</Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <Badge variant={STATUS_BADGE_VARIANT[status]}>{status}</Badge>
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        {member.usesDefaultPassword ? (
                          <Badge variant="error">Default password</Badge>
                        ) : (
                          <span className="text-muted-foreground">Set</span>
                        )}
                      </TableCell>
                      <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                        <StaffRowActions
                          member={member}
                          disabled={actionBusy}
                          onAction={(action) => openAction(member, action)}
                        />
                      </TableCell>
                    </TableRow>
                  )
                })
              )}
            </TableBody>
          </Table>
        </div>
      </section>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="gap-6 bg-white sm:max-w-[560px]">
          <DialogHeader className="gap-2">
            <DialogTitle>Add staff account</DialogTitle>
            <DialogDescription>
              The account can sign in at once with this password. Ask the person
              to change it under their account menu.
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4">
            <FloatingLabelInput
              label="Full name"
              value={createFields.fullName}
              onChange={(event) =>
                setCreateFields((current) => ({ ...current, fullName: event.target.value }))
              }
              error={createErrors.fullName}
            />
            <FloatingLabelInput
              label="Email"
              type="email"
              value={createFields.email}
              onChange={(event) =>
                setCreateFields((current) => ({ ...current, email: event.target.value }))
              }
              error={createErrors.email}
            />
            <FloatingLabelSelect
              label="Role"
              options={STAFF_ROLES.map((role) => ({ value: role, label: role }))}
              value={createFields.role}
              onValueChange={(value) =>
                setCreateFields((current) => ({ ...current, role: value as StaffRole }))
              }
              contentClassName={DIALOG_SELECT_CONTENT_CLASS}
            />
            <FloatingLabelInput
              label="Initial password"
              type="password"
              autoComplete="new-password"
              value={createFields.initialPassword}
              onChange={(event) =>
                setCreateFields((current) => ({
                  ...current,
                  initialPassword: event.target.value,
                }))
              }
              error={createErrors.initialPassword}
            />
            <FloatingLabelTextarea
              label="Reason (required)"
              value={createFields.reason}
              onChange={(event) =>
                setCreateFields((current) => ({ ...current, reason: event.target.value }))
              }
              maxLength={500}
              error={createErrors.reason}
            />
          </div>

          {createError !== null ? (
            <p role="alert" className="text-sm text-destructive">
              {createError}
            </p>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button type="button" disabled={createBusy} onClick={() => void submitCreate()}>
              {createBusy ? "Adding…" : "Add staff"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={pending !== null} onOpenChange={(open) => !open && closeAction()}>
        <DialogContent className="gap-6 bg-white sm:max-w-[480px]">
          {pending !== null ? (
            <>
              <DialogHeader className="gap-2">
                <DialogTitle>{ACTION_TITLES[pending.action]}</DialogTitle>
                <DialogDescription>
                  {pending.member.fullName} · {pending.member.email}
                </DialogDescription>
              </DialogHeader>

              {pending.action === "reset" ? (
                <FloatingLabelInput
                  label="New password"
                  type="password"
                  autoComplete="new-password"
                  value={actionPassword}
                  onChange={(event) => setActionPassword(event.target.value)}
                  error={actionErrors.newPassword}
                />
              ) : null}

              <FloatingLabelTextarea
                label="Reason (required)"
                value={actionReason}
                onChange={(event) => setActionReason(event.target.value)}
                maxLength={500}
                error={actionErrors.reason}
              />

              {actionError !== null ? (
                <p role="alert" className="text-sm text-destructive">
                  {actionError}
                </p>
              ) : null}

              <DialogFooter>
                <Button type="button" variant="ghost" onClick={closeAction}>
                  Cancel
                </Button>
                <Button
                  type="button"
                  variant={pending.action === "disable" ? "destructive-solid" : "default"}
                  disabled={actionBusy}
                  onClick={() => void submitAction()}
                >
                  {actionBusy ? "Saving…" : ACTION_TITLES[pending.action]}
                </Button>
              </DialogFooter>
            </>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  )
}

function StaffRowActions({
  member,
  disabled,
  onAction,
}: {
  member: AdminStaffMember
  disabled: boolean
  onAction: (action: StaffAction) => void
}) {
  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          disabled={disabled}
          aria-label={`Actions for ${member.email}`}
          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
        >
          <MoreVertical className="size-4" aria-hidden />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className={GUESTS_ROW_ACTIONS_MENU_CLASS}>
        <DropdownMenuItem
          className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
          onSelect={() => onAction(member.isActive ? "disable" : "enable")}
        >
          {member.isActive ? "Disable" : "Enable"}
        </DropdownMenuItem>
        <DropdownMenuItem
          className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
          onSelect={() => onAction("unlock")}
        >
          Unlock
        </DropdownMenuItem>
        <DropdownMenuSeparator className={GUESTS_ROW_ACTIONS_SEPARATOR_CLASS} />
        <DropdownMenuItem
          className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
          onSelect={() => onAction("reset")}
        >
          Reset password
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
