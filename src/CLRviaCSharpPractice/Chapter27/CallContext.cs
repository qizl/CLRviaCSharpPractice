using System.Collections.Concurrent;

namespace CLRviaCSharpPractice.Chapter27
{
    /// <summary>
    /// 参考：https://blog.csdn.net/qq_33649351/article/details/80016359
    /// </summary>
    public static class CallContext
    {
        static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

        public static void LogicalSetData(string name, object data) => state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

        public static object LogicalGetData(string name) => state.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : null;
    }
}
