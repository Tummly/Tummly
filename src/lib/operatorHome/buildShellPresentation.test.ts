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
      { id: 10, name: "Mehmet's Grill", address: "Leeds", isActive: true },
      { id: 11, name: "Second Venue", address: "Manchester", isActive: true },
    ],
    selectedLocationId: 10,
    ...overrides,
  };
}

describe("buildOperatorShellPresentation", () => {
  const now = new Date("2026-07-12T12:00:00.000Z");

  it("exposes Activation period badge, Profile, and sidebar from workspace inputs", () => {
    const presentation = buildOperatorShellPresentation(makeShellInput(), now);

    expect(presentation.activationPeriodBadge).toEqual({
      remaining: "14 days left",
      endsOn: "26 Jul 2026",
      tone: "warning",
    });
    expect(presentation.profileDisplayName).toBe("Mohamed Mahmoud");
    expect(presentation.profileFirstName).toBe("Mohamed");
    expect(presentation.profileInitials).toBe("MM");
    expect(presentation.profileSelfRoleSubtitle).toBeNull();
    // Figma shell has no page headline; the body hero owns the h1.
    expect(presentation).not.toHaveProperty("pageTitle");
    expect(presentation.omittedNavbarControls).toEqual([
      "search",
      "ai-copilot",
      "help",
    ]);
    expect(
      presentation.sidebarNav.primary.find((item) => item.id === "home"),
    ).toEqual({
      id: "home",
      label: "Home",
      navigable: true,
      active: true,
      to: undefined,
    });
    expect(
      presentation.sidebarNav.primary
        .filter(
          (item) =>
            item.id !== "home" &&
            item.id !== "guests" &&
            item.id !== "capture" &&
            item.id !== "feedback",
        )
        .every((item) => item.navigable === false),
    ).toBe(true);
    expect(
      presentation.sidebarNav.primary.find((item) => item.id === "guests"),
    ).toMatchObject({
      id: "guests",
      label: "Guests",
      navigable: true,
      active: false,
    });
    expect(
      presentation.sidebarNav.primary.find((item) => item.id === "capture"),
    ).toMatchObject({
      id: "capture",
      label: "Capture",
      navigable: true,
      active: false,
    });
    expect(
      presentation.sidebarNav.primary.find((item) => item.id === "feedback"),
    ).toMatchObject({
      id: "feedback",
      label: "Feedback",
      navigable: true,
      active: false,
    });
    expect(presentation.sidebarNav.settings).toMatchObject({
      id: "settings",
      navigable: false,
      active: false,
      forceExpanded: false,
    });
    expect(presentation.sidebarNav.footer).toEqual([
      {
        id: "tummly-shop",
        label: "Tummly Shop",
        navigable: false,
        active: false,
      },
    ]);
    expect(presentation.locationSwitcher).toEqual({
      interactive: true,
      selectedLocationId: 10,
      selectedLocationName: "Mehmet's Grill",
      options: [
        { id: 10, name: "Mehmet's Grill", address: "Leeds", isActive: true },
        { id: 11, name: "Second Venue", address: "Manchester", isActive: true },
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
        locations: [{ id: 5, name: "Solo Kitchen", address: "", isActive: true }],
        selectedLocationId: 5,
      }),
      now,
    );

    expect(presentation.locationSwitcher.interactive).toBe(false);
    expect(presentation.locationSwitcher.selectedLocationName).toBe(
      "Solo Kitchen",
    );
  });

  it("exposes a normalized Self role subtitle for the account trigger", () => {
    const presentation = buildOperatorShellPresentation(
      makeShellInput({ selfRole: "owner-operator" }),
      now,
    );

    expect(presentation.profileSelfRoleSubtitle).toBe("Owner");
  });
});
