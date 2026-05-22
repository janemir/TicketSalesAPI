using Prometheus;

namespace AuthService.Metrics;

public static class AuthMetrics
{
    public const string ResultSuccess = "success";
    public const string ResultInvalidCredentials = "invalid_credentials";
    public const string ResultUserServiceUnavailable = "user_service_unavailable";

    public static readonly Counter LoginAttempts = Prometheus.Metrics.CreateCounter(
        "auth_login_attempts_total",
        "Попытки входа в AuthService (проверка через UserService)",
        new CounterConfiguration { LabelNames = new[] { "result" } });

    public static readonly Counter TokensIssued = Prometheus.Metrics.CreateCounter(
        "auth_tokens_issued_total",
        "Успешно выданные JWT-токены");
}
