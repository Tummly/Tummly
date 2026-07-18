import { describe, expect, it } from "vitest";

import {
  buildOperatorShellPresentation,
  type BuildOperatorShellPresentationInput,
} from "./buildShellPresentation";

function makeShellInput(
  overrides: Partial<BuildOperatorShellPresentationInput> = {},
): BuildOperatorShellPresentationInput {
  return {
    operatorDisplayName: "Mohamed Mahmoud",
    activationExpiresAt: "2026-07-26T12:00:00.000Z",
    locationSwitcherInteractive: true,
    locations: [
      { id: 10, name: "Mehmet's Grill" },
      { id: 11, name: "Second Venue" },
    ],
    selectedLocationId: 10,
    ...overrides,
  };
}

describe("buildOperatorShellPresentation", () => {
  const now = new Date("2026-07-12T12:00:00.000Z");

  it("exposes Activation period badge, Profile, sidebar, and Home title from workspace inputs", () => {
    const presentation = buildOperatorShellPresentation(makeShellInput(), now);

    expect(presentation.activationPeriodBadge).toEqual({
      remaining: "14 days left",
      endsOn: "26 Jul 2026",
      tone: "warning",
    });
    expect(presentation.profileDisplayName).toBe("Mohamed Mahmoud");
    expect(presentation.profileFirstName).toBe("Mohamed");
    expect(presentation.profileInitials).toBe("MM");
    expect(presentation.pageTitle).toBe("Home");
    expect(presentation.omittedNavbarControls).toEqual([
      "search",
      "ai-copilot",
      "help",
    ]);
    expect(presentation.sidebarNav.find((item) => item.id === "home")).toEqual({
      id: "home",
      label: "Home",
      navigable: true,
      active: true,
    });
    expect(
      presentation.sidebarNav
        .filter((item) => item.id !== "home")
        .every((item) => item.navigable === false),
    ).toBe(true);
    expect(presentation.locationSwitcher).toEqual({
      interactive: true,
      selectedLocationId: 10,
      selectedLocationName: "Mehmet's Grill",
      options: [
        { id: 10, name: "Mehmet's Grill" },
        { id: 11, name: "Second Venue" },
      ],
    });
  });

  it("hides the Activation period badge when Activation expiry is missing", () => {
    const presentation = buildOperatorShellPresentation(
      makeShellInput({ activationExpiresAt: null }),
      now,
    );

    expect(presentation.activationPeriodBadge).toBeNull();
  });

  it("presents a non-interactive location switcher for single-location operators", () => {
    const presentation = buildOperatorShellPresentation(
      makeShellInput({
        locationSwitcherInteractive: false,
        locations: [{ id: 5, name: "Solo Kitchen" }],
        selectedLocationId: 5,
      }),
      now,
    );

    expect(presentation.locationSwitcher.interactive).toBe(false);
    expect(presentation.locationSwitcher.selectedLocationName).toBe(
      "Solo Kitchen",
    );
  });
});
