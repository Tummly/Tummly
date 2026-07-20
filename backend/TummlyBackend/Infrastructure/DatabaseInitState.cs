namespace TummlyBackend.Infrastructure;

/// <summary>
/// Tracks startup DB initialization for the fail-closed <c>/health/ready</c> gate (ADR-0015).
/// </summary>
public sealed class DatabaseInitState
{
    private readonly object _gate = new();
    private DatabaseInitStatus _status = DatabaseInitStatus.NotStarted;

    public DatabaseInitStatus Status
    {
        get
        {
            lock (_gate)
            {
                return _status;
            }
        }
    }

    public void MarkInProgress()
    {
        lock (_gate)
        {
            _status = DatabaseInitStatus.InProgress;
        }
    }

    public void MarkSucceeded()
    {
        lock (_gate)
        {
            _status = DatabaseInitStatus.Succeeded;
        }
    }

    public void MarkFailed()
    {
        lock (_gate)
        {
            _status = DatabaseInitStatus.Failed;
        }
    }
}

public enum DatabaseInitStatus
{
    NotStarted,
    InProgress,
    Succeeded,
    Failed
}
