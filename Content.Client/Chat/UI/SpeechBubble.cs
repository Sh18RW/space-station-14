using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI
{
    public abstract class SpeechBubble : Control
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] protected readonly IConfigurationManager ConfigManager = default!;
        private readonly SharedTransformSystem _transformSystem;

        public enum SpeechType : byte
        {
            Emote,
            Say,
            Whisper,
            Looc
        }

        /// <summary>
        ///     The total time a speech bubble stays on screen.
        /// </summary>
        private const float TotalTime = 4;

        /// <summary>
        ///     The amount of time at the end of the bubble's life at which it starts fading.
        /// </summary>
        private const float FadeTime = 0.25f;

        /// <summary>
        ///     The distance in world space to offset the speech bubble from the center of the entity.
        ///     i.e. greater -> higher above the mob's head.
        /// </summary>
        private const float EntityVerticalOffset = 0.5f;

        /// <summary>
        ///     The default maximum width for speech bubbles.
        /// </summary>
        public const float SpeechMaxWidth = 256;

        private readonly EntityUid _senderEntity;

        private float _timeLeft = TotalTime;

        public float VerticalOffset { get; set; }
        private float _verticalOffsetAchieved;

        public Vector2 ContentSize { get; private set; }

        // man down
        public event Action<EntityUid, SpeechBubble>? OnDied;

        public static List<SpeechBubble> CreateSpeechBubbles(SpeechType type, string message, EntityUid senderEntity)
        {
            var bubbles = new List<SpeechBubble>();
            var bubblesData = new List<(string[] message, SpeechType type)>();
            var bubbleContent = ExtractAndFormatSpeechSubstring(message, "BubbleContent");
            if (type is SpeechType.Say or SpeechType.Whisper)
            {
                var bubbleHeader = ExtractAndFormatSpeechSubstring(message, "BubbleHeader");
                foreach (var data in SharedChatSystem.ExtractEmotes(bubbleContent))
                {
                    bubblesData.Add(data.isEmote ? ([data.content], SpeechType.Emote) : ([bubbleHeader, data.content], type));
                }

            }
            else
            {
                bubblesData.Add(([bubbleContent], type));
            }

            var hideName = false;
            foreach (var bubbleData in bubblesData)
            {
                switch (bubbleData.type)
                {
                    case SpeechType.Emote:
                        bubbles.Add(new TextSpeechBubble(bubbleData.message[0], senderEntity, "emoteBox"));
                        break;
                    case SpeechType.Say:
                        bubbles.Add(new FancyTextSpeechBubble(bubbleData.message[0], bubbleData.message[1], senderEntity, "sayBox", hideHeader: hideName));
                        hideName = true;
                        break;
                    case SpeechType.Whisper:
                        bubbles.Add(new FancyTextSpeechBubble(bubbleData.message[0], bubbleData.message[1], senderEntity, "whisperBox", hideHeader: hideName));
                        hideName = true;
                        break;
                    case SpeechType.Looc:
                        bubbles.Add(new TextSpeechBubble(bubbleData.message[0], senderEntity, "emoteBox", Color.FromHex("#48d1cc")));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return bubbles;
        }

        protected SpeechBubble(string[] message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null, bool hideHeader = false)
        {
            IoCManager.InjectDependencies(this);
            _senderEntity = senderEntity;
            _transformSystem = _entityManager.System<SharedTransformSystem>();

            // Use text clipping so new messages don't overlap old ones being pushed up.
            RectClipContent = true;

            var bubble = BuildBubble(message, speechStyleClass, fontColor, hideHeader);

            AddChild(bubble);

            ForceRunStyleUpdate();

            bubble.Measure(Vector2Helpers.Infinity);
            ContentSize = bubble.DesiredSize;
            _verticalOffsetAchieved = -ContentSize.Y;
        }

        protected abstract Control BuildBubble(string[] message, string speechStyleClass, Color? fontColor = null, bool hideHeader = false);

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _timeLeft -= args.DeltaSeconds;
            if (_entityManager.Deleted(_senderEntity) || _timeLeft <= 0)
            {
                // Timer spawn to prevent concurrent modification exception.
                Timer.Spawn(0, Die);
                return;
            }

            // Lerp to our new vertical offset if it's been modified.
            if (MathHelper.CloseToPercent(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
            {
                _verticalOffsetAchieved = VerticalOffset;
            }
            else
            {
                _verticalOffsetAchieved = MathHelper.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform) || xform.MapID != _eyeManager.CurrentMap)
            {
                Modulate = Color.White.WithAlpha(0);
                return;
            }

            if (_timeLeft <= FadeTime)
            {
                // Update alpha if we're fading.
                Modulate = Color.White.WithAlpha(_timeLeft / FadeTime);
            }
            else
            {
                // Make opaque otherwise, because it might have been hidden before
                Modulate = Color.White;
            }

            var baseOffset = 0f;

           if (_entityManager.TryGetComponent<SpeechComponent>(_senderEntity, out var speech))
                baseOffset = speech.SpeechBubbleOffset;

            var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -(EntityVerticalOffset + baseOffset);
            var worldPos = _transformSystem.GetWorldPosition(xform) + offset;

            var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;
            var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y + _verticalOffsetAchieved);
            // Round to nearest 0.5
            screenPos = (screenPos * 2).Rounded() / 2;
            LayoutContainer.SetPosition(this, screenPos);

            var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentSize.Y));
            SetHeight = height;
        }

        private void Die()
        {
            if (Disposed)
            {
                return;
            }

            OnDied?.Invoke(_senderEntity, this);
        }

        /// <summary>
        ///     Causes the speech bubble to start fading IMMEDIATELY.
        /// </summary>
        public void FadeNow()
        {
            if (_timeLeft > FadeTime)
            {
                _timeLeft = FadeTime;
            }
        }

        protected static FormattedMessage FormatSpeech(string message, Color? fontColor = null)
        {
            var msg = new FormattedMessage();
            if (fontColor != null)
                msg.PushColor(fontColor.Value);
            msg.AddMarkupOrThrow(message);
            return msg;
        }

        private static string ExtractAndFormatSpeechSubstring(string message, string tag)
        {
            return SharedChatSystem.GetStringInsideTag(message, tag);
        }

    }

    public sealed class TextSpeechBubble(
        string message,
        EntityUid senderEntity,
        string speechStyleClass,
        Color? fontColor = null)
        : SpeechBubble([message], senderEntity, speechStyleClass, fontColor)
    {
        protected override Control BuildBubble(string[] message, string speechStyleClass, Color? fontColor = null, bool hideHeader = false)
        {
            var label = new RichTextLabel
            {
                MaxWidth = SpeechMaxWidth,
            };

            label.SetMessage(FormatSpeech(message[0], fontColor));

            var panel = new PanelContainer
            {
                StyleClasses = { "speechBox", speechStyleClass },
                Children = { label },
                ModulateSelfOverride = Color.White.WithAlpha(0.75f)
            };

            return panel;
        }
    }

    public sealed class FancyTextSpeechBubble : SpeechBubble
    {
        public FancyTextSpeechBubble(string header, string message, EntityUid senderEntity, string speechStyleClass, Color? fontColor = null, bool hideHeader = false)
            : base([header, message], senderEntity, speechStyleClass, fontColor, hideHeader)
        {
        }

        protected override Control BuildBubble(string[] message, string speechStyleClass, Color? fontColor = null, bool hideHeader = false)
        {
            if (!ConfigManager.GetCVar(CCVars.ChatEnableFancyBubbles) || hideHeader)
            {
                var label = new RichTextLabel
                {
                    MaxWidth = SpeechMaxWidth
                };

                label.SetMessage(FormatSpeech(message[1], fontColor));

                var unfanciedPanel = new PanelContainer
                {
                    StyleClasses = { "speechBox", speechStyleClass },
                    Children = { label },
                    ModulateSelfOverride = Color.White.WithAlpha(0.75f)
                };
                return unfanciedPanel;
            }

            var bubbleHeader = new RichTextLabel
            {
                Margin = new Thickness(1, 1, 1, 1)
            };

            var bubbleContent = new RichTextLabel
            {
                MaxWidth = SpeechMaxWidth,
                Margin = new Thickness(2, 6, 2, 2),
                StyleClasses = { "bubbleContent" }
            };

            //We'll be honest. *Yes* this is hacky. Doing this in a cleaner way would require a bottom-up refactor of how saycode handles sending chat messages. -Myr
            bubbleHeader.SetMessage(FormatSpeech(message[0], fontColor));
            bubbleContent.SetMessage(FormatSpeech(message[1], fontColor));

            //As for below: Some day this could probably be converted to xaml. But that is not today. -Myr
            var mainPanel = new PanelContainer
            {
                StyleClasses = { "speechBox", speechStyleClass },
                Children = { bubbleContent },
                ModulateSelfOverride = Color.White.WithAlpha(0.75f),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Bottom,
                Margin = new Thickness(4, 14, 4, 2)
            };

            var headerPanel = new PanelContainer
            {
                StyleClasses = { "speechBox", speechStyleClass },
                Children = { bubbleHeader },
                ModulateSelfOverride = Color.White.WithAlpha(ConfigManager.GetCVar(CCVars.ChatFancyNameBackground) ? 0.75f : 0f),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top
            };

            var panel = new PanelContainer
            {
                Children = { mainPanel, headerPanel }
            };

            return panel;
        }
    }
}
