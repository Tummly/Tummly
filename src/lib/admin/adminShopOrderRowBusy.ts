/**
 * Immutable update of the set of order ids with a row action in flight.
 * Returns the same set when nothing changes, so React state is not re-rendered needlessly.
 */
export function withRowBusy(
  busyOrderIds: ReadonlySet<string>,
  orderId: string,
  busy: boolean
): ReadonlySet<string> {
  if (busyOrderIds.has(orderId) === busy) {
    return busyOrderIds
  }
  const next = new Set(busyOrderIds)
  if (busy) {
    next.add(orderId)
  } else {
    next.delete(orderId)
  }
  return next
}
