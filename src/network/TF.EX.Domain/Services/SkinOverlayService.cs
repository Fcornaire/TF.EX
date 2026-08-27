using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Reflection;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models.Skin;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain.Services
{
    internal class SkinOverlayService(IGameContext gameContext, ISkinStreamService skinStream, IMatchmakingService matchmaking, ILogger logger) : ISkinOverlayService
    {
        private readonly IGameContext _gameContext = gameContext;
        private readonly ISkinStreamService _skinStream = skinStream;
        private readonly IMatchmakingService _matchmaking = matchmaking;
        private readonly ILogger _logger = logger;

        private static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Dictionary<string, ArcherData> _clones = [];
        private readonly Dictionary<string, StreamedAssets> _streamedAssets = [];
        private readonly HashSet<string> _failed = [];

        private Dictionary<int, string> _replaySkinSeats = [];

        private class StreamedAssets
        {
            public Subtexture PortraitNotJoined;
            public Subtexture PortraitJoined;
            public Subtexture PortraitWin;
            public Subtexture PortraitLose;
            public Subtexture Aimer;
            public Subtexture HatNormal;
            public Subtexture HatBlue;
            public Subtexture HatRed;
            public string BodyId;
            public string HeadNormalId;
            public string HeadNoHatId;
            public string HeadCrownId;
            public string HeadBackId;
            public string BowId;
            public string GemMenuId;
            public string GemGameplayId;
            public FortRise.Option<FortRise.HairInfo> Hair;
            public ArcherSkinBundle Bundle;
        }

        public ArcherData ResolveArcherSkinned(int seat, int characterIndex, int altIndex, ArcherData original)
        {
            try
            {
                if (original == null || characterIndex >= ArcherDataExtensions.VanillaArcherCount)
                {
                    return null;
                }

                if (Models.NetplayPreferences.CustomSkins == Models.CustomSkinMode.Disabled)
                {
                    return null;
                }

                if (!_replaySkinSeats.TryGetValue(seat, out var customArcherId))
                {
                    if (_matchmaking.GetOwnLobby().IsEmpty)
                    {
                        return null;
                    }

                    customArcherId = _gameContext.GetPlayers().FirstOrDefault(entry => entry.Item1 == seat).Item2?.CustomArcherId;
                }

                if (string.IsNullOrEmpty(customArcherId))
                {
                    return null;
                }

                var registered = ArcherRegistryApi.Current?.RegisteredArchers;

                if (registered != null && registered.TryGetValue(customArcherId, out var entry) && entry?.ArcherData != null)
                {
                    return GetOrBuildClone($"local|{customArcherId}|{characterIndex}|{altIndex}", original, () => ApplyLocal(Clone(original), entry));
                }

                var bundle = _skinStream.GetBundle(customArcherId);

                if (bundle == null)
                {
                    return null;
                }

                return GetOrBuildClone($"{bundle.ReceivedBundleId}|{characterIndex}|{altIndex}", original, () => ApplyStreamed(Clone(original), bundle));
            }
            catch (Exception e)
            {
                _logger.LogError<SkinOverlayService>($"Failed to resolve the skin for seat {seat}", e);
                return null;
            }
        }

        public void SetReplaySkinSeats(int[] seats, string[] skinArcherIds)
        {
            var map = new Dictionary<int, string>();

            for (int i = 0; i < (seats?.Length ?? 0) && i < (skinArcherIds?.Length ?? 0); i++)
            {
                if (!string.IsNullOrEmpty(skinArcherIds[i]))
                {
                    map[seats[i]] = skinArcherIds[i];
                }
            }

            _replaySkinSeats = map;
        }

        public void ClearReplaySkinSeats()
        {
            _replaySkinSeats = [];
        }

        public bool HasReplaySkins => _replaySkinSeats.Count > 0;

        private ArcherData GetOrBuildClone(string key, ArcherData original, Func<ArcherData> build)
        {
            if (_clones.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (_failed.Contains(key))
            {
                return null;
            }

            try
            {
                var clone = build();
                _clones[key] = clone;
                _logger.LogDebug<SkinOverlayService>($"Skinned archer built for {key}");
                return clone;
            }
            catch (Exception e)
            {
                _failed.Add(key);
                _logger.LogError<SkinOverlayService>($"Failed to build the skinned archer {key}", e);
                return null;
            }
        }

        private static ArcherData Clone(ArcherData original)
        {
            return (ArcherData)MemberwiseCloneMethod.Invoke(original, null);
        }

        private static ArcherData ApplyLocal(ArcherData clone, FortRise.IArcherEntry entry)
        {
            var custom = entry.ArcherData;

            clone.Name0 = custom.Name0;
            clone.Name1 = custom.Name1;
            clone.Portraits = custom.Portraits;
            clone.Aimer = custom.Aimer ?? clone.Aimer;
            clone.Sprites = custom.Sprites;
            clone.SleepHeadFrame = custom.SleepHeadFrame;
            clone.Breathing = custom.Breathing;
            clone.ExtraHairData = custom.ExtraHairData;

            clone.SFXID = custom.SFXID;
            clone.VictoryMusic = custom.VictoryMusic;
            clone.Gems = custom.Gems;

            clone.Hat = new ArcherData.HatInfo
            {
                Material = clone.Hat.Material,
                Normal = custom.Hat.Normal ?? clone.Hat.Normal,
                Blue = custom.Hat.Blue ?? clone.Hat.Blue,
                Red = custom.Hat.Red ?? clone.Hat.Red,
            };

            return clone;
        }

        private ArcherData ApplyStreamed(ArcherData clone, ArcherSkinBundle bundle)
        {
            var assets = GetOrBuildAssets(bundle);

            clone.Name0 = bundle.Name0 ?? clone.Name0;
            clone.Name1 = bundle.Name1 ?? clone.Name1;

            clone.Portraits = new ArcherData.PortraitInfo
            {
                NotJoined = assets.PortraitNotJoined ?? clone.Portraits.NotJoined,
                Joined = assets.PortraitJoined ?? clone.Portraits.Joined,
                Win = assets.PortraitWin ?? clone.Portraits.Win,
                Lose = assets.PortraitLose ?? clone.Portraits.Lose,
            };

            clone.Aimer = assets.Aimer ?? clone.Aimer;

            clone.Hat = new ArcherData.HatInfo
            {
                Material = clone.Hat.Material,
                Normal = assets.HatNormal ?? clone.Hat.Normal,
                Blue = assets.HatBlue ?? clone.Hat.Blue,
                Red = assets.HatRed ?? clone.Hat.Red,
            };

            clone.Sprites = new ArcherData.SpriteInfo
            {
                Body = assets.BodyId ?? clone.Sprites.Body,
                HeadNormal = assets.HeadNormalId ?? clone.Sprites.HeadNormal,
                HeadNoHat = assets.HeadNoHatId ?? clone.Sprites.HeadNoHat,
                HeadCrown = assets.HeadCrownId ?? clone.Sprites.HeadCrown,
                HeadBack = assets.HeadBackId ?? clone.Sprites.HeadBack,
                Bow = assets.BowId ?? clone.Sprites.Bow,
            };

            if (bundle.SleepHeadFrame > 0 && assets.HeadNormalId != null)
            {
                clone.SleepHeadFrame = bundle.SleepHeadFrame;
            }

            if (bundle.HasBreathing)
            {
                clone.Breathing = new ArcherData.BreathingInfo
                {
                    Interval = bundle.BreathingInterval,
                    Offset = new Vector2(bundle.BreathingOffsetX, bundle.BreathingOffsetY),
                    DuckingOffset = new Vector2(bundle.BreathingDuckingOffsetX, bundle.BreathingDuckingOffsetY),
                };
            }

            clone.ExtraHairData = assets.Hair;

            if (assets.GemMenuId != null || assets.GemGameplayId != null)
            {
                clone.Gems = new ArcherData.GemInfo
                {
                    Menu = assets.GemMenuId ?? clone.Gems.Menu,
                    Gameplay = assets.GemGameplayId ?? clone.Gems.Gameplay,
                };
            }

            return clone;
        }

        private StreamedAssets GetOrBuildAssets(ArcherSkinBundle bundle)
        {
            var hash = bundle.ReceivedBundleId;

            if (_streamedAssets.TryGetValue(hash, out var cached))
            {
                return cached;
            }

            var prefix = $"skin-{(hash.Length >= 16 ? hash.Substring(0, 16) : hash)}";

            var assets = new StreamedAssets
            {
                Bundle = bundle,
                PortraitNotJoined = CreateSubtexture(bundle.PortraitNotJoined),
                PortraitJoined = CreateSubtexture(bundle.PortraitJoined),
                PortraitWin = CreateSubtexture(bundle.PortraitWin),
                PortraitLose = CreateSubtexture(bundle.PortraitLose),
                Aimer = CreateSubtexture(bundle.Aimer),
                HatNormal = CreateSubtexture(bundle.HatNormal),
                HatBlue = CreateSubtexture(bundle.HatBlue),
                HatRed = CreateSubtexture(bundle.HatRed),
                BodyId = RegisterSprite(prefix, "body", bundle.Body),
                HeadNormalId = RegisterSprite(prefix, "headnormal", bundle.HeadNormal),
                HeadNoHatId = RegisterSprite(prefix, "headnohat", bundle.HeadNoHat),
                HeadCrownId = RegisterSprite(prefix, "headcrown", bundle.HeadCrown),
                HeadBackId = RegisterSprite(prefix, "headback", bundle.HeadBack),
                BowId = bundle.Bow?.ExtraData?.ContainsKey("DownY") == true ? RegisterSprite(prefix, "bow", bundle.Bow) : null,
                GemMenuId = RegisterMenuSprite(prefix, "gemmenu", bundle.GemMenu),
                GemGameplayId = RegisterGameplaySprite(prefix, "gemgameplay", bundle.GemGameplay),
            };

            if (bundle.Hair != null)
            {
                assets.Hair = new FortRise.HairInfo
                {
                    Color = PackedColor(bundle.Hair.Color),
                    OutlineColor = PackedColor(bundle.Hair.OutlineColor),
                    Offset = new Vector2(bundle.Hair.OffsetX, bundle.Hair.OffsetY),
                    DuckingOffset = new Vector2(bundle.Hair.DuckingOffsetX, bundle.Hair.DuckingOffsetY),
                    AddLinks = bundle.Hair.AddLinks,
                    AddLinkDistance = bundle.Hair.AddLinkDistance,
                    ShowOnHat = bundle.Hair.ShowOnHat,
                    Texture = RegisterImage($"{prefix}-hair", bundle.Hair.Texture),
                    TextureEnd = RegisterImage($"{prefix}-hairend", bundle.Hair.TextureEnd),
                };
            }

            _streamedAssets[hash] = assets;

            return assets;
        }

        private string RegisterSprite(string prefix, string part, SkinSprite sprite)
        {
            if (sprite?.Sheet == null || ModRegistryApi.Current == null)
            {
                return null;
            }

            var id = $"{prefix}-{part}";
            var mainEntry = RegisterImage(id, sprite.Sheet);

            if (mainEntry == null)
            {
                return null;
            }

            var blueEntry = sprite.BlueSheet != null ? RegisterImage($"{id}-blue", sprite.BlueSheet) ?? mainEntry : mainEntry;
            var redEntry = sprite.RedSheet != null ? RegisterImage($"{id}-red", sprite.RedSheet) ?? mainEntry : mainEntry;

            Dictionary<string, object> additional = null;

            if (sprite.BowXOffsets != null)
            {
                AddExtra(ref additional, "BowXOffsets", string.Join(",", sprite.BowXOffsets));
            }

            if (sprite.BowYOffsets != null)
            {
                AddExtra(ref additional, "BowYOffsets", string.Join(",", sprite.BowYOffsets));
            }

            foreach (var pair in sprite.ExtraData ?? [])
            {
                AddExtra(ref additional, pair.Key, pair.Value);
            }

            var configuration = new FortRise.SpriteConfiguration<string>
            {
                Texture = mainEntry,
                FrameWidth = sprite.FrameWidth,
                FrameHeight = sprite.FrameHeight,
                OriginX = (int)sprite.OriginX,
                OriginY = (int)sprite.OriginY,
                X = sprite.X,
                Y = sprite.Y,
                BlueTexture = blueEntry,
                RedTexture = redEntry,
                HeadXOrigins = sprite.HeadXOrigins,
                HeadYOrigins = sprite.HeadYOrigins,
                AdditionalData = additional,
                Animations = [.. (sprite.Animations ?? [])
                    .Where(animation => animation?.Id != null && animation.Frames != null)
                    .Select(animation => new FortRise.Animation<string>
                    {
                        ID = animation.Id,
                        Frames = animation.Frames,
                        Delay = animation.Delay,
                        Loop = animation.Loop,
                    })],
            };

            ModRegistryApi.Current.Sprites.RegisterSprite(id, configuration);

            return $"{ModRegistryApi.ModName}/{id}";
        }

        private string RegisterMenuSprite(string prefix, string part, SkinSprite sprite)
        {
            if (sprite?.Sheet == null || ModRegistryApi.Current == null)
            {
                return null;
            }

            var id = $"{prefix}-{part}";
            var mainEntry = RegisterImage(id, sprite.Sheet, FortRise.SubtextureAtlasDestination.MenuAtlas);

            if (mainEntry == null)
            {
                return null;
            }

            ModRegistryApi.Current.Sprites.RegisterMenuSprite(id, new FortRise.SpriteConfiguration<string>
            {
                Texture = mainEntry,
                FrameWidth = sprite.FrameWidth,
                FrameHeight = sprite.FrameHeight,
                OriginX = (int)sprite.OriginX,
                OriginY = (int)sprite.OriginY,
                X = sprite.X,
                Y = sprite.Y,
                Animations = [.. (sprite.Animations ?? [])
                    .Where(animation => animation?.Id != null && animation.Frames != null)
                    .Select(animation => new FortRise.Animation<string>
                    {
                        ID = animation.Id,
                        Frames = animation.Frames,
                        Delay = animation.Delay,
                        Loop = animation.Loop,
                    })],
            });

            return $"{ModRegistryApi.ModName}/{id}";
        }

        private string RegisterGameplaySprite(string prefix, string part, SkinSprite sprite)
        {
            if (sprite?.Sheet == null || ModRegistryApi.Current == null)
            {
                return null;
            }

            var id = $"{prefix}-{part}";
            var mainEntry = RegisterImage(id, sprite.Sheet);

            if (mainEntry == null)
            {
                return null;
            }

            ModRegistryApi.Current.Sprites.RegisterSprite(id, new FortRise.SpriteConfiguration<int>
            {
                Texture = mainEntry,
                FrameWidth = sprite.FrameWidth,
                FrameHeight = sprite.FrameHeight,
                OriginX = (int)sprite.OriginX,
                OriginY = (int)sprite.OriginY,
                X = sprite.X,
                Y = sprite.Y,
                Animations = [.. (sprite.Animations ?? [])
                    .Where(animation => animation?.Frames != null && int.TryParse(animation.Id, out _))
                    .Select(animation => new FortRise.Animation<int>
                    {
                        ID = int.Parse(animation.Id),
                        Frames = animation.Frames,
                        Delay = animation.Delay,
                        Loop = animation.Loop,
                    })],
            });

            return $"{ModRegistryApi.ModName}/{id}";
        }

        private void AddExtra(ref Dictionary<string, object> additional, string key, string value)
        {
            additional ??= [];
            additional[key] = value;
        }

        private FortRise.ISubtextureEntry RegisterImage(string id, SkinImage image,FortRise.SubtextureAtlasDestination destination = FortRise.SubtextureAtlasDestination.Atlas)
        {
            var subtexture = CreateSubtexture(image);

            if (subtexture == null || ModRegistryApi.Current == null)
            {
                return null;
            }

            return ModRegistryApi.Current.Subtextures.RegisterTexture(id, () => subtexture, destination);
        }

        private Subtexture CreateSubtexture(SkinImage image)
        {
            if (image == null)
            {
                return null;
            }

            var rgba = _skinStream.InflateRgba(image);

            if (rgba == null)
            {
                return null;
            }

            var texture2D = new Texture2D(Engine.Instance.GraphicsDevice, image.Width, image.Height);
            texture2D.SetData(rgba);

            return new Subtexture(new Monocle.Texture(texture2D));
        }

        private static Color PackedColor(uint packed)
        {
            return new Color { PackedValue = packed };
        }
    }
}
