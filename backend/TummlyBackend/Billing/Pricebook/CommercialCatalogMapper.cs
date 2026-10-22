using TummlyBackend.Models;

namespace TummlyBackend.Billing.Pricebook
{
    public static class CommercialCatalogMapper
    {
        public static PricebookSnapshot ToSnapshot(CommercialCatalogVersion version)
        {
            var plans = new Dictionary<string, PricebookPlan>(StringComparer.Ordinal);
            foreach (var row in version.Plans)
            {
                plans[row.PlanKey] = new PricebookPlan
                {
                    Key = row.PlanKey,
                    DisplayName = row.DisplayName,
                    MonthlyNetPence = row.MonthlyNetPence,
                    AnnualNetPence = row.AnnualNetPence,
                    IncludedLocations = row.IncludedLocations,
                    IncludedTeamMembers = row.IncludedTeamMembers,
                    ActiveOffersAccount = row.ActiveOffersAccount,
                    ActiveQrPlacementsPerLocation = row.ActiveQrPlacementsPerLocation,
                    PublishedGuestFormsPerLocation = row.PublishedGuestFormsPerLocation,
                    DraftGuestFormsPerLocation = row.DraftGuestFormsPerLocation,
                    CreditsMonthly = BindMonthly(row),
                    CreditsOneTime = BindOneTime(row),
                };
            }

            var topUps = version.TopUpPacks
                .Select(pack => new PricebookTopUpPack
                {
                    Channel = pack.Channel,
                    Quantity = pack.Quantity,
                    NetPence = pack.NetPence,
                    LookupKey = pack.LookupKey,
                    ApprovalRequired = pack.ApprovalRequired,
                })
                .ToList();

            var extra = version.ExtraLocations.FirstOrDefault();

            return new PricebookSnapshot
            {
                Id = version.PublicId,
                Plans = plans,
                TopUpPacks = topUps,
                VatRateBps = version.VatRateBps,
                ExtraGroupLocationMonthlyNetPence = extra?.MonthlyNetPence,
                ExtraGroupLocationAnnualNetPence = extra?.AnnualNetPence,
                ExtraLocationCreditsMonthly = extra == null
                    ? null
                    : new PricebookChannelCredits
                    {
                        Ai = extra.CreditsMonthlyAi,
                        Email = extra.CreditsMonthlyEmail,
                        Sms = extra.CreditsMonthlySms,
                    },
                InternalCostAssumptions = null,
                PaymentProvider = null,
            };
        }

        private static PricebookChannelCredits? BindMonthly(CommercialPlanRow row)
        {
            if (
                row.CreditsMonthlyAi == null
                && row.CreditsMonthlyEmail == null
                && row.CreditsMonthlySms == null
            )
            {
                return null;
            }

            return new PricebookChannelCredits
            {
                Ai = row.CreditsMonthlyAi ?? 0,
                Email = row.CreditsMonthlyEmail ?? 0,
                Sms = row.CreditsMonthlySms ?? 0,
            };
        }

        private static PricebookChannelCredits? BindOneTime(CommercialPlanRow row)
        {
            if (
                row.CreditsOneTimeAi == null
                && row.CreditsOneTimeEmail == null
                && row.CreditsOneTimeSms == null
            )
            {
                return null;
            }

            return new PricebookChannelCredits
            {
                Ai = row.CreditsOneTimeAi ?? 0,
                Email = row.CreditsOneTimeEmail ?? 0,
                Sms = row.CreditsOneTimeSms ?? 0,
            };
        }

        public static CommercialCatalogVersion CloneAsDraft(
            CommercialCatalogVersion source,
            DateTime nowUtc
        )
        {
            var draft = new CommercialCatalogVersion
            {
                Id = Guid.NewGuid(),
                PublicId = $"DRAFT-{Guid.NewGuid():N}"[..32],
                Status = CommercialCatalogStatuses.Draft,
                Notes = source.Notes,
                VatRateBps = source.VatRateBps,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc,
            };

            foreach (var plan in source.Plans)
            {
                draft.Plans.Add(
                    new CommercialPlanRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        PlanKey = plan.PlanKey,
                        DisplayName = plan.DisplayName,
                        MonthlyNetPence = plan.MonthlyNetPence,
                        AnnualNetPence = plan.AnnualNetPence,
                        IncludedLocations = plan.IncludedLocations,
                        IncludedTeamMembers = plan.IncludedTeamMembers,
                        ActiveOffersAccount = plan.ActiveOffersAccount,
                        ActiveQrPlacementsPerLocation =
                            plan.ActiveQrPlacementsPerLocation,
                        PublishedGuestFormsPerLocation =
                            plan.PublishedGuestFormsPerLocation,
                        DraftGuestFormsPerLocation = plan.DraftGuestFormsPerLocation,
                        CreditsMonthlyAi = plan.CreditsMonthlyAi,
                        CreditsMonthlyEmail = plan.CreditsMonthlyEmail,
                        CreditsMonthlySms = plan.CreditsMonthlySms,
                        CreditsOneTimeAi = plan.CreditsOneTimeAi,
                        CreditsOneTimeEmail = plan.CreditsOneTimeEmail,
                        CreditsOneTimeSms = plan.CreditsOneTimeSms,
                        LookupKeyMonthly = plan.LookupKeyMonthly,
                        LookupKeyAnnual = plan.LookupKeyAnnual,
                        RevolutPlanVariationIdMonthly =
                            plan.RevolutPlanVariationIdMonthly,
                        RevolutPlanVariationIdAnnual =
                            plan.RevolutPlanVariationIdAnnual,
                        SelfServeMaxLocations = plan.SelfServeMaxLocations,
                    }
                );
            }

            foreach (var pack in source.TopUpPacks)
            {
                draft.TopUpPacks.Add(
                    new CommercialTopUpPackRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        Channel = pack.Channel,
                        Quantity = pack.Quantity,
                        NetPence = pack.NetPence,
                        LookupKey = pack.LookupKey,
                        ApprovalRequired = pack.ApprovalRequired,
                    }
                );
            }

            foreach (var extra in source.ExtraLocations)
            {
                draft.ExtraLocations.Add(
                    new CommercialExtraLocationRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        MonthlyNetPence = extra.MonthlyNetPence,
                        AnnualNetPence = extra.AnnualNetPence,
                        UsersAdded = extra.UsersAdded,
                        CreditsMonthlyAi = extra.CreditsMonthlyAi,
                        CreditsMonthlyEmail = extra.CreditsMonthlyEmail,
                        CreditsMonthlySms = extra.CreditsMonthlySms,
                        LookupKeyMonthly = extra.LookupKeyMonthly,
                        LookupKeyAnnual = extra.LookupKeyAnnual,
                        RevolutPlanVariationIdMonthly =
                            extra.RevolutPlanVariationIdMonthly,
                        RevolutPlanVariationIdAnnual =
                            extra.RevolutPlanVariationIdAnnual,
                    }
                );
            }

            foreach (var rate in source.CustomUnitRates)
            {
                draft.CustomUnitRates.Add(
                    new CommercialCustomUnitRateRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        RateKey = rate.RateKey,
                        UnitQuantity = rate.UnitQuantity,
                        MonthlyNetPence = rate.MonthlyNetPence,
                        AnnualNetPence = rate.AnnualNetPence,
                    }
                );
            }

            return draft;
        }
    }
}
