using Microsoft.EntityFrameworkCore;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class SqlServerSmokeTests
    {
        private readonly SqlServerFixture _fixture;

        public SqlServerSmokeTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task Migrations_ApplyOnEmptyDatabase()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var context = _fixture.CreateContext();

            Assert.Empty(await context.Database.GetPendingMigrationsAsync());

            var tableNames = await context
                .Database.SqlQueryRaw<string>(
                    """
                    SELECT TABLE_NAME AS [Value]
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = N'BASE TABLE'
                      AND TABLE_NAME IN (
                        N'OfferIssues',
                        N'CampaignFrozenRecipients',
                        N'CampaignRecipientDeliveries'
                      )
                    """
                )
                .ToListAsync();

            Assert.Contains("OfferIssues", tableNames);
            Assert.Contains("CampaignFrozenRecipients", tableNames);
            Assert.Contains("CampaignRecipientDeliveries", tableNames);
        }
    }
}
