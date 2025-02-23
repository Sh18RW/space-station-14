using Content.Server.Chat.Managers;
using Content.Server.Players.RateLimiting;
using Content.Shared._CP.CCVars;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;

namespace Content.Server._CP.TTS.Systems;
// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    [Dependency] private readonly PlayerRateLimitManager _rateLimitManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    private const string RateLimitKey = "TTS";

    private void RegisterRateLimits()
    {
        _rateLimitManager.Register(RateLimitKey,
            new RateLimitRegistration(
                CPCCVars.TTSRateLimitPeriod,
                CPCCVars.TTSRateLimitCount,
                RateLimitPlayerLimited)
            );
    }

    private void RateLimitPlayerLimited(ICommonSession player)
    {
        _chat.DispatchServerMessage(player, Robust.Shared.Localization.Loc.GetString("tts-rate-limited"), suppressLog: true);
    }

    private RateLimitStatus HandleRateLimit(ICommonSession player)
    {
        return _rateLimitManager.CountAction(player, RateLimitKey);
    }
}
