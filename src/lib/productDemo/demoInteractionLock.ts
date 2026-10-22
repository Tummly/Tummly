/**
 * Public product demo: the dashboard is a guided tour, not a live app.
 * Visitor input (click, press, keys, submit) is blocked everywhere except the
 * tour chrome (`[data-demo-chrome]`). Hover still works, so the UI keeps its
 * hover states. Tour actions use programmatic `click()` calls, which are not
 * trusted events, so the lock lets them through.
 */

export const DEMO_CHROME_ATTRIBUTE = "data-demo-chrome"

const BLOCKED_POINTER_EVENTS = [
  "pointerdown",
  "pointerup",
  "mousedown",
  "mouseup",
  "click",
  "dblclick",
  "auxclick",
  "contextmenu",
  "dragstart",
] as const

const BLOCKED_KEY_EVENTS = ["keydown", "keypress", "keyup"] as const

/** Keys that only move focus or scroll the page. */
const PASS_THROUGH_KEYS = new Set([
  "Tab",
  "ArrowUp",
  "ArrowDown",
  "PageUp",
  "PageDown",
  "Home",
  "End",
])

/** Dialog libraries close on outside press / focus; chrome must not count. */
const CHROME_ISOLATED_EVENTS = [
  "pointerdown",
  "mousedown",
  "touchstart",
  "focusin",
  "focusout",
] as const

function isInChrome(target: EventTarget | null): boolean {
  return (
    target instanceof Element &&
    target.closest(`[${DEMO_CHROME_ATTRIBUTE}]`) != null
  )
}

function block(event: Event): void {
  event.preventDefault()
  event.stopImmediatePropagation()
}

/** Installs the lock and returns a function that removes it. */
export function installDemoInteractionLock(): () => void {
  if (typeof window === "undefined" || typeof document === "undefined") {
    return () => {}
  }

  const onPointer = (event: Event) => {
    if (!event.isTrusted || isInChrome(event.target)) return
    block(event)
  }

  const onKey = (event: KeyboardEvent) => {
    if (!event.isTrusted || isInChrome(event.target)) return
    if (PASS_THROUGH_KEYS.has(event.key)) {
      const target = event.target
      const onPageOnly =
        event.key === "Tab" ||
        target === document.body ||
        target === document.documentElement
      if (onPageOnly) return
    }
    block(event)
  }

  const onSubmit = (event: Event) => {
    if (!event.isTrusted || isInChrome(event.target)) return
    block(event)
  }

  // Runs after React's listener on <body> (the portal container), and stops
  // the event before document-level "outside press" handlers of open dialogs.
  const onChromeEvent = (event: Event) => {
    if (isInChrome(event.target)) {
      event.stopPropagation()
    }
  }

  for (const type of BLOCKED_POINTER_EVENTS) {
    window.addEventListener(type, onPointer, { capture: true })
  }
  for (const type of BLOCKED_KEY_EVENTS) {
    window.addEventListener(type, onKey, { capture: true })
  }
  window.addEventListener("submit", onSubmit, { capture: true })
  for (const type of CHROME_ISOLATED_EVENTS) {
    document.body.addEventListener(type, onChromeEvent)
  }

  // Modal dialogs hide every other body child from assistive tech. The tour
  // chrome must stay reachable (its CTA is the only way forward).
  const revealChrome = (node: Element) => {
    if (node.hasAttribute(DEMO_CHROME_ATTRIBUTE)) {
      node.removeAttribute("aria-hidden")
      node.removeAttribute("data-aria-hidden")
      node.removeAttribute("inert")
    }
  }
  const observer = new MutationObserver((records) => {
    for (const record of records) {
      if (record.target instanceof Element) {
        revealChrome(record.target)
      }
    }
  })
  observer.observe(document.body, {
    subtree: true,
    attributes: true,
    attributeFilter: ["aria-hidden", "inert"],
  })
  document
    .querySelectorAll(`[${DEMO_CHROME_ATTRIBUTE}]`)
    .forEach(revealChrome)

  return () => {
    observer.disconnect()
    for (const type of BLOCKED_POINTER_EVENTS) {
      window.removeEventListener(type, onPointer, { capture: true })
    }
    for (const type of BLOCKED_KEY_EVENTS) {
      window.removeEventListener(type, onKey, { capture: true })
    }
    window.removeEventListener("submit", onSubmit, { capture: true })
    for (const type of CHROME_ISOLATED_EVENTS) {
      document.body.removeEventListener(type, onChromeEvent)
    }
  }
}
