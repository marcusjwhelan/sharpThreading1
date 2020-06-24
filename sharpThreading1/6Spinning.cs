using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sharpThreading1
{
    class Spinning
    {
        private static bool _done;

        private static void MultiplyXbyY(ref int val, int factor)
        {
            var spinWait = new SpinWait();
            while(true)
            {
                int snapshot1 = val;
                int calc = snapshot1 * factor;
                int snapshot2 = Interlocked.CompareExchange(ref val, calc, snapshot1);
                if (snapshot1 == snapshot2)
                {
                    return; // no one preemted us
                }
                spinWait.SpinOnce();
            }
        }
        public Spinning()
        {
            /*
             * SpinWait example
             */
            // MultiplyXbyY();

            /*
             * SpinLock and spin wait
             */
            //SpinLockSpinWaitExample();
        }

        public static void SpinLockSpinWaitExample()
        {
            Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("Task started");
                    Thread.Sleep(1000);
                    Console.WriteLine("task is done.");
                }
                finally
                {
                    _done = true;
                }
            });
            // spin wait instead
            SpinWait.SpinUntil(() =>
            {
                Thread.MemoryBarrier(); // avoid instruction reordering.. in case of cpu optimizations
                return _done;
            });
            /* while(!_done)
             {
                 Thread.Sleep(10);
             }*/
            Console.WriteLine("The End of program");
            Console.Read();
        }
    }
}
