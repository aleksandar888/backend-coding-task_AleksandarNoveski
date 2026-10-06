namespace Claims.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly AuditContext _context;

    public AuditService(AuditContext context)
    {
        _context = context;
    }

    public Task AuditClaimAsync(string id, string requestType)
    {
        var audit = new ClaimAudit
        {
            Created = DateTime.Now,
            HttpRequestType = requestType,
            ClaimId = id
        };

        _context.ClaimAudits.Add(audit);
        return _context.SaveChangesAsync();
    }

    public Task AuditCoverAsync(string id, string requestType)
    {
        var audit = new CoverAudit
        {
            Created = DateTime.Now,
            HttpRequestType = requestType,
            CoverId = id
        };

        _context.CoverAudits.Add(audit);
        return _context.SaveChangesAsync();
    }
}
