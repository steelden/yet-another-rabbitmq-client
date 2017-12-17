using System;

namespace YARC.Messages.Bus
{
    public static class ICanValidateExtensions
    {
        public static bool IsNull<T>(this T obj) where T : class { return obj == null; }
        public static bool NotNull<T>(this T obj) where T : class { return obj != null; }
        public static bool NotNullOrEmpty(this String s) { return !String.IsNullOrEmpty(s); }

        public static bool IsZero(this Int32 i) { return i == 0; }
        public static bool NotZero(this Int32 i) { return i != 0; }
        public static bool GreaterThan(this Int32 i, Int32 num) { return i > num; }
        public static bool GreaterThanOrEqual(this Int32 i, Int32 num) { return i >= num; }
        public static bool LessThen(this Int32 i, Int32 num) { return i < num; }
        public static bool LessThenOrEqual(this Int32 i, Int32 num) { return i <= num; }
        public static bool InRange(this Int32 i, Int32 lo, Int32 hi) { return i >= lo && i <= hi; }
    }
}
