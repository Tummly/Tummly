# Pilot “See Tummly in Action” scroll hijack

Date: 2026-10-19  
Status: approved  
Reference: `HomeHowItWorksSteps` (`src/components/home/HomeHowItWorksSteps.tsx`)

## Goal

On the 30-day Pilot marketing page, the **See Tummly in Action** section (`PilotInAction`) must advance its four tabs with sticky scroll, the same way homepage How-it-works steps do. Tab clicks still jump the scroll track. Section header copy stays above the sticky track and scrolls away.

## Decisions (locked)

| Topic | Choice |
|---|---|
| Behaviour model | Mirror `HomeHowItWorksSteps` (Approach 1) |
| Sticky chrome | Tabs + copy/CTAs + image only |
| Header | Eyebrow / title / body stay above the track; not sticky |
| Shared hook extract | Out of scope |
| Copy / Figma layout | Unchanged except sticky scroll structure |
| Viewports | All viewports (same as home) |

## Behaviour

1. Measure sticky panel height with `ResizeObserver` (fallback height until measured).
2. Set track height to `panelHeight × TAB_COUNT` (`TAB_COUNT = 4`).
3. Pin the panel with `position: sticky` and `top` from `stickyTopForViewport` (header clearance `86px`, centre when panel fits).
4. On window scroll, map track progress to active tab index (`floor((scrolled / scrollable) * TAB_COUNT)`, clamped).
5. On tab select (click / keyboard), update active index and `window.scrollTo` the matching track offset; use `smooth` unless `prefers-reduced-motion`, then `auto`.
6. Keep existing crossfade for tab copy and image; keep tablist / tabpanel a11y ids.

## Structure

```
<section>                          <!-- PilotInAction -->
  <header>…</header>               <!-- not sticky -->
  <div ref={track} height=track>   <!-- tall scroll track -->
    <div sticky top=stickyTop>
      <PanelChrome>                <!-- measured node -->
        tabs + panel (copy/CTAs + image)
      </PanelChrome>
    </div>
  </div>
</section>
```

Reuse local helpers copied from home pattern: `stickyTopForViewport`, `clampIndex`, `applyPanelHeight` / `onPanelNode`, `syncFromScroll`, `scrollToIndex`. Do not change `PILOT_IN_ACTION` content.

## Testing

- Manual: scroll through the section on desktop and mobile; each of the four tabs activates in order; tab click jumps and stays in sync; reduced-motion jumps without smooth scroll.
- No new automated test required unless an existing Pilot / marketing media test breaks.

## Out of scope

- Extracting a shared sticky-steps hook or chrome component
- Changing tab labels, body copy, media, or CTAs
- Applying this pattern to other Pilot sections
