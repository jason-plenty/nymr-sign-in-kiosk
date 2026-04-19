using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NymrSignIn.Api.Admin;

public sealed class AdminGroupAuthorizationHandler : AuthorizationHandler<AdminGroupRequirement>
{
    private const string GroupsClaimType = "groups";
    private const string GroupsOverageClaimType = "_claim_names";

    private readonly AdminSettings _adminSettings;
    private readonly ILogger<AdminGroupAuthorizationHandler> _logger;

    public AdminGroupAuthorizationHandler(
        IOptions<AdminSettings> adminSettings,
        ILogger<AdminGroupAuthorizationHandler> logger)
    {
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminGroupRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(_adminSettings.GroupId))
        {
            _logger.LogError("AdminSettings:GroupId is not configured — all admin requests will be denied.");
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(c => c.Type == GroupsOverageClaimType))
        {
            _logger.LogWarning(
                "User {User} token contains groups overage claim; admin access denied. Resolve via Graph API if needed.",
                context.User.Identity?.Name ?? "unknown");
            return Task.CompletedTask;
        }

        var isMember = context.User.Claims
            .Where(c => c.Type == GroupsClaimType)
            .Any(c => string.Equals(c.Value, _adminSettings.GroupId, StringComparison.OrdinalIgnoreCase));

        if (isMember)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
