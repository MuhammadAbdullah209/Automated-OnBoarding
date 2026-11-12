namespace AOB.BAL.Interfaces.Common
{
    /// <summary>The signed-in user behind the current request, recorded on activity log entries.</summary>
    public interface ICurrentActor
    {
        Guid? UserAuthId { get; }
        string? Name { get; }
        string? Role { get; }
    }
}
