using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// SQL Server <c>sp_getapplock</c> exclusive lock for concurrent Guest Form
    /// submits. No-op (<c>true</c>) when the provider is not SQL Server.
    /// </summary>
    public sealed class GuestFormSubmitLock : IGuestFormSubmitLock
    {
        private const int LockTimeoutMs = 10_000;

        public async Task<bool> TryAcquireAsync(
            ApplicationDbContext context,
            int restaurantId,
            string contactKey,
            CancellationToken ct = default
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(contactKey);

            if (!context.Database.IsSqlServer())
            {
                return true;
            }

            var transaction = context.Database.CurrentTransaction
                ?? throw new InvalidOperationException(
                    "Guest form submit lock requires an open transaction."
                );

            var resource =
                "guest-form:"
                + restaurantId.ToString(CultureInfo.InvariantCulture)
                + ":"
                + Sha256Hex(contactKey);

            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await context.Database.OpenConnectionAsync(ct);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "sp_getapplock";
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
                CreateParameter(
                    command,
                    "@LockMode",
                    DbType.String,
                    "Exclusive"
                )
            );
            command.Parameters.Add(
                CreateParameter(
                    command,
                    "@LockOwner",
                    DbType.String,
                    "Transaction"
                )
            );
            command.Parameters.Add(
                CreateParameter(
                    command,
                    "@LockTimeout",
                    DbType.Int32,
                    LockTimeoutMs
                )
            );

            await command.ExecuteNonQueryAsync(ct);

            return returnParam.Value is int code && code >= 0;
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

        private static string Sha256Hex(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
