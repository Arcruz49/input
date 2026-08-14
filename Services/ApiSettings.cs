using System;

namespace Input.Services;

public sealed class ApiSettings
{
    private const string DefaultBaseUrl =
        "https://inputweb-api.salmonwater-a0a4cfd6.brazilsouth.azurecontainerapps.io";

    public string BaseUrl { get; set; } =
        Environment.GetEnvironmentVariable("INPUT_API_BASEURL") is { Length: > 0 } url
            ? url
            : DefaultBaseUrl;
}