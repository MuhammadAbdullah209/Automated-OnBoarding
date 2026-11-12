namespace AOB.BAL.DTOs.Projects
{
    /// <summary>
    /// A PCI Pal flow session that has been launched for a project. The browser opens it in an
    /// iframe by POSTing the two tokens (as X-BEARER-TOKEN and X-REFRESH-TOKEN) to FrameUrl.
    /// </summary>
    public class FlowLaunchDto
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Session id returned by the launch API.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Link id returned by the launch API.</summary>
        public string? LinkId { get; set; }

        /// <summary>The framed session page, e.g. https://euwest1.pcipalstaging.cloud/session/768/view/{id}/framed.</summary>
        public string FrameUrl { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
