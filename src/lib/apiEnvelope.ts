function asRecord(value: unknown): Record<string, unknown> | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return null
  }

  return value as Record<string, unknown>
}

function findEntry(
  record: Record<string, unknown>,
  key: string
): unknown {
  const normalizedKey = key.toLowerCase()

  for (const [entryKey, entryValue] of Object.entries(record)) {
    if (entryKey.toLowerCase() === normalizedKey) {
      return entryValue
    }
  }

  return undefined
}

export function readString(
  record: unknown,
  key: string
): string | null {
  const source = asRecord(record)

  if (!source) {
    return null
  }

  const value = findEntry(source, key)

  if (typeof value === "string" && value.trim()) {
    return value.trim()
  }

  return null
}

export function readFirstString(
  record: unknown,
  keys: string[]
): string | null {
  for (const key of keys) {
    const value = readString(record, key)

    if (value) {
      return value
    }
  }

  return null
}

export function readBoolean(
  record: unknown,
  key: string
): boolean | undefined {
  const source = asRecord(record)

  if (!source) {
    return undefined
  }

  const value = findEntry(source, key)

  if (typeof value === "boolean") {
    return value
  }

  return undefined
}

export function readNumber(
  record: unknown,
  key: string
): number | null {
  const source = asRecord(record)

  if (!source) {
    return null
  }

  const value = findEntry(source, key)

  if (typeof value === "number" && Number.isFinite(value)) {
    return value
  }

  return null
}

/** Peel `{ data: { … } }` (any casing) or return the root object. */
export function unwrapDataObject(
  payload: unknown
): Record<string, unknown> | null {
  const envelope = asRecord(payload)

  if (!envelope) {
    return null
  }

  const nested = asRecord(findEntry(envelope, "data"))

  if (nested) {
    return nested
  }

  return envelope
}

/** Peel `{ data: [ … ] }` (any casing) for list envelopes. */
export function unwrapDataArray(payload: unknown): unknown[] {
  const envelope = asRecord(payload)

  if (!envelope) {
    return []
  }

  const data = findEntry(envelope, "data")

  return Array.isArray(data) ? data : []
}

export function getFetchErrorMessage(
  result: { message?: string },
  fallback: string
): string {
  return result.message?.trim() || fallback
}
