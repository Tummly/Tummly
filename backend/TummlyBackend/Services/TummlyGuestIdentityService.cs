using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class TummlyGuestIdentityService : ITummlyGuestIdentityService
    {
        private readonly ApplicationDbContext _context;

        public TummlyGuestIdentityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProvenContact> FindOrCreateByContactAsync(
            string contactType,
            string normalizedValue,
            CancellationToken ct
        )
        {
            var existing = await _context.TummlyGuestContacts.FirstOrDefaultAsync(
                c => c.ContactType == contactType && c.NormalizedValue == normalizedValue,
                ct
            );
            if (existing != null)
            {
                return new ProvenContact(existing.TummlyGuestId, existing.Id);
            }

            var now = DateTime.UtcNow;
            var contact = new TummlyGuestContact
            {
                ContactType = contactType,
                NormalizedValue = normalizedValue,
                VerifiedAt = now,
                CreatedAt = now,
            };
            var guest = new TummlyGuest
            {
                CreatedAt = now,
                UpdatedAt = now,
            };
            guest.Contacts.Add(contact);

            _context.TummlyGuests.Add(guest);
            await _context.SaveChangesAsync(ct);

            return new ProvenContact(guest.Id, contact.Id);
        }

        public async Task<int> LinkContactAsync(
            int tummlyGuestId,
            string contactType,
            string normalizedValue,
            CancellationToken ct
        )
        {
            var target = await _context.TummlyGuests.FirstOrDefaultAsync(
                g => g.Id == tummlyGuestId,
                ct
            );
            if (target == null)
            {
                throw new InvalidOperationException(
                    "Tummly Guest does not exist."
                );
            }

            var now = DateTime.UtcNow;
            var contact = await _context.TummlyGuestContacts.FirstOrDefaultAsync(
                c => c.ContactType == contactType && c.NormalizedValue == normalizedValue,
                ct
            );

            if (contact == null)
            {
                contact = new TummlyGuestContact
                {
                    TummlyGuestId = target.Id,
                    ContactType = contactType,
                    NormalizedValue = normalizedValue,
                    VerifiedAt = now,
                    CreatedAt = now,
                };
                _context.TummlyGuestContacts.Add(contact);
                target.UpdatedAt = now;
                await _context.SaveChangesAsync(ct);
                return contact.Id;
            }

            if (contact.TummlyGuestId == target.Id)
            {
                return contact.Id;
            }

            // The owner of this contact just verified it for the target guest,
            // so the target guest absorbs the other guest's rows.
            await MergeGuestIntoAsync(contact.TummlyGuestId, target.Id, ct);
            contact.VerifiedAt = now;
            target.UpdatedAt = now;
            await _context.SaveChangesAsync(ct);

            return contact.Id;
        }

        public async Task<IReadOnlyList<LocationGuest>> FindLocationGuestsAsync(
            int tummlyGuestId,
            CancellationToken ct
        )
        {
            var contacts = await _context
                .TummlyGuestContacts.Where(c => c.TummlyGuestId == tummlyGuestId)
                .Select(c => new { c.ContactType, c.NormalizedValue })
                .ToListAsync(ct);

            var emails = contacts
                .Where(c => c.ContactType == TummlyGuestContactTypes.Email)
                .Select(c => c.NormalizedValue)
                .Distinct()
                .ToList();
            var phones = contacts
                .Where(c => c.ContactType == TummlyGuestContactTypes.Phone)
                .Select(c => c.NormalizedValue)
                .Distinct()
                .ToList();

            var matches = new Dictionary<int, LocationGuest>();

            if (emails.Count > 0)
            {
                var byEmail = await LocationGuestMatchQuery()
                    .Where(lg =>
                        lg.MasterGuest != null
                        && lg.MasterGuest.NormalizedEmail != null
                        && emails.Contains(lg.MasterGuest.NormalizedEmail)
                    )
                    .ToListAsync(ct);
                foreach (var lg in byEmail)
                {
                    matches[lg.Id] = lg;
                }
            }

            foreach (var phone in phones)
            {
                var digits = new string(phone.Where(char.IsDigit).ToArray());
                var last9 = digits.Length >= 9 ? digits[^9..] : digits;

                var candidates = await LocationGuestMatchQuery()
                    .Where(lg =>
                        lg.MasterGuest != null
                        && lg.MasterGuest.NormalizedPhone != null
                        && lg.MasterGuest.NormalizedPhone.EndsWith(last9)
                    )
                    .ToListAsync(ct);

                foreach (var lg in candidates)
                {
                    if (GuestPhoneMatch.MasterGuestMatchesE164(lg.MasterGuest!, phone))
                    {
                        matches[lg.Id] = lg;
                    }
                }
            }

            if (matches.Count == 0)
            {
                return [];
            }

            var matchedIds = matches.Keys.ToList();
            var everGrantedIds = (
                await _context
                    .LocationGuestPermissionLedgerEntries.Where(e =>
                        matchedIds.Contains(e.LocationGuestId)
                        && e.EventKind == LocationGuestPermissionLedgerEventKinds.Grant
                        && (
                            e.PermissionKind == LocationGuestPermissionKind.EmailMarketing
                            || e.PermissionKind == LocationGuestPermissionKind.SmsMarketing
                        )
                    )
                    .Select(e => e.LocationGuestId)
                    .Distinct()
                    .ToListAsync(ct)
            ).ToHashSet();

            return matches
                .Values.Where(lg => everGrantedIds.Contains(lg.Id))
                .OrderBy(lg => lg.Id)
                .ToList();
        }

        private IQueryable<LocationGuest> LocationGuestMatchQuery() =>
            _context
                .LocationGuests.Include(lg => lg.MasterGuest)
                .Include(lg => lg.RestaurantLocation)
                .ThenInclude(rl => rl!.Restaurant);

        /// <summary>
        /// Moves every contact, event, and non-duplicate mute from
        /// <paramref name="sourceId"/> to <paramref name="targetId"/>, then removes
        /// the source guest. Tracked changes only; the caller saves once.
        /// </summary>
        private async Task MergeGuestIntoAsync(
            int sourceId,
            int targetId,
            CancellationToken ct
        )
        {
            var source = await _context.TummlyGuests.FirstAsync(g => g.Id == sourceId, ct);

            var sourceContacts = await _context
                .TummlyGuestContacts.Where(c => c.TummlyGuestId == sourceId)
                .ToListAsync(ct);
            foreach (var sourceContact in sourceContacts)
            {
                sourceContact.TummlyGuestId = targetId;
            }

            var targetMuteLocationIds = (
                await _context
                    .TummlyGuestLocationMutes.Where(m => m.TummlyGuestId == targetId)
                    .Select(m => m.RestaurantLocationId)
                    .ToListAsync(ct)
            ).ToHashSet();

            var sourceMutes = await _context
                .TummlyGuestLocationMutes.Where(m => m.TummlyGuestId == sourceId)
                .ToListAsync(ct);
            foreach (var sourceMute in sourceMutes)
            {
                if (targetMuteLocationIds.Add(sourceMute.RestaurantLocationId))
                {
                    sourceMute.TummlyGuestId = targetId;
                }
                else
                {
                    _context.TummlyGuestLocationMutes.Remove(sourceMute);
                }
            }

            var sourceEvents = await _context
                .TummlyGuestMarketingEvents.Where(e => e.TummlyGuestId == sourceId)
                .ToListAsync(ct);
            foreach (var sourceEvent in sourceEvents)
            {
                sourceEvent.TummlyGuestId = targetId;
            }

            _context.TummlyGuests.Remove(source);
        }
    }
}
