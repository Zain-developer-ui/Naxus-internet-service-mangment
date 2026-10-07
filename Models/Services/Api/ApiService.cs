using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Api
{
    /// <summary>
    /// MOCK implementation of IApiService.
    ///
    /// The backend developer should replace the body of each method
    /// with a real HTTP call, e.g.:
    ///   var res = await _http.GetAsync($"{_baseUrl}plans");
    ///   return await res.Content.ReadFromJsonAsync<List<PlanViewModel>>();
    ///
    /// For now every method returns realistic demo data so that the
    /// frontend can be viewed, tested and demonstrated with no backend.
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly ILogger<ApiService> _logger;

        public ApiService(ILogger<ApiService> logger) => _logger = logger;

        // ================================================================
        // SERVICES
        // ================================================================
        public Task<List<ServiceViewModel>> GetServicesAsync()
        {
            var data = new List<ServiceViewModel>
            {
                new()
                {
                    Id = 1, Name = "Broadband Internet", Category = "Internet",
                    IconClass = "fa-solid fa-wifi",
                    ImageUrl = "~/images/broadband.jpg",
                    ShortDescription = "Always-on high-speed internet for homes and small offices.",
                    FullDescription = "NEXUS Broadband brings reliable, always-on internet to your home with speeds ranging from 5 Mbps up to 30 Mbps. Our copper and hybrid infrastructure delivers consistent performance for browsing, streaming and remote work.",
                    SpeedRange = "5–30 Mbps", Availability = "Nationwide",
                    Features = new() { "Unlimited data", "Free Wi-Fi router", "24/7 support", "No hidden charges" },
                    Benefits = new() { "Consistent speed", "Affordable monthly rental", "Simple installation" }
                },
                new()
                {
                    Id = 2, Name = "Fiber Internet", Category = "Internet",
                    IconClass = "fa-solid fa-bolt",
                    ImageUrl = "~/images/fiber-backbone.jpg",
                    ShortDescription = "Next-generation fiber-optic connectivity for maximum speed.",
                    FullDescription = "NEXUS Fiber uses pure fiber-optic lines to deliver symmetrical speeds up to 100 Mbps with ultra-low latency. Ideal for HD streaming, online gaming and multiple devices working simultaneously.",
                    SpeedRange = "25–100 Mbps", Availability = "Major cities",
                    Features = new() { "Symmetrical upload/download", "Ultra-low latency", "Static IP option", "Business SLA available" },
                    Benefits = new() { "Gaming-grade speed", "Reliable video calls", "Future-proof infrastructure" }
                },
                new()
                {
                    Id = 3, Name = "Wireless Internet", Category = "Internet",
                    IconClass = "fa-solid fa-tower-broadcast",
                    ImageUrl = "~/images/wireless.jpg",
                    ShortDescription = "Fast wireless coverage for areas without wired infrastructure.",
                    FullDescription = "Our wireless network reaches homes and businesses where cable or fiber is unavailable. A discreet outdoor CPE combined with a home Wi-Fi router delivers solid day-to-day performance.",
                    SpeedRange = "4–20 Mbps", Availability = "Selected regions",
                    Features = new() { "No cable needed", "Rapid deployment", "Weatherproof CPE", "Flexible plans" },
                    Benefits = new() { "Fast to install", "Great for remote areas", "Reliable connectivity" }
                },
                new()
                {
                    Id = 4, Name = "Home Internet", Category = "Internet",
                    IconClass = "fa-solid fa-house-signal",
                    ImageUrl = "~/images/home-internet.jpg",
                    ShortDescription = "Family-friendly internet plans for streaming and study.",
                    FullDescription = "NEXUS Home Internet bundles the right speed for families with multiple devices, giving everyone a smooth experience from video calls to cartoons and homework research.",
                    SpeedRange = "10–50 Mbps", Availability = "Nationwide",
                    Features = new() { "Multiple device support", "Parental controls", "Free installation option" },
                    Benefits = new() { "Made for families", "Stream on all screens", "Simple billing" }
                },
                new()
                {
                    Id = 5, Name = "Business Internet", Category = "Internet",
                    IconClass = "fa-solid fa-briefcase",
                    ImageUrl = "~/images/business-office.jpg",
                    ShortDescription = "Dedicated bandwidth for offices, retail and enterprise.",
                    FullDescription = "NEXUS Business Internet delivers priority bandwidth, static IP addressing, uptime SLAs and dedicated account managers so your organization stays online when it matters most.",
                    SpeedRange = "20–200 Mbps", Availability = "Major cities",
                    Features = new() { "Priority bandwidth", "Static IPs", "99.9% uptime SLA", "Dedicated support" },
                    Benefits = new() { "Business-grade reliability", "Account manager", "Scalable upgrades" }
                },
                new()
                {
                    Id = 6, Name = "Dedicated Internet", Category = "Internet",
                    IconClass = "fa-solid fa-server",
                    ImageUrl = "~/images/data-center.jpg",
                    ShortDescription = "Uncontended 1:1 bandwidth for critical operations.",
                    FullDescription = "Pure dedicated internet access — no contention, no downtime surprises. Built for data centers, call centers and enterprises that demand guaranteed throughput.",
                    SpeedRange = "50 Mbps – 1 Gbps", Availability = "On request",
                    Features = new() { "1:1 uncontended", "Redundant links", "24/7 NOC", "Custom SLAs" },
                    Benefits = new() { "Zero contention", "Mission-critical uptime", "Enterprise support" }
                },
                new()
                {
                    Id = 7, Name = "Technical Support", Category = "Support",
                    IconClass = "fa-solid fa-headset",
                    ImageUrl = "~/images/installation.jpg",
                    ShortDescription = "Fast, friendly assistance whenever you need it.",
                    FullDescription = "Our 24/7 support team can be reached by phone, email or through your customer dashboard. Average first-response time is under 15 minutes during business hours.",
                    SpeedRange = "—", Availability = "24/7",
                    Features = new() { "Phone, email & chat", "Remote diagnostics", "Onsite escalation" },
                    Benefits = new() { "Short response times", "Trained agents", "Free for active customers" }
                },
                new()
                {
                    Id = 8, Name = "Installation Service", Category = "Installation",
                    IconClass = "fa-solid fa-screwdriver-wrench",
                    ImageUrl = "~/images/field-team.jpg",
                    ShortDescription = "Professional onsite installation by certified technicians.",
                    FullDescription = "Our certified technicians install your router, run cabling, verify signal quality and confirm activation — usually within a single visit. Same-day slots are available in most cities.",
                    SpeedRange = "—", Availability = "Nationwide",
                    Features = new() { "Certified technicians", "Same-day slots", "Post-install check" },
                    Benefits = new() { "Turnkey setup", "Quality assurance", "Quick activation" }
                },
                new()
                {
                    Id = 9, Name = "Network Maintenance", Category = "Support",
                    IconClass = "fa-solid fa-network-wired",
                    ImageUrl = "~/images/network-maintenance.jpg",
                    ShortDescription = "Scheduled maintenance and proactive network monitoring.",
                    FullDescription = "We monitor our network 24/7 and perform scheduled maintenance to keep your connection fast and stable. Business customers can opt-in to advance maintenance notifications.",
                    SpeedRange = "—", Availability = "Nationwide",
                    Features = new() { "24/7 monitoring", "Scheduled maintenance", "Advance notice" },
                    Benefits = new() { "Stable connection", "Fewer outages", "Proactive fixes" }
                }
            };
            return Task.FromResult(data);
        }

        public async Task<ServiceViewModel?> GetServiceAsync(int id)
        {
            var list = await GetServicesAsync();
            return list.FirstOrDefault(s => s.Id == id);
        }

        // ================================================================
        // PLANS
        // ================================================================
        public Task<List<PlanViewModel>> GetPlansAsync(string? serviceType = null)
        {
            var plans = new List<PlanViewModel>
            {
                new()
                {
                    Id = 1, Name = "BASIC", ServiceType = "Broadband",
                    SpeedMbps = 5, MonthlyRental = 1200m, SecurityDeposit = 450m,
                    Tagline = "Perfect for light browsing",
                    Description = "Entry-level broadband for email, browsing and social media.",
                    ImageUrl = "~/images/plan-basic.jpg",
                    RouterImageUrl = "~/images/router-1.jpg",
                    Features = new() { "Unlimited Data", "Email & Browsing", "Free Router", "24/7 Support" },
                    Benefits = new() { "Lowest monthly rental", "No data caps", "Simple setup" }
                },
                new()
                {
                    Id = 2, Name = "STANDARD", ServiceType = "Broadband",
                    SpeedMbps = 15, MonthlyRental = 2000m, SecurityDeposit = 450m, IsPopular = true,
                    Tagline = "Most popular choice",
                    Description = "Balanced speed for streaming, work and multiple devices.",
                    ImageUrl = "~/images/broadband.jpg",
                    RouterImageUrl = "~/images/router-2.jpg",
                    Features = new() { "Unlimited Data", "HD Streaming", "Social Media", "Free Router", "24/7 Support" },
                    Benefits = new() { "Stream on multiple screens", "Great for families", "Best value" }
                },
                new()
                {
                    Id = 3, Name = "PREMIUM", ServiceType = "Broadband",
                    SpeedMbps = 30, MonthlyRental = 3500m, SecurityDeposit = 450m,
                    Tagline = "Built for power users",
                    Description = "Top-tier speed for 4K streaming, gaming and heavy multitasking.",
                    ImageUrl = "~/images/plan-premium.jpg",
                    RouterImageUrl = "~/images/broadband.jpg",
                    Features = new() { "Unlimited Data", "Gaming & Streaming", "Multiple Devices", "Free Router", "24/7 Support" },
                    Benefits = new() { "Lowest latency", "4K-ready", "Priority support" }
                }
            };

            if (!string.IsNullOrWhiteSpace(serviceType))
                plans = plans.Where(p => p.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase)).ToList();

            return Task.FromResult(plans);
        }

        public async Task<PlanViewModel?> GetPlanAsync(int id)
        {
            var list = await GetPlansAsync();
            return list.FirstOrDefault(p => p.Id == id) ?? list.FirstOrDefault();
        }

        // ================================================================
        // CUSTOMER
        // ================================================================
        public Task<AccountViewModel?> GetCustomerAsync(string accountId)
        {
            // Demo: any value starting with "NX" returns the flagship demo customer.
            var acc = accountId?.ToUpper().StartsWith("NX") == true
                ? new AccountViewModel
                {
                    AccountId = accountId,
                    CustomerName = "Ahmed Khan",
                    Email = "ahmed.khan@example.com",
                    Phone = "03001234567",
                    Cnic = "35202-1234567-1",
                    Address = "House 12, Street 4, Gulberg III, Lahore",
                    ConnectionType = "Broadband",
                    Plan = "Standard 15 Mbps",
                    Speed = "15 Mbps",
                    Status = "Active",
                    BillingStatus = "Paid",
                    InstallationStatus = "Completed",
                    InstallationDate = new DateTime(2025, 1, 10),
                    ActivationDate = new DateTime(2025, 1, 12),
                    RouterModel = "NEXUS Router R2 (Wi-Fi 6)",
                    ServiceArea = "Gulberg III, Lahore",
                    TechnicianName = "Usman Ahmed"
                }
                : null;

            return Task.FromResult(acc);
        }

        public Task<AccountViewModel?> GetCustomerByPhoneAsync(string phone) => GetCustomerAsync("NX12345678");

        public Task<AccountViewModel?> GetCustomerByCnicAsync(string cnic) => GetCustomerAsync("NX12345678");

        // ================================================================
        // ORDERS
        // ================================================================
        public Task<OrderTrackingViewModel?> GetOrderAsync(string orderId)
        {
            var placed = DateTime.Today.AddDays(-2);

            var tracking = new OrderTrackingViewModel
            {
                OrderId = orderId,
                CustomerName = "Ahmed Khan",
                ConnectionType = "Broadband",
                PlanName = "Broadband 30 Hours",
                City = "Lahore",
                Address = "House 12, Street 4, Gulberg III",
                Status = "Awaiting Feasibility Check",
                StatusBadge = "badge-warning",
                PlacedAt = placed,
                ScheduledFor = DateTime.Today.AddDays(3),
                MonthlyRate = 45m,
                SecurityDeposit = 500m,
                Timeline = new[]
                {
                    new TrackingStep("Order Received", "We have received your application.", "done"),
                    new TrackingStep("Feasibility Check", "Our team is verifying coverage at your address.", "active"),
                    new TrackingStep("Installation Scheduled", "We will contact you to confirm a slot.", ""),
                    new TrackingStep("Connection Activated", "You will be notified once the line is live.", "")
                }
            };

            return Task.FromResult<OrderTrackingViewModel?>(tracking);
        }

        // ================================================================
        // BILLS
        // ================================================================
        public Task<List<BillViewModel>> GetBillsAsync(string accountId)
        {
            var bills = new List<BillViewModel>
            {
                new() { BillNo = "INV-1003", Period = "Sep 2025", Amount = 2450m, Status = "Due",     DueDate = new DateTime(2025,10,25) },
                new() { BillNo = "INV-1002", Period = "Aug 2025", Amount = 2450m, Status = "Paid",    DueDate = new DateTime(2025, 9,25), PaidOn = new DateTime(2025,9,12) },
                new() { BillNo = "INV-1001", Period = "Jul 2025", Amount = 2450m, Status = "Paid",    DueDate = new DateTime(2025, 8,25), PaidOn = new DateTime(2025,8,10) }
            };
            return Task.FromResult(bills);
        }

        public Task<BillDetailsViewModel?> GetBillDetailsAsync(string billNo)
        {
            var vm = new BillDetailsViewModel
            {
                BillNo = billNo,
                AccountId = "NX12345678",
                CustomerName = "Ahmed Khan",
                BillingPeriod = "Sep 2025",
                BillDate = new DateTime(2025, 9, 1),
                DueDate = new DateTime(2025, 9, 25),
                Status = "Due",
                MonthlyRental = 2000m,
                InstallationCharges = 0m,
                SecurityDeposit = 450m,
                PreviousDues = 0m,
                Taxes = 0m,
                Discounts = 0m,
                Adjustments = 0m,
                PaymentHistory = new()
                {
                    new() { BillNo = "INV-1002", PaidOn = new DateTime(2025, 9, 12), Amount = 2450m, Method = "Credit Card",   Status = "Paid" },
                    new() { BillNo = "INV-1001", PaidOn = new DateTime(2025, 8, 10), Amount = 2450m, Method = "Bank Transfer", Status = "Paid" }
                }
            };
            return Task.FromResult<BillDetailsViewModel?>(vm);
        }

        public Task<bool> SubmitPaymentAsync(string billNo, decimal amount, string method)
        {
            _logger.LogInformation("Mock payment: {Bill} / {Amount} / {Method}", billNo, amount, method);
            return Task.FromResult(true);
        }

        // ================================================================
        // FEEDBACK
        // ================================================================
        public Task<bool> SubmitFeedbackAsync(FeedbackViewModel feedback)
        {
            _logger.LogInformation("Mock feedback: {Rating} stars / {Category}",
                feedback.OverallRating, feedback.Category);
            return Task.FromResult(true);
        }

        // ================================================================
        // CUSTOMER DASHBOARD (aggregated)
        // ================================================================
        public async Task<CustomerDashboardViewModel> GetCustomerDashboardAsync(string accountId)
        {
            var account = await GetCustomerAsync(accountId) ?? new AccountViewModel();
            var bills = await GetBillsAsync(accountId);

            var vm = new CustomerDashboardViewModel
            {
                Account = account,
                CurrentBill = bills.FirstOrDefault(b => b.Status != "Paid"),
                RecentBills = bills.Take(3).ToList(),
                RecentPayments = new()
                {
                    new() { BillNo = "INV-1002", PaidOn = new DateTime(2025, 9, 12), Amount = 2450m, Method = "Credit Card",   Status = "Paid" },
                    new() { BillNo = "INV-1001", PaidOn = new DateTime(2025, 8, 10), Amount = 2450m, Method = "Bank Transfer", Status = "Paid" },
                    new() { BillNo = "INV-1000", PaidOn = new DateTime(2025, 7, 14), Amount = 2450m, Method = "JazzCash",      Status = "Paid" }
                },
                RecentActivity = new()
                {
                    new() { Icon = "fa-file-invoice", Title = "Bill generated",       Description = "Invoice INV-1003 for Sep 2025", When = "2 days ago",  Tone = "info" },
                    new() { Icon = "fa-circle-check", Title = "Payment received",     Description = "Rs. 2,450 paid via Credit Card", When = "Sep 12",     Tone = "success" },
                    new() { Icon = "fa-signal",       Title = "Speed upgrade",        Description = "Upgraded to Standard 15 Mbps",  When = "Aug 1",      Tone = "info" },
                    new() { Icon = "fa-screwdriver",  Title = "Maintenance complete", Description = "Network maintenance in your area", When = "Jul 20",   Tone = "success" }
                },
                UsageLabels = new() { "Apr", "May", "Jun", "Jul", "Aug", "Sep" },
                UsageValues = new() { 120, 145, 138, 172, 190, 205 },   // GB
                PaymentLabels = new() { "Apr", "May", "Jun", "Jul", "Aug", "Sep" },
                PaymentValues = new() { 2450, 2450, 2450, 2450, 2450, 0 } // 0 = unpaid Sep
            };

            return vm;
        }
    }
}