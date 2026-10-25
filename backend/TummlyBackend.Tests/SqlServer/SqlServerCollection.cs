namespace TummlyBackend.Tests.SqlServer
{
    [CollectionDefinition(SqlServerCollection.Name)]
    public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
    {
        public const string Name = "SqlServer";
    }
}
