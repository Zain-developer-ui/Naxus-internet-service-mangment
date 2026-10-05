using Microsoft.AspNetCore.Mvc;
using NEXUS.Models.ViewModels;

namespace NEXUS.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            var vm = new ProfileViewModel
            {
                FullName = "Ahmed Khan",
                Email = "ahmed.khan@example.com",
                Phone = "03001234567",
                Cnic = "35202-1234567-1",
                Address = "House 12, Street 4, Gulberg III, Lahore",
                AvatarUrl = "https://i.pravatar.cc/160?img=12",
                AccountId = "NX12345678",
                MemberSince = "January 2025",
                Documents = new()
                {
                    new() { Name = "CNIC Copy",       FileName = "cnic.pdf",        UploadedOn = "10 Jan 2025" },
                    new() { Name = "Address Proof",   FileName = "address.pdf",     UploadedOn = "10 Jan 2025" },
                    new() { Name = "Application Form",FileName = "application.pdf", UploadedOn = "11 Jan 2025" }
                }
            };
            return View(vm);
        }
    }
}