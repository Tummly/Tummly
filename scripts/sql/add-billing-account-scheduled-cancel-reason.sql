-- Adds cancel-reason columns when migration 20260831134909 was applied empty.
-- Safe to run more than once.

IF COL_LENGTH('BillingAccounts', 'ScheduledCancelReason') IS NULL
BEGIN
    ALTER TABLE BillingAccounts
        ADD ScheduledCancelReason nvarchar(64) NULL;
END;

IF COL_LENGTH('BillingAccounts', 'ScheduledCancelNotes') IS NULL
BEGIN
    ALTER TABLE BillingAccounts
        ADD ScheduledCancelNotes nvarchar(500) NULL;
END;

-- If the empty migration row exists but columns were missing, no further action needed.
-- If the migration row is missing, apply via: dotnet ef database update
