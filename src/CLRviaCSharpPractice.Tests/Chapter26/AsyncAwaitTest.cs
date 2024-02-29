using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLRviaCSharpPractice.Tests.Chapter26
{
    [TestClass]
    public class AsyncAwaitTest
    {
        [TestMethod]
        public void TestAsyncMethods()
        {
            Debug.WriteLine($"{nameof(TestAsyncMethods)} begin...{Thread.CurrentThread.ManagedThreadId}");
            var watch = Stopwatch.StartNew();
            var b = false;
            var tasks = new[] {
                this.DoSomeThingAsync(2,b),
                this.DoSomeThingAsync(3,b),
                this.DoSomeThingAsync(4,b)
            };

            Debug.WriteLine($"{string.Join(",", tasks.Select(t => t.Result))}");
            watch.Stop();
            Debug.WriteLine($"total used：{watch.ElapsedMilliseconds / 1000}s");
        }

        [TestMethod]
        public async Task TestAsyncMethodsAsync()
        {
            Debug.WriteLine($"{nameof(TestAsyncMethodsAsync)} begin...{Thread.CurrentThread.ManagedThreadId}");
            var watch = Stopwatch.StartNew();
            var b = true;
            var tasks = new[] {
                this.DoSomeThingAsync(2,b),
                this.DoSomeThingAsync(3,b),
                this.DoSomeThingAsync(4,b)
            };

            await Task.WhenAll(tasks);
            watch.Stop();
            Debug.WriteLine($"total used：{watch.ElapsedMilliseconds / 1000}s");
        }

        [TestMethod]
        public async Task TestAsyncMethods1Async()
        {
            Debug.WriteLine($"{nameof(TestAsyncMethods1Async)} begin...{Thread.CurrentThread.ManagedThreadId}");
            var watch = Stopwatch.StartNew();
            var b = true;
            await this.DoSomeThingAsync(2, b);
            await this.DoSomeThingAsync(3, b);
            await this.DoSomeThingAsync(4, b);

            watch.Stop();
            Debug.WriteLine($"total used：{watch.ElapsedMilliseconds / 1000}s");
        }

        public async Task<string> DoSomeThingAsync(int seconds, bool isAsync = true)
        {
            var watch = Stopwatch.StartNew();
            Debug.WriteLine($"{nameof(DoSomeThingAsync)}-{seconds} begin...{Thread.CurrentThread.ManagedThreadId}");

            if (isAsync)
            {
                await Task.Delay(seconds * 1000);
            }
            else
            {
                Thread.Sleep(seconds * 1000);
            }

            watch.Stop();
            Debug.WriteLine($"{nameof(DoSomeThingAsync)}-{seconds} end! used：{watch.ElapsedMilliseconds / 1000}s");

            return $"used：{watch.ElapsedMilliseconds / 1000}s";
        }
    }
}
