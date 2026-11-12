using AOB.BAL.Common.Enums;

namespace AOB.BAL.DTOs.Activity
{
    public class ActivityLogDto
    {
        public ActivityTarget Target { get; set; }
        public Guid? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? ActorName { get; set; }
        public string? ActorRole { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
