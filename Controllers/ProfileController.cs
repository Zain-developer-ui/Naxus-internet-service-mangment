using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEXUS.Common.Extensions;
using NEXUS.Models.ViewModels;
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

        [HttpGet]
        public async Task<IActionResult> Edit(CancellationToken ct)
        {
            var userId = User.UserId();
            if (userId is null) return RedirectToAction("Login", "Account");

            var result = await _profiles.GetForEditAsync(userId.Value, ct);
            if (!result.IsSuccess) return NotFound();

            ViewData.SetActiveItem("profile");
            return View(result.Value);
        }

        /**
         * Separate method name because C# treats attributes as no part of the
         * signature - a second Edit() would be CS0111. The route stays /Profile/Edit.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> SaveEdit(ProfileEditViewModel form, CancellationToken ct)
        {
            var userId = User.UserId();
            if (userId is null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(await RedrawAsync(userId.Value, form, ct));

            var result = await _profiles.UpdateAsync(userId.Value, form, ct);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty,
                    result.Message ?? "Your profile could not be saved.");
                return View(await RedrawAsync(userId.Value, form, ct));
            }

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        /**
         * Identity fields are not posted, so a redrawn form would show them blank.
         * Reload them from the account before handing the model back to the view.
         */
        private async Task<ProfileEditViewModel> RedrawAsync(int userId, ProfileEditViewModel form,
            CancellationToken ct)
        {
            var current = await _profiles.GetForEditAsync(userId, ct);
            if (current.IsSuccess && current.Value is not null)
            {
                form.AccountId = current.Value.AccountId;
                form.Email = current.Value.Email;
                form.RoleLabel = current.Value.RoleLabel;
                form.IsStaff = current.Value.IsStaff;
            }

            ViewData.SetActiveItem("profile");
            return form;
        }
    }
}
