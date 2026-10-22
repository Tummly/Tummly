import { useCallback, useEffect, useState } from "react"
import { toast } from "sonner"

import {
  ensureDraftCommercialCatalog,
  fetchDraftCommercialCatalog,
  fetchPublishedCommercialCatalog,
  publishCommercialCatalog,
  saveDraftCommercialCatalog,
  type AdminCommercialCatalogVersion,
  type AdminCommercialCustomUnitRate,
  type AdminCommercialPlan,
  type AdminCommercialTopUpPack,
} from "@/api/adminCommercialCatalogApi"
import { Button } from "@/components/ui/button"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"

function penceField(
  label: string,
  value: number,
  onChange: (n: number) => void
) {
  return (
    <FloatingLabelInput
      label={label}
      type="number"
      value={String(value)}
      onChange={(e) => onChange(Number(e.target.value) || 0)}
    />
  )
}

export function AdminCommercialCatalogPanel() {
  const [published, setPublished] =
    useState<AdminCommercialCatalogVersion | null>(null)
  const [draft, setDraft] = useState<AdminCommercialCatalogVersion | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [tab, setTab] = useState("plans")

  const reload = useCallback(async () => {
    setLoading(true)
    try {
      const [pub, dr] = await Promise.all([
        fetchPublishedCommercialCatalog(),
        fetchDraftCommercialCatalog(),
      ])
      setPublished(pub)
      setDraft(dr)
    } catch {
      toast.error("Could not load commercial catalog.")
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void reload()
  }, [reload])

  async function onEnsureDraft() {
    setBusy(true)
    try {
      const next = await ensureDraftCommercialCatalog()
      setDraft(next)
      toast.success("Draft ready.")
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Ensure draft failed.")
    } finally {
      setBusy(false)
    }
  }

  async function onSave() {
    if (draft == null) {
      return
    }
    setBusy(true)
    try {
      const next = await saveDraftCommercialCatalog({
        notes: draft.notes,
        vatRateBps: draft.vatRateBps,
        plans: draft.plans,
        topUpPacks: draft.topUpPacks,
        extraLocation: draft.extraLocation,
        customUnitRates: draft.customUnitRates,
      })
      setDraft(next)
      toast.success("Draft saved.")
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Save failed.")
    } finally {
      setBusy(false)
    }
  }

  async function onPublish() {
    setBusy(true)
    try {
      const next = await publishCommercialCatalog({
        notes: draft?.notes ?? undefined,
      })
      setPublished(next)
      setDraft(null)
      toast.success(`Published ${next.publicId}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Publish failed.")
    } finally {
      setBusy(false)
    }
  }

  function updatePlan(index: number, patch: Partial<AdminCommercialPlan>) {
    if (draft == null) {
      return
    }
    const plans = draft.plans.map((p, i) =>
      i === index ? { ...p, ...patch } : p
    )
    setDraft({ ...draft, plans })
  }

  function updatePack(index: number, patch: Partial<AdminCommercialTopUpPack>) {
    if (draft == null) {
      return
    }
    const topUpPacks = draft.topUpPacks.map((p, i) =>
      i === index ? { ...p, ...patch } : p
    )
    setDraft({ ...draft, topUpPacks })
  }

  function updateRate(
    index: number,
    patch: Partial<AdminCommercialCustomUnitRate>
  ) {
    if (draft == null) {
      return
    }
    const customUnitRates = draft.customUnitRates.map((r, i) =>
      i === index ? { ...r, ...patch } : r
    )
    setDraft({ ...draft, customUnitRates })
  }

  if (loading) {
    return (
      <div className={GUESTS_PAGE_STACK_CLASS}>
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    )
  }

  const editing = draft

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div>
        <h1 className={GUESTS_PAGE_TITLE_CLASS}>Billing catalog</h1>
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          Published: {published?.publicId ?? "none"}. Edit a draft, then
          publish. Revolut variation UUIDs must match live Revolut products when
          you change catalog plan prices.
        </p>
      </div>

      <div className="flex flex-wrap gap-3">
        <Button
          type="button"
          variant="op-secondary"
          disabled={busy}
          onClick={() => void onEnsureDraft()}
        >
          {draft ? "Reload draft from published" : "Create draft"}
        </Button>
        <Button
          type="button"
          variant="op-secondary"
          disabled={busy || draft == null}
          onClick={() => void onSave()}
        >
          Save draft
        </Button>
        <Button
          type="button"
          variant="op-primary"
          disabled={busy || draft == null}
          onClick={() => void onPublish()}
        >
          Publish
        </Button>
      </div>

      {editing == null ? (
        <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
          No draft. Create a draft to edit rates.
        </p>
      ) : (
        <Tabs value={tab} onValueChange={setTab} className="flex flex-col gap-4">
          <TabsList variant="line" className="w-fit">
            <TabsTrigger value="plans">Plans</TabsTrigger>
            <TabsTrigger value="topups">Top-ups</TabsTrigger>
            <TabsTrigger value="extra">Extra location</TabsTrigger>
            <TabsTrigger value="custom">Custom unit rates</TabsTrigger>
          </TabsList>

          <TabsContent value="plans" className="mt-0 space-y-4">
            {editing.plans.map((plan, index) => (
              <section key={plan.id} className={GUESTS_SECTION_CLASS}>
                <h2 className="text-base font-semibold">{plan.displayName}</h2>
                <div className="grid gap-3 md:grid-cols-3">
                  {penceField("Monthly net pence", plan.monthlyNetPence, (n) =>
                    updatePlan(index, { monthlyNetPence: n })
                  )}
                  {penceField("Annual net pence", plan.annualNetPence, (n) =>
                    updatePlan(index, { annualNetPence: n })
                  )}
                  {penceField("Locations", plan.includedLocations, (n) =>
                    updatePlan(index, { includedLocations: n })
                  )}
                  {penceField("Team users", plan.includedTeamMembers, (n) =>
                    updatePlan(index, { includedTeamMembers: n })
                  )}
                  {penceField(
                    "SMS / mo",
                    plan.creditsMonthlySms ?? 0,
                    (n) => updatePlan(index, { creditsMonthlySms: n })
                  )}
                  {penceField(
                    "Email / mo",
                    plan.creditsMonthlyEmail ?? 0,
                    (n) => updatePlan(index, { creditsMonthlyEmail: n })
                  )}
                  {penceField("AI / mo", plan.creditsMonthlyAi ?? 0, (n) =>
                    updatePlan(index, { creditsMonthlyAi: n })
                  )}
                  <FloatingLabelInput
                    label="Revolut variation monthly"
                    value={plan.revolutPlanVariationIdMonthly ?? ""}
                    onChange={(e) =>
                      updatePlan(index, {
                        revolutPlanVariationIdMonthly:
                          e.target.value || null,
                      })
                    }
                  />
                  <FloatingLabelInput
                    label="Revolut variation annual"
                    value={plan.revolutPlanVariationIdAnnual ?? ""}
                    onChange={(e) =>
                      updatePlan(index, {
                        revolutPlanVariationIdAnnual: e.target.value || null,
                      })
                    }
                  />
                </div>
              </section>
            ))}
          </TabsContent>

          <TabsContent value="topups" className="mt-0 space-y-4">
            {editing.topUpPacks.map((pack, index) => (
              <section key={pack.id} className={GUESTS_SECTION_CLASS}>
                <h2 className="text-base font-semibold">
                  {pack.channel} × {pack.quantity}
                </h2>
                <div className="grid gap-3 md:grid-cols-3">
                  {penceField("Net pence", pack.netPence, (n) =>
                    updatePack(index, { netPence: n })
                  )}
                  <FloatingLabelInput
                    label="Lookup key"
                    value={pack.lookupKey}
                    onChange={(e) =>
                      updatePack(index, { lookupKey: e.target.value })
                    }
                  />
                </div>
              </section>
            ))}
          </TabsContent>

          <TabsContent value="extra" className="mt-0">
            {editing.extraLocation ? (
              <section className={GUESTS_SECTION_CLASS}>
                <div className="grid gap-3 md:grid-cols-3">
                  {penceField(
                    "Monthly net pence",
                    editing.extraLocation.monthlyNetPence,
                    (n) =>
                      setDraft({
                        ...editing,
                        extraLocation: {
                          ...editing.extraLocation!,
                          monthlyNetPence: n,
                        },
                      })
                  )}
                  {penceField(
                    "Annual net pence",
                    editing.extraLocation.annualNetPence,
                    (n) =>
                      setDraft({
                        ...editing,
                        extraLocation: {
                          ...editing.extraLocation!,
                          annualNetPence: n,
                        },
                      })
                  )}
                  <FloatingLabelInput
                    label="Revolut variation monthly"
                    value={
                      editing.extraLocation.revolutPlanVariationIdMonthly ?? ""
                    }
                    onChange={(e) =>
                      setDraft({
                        ...editing,
                        extraLocation: {
                          ...editing.extraLocation!,
                          revolutPlanVariationIdMonthly:
                            e.target.value || null,
                        },
                      })
                    }
                  />
                </div>
              </section>
            ) : (
              <p className={GUESTS_PAGE_SUBTITLE_CLASS}>No extra location row.</p>
            )}
          </TabsContent>

          <TabsContent value="custom" className="mt-0 space-y-4">
            {editing.customUnitRates.map((rate, index) => (
              <section key={rate.id} className={GUESTS_SECTION_CLASS}>
                <h2 className="text-base font-semibold">{rate.rateKey}</h2>
                <div className="grid gap-3 md:grid-cols-3">
                  {penceField("Unit quantity", rate.unitQuantity, (n) =>
                    updateRate(index, { unitQuantity: Math.max(1, n) })
                  )}
                  {penceField("Monthly net pence", rate.monthlyNetPence, (n) =>
                    updateRate(index, { monthlyNetPence: n })
                  )}
                  {penceField(
                    "Annual net pence (empty = 15% fallback)",
                    rate.annualNetPence ?? 0,
                    (n) => updateRate(index, { annualNetPence: n || null })
                  )}
                </div>
              </section>
            ))}
          </TabsContent>
        </Tabs>
      )}
    </div>
  )
}
