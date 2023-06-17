using System.Collections;

namespace TF.EX.Domain.Extensions
{
    public static class IEnumeratorExtensions
    {
        public static string GetClassName(this IEnumerator enumerator)
        {
            Type classType = enumerator.GetType().DeclaringType;
            return classType.Name;
        }
    }
}
