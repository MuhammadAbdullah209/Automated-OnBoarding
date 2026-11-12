using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Launches a PCI Pal flow with the project's ProjectProvisioningDetails:
    ///   1. POST https://{platform}.cloud/api/v1/token (client credentials) for an access token.
    ///   2. POST https://{region}.{platform}.cloud/api/v1/session/{tenantId}/launch with the flow id.
    /// The session is then shown at https://{region}.{platform}.cloud/session/{tenantId}/view/{sessionId}/framed.
    /// </summary>
    public class FlowLaunchService(
        IProjectRepository projectRepository,
        IActivityLogService activityLog,
        ILogger<FlowLaunchService> logger) : IFlowLaunchService
    {
        private const string LaunchFailed = "The flow could not be launched. Please try again.";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        public async Task<ServiceResult<FlowLaunchDto>> LaunchAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetForCustomerAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<FlowLaunchDto>.Failure("Project not found.");
            }

            var details = await projectRepository.GetProvisioningDetailAsync(projectId);
            if (details is null)
            {
                return ServiceResult<FlowLaunchDto>.Failure("This project has no provisioning details yet. Submit it first.");
            }

            if (!int.TryParse(details.FlowId, out var flowId)
                || string.IsNullOrWhiteSpace(details.Region)
                || string.IsNullOrWhiteSpace(details.TenantId))
            {
                return ServiceResult<FlowLaunchDto>.Failure("The project's provisioning details are incomplete.");
            }

            try
            {
                // 1. Token
                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://{details.Platform}.cloud/api/v1/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["tenantname"] = details.TenantName ?? "",
                        ["username"] = details.Username ?? "",
                        ["client_id"] = details.ClientId ?? "",
                        ["client_secret"] = details.ClientSecret ?? ""
                    })
                };
                tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var tokenResponse = await Http.SendAsync(tokenRequest);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    logger.LogWarning("PCI Pal token request for project {ProjectId} failed with {Status}.",
                        projectId, (int)tokenResponse.StatusCode);
                    return ServiceResult<FlowLaunchDto>.Failure("Could not sign in to PCI Pal with this project's credentials.");
                }

                var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
                if (string.IsNullOrEmpty(token?.AccessToken))
                {
                    return ServiceResult<FlowLaunchDto>.Failure(LaunchFailed);
                }

                // 2. Launch
                var host = $"https://{details.Region}.{details.Platform}.cloud";
                using var launchRequest = new HttpRequestMessage(HttpMethod.Post, $"{host}/api/v1/session/{details.TenantId}/launch")
                {
                    Content = JsonContent.Create(new { FlowId = flowId, InitialValues = new { } })
                };
                launchRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                launchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                using var launchResponse = await Http.SendAsync(launchRequest);
                if (!launchResponse.IsSuccessStatusCode)
                {
                    logger.LogWarning("PCI Pal flow launch for project {ProjectId} failed with {Status}.",
                        projectId, (int)launchResponse.StatusCode);
                    return ServiceResult<FlowLaunchDto>.Failure(LaunchFailed);
                }

                var launch = await launchResponse.Content.ReadFromJsonAsync<LaunchResponse>();
                if (string.IsNullOrEmpty(launch?.Id))
                {
                    return ServiceResult<FlowLaunchDto>.Failure(LaunchFailed);
                }

                await activityLog.LogProjectAsync(customerId, projectId, "Flow launched", "A PCI Pal flow session was started for this project.");

                return ServiceResult<FlowLaunchDto>.Success(new FlowLaunchDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    SessionId = launch.Id,
                    LinkId = launch.LinkId,
                    FrameUrl = $"{host}/session/{details.TenantId}/view/{launch.Id}/framed",
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken ?? ""
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                logger.LogError(ex, "PCI Pal flow launch for project {ProjectId} failed.", projectId);
                return ServiceResult<FlowLaunchDto>.Failure(LaunchFailed);
            }
        }

        private sealed class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
        }

        private sealed class LaunchResponse
        {
            public string? Id { get; set; }

            public string? LinkId { get; set; }
        }
    }
}
