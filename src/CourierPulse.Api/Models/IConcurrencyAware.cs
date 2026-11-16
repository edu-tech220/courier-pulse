namespace CourierPulse.Api.Models;

/// <summary>
/// Implemented by entities that use an application-managed concurrency token instead of a
/// database-generated rowversion, so optimistic concurrency works the same way on SQL Server
/// (production) and SQLite (tests).
/// </summary>
public interface IConcurrencyAware
{
    byte[] RowVersion { get; set; }
}
