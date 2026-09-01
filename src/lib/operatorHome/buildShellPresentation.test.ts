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
    subscriptionPlan: "Pilot",
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
      choosePlanHref: null,
    });
    expect(presentation.profileDisplayName).toBe("Mohamed Mahmoud");
    expect(presentation.profileFirstName).toBe("Mohamed");
    expect(presentation.profileInitials).toBe("MM");
    expect(presentation.profileSelfRoleSubtitle).toBeNull();
    // Figma shell has no page headline; the body hero owns the h1.
    expect(presentation).not.toHaveProperty("pageTitle");
    expect(presentation.omittedNavbarControls).toEqual([
      "search",
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
            item.id !== "feedback" &&
            item.id !== "campaigns" &&
            item.id !== "offers",
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
    expect(
      presentation.sidebarNav.primary.find((item) => item.id === "campaigns"),
    ).toMatchObject({
      id: "campaigns",
      label: "Campaigns",
      navigable: true,
      active: false,
    });
    expect(
      presentation.sidebarNav.primary.find((item) => item.id === "offers"),
    ).toMatchObject({
      id: "offers",
      label: "Offers",
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
      brandLogoPublicUrl: null,
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

  it("hides the Activation period badge on a paid plan", () => {
    const presentation = buildOperatorShellPresentation(
      makeShellInput({ subscriptionPlan: "Growth" }),
      now,
    );

    expect(presentation.activationPeriodBadge).toBeNull();
  });

  it("exposes Choose a plan href only for Owner with Billing & credits Manage", () => {
    const ownerPresentation = buildOperatorShellPresentation(
      makeShellInput({
        permissionRole: "Owner",
        billingCreditsAccess: "manage",
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );

    expect(ownerPresentation.activationPeriodBadge?.choosePlanHref).toBe(
      "/multi-dashboard/settings/billing-credits/manage-plan?location=10",
    );

    const viewPresentation = buildOperatorShellPresentation(
      makeShellInput({
        permissionRole: "Admin",
        billingCreditsAccess: "view",
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );

    expect(viewPresentation.activationPeriodBadge?.choosePlanHref).toBeNull();

    const billingAdminPresentation = buildOperatorShellPresentation(
      makeShellInput({
        permissionRole: "Billing Admin",
        billingCreditsAccess: "manage",
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );

    expect(
      billingAdminPresentation.activationPeriodBadge?.choosePlanHref
    ).toBeNull();

    const omitAccessPresentation = buildOperatorShellPresentation(
      makeShellInput({
        permissionRole: "Owner",
        billingCreditsAccess: undefined,
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );

    expect(omitAccessPresentation.activationPeriodBadge?.choosePlanHref).toBe(
      "/multi-dashboard/settings/billing-credits/manage-plan?location=10",
    );
  });

  it("shows Lock Alert on Home and Billing & credits for Soft lock Pilot Owner", () => {
    const home = buildOperatorShellPresentation(
      makeShellInput({
        billingStatus: "Soft lock",
        subscriptionPlan: "Pilot",
        permissionRole: "Owner",
        billingCreditsAccess: "manage",
        activeNavId: "home",
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );
    expect(home.lockAlert).toEqual({
      title: "Soft lock",
      body: "Your Pilot period has ended. Paid actions are paused. Existing Feedback links stay live.",
      buttonLabel: "Choose a plan",
      buttonHref:
        "/multi-dashboard/settings/billing-credits/manage-plan?location=10#plan-cards",
    });

    const billing = buildOperatorShellPresentation(
      makeShellInput({
        billingStatus: "Soft lock",
        subscriptionPlan: "Pilot",
        permissionRole: "Owner",
        billingCreditsAccess: "manage",
        activeNavId: "billing-credits",
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );
    expect(billing.lockAlert?.title).toBe("Soft lock");
    expect(billing.lockAlert?.buttonLabel).toBe("Choose a plan");
  });

  it("shows Lock Alert without Button for View", () => {
    const presentation = buildOperatorShellPresentation(
      makeShellInput({
        billingStatus: "Dormant",
        subscriptionPlan: "Pilot",
        permissionRole: "Admin",
        billingCreditsAccess: "view",
        navTargets: { mode: "multi", locationId: 10 },
      }),
      now,
    );

    expect(presentation.lockAlert?.title).toBe("Dormant");
    expect(presentation.lockAlert?.buttonLabel).toBeNull();
    expect(presentation.lockAlert?.buttonHref).toBeNull();
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

  it("passes Paused switcher badge flag through to options", () => {
    const presentation = buildOperatorShellPresentation(
      makeShellInput({
        locations: [
          {
            id: 10,
            name: "Mehmet's Grill",
            address: "Leeds",
            isActive: false,
            showPausedBadge: true,
          },
        ],
        selectedLocationId: 10,
      }),
      now,
    );

    expect(presentation.locationSwitcher.options[0]).toMatchObject({
      id: 10,
      name: "Mehmet's Grill",
      isActive: false,
      showPausedBadge: true,
    });
  });
});
