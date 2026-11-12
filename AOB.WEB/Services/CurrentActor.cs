using System.Security.Claims;
using AOB.BAL.Interfaces.Common;

namespace AOB.WEB.Services
{
    /// <summary>The signed-in user of the current request (empty outside a request, e.g. at startup).</summary>
    public class CurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
    {
        private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        public Guid? UserAuthId
            => Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        public string? Name => User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

        public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
