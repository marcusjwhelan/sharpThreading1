using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace sharpThreading1
{
    public class PrintingInfo
    {
        public int ProcessedNumbers { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            /*
             * Concurrent collections
             */
            ConcurrentCollections c = new ConcurrentCollections();

            /*
             * Spinning
             */
            // Spinning s = new Spinning();

            /*
             * Signaling
             */
            // Signaling s = new Signaling();

            /*
             * Deadlock
             */
            // deadlock d = new deadlock();

            /* 
             * Multithreaded Synchronization
             */
            // MultiThreadedSynchronization ms = new MultiThreadedSynchronization();

            /* 
             * Async await
             */
            // AsyncAwait a = new AsyncAwait();

            /* 
             * Threads - Task Parallel library
             */
            // TPL tpl = new TPL();



            /*
             * this page
             */
            // Program p = new Program();
            // Starting Thread ---
            // p.StartThread();

            // Simple Process ----
            // p.course_process();
        }

        // Starting Thread -----------------------------------------------------------------------------
        public void StartThread()
        {
            // Thread t1 = new Thread(PrintEven);
            // t1.Start();
            // PrintOdd();

            /* 
             * argument - the old way
             */
            // Thread t1 = new Thread(Print1);
            // t1.Start(false);
            // Print1(true);

            // argument the new way
            var printInfo = new PrintingInfo();
            Thread t1 = new Thread(() => Print2(false, printInfo));
            /* regularly threads are Foreground threads. need to mark as background */
            t1.IsBackground = true;
            t1.Start();
            if(t1.Join(TimeSpan.FromMilliseconds(50)))
            {
                Console.WriteLine($"Im sure that spawned thread processed taht many: {printInfo.ProcessedNumbers}");
            }
            else
            {
                Console.WriteLine("Time out. Can't process results");
            }
            /* 
             * Cancel a Thread 
             */
            // Thread.Sleep(10);
            // t1.Abort();  // not supported on this platform


            // Print2(true, printInfo);
        }
        public void Print2(bool isEven, PrintingInfo printInfo)
        {
            try
            {
                if (isEven)
                {
                    PrintEven2(printInfo);
                }
                else
                {
                    PrintOdd2(printInfo);
                }
            }
            catch(ThreadAbortException ex)
            {
                Console.WriteLine(ex);
            }
        }
        public void PrintEven2(PrintingInfo printInfo)
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 == 0)
                {
                    printInfo.ProcessedNumbers++;
                    Console.WriteLine(i);
                }
            }
        }
        public void PrintOdd2(PrintingInfo printInfo)
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 != 0)
                {
                    printInfo.ProcessedNumbers++;
                    Console.WriteLine(i);
                }
            }
        }

        public void Print1(object arg)
        {
            bool isEven = (bool)arg;
            if (isEven)
            {
                PrintEven1();
            }
            else
            {
                PrintOdd1();
            }
        }
        public void PrintEven1()
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 == 0)
                {

                    Console.WriteLine(i);
                }
            }
        }
        public void PrintOdd1()
        {
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 != 0)
                {
                    Console.WriteLine(i);
                }
            }
        }

        // Simple process ------------------------------------------------------------------------------
        public void course_process()
        {
            // Process.Start("notepad.exe"); // can add a file path with ","

            // var app = new Process();
            // app.StartInfo.FileName = @"notepad.exe";
            // app.StartInfo.Arguments = "file path";
            // or
            var app = new Process
            {
                StartInfo =
                {
                    FileName = @"notepad.exe"
                    // Arguments = "file path"
                }
            };



            app.Start();
            app.PriorityClass = ProcessPriorityClass.RealTime;

            // app.WaitForExit(); need to remove this line to kill notepad
            Console.WriteLine("No More Waiting");

            // fetch all processes in system
            var processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                if(p.ProcessName == "notepad")
                {
                    p.Kill();
                }
            }
            Console.Read();
        }
    }
}
