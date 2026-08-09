import { describe, expect, it, vi, type Mock } from "vitest"

import {
  CAMPAIGNS_LOAD_ERROR_MESSAGE,
  CAMPAIGNS_PAGE_COPY,
  createOperatorCampaignsPageModule,
  type OperatorCampaignsPageAdapters,
} from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"
import {
  DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import type {
  CampaignRecommendationResponse,
  CampaignsListResponse,
} from "@/types/operatorCampaigns"

function emptyListResponse(
  overrides: Partial<CampaignsListResponse> = {}
): CampaignsListResponse {
  return {
    success: true,
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 25,
    tabCounts: {
      all: 0,
      needsAttention: 0,
      drafts: 0,
      inFlight: 0,
      sent: 0,
    },
    ...overrides,
  }
}

function draftListItem(
  overrides: Partial<CampaignsListResponse["items"][number]> & {
    id: number
    name: string
  }
): CampaignsListResponse["items"][number] {
  return {
    status: "draft",
    goalId: "thank-recent-guests",
    locationId: 42,
    locationName: "Camden",
    channel: "email",
    audienceKey: "all-eligible-guests",
    offerStance: "no-offer",
    updatedAt: "2026-08-08T10:00:00.000Z",
    sendDate: null,
    delivery: null,
    engagement: null,
    redemptions: null,
    ...overrides,
  }
}

function createAdapters(
  overrides: Partial<OperatorCampaignsPageAdapters> & {
    loadCampaignsList?: Mock<OperatorCampaignsPageAdapters["loadCampaignsList"]>
    loadMarketingEligible?: Mock<
      OperatorCampaignsPageAdapters["loadMarketingEligible"]
    >
    loadCampaignRecommendation?: Mock<
      OperatorCampaignsPageAdapters["loadCampaignRecommendation"]
    >
  } = {}
): OperatorCampaignsPageAdapters {
  return {
    loadCampaignsList:
      overrides.loadCampaignsList
      ?? vi.fn(async () => emptyListResponse()),
    loadMarketingEligible:
      overrides.loadMarketingEligible
      ?? vi.fn(async () => 42),
    loadCampaignRecommendation:
      overrides.loadCampaignRecommendation
      ?? vi.fn(async () => ({
        success: true,
        recommendation: { type: "none" },
      }) satisfies CampaignRecommendationResponse),
    getCampaignsOverviewDateRange:
      overrides.getCampaignsOverviewDateRange
      ?? (() => DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE),
    debounceMs: overrides.debounceMs ?? 0,
    getNow: overrides.getNow,
    loadMessagingBalances: overrides.loadMessagingBalances,
  }
}

describe("createOperatorCampaignsPageModule", () => {
  it("loads true-empty overview chrome for a location with no campaigns", async () => {
    const loadCampaignsList = vi.fn(async () => emptyListResponse())
    const loadMarketingEligible = vi.fn(async () => 12)
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList, loadMarketingEligible })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(loadCampaignsList).toHaveBeenCalledWith({
      locationId: 42,
      view: "all",
      q: undefined,
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(loadMarketingEligible).toHaveBeenCalledWith({
      locationId: 42,
      overviewDateRange: DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
    })
    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel).toMatchObject({
      locationId: 42,
      locationName: "Camden",
      isTrueEmpty: true,
      dateRangeLabel: "Last 30 days",
      selectedDateRange: DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE,
      header: {
        createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
        useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
      },
      list: {
        activeViewId: "all",
        showListChrome: false,
        empty: {
          kind: "true-empty",
          title: CAMPAIGNS_PAGE_COPY.trueEmptyTitle,
          helper: CAMPAIGNS_PAGE_COPY.trueEmptyHelper,
          createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
          useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
        },
      },
    })
    expect(snapshot.viewModel?.list.tabs.map((tab) => tab.id)).toEqual([
      "all",
      "needs-attention",
      "drafts",
      "in-flight",
      "sent",
    ])
    expect(
      snapshot.viewModel?.list.tabs.find((tab) => tab.id === "all")?.showCount
    ).toBe(false)
    expect(snapshot.viewModel?.summary.kpis).toEqual([
      {
        id: "marketing-eligible",
        label: CAMPAIGNS_PAGE_COPY.marketingEligibleLabel,
        description: CAMPAIGNS_PAGE_COPY.marketingEligibleDescription,
        value: 12,
      },
      {
        id: "campaigns-in-flight",
        label: CAMPAIGNS_PAGE_COPY.campaignsInFlightLabel,
        description: CAMPAIGNS_PAGE_COPY.campaignsInFlightDescription,
        value: 3,
      },
      {
        id: "messages-sent",
        label: CAMPAIGNS_PAGE_COPY.messagesSentLabel,
        description: CAMPAIGNS_PAGE_COPY.messagesSentDescription,
        value: 1842,
      },
      {
        id: "campaign-attributed-redemptions",
        label: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsLabel,
        description: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsDescription,
        value: 0,
      },
    ])
  })

  it("exposes Figma Messaging usage fixture numbers for Channel-step reuse", async () => {
    const pageModule = createOperatorCampaignsPageModule(createAdapters())

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const messagingUsage = pageModule.getSnapshot().viewModel?.messagingUsage
    expect(messagingUsage?.status).toBe("ready")
    expect(messagingUsage?.viewModel).toMatchObject({
      title: "Messaging usage",
      email: {
        used: 3240,
        allowance: 10000,
        remaining: 6760,
        usageLine: "3,240 of 10,000 used",
        detailLine: "6,760 remaining · Refreshes 15 August",
        meterMaxLabel: "10,000",
      },
      sms: {
        total: 420,
        reserved: 120,
        available: 300,
        usageLine: "420 total",
        detailLine: "120 reserved · 300 available",
        meterMaxLabel: "300",
      },
      plan: {
        name: "Growth",
        locationCount: 3,
        planLine: "Growth · 3 locations",
        billingLine: "Billed monthly · Next refresh 15 August",
      },
    })
  })

  it("maps live Billing balances for Messaging usage and retries after load-failed", async () => {
    const loadMessagingBalances = vi
      .fn()
      .mockRejectedValueOnce(new Error("billing down"))
      .mockResolvedValueOnce({
        email: {
          used: 100,
          allowance: 500,
          remaining: 400,
          refreshLabel: "1 September",
        },
        sms: { total: 50, reserved: 10, available: 40 },
        plan: {
          name: "Starter",
          locationCount: 1,
          billingLine: "Billed monthly · Next refresh 1 September",
        },
        ai: { available: 12 },
        softLocked: false,
      })

    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadMessagingBalances })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel?.messagingUsage).toEqual({
      status: "load-failed",
      viewModel: null,
      errorMessage: "Could not load messaging usage. Please try again.",
    })

    await pageModule.retryMessagingUsage()

    expect(pageModule.getSnapshot().viewModel?.messagingUsage).toMatchObject({
      status: "ready",
      viewModel: {
        plan: { name: "Starter" },
        email: { remaining: 400 },
        sms: { available: 40 },
      },
      errorMessage: null,
    })
  })

  it("surfaces load error and recovers on retry", async () => {
    const loadCampaignsList = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce(emptyListResponse())
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Soho" }],
    })

    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "error",
      viewModel: null,
      loadError: CAMPAIGNS_LOAD_ERROR_MESSAGE,
    })

    await pageModule.retryLoad()

    expect(pageModule.getSnapshot()).toMatchObject({
      loadStatus: "loaded",
      loadError: null,
      viewModel: {
        locationId: 7,
        isTrueEmpty: true,
      },
    })
  })

  it("reloads when the selected Owned location changes", async () => {
    const loadCampaignsList = vi.fn(async () => emptyListResponse())
    const loadMarketingEligible = vi.fn(async () => 5)
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList, loadMarketingEligible })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })
    await pageModule.syncWorkspace({
      selectedLocationId: 2,
      locations: [
        { id: 1, locationName: "Camden" },
        { id: 2, locationName: "Soho" },
      ],
    })

    expect(loadCampaignsList).toHaveBeenNthCalledWith(1, {
      locationId: 1,
      view: "all",
      q: undefined,
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(loadCampaignsList).toHaveBeenNthCalledWith(2, {
      locationId: 2,
      view: "all",
      q: undefined,
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(pageModule.getSnapshot().viewModel?.locationName).toBe("Soho")
  })

  it("refetches Marketing eligible on date-window change while sibling mock KPIs stay fixed", async () => {
    let range: CampaignsOverviewDateRange = DEFAULT_CAMPAIGNS_OVERVIEW_DATE_RANGE
    const loadCampaignsList = vi.fn(async () => emptyListResponse())
    const loadMarketingEligible = vi.fn(
      async (input: {
        locationId: number
        overviewDateRange: CampaignsOverviewDateRange
      }) => (input.overviewDateRange.kind === "all-time" ? 99 : 12)
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({
        loadCampaignsList,
        loadMarketingEligible,
        getCampaignsOverviewDateRange: () => range,
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(loadCampaignsList).toHaveBeenCalledTimes(1)
    expect(loadMarketingEligible).toHaveBeenCalledTimes(1)
    const firstKpis = pageModule.getSnapshot().viewModel?.summary.kpis
    expect(firstKpis?.find((kpi) => kpi.id === "marketing-eligible")?.value).toBe(
      12
    )
    const mockValuesBefore = firstKpis
      ?.filter((kpi) => kpi.id !== "marketing-eligible")
      .map((kpi) => ({ id: kpi.id, value: kpi.value }))

    range = { kind: "all-time" }
    await pageModule.reloadForOverviewDateRange()

    expect(loadCampaignsList).toHaveBeenCalledTimes(1)
    expect(loadMarketingEligible).toHaveBeenCalledTimes(2)
    expect(loadMarketingEligible).toHaveBeenLastCalledWith({
      locationId: 42,
      overviewDateRange: { kind: "all-time" },
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.viewModel?.dateRangeLabel).toBe("All time")
    expect(snapshot.viewModel?.selectedDateRange).toEqual({ kind: "all-time" })
    expect(
      snapshot.viewModel?.summary.kpis.find(
        (kpi) => kpi.id === "marketing-eligible"
      )?.value
    ).toBe(99)
    expect(
      snapshot.viewModel?.summary.kpis
        .filter((kpi) => kpi.id !== "marketing-eligible")
        .map((kpi) => ({ id: kpi.id, value: kpi.value }))
    ).toEqual(mockValuesBefore)
  })

  it("selects view-scoped empty when All has drafts but Needs attention is empty", async () => {
    const loadCampaignsList = vi.fn(
      async (params: { view: string }): Promise<CampaignsListResponse> =>
        emptyListResponse({
          totalCount: 0,
          tabCounts: {
            all: 2,
            drafts: 2,
            needsAttention: 0,
            inFlight: 0,
            sent: 0,
          },
          ...(params.view === "all"
            ? {
                totalCount: 2,
                items: [
                  draftListItem({ id: 1, name: "A" }),
                  draftListItem({ id: 2, name: "B" }),
                ],
              }
            : {}),
        })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel?.isTrueEmpty).toBe(false)
    expect(pageModule.getSnapshot().viewModel?.list.showListChrome).toBe(true)

    await pageModule.setListView("needs-attention")

    expect(loadCampaignsList).toHaveBeenLastCalledWith({
      locationId: 42,
      view: "needs-attention",
      q: undefined,
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(pageModule.getSnapshot().viewModel?.list).toMatchObject({
      activeViewId: "needs-attention",
      showListChrome: true,
      empty: {
        kind: "view-scoped",
        title: "No campaigns need attention",
      },
    })
  })

  it("selects filter-search empty when search returns no rows", async () => {
    const loadCampaignsList = vi.fn(
      async (params: {
        q?: string
      }): Promise<CampaignsListResponse> =>
        emptyListResponse({
          totalCount: params.q ? 0 : 2,
          items: params.q
            ? []
            : [
                draftListItem({ id: 1, name: "Weekend brunch" }),
                draftListItem({ id: 2, name: "Lunch special" }),
              ],
          tabCounts: {
            all: 2,
            drafts: 2,
            needsAttention: 0,
            inFlight: 0,
            sent: 0,
          },
        })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList, debounceMs: 0 })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.setSearchQuery("xyz")
    await vi.waitFor(() => {
      expect(pageModule.getSnapshot().viewModel?.list.empty?.kind).toBe(
        "filter-search"
      )
    })

    expect(loadCampaignsList).toHaveBeenLastCalledWith({
      locationId: 42,
      view: "all",
      q: "xyz",
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(pageModule.getSnapshot().viewModel?.list).toMatchObject({
      searchQuery: "xyz",
      searchMissLabel: "No campaigns found for “xyz”",
      empty: {
        kind: "filter-search",
        title: CAMPAIGNS_PAGE_COPY.filterSearchTitle,
        helper: CAMPAIGNS_PAGE_COPY.filterSearchHelper,
        viewAllCampaignsLabel: CAMPAIGNS_PAGE_COPY.viewAllCampaigns,
        clearAllFiltersLabel: CAMPAIGNS_PAGE_COPY.clearAllFilters,
      },
    })
  })

  it("viewAllCampaigns resets to All and clears search", async () => {
    const loadCampaignsList = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [
          draftListItem({ id: 1, name: "Draft" }),
        ],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList, debounceMs: 0 })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    await pageModule.setListView("drafts")
    pageModule.setSearchQuery("miss")
    await vi.waitFor(() => {
      expect(loadCampaignsList).toHaveBeenCalledWith(
        expect.objectContaining({ view: "drafts", q: "miss" })
      )
    })

    await pageModule.viewAllCampaigns()

    expect(loadCampaignsList).toHaveBeenLastCalledWith({
      locationId: 42,
      view: "all",
      q: undefined,
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(pageModule.getSnapshot().viewModel?.list.activeViewId).toBe("all")
    expect(pageModule.getSnapshot().viewModel?.list.searchQuery).toBe("")
  })

  it("maps Draft list items into table rows after Save", async () => {
    const loadCampaignsList = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [
          draftListItem({
            id: 9,
            name: "Tuesday lunch reminder",
            goalId: "boost-quieter-time",
            channel: "sms",
          }),
        ],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({
        loadCampaignsList,
        getNow: () => new Date("2026-08-08T12:00:00.000Z"),
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const list = pageModule.getSnapshot().viewModel?.list
    expect(list?.empty).toBeNull()
    expect(list?.showListChrome).toBe(true)
    expect(list?.tabs.find((tab) => tab.id === "drafts")?.count).toBe(1)
    expect(list?.rows).toEqual([
      expect.objectContaining({
        id: 9,
        name: "Tuesday lunch reminder",
        metaLine: "Boost a quieter time · Updated 2 hours ago",
        statusLabel: "Draft",
        locationName: "Camden",
        channelLabel: "SMS",
        sendDateLabel: "—",
        deliveryLabel: "—",
        engagementLabel: "—",
        redemptionsLabel: "—",
        status: "draft",
      }),
    ])
  })

  it("loads a success recommendation onto the overview card", async () => {
    const loadCampaignRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: {
        type: "thank-recent-guests" as const,
        title: "Thank guests who recently joined",
        opportunity: "Several guests joined recently.",
        eligibleAudience: "New guests with permission.",
        whyBullets: ["Have a valid marketing permission"],
        suggestedChannel: "email" as const,
        estimatedUsage: "Within current allowance",
        echoedCounts: {
          marketingEligible: 12,
          allGuests: 40,
          newGuests: 5,
          needsRecovery: 1,
          positiveFeedback: 8,
          dormantGuests: 2,
        },
        draftPrefill: {
          goalId: "thank-recent-guests",
          audienceKey: "new-guests",
          channel: "email",
          offerStance: "no-offer",
          campaignName: "Thank you",
          messageSubject: "Thanks",
          messageBody: "Thank you for joining.",
        },
        locationName: "Camden",
      },
    }))
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignRecommendation })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(loadCampaignRecommendation).toHaveBeenCalledWith({
      request: expect.objectContaining({
        locationId: 42,
        overviewDatePreset: "last30",
        refresh: false,
      }),
    })
    const recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.status).toBe("ready")
    expect(recommendation?.isNone).toBe(false)
    expect(recommendation?.recommendation?.title).toBe(
      "Thank guests who recently joined"
    )
    expect(recommendation?.recommendation?.echoedCounts?.marketingEligible).toBe(
      12
    )
  })

  it("maps type none to the empty recommendation card state", async () => {
    const loadCampaignRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: { type: "none" as const },
    }))
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignRecommendation })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    const recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.status).toBe("ready")
    expect(recommendation?.isNone).toBe(true)
    expect(recommendation?.recommendation).toBeNull()
  })

  it("shows fail+retry and refreshes on retryRecommendation", async () => {
    const loadCampaignRecommendation = vi
      .fn()
      .mockResolvedValueOnce({
        success: false,
        message: "upstream failed",
        retryable: true,
      })
      .mockResolvedValueOnce({
        success: true,
        recommendation: {
          type: "re-engage" as const,
          title: "Re-engage dormant guests",
          opportunity: "Quiet guests.",
          eligibleAudience: "Dormant guests.",
          whyBullets: ["No recent activity"],
          suggestedChannel: "sms" as const,
          estimatedUsage: "Within current allowance",
          echoedCounts: {
            marketingEligible: 3,
            allGuests: 10,
            newGuests: 0,
            needsRecovery: 0,
            positiveFeedback: 0,
            dormantGuests: 4,
          },
          draftPrefill: {
            goalId: "re-engage-inactive",
            audienceKey: "dormant-guests",
            channel: "sms",
            offerStance: "no-offer",
            campaignName: "We miss you",
            messageSubject: null,
            messageBody: "Come back soon.",
          },
        },
      })

    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignRecommendation })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    let recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.status).toBe("error")
    expect(recommendation?.errorRetryable).toBe(true)

    await pageModule.retryRecommendation()

    expect(loadCampaignRecommendation).toHaveBeenCalledTimes(2)
    expect(loadCampaignRecommendation).toHaveBeenLastCalledWith({
      request: expect.objectContaining({
        locationId: 42,
        refresh: true,
      }),
    })
    recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.status).toBe("ready")
    expect(recommendation?.recommendation?.type).toBe("re-engage")
  })

  it("Not now hides the recommendation for the session without calling the API again", async () => {
    const loadCampaignRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: {
        type: "thank-recent-guests" as const,
        title: "Thank guests who recently joined",
        opportunity: "Several guests joined recently.",
        eligibleAudience: "New guests with permission.",
        whyBullets: ["Have a valid marketing permission"],
        suggestedChannel: "email" as const,
        estimatedUsage: "Within current allowance",
        echoedCounts: {
          marketingEligible: 12,
          allGuests: 40,
          newGuests: 5,
          needsRecovery: 1,
          positiveFeedback: 8,
          dormantGuests: 2,
        },
        draftPrefill: {
          goalId: "thank-recent-guests",
          audienceKey: "new-guests",
          channel: "email",
          offerStance: "no-offer",
          campaignName: "Thank you",
          messageSubject: "Thanks",
          messageBody: "Thank you for joining.",
        },
        locationName: "Camden",
      },
    }))
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignRecommendation })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })
    expect(loadCampaignRecommendation).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().viewModel?.recommendation.status).toBe(
      "ready"
    )

    pageModule.dismissRecommendation()

    expect(loadCampaignRecommendation).toHaveBeenCalledTimes(1)
    const recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.status).toBe("dismissed")
    expect(recommendation?.recommendation).toBeNull()
    expect(recommendation?.showAudiencePanel).toBe(false)

    await pageModule.reloadForOverviewDateRange()
    expect(loadCampaignRecommendation).toHaveBeenCalledTimes(1)
    expect(pageModule.getSnapshot().viewModel?.recommendation.status).toBe(
      "dismissed"
    )
  })

  it("View eligible audience toggles the panel with echoed live counts", async () => {
    const loadCampaignRecommendation = vi.fn(async () => ({
      success: true,
      recommendation: {
        type: "thank-recent-guests" as const,
        title: "Thank guests who recently joined",
        opportunity: "Several guests joined recently.",
        eligibleAudience: "New guests with permission.",
        whyBullets: ["Have a valid marketing permission"],
        suggestedChannel: "email" as const,
        estimatedUsage: "Within current allowance",
        echoedCounts: {
          marketingEligible: 12,
          allGuests: 40,
          newGuests: 5,
          needsRecovery: 1,
          positiveFeedback: 8,
          dormantGuests: 2,
        },
        draftPrefill: {
          goalId: "thank-recent-guests",
          audienceKey: "new-guests",
          channel: "email",
          offerStance: "no-offer",
          campaignName: "Thank you",
          messageSubject: "Thanks",
          messageBody: "Thank you for joining.",
        },
      },
    }))
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignRecommendation })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.openRecommendationAudience()

    let recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.showAudiencePanel).toBe(true)
    expect(recommendation?.recommendation?.echoedCounts).toEqual({
      marketingEligible: 12,
      allGuests: 40,
      newGuests: 5,
      needsRecovery: 1,
      positiveFeedback: 8,
      dormantGuests: 2,
    })
    expect(CAMPAIGNS_PAGE_COPY.recommendationAudienceDisclaimer).toContain(
      "not full Campaign eligibility"
    )

    pageModule.closeRecommendationAudience()
    recommendation = pageModule.getSnapshot().viewModel?.recommendation
    expect(recommendation?.showAudiencePanel).toBe(false)
  })

  it("applyFilters sends filter params and resets page", async () => {
    const loadCampaignsList = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [draftListItem({ id: 1, name: "Draft" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.applyFilters({
      status: { kind: "multi-select", ids: ["draft"] },
      channel: { kind: "multi-select", ids: ["sms"] },
      location: { kind: "location-scope", value: { kind: "none" } },
      goal: { kind: "multi-select", ids: [] },
      offerStance: { kind: "multi-select", ids: [] },
      createdBy: { kind: "multi-select", ids: [] },
      deliveryIssue: { kind: "multi-select", ids: [] },
      date: { kind: "date", value: { kind: "none" } },
    })

    await vi.waitFor(() => {
      expect(loadCampaignsList).toHaveBeenLastCalledWith(
        expect.objectContaining({
          status: ["draft"],
          channel: ["sms"],
          page: 1,
          sort: "recent-activity",
        })
      )
    })
    expect(pageModule.getSnapshot().viewModel?.list.filterChipCount).toBeGreaterThan(
      0
    )
  })

  it("setSortId resets page and requests sort key", async () => {
    const loadCampaignsList = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [draftListItem({ id: 1, name: "Draft" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.setSortId("name-az")

    await vi.waitFor(() => {
      expect(loadCampaignsList).toHaveBeenLastCalledWith(
        expect.objectContaining({
          sort: "name-az",
          page: 1,
        })
      )
    })
    expect(pageModule.getSnapshot().viewModel?.list.sortId).toBe("name-az")
  })

  it("selects filter-search empty when filters return no rows", async () => {
    const loadCampaignsList = vi.fn(
      async (params: { status?: string[] }): Promise<CampaignsListResponse> =>
        emptyListResponse({
          totalCount: params.status ? 0 : 1,
          items: params.status
            ? []
            : [draftListItem({ id: 1, name: "Draft" })],
          tabCounts: {
            all: 1,
            drafts: 1,
            needsAttention: 0,
            inFlight: 0,
            sent: 0,
          },
        })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.applyFilters({
      status: { kind: "multi-select", ids: ["failed"] },
      channel: { kind: "multi-select", ids: [] },
      location: { kind: "location-scope", value: { kind: "none" } },
      goal: { kind: "multi-select", ids: [] },
      offerStance: { kind: "multi-select", ids: [] },
      createdBy: { kind: "multi-select", ids: [] },
      deliveryIssue: { kind: "multi-select", ids: [] },
      date: { kind: "date", value: { kind: "none" } },
    })

    await vi.waitFor(() => {
      expect(pageModule.getSnapshot().viewModel?.list.empty?.kind).toBe(
        "filter-search"
      )
    })
  })

  it("clearSearchAndFilters clears search and applied filters", async () => {
    const loadCampaignsList = vi.fn(async () =>
      emptyListResponse({
        totalCount: 1,
        items: [draftListItem({ id: 1, name: "Draft" })],
        tabCounts: {
          all: 1,
          drafts: 1,
          needsAttention: 0,
          inFlight: 0,
          sent: 0,
        },
      })
    )
    const pageModule = createOperatorCampaignsPageModule(
      createAdapters({ loadCampaignsList })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    pageModule.applyFilters({
      status: { kind: "multi-select", ids: ["draft"] },
      channel: { kind: "multi-select", ids: [] },
      location: { kind: "location-scope", value: { kind: "none" } },
      goal: { kind: "multi-select", ids: [] },
      offerStance: { kind: "multi-select", ids: [] },
      createdBy: { kind: "multi-select", ids: [] },
      deliveryIssue: { kind: "multi-select", ids: [] },
      date: { kind: "date", value: { kind: "none" } },
    })
    pageModule.setSearchQuery("miss")
    await vi.waitFor(() => {
      expect(loadCampaignsList).toHaveBeenCalledWith(
        expect.objectContaining({ q: "miss", status: ["draft"] })
      )
    })

    await pageModule.clearSearchAndFilters()

    expect(loadCampaignsList).toHaveBeenLastCalledWith({
      locationId: 42,
      view: "all",
      q: undefined,
      sort: "recent-activity",
      page: 1,
      pageSize: 25,
    })
    expect(pageModule.getSnapshot().viewModel?.list.filterChipCount).toBe(0)
    expect(pageModule.getSnapshot().viewModel?.list.searchQuery).toBe("")
  })

  it("exposes messaging usage anchor and campaign help URL on header", async () => {
    const pageModule = createOperatorCampaignsPageModule(createAdapters())

    await pageModule.syncWorkspace({
      selectedLocationId: 42,
      locations: [{ id: 42, locationName: "Camden" }],
    })

    expect(pageModule.getSnapshot().viewModel?.header).toMatchObject({
      messagingUsageAnchorId: "campaigns-messaging-usage",
      campaignHelpUrl: "/help-center/articles/campaigns",
    })
  })
})
