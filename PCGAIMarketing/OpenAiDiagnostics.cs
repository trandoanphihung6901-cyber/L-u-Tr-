using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PCGAIMarketing;

public static class OpenAiKey
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var cleaned = value.Trim().Trim('"', '\'', '`');
        cleaned = cleaned
            .Replace("\u200B", string.Empty)
            .Replace("\u200C", string.Empty)
            .Replace("\u200D", string.Empty)
            .Replace("\uFEFF", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\t", string.Empty)
            .Replace(" ", string.Empty);
        return cleaned;
    }

    public static void EnsureLooksValid(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Chưa nhập OpenAI API key.");
        if (!key.StartsWith("sk-", StringComparison.Ordinal))
            throw new InvalidOperationException("API key không đúng định dạng. Key OpenAI phải bắt đầu bằng sk-.");
        if (key.Length < 30)
            throw new InvalidOperationException("API key quá ngắn hoặc đã bị cắt mất một phần.");
    }
}

public sealed class OpenAiDiagnostics
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    public async Task<string> ValidateAsync(string rawKey, string textModel, string imageModel, CancellationToken ct)
    {
        var key = OpenAiKey.Normalize(rawKey);
        OpenAiKey.EnsureLooksValid(key);

        var identity = await SendAsync(HttpMethod.Get, "https://api.openai.com/v1/me", key, null, ct);
        if (!identity.IsSuccessStatusCode)
        {
            // Một số loại key có thể không hỗ trợ /v1/me; kiểm tra bằng danh sách model.
            var modelsFallback = await SendAsync(HttpMethod.Get, "https://api.openai.com/v1/models", key, null, ct);
            if (!modelsFallback.IsSuccessStatusCode)
                throw new InvalidOperationException(FriendlyError(modelsFallback.StatusCode, modelsFallback.Body, modelsFallback.RequestId));
        }

        var models = await SendAsync(HttpMethod.Get, "https://api.openai.com/v1/models", key, null, ct);
        if (!models.IsSuccessStatusCode)
            throw new InvalidOperationException(FriendlyError(models.StatusCode, models.Body, models.RequestId));

        var available = ParseModelIds(models.Body);
        if (available.Count > 0 && !available.Contains(textModel))
            throw new InvalidOperationException($"API key hoạt động nhưng project chưa có quyền dùng model '{textModel}'. Hãy chọn model có trong project hoặc cấp quyền model cho key.");
        if (available.Count > 0 && !available.Contains(imageModel))
            throw new InvalidOperationException($"API key hoạt động nhưng project chưa có quyền dùng model ảnh '{imageModel}'. Hãy cấp quyền model ảnh hoặc chọn model khác.");

        var payload = JsonSerializer.Serialize(new
        {
            model = textModel,
            input = "Trả lời đúng một từ: OK",
            max_output_tokens = 16,
            store = false
        });
        var test = await SendAsync(HttpMethod.Post, "https://api.openai.com/v1/responses", key, payload, ct);
        if (!test.IsSuccessStatusCode)
            throw new InvalidOperationException(FriendlyError(test.StatusCode, test.Body, test.RequestId));

        return "Kết nối OpenAI thành công. API key, project và model nội dung đã được xác nhận.";
    }

    public static string FriendlyError(HttpStatusCode status, string body, string requestId = "")
    {
        var (code, message, type) = ParseError(body);
        var suffix = string.IsNullOrWhiteSpace(requestId) ? string.Empty : $"\nMã yêu cầu: {requestId}";

        if (status == HttpStatusCode.Unauthorized)
            return "API key không hợp lệ, đã bị thu hồi hoặc bị OpenAI vô hiệu hóa. Hãy xóa key cũ, tạo project key mới rồi nhập lại." + suffix;

        if (status == HttpStatusCode.Forbidden)
            return "API key hợp lệ nhưng không có quyền dùng tính năng hoặc model này. Kiểm tra quyền của project key và quyền truy cập model." + suffix;

        if ((int)status == 429)
        {
            if (code.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("billing", StringComparison.OrdinalIgnoreCase))
                return "Tài khoản API chưa có hạn mức, hết số dư hoặc chưa thiết lập thanh toán. ChatGPT Plus không bao gồm chi phí OpenAI API; cần bật Billing cho project API." + suffix;
            return "OpenAI đang giới hạn tốc độ gọi API. Chờ một lúc rồi thử lại hoặc kiểm tra giới hạn của project." + suffix;
        }

        if (status == HttpStatusCode.NotFound && message.Contains("model", StringComparison.OrdinalIgnoreCase))
            return "Không tìm thấy model hoặc project key chưa được cấp quyền dùng model đã chọn. Hãy kiểm tra tên model trong Cài đặt." + suffix;

        if (status == HttpStatusCode.BadRequest)
            return "Yêu cầu gửi lên OpenAI chưa hợp lệ: " + (string.IsNullOrWhiteSpace(message) ? "hãy kiểm tra model và cấu hình project." : message) + suffix;

        if ((int)status >= 500)
            return "Dịch vụ OpenAI đang gặp lỗi tạm thời. Hãy thử lại sau." + suffix;

        return string.IsNullOrWhiteSpace(message)
            ? $"Không thể kết nối OpenAI (HTTP {(int)status}).{suffix}"
            : $"OpenAI báo lỗi: {message}{suffix}";
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, string key, string? json, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Headers.TryAddWithoutValidation("X-Client-Request-Id", Guid.NewGuid().ToString());
        if (json is not null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            var requestId = res.Headers.TryGetValues("x-request-id", out var values) ? values.FirstOrDefault() ?? string.Empty : string.Empty;
            return new ApiResult(res.StatusCode, body, requestId);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("Kết nối OpenAI bị quá thời gian. Kiểm tra Internet, tường lửa hoặc VPN rồi thử lại.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Không kết nối được tới OpenAI. Kiểm tra Internet, DNS, tường lửa hoặc ngày giờ Windows. Chi tiết: " + ex.Message);
        }
    }

    private static HashSet<string> ParseModelIds(string body)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data))
                foreach (var model in data.EnumerateArray())
                    if (model.TryGetProperty("id", out var id) && id.GetString() is { Length: > 0 } value)
                        set.Add(value);
        }
        catch { }
        return set;
    }

    private static (string Code, string Message, string Type) ParseError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("error", out var error)) return (string.Empty, string.Empty, string.Empty);
            return (
                error.TryGetProperty("code", out var c) ? c.ToString() : string.Empty,
                error.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty,
                error.TryGetProperty("type", out var t) ? t.GetString() ?? string.Empty : string.Empty
            );
        }
        catch { return (string.Empty, string.Empty, string.Empty); }
    }

    private sealed record ApiResult(HttpStatusCode StatusCode, string Body, string RequestId)
    {
        public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
    }
}
