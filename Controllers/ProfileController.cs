using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Extensions;
using NEXUS.Services.Profile;

namespace NEXUS.Controllers
{
    /**
     * One profile screen for everyone. What it shows depends on who is signed
     * in - staff see their role and employee record, customers see their CNIC,
     * address and documents. The page used to serve a customer fixture to
     * administrators.
     */
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profiles;

        public ProfileController(IProfileService profiles) => _profiles = profiles;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var userId = User.UserId();
            if (userId is null) return RedirectToAction("Login", "Account");

            var result = await _profiles.GetAsync(userId.Value, ct);
            if (!result.IsSuccess) return NotFound();

            ViewData.SetActiveItem("profile");
            return View(result.Value);
        }
    }
}
