using MessagePack;

namespace TF.State.Domain
{
    public static class StateSerialization
    {
        public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
    }
}
