import { useEffect, useMemo, useRef, useState } from "react"
import { SearchIcon } from "lucide-react"
import { toast } from "sonner"

import {
  approveTrialRequest,
  deleteTrialRequest,
  getTrialRequests,
  resendInvite,
  updateStatus,
} from "@/api/adminApi"
import {
  AccountTypeBadge,
  ActivationStatusBadge,
  TrialRequestStatusBadge,
} from "@/components/dashboard/admin/adminTrialRequestStatus"
import { TrialRequestActionsMenu } from "@/components/dashboard/admin/TrialRequestActionsMenu"
import { OperatorDetailsDrawer } from "@/components/dashboard/admin/OperatorDetailsDrawer"
import {
  TrialRequestFeedbackDialog,
  type TrialRequestFeedbackKind,
} from "@/components/dashboard/admin/TrialRequestFeedbackDialog"
import {
  applyTrialRequestApprove,
  applyTrialRequestDecline,
  applyTrialRequestDelete,
  applyTrialRequestMoreInfo,
  applyTrialRequestResendInvite,
} from "@/lib/adminTrialRequestOptimistic"
import { cn } from "@/lib/utils"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { canPurgeTrialData } from "@/lib/env"
import type { AdminTrialRequest } from "@/types/admin"

const PAGE_SIZE = 10

type PendingAction =
  | { kind: "approve"; request: AdminTrialRequest }
  | { kind: "decline"; request: AdminTrialRequest }
  | { kind: "more-info"; request: AdminTrialRequest }
  | { kind: "resend"; request: AdminTrialRequest }
  | { kind: "delete"; request: AdminTrialRequest }

type SimpleConfirmAction =
  | { kind: "approve"; request: AdminTrialRequest }
  | { kind: "resend"; request: AdminTrialRequest }
  | { kind: "delete"; request: AdminTrialRequest }

const SIMPLE_CONFIRM_COPY: Record<
  SimpleConfirmAction["kind"],
  {
    title: string
    description: (request: AdminTrialRequest) => string
    confirmLabel: string
    variant: "default" | "destructive-solid"
  }
> = {
  approve: {
    title: "Approve trial request?",
    description: (request) =>
      `This approves ${request.businessName} and sends an Operator Setup invitation to ${request.email}.`,
    confirmLabel: "Approve",
    variant: "default",
  },
  resend: {
    title: "Resend Operator Setup invitation?",
    description: (request) =>
      `This sends a new setup link to ${request.email} for ${request.businessName}.`,
    confirmLabel: "Resend invitation",
    variant: "default",
  },
  delete: {
    title: "Delete trial request?",
    description: (request) =>
      `This permanently removes the trial request for ${request.email} and all related data. This cannot be undone.`,
    confirmLabel: "Delete",
    variant: "destructive-solid",
  },
}

function isFeedbackAction(
  action: PendingAction | null
): action is { kind: TrialRequestFeedbackKind; request: AdminTrialRequest } {
  return action?.kind === "decline" || action?.kind === "more-info"
}

function buildPageNumbers(current: number, total: number) {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1)
  }

  const pages = new Set<number>([1, total, current, current - 1, current + 1])
  return [...pages]
    .filter((page) => page >= 1 && page <= total)
    .sort((a, b) => a - b)
}

function Dashboard() {
  const [requests, setRequests] = useState<AdminTrialRequest[]>([])
  const [search, setSearch] = useState("")
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [actionId, setActionId] = useState<number | null>(null)
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(null)
  const [feedbackMessage, setFeedbackMessage] = useState("")
  const [selectedRequestId, setSelectedRequestId] = useState<number | null>(null)
  const keepDrawerOpenUntilRef = useRef(0)

  const feedbackAction = isFeedbackAction(pendingAction) ? pendingAction : null
  const simpleConfirmAction =
    pendingAction && !isFeedbackAction(pendingAction) ? pendingAction : null
  const isConfirmDialogOpen = pendingAction !== null

  const shouldKeepDrawerOpen = () =>
    isConfirmDialogOpen || Date.now() < keepDrawerOpenUntilRef.current

  const selectedRequest = useMemo(
    () => requests.find((request) => request.id === selectedRequestId) ?? null,
    [requests, selectedRequestId]
  )

  const showPurgeButton = canPurgeTrialData()

  const loadData = async () => {
    try {
      setLoading(true)
      const result = await getTrialRequests()
      setRequests(result)
    } catch (error) {
      console.error(error)
      toast.error("Could not load trial requests")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadData()
  }, [])

  useEffect(() => {
    setPage(1)
  }, [search])

  const filteredRequests = useMemo(() => {
    const query = search.trim().toLowerCase()

    if (!query) {
      return requests
    }

    return requests.filter((request) => {
      return (
        request.businessName?.toLowerCase().includes(query) ||
        request.fullName?.toLowerCase().includes(query) ||
        request.email?.toLowerCase().includes(query) ||
        request.mobile?.toLowerCase().includes(query) ||
        request.status?.toLowerCase().includes(query)
      )
    })
  }, [requests, search])

  const totalPages = Math.max(1, Math.ceil(filteredRequests.length / PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)

  const paginatedRequests = useMemo(() => {
    const start = (currentPage - 1) * PAGE_SIZE
    return filteredRequests.slice(start, start + PAGE_SIZE)
  }, [currentPage, filteredRequests])

  const pageNumbers = buildPageNumbers(currentPage, totalPages)

  const runOptimisticAction = async ({
    requestId,
    applyUpdate,
    apiCall,
    successMessage,
    onOptimisticApplied,
    onRollback,
  }: {
    requestId: number
    applyUpdate: (current: AdminTrialRequest[]) => AdminTrialRequest[]
    apiCall: () => Promise<unknown>
    successMessage: string
    onOptimisticApplied?: () => void
    onRollback?: () => void
  }) => {
    const snapshot = requests

    setRequests((current) => applyUpdate(current))
    setActionId(requestId)
    onOptimisticApplied?.()

    try {
      await apiCall()
      toast.success(successMessage)
    } catch (error) {
      console.error(error)
      setRequests(snapshot)
      onRollback?.()
      toast.error("Something went wrong")
    } finally {
      setActionId(null)
    }
  }

  const handleApprove = (request: AdminTrialRequest) =>
    setPendingAction({ kind: "approve", request })

  const handleDecline = (request: AdminTrialRequest) => {
    setFeedbackMessage("")
    setPendingAction({ kind: "decline", request })
  }

  const handleRequestMoreInfo = (request: AdminTrialRequest) => {
    setFeedbackMessage("")
    setPendingAction({ kind: "more-info", request })
  }

  const handleResendInvite = (request: AdminTrialRequest) =>
    setPendingAction({ kind: "resend", request })

  const handleDelete = (request: AdminTrialRequest) =>
    setPendingAction({ kind: "delete", request })

  const handleConfirmAction = async (action: SimpleConfirmAction) => {
    setPendingAction(null)

    const { kind, request } = action

    switch (kind) {
      case "approve":
        await runOptimisticAction({
          requestId: request.id,
          applyUpdate: (current) => applyTrialRequestApprove(current, request.id),
          apiCall: () => approveTrialRequest(request.id),
          successMessage: "Trial request approved",
        })
        break
      case "resend":
        await runOptimisticAction({
          requestId: request.id,
          applyUpdate: (current) =>
            applyTrialRequestResendInvite(current, request.id),
          apiCall: () => resendInvite(request.id),
          successMessage: "Operator Setup invitation resent",
        })
        break
      case "delete":
        await runOptimisticAction({
          requestId: request.id,
          applyUpdate: (current) => applyTrialRequestDelete(current, request.id),
          apiCall: () => deleteTrialRequest(request.id),
          successMessage: "Trial request deleted",
          onOptimisticApplied: () => setSelectedRequestId(null),
          onRollback: () => setSelectedRequestId(request.id),
        })
        break
    }
  }

  const handleConfirmFeedbackAction = async () => {
    if (!feedbackAction) {
      return
    }

    const trimmedMessage = feedbackMessage.trim()
    if (!trimmedMessage) {
      return
    }

    const { kind, request } = feedbackAction
    setPendingAction(null)
    setFeedbackMessage("")

    if (kind === "decline") {
      await runOptimisticAction({
        requestId: request.id,
        applyUpdate: (current) =>
          applyTrialRequestDecline(current, request.id, trimmedMessage),
        apiCall: () =>
          updateStatus({
            trialRequestId: request.id,
            status: "DECLINED",
            declineReason: trimmedMessage,
          }),
        successMessage: "Trial request declined",
      })
      return
    }

    await runOptimisticAction({
      requestId: request.id,
      applyUpdate: (current) =>
        applyTrialRequestMoreInfo(current, request.id, trimmedMessage),
      apiCall: () =>
        updateStatus({
          trialRequestId: request.id,
          status: "MORE_INFO_REQUESTED",
          moreInfoMessage: trimmedMessage,
        }),
      successMessage: "More info email sent",
    })
  }

  const closePendingAction = () => {
    keepDrawerOpenUntilRef.current = Date.now() + 350
    setPendingAction(null)
    setFeedbackMessage("")
  }

  const stats = [
    { title: "Trial requests", value: requests.length },
    {
      title: "Email verified",
      value: requests.filter((request) => request.isEmailVerified).length,
    },
    {
      title: "Approved",
      value: requests.filter((request) => request.isApproved).length,
    },
    {
      title: "Operator accounts ready",
      value: requests.filter((request) => request.isAccountCreated).length,
    },
  ]

  return (
    <div className="min-h-screen bg-muted/40 px-4 py-8 [--radius:0.75rem] sm:px-8">
      <div className="mx-auto flex w-full max-w-[1400px] flex-col gap-8">
        <header className="flex flex-col gap-2">
          <p className="text-sm font-medium tracking-wide text-primary uppercase">
            Tummly admin
          </p>
          <h1 className="font-heading text-3xl font-semibold tracking-tight text-foreground">
            Trial request review
          </h1>
          <p className="max-w-2xl text-sm text-muted-foreground">
            Review incoming trial requests, approve operators, and manage
            Operator Setup invitations.
          </p>
        </header>

        <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {stats.map((item) => (
            <Card key={item.title} size="sm" className="rounded-2xl">
              <CardHeader>
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  {item.title}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="font-heading text-3xl font-semibold text-foreground">
                  {loading ? <Skeleton className="h-9 w-16" /> : item.value}
                </div>
              </CardContent>
            </Card>
          ))}
        </section>

        <Card className="overflow-hidden rounded-2xl">
          <CardHeader className="border-b">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <CardTitle>All trial requests</CardTitle>
                <p className="text-sm text-muted-foreground">
                  {loading
                    ? "Loading requests…"
                    : `${filteredRequests.length} result${filteredRequests.length === 1 ? "" : "s"}`}
                </p>
              </div>
              <div className="relative w-full sm:max-w-sm">
                <SearchIcon className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search business, owner, email, or status"
                  className="h-10 rounded-xl pl-9"
                  aria-label="Search trial requests"
                />
              </div>
            </div>
          </CardHeader>

          <CardContent className="px-0">
            <Table>
              <TableHeader>
                <TableRow className="bg-brand-dark hover:bg-brand-dark">
                  <TableHead className="text-brand-dark-foreground">ID</TableHead>
                  <TableHead className="text-brand-dark-foreground">Business</TableHead>
                  <TableHead className="text-brand-dark-foreground">Category</TableHead>
                  <TableHead className="text-brand-dark-foreground">Owner</TableHead>
                  <TableHead className="text-brand-dark-foreground">Role</TableHead>
                  <TableHead className="text-brand-dark-foreground">Email</TableHead>
                  <TableHead className="text-brand-dark-foreground">Mobile</TableHead>
                  <TableHead className="text-brand-dark-foreground">Account</TableHead>
                  <TableHead className="text-brand-dark-foreground">Status</TableHead>
                  <TableHead className="text-brand-dark-foreground">Submitted</TableHead>
                  <TableHead className="text-right text-brand-dark-foreground">
                    Actions
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading &&
                  Array.from({ length: 5 }).map((_, index) => (
                    <TableRow key={`skeleton-${index}`}>
                      {Array.from({ length: 11 }).map((__, cellIndex) => (
                        <TableCell key={cellIndex}>
                          <Skeleton className="h-5 w-full max-w-28" />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {!loading &&
                  paginatedRequests.map((request) => (
                    <TableRow
                      key={request.id}
                      className={cn(
                        "cursor-pointer transition-colors hover:bg-muted/50",
                        selectedRequestId === request.id && "bg-muted/50"
                      )}
                      onClick={() => setSelectedRequestId(request.id)}
                    >
                      <TableCell className="font-medium text-muted-foreground">
                        {request.id}
                      </TableCell>
                      <TableCell className="max-w-44 truncate font-medium">
                        {request.businessName}
                      </TableCell>
                      <TableCell className="max-w-40 truncate text-muted-foreground">
                        {request.businessCategory}
                      </TableCell>
                      <TableCell>{request.fullName}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {request.role}
                      </TableCell>
                      <TableCell className="max-w-48 truncate">
                        {request.email}
                      </TableCell>
                      <TableCell>{request.mobile}</TableCell>
                      <TableCell>
                        <AccountTypeBadge accountType={request.accountType} />
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col gap-1">
                          <TrialRequestStatusBadge request={request} />
                          <ActivationStatusBadge request={request} />
                        </div>
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {new Date(request.createdAt).toLocaleDateString()}
                      </TableCell>
                      <TableCell
                        className="text-right"
                        onClick={(event) => event.stopPropagation()}
                      >
                        <TrialRequestActionsMenu
                          request={request}
                          showDelete={showPurgeButton}
                          disabled={actionId === request.id}
                          onApprove={handleApprove}
                          onDecline={handleDecline}
                          onRequestMoreInfo={handleRequestMoreInfo}
                          onResendInvite={handleResendInvite}
                          onDelete={handleDelete}
                        />
                      </TableCell>
                    </TableRow>
                  ))}

                {!loading && filteredRequests.length === 0 && (
                  <TableRow>
                    <TableCell
                      colSpan={11}
                      className="h-32 text-center text-muted-foreground"
                    >
                      No trial requests match your search.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>

          {!loading && filteredRequests.length > 0 && (
            <div className="flex flex-col gap-3 rounded-b-2xl border-t px-4 py-4 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-sm text-muted-foreground">
                Showing {(currentPage - 1) * PAGE_SIZE + 1}–
                {Math.min(currentPage * PAGE_SIZE, filteredRequests.length)} of{" "}
                {filteredRequests.length}
              </p>
              <Pagination className="mx-0 w-auto justify-end">
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      onClick={() => setPage((value) => Math.max(1, value - 1))}
                      disabled={currentPage === 1}
                    />
                  </PaginationItem>

                  {pageNumbers.flatMap((pageNumber, index) => {
                    const previous = pageNumbers[index - 1]
                    const items = []

                    if (previous !== undefined && pageNumber - previous > 1) {
                      items.push(
                        <PaginationItem key={`ellipsis-${pageNumber}`}>
                          <PaginationEllipsis />
                        </PaginationItem>
                      )
                    }

                    items.push(
                      <PaginationItem key={pageNumber}>
                        <PaginationLink
                          isActive={pageNumber === currentPage}
                          onClick={() => setPage(pageNumber)}
                        >
                          {pageNumber}
                        </PaginationLink>
                      </PaginationItem>
                    )

                    return items
                  })}

                  <PaginationItem>
                    <PaginationNext
                      onClick={() =>
                        setPage((value) => Math.min(totalPages, value + 1))
                      }
                      disabled={currentPage === totalPages}
                    />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          )}
        </Card>
      </div>

      <OperatorDetailsDrawer
        request={selectedRequest}
        open={selectedRequest !== null}
        dismissible={!isConfirmDialogOpen}
        onOpenChange={(open) => {
          if (!open && shouldKeepDrawerOpen()) {
            return
          }

          if (!open) {
            setSelectedRequestId(null)
          }
        }}
        showDelete={showPurgeButton}
        actionsDisabled={selectedRequest !== null && actionId === selectedRequest.id}
        onApprove={handleApprove}
        onDecline={handleDecline}
        onRequestMoreInfo={handleRequestMoreInfo}
        onResendInvite={handleResendInvite}
        onDelete={handleDelete}
        onRequestUpdated={(updatedRequest) => {
          setRequests((current) =>
            current.map((item) =>
              item.id === updatedRequest.id ? updatedRequest : item
            )
          )
        }}
      />

      {feedbackAction && (
        <TrialRequestFeedbackDialog
          kind={feedbackAction.kind}
          request={feedbackAction.request}
          message={feedbackMessage}
          onMessageChange={setFeedbackMessage}
          open
          onOpenChange={(open) => {
            if (!open) {
              closePendingAction()
            }
          }}
          onConfirm={() => void handleConfirmFeedbackAction()}
          disabled={actionId === feedbackAction.request.id}
        />
      )}

      <AlertDialog
        open={simpleConfirmAction !== null}
        onOpenChange={(open) => {
          if (!open) {
            closePendingAction()
          }
        }}
      >
        <AlertDialogContent className="rounded-2xl">
          {simpleConfirmAction && (
            <>
              <AlertDialogHeader>
                <AlertDialogTitle>
                  {SIMPLE_CONFIRM_COPY[simpleConfirmAction.kind].title}
                </AlertDialogTitle>
                <AlertDialogDescription>
                  {SIMPLE_CONFIRM_COPY[simpleConfirmAction.kind].description(
                    simpleConfirmAction.request
                  )}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  variant={SIMPLE_CONFIRM_COPY[simpleConfirmAction.kind].variant}
                  onClick={() => void handleConfirmAction(simpleConfirmAction)}
                >
                  {SIMPLE_CONFIRM_COPY[simpleConfirmAction.kind].confirmLabel}
                </AlertDialogAction>
              </AlertDialogFooter>
            </>
          )}
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

export default Dashboard
