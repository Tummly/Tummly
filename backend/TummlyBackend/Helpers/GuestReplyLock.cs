using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// SQL Server session-scoped <c>sp_getapplock</c> for guest replies, keyed by
    /// <c>guest-reply:{feedbackId}</c>. The lock belongs to the connection, not to
    /// a transaction, so it does not clash with the Recovery SMS billing ledger,
    /// which opens its own transactions. Non-SQL Server providers get a no-op
    /// handle.
    /// </summary>
    public sealed class GuestReplyLock : IGuestReplyLock
    {
        private const int LockTimeoutMs = 10_000;

        public async Task<IAsyncDisposable?> TryAcquireAsync(
            ApplicationDbContext context,
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!context.Database.IsSqlServer())
            {
                return NoOpLease.Instance;
            }

            var resource = "guest-reply:"
                + feedbackId.ToString(CultureInfo.InvariantCulture);

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

            return new SessionLease(context, resource, openedHere);
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
            // Join the ambient transaction when one is open. Session locks do
            // not depend on it, but the connection rejects commands without it.
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
                    CreateParameter(command, "@LockTimeout", DbType.Int32, LockTimeoutMs)
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
            private readonly ApplicationDbContext _context;
            private readonly string _resource;
            private readonly bool _closeConnection;
            private bool _released;

            public SessionLease(
                ApplicationDbContext context,
                string resource,
                bool closeConnection
            )
            {
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
                    await ExecuteAppLockAsync(
                        _context,
                        "sp_releaseapplock",
                        _resource,
                        acquire: false,
                        CancellationToken.None
                    );
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
