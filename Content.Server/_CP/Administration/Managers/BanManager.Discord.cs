using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Server.Discord;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    private readonly HttpClient _httpClient = new();

    private bool _banWebhookEnabled;
    private string _banWebhookUrl = string.Empty;
    private string _banNotificationName = string.Empty;
    private string _banAvatarUrl = string.Empty;
    private string _banFooterIconUrl = string.Empty;

    private async Task SendServerBan(string bannedUser, string administrator, string reason, DateTimeOffset? duration, int? round)
    {
        if (!_banWebhookEnabled)
        {
            return;
        }

        var expiration = duration != null
            ? Loc.GetString("ban-notification-time-expires", ("totalSeconds", duration.Value.ToUnixTimeSeconds()))
            : Loc.GetString("ban-notification-never-expires");

        bannedUser = GetOnlyNickname(bannedUser);
        var embed = new WebhookEmbed
        {
            Description = Loc.GetString(
                "discord-webhook-server-ban-notification",
                ("nickname", bannedUser),
                ("administrator", administrator),
                ("expiration", expiration),
                ("reason", reason)
            ),
            Color = 0XFF0000,
            Footer = new WebhookEmbedFooter
            {
                Text = Loc.GetString("discord-webhook-server-ban-notification-footer", ("round", round ?? 0)),
                IconUrl = _banFooterIconUrl,
            },
        };

        await SendDiscordEmbed([embed], _banNotificationName, _banAvatarUrl);
    }

    private async Task SendJobBan(string bannedUser, string administrator, string job, string reason, DateTimeOffset? duration, int? round)
    {
        if (!_banWebhookEnabled)
        {
            return;
        }

        var expiration = duration != null
            ? Loc.GetString("ban-notification-time-expires", ("totalSeconds", duration.Value.ToUnixTimeSeconds()))
            : Loc.GetString("ban-notification-never-expires");

        bannedUser = GetOnlyNickname(bannedUser);
        var embed = new WebhookEmbed
        {
            Description = Loc.GetString(
                "discord-webhook-job-ban-notification",
                ("nickname", bannedUser),
                ("administrator", administrator),
                ("expiration", expiration),
                ("job", job),
                ("reason", reason)
            ),
            Color = 0XFF0000,
            Footer = new WebhookEmbedFooter
            {
                Text = Loc.GetString("discord-webhook-server-ban-notification-footer", ("round", round ?? 0)),
                IconUrl = _banFooterIconUrl,
            },
        };

        await SendDiscordEmbed([embed], _banNotificationName, _banAvatarUrl);
    }

    public async Task SendJobBansRoles(string bannedUser, string administrator, string[] roles, string reason, DateTimeOffset? duration, int? round)
    {
        if (!_banWebhookEnabled)
        {
            return;
        }

        var expiration = duration != null
            ? Loc.GetString("ban-notification-time-expires", ("totalSeconds", duration.Value.ToUnixTimeSeconds()))
            : Loc.GetString("ban-notification-never-expires");

        var list = new StringBuilder();

        foreach (var role in roles)
        {
            list.Append(Loc.GetString("discord-webhook-job-ban-notification-entry", ("job", role)));
            list.Append('\n');
        }

        bannedUser = GetOnlyNickname(bannedUser);
        var embed = new WebhookEmbed
        {
            Description = Loc.GetString(
                "discord-webhook-job-roles-ban-notification",
                ("nickname", bannedUser),
                ("administrator", administrator),
                ("expiration", expiration),
                ("list", list),
                ("reason", reason)
            ),
            Color = 0XFF0000,
            Footer = new WebhookEmbedFooter
            {
                Text = Loc.GetString("discord-webhook-server-ban-notification-footer", ("round", round ?? 0)),
                IconUrl = _banFooterIconUrl,
            },
        };

        await SendDiscordEmbed([embed], _banNotificationName, _banAvatarUrl);
    }

    public async Task SendJobBanDepartment(string bannedUser, string administrator, string department, string reason, DateTimeOffset? duration, int? round)
    {
        if (!_banWebhookEnabled)
        {
            return;
        }

        var expiration = duration != null
            ? Loc.GetString("ban-notification-time-expires", ("totalSeconds", duration.Value.ToUnixTimeSeconds()))
            : Loc.GetString("ban-notification-never-expires");

        bannedUser = GetOnlyNickname(bannedUser);
        var embed = new WebhookEmbed
        {
            Description = Loc.GetString(
                "discord-webhook-job-departments-ban-notification",
                ("nickname", bannedUser),
                ("administrator", administrator),
                ("expiration", expiration),
                ("department", department),
                ("reason", reason)
            ),
            Color = 0XFF0000,
            Footer = new WebhookEmbedFooter
            {
                Text = Loc.GetString("discord-webhook-server-ban-notification-footer", ("round", round ?? 0)),
                IconUrl = _banFooterIconUrl,
            },
        };

        await SendDiscordEmbed([embed], _banNotificationName, _banAvatarUrl);
    }

    private async Task SendDiscordEmbed(List<WebhookEmbed> embeds, string name, string? avatarUrl = null)
    {
        if (_banWebhookUrl == string.Empty)
        {
            _sawmill.Info("Ban Webhook URL is empty, ignoring sending discord notification.");
            return;
        }

        var payload = new WebhookPayload()
        {
            Embeds = embeds,
            Username = name,
            AvatarUrl = avatarUrl,
        };

        var request = await _httpClient.PostAsync(
            $"{_banWebhookUrl}?wait=true",
            new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            ));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
            return;
        }

        var id = JsonNode.Parse(content)?["id"];
        if (id == null)
        {
            _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
        }
    }

    private string GetOnlyNickname(string nickname)
    {
        var index = nickname.LastIndexOf(' ');
        return index == -1 ? nickname : nickname[..index];
    }
}
