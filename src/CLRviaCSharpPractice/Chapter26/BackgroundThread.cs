namespace CLRviaCSharpPractice.Chapter26
{
    internal class BackgroundThread
    {
        public static void Run()
        {
            var isBackground = false;
            Console.WriteLine($"isBackground：{isBackground}");

            // Create a new thread (defaults to Foreground)
            Thread t = new Thread(new ThreadStart(threadMethod));

            // Make the thread a background thread if desired
            if (isBackground) t.IsBackground = true;

            Console.WriteLine("ThreadMethod Start.");
            t.Start(); // Start the thread
            Console.WriteLine("Main Thread Exit.");
        }

        private static void threadMethod()
        {
            //Thread.Sleep(10000); // Simulate 10 seconds of work
            var i = 10;
            while (i-- > 0)
            {
                Console.WriteLine($"ThreadMethod Running({i})...");
                Thread.Sleep(1000);
            }
            Console.WriteLine("ThreadMethod Exited!");
        }

        public static void RunTask()
        {
            Console.WriteLine("ThreadMethod Start.");

            //Task.Factory.StartNew(threadMethod);

            Task.Run(threadMethod);

            Console.WriteLine("Main Thread Exit.");
        }
    }
}
