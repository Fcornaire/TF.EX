using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class StatusExtension
    {
        public static StatusImpl ToModelGGrsFFI(this Status statusStruct) => new StatusImpl(statusStruct, GGRSFFI.status_info_free);

        public static bool ToStatusBoolModel(this short isOk)
        {
            return isOk != 0;
        }

    }
}
