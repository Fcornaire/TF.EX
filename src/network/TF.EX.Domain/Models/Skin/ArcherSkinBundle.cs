using MessagePack;

namespace TF.EX.Domain.Models.Skin
{
    public static class SkinLimits
    {
        public const int MaxTextureDim = 1024;
        public const int MaxDeflatedImageBytes = 2 * 1024 * 1024;
        public const int MaxBundleBytes = 5 * 1024 * 1024;
        public const int MaxChunkBytes = 192 * 1024;
        public const uint MaxChunks = 32;
        public const int MaxStringLength = 256;
        public const int MaxAnimations = 64;
        public const int MaxFramesPerAnimation = 256;
        public const int MaxOriginEntries = 512;

        public static readonly string[] AllowedExtraDataKeys = { "DownY", "SlideHead", "HideBow" };
    }

    //Towerfall serialized version of a skin

    [MessagePackObject]
    public class ArcherSkinBundle
    {
        [Key(0)]
        public string CustomArcherId { get; set; } = "";

        [Key(1)]
        public string Name0 { get; set; } = "";

        [Key(2)]
        public string Name1 { get; set; } = "";

        [Key(3)]
        public uint ColorA { get; set; }

        [Key(4)]
        public uint ColorB { get; set; }

        [Key(5)]
        public SkinImage PortraitNotJoined { get; set; }

        [Key(6)]
        public SkinImage PortraitJoined { get; set; }

        [Key(7)]
        public SkinImage PortraitWin { get; set; }

        [Key(8)]
        public SkinImage PortraitLose { get; set; }

        [Key(9)]
        public SkinImage Aimer { get; set; }

        [Key(10)]
        public SkinSprite Body { get; set; }

        [Key(11)]
        public SkinSprite HeadNormal { get; set; }

        [Key(12)]
        public SkinSprite HeadNoHat { get; set; }

        [Key(13)]
        public SkinSprite HeadCrown { get; set; }

        [Key(14)]
        public SkinSprite HeadBack { get; set; }

        [Key(15)]
        public SkinSprite Bow { get; set; }

        [Key(16)]
        public SkinImage HatNormal { get; set; }

        [Key(17)]
        public SkinImage HatBlue { get; set; }

        [Key(18)]
        public SkinImage HatRed { get; set; }

        [Key(19)]
        public SkinHair Hair { get; set; }

        [Key(20)]
        public bool HasBreathing { get; set; }

        [Key(21)]
        public int BreathingInterval { get; set; }

        [Key(22)]
        public float BreathingOffsetX { get; set; }

        [Key(23)]
        public float BreathingOffsetY { get; set; }

        [Key(24)]
        public float BreathingDuckingOffsetX { get; set; }

        [Key(25)]
        public float BreathingDuckingOffsetY { get; set; }

        [Key(26)]
        public int SleepHeadFrame { get; set; }

        [Key(27)]
        public SkinSprite GemMenu { get; set; }

        [Key(28)]
        public SkinSprite GemGameplay { get; set; }

        [IgnoreMember]
        public string ReceivedBundleId { get; set; } = "";
    }

    [MessagePackObject]
    public class SkinImage
    {
        [Key(0)]
        public int Width { get; set; }

        [Key(1)]
        public int Height { get; set; }

        [Key(2)]
        public byte[] DeflatedRgba { get; set; }
    }

    [MessagePackObject]
    public class SkinSprite
    {
        [Key(0)]
        public SkinImage Sheet { get; set; }

        [Key(1)]
        public int FrameWidth { get; set; }

        [Key(2)]
        public int FrameHeight { get; set; }

        [Key(3)]
        public float OriginX { get; set; }

        [Key(4)]
        public float OriginY { get; set; }

        [Key(5)]
        public List<SkinAnimation> Animations { get; set; } = new List<SkinAnimation>();

        [Key(6)]
        public SkinImage BlueSheet { get; set; }

        [Key(7)]
        public SkinImage RedSheet { get; set; }

        [Key(8)]
        public int[] HeadXOrigins { get; set; }

        [Key(9)]
        public int[] HeadYOrigins { get; set; }

        [Key(10)]
        public int[] BowXOffsets { get; set; }

        [Key(11)]
        public int[] BowYOffsets { get; set; }

        [Key(12)]
        public int X { get; set; }

        [Key(13)]
        public int Y { get; set; }

        [Key(14)]
        public Dictionary<string, string> ExtraData { get; set; }
    }

    [MessagePackObject]
    public class SkinAnimation
    {
        [Key(0)]
        public string Id { get; set; } = "";

        [Key(1)]
        public float Delay { get; set; }

        [Key(2)]
        public bool Loop { get; set; }

        [Key(3)]
        public int[] Frames { get; set; } = Array.Empty<int>();
    }

    [MessagePackObject]
    public class SkinHair
    {
        [Key(0)]
        public uint Color { get; set; }

        [Key(1)]
        public uint OutlineColor { get; set; }

        [Key(2)]
        public float OffsetX { get; set; }

        [Key(3)]
        public float OffsetY { get; set; }

        [Key(4)]
        public float DuckingOffsetX { get; set; }

        [Key(5)]
        public float DuckingOffsetY { get; set; }

        [Key(6)]
        public int AddLinks { get; set; }

        [Key(7)]
        public float AddLinkDistance { get; set; }

        [Key(8)]
        public bool ShowOnHat { get; set; }

        [Key(9)]
        public SkinImage Texture { get; set; }

        [Key(10)]
        public SkinImage TextureEnd { get; set; }
    }
}
