using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace sharpThreading1
{
    class AsyncAwait
    {
        public AsyncAwait()
        {
            /* ---------------------------------------------------------------------------
             * Exception handling
             */
            CatchMultipleExceptions();
        }
        private static async void CatchMultipleExceptions()
        {
            int[] numbers = { 0 };
            Task<int> t1 = Task.Run(() => 5 / numbers[0]); // /0
            Task<int> t2 = Task.Run(() => numbers[1]); // index

            Task<int[]> allTask = Task.WhenAll(t1, t2);

            try
            {
                await allTask;
            }
            catch
            {
                foreach( var ex in allTask.Exception.InnerExceptions)
                {
                    Console.WriteLine(ex);
                }
            }
            // Catcher();
            Console.Read();
        }

        static async Task Thrower()
        {
            await Task.Delay(100);
            throw new InvalidOperationException();
        }
        static async Task Catcher()
        {
            try
            {
                Task thrower = Thrower();
                await thrower;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex);
            }
        }
        // multiple tasks 
        private async void Test()
        {
            // don't do this
            Task operation1 = Operation1();
            Task operation2 = Operation2();
            await operation1;
            await operation2;
        }
        private Task Operation1()
        {
            return Task.Delay(TimeSpan.FromMilliseconds(1000));
        }
        private Task Operation2()
        {
            return Task.Delay(TimeSpan.FromMilliseconds(1000));
        }
        // ---------------------------------------
        // None async way of downloading webpage
        private void DumpWebPage(string uri)
        {
            WebClient wc = new WebClient();
            string page = wc.DownloadString(uri);
            Console.WriteLine(page);
        }
        // Async version of DumpWebPage
        private async void DumpWebPageAsync(string uri)
        {
            WebClient wc = new WebClient();
            string page = await wc.DownloadStringTaskAsync(uri);
            Console.WriteLine(page);
        }
        // What the compiler really creates from the async await keywords
        private void DumpWebPageTaskBased(string uri)
        {
            WebClient wc = new WebClient();
            Task<string> task = wc.DownloadStringTaskAsync(uri);
            task.ContinueWith(t => { Console.WriteLine(t.Result); });
        }
    }
}
