using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Search;

namespace NEXUS.Controllers
{
    /**
     * The advanced search from the SRS. It sits in its own controller rather
     * than under Retail because every staff console needs it - a technician
     * checking a line and an accounts clerk chasing a bill want the same five
     * filters.
     */
    [Authorize(Roles = NexusRoles.StaffRoles)]
    public class SearchController : Controller
    {
        private readonly IAdvancedSearchService _search;

        public SearchController(IAdvancedSearchService search) => _search = search;

        public async Task<IActionResult> Advanced(AdvancedSearchQuery? query, CancellationToken ct)
        {
            ViewData.SetActiveItem("advanced-search");

            var filters = query ?? new AdvancedSearchQuery();
            var result = await _search.SearchAsync(filters, ct);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new AdvancedSearchResults { Query = filters });
            }

            return View(result.Value!);
        }
    }
}
