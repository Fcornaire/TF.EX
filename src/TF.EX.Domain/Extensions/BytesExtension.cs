using System;

namespace TF.EX.Domain.Extensions
{
    public static class BytesExtension //TODO: Removes
    {
        public static byte ToModel(this bool boolean)
        {
            switch (boolean)
            {
                case true:
                    return 1;
                case false:
                    return 0;
            }

            throw new NotImplementedException($"Can't create a byte for bool {boolean} for some reason");
        }

        public static bool ToBool(this byte booleanByte)
        {
            switch (booleanByte)
            {
                case 1:
                    return true;
                case 0:
                    return false;
            }

            throw new NotImplementedException($"Can't create a bool for byte {booleanByte} for some reason");
        }
    }
}
