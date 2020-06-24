using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sharpThreading1
{
    class deadlock
    {
        static readonly object firstLock = new object();
        static readonly object secondLock = new object();

        public deadlock()
        {
            Task.Run((Action)Do);

            // wait until first lock has be grabbed
            Thread.Sleep(500);
            Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-locking secondlock");

            lock (secondLock)
            {
                Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-locked secondlock");

                Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-locking firstlock");
                lock(firstLock)
                {
                    Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-locked firstlock");
                }
                Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-released firstlock");
            }
            Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-released secondlock");

            Console.Read();
        }

        private static void Do()
        {
            Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-Locking FirstLock");
            lock (firstLock)
            {
                Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-Locked FirstLock");
                Thread.Sleep(1000);

                Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-Locking SecondLock");
                lock (secondLock)
                {
                    Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-Locked SecondLock");
                }
                Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-Release SecondLock");
            }
            Console.WriteLine($"\t\t\t\t{Thread.CurrentThread.ManagedThreadId}-Release FirstLock");

        }
    }
}
