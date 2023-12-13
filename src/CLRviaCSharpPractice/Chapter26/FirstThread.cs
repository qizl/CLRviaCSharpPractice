namespace CLRviaCSharpPractice.Chapter26
{
    internal class FirstThread
    {
        public static void Run()
        {
            Console.WriteLine("Main thread: starting a dedicated thread to do an asynchronous operation");
            Thread dedicatedThread = new Thread(computeBoundOp);
            dedicatedThread.Start(5);

            Console.WriteLine("Main thread: Doing other work here...");
            Thread.Sleep(1000);     // Simulating other work (10 seconds)

            dedicatedThread.Join();  // Wait for thread to terminate
            Console.ReadLine();
        }

        // This method's signature must match the ParametizedThreadStart delegate
        private static void computeBoundOp(object state)
        {
            // This method is executed by another thread

            Console.WriteLine("In ComputeBoundOp: state={0}", state);
            Thread.Sleep(1000);  // Simulates other work (1 second)

            // When this method returns, the dedicated thread dies
        }
    }
}
