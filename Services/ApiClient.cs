using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Input.Services;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly CookieContainer _cookies = new();

    public bool IsAuthenticated { get; private set; }

    public ApiClient(ApiSettings settings)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseUrl)
        };
    }

    public async Task LoginAsync(string email, string password)
    {
        var payload = JsonSerializer.Serialize(new { email, password });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync("/Auth/login", content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new ApiException($"Falha no login ({(int)response.StatusCode}). {body}");
        }

        IsAuthenticated = true;
    }

    public async Task<string> UploadRecordingAsync(
        string projectName,
        string videoPath,
        string eventsPath)
    {
        if (!IsAuthenticated)
            throw new ApiException("É necessário fazer login antes de enviar.");

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent(projectName), "projectName");

        // Abre os arquivos como stream — nada é carregado inteiro em memória.
        await using var videoStream = File.OpenRead(videoPath);
        await using var eventsStream = File.OpenRead(eventsPath);

        var videoContent = new StreamContent(videoStream);
        videoContent.Headers.ContentType = new("video/mp4");
        form.Add(videoContent, "video", Path.GetFileName(videoPath));

        var eventsContent = new StreamContent(eventsStream);
        eventsContent.Headers.ContentType = new("text/plain");
        form.Add(eventsContent, "events", Path.GetFileName(eventsPath));

        using var response = await _http.PostAsync("/Record", form);

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new ApiException($"Falha no upload ({(int)response.StatusCode}). {responseBody}");

        return responseBody;
    }

    public void Dispose() => _http.Dispose();
}

public sealed class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}