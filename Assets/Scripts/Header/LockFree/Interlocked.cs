

namespace System.Collections.Generic.LockFree {
    public static class Interlocked {
        public static bool CAS<T>(ref T location, T comparand, T newValue) where T : class {
            return
                comparand == Threading.Interlocked.CompareExchange(ref location, newValue, comparand);
        }
    }
}
