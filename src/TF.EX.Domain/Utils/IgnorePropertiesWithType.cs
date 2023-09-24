using System.Text.Json.Serialization.Metadata;

namespace TF.EX.Domain.Utils
{
    internal class IgnorePropertiesWithType
    {
        private readonly Type[] _ignoredTypes;

        public IgnorePropertiesWithType(params Type[] ignoredTypes)
            => _ignoredTypes = ignoredTypes;

        public void ModifyTypeInfo(JsonTypeInfo ti)
        {
            if (ti.Kind != JsonTypeInfoKind.Object)
                return;

            var toRemove = new List<JsonPropertyInfo>();

            foreach (var prop in ti.Properties)
            {
                if (_ignoredTypes.Contains(prop.PropertyType))
                {
                    toRemove.Add(prop);
                }
            }

            foreach (var prop in toRemove)
            {
                ti.Properties.Remove(prop);
            }

        }
    }
}
