using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Models.Skin;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Domain.Services
{
    internal class SkinStreamService(ILogger logger) : ISkinStreamService
    {
        private readonly Dictionary<(int, int), ArtifactSkinBundle> _localBundles = [];
        private volatile ArtifactSkinBundle _lastPublished;

        private readonly ConcurrentDictionary<string, PendingAssembly> _incoming = new();
        private readonly ConcurrentDictionary<string, ArcherSkinBundle> _bundles = new();
        private readonly ConcurrentDictionary<string, byte> _completedIds = new();

        private const int MaxConcurrentAssemblies = 8;

        private class PendingAssembly
        {
            public byte[][] Parts;
        }

        public ArtifactSkinBundle GetOrBuildArcherBundle(int archerIndex, int archerAltIndex)
        {
            if (_localBundles.TryGetValue((archerIndex, archerAltIndex), out var cached))
            {
                return cached;
            }

            ArtifactSkinBundle bundle = null;

            try
            {
                bundle = Build(archerIndex, archerAltIndex);
            }
            catch (Exception e)
            {
                logger.LogError<SkinStreamService>($"Failed to build the skin bundle for archer {archerIndex}-{archerAltIndex}", e);
            }

            _localBundles[(archerIndex, archerAltIndex)] = bundle;

            return bundle;
        }

        public ArtifactSkinBundle GetLastPublished()
        {
            return _lastPublished;
        }

        public void MarkPublished(ArtifactSkinBundle bundle)
        {
            _lastPublished = bundle;
        }

        public void ReceiveChunk(string fromPeerId, SkinChunk chunk)
        {
            if (chunk == null || string.IsNullOrEmpty(fromPeerId) || Models.NetplayPreferences.CustomSkins == Models.CustomSkinMode.Disabled)
            {
                return;
            }

            byte[] part;

            try
            {
                part = Convert.FromBase64String(chunk.Data ?? "");
            }
            catch (FormatException)
            {
                return;
            }

            if (_completedIds.ContainsKey(chunk.BundleId))
            {
                return;
            }

            if (chunk.ChunkCount == 0 || chunk.ChunkCount > SkinLimits.MaxChunks
                || chunk.ChunkIndex >= chunk.ChunkCount
                || chunk.BundleId.Length > 128
                || (chunk.CustomArcherId?.Length ?? 0) > SkinLimits.MaxStringLength
                || part.Length == 0 || part.Length > SkinLimits.MaxChunkBytes)
            {
                return;
            }

            var stale = _incoming.Keys.Where(key => key.StartsWith($"{fromPeerId}|", StringComparison.Ordinal) && key != $"{fromPeerId}|{chunk.BundleId}").ToList();
            foreach (var key in stale)
            {
                _incoming.TryRemove(key, out _);
            }

            var assemblyKey = $"{fromPeerId}|{chunk.BundleId}";

            if (!_incoming.TryGetValue(assemblyKey, out var assembly))
            {
                if (_incoming.Count >= MaxConcurrentAssemblies)
                {
                    return;
                }

                assembly = new PendingAssembly { Parts = new byte[chunk.ChunkCount][] };
                _incoming[assemblyKey] = assembly;
            }

            if (assembly.Parts.Length != chunk.ChunkCount)
            {
                _incoming.TryRemove(assemblyKey, out _);
                return;
            }

            assembly.Parts[chunk.ChunkIndex] = part;

            if (assembly.Parts.Any(p => p == null))
            {
                return;
            }

            _incoming.TryRemove(assemblyKey, out _);

            var total = assembly.Parts.SelectMany(p => p).ToArray();

            if (total.Length > SkinLimits.MaxBundleBytes || Sha256Hex(total) != chunk.BundleId)
            {
                return;
            }

            ArcherSkinBundle bundle;

            try
            {
                bundle = MessagePackSerializer.Deserialize<ArcherSkinBundle>(total);
            }
            catch (Exception)
            {
                return;
            }

            if (bundle == null || bundle.CustomArcherId != chunk.CustomArcherId || !IsValidBundle(bundle))
            {
                return;
            }

            bundle.ReceivedBundleId = chunk.BundleId;

            _completedIds[chunk.BundleId] = 1;
            _bundles[bundle.CustomArcherId] = bundle;

            logger.LogDebug<SkinStreamService>($"Skin bundle received for {bundle.CustomArcherId} ({total.Length} bytes)");
        }

        public ArcherSkinBundle GetBundle(string customArcherId)
        {
            return !string.IsNullOrEmpty(customArcherId) && _bundles.TryGetValue(customArcherId, out var bundle) ? bundle : null;
        }

        public byte[] InflateRgba(SkinImage image)
        {
            if (image?.DeflatedRgba == null)
            {
                return null;
            }

            var expected = image.Width * image.Height * 4;

            if (expected <= 0 || expected > SkinLimits.MaxTextureDim * SkinLimits.MaxTextureDim * 4)
            {
                return null;
            }

            try
            {
                using var input = new MemoryStream(image.DeflatedRgba);
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);

                var output = new byte[expected];
                var read = 0;

                while (read < expected)
                {
                    var n = deflate.Read(output, read, expected - read);

                    if (n <= 0)
                    {
                        return null;
                    }

                    read += n;
                }

                return deflate.ReadByte() == -1 ? output : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Reset()
        {
            _incoming.Clear();
            _bundles.Clear();
            _completedIds.Clear();
            _localBundles.Clear();
            _lastPublished = null;
        }

        private ArtifactSkinBundle Build(int archerIndex, int archerAltIndex)
        {
            var entries = Interop.ArcherRegistryApi.Current?.GetAllArchers();

            var entry = entries?.FirstOrDefault(e => e?.Index == archerIndex && (int)e.Type == archerAltIndex)
                ?? entries?.FirstOrDefault(e => e?.Index == archerIndex && e.Type == FortRise.ArcherEntryType.Normal)
                ?? entries?.FirstOrDefault(e => e?.Index == archerIndex);

            if (entry?.ArcherData == null)
            {
                return null;
            }

            var data = entry.ArcherData;

            var bundle = new ArcherSkinBundle
            {
                CustomArcherId = entry.Name ?? "",
                Name0 = Truncate(data.Name0),
                Name1 = Truncate(data.Name1),
                ColorA = data.ColorA.PackedValue,
                ColorB = data.ColorB.PackedValue,
                PortraitNotJoined = FromSubtexture(data.Portraits.NotJoined),
                PortraitJoined = FromSubtexture(data.Portraits.Joined),
                PortraitWin = FromSubtexture(data.Portraits.Win),
                PortraitLose = FromSubtexture(data.Portraits.Lose),
                Aimer = FromSubtexture(data.Aimer),
                Body = FromSpriteId(data.Sprites.Body),
                HeadNormal = FromSpriteId(data.Sprites.HeadNormal),
                HeadNoHat = FromSpriteId(data.Sprites.HeadNoHat),
                HeadCrown = FromSpriteId(data.Sprites.HeadCrown),
                HeadBack = FromSpriteId(data.Sprites.HeadBack),
                Bow = FromSpriteId(data.Sprites.Bow),
                HatNormal = FromSubtexture(data.Hat.Normal),
                HatBlue = FromSubtexture(data.Hat.Blue),
                HatRed = FromSubtexture(data.Hat.Red),
                SleepHeadFrame = data.SleepHeadFrame,
                GemMenu = FromGemSpriteId(TFGame.MenuSpriteData, data.Gems.Menu, intKeyed: false),
                GemGameplay = FromGemSpriteId(TFGame.SpriteData, data.Gems.Gameplay, intKeyed: true),
                HasBreathing = data.Breathing.Interval > 0,
                BreathingInterval = data.Breathing.Interval,
                BreathingOffsetX = data.Breathing.Offset.X,
                BreathingOffsetY = data.Breathing.Offset.Y,
                BreathingDuckingOffsetX = data.Breathing.DuckingOffset.X,
                BreathingDuckingOffsetY = data.Breathing.DuckingOffset.Y,
            };

            if (entry.Configuration.Hair.TryGetValue(out var hair))
            {
                bundle.Hair = new SkinHair
                {
                    Color = hair.Color.PackedValue,
                    OutlineColor = hair.OutlineColor.PackedValue,
                    OffsetX = hair.Offset.X,
                    OffsetY = hair.Offset.Y,
                    DuckingOffsetX = hair.DuckingOffset.X,
                    DuckingOffsetY = hair.DuckingOffset.Y,
                    AddLinks = hair.AddLinks,
                    AddLinkDistance = hair.AddLinkDistance,
                    ShowOnHat = hair.ShowOnHat,
                    Texture = FromSubtexture(hair.Texture?.Subtexture),
                    TextureEnd = FromSubtexture(hair.TextureEnd?.Subtexture),
                };
            }

            var bytes = MessagePackSerializer.Serialize(bundle);

            if (bytes.Length > SkinLimits.MaxBundleBytes)
            {
                logger.LogError<SkinStreamService>($"Skin bundle for {bundle.CustomArcherId} is too large to stream ({bytes.Length} bytes)");
                return null;
            }

            return new ArtifactSkinBundle
            {
                BundleId = Sha256Hex(bytes),
                CustomArcherId = bundle.CustomArcherId,
                Bytes = bytes,
            };
        }

        private static SkinImage FromSubtexture(Subtexture subtexture)
        {
            var texture2D = subtexture?.Texture?.Texture2D;

            return texture2D == null ? null : FromTextureRegion(texture2D, subtexture.Rect);
        }

        private static SkinImage FromTextureRegion(Texture2D texture, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || rect.Width > SkinLimits.MaxTextureDim || rect.Height > SkinLimits.MaxTextureDim)
            {
                return null;
            }

            var pixels = new Color[rect.Width * rect.Height];
            texture.GetData(0, rect, pixels, 0, pixels.Length);

            var rgba = new byte[pixels.Length * 4];

            for (int i = 0; i < pixels.Length; i++)
            {
                rgba[i * 4] = pixels[i].R;
                rgba[i * 4 + 1] = pixels[i].G;
                rgba[i * 4 + 2] = pixels[i].B;
                rgba[i * 4 + 3] = pixels[i].A;
            }

            return new SkinImage
            {
                Width = rect.Width,
                Height = rect.Height,
                DeflatedRgba = Deflate(rgba),
            };
        }

        private static SkinSprite FromSpriteId(string id)
        {
            if (string.IsNullOrEmpty(id) || TFGame.SpriteData?.Contains(id) != true)
            {
                return null;
            }

            var xml = TFGame.SpriteData.GetXML(id);
            var sprite = TFGame.SpriteData.GetSpriteString(id);
            var texture2D = sprite?.Texture?.Texture2D;

            if (texture2D == null || xml == null)
            {
                return null;
            }

            var skinSprite = new SkinSprite
            {
                Sheet = FromTextureRegion(texture2D, sprite.ClipRect),
                FrameWidth = xml.ChildInt("FrameWidth"),
                FrameHeight = xml.ChildInt("FrameHeight"),
                OriginX = xml.ChildFloat("OriginX", 0f),
                OriginY = xml.ChildFloat("OriginY", 0f),
                X = (int)xml.ChildFloat("X", 0f),
                Y = (int)xml.ChildFloat("Y", 0f),
                BlueSheet = FromAtlasChild(xml, "BlueTexture"),
                RedSheet = FromAtlasChild(xml, "RedTexture"),
                HeadXOrigins = CsvChild(xml, "HeadXOrigins"),
                HeadYOrigins = CsvChild(xml, "HeadYOrigins"),
                BowXOffsets = CsvChild(xml, "BowXOffsets"),
                BowYOffsets = CsvChild(xml, "BowYOffsets"),
            };

            foreach (var key in SkinLimits.AllowedExtraDataKeys)
            {
                if (xml.HasChild(key))
                {
                    skinSprite.ExtraData ??= new Dictionary<string, string>();
                    skinSprite.ExtraData[key] = Truncate(xml.ChildText(key));
                }
            }

            ReadAnimations(xml, skinSprite);

            return skinSprite.Sheet == null ? null : skinSprite;
        }

        private static SkinSprite FromGemSpriteId(SpriteData spriteData, string id, bool intKeyed)
        {
            if (string.IsNullOrEmpty(id) || spriteData?.Contains(id) != true)
            {
                return null;
            }

            try
            {
                var xml = spriteData.GetXML(id);

                Microsoft.Xna.Framework.Graphics.Texture2D texture2D;
                Rectangle clipRect;

                if (intKeyed)
                {
                    var sprite = spriteData.GetSpriteInt(id);
                    texture2D = sprite?.Texture?.Texture2D;
                    clipRect = sprite?.ClipRect ?? default;
                }
                else
                {
                    var sprite = spriteData.GetSpriteString(id);
                    texture2D = sprite?.Texture?.Texture2D;
                    clipRect = sprite?.ClipRect ?? default;
                }

                if (texture2D == null || xml == null)
                {
                    return null;
                }

                var skinSprite = new SkinSprite
                {
                    Sheet = FromTextureRegion(texture2D, clipRect),
                    FrameWidth = xml.ChildInt("FrameWidth"),
                    FrameHeight = xml.ChildInt("FrameHeight"),
                    OriginX = xml.ChildFloat("OriginX", 0f),
                    OriginY = xml.ChildFloat("OriginY", 0f),
                    X = (int)xml.ChildFloat("X", 0f),
                    Y = (int)xml.ChildFloat("Y", 0f),
                };

                ReadAnimations(xml, skinSprite);

                return skinSprite.Sheet == null ? null : skinSprite;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ReadAnimations(System.Xml.XmlElement xml, SkinSprite skinSprite)
        {
            var animations = xml["Animations"];

            if (animations != null)
            {
                foreach (System.Xml.XmlElement anim in animations.GetElementsByTagName("Anim"))
                {
                    skinSprite.Animations.Add(new SkinAnimation
                    {
                        Id = Truncate(anim.Attr("id")),
                        Delay = anim.AttrFloat("delay", 0f),
                        Loop = anim.AttrBool("loop", true),
                        Frames = Calc.ReadCSVInt(anim.Attr("frames")),
                    });
                }
            }
        }

        private static SkinImage FromAtlasChild(System.Xml.XmlElement xml, string childName)
        {
            if (!xml.HasChild(childName))
            {
                return null;
            }

            try
            {
                return FromSubtexture(TFGame.Atlas[xml.ChildText(childName)]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static int[] CsvChild(System.Xml.XmlElement xml, string childName)
        {
            return xml.HasChild(childName) ? Calc.ReadCSVInt(xml.ChildText(childName)) : null;
        }

        private static bool IsValidBundle(ArcherSkinBundle bundle)
        {
            if ((bundle.CustomArcherId?.Length ?? 0) is 0 or > SkinLimits.MaxStringLength
                || (bundle.Name0?.Length ?? 0) > SkinLimits.MaxStringLength
                || (bundle.Name1?.Length ?? 0) > SkinLimits.MaxStringLength)
            {
                return false;
            }

            var images = new[]
            {
                bundle.PortraitNotJoined, bundle.PortraitJoined, bundle.PortraitWin, bundle.PortraitLose,
                bundle.Aimer, bundle.HatNormal, bundle.HatBlue, bundle.HatRed,
                bundle.Hair?.Texture, bundle.Hair?.TextureEnd,
            };

            if (images.Any(image => image != null && !IsValidImage(image)))
            {
                return false;
            }

            var sprites = new[] { bundle.Body, bundle.HeadNormal, bundle.HeadNoHat, bundle.HeadCrown, bundle.HeadBack, bundle.Bow, bundle.GemMenu, bundle.GemGameplay };

            foreach (var sprite in sprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                if (!IsValidImage(sprite.Sheet)
                    || sprite.FrameWidth <= 0 || sprite.FrameWidth > SkinLimits.MaxTextureDim
                    || sprite.FrameHeight <= 0 || sprite.FrameHeight > SkinLimits.MaxTextureDim
                    || sprite.FrameWidth > sprite.Sheet.Width || sprite.FrameHeight > sprite.Sheet.Height
                    || (sprite.Animations?.Count ?? 0) > SkinLimits.MaxAnimations)
                {
                    return false;
                }

                if ((sprite.BlueSheet != null && !IsValidImage(sprite.BlueSheet)) || (sprite.RedSheet != null && !IsValidImage(sprite.RedSheet)))
                {
                    return false;
                }

                var arrays = new[] { sprite.HeadXOrigins, sprite.HeadYOrigins, sprite.BowXOffsets, sprite.BowYOffsets };

                if (arrays.Any(array => array != null && array.Length > SkinLimits.MaxOriginEntries))
                {
                    return false;
                }

                if (sprite.ExtraData != null
                    && (sprite.ExtraData.Count > SkinLimits.AllowedExtraDataKeys.Length
                        || sprite.ExtraData.Any(pair => !SkinLimits.AllowedExtraDataKeys.Contains(pair.Key)
                            || (pair.Value?.Length ?? 0) > 32)))
                {
                    return false;
                }

                var maxFrame = (sprite.Sheet.Width / sprite.FrameWidth) * (sprite.Sheet.Height / sprite.FrameHeight);

                foreach (var animation in sprite.Animations ?? Enumerable.Empty<SkinAnimation>())
                {
                    if (animation == null
                        || (animation.Id?.Length ?? 0) > SkinLimits.MaxStringLength
                        || animation.Frames == null || animation.Frames.Length == 0
                        || animation.Frames.Length > SkinLimits.MaxFramesPerAnimation
                        || animation.Frames.Any(frame => frame < 0 || frame >= maxFrame))
                    {
                        return false;
                    }
                }
            }

            if (bundle.Body != null)
            {
                var bodyMaxFrame = bundle.Body.Animations?
                    .SelectMany(animation => animation.Frames)
                    .DefaultIfEmpty(0)
                    .Max() ?? 0;

                if (bundle.Body.HeadYOrigins == null || bundle.Body.HeadYOrigins.Length <= bodyMaxFrame)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidImage(SkinImage image)
        {
            return image != null
                && image.Width > 0 && image.Width <= SkinLimits.MaxTextureDim
                && image.Height > 0 && image.Height <= SkinLimits.MaxTextureDim
                && image.DeflatedRgba != null
                && image.DeflatedRgba.Length > 0
                && image.DeflatedRgba.Length <= SkinLimits.MaxDeflatedImageBytes;
        }

        private static byte[] Deflate(byte[] data)
        {
            using var output = new MemoryStream();

            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            {
                deflate.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        private static string Sha256Hex(byte[] data)
        {
            return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        }

        private static string Truncate(string value)
        {
            return value == null ? "" : value.Length <= SkinLimits.MaxStringLength ? value : value.Substring(0, SkinLimits.MaxStringLength);
        }
    }
}
