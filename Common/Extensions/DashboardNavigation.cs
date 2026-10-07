using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NEXUS.Common.Constants;

namespace NEXUS.Common.Extensions;

/**
 * Works out which dashboard an identity belongs to and which sidebar item is
 * active. Views used to set ViewData["UserRole"] by hand, which meant any page
 * that forgot drifted to the sidebar's "Customer" fallback - an administrator
 * landing on an operations screen would suddenly get the customer menu.
 *
 * The role now comes from the claims cookie only. A view can still override the
 * active item, but it can no longer pick the wrong sidebar.
 */
public static class DashboardNavigation
{
    public const string RoleKey = "UserRole";
    public const string ActiveKey = "ActiveSidebar";
    public const string RoleLabelKey = "UserRoleLabel";

    /** Sidebar role for this identity - never "Customer" for staff. */
    public static string SidebarRole(this ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return NexusRoles.Customer;

        var role = user.RoleName();

        return role switch
        {
            NexusRoles.Admin => NexusRoles.Admin,
            NexusRoles.Accounts => NexusRoles.Accounts,
            NexusRoles.Technical => NexusRoles.Technical,
            NexusRoles.Retail => NexusRoles.Retail,
            _ => NexusRoles.Customer
        };
    }

    /** Landing page for a role, used by the navbar and post-login redirect. */
    public static string HomeFor(string? role) => role switch
    {
        NexusRoles.Admin => "/Admin/Dashboard",
        NexusRoles.Accounts => "/Accounts/Dashboard",
        NexusRoles.Technical => "/Technical/Dashboard",
        NexusRoles.Retail => "/Retail/Dashboard",
        _ => "/Customer/Dashboard"
    };

    /** Marks the active sidebar entry, leaving the role choice to SidebarRole(). */
    public static void SetActiveItem(this ViewDataDictionary viewData, string item)
    {
        viewData[ActiveKey] = item;
    }
}
