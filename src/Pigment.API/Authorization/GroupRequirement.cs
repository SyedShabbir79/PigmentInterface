using Microsoft.AspNetCore.Authorization;

namespace Pigment.API.Authorization;

/// <summary>
/// Authorization requirement that passes when the authenticated user
/// belongs to at least one of the configured Entra ID group object IDs.
/// </summary>
public sealed class GroupRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> AllowedGroupObjectIds { get; }

    public GroupRequirement(IEnumerable<string> allowedGroupObjectIds)
    {
        AllowedGroupObjectIds = allowedGroupObjectIds?.ToList() ?? [];
    }
}
