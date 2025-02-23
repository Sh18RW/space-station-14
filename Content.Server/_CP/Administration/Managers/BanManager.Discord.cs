using System.Net.Http;
using Content.Server.Discord;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    private readonly HttpClient _httpClient = new();

    private async void SendWebhook(WebhookEmbed embed)
    {
        
    }
}
