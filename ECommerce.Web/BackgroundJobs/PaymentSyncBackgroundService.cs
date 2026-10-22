using Ecommerce.Domain.Models.Contracts.UOW;
using Ecommerce.Domain.Models.Orders;
using Ecommerce.Service.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Stripe;

namespace ECommerce.Web.BackgroundJobs
{
    /// <summary>
    /// Background job định kỳ đồng bộ trạng thái giao dịch thanh toán từ Stripe về hệ thống,
    /// đảm bảo không thất lạc đơn hàng chưa được xác nhận và chống xử lý trùng lặp giao dịch (Idempotency).
    /// </summary>
    public class PaymentSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentSyncBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConnectionMultiplexer _redis;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5);

        public PaymentSyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PaymentSyncBackgroundService> logger,
            IConfiguration configuration,
            IConnectionMultiplexer redis)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentSyncBackgroundService is starting.");

            // Đợi 30 giây đầu tiên để ứng dụng hoàn tất khởi động và seed data
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            using var timer = new PeriodicTimer(_period);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SyncPendingPaymentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing PaymentSyncBackgroundService.");
                }
            }

            _logger.LogInformation("PaymentSyncBackgroundService is stopping.");
        }

        private async Task SyncPendingPaymentsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var secretKey = _configuration["StripeSettings:secretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("your_stripe_secret_key_here"))
            {
                return;
            }
            StripeConfiguration.ApiKey = secretKey;

            // Lấy các đơn hàng pending tạo cách đây ít nhất 10 phút để tránh race condition với thanh toán vừa tạo
            var cutoffTime = DateTimeOffset.Now.AddMinutes(-10);
            var orderRepo = unitOfWork.GetRepository<Ecommerce.Domain.Models.Orders.Order, Guid>();
            var pendingOrders = await orderRepo.GetAllWithSpecificatonsAsync(new PendingOrdersSpecification(cutoffTime));

            var ordersList = pendingOrders.ToList();
            if (ordersList.Count == 0)
                return;

            _logger.LogInformation("Found {Count} pending orders to reconcile with Stripe.", ordersList.Count);

            var intentService = new PaymentIntentService();
            var redisDb = _redis.GetDatabase();

            foreach (var order in ordersList)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var lockKey = $"lock:payment:{order.PaymentIntentId}";

                // Sử dụng Redis Distributed Lock để chống chạy trùng với Webhook hoặc Worker khác
                var acquired = await redisDb.StringSetAsync(lockKey, "locked", TimeSpan.FromSeconds(30), When.NotExists);
                if (!acquired)
                {
                    continue;
                }

                try
                {
                    // Truy vấn trực tiếp từ Stripe API
                    var paymentIntent = await intentService.GetAsync(order.PaymentIntentId, cancellationToken: stoppingToken);
                    if (paymentIntent == null)
                        continue;

                    // Kiểm tra lại trạng thái hiện tại (Idempotency check)
                    var currentOrder = await orderRepo.GetByIdAsync(order.Id);
                    if (currentOrder == null || currentOrder.Status != OrderStatus.pending)
                        continue;

                    if (paymentIntent.Status == "succeeded")
                    {
                        currentOrder.Status = OrderStatus.PaymentReceived;
                        orderRepo.update(currentOrder);
                        await unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("Reconciled order {OrderId} to PaymentReceived via background job.", order.Id);
                    }
                    else if (paymentIntent.Status == "canceled" || paymentIntent.Status == "requires_payment_method" && order.OrderDate <= DateTimeOffset.Now.AddHours(-24))
                    {
                        // Đơn quá 24h không thanh toán thành công hoặc bị hủy bên Stripe
                        currentOrder.Status = OrderStatus.PaymentFailed;
                        orderRepo.update(currentOrder);
                        await unitOfWork.SaveChangesAsync();
                        _logger.LogInformation("Reconciled order {OrderId} to PaymentFailed via background job.", order.Id);
                    }
                }
                catch (StripeException sEx)
                {
                    _logger.LogWarning(sEx, "Stripe API error for PaymentIntent {IntentId}", order.PaymentIntentId);
                }
                finally
                {
                    await redisDb.KeyDeleteAsync(lockKey);
                }
            }
        }
    }
}
