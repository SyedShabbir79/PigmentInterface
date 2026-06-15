using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Pigment.API.Authorization;

/// <summary>
/// Checks that the authenticated user's token contains at least one
/// "groups" claim matching the configured Entra ID group object IDs.
/// </summary>
public sealed class GroupAuthorizationHandler
    : AuthorizationHandler<GroupRequirement>
{
    private readonly ILogger<GroupAuthorizationHandler> _logger;

    public GroupAuthorizationHandler(ILogger<GroupAuthorizationHandler> logger)
        => _logger = logger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupRequirement requirement)
    {
        var userName  = context.User.Identity?.Name ?? "Unknown";
        var userEmail = context.User.FindFirst("preferred_username")?.Value
                     ?? context.User.FindFirst("email")?.Value
                     ?? "Unknown";

        _logger.LogInformation(
            "Evaluating group authorisation for user {UserName} ({Email})",
            userName, userEmail);

        var userGroups = context.User
            .FindAll("groups")
            .Select(c => c.Value)
            .ToList();

        if (!userGroups.Any())
        {
            _logger.LogWarning(
                "User {UserName} has no 'groups' claims in token. " +
                "Ensure groupMembershipClaims is set in the Entra ID app registration.",
                userName);
            return Task.CompletedTask;
        }

        var matched = userGroups
            .Intersect(requirement.AllowedGroupObjectIds, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matched.Count > 0)
        {
            _logger.LogInformation(
                "User {UserName} authorised via group(s): {Groups}",
                userName, string.Join(", ", matched));
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserName} ({Email}) is NOT in any allowed group. " +
                "UserGroups=[{UserGroups}] AllowedGroups=[{AllowedGroups}]",
                userName, userEmail,
                string.Join(", ", userGroups),
                string.Join(", ", requirement.AllowedGroupObjectIds));
        }

        return Task.CompletedTask;
    }
}
