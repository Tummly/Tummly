using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Shared fakes and seeding for the public guest preference endpoint integration tests
    /// (<c>api/public/preferences/*</c>).
    /// </summary>
    public static class GuestPreferencesEndpointTestSupport
    {
        /// <summary>Replaces the email and SMS senders so no test sends for real.</summary>
        public static void ReplaceSenders(IServiceCollection services, IEmailService email)
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton(email);

            services.RemoveAll<ISmsService>();
            services.AddSingleton<ISmsService>(new GuestPreferencesNoOpSmsService());
        }

        public static string SessionSecret(IServiceProvider services) =>
            GuestPreferencesSessionToken.ResolveSecret(services.GetRequiredService<IConfiguration>())
            ?? throw new InvalidOperationException("Session signing secret missing in test host.");

        public static string UnsubscribeSecret(IServiceProvider services) =>
            UnsubscribeLink.ResolveSigningSecret(services.GetRequiredService<IConfiguration>())
            ?? throw new InvalidOperationException("Unsubscribe signing secret missing in test host.");

        public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        /// <summary>
        /// Restaurant (permissions on), one location, a Master Guest for <paramref name="email"/>
        /// and an Allowed Location Guest at that location. Writes no ledger rows.
        /// </summary>
        public static async Task<SeededVenue> SeedVenueAsync(
            ApplicationDbContext context,
            string email,
            string restaurantName,
            string locationName
        )
        {
            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = 1,
                BillingContactUserId = 1,
                PrivacyContactUserId = 1,
                SupportContactUserId = 1,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var masterGuest = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = email,
                NormalizedEmail = email,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(masterGuest);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = masterGuest.Id,
                MasterGuest = masterGuest,
                RestaurantLocationId = location.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            return new SeededVenue(restaurant, location, guest);
        }

        public sealed record SeededVenue(
            Restaurant Restaurant,
            RestaurantLocation Location,
            LocationGuest Guest
        );

        /// <summary>
        /// Captures confirm emails (entry and add-contact links), with <c>IsAddContact</c> set for
        /// the add-contact template. Provider sends run on the background worker, so a start
        /// returns before the email exists.
        /// </summary>
        public sealed class CapturingGuestPreferencesEmailService : EmailServiceStubBase
        {
            private readonly object _lock = new();
            private readonly List<(string ToEmail, string ConfirmUrl, bool IsAddContact)> _sent = new();

            public IReadOnlyList<(string ToEmail, string ConfirmUrl, bool IsAddContact)> Sent
            {
                get
                {
                    lock (_lock)
                    {
                        return _sent.ToList();
                    }
                }
            }

            /// <summary>Waits up to 5 seconds for exactly one email.</summary>
            public async Task<(string ToEmail, string ConfirmUrl, bool IsAddContact)> WaitForSingleAsync()
            {
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (Sent.Count == 0 && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(10);
                }

                return Assert.Single(Sent);
            }

            public override Task SendGuestPreferencesConfirmEmailAsync(
                string toEmail,
                string confirmUrl
            )
            {
                lock (_lock)
                {
                    _sent.Add((toEmail, confirmUrl, false));
                }

                return Task.CompletedTask;
            }

            public override Task SendGuestPreferencesAddContactConfirmEmailAsync(
                string toEmail,
                string confirmUrl
            )
            {
                lock (_lock)
                {
                    _sent.Add((toEmail, confirmUrl, true));
                }

                return Task.CompletedTask;
            }
        }

        private sealed class GuestPreferencesNoOpSmsService : ISmsService
        {
            public Task SendOtpSmsAsync(string phoneNumber) => Task.CompletedTask;

            public Task<bool> VerifyOtpSmsAsync(string phoneNumber, string otp) =>
                Task.FromResult(false);
        }
    }
}
