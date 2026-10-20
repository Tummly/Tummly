using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminCommercialCatalogService : IAdminCommercialCatalogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;
        private readonly DbPricebookCatalog? _dbCatalog;

        public AdminCommercialCatalogService(
            ApplicationDbContext context,
            IAdminAuditService audit,
            IPricebookCatalog pricebook
        )
        {
            _context = context;
            _audit = audit;
            _dbCatalog = pricebook as DbPricebookCatalog;
        }

        public async Task<AdminCommercialCatalogVersionDto?> GetPublishedAsync(
            CancellationToken cancellationToken = default
        )
        {
            var version = await LoadVersionQuery()
                .Where(v => v.Status == CommercialCatalogStatuses.Published)
                .OrderByDescending(v => v.PublishedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            return version == null ? null : ToDto(version);
        }

        public async Task<AdminCommercialCatalogVersionDto?> GetDraftAsync(
            CancellationToken cancellationToken = default
        )
        {
            var version = await LoadVersionQuery()
                .Where(v => v.Status == CommercialCatalogStatuses.Draft)
                .OrderByDescending(v => v.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            return version == null ? null : ToDto(version);
        }

        public async Task<(AdminCommercialCatalogVersionDto? Draft, string? Error)> EnsureDraftAsync(
            int actorAdminUserId,
            CancellationToken cancellationToken = default
        )
        {
            var existing = await LoadVersionQuery()
                .Where(v => v.Status == CommercialCatalogStatuses.Draft)
                .OrderByDescending(v => v.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
            {
                return (ToDto(existing), null);
            }

            var published = await _context.CommercialCatalogVersions
                .Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .Where(v => v.Status == CommercialCatalogStatuses.Published)
                .OrderByDescending(v => v.PublishedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (published == null)
            {
                return (null, "No published commercial catalog to clone.");
            }

            var now = DateTime.UtcNow;
            var draft = CommercialCatalogMapper.CloneAsDraft(published, now);
            _context.CommercialCatalogVersions.Add(draft);
            await _context.SaveChangesAsync(cancellationToken);
            AuditDraftSave(actorAdminUserId, draft.Id, "ensure_draft");
            return (ToDto(await ReloadAsync(draft.Id, cancellationToken)), null);
        }

        public async Task<(AdminCommercialCatalogVersionDto? Draft, string? Error)> SaveDraftAsync(
            int actorAdminUserId,
            AdminCommercialCatalogSaveDraftRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var (draftDto, ensureError) = await EnsureDraftAsync(
                actorAdminUserId,
                cancellationToken
            );
            if (ensureError != null || draftDto == null)
            {
                return (null, ensureError ?? "Draft missing.");
            }

            var draft = await _context.CommercialCatalogVersions
                .Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .FirstAsync(v => v.Id == draftDto.Id, cancellationToken);

            if (request.Notes != null)
            {
                draft.Notes = request.Notes.Trim();
            }

            if (request.VatRateBps != null)
            {
                draft.VatRateBps = request.VatRateBps.Value;
            }

            if (request.Plans != null)
            {
                ApplyPlans(draft, request.Plans);
            }

            if (request.TopUpPacks != null)
            {
                ApplyTopUps(draft, request.TopUpPacks);
            }

            if (request.ExtraLocation != null)
            {
                ApplyExtra(draft, request.ExtraLocation);
            }

            if (request.CustomUnitRates != null)
            {
                ApplyRates(draft, request.CustomUnitRates);
            }

            draft.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            AuditDraftSave(actorAdminUserId, draft.Id, "save_draft");
            return (ToDto(await ReloadAsync(draft.Id, cancellationToken)), null);
        }

        public async Task<(AdminCommercialCatalogVersionDto? Published, string? Error)> PublishAsync(
            int actorAdminUserId,
            AdminCommercialCatalogPublishRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var draft = await _context.CommercialCatalogVersions
                .Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates)
                .Where(v => v.Status == CommercialCatalogStatuses.Draft)
                .OrderByDescending(v => v.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (draft == null)
            {
                return (null, "No draft to publish. Save a draft first.");
            }

            if (draft.Plans.Count == 0)
            {
                return (null, "Draft has no plans.");
            }

            var now = DateTime.UtcNow;
            var publicId = string.IsNullOrWhiteSpace(request.PublicId)
                ? $"TUMMLY-UK-GBP-{now:yyyyMMddHHmmss}"
                : request.PublicId.Trim();

            if (
                await _context.CommercialCatalogVersions.AnyAsync(
                    v => v.PublicId == publicId && v.Id != draft.Id,
                    cancellationToken
                )
            )
            {
                return (null, "Public id is already in use.");
            }

            var previous = await _context.CommercialCatalogVersions
                .Where(v => v.Status == CommercialCatalogStatuses.Published)
                .ToListAsync(cancellationToken);
            foreach (var row in previous)
            {
                row.Status = CommercialCatalogStatuses.Superseded;
            }

            draft.Status = CommercialCatalogStatuses.Published;
            draft.PublicId = publicId;
            draft.PublishedAtUtc = now;
            draft.PublishedByAdminUserId = actorAdminUserId;
            draft.UpdatedAtUtc = now;
            if (request.Notes != null)
            {
                draft.Notes = request.Notes.Trim();
            }

            await _context.SaveChangesAsync(cancellationToken);
            _dbCatalog?.InvalidateCache();

            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.CommercialCatalogPublish,
                    ActorIdentity: $"admin:{actorAdminUserId}",
                    TargetType: AdminAuditTargetTypes.CommercialCatalogVersion,
                    TargetId: draft.Id.ToString(),
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(
                        new { publicId, draft.VatRateBps }
                    )
                )
            );

            return (ToDto(await ReloadAsync(draft.Id, cancellationToken)), null);
        }

        public async Task<PublicCustomUnitRatesDto?> GetPublicCustomUnitRatesAsync(
            CancellationToken cancellationToken = default
        )
        {
            var version = await LoadVersionQuery()
                .Where(v => v.Status == CommercialCatalogStatuses.Published)
                .OrderByDescending(v => v.PublishedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (version == null)
            {
                return null;
            }

            return new PublicCustomUnitRatesDto
            {
                CatalogPublicId = version.PublicId,
                VatRateBps = version.VatRateBps,
                MaxLocations = 500,
                UnitRates = version.CustomUnitRates
                    .OrderBy(r => r.RateKey)
                    .Select(r => new AdminCommercialCustomUnitRateDto
                    {
                        Id = r.Id,
                        RateKey = r.RateKey,
                        UnitQuantity = r.UnitQuantity,
                        MonthlyNetPence = r.MonthlyNetPence,
                        AnnualNetPence = r.AnnualNetPence,
                    })
                    .ToList(),
            };
        }

        private IQueryable<CommercialCatalogVersion> LoadVersionQuery()
        {
            return _context.CommercialCatalogVersions
                .AsNoTracking()
                .Include(v => v.Plans)
                .Include(v => v.TopUpPacks)
                .Include(v => v.ExtraLocations)
                .Include(v => v.CustomUnitRates);
        }

        private async Task<CommercialCatalogVersion> ReloadAsync(
            Guid id,
            CancellationToken cancellationToken
        )
        {
            return await LoadVersionQuery().FirstAsync(v => v.Id == id, cancellationToken);
        }

        private void AuditDraftSave(int actorAdminUserId, Guid draftId, string kind)
        {
            _audit.Append(
                new AdminAuditAppendRequest(
                    Action: AdminAuditActions.CommercialCatalogDraftSave,
                    ActorIdentity: $"admin:{actorAdminUserId}",
                    TargetType: AdminAuditTargetTypes.CommercialCatalogVersion,
                    TargetId: draftId.ToString(),
                    ActorAdminUserId: actorAdminUserId,
                    DetailJson: JsonSerializer.Serialize(new { kind })
                )
            );
        }

        private static void ApplyPlans(
            CommercialCatalogVersion draft,
            List<AdminCommercialPlanDto> plans
        )
        {
            var byKey = draft.Plans.ToDictionary(p => p.PlanKey, StringComparer.Ordinal);
            foreach (var dto in plans)
            {
                if (!byKey.TryGetValue(dto.PlanKey, out var row))
                {
                    row = new CommercialPlanRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        PlanKey = dto.PlanKey,
                    };
                    draft.Plans.Add(row);
                    byKey[dto.PlanKey] = row;
                }

                row.DisplayName = dto.DisplayName;
                row.MonthlyNetPence = dto.MonthlyNetPence;
                row.AnnualNetPence = dto.AnnualNetPence;
                row.IncludedLocations = dto.IncludedLocations;
                row.IncludedTeamMembers = dto.IncludedTeamMembers;
                row.ActiveOffersAccount = dto.ActiveOffersAccount;
                row.ActiveQrPlacementsPerLocation = dto.ActiveQrPlacementsPerLocation;
                row.PublishedGuestFormsPerLocation = dto.PublishedGuestFormsPerLocation;
                row.DraftGuestFormsPerLocation = dto.DraftGuestFormsPerLocation;
                row.CreditsMonthlyAi = dto.CreditsMonthlyAi;
                row.CreditsMonthlyEmail = dto.CreditsMonthlyEmail;
                row.CreditsMonthlySms = dto.CreditsMonthlySms;
                row.CreditsOneTimeAi = dto.CreditsOneTimeAi;
                row.CreditsOneTimeEmail = dto.CreditsOneTimeEmail;
                row.CreditsOneTimeSms = dto.CreditsOneTimeSms;
                row.LookupKeyMonthly = dto.LookupKeyMonthly;
                row.LookupKeyAnnual = dto.LookupKeyAnnual;
                row.RevolutPlanVariationIdMonthly = dto.RevolutPlanVariationIdMonthly;
                row.RevolutPlanVariationIdAnnual = dto.RevolutPlanVariationIdAnnual;
                row.SelfServeMaxLocations = dto.SelfServeMaxLocations;
            }
        }

        private void ApplyTopUps(
            CommercialCatalogVersion draft,
            List<AdminCommercialTopUpPackDto> packs
        )
        {
            static string PackKey(string channel, int quantity) =>
                $"{channel.Trim().ToLowerInvariant()}:{quantity}";

            var byKey = draft.TopUpPacks.ToDictionary(
                p => PackKey(p.Channel, p.Quantity),
                StringComparer.Ordinal
            );
            var keep = new HashSet<string>(StringComparer.Ordinal);

            foreach (var dto in packs)
            {
                var key = PackKey(dto.Channel, dto.Quantity);
                keep.Add(key);
                if (!byKey.TryGetValue(key, out var row))
                {
                    row = new CommercialTopUpPackRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        Channel = dto.Channel,
                        Quantity = dto.Quantity,
                    };
                    draft.TopUpPacks.Add(row);
                    byKey[key] = row;
                }

                row.Channel = dto.Channel;
                row.Quantity = dto.Quantity;
                row.NetPence = dto.NetPence;
                row.LookupKey = dto.LookupKey;
                row.ApprovalRequired = dto.ApprovalRequired;
            }

            foreach (var pack in draft.TopUpPacks.ToList())
            {
                if (keep.Contains(PackKey(pack.Channel, pack.Quantity)))
                {
                    continue;
                }

                draft.TopUpPacks.Remove(pack);
                _context.CommercialTopUpPackRows.Remove(pack);
            }
        }

        private void ApplyExtra(
            CommercialCatalogVersion draft,
            AdminCommercialExtraLocationDto dto
        )
        {
            var row = draft.ExtraLocations.FirstOrDefault();
            if (row == null)
            {
                row = new CommercialExtraLocationRow
                {
                    Id = Guid.NewGuid(),
                    CatalogVersionId = draft.Id,
                };
                draft.ExtraLocations.Add(row);
            }

            row.MonthlyNetPence = dto.MonthlyNetPence;
            row.AnnualNetPence = dto.AnnualNetPence;
            row.UsersAdded = dto.UsersAdded;
            row.CreditsMonthlyAi = dto.CreditsMonthlyAi;
            row.CreditsMonthlyEmail = dto.CreditsMonthlyEmail;
            row.CreditsMonthlySms = dto.CreditsMonthlySms;
            row.LookupKeyMonthly = dto.LookupKeyMonthly;
            row.LookupKeyAnnual = dto.LookupKeyAnnual;
            row.RevolutPlanVariationIdMonthly = dto.RevolutPlanVariationIdMonthly;
            row.RevolutPlanVariationIdAnnual = dto.RevolutPlanVariationIdAnnual;
        }

        private void ApplyRates(
            CommercialCatalogVersion draft,
            List<AdminCommercialCustomUnitRateDto> rates
        )
        {
            var byKey = draft.CustomUnitRates.ToDictionary(
                r => r.RateKey,
                StringComparer.Ordinal
            );
            var keep = new HashSet<string>(StringComparer.Ordinal);

            foreach (var dto in rates)
            {
                keep.Add(dto.RateKey);
                if (!byKey.TryGetValue(dto.RateKey, out var row))
                {
                    row = new CommercialCustomUnitRateRow
                    {
                        Id = Guid.NewGuid(),
                        CatalogVersionId = draft.Id,
                        RateKey = dto.RateKey,
                    };
                    draft.CustomUnitRates.Add(row);
                    byKey[dto.RateKey] = row;
                }

                row.UnitQuantity = Math.Max(1, dto.UnitQuantity);
                row.MonthlyNetPence = dto.MonthlyNetPence;
                row.AnnualNetPence = dto.AnnualNetPence;
            }

            foreach (var existing in draft.CustomUnitRates.ToList())
            {
                if (keep.Contains(existing.RateKey))
                {
                    continue;
                }

                draft.CustomUnitRates.Remove(existing);
                _context.CommercialCustomUnitRateRows.Remove(existing);
            }
        }

        private static AdminCommercialCatalogVersionDto ToDto(CommercialCatalogVersion v)
        {
            return new AdminCommercialCatalogVersionDto
            {
                Id = v.Id,
                PublicId = v.PublicId,
                Status = v.Status,
                PublishedAtUtc = v.PublishedAtUtc,
                Notes = v.Notes,
                VatRateBps = v.VatRateBps,
                Plans = v.Plans
                    .OrderBy(p => p.PlanKey)
                    .Select(p => new AdminCommercialPlanDto
                    {
                        Id = p.Id,
                        PlanKey = p.PlanKey,
                        DisplayName = p.DisplayName,
                        MonthlyNetPence = p.MonthlyNetPence,
                        AnnualNetPence = p.AnnualNetPence,
                        IncludedLocations = p.IncludedLocations,
                        IncludedTeamMembers = p.IncludedTeamMembers,
                        ActiveOffersAccount = p.ActiveOffersAccount,
                        ActiveQrPlacementsPerLocation = p.ActiveQrPlacementsPerLocation,
                        PublishedGuestFormsPerLocation = p.PublishedGuestFormsPerLocation,
                        DraftGuestFormsPerLocation = p.DraftGuestFormsPerLocation,
                        CreditsMonthlyAi = p.CreditsMonthlyAi,
                        CreditsMonthlyEmail = p.CreditsMonthlyEmail,
                        CreditsMonthlySms = p.CreditsMonthlySms,
                        CreditsOneTimeAi = p.CreditsOneTimeAi,
                        CreditsOneTimeEmail = p.CreditsOneTimeEmail,
                        CreditsOneTimeSms = p.CreditsOneTimeSms,
                        LookupKeyMonthly = p.LookupKeyMonthly,
                        LookupKeyAnnual = p.LookupKeyAnnual,
                        RevolutPlanVariationIdMonthly = p.RevolutPlanVariationIdMonthly,
                        RevolutPlanVariationIdAnnual = p.RevolutPlanVariationIdAnnual,
                        SelfServeMaxLocations = p.SelfServeMaxLocations,
                    })
                    .ToList(),
                TopUpPacks = v.TopUpPacks
                    .OrderBy(p => p.Channel)
                    .ThenBy(p => p.Quantity)
                    .Select(p => new AdminCommercialTopUpPackDto
                    {
                        Id = p.Id,
                        Channel = p.Channel,
                        Quantity = p.Quantity,
                        NetPence = p.NetPence,
                        LookupKey = p.LookupKey,
                        ApprovalRequired = p.ApprovalRequired,
                    })
                    .ToList(),
                ExtraLocation = v.ExtraLocations
                    .Select(e => new AdminCommercialExtraLocationDto
                    {
                        Id = e.Id,
                        MonthlyNetPence = e.MonthlyNetPence,
                        AnnualNetPence = e.AnnualNetPence,
                        UsersAdded = e.UsersAdded,
                        CreditsMonthlyAi = e.CreditsMonthlyAi,
                        CreditsMonthlyEmail = e.CreditsMonthlyEmail,
                        CreditsMonthlySms = e.CreditsMonthlySms,
                        LookupKeyMonthly = e.LookupKeyMonthly,
                        LookupKeyAnnual = e.LookupKeyAnnual,
                        RevolutPlanVariationIdMonthly = e.RevolutPlanVariationIdMonthly,
                        RevolutPlanVariationIdAnnual = e.RevolutPlanVariationIdAnnual,
                    })
                    .FirstOrDefault(),
                CustomUnitRates = v.CustomUnitRates
                    .OrderBy(r => r.RateKey)
                    .Select(r => new AdminCommercialCustomUnitRateDto
                    {
                        Id = r.Id,
                        RateKey = r.RateKey,
                        UnitQuantity = r.UnitQuantity,
                        MonthlyNetPence = r.MonthlyNetPence,
                        AnnualNetPence = r.AnnualNetPence,
                    })
                    .ToList(),
            };
        }
    }
}
