using System.Security.Permissions;
using System.Security;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

namespace CLRviaCSharpPractice.Chapter27
{
    internal class ThreadPoolDemo
    {
        public static void Run()
        {
            Console.WriteLine("Main thread: queuing an asynchronous operation");
            ThreadPool.QueueUserWorkItem(computeBoundOp, 6);
            Console.WriteLine("Main thread: Doing other work here...");
            Thread.Sleep(10000);    // 模拟其他工作(10秒)
            Console.WriteLine("Hit <Enter> to end this program...");
            Console.ReadLine();
        }
        private static void computeBoundOp(Object state)
        {
            // 这个方法由一个线程池线程执行

            Console.WriteLine("In ComputeBoundOp: state={0}", state);
            Thread.Sleep(1000); // 模拟其他工作(1秒)

            // 这个方法返回后，线程回到池中，等待另一个任务
        }
    }

    internal class ExecutionContexts
    {
        public static void Run()
        {
            // Put some data into the Main thread’s logical call context
            CallContext.LogicalSetData("Name", "Jeffrey");

            // Initiate some work to be done by a thread pool thread
            // The thread pool thread can access the logical call context data 
            ThreadPool.QueueUserWorkItem(state => Console.WriteLine("Name={0}", CallContext.LogicalGetData("Name")));

            // Suppress the flowing of the Main thread’s execution context
            ExecutionContext.SuppressFlow();

            // Initiate some work to be done by a thread pool thread
            // The thread pool thread can NOT access the logical call context data
            ThreadPool.QueueUserWorkItem(state => Console.WriteLine("Name={0}", CallContext.LogicalGetData("Name")));

            // Restore the flowing of the Main thread’s execution context in case 
            // it employs more thread pool threads in the future
            ExecutionContext.RestoreFlow();

            // Initiate some work to be done by a thread pool thread
            // The thread pool thread can NOT access the logical call context data
            ThreadPool.QueueUserWorkItem(state => Console.WriteLine("Name={0}", CallContext.LogicalGetData("Name")));

            Console.ReadLine();
            //SecurityExample();
        }

        private static void SecurityExample()
        {
            //ProxyType highSecurityObject = new ProxyType();
            //highSecurityObject.AttemptAccess("High");   // Works OK

            //PermissionSet grantSet = new PermissionSet(PermissionState.None);
            //grantSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            //AppDomain lowSecurityAppDomain = AppDomain.CreateDomain("LowSecurity", null, new AppDomainSetup() { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory }, grantSet, null);
            //ProxyType lowSecurityObject = (ProxyType)lowSecurityAppDomain.CreateInstanceAndUnwrap(typeof(ProxyType).Assembly.ToString(), typeof(ProxyType).FullName);
            //lowSecurityObject.DoSomething(highSecurityObject);
            //Console.ReadLine();
        }

        public sealed class ProxyType : MarshalByRefObject
        {
            // This method executes in the low-security AppDomain
            public void DoSomething(ProxyType highSecurityObject)
            {
                AttemptAccess("High->Low"); // Throws

                // Attempt access from the high-security AppDomain via the low-security AppDomain: Throws
                highSecurityObject.AttemptAccess("High->Low->High");

                // Have the high-security AppDomain via the low-security AppDomain queue a work item to 
                // the thread pool normally (without suppressing the execution context): Throws
                highSecurityObject.AttemptAccessViaThreadPool(false, "TP (with EC)->High");

                // Wait a bit for the work item to complete writing to the console before starting the next work item
                Thread.Sleep(1000);

                // Have the high-security AppDomain via the low-security AppDomain queue a work item to 
                // the thread pool suppressing the execution context: Works OK
                highSecurityObject.AttemptAccessViaThreadPool(true, "TP (no EC)->High");
            }

            public void AttemptAccessViaThreadPool(Boolean suppressExecutionContext, String stack)
            {
                // Since the work item is queued from the high-security AppDomain, the thread pool 
                // thread will start in the High-security AppDomain with the low-security AppDomain's 
                // ExecutionContext (unless it is suppressed when queuing the work item)
                using (suppressExecutionContext ? (IDisposable)ExecutionContext.SuppressFlow() : null)
                {
                    ThreadPool.QueueUserWorkItem(AttemptAccess, stack);
                }
            }

            public void AttemptAccess(Object stack)
            {
                String domain = AppDomain.CurrentDomain.IsDefaultAppDomain() ? "HighSecurity" : "LowSecurity";
                Console.Write("Stack={0}, AppDomain={1}, Username=", stack, domain);
                try
                {
                    Console.WriteLine(Environment.GetEnvironmentVariable("USERNAME"));
                }
                catch (SecurityException)
                {
                    Console.WriteLine("(SecurityException)");
                }
            }
        }
    }

    internal class CancellationDemo
    {
        public static void Run()
        {
            //cancellingAWorkItem();

            //Register();

            linking();
        }

        private static void cancellingAWorkItem()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            // Pass the CancellationToken and the number-to-count-to into the operation
            ThreadPool.QueueUserWorkItem(o => count(cts.Token, 1000));

            Console.WriteLine("Press <Enter> to cancel the operation.");
            Console.ReadLine();
            cts.Cancel();  // If Count returned already, Cancel has no effect on it
                           // Cancel returns immediately, and the method continues running here...

            Console.ReadLine();  // For testing purposes
        }
        private static void count(CancellationToken token, Int32 countTo)
        {
            for (Int32 count = 0; count < countTo; count++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Count is cancelled");
                    break; // Exit the loop to stop the operation
                }

                Console.WriteLine(count);
                Thread.Sleep(200);   // For demo, waste some time
            }
            Console.WriteLine("Count is done");
        }

        private static void register()
        {
            var cts = new CancellationTokenSource();
            cts.Token.Register(() => Console.WriteLine("Canceled 1"));
            cts.Token.Register(() => Console.WriteLine("Canceled 2"));

            ThreadPool.QueueUserWorkItem(o => count(cts.Token, 1000));

            Console.WriteLine("Press <Enter> to cancel the operation.");
            Console.ReadLine();
            // To test, let's just cancel it now and have the 2 callbacks execute
            cts.Cancel();

            Console.ReadLine();  // For testing purposes
        }

        private static void linking()
        {
            // Create a CancellationTokenSource
            var cts1 = new CancellationTokenSource();
            cts1.Token.Register(() => Console.WriteLine("cts1 canceled"));

            // Create another CancellationTokenSource
            var cts2 = new CancellationTokenSource();
            cts2.Token.Register(() => Console.WriteLine("cts2 canceled"));

            // Create a new CancellationTokenSource that is canceled when cts1 or ct2 is canceled
            /* Basically, Constructs a new CTS and registers callbacks with all he passed-in tokens. Each callback calls Cancel(false) on the new CTS */
            var ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);
            ctsLinked.Token.Register(() => Console.WriteLine("linkedCts canceled"));

            // Cancel one of the CancellationTokenSource objects (I chose cts2)
            cts2.Cancel();

            // Display which CancellationTokenSource objects are canceled
            Console.WriteLine("cts1 canceled={0}, cts2 canceled={1}, ctsLinked canceled={2}",
                cts1.IsCancellationRequested, cts2.IsCancellationRequested, ctsLinked.IsCancellationRequested);
        }
    }

    internal static class TaskDemo
    {
        public static void Run()
        {
            //usingTaskInsteadOfQueueUserWorkItem();

            //waitForResult();

            //cancel();

            //continueWith();

            //multipleContinueWith();

            //parentChild();

            //taskFactory();

            unobservedException();

            //synchronizationContextTaskScheduler();
        }

        private static void usingTaskInsteadOfQueueUserWorkItem()
        {
            //ThreadPool.QueueUserWorkItem(ComputeBoundOp, 5); // 调用 QueueUserWorkItem
            //new Task(ComputeBoundOp, 5).Start(); // 用 Task 来做相同的事情
            //Task.Run(() => ComputeBoundOp(5)); // 另一个等价的写法
        }

        private static void waitForResult()
        {
            // Create and start a Task
            Task<Int32> t = new Task<Int32>(n => sum((Int32)n), 10000);

            // You can start the task sometime later
            t.Start();

            // Optionally, you can explicitly wait for the task to complete
            t.Wait(); // FYI: Overloads exist accepting a timeout/CancellationToken

            // Get the result (the Result property internally calls Wait) 
            Console.WriteLine("The sum is: " + t.Result);   // An Int32 value
        }
        private static Int32 sum(Int32 n)
        {
            Int32 sum = 0;
            for (; n > 0; n--) checked { sum += n; }
            return sum;
        }

        private static void cancel()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<Int32> t = Task.Run(() => sum(cts.Token, 10000), cts.Token);

            // Sometime later, cancel the CancellationTokenSource to cancel the Task
            cts.Cancel();

            try
            {
                // If the task got canceled, Result will throw an AggregateException
                Console.WriteLine("The sum is: " + t.Result);   // An Int32 value
            }
            catch (AggregateException ae)
            {
                // Consider any OperationCanceledException objects as handled. 
                // Any other exceptions cause a new AggregateException containing 
                // only the unhandled exceptions to be thrown          
                ae.Handle(e => e is OperationCanceledException);

                // If all the exceptions were handled, the following executes
                Console.WriteLine("Sum was canceled");
            }
        }
        private static Int32 sum(CancellationToken ct, Int32 n)
        {
            Int32 sum = 0;
            for (; n > 0; n--)
            {
                // The following line throws OperationCanceledException when Cancel 
                // is called on the CancellationTokenSource referred to by the token
                ct.ThrowIfCancellationRequested();

                //Thread.Sleep(0);   // Simulate taking a long time
                checked { sum += n; }
            }
            return sum;
        }

        private static void continueWith()
        {
            // Create and start a Task, continue with another task
            Task<Int32> t = Task.Run(() => sum(10000));

            // ContinueWith returns a Task but you usually don't care
            Task cwt = t.ContinueWith(task => Console.WriteLine("The sum is: " + task.Result));
            cwt.Wait();  // For the testing only
        }

        private static void multipleContinueWith()
        {
            // Create and start a Task, continue with multiple other tasks
            Task<Int32> t = Task.Run(() => sum(10000));

            // Each ContinueWith returns a Task but you usually don't care
            t.ContinueWith(task => Console.WriteLine("The sum is: " + task.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            t.ContinueWith(task => Console.WriteLine("Sum threw: " + task.Exception), TaskContinuationOptions.OnlyOnFaulted);
            t.ContinueWith(task => Console.WriteLine("Sum was canceled"), TaskContinuationOptions.OnlyOnCanceled);

            try
            {
                t.Wait();  // For the testing only
            }
            catch (AggregateException)
            {
            }

            Console.ReadLine();
        }

        private static void parentChild()
        {
            Task<Int32[]> parent = new Task<Int32[]>(() =>
            {
                var results = new Int32[3];   // Create an array for the results

                // This tasks creates and starts 3 child tasks
                new Task(() => results[0] = sum(10000), TaskCreationOptions.AttachedToParent).Start();
                new Task(() => results[1] = sum(20000), TaskCreationOptions.AttachedToParent).Start();
                new Task(() => results[2] = sum(30000), TaskCreationOptions.AttachedToParent).Start();

                // Returns a reference to the array (even though the elements may not be initialized yet)
                return results;
            });

            // When the parent and its children have run to completion, display the results
            var cwt = parent.ContinueWith(parentTask => Array.ForEach(parentTask.Result, Console.WriteLine));

            // Start the parent Task so it can start its children
            parent.Start();

            cwt.Wait(); // For testing purposes

            Console.ReadLine();
        }

        private static void taskFactory()
        {
            Task parent = new Task(() =>
            {
                var cts = new CancellationTokenSource();
                var tf = new TaskFactory<Int32>(cts.Token, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                // This tasks creates and starts 3 child tasks
                var childTasks = new[] {
                    tf.StartNew(() => sum(cts.Token, 10000)),
                    tf.StartNew(() => sum(cts.Token, 20000)),
                    tf.StartNew(() => sum(cts.Token, Int32.MaxValue))  // Too big, throws OverflowException
                };

                // If any of the child tasks throw, cancel the rest of them
                for (Int32 task = 0; task < childTasks.Length; task++)
                    childTasks[task].ContinueWith(t => cts.Cancel(), TaskContinuationOptions.OnlyOnFaulted);

                // When all children are done, get the maximum value returned from the non-faulting/canceled tasks
                // Then pass the maximum value to another task which displays the maximum result
                tf.ContinueWhenAll(
                    childTasks,
                    completedTasks => completedTasks.Where(t => t.Status == TaskStatus.RanToCompletion).Max(t => t.Result),
                    CancellationToken.None)
                    .ContinueWith(t => Console.WriteLine("The maximum is: " + t.Result),
                        TaskContinuationOptions.ExecuteSynchronously).Wait(); // Wait is for testing only
            });

            // When the children are done, show any unhandled exceptions too
            parent.ContinueWith(p =>
            {
                // I put all this text in a StringBuilder and call Console.WrteLine just once because this task 
                // could execute concurrently with the task above & I don't want the tasks' output interspersed
                StringBuilder sb = new StringBuilder("The following exception(s) occurred:" + Environment.NewLine);
                foreach (var e in p.Exception.Flatten().InnerExceptions)
                    sb.AppendLine("   " + e.GetType().ToString());
                Console.WriteLine(sb.ToString());
            }, TaskContinuationOptions.OnlyOnFaulted);

            // Start the parent Task so it can start its children
            parent.Start();

            try
            {
                parent.Wait(); // For testing purposes
            }
            catch (AggregateException)
            {
            }

            Console.ReadLine();
        }

        private static void unobservedException()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                //e.SetObserved();
                Console.WriteLine("Unobserved exception {0}", e.Exception, e.Observed);
            };

            Task parent = Task.Factory.StartNew(() =>
            {
                Task child = Task.Factory.StartNew(() => { throw new InvalidOperationException(); }, TaskCreationOptions.AttachedToParent);
                // Child’s exception is observed, but not from its parent
                child.ContinueWith((task) => { var _error = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            });

            // If we do not Wait, the finalizer thread will throw an unhandled exception terminating the process
            //parent.Wait(); // throws AggregateException(AggregateException(InvalidOperationException))

            parent = null;
            Console.ReadLine();  // Wait for the tasks to finish running

            GC.Collect();
            Console.ReadLine();
        }

        private static void synchronizationContextTaskScheduler()
        {
            //var f = new MyForm();
            //System.Windows.Forms.Application.Run();
        }

        private static void computeBoundOp(Object state) { }


        ////private sealed class MyForm : System.Windows.Forms.Form
        //private sealed class MyForm
        //{
        //    private readonly TaskScheduler m_syncContextTaskScheduler;
        //    public MyForm()
        //    {
        //        // Get a reference to a synchronization context task scheduler
        //        m_syncContextTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        //        Text = "Synchronization Context Task Scheduler Demo";
        //        Visible = true; Width = 400; Height = 100;
        //    }

        //    private CancellationTokenSource m_cts;

        //    //protected override void OnMouseClick(System.Windows.Forms.MouseEventArgs e)
        //    protected override void OnMouseClick()
        //    {
        //        if (m_cts != null)
        //        {    // An operation is in flight, cancel it
        //            m_cts.Cancel();
        //            m_cts = null;
        //        }
        //        else
        //        {                // An operation is not in flight, start it
        //            Text = "Operation running";
        //            m_cts = new CancellationTokenSource();

        //            // This task uses the default task scheduler and executes on a thread pool thread
        //            Task<Int32> t = Task.Run(() => Sum(m_cts.Token, 20000), m_cts.Token);

        //            // These tasks use the synchronization context task scheduler and execute on the GUI thread
        //            t.ContinueWith(task => Text = "Result: " + task.Result,
        //                CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
        //                m_syncContextTaskScheduler);

        //            t.ContinueWith(task => Text = "Operation canceled",
        //                CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled,
        //                m_syncContextTaskScheduler);

        //            t.ContinueWith(task => Text = "Operation faulted",
        //                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted,
        //                m_syncContextTaskScheduler);
        //        }
        //        //base.OnMouseClick(e);
        //    }
        //}

        private sealed class ThreadPerTaskScheduler : TaskScheduler
        {
            protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }
            protected override void QueueTask(Task task)
            {
                new Thread(() => TryExecuteTask(task)) { IsBackground = true }.Start();
            }
            protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }
        }
    }

    internal static class ParallelDemo
    {
        public static void Run()
        {
            SimpleUsage();

            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Console.WriteLine("The total bytes of all files in {0} is {1:N0}.", path, DirectoryBytes(@path, "*.*", SearchOption.TopDirectoryOnly));
        }

        private static void SimpleUsage()
        {
            // One thread performs all this work sequentially
            for (Int32 i = 0; i < 1000; i++) DoWork(i);

            // The thread pool’s threads process the work in parallel
            Parallel.For(0, 1000, i => DoWork(i));

            var collection = new Int32[0];
            // One thread performs all this work sequentially
            foreach (var item in collection) DoWork(item);

            // The thread pool’s threads process the work in parallel
            Parallel.ForEach(collection, item => DoWork(item));

            // One thread executes all the methods sequentially
            Method1();
            Method2();
            Method3();

            // The thread pool’s threads execute the methods in parallel
            Parallel.Invoke(
                () => Method1(),
                () => Method2(),
                () => Method3());
        }

        private static Int64 DirectoryBytes(String path, String searchPattern, SearchOption searchOption)
        {
            var files = Directory.EnumerateFiles(path, searchPattern, searchOption);
            Int64 masterTotal = 0;

            ParallelLoopResult result = Parallel.ForEach<String, Int64>(files,
               () =>
               { // localInit: Invoked once per task at start
                 // Initialize that this task has seen 0 bytes
                   return 0;   // Set's taskLocalTotal to 0
               },

               (file, parallelLoopState, index, taskLocalTotal) =>
               { // body: Invoked once per work item
                 // Get this file's size and add it to this task's running total
                   Int64 fileLength = 0;
                   FileStream fs = null;
                   try
                   {
                       fs = File.OpenRead(file);
                       fileLength = fs.Length;
                   }
                   catch (IOException) { /* Ignore any files we can't access */ }
                   finally { if (fs != null) fs.Dispose(); }
                   return taskLocalTotal + fileLength;
               },

               taskLocalTotal =>
               { // localFinally: Invoked once per task at end
                 // Atomically add this task's total to the "master" total
                   Interlocked.Add(ref masterTotal, taskLocalTotal);
               });
            return masterTotal;
        }

        private static void DoWork(Int32 i) { }
        private static void Method1() { }
        private static void Method2() { }
        private static void Method3() { }
    }
}
