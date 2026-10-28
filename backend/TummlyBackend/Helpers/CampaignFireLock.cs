using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// SQL Server session-scoped <c>sp_getapplock</c> for a Campaign fire run,
    /// keyed by <c>campaign-fire:{campaignId}</c>. Acquire waits zero
    /// milliseconds: a second run returns at once and the sweep retries later.
    /// Non-SQL Server providers get a no-op handle. Same mechanism as
    /// <see cref="GuestReplyLock"/>.
    /// </summary>
    public sealed class CampaignFireLock : ICampaignFireLock
    {
        private readonly ILogger<CampaignFireLock> _logger;

        public CampaignFireLock(ILogger<CampaignFireLock> logger)
        {
            _logger = logger;
        }

        public async Task<IAsyncDisposable?> TryAcquireAsync(
            ApplicationDbContext context,
            int campaignId,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!context.Database.IsSqlServer())
            {
                return NoOpLease.Instance;
            }

            var resource = "campaign-fire:"
                + campaignId.ToString(CultureInfo.InvariantCulture);

            var openedHere = context.Database.GetDbConnection().State
                != ConnectionState.Open;
            if (openedHere)
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
            }

            int returnCode;
            try
            {
                returnCode = await ExecuteAppLockAsync(
                    context,
                    "sp_getapplock",
                    resource,
                    acquire: true,
                    cancellationToken
                );
            }
            catch
            {
                if (openedHere)
                {
                    context.Database.CloseConnection();
                }

                throw;
            }

            if (returnCode < 0)
            {
                if (openedHere)
                {
                    context.Database.CloseConnection();
                }

                return null;
            }

            return new SessionLease(_logger, context, resource, openedHere);
        }

        private static async Task<int> ExecuteAppLockAsync(
            ApplicationDbContext context,
            string procedure,
            string resource,
            bool acquire,
            CancellationToken cancellationToken
        )
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            // Join the ambient transaction when one is open; the connection
            // rejects commands that skip it.
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;

            var returnParam = CreateParameter(
                command,
                "@ReturnCode",
                DbType.Int32,
                null,
                ParameterDirection.ReturnValue
            );
            command.Parameters.Add(returnParam);
            command.Parameters.Add(
                CreateParameter(command, "@Resource", DbType.String, resource)
            );
            command.Parameters.Add(
                CreateParameter(command, "@LockOwner", DbType.String, "Session")
            );

            if (acquire)
            {
                command.Parameters.Add(
                    CreateParameter(command, "@LockMode", DbType.String, "Exclusive")
                );
                command.Parameters.Add(
                    CreateParameter(command, "@LockTimeout", DbType.Int32, 0)
                );
            }

            await command.ExecuteNonQueryAsync(cancellationToken);

            return returnParam.Value is null or DBNull
                ? int.MinValue
                : Convert.ToInt32(returnParam.Value, CultureInfo.InvariantCulture);
        }

        private static DbParameter CreateParameter(
            DbCommand command,
            string name,
            DbType type,
            object? value,
            ParameterDirection direction = ParameterDirection.Input
        )
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Direction = direction;
            if (direction != ParameterDirection.ReturnValue)
            {
                parameter.Value = value ?? DBNull.Value;
            }

            return parameter;
        }

        private sealed class SessionLease : IAsyncDisposable
        {
            private readonly ILogger<CampaignFireLock> _logger;
            private readonly ApplicationDbContext _context;
            private readonly string _resource;
            private readonly bool _closeConnection;
            private bool _released;

            public SessionLease(
                ILogger<CampaignFireLock> logger,
                ApplicationDbContext context,
                string resource,
                bool closeConnection
            )
            {
                _logger = logger;
                _context = context;
                _resource = resource;
                _closeConnection = closeConnection;
            }

            public async ValueTask DisposeAsync()
            {
                if (_released)
                {
                    return;
                }

                _released = true;
                try
                {
                    // A failed release must not replace the run's outcome. The
                    // session lock also ends when the connection closes. Log the
                    // resource name only.
                    try
                    {
                        await ExecuteAppLockAsync(
                            _context,
                            "sp_releaseapplock",
                            _resource,
                            acquire: false,
                            CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Campaign fire lock release failed for {Resource}.",
                            _resource
                        );
                    }
                }
                finally
                {
                    if (_closeConnection)
                    {
                        _context.Database.CloseConnection();
                    }
                }
            }
        }

        private sealed class NoOpLease : IAsyncDisposable
        {
            public static NoOpLease Instance { get; } = new();

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
