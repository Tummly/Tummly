import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"

import {
  fetchAdminShopInterest,
  type AdminShopInterestProductCount,
  type AdminShopInterestRegistration,
} from "@/api/adminShopInterestApi"
import { Badge } from "@/components/ui/badge"
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
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

function formatUtc(iso: string) {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  })
}

export function AdminShopInterestPanel() {
  const [items, setItems] = useState<AdminShopInterestRegistration[]>([])
  const [counts, setCounts] = useState<AdminShopInterestProductCount[]>([])
  const [loading, setLoading] = useState(true)

  const reload = useCallback(async () => {
    setLoading(true)
    try {
      const next = await fetchAdminShopInterest()
      setItems(next.items)
      setCounts(next.countsByProduct)
    } catch {
      toast.error("Could not load shop interest.")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void reload()
  }, [reload])

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div>
        <h1 className={GUESTS_PAGE_TITLE_CLASS}>Shop interest</h1>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Operators who registered interest in wider media concepts. No
          automatic marketing contact.
        </p>
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="mb-4">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Counts by product</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            Total registrations per interest product.
          </p>
        </div>
        {loading ? (
          <Skeleton className="h-10 w-full" />
        ) : counts.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No interest registrations yet.
          </p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {counts.map((row) => (
              <Badge key={row.productKey} variant="soft">
                {row.title}: {row.count}
              </Badge>
            ))}
          </div>
        )}
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="mb-4">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Interest list</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            Newest registrations first.
          </p>
        </div>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Product
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Business
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Location
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  User
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  When (UTC)
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                Array.from({ length: 3 }).map((_, index) => (
                  <TableRow key={index} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell
                      colSpan={5}
                      className={GUESTS_TABLE_BODY_CELL_CLASS}
                    >
                      <Skeleton className="h-8 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              ) : items.length === 0 ? (
                <TableRow className={GUESTS_TABLE_BODY_ROW_CLASS}>
                  <TableCell
                    colSpan={5}
                    className={GUESTS_TABLE_BODY_CELL_CLASS}
                  >
                    No interest records yet.
                  </TableCell>
                </TableRow>
              ) : (
                items.map((row) => (
                  <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div className="font-medium">{row.productTitle}</div>
                      <div className="text-xs text-muted-foreground">
                        {row.productKey}
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.restaurantName}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {row.locationName ?? "—"}
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <div>{row.userFullName}</div>
                      <div className="text-xs text-muted-foreground">
                        {row.userEmail}
                      </div>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {formatUtc(row.createdAtUtc)}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </section>
    </div>
  )
}
