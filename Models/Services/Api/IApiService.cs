using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Api
{
    /// <summary>
    /// Abstraction over the backend API.
    /// NEXUS frontend depends only on this interface, so the real
    /// backend can be plugged in without touching the UI layer.
    /// </summary>
    public interface IApiService
    {
        // ---------- Public data ----------
        Task<List<ServiceViewModel>> GetServicesAsync();
        Task<ServiceViewModel?> GetServiceAsync(int id);

        Task<List<PlanViewModel>> GetPlansAsync(string? serviceType = null);
        Task<PlanViewModel?> GetPlanAsync(int id);

        // ---------- Customer / Account ----------
        Task<AccountViewModel?> GetCustomerAsync(string accountId);
        Task<AccountViewModel?> GetCustomerByPhoneAsync(string phone);
        Task<AccountViewModel?> GetCustomerByCnicAsync(string cnic);

        // ---------- Orders ----------
        Task<OrderViewModel?> GetOrderAsync(string orderId);
        Task<bool> SubmitOrderAsync(OrderViewModel order);

        // ---------- Bills & payments ----------
        Task<List<BillViewModel>> GetBillsAsync(string accountId);
        Task<BillDetailsViewModel?> GetBillDetailsAsync(string billNo);
        Task<bool> SubmitPaymentAsync(string billNo, decimal amount, string method);

        // ---------- Feedback ----------
        Task<bool> SubmitFeedbackAsync(FeedbackViewModel feedback);

        // ---------- Customer dashboard ----------
        Task<CustomerDashboardViewModel> GetCustomerDashboardAsync(string accountId);
    }
}