using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class SelfRoleResolverTests
    {
        [Fact]
        public void Resolve_returns_Role_from_account_created_Trial_Request()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "owner@example.com",
                    role: "owner-operator",
                    isAccountCreated: true,
                    createdAt: DateTime.UtcNow.AddDays(-1)
                ),
            };

            Assert.Equal(
                "owner-operator",
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        [Fact]
        public void Resolve_is_case_insensitive_on_email()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "Owner@Example.com",
                    role: "founder-director",
                    isAccountCreated: true
                ),
            };

            Assert.Equal(
                "founder-director",
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        [Fact]
        public void Resolve_prefers_account_created_Trial_Request_over_others()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "owner@example.com",
                    role: "admin-support",
                    isAccountCreated: false,
                    createdAt: DateTime.UtcNow
                ),
                MakeTrial(
                    email: "owner@example.com",
                    role: "general-manager",
                    isAccountCreated: true,
                    createdAt: DateTime.UtcNow.AddDays(-2)
                ),
            };

            Assert.Equal(
                "general-manager",
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        [Fact]
        public void Resolve_picks_most_recent_when_multiple_account_created()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "owner@example.com",
                    role: "owner-operator",
                    isAccountCreated: true,
                    createdAt: DateTime.UtcNow.AddDays(-10),
                    accountCreatedAt: DateTime.UtcNow.AddDays(-10)
                ),
                MakeTrial(
                    email: "owner@example.com",
                    role: "marketing-growth",
                    isAccountCreated: true,
                    createdAt: DateTime.UtcNow.AddDays(-1),
                    accountCreatedAt: DateTime.UtcNow.AddDays(-1)
                ),
            };

            Assert.Equal(
                "marketing-growth",
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        [Fact]
        public void Resolve_returns_null_when_no_matching_Trial_Request()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "other@example.com",
                    role: "owner-operator",
                    isAccountCreated: true
                ),
            };

            Assert.Null(
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        [Fact]
        public void Resolve_returns_null_when_Trial_Request_is_not_account_created()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "owner@example.com",
                    role: "owner-operator",
                    isAccountCreated: false
                ),
            };

            Assert.Null(
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        [Fact]
        public void Resolve_returns_null_for_blank_Role()
        {
            var trials = new[]
            {
                MakeTrial(
                    email: "owner@example.com",
                    role: "   ",
                    isAccountCreated: true
                ),
            };

            Assert.Null(
                SelfRoleResolver.Resolve("owner@example.com", trials)
            );
        }

        private static TrialRequest MakeTrial(
            string email,
            string role,
            bool isAccountCreated,
            DateTime? createdAt = null,
            DateTime? accountCreatedAt = null
        )
        {
            return new TrialRequest
            {
                BusinessName = "Test",
                BusinessCategory = "cafe",
                Locations = "1",
                FullName = "Test User",
                Email = email,
                Mobile = "07000000000",
                MainLocation = "1 High Street",
                TownCity = "Leeds",
                Postcode = "LS1 1AA",
                Role = role,
                Goal = "Grow",
                TermsAccepted = true,
                IsEmailVerified = true,
                IsAccountCreated = isAccountCreated,
                AccountCreatedAt = accountCreatedAt,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                AccountType = "Single",
            };
        }
    }
}
