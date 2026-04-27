namespace VinhKhanhGuide.Core.Contracts;

public sealed class AuditLogCreateRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public sealed class AuditLogDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public AuditLogDto Clone()
    {
        return new AuditLogDto
        {
            Id = Id,
            UserId = UserId,
            Username = Username,
            Action = Action,
            EntityName = EntityName,
            EntityId = EntityId,
            Description = Description,
            IpAddress = IpAddress,
            CreatedAtUtc = CreatedAtUtc
        };
    }
}
