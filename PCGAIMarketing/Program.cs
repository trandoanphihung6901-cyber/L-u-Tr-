using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PCGAIMarketing;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show($"Ứng dụng gặp lỗi ngoài dự kiến.\n\n{e.Exception.Message}", "PCG AI Marketing", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
        app.Run(new MainWindow());
    }
}

public sealed class BrandSettings
{
    public string CompanyName { get; set; } = "PHÚ CƯỜNG GROUP";
    public string Slogan { get; set; } = "CHUẨN TỪ MÓNG – VỮNG TỪ TÂM";
    public string Website { get; set; } = "phucuonggroups.com";
    public string Hotline { get; set; } = "0905 233 978 – 0905 263 048";
    public string Contact { get; set; } = "Thảo – 0903 570 014";
    public string Address { get; set; } = "1216 Lê Hồng Phong, Phường Nam Nha Trang, Khánh Hòa";
    public string Footer { get; set; } = "_____________\nPHÚ CƯỜNG GROUP DESIGN & BUILD\nCHUẨN TỪ MÓNG – VỮNG TỪ TÂM\n✦ Thiết kế độc bản\n✦ Thi công chuẩn mực\n✦ Nhà bê tông nguyên khối\n✦ Xây nhà trọn gói\n✦ Giá trị bền vững\n📍 1216 Lê Hồng Phong, Phường Nam Nha Trang, Khánh Hòa\n🌐 phucuonggroups.com\n☎ 0905 233 978 – 0905 263 048";
}

public sealed class AppSettings
{
    public BrandSettings Brand { get; set; } = new();
    public string OpenAiKey { get; set; } = "";
    public string TextModel { get; set; } = "gpt-5-mini";
    public string ImageModel { get; set; } = "gpt-image-1";
    public string MetaVersion { get; set; } = "v25.0";
    public List<string> DefaultTimes { get; set; } = ["07:30", "11:30", "15:30", "19:30"];
    public bool SchedulerPaused { get; set; }
}

public sealed class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Topic { get; set; } = "";
    public string Category { get; set; } = "Công trình thực tế";
    public string Objective { get; set; } = "Tạo niềm tin";
    public string Title { get; set; } = "";
    public string Hook { get; set; } = "";
    public string Body { get; set; } = "";
    public string Cta { get; set; } = "";
    public string Hashtags { get; set; } = "";
    public string Footer { get; set; } = "";
    public string ImagePrompt { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string Status { get; set; } = "Bản nháp";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string FullCaption => string.Join("\n\n", new[] { Hook, Body, Cta, Footer, Hashtags }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class FanPage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string PageId { get; set; } = "";
    public string Token { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed class ScheduleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentId { get; set; }
    public Guid PageId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = "Đã lên lịch";
    public int AttemptCount { get; set; }
    public string LastError { get; set; } = "";
    public string PublishedId { get; set; } = "";
}

public sealed class AppData
{
    public AppSettings Settings { get; set; } = new();
    public List<ContentItem> Contents { get; set; } = [];
    public List<FanPage> Pages { get; set; } = [];
    public List<ScheduleItem> Schedules { get; set; } = [];
    public List<string> Logs { get; set; } = [];
}

public static class Crypto
{
    public static string Protect(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
    }

    public static string Unprotect(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        try
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
        }
        catch { return ""; }
    }
}

public sealed class StorageService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    public string Folder { get; }
    public string ImagesFolder { get; }
    private string DataFile => Path.Combine(Folder, "pcg-data.json");

    public StorageService()
    {
        Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhuCuongGroup", "PCGAIMarketing");
        ImagesFolder = Path.Combine(Folder, "images");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(ImagesFolder);
    }

    public async Task<AppData> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(DataFile)) return new AppData();
            var text = await File.ReadAllTextAsync(DataFile);
            return JsonSerializer.Deserialize<AppData>(text, _json) ?? new AppData();
        }
        catch
        {
            try { File.Copy(DataFile, DataFile + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true); } catch { }
            return new AppData();
        }
        finally { _lock.Release(); }
    }

    public async Task SaveAsync(AppData data)
    {
        await _lock.WaitAsync();
        try
        {
            var temp = DataFile + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(data, _json));
            if (File.Exists(DataFile))
            {
                try { File.Replace(temp, DataFile, DataFile + ".bak", true); }
                catch { File.Copy(temp, DataFile, true); File.Delete(temp); }
            }
            else File.Move(temp, DataFile);
        }
        finally { _lock.Release(); }
    }
}

public sealed class OpenAiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public async Task<List<ContentItem>> GenerateContentAsync(AppSettings settings, string topic, string category, string objective, int count, string notes, CancellationToken ct)
    {
        var key = Crypto.Unprotect(settings.OpenAiKey);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Chưa cài OpenAI API key trong mục Cài đặt.");
        var prompt = $"""
Bạn là Giám đốc Marketing của PHÚ CƯỜNG GROUP, chuyên thiết kế và thi công nhà ở tại Nha Trang – Khánh Hòa.
Tạo đúng {count} bài Facebook khác nhau.
Chủ đề: {topic}
Nhóm nội dung: {category}
Mục tiêu: {objective}
Thông tin thực tế: {notes}

Quy tắc:
- Viết tiếng Việt tự nhiên, rõ ràng, gần gũi và đáng tin.
- Không bịa thông số, tên khách hàng, địa điểm, giá, tiến độ hoặc cam kết kỹ thuật.
- Mỗi bài khác nhau về góc nhìn, tiêu đề, hook và CTA.
- CTA ưu tiên tư vấn xây nhà tại Nha Trang – Khánh Hòa, liên hệ Thảo 0903 570 014.
- Hashtag 5–8 thẻ, trên một dòng.
- imagePrompt: công trình dân dụng Việt Nam chân thực, kỹ thuật hợp lý, cinematic trầm, điểm nhấn gold tinh tế, chừa vùng đặt tiêu đề, không chữ, không logo, không watermark.
- Footer phải giữ nguyên:
{settings.Brand.Footer}

Trả JSON thuần: {{"items":[{{"title":"...","hook":"...","body":"...","cta":"...","hashtags":"...","imagePrompt":"..."}}]}}
""";
        var payload = JsonSerializer.Serialize(new { model = settings.TextModel, input = prompt, text = new { format = new { type = "json_object" } } });
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(ApiError(raw, "OpenAI"));
        using var doc = JsonDocument.Parse(raw);
        var output = ExtractText(doc.RootElement);
        using var contentDoc = JsonDocument.Parse(output);
        var result = new List<ContentItem>();
        foreach (var x in contentDoc.RootElement.GetProperty("items").EnumerateArray().Take(count))
        {
            result.Add(new ContentItem
            {
                Topic = topic,
                Category = category,
                Objective = objective,
                Title = GetString(x, "title"),
                Hook = GetString(x, "hook"),
                Body = GetString(x, "body"),
                Cta = GetString(x, "cta"),
                Hashtags = GetString(x, "hashtags"),
                ImagePrompt = GetString(x, "imagePrompt"),
                Footer = settings.Brand.Footer
            });
        }
        if (result.Count == 0) throw new InvalidOperationException("AI chưa trả về nội dung hợp lệ.");
        return result;
    }

    public async Task<byte[]> GenerateImageAsync(AppSettings settings, string prompt, CancellationToken ct)
    {
        var key = Crypto.Unprotect(settings.OpenAiKey);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Chưa cài OpenAI API key.");
        var payload = JsonSerializer.Serialize(new
        {
            model = settings.ImageModel,
            prompt = prompt + "\nẢnh editorial xây dựng cao cấp, chân thực, không chữ, không watermark, không logo giả.",
            size = "1024x1536",
            quality = "high",
            output_format = "png"
        });
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(ApiError(raw, "OpenAI Image"));
        using var doc = JsonDocument.Parse(raw);
        var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
        return string.IsNullOrWhiteSpace(b64) ? throw new InvalidOperationException("AI không trả về ảnh.") : Convert.FromBase64String(b64);
    }

    private static string GetString(JsonElement e, string name) => e.TryGetProperty(name, out var p) ? p.GetString() ?? "" : "";
    private static string ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String) return direct.GetString() ?? "";
        if (root.TryGetProperty("output", out var output))
            foreach (var block in output.EnumerateArray())
                if (block.TryGetProperty("content", out var content))
                    foreach (var part in content.EnumerateArray())
                        if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String) return text.GetString() ?? "";
        throw new InvalidOperationException("Không đọc được dữ liệu trả về từ AI.");
    }
    private static string ApiError(string raw, string source)
    {
        try { using var doc = JsonDocument.Parse(raw); return doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? $"Lỗi {source}."; }
        catch { return $"Không thể kết nối {source}."; }
    }
}

public sealed class MetaService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };
    public async Task<string> ValidateAsync(AppSettings settings, string pageId, string token, CancellationToken ct)
    {
        var url = $"https://graph.facebook.com/{settings.MetaVersion}/{pageId}?fields=id,name&access_token={Uri.EscapeDataString(token)}";
        using var res = await _http.GetAsync(url, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(ReadError(raw));
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.GetProperty("name").GetString() ?? pageId;
    }

    public async Task<string> PublishAsync(AppSettings settings, FanPage page, ContentItem content, CancellationToken ct)
    {
        if (!File.Exists(content.ImagePath)) throw new InvalidOperationException("Bài chưa có ảnh hợp lệ.");
        var token = Crypto.Unprotect(page.Token);
        using var form = new MultipartFormDataContent();
        await using var file = File.OpenRead(content.ImagePath);
        var stream = new StreamContent(file);
        stream.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(stream, "source", Path.GetFileName(content.ImagePath));
        form.Add(new StringContent(content.FullCaption), "caption");
        form.Add(new StringContent("true"), "published");
        form.Add(new StringContent(token), "access_token");
        using var res = await _http.PostAsync($"https://graph.facebook.com/{settings.MetaVersion}/{page.PageId}/photos", form, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(ReadError(raw));
        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.TryGetProperty("post_id", out var post)) return post.GetString() ?? "";
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
    }

    private static string ReadError(string raw)
    {
        try { using var doc = JsonDocument.Parse(raw); return doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "Lỗi Meta API."; }
        catch { return "Không thể kết nối Meta API."; }
    }
}

public sealed class BrandingService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const string LogoUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR2WNcd58cd43Oq6-OL-eTwfr-iuFaqkxQ-mWj3CoTTAg&s=10";

    public async Task<string> EnsureLogoAsync(string folder)
    {
        var output = Path.Combine(folder, "pcg-logo-transparent.png");
        if (File.Exists(output)) return output;
        var bytes = await _http.GetByteArrayAsync(LogoUrl);
        var input = Path.Combine(folder, "pcg-logo-source.png");
        await File.WriteAllBytesAsync(input, bytes);
        RemoveWhiteBackground(input, output);
        return output;
    }

    public async Task ComposeAsync(string sourcePath, string outputPath, string headline, string folder)
    {
        var logoPath = await EnsureLogoAsync(folder);
        var source = Load(sourcePath);
        var logo = Load(logoPath);
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(source, new Rect(0, 0, width, height));
            var overlayHeight = height * 0.36;
            var gradient = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(235, 5, 6, 8), new Point(0, 0), new Point(0, 1));
            dc.DrawRectangle(gradient, null, new Rect(0, height - overlayHeight, width, overlayHeight));
            var logoWidth = Math.Min(width * 0.28, 330);
            var logoHeight = logoWidth * logo.PixelHeight / logo.PixelWidth;
            dc.DrawImage(logo, new Rect(width * 0.055, height * 0.045, logoWidth, logoHeight));
            var text = new FormattedText(headline.ToUpperInvariant(), CultureInfo.GetCultureInfo("vi-VN"), FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), Math.Max(36, width * 0.048), Brushes.White, 96)
            { MaxTextWidth = width * 0.86, MaxLineCount = 3, Trimming = TextTrimming.WordEllipsis };
            dc.DrawText(text, new Point(width * 0.07, height - overlayHeight * 0.72));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(212, 174, 90)), null, new Rect(width * 0.07, height - height * 0.055, width * 0.43, Math.Max(5, height * 0.006)));
        }
        var render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        render.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(render));
        await using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }

    private static BitmapImage Load(string path)
    {
        var b = new BitmapImage();
        b.BeginInit(); b.CacheOption = BitmapCacheOption.OnLoad; b.UriSource = new Uri(path, UriKind.Absolute); b.EndInit(); b.Freeze(); return b;
    }

    private static void RemoveWhiteBackground(string input, string output)
    {
        var source = Load(input);
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var b = pixels[i]; var g = pixels[i + 1]; var r = pixels[i + 2];
            if (r > 235 && g > 235 && b > 235) pixels[i + 3] = 0;
        }
        var wb = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(wb));
        using var stream = File.Create(output); encoder.Save(stream);
    }
}

public sealed class MainWindow : Window
{
    private static readonly Brush Bg = Brush("#0A0B0E");
    private static readonly Brush Panel = Brush("#121419");
    private static readonly Brush Panel2 = Brush("#191C22");
    private static readonly Brush Line = Brush("#2B3038");
    private static readonly Brush Gold = Brush("#D4AE5A");
    private static readonly Brush Text = Brush("#F6F3EA");
    private static readonly Brush Muted = Brush("#9DA3AD");

    private readonly StorageService _storage = new();
    private readonly OpenAiService _openAi = new();
    private readonly MetaService _meta = new();
    private readonly BrandingService _branding = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly SemaphoreSlim _scheduleLock = new(1, 1);
    private AppData _data = new();
    private readonly List<FrameworkElement> _pages = [];
    private readonly TextBlock _title = Txt("Tổng quan", 26, Text, FontWeights.SemiBold);
    private readonly TextBlock _subtitle = Txt("Trung tâm vận hành nội dung và lịch đăng Facebook.", 13, Muted);
    private readonly TextBlock _status = Txt("Sẵn sàng", 12, Muted);
    private readonly TextBlock _schedulerBadge = Txt("Lịch đăng đang chạy", 12, Brush("#69D29A"), FontWeights.SemiBold);
    private readonly TextBlock[] _metrics = [Txt("0", 34, Text, FontWeights.Bold), Txt("0", 34, Gold, FontWeights.Bold), Txt("0", 34, Text, FontWeights.Bold), Txt("0", 34, Text, FontWeights.Bold)];
    private readonly TextBox _topic = Input(90, true);
    private readonly TextBox _notes = Input(105, true);
    private readonly ComboBox _category = Combo(["Công trình thực tế", "Kiến thức xây nhà", "Báo giá – tư vấn", "Tuyển dụng", "Cộng tác viên"]);
    private readonly ComboBox _objective = Combo(["Tạo niềm tin", "Tìm khách hàng", "Tăng tương tác", "Nhận diện thương hiệu"]);
    private readonly ComboBox _count = Combo(["3", "5", "10"]);
    private readonly StackPanel _generatedPanel = new();
    private readonly StackPanel _libraryPanel = new();
    private readonly StackPanel _schedulePanel = new();
    private readonly StackPanel _pagePanel = new();
    private readonly StackPanel _logPanel = new();
    private readonly TextBox _search = Input(38);
    private readonly DatePicker _scheduleDate = new() { SelectedDate = DateTime.Today, Width = 160, Margin = new Thickness(8, 0, 18, 0) };
    private readonly TextBox _times = Input(38);
    private readonly TextBox _pageName = Input(38);
    private readonly TextBox _pageId = Input(38);
    private readonly PasswordBox _pageToken = Password();
    private readonly PasswordBox _apiKey = Password();
    private readonly TextBox _textModel = Input(38);
    private readonly TextBox _imageModel = Input(38);
    private readonly TextBox _metaVersion = Input(38);
    private readonly TextBox _footer = Input(220, true);
    private readonly CheckBox _pauseScheduler = new() { Content = "Tạm dừng toàn bộ lịch đăng", Foreground = Text, Margin = new Thickness(0, 14, 0, 0) };

    public MainWindow()
    {
        Title = "PCG AI Marketing";
        Width = 1440; Height = 900; MinWidth = 1120; MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Bg; Foreground = Text; FontFamily = new FontFamily("Segoe UI");
        Content = BuildShell();
        Loaded += async (_, _) => await InitializeAsync();
        _timer.Tick += async (_, _) => await RunSchedulerAsync();
        Closed += (_, _) => _timer.Stop();
    }

    private async Task InitializeAsync()
    {
        _data = await _storage.LoadAsync();
        _times.Text = string.Join(", ", _data.Settings.DefaultTimes);
        _textModel.Text = _data.Settings.TextModel;
        _imageModel.Text = _data.Settings.ImageModel;
        _metaVersion.Text = _data.Settings.MetaVersion;
        _footer.Text = _data.Settings.Brand.Footer;
        _pauseScheduler.IsChecked = _data.Settings.SchedulerPaused;
        RefreshAll();
        _timer.Start();
        try { await _branding.EnsureLogoAsync(_storage.Folder); } catch { AddLog("Không tải được logo ở lần mở đầu. Ứng dụng sẽ thử lại khi tạo ảnh."); }
    }

    private UIElement BuildShell()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        var sidebar = new Border { Background = Brush("#0E1014"), BorderBrush = Line, BorderThickness = new Thickness(0, 0, 1, 0), Child = BuildSidebar() };
        Grid.SetColumn(sidebar, 0); root.Children.Add(sidebar);
        var main = new Grid(); main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(92) }); main.RowDefinitions.Add(new RowDefinition()); main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
        Grid.SetColumn(main, 1); root.Children.Add(main);
        var header = new Border { BorderBrush = Line, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(30, 18, 30, 18) };
        var headerGrid = new Grid();
        headerGrid.Children.Add(new StackPanel { Children = { _title, _subtitle } });
        var badge = new Border { Background = Brush("#14261E"), CornerRadius = new CornerRadius(18), Padding = new Thickness(13, 7, 13, 7), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Child = _schedulerBadge };
        headerGrid.Children.Add(badge); header.Child = headerGrid; main.Children.Add(header);
        var pageHost = new Grid { Margin = new Thickness(30) }; Grid.SetRow(pageHost, 1); main.Children.Add(pageHost);
        _pages.Add(BuildDashboard()); _pages.Add(BuildAssistant()); _pages.Add(BuildLibrary()); _pages.Add(BuildSchedule()); _pages.Add(BuildFanPages()); _pages.Add(BuildLogs()); _pages.Add(BuildSettings());
        foreach (var p in _pages) { p.Visibility = Visibility.Collapsed; pageHost.Children.Add(p); }
        _pages[0].Visibility = Visibility.Visible;
        var statusBar = new Border { Background = Brush("#0E1014"), BorderBrush = Line, BorderThickness = new Thickness(0, 1, 0, 0), Child = new Grid { Children = { _status } } };
        _status.Margin = new Thickness(16, 0, 0, 0); _status.VerticalAlignment = VerticalAlignment.Center; Grid.SetRow(statusBar, 2); main.Children.Add(statusBar);
        return root;
    }

    private UIElement BuildSidebar()
    {
        var dock = new DockPanel { Margin = new Thickness(16) };
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); dock.Children.Add(top);
        var logo = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 8, 0, 28) };
        logo.Children.Add(new Border { Width = 48, Height = 48, BorderBrush = Gold, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Child = new TextBlock { Text = "PCG", Foreground = Gold, FontWeight = FontWeights.Bold, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        var names = new StackPanel { Margin = new Thickness(12, 5, 0, 0) }; names.Children.Add(Txt("AI MARKETING", 16, Text, FontWeights.Bold)); names.Children.Add(Txt("PHÚ CƯỜNG GROUP", 10, Muted)); logo.Children.Add(names); top.Children.Add(logo);
        string[] labels = ["◈  Tổng quan", "✦  Trợ lý tạo bài", "▦  Kho nội dung", "◷  Lịch đăng", "●  Fanpage", "≡  Nhật ký", "⚙  Cài đặt"];
        for (var i = 0; i < labels.Length; i++)
        {
            var index = i; var b = Button(labels[i], false); b.HorizontalContentAlignment = HorizontalAlignment.Left; b.Margin = new Thickness(0, 3, 0, 3); b.Click += (_, _) => Navigate(index); top.Children.Add(b);
        }
        var foot = Txt("CHUẨN TỪ MÓNG – VỮNG TỪ TÂM", 10, Brush("#60656F")); foot.TextWrapping = TextWrapping.Wrap; foot.Margin = new Thickness(8); DockPanel.SetDock(foot, Dock.Bottom); dock.Children.Add(foot);
        return dock;
    }

    private FrameworkElement BuildDashboard()
    {
        var panel = new StackPanel();
        var cards = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 20) };
        string[] names = ["Tổng nội dung", "Đã duyệt", "Chờ đăng", "Đã đăng"];
        for (var i = 0; i < 4; i++)
        {
            var s = new StackPanel(); s.Children.Add(Txt(names[i], 13, Muted)); _metrics[i].Margin = new Thickness(0, 9, 0, 0); s.Children.Add(_metrics[i]);
            cards.Children.Add(Card(s, i == 3 ? new Thickness(0) : new Thickness(0, 0, 12, 0), 20));
        }
        panel.Children.Add(cards);
        var workflow = new StackPanel(); workflow.Children.Add(Txt("Quy trình thông minh", 19, Text, FontWeights.SemiBold)); var d = Txt("Nhập một yêu cầu → AI tạo nhiều nội dung → tạo ảnh → duyệt → tự xếp lịch → đăng nhiều fanpage → báo kết quả.", 14, Muted); d.Margin = new Thickness(0, 10, 0, 0); workflow.Children.Add(d); panel.Children.Add(Card(workflow, new Thickness(0), 24));
        return new ScrollViewer { Content = panel };
    }

    private FrameworkElement BuildAssistant()
    {
        var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) }); grid.ColumnDefinitions.Add(new ColumnDefinition());
        var form = new StackPanel(); form.Children.Add(Txt("Hôm nay bạn muốn tạo nội dung gì?", 21, Text, FontWeights.SemiBold)); var note = Txt("Mô tả ngắn như đang nhắn cho một nhân viên marketing.", 13, Muted); note.Margin = new Thickness(0, 6, 0, 18); form.Children.Add(note);
        AddField(form, "Chủ đề", _topic); AddField(form, "Nhóm nội dung", _category); AddField(form, "Mục tiêu", _objective); AddField(form, "Số lượng bài", _count); AddField(form, "Ghi chú thực tế", _notes);
        var generate = Button("TẠO BỘ NỘI DUNG", true); generate.Click += async (_, _) => await GenerateContentAsync(generate); form.Children.Add(generate);
        var left = Card(new ScrollViewer { Content = form }, new Thickness(0, 0, 18, 0), 22); grid.Children.Add(left);
        var rightDock = new DockPanel(); var h = Txt("Kết quả mới tạo", 18, Text, FontWeights.SemiBold); h.Margin = new Thickness(0, 0, 0, 12); DockPanel.SetDock(h, Dock.Top); rightDock.Children.Add(h); rightDock.Children.Add(new ScrollViewer { Content = _generatedPanel });
        var right = Card(rightDock, new Thickness(0), 18); Grid.SetColumn(right, 1); grid.Children.Add(right); return grid;
    }

    private FrameworkElement BuildLibrary()
    {
        var root = new DockPanel(); var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) }; _search.Width = 360; _search.TextChanged += (_, _) => RefreshLibrary(); toolbar.Children.Add(_search); var refresh = Button("Làm mới", false); refresh.Margin = new Thickness(10, 0, 0, 0); refresh.Click += (_, _) => RefreshLibrary(); toolbar.Children.Add(refresh); DockPanel.SetDock(toolbar, Dock.Top); root.Children.Add(toolbar); root.Children.Add(new ScrollViewer { Content = _libraryPanel }); return root;
    }

    private FrameworkElement BuildSchedule()
    {
        var dock = new DockPanel(); var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; row.Children.Add(Txt("Ngày bắt đầu", 13, Text)); row.Children.Add(_scheduleDate); row.Children.Add(Txt("Giờ đăng", 13, Text)); _times.Width = 290; _times.Margin = new Thickness(8, 0, 18, 0); row.Children.Add(_times); var auto = Button("TỰ XẾP LỊCH", true); auto.Click += async (_, _) => await AutoScheduleAsync(); row.Children.Add(auto); var top = Card(row, new Thickness(0, 0, 0, 16), 20); DockPanel.SetDock(top, Dock.Top); dock.Children.Add(top); dock.Children.Add(new ScrollViewer { Content = _schedulePanel }); return dock;
    }

    private FrameworkElement BuildFanPages()
    {
        var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(400) }); grid.ColumnDefinitions.Add(new ColumnDefinition());
        var form = new StackPanel(); form.Children.Add(Txt("Kết nối fanpage", 19, Text, FontWeights.SemiBold)); AddField(form, "Tên fanpage", _pageName); AddField(form, "Page ID", _pageId); AddField(form, "Page Access Token", _pageToken); var add = Button("KIỂM TRA & KẾT NỐI", true); add.Click += async (_, _) => await AddFanPageAsync(add); form.Children.Add(add); grid.Children.Add(Card(form, new Thickness(0, 0, 18, 0), 20)); var list = Card(new ScrollViewer { Content = _pagePanel }, new Thickness(0), 16); Grid.SetColumn(list, 1); grid.Children.Add(list); return grid;
    }

    private FrameworkElement BuildLogs() => Card(new ScrollViewer { Content = _logPanel }, new Thickness(0), 16);

    private FrameworkElement BuildSettings()
    {
        var form = new StackPanel { Width = 760, HorizontalAlignment = HorizontalAlignment.Left }; form.Children.Add(Txt("Cài đặt hệ thống", 21, Text, FontWeights.SemiBold)); AddField(form, "OpenAI API key", _apiKey); AddField(form, "Text model", _textModel); AddField(form, "Image model", _imageModel); AddField(form, "Meta Graph version", _metaVersion); AddField(form, "Footer cố định", _footer); form.Children.Add(_pauseScheduler); var save = Button("LƯU CÀI ĐẶT", true); save.Width = 180; save.HorizontalAlignment = HorizontalAlignment.Left; save.Margin = new Thickness(0, 20, 0, 0); save.Click += async (_, _) => await SaveSettingsAsync(); form.Children.Add(save); return new ScrollViewer { Content = Card(form, new Thickness(0), 24) };
    }

    private async Task GenerateContentAsync(Button button)
    {
        if (string.IsNullOrWhiteSpace(_topic.Text)) { Warn("Vui lòng nhập chủ đề."); return; }
        try
        {
            SetBusy(button, true, "AI đang phân tích và viết nội dung...");
            var items = await _openAi.GenerateContentAsync(_data.Settings, _topic.Text.Trim(), _category.SelectedItem?.ToString() ?? "Công trình thực tế", _objective.SelectedItem?.ToString() ?? "Tạo niềm tin", int.Parse(_count.SelectedItem?.ToString() ?? "3"), _notes.Text.Trim(), CancellationToken.None);
            foreach (var x in items) _data.Contents.Insert(0, x);
            await SaveAsync(); RenderGenerated(items); RefreshAll(); _status.Text = $"Đã tạo {items.Count} bài.";
        }
        catch (Exception ex) { Warn(ex.Message); }
        finally { SetBusy(button, false, "Sẵn sàng"); }
    }

    private async Task GenerateImageAsync(ContentItem item, Button button)
    {
        try
        {
            SetBusy(button, true, "AI đang tạo ảnh và ghép nhận diện...");
            var bytes = await _openAi.GenerateImageAsync(_data.Settings, item.ImagePrompt, CancellationToken.None);
            var raw = Path.Combine(_storage.ImagesFolder, $"raw-{item.Id:N}.png");
            var final = Path.Combine(_storage.ImagesFolder, $"PCG-{item.Id:N}.png");
            await File.WriteAllBytesAsync(raw, bytes);
            await _branding.ComposeAsync(raw, final, item.Title, _storage.Folder);
            item.ImagePath = final; item.UpdatedAt = DateTime.Now;
            await SaveAsync(); RefreshAll(); _status.Text = "Đã tạo ảnh và ghép logo.";
        }
        catch (Exception ex) { Warn(ex.Message); }
        finally { SetBusy(button, false, "Sẵn sàng"); }
    }

    private async Task AddFanPageAsync(Button button)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_pageId.Text) || string.IsNullOrWhiteSpace(_pageToken.Password)) throw new InvalidOperationException("Nhập Page ID và Page Access Token.");
            SetBusy(button, true, "Đang kiểm tra quyền fanpage...");
            var verified = await _meta.ValidateAsync(_data.Settings, _pageId.Text.Trim(), _pageToken.Password.Trim(), CancellationToken.None);
            _data.Pages.Add(new FanPage { Name = string.IsNullOrWhiteSpace(_pageName.Text) ? verified : _pageName.Text.Trim(), PageId = _pageId.Text.Trim(), Token = Crypto.Protect(_pageToken.Password.Trim()) });
            _pageToken.Clear(); await SaveAsync(); RefreshAll(); _status.Text = "Đã kết nối fanpage.";
        }
        catch (Exception ex) { Warn(ex.Message); }
        finally { SetBusy(button, false, "Sẵn sàng"); }
    }

    private async Task AutoScheduleAsync()
    {
        try
        {
            var pages = _data.Pages.Where(x => x.Active).ToList(); if (pages.Count == 0) throw new InvalidOperationException("Chưa có fanpage đang hoạt động.");
            var contents = _data.Contents.Where(x => x.Status == "Đã duyệt" && !_data.Schedules.Any(s => s.ContentId == x.Id && s.Status != "Lỗi")).OrderBy(x => x.CreatedAt).ToList();
            if (contents.Count == 0) throw new InvalidOperationException("Không có bài đã duyệt chưa lên lịch.");
            var times = _times.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(TimeSpan.Parse).ToList();
            if (times.Count == 0) throw new InvalidOperationException("Nhập ít nhất một giờ đăng.");
            var day = _scheduleDate.SelectedDate ?? DateTime.Today; var cursor = 0;
            while (cursor < contents.Count)
            {
                foreach (var t in times)
                {
                    if (cursor >= contents.Count) break;
                    var baseTime = day.Date + t;
                    for (var i = 0; i < pages.Count; i++) _data.Schedules.Add(new ScheduleItem { ContentId = contents[cursor].Id, PageId = pages[i].Id, ScheduledAt = baseTime.AddMinutes(i * 20) });
                    contents[cursor].Status = "Đã lên lịch"; cursor++;
                }
                day = day.AddDays(1);
            }
            _data.Settings.DefaultTimes = times.Select(x => x.ToString("hh\\:mm")).ToList();
            await SaveAsync(); RefreshAll(); _status.Text = "Đã tự xếp lịch.";
        }
        catch (Exception ex) { Warn(ex.Message); }
    }

    private async Task RunSchedulerAsync()
    {
        if (_data.Settings.SchedulerPaused || !await _scheduleLock.WaitAsync(0)) return;
        try
        {
            var due = _data.Schedules.Where(x => x.Status == "Đã lên lịch" && x.ScheduledAt <= DateTime.Now).OrderBy(x => x.ScheduledAt).Take(3).ToList();
            foreach (var job in due)
            {
                var content = _data.Contents.FirstOrDefault(x => x.Id == job.ContentId);
                var page = _data.Pages.FirstOrDefault(x => x.Id == job.PageId && x.Active);
                if (content is null || page is null) { job.Status = "Lỗi"; job.LastError = "Thiếu nội dung hoặc fanpage."; continue; }
                job.Status = "Đang đăng"; job.AttemptCount++;
                try
                {
                    job.PublishedId = await _meta.PublishAsync(_data.Settings, page, content, CancellationToken.None);
                    job.Status = "Đã đăng"; job.LastError = ""; content.Status = "Đã đăng";
                    AddLog($"Đăng thành công · {page.Name} · {content.Title}");
                }
                catch (Exception ex)
                {
                    job.LastError = ex.Message;
                    job.Status = job.AttemptCount >= 3 ? "Lỗi" : "Đã lên lịch";
                    if (job.Status == "Đã lên lịch") job.ScheduledAt = DateTime.Now.AddMinutes(5);
                    AddLog($"Đăng lỗi · {page.Name} · {ex.Message}");
                }
                await SaveAsync(); RefreshAll();
            }
        }
        finally { _scheduleLock.Release(); }
    }

    private async Task SaveSettingsAsync()
    {
        if (!string.IsNullOrWhiteSpace(_apiKey.Password)) _data.Settings.OpenAiKey = Crypto.Protect(_apiKey.Password.Trim());
        _data.Settings.TextModel = _textModel.Text.Trim(); _data.Settings.ImageModel = _imageModel.Text.Trim(); _data.Settings.MetaVersion = _metaVersion.Text.Trim(); _data.Settings.Brand.Footer = _footer.Text; _data.Settings.SchedulerPaused = _pauseScheduler.IsChecked == true;
        _apiKey.Clear(); await SaveAsync(); RefreshAll(); _status.Text = "Đã lưu cài đặt.";
    }

    private void Navigate(int index)
    {
        for (var i = 0; i < _pages.Count; i++) _pages[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
        string[] titles = ["Tổng quan", "Trợ lý tạo bài", "Kho nội dung", "Lịch đăng", "Fanpage", "Nhật ký", "Cài đặt"];
        string[] subs = ["Trung tâm vận hành nội dung và lịch đăng Facebook.", "Tạo content và ảnh theo đúng nhận diện Phú Cường.", "Duyệt và quản lý toàn bộ tài sản nội dung.", "Đăng nhiều lần mỗi ngày và lệch giờ giữa fanpage.", "Quản lý kết nối Meta Page.", "Theo dõi thành công, lỗi và thử lại.", "Bảo mật API key và tùy chỉnh thương hiệu."];
        _title.Text = titles[index]; _subtitle.Text = subs[index];
        if (index == 2) RefreshLibrary(); if (index == 3) RefreshSchedule(); if (index == 4) RefreshPages(); if (index == 5) RefreshLogs();
    }

    private void RefreshAll()
    {
        _metrics[0].Text = _data.Contents.Count.ToString(); _metrics[1].Text = _data.Contents.Count(x => x.Status == "Đã duyệt").ToString(); _metrics[2].Text = _data.Schedules.Count(x => x.Status == "Đã lên lịch").ToString(); _metrics[3].Text = _data.Schedules.Count(x => x.Status == "Đã đăng").ToString();
        _schedulerBadge.Text = _data.Settings.SchedulerPaused ? "Lịch đăng đang tạm dừng" : "Lịch đăng đang chạy"; _schedulerBadge.Foreground = _data.Settings.SchedulerPaused ? Gold : Brush("#69D29A");
        RefreshLibrary(); RefreshSchedule(); RefreshPages(); RefreshLogs();
    }

    private void RenderGenerated(IEnumerable<ContentItem> items)
    {
        _generatedPanel.Children.Clear(); foreach (var x in items) _generatedPanel.Children.Add(ContentPreview(x));
    }

    private void RefreshLibrary()
    {
        if (_libraryPanel is null) return; _libraryPanel.Children.Clear(); var q = _search.Text.Trim();
        var items = _data.Contents.Where(x => string.IsNullOrWhiteSpace(q) || x.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || x.Topic.Contains(q, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.CreatedAt).ToList();
        if (items.Count == 0) { _libraryPanel.Children.Add(Txt("Chưa có nội dung.", 14, Muted)); return; }
        foreach (var item in items)
        {
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) }); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
            var imageBorder = new Border { Width = 92, Height = 92, Background = Brush("#0C0E12"), CornerRadius = new CornerRadius(8) };
            if (File.Exists(item.ImagePath)) imageBorder.Child = new Image { Source = Bitmap(item.ImagePath), Stretch = Stretch.UniformToFill };
            grid.Children.Add(imageBorder);
            var info = new StackPanel { Margin = new Thickness(15, 0, 12, 0) }; info.Children.Add(Txt(item.Title, 16, Text, FontWeights.SemiBold)); var topic = Txt(item.Topic, 12, Muted); topic.Margin = new Thickness(0, 5, 0, 0); info.Children.Add(topic); var st = Txt(item.Status, 12, Gold); st.Margin = new Thickness(0, 7, 0, 0); info.Children.Add(st); Grid.SetColumn(info, 1); grid.Children.Add(info);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            var edit = Button("Sửa", false); edit.Click += (_, _) => OpenEditor(item); actions.Children.Add(edit);
            var image = Button("Tạo ảnh", false); image.Margin = new Thickness(6, 0, 0, 0); image.Click += async (_, _) => await GenerateImageAsync(item, image); actions.Children.Add(image);
            var approve = Button("Duyệt", true); approve.Margin = new Thickness(6, 0, 0, 0); approve.Click += async (_, _) => { if (!File.Exists(item.ImagePath)) { Warn("Hãy tạo ảnh trước khi duyệt bài."); return; } item.Status = "Đã duyệt"; item.UpdatedAt = DateTime.Now; await SaveAsync(); RefreshAll(); }; actions.Children.Add(approve);
            var copy = Button("Copy", false); copy.Margin = new Thickness(6, 0, 0, 0); copy.Click += (_, _) => { Clipboard.SetText(item.FullCaption); _status.Text = "Đã copy content."; }; actions.Children.Add(copy);
            Grid.SetColumn(actions, 2); grid.Children.Add(actions); _libraryPanel.Children.Add(Card(grid, new Thickness(0, 0, 0, 10), 16));
        }
    }

    private void RefreshSchedule()
    {
        if (_schedulePanel is null) return; _schedulePanel.Children.Clear();
        foreach (var job in _data.Schedules.OrderBy(x => x.ScheduledAt))
        {
            var content = _data.Contents.FirstOrDefault(x => x.Id == job.ContentId); var page = _data.Pages.FirstOrDefault(x => x.Id == job.PageId);
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) }); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.Children.Add(Txt(job.ScheduledAt.ToString("dd/MM/yyyy HH:mm"), 13, Text)); var title = Txt(content?.Title ?? "Bài đã xóa", 13, Text, FontWeights.SemiBold); Grid.SetColumn(title, 1); grid.Children.Add(title); var pageText = Txt(page?.Name ?? "Page đã xóa", 13, Muted); Grid.SetColumn(pageText, 2); grid.Children.Add(pageText); var status = Txt(job.Status, 12, job.Status == "Lỗi" ? Brush("#FF8585") : Gold); Grid.SetColumn(status, 3); grid.Children.Add(status);
            _schedulePanel.Children.Add(Card(grid, new Thickness(0, 0, 0, 8), 14));
            if (!string.IsNullOrWhiteSpace(job.LastError)) { var err = Txt(job.LastError, 11, Brush("#FF8585")); err.Margin = new Thickness(15, -5, 0, 9); _schedulePanel.Children.Add(err); }
        }
        if (_data.Schedules.Count == 0) _schedulePanel.Children.Add(Txt("Chưa có lịch đăng.", 14, Muted));
    }

    private void RefreshPages()
    {
        if (_pagePanel is null) return; _pagePanel.Children.Clear();
        foreach (var page in _data.Pages)
        {
            var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            var info = new StackPanel(); info.Children.Add(Txt(page.Name, 17, Text, FontWeights.SemiBold)); var id = Txt(page.PageId, 12, Muted); id.Margin = new Thickness(0, 5, 0, 0); info.Children.Add(id); grid.Children.Add(info);
            var remove = Button("Xóa", false); remove.Click += async (_, _) => { if (MessageBox.Show("Xóa fanpage này?", "PCG", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { _data.Pages.Remove(page); _data.Schedules.RemoveAll(x => x.PageId == page.Id && x.Status != "Đã đăng"); await SaveAsync(); RefreshAll(); } }; Grid.SetColumn(remove, 1); grid.Children.Add(remove);
            _pagePanel.Children.Add(Card(grid, new Thickness(0, 0, 0, 10), 16));
        }
        if (_data.Pages.Count == 0) _pagePanel.Children.Add(Txt("Chưa kết nối fanpage.", 14, Muted));
    }

    private void RefreshLogs()
    {
        if (_logPanel is null) return; _logPanel.Children.Clear(); foreach (var log in _data.Logs.Take(300)) { var t = Txt(log, 13, Text); t.Margin = new Thickness(2, 8, 2, 8); _logPanel.Children.Add(t); _logPanel.Children.Add(new Border { Height = 1, Background = Line }); } if (_data.Logs.Count == 0) _logPanel.Children.Add(Txt("Chưa có nhật ký.", 14, Muted));
    }

    private void OpenEditor(ContentItem item)
    {
        var win = new Window { Title = "Chỉnh sửa bài", Width = 820, Height = 760, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, Background = Bg, Foreground = Text, FontFamily = FontFamily };
        var panel = new StackPanel { Margin = new Thickness(22) }; var title = Input(42); title.Text = item.Title; var hook = Input(70, true); hook.Text = item.Hook; var body = Input(220, true); body.Text = item.Body; var cta = Input(80, true); cta.Text = item.Cta; var tags = Input(60, true); tags.Text = item.Hashtags; var prompt = Input(120, true); prompt.Text = item.ImagePrompt; AddField(panel, "Tiêu đề", title); AddField(panel, "Hook", hook); AddField(panel, "Nội dung", body); AddField(panel, "CTA", cta); AddField(panel, "Hashtag", tags); AddField(panel, "Prompt ảnh", prompt); var save = Button("LƯU THAY ĐỔI", true); save.Click += async (_, _) => { item.Title = title.Text; item.Hook = hook.Text; item.Body = body.Text; item.Cta = cta.Text; item.Hashtags = tags.Text; item.ImagePrompt = prompt.Text; item.UpdatedAt = DateTime.Now; await SaveAsync(); win.Close(); RefreshAll(); }; panel.Children.Add(save); win.Content = new ScrollViewer { Content = panel }; win.ShowDialog();
    }

    private FrameworkElement ContentPreview(ContentItem x)
    {
        var p = new StackPanel(); p.Children.Add(Txt(x.Title, 16, Text, FontWeights.SemiBold)); var hook = Txt(x.Hook, 13, Gold); hook.TextWrapping = TextWrapping.Wrap; hook.Margin = new Thickness(0, 6, 0, 0); p.Children.Add(hook); var body = Txt(x.Body, 13, Brush("#D7DAE0")); body.TextWrapping = TextWrapping.Wrap; body.Margin = new Thickness(0, 7, 0, 0); p.Children.Add(body); return Card(p, new Thickness(0, 0, 0, 10), 14);
    }

    private void AddLog(string message)
    {
        _data.Logs.Insert(0, $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} · {message}"); if (_data.Logs.Count > 1000) _data.Logs.RemoveRange(1000, _data.Logs.Count - 1000);
    }

    private Task SaveAsync() => _storage.SaveAsync(_data);
    private void SetBusy(Button button, bool busy, string message) { button.IsEnabled = !busy; _status.Text = message; Mouse.OverrideCursor = busy ? Cursors.Wait : null; }
    private static void Warn(string message) => MessageBox.Show(message, "PCG AI Marketing", MessageBoxButton.OK, MessageBoxImage.Warning);

    private static Border Card(UIElement child, Thickness margin, double padding) => new() { Background = Panel, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(padding), Margin = margin, Child = child };
    private static Button Button(string text, bool primary) { var b = new Button { Content = text, Background = primary ? Gold : Brush("#242832"), Foreground = primary ? Brush("#111216") : Text, BorderThickness = new Thickness(0), Padding = new Thickness(15, 10, 15, 10), FontWeight = FontWeights.SemiBold, Cursor = Cursors.Hand }; return b; }
    private static TextBlock Txt(string text, double size, Brush brush, FontWeight? weight = null) => new() { Text = text, FontSize = size, Foreground = brush, FontWeight = weight ?? FontWeights.Normal, TextWrapping = TextWrapping.Wrap };
    private static TextBox Input(double height, bool multiline = false) => new() { Height = height, AcceptsReturn = multiline, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap, VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden, Background = Brush("#0E1014"), Foreground = Text, BorderBrush = Line, CaretBrush = Gold, Padding = new Thickness(10), BorderThickness = new Thickness(1) };
    private static PasswordBox Password() => new() { Height = 40, Background = Brush("#0E1014"), Foreground = Text, BorderBrush = Line, Padding = new Thickness(10), BorderThickness = new Thickness(1) };
    private static ComboBox Combo(string[] items) { var c = new ComboBox { Height = 40, Background = Brush("#0E1014"), Foreground = Brush("#111216"), BorderBrush = Line, Padding = new Thickness(8) }; foreach (var i in items) c.Items.Add(i); c.SelectedIndex = 0; return c; }
    private static void AddField(Panel panel, string label, Control control) { var l = Txt(label, 13, Text); l.Margin = new Thickness(0, 12, 0, 6); panel.Children.Add(l); panel.Children.Add(control); }
    private static Brush Brush(string color) => (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
    private static BitmapImage Bitmap(string path) { var b = new BitmapImage(); b.BeginInit(); b.CacheOption = BitmapCacheOption.OnLoad; b.UriSource = new Uri(path, UriKind.Absolute); b.EndInit(); b.Freeze(); return b; }
}
