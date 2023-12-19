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
    }
}
