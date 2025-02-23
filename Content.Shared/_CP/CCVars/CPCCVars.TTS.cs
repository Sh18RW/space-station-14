using Robust.Shared.Configuration;

namespace Content.Shared._CP.CCVars;

/// <summary>
/// Everything about CP TTS.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed partial class CPCCVars
{
    #region Server
    /// <summary>
    /// Server TTS enabled.
    /// </summary>
    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Url to TTS server.
    /// </summary>
    public static readonly CVarDef<string> TTSApiUrl =
        CVarDef.Create("tts.api_url", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Token for TTS server.
    /// </summary>
    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Timeout for TTS server generation.
    /// </summary>
    public static readonly CVarDef<float> TTSTimeout =
        CVarDef.Create("tts.timeout", 2f, CVar.SERVERONLY);

    /// <summary>
    /// TTS effects on generation enabled.
    /// </summary>
    public static readonly CVarDef<bool> TTSEffectsEnabled =
        CVarDef.Create("tts.effects_enabled", false, CVar.SERVERONLY);

    /// <summary>
    /// Sets count of generated TTS audio saved in cache.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxCacheCount =
        CVarDef.Create("tts.max_cache_count", 400, CVar.SERVERONLY);

    /// <summary>
    /// Max length of message sends to TTS server.
    /// </summary>
    public static readonly CVarDef<int> TTSMessageMaxLength =
        CVarDef.Create("tts.message_max_length", 600, CVar.SERVERONLY);

    public static readonly CVarDef<float> TTSRateLimitPeriod =
        CVarDef.Create("tts.rate_limit_period", 0.5f, CVar.SERVERONLY);

    public static readonly CVarDef<int> TTSRateLimitCount =
        CVarDef.Create("tts.rate_limit_count", 5, CVar.SERVERONLY);
    #endregion
    #region Client

    public static readonly CVarDef<bool> TTSClientEnabled =
        CVarDef.Create("tts.client_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion
}
