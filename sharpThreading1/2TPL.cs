using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace sharpThreading1
{
    class TPL // task parallel library
    {
        private const string FilePath = @"c:\temp\demo.txt";
 
        public TPL()
        {
            /* -----------------------------------------------------------------------------------
             * Nested and child tasks
             */
            Task.Factory.StartNew(() =>
            {
                // will not print
                // Task nested = Task.Factory.StartNew(() => Console.WriteLine("Hello World"));
                // convert nested task to child task
                Task nestedToChild = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Hello World");
                }, TaskCreationOptions.AttachedToParent);

                // Can set a parent to not allow child tasks
            }).Wait();
            Thread.Sleep(100);
            /* -----------------------------------------------------------------------------------
             * Error Handling 2 multiple exceptions
             */
            // ErrorHandleAggregateException();

            /* -----------------------------------------------------------------------------------
             * Error Handling 1
             */
            // ErrorHandleOneException();

            /* -----------------------------------------------------------------------------------
             * test from async
             */
            // TestFromAsync();

            /* -----------------------------------------------------------------------------------
             * Continue but only after a delay
             */
            // ContinueTaskAfterDelay();

            /* -----------------------------------------------------------------------------------
             * Continue when all tasks are complete
             */
            // ContinueTasks();

            /* -----------------------------------------------------------------------------------
             * Chaining Tasks - wait to continue a task only once another is done
             */
            // ChainingTasks();

            /* -----------------------------------------------------------------------------------
             * Cancel linked child task when parent is canceled
             */
            // CancelLinkedChildTask();

            /* -----------------------------------------------------------------------------------
             * Cancel the thread pool task
             */
            // CancelThreadPoolTask();

            /* -----------------------------------------------------------------------------------
             * With Return type
             */
            // TaskFactoryWithReturnType();

            /* -----------------------------------------------------------------------------------
             * Same as Task.Run but with more abilities
             */
            // TaskFactoryStartNew();

            /* -----------------------------------------------------------------------------------
             * Task Run is the simple version -> run is the shortcut of the above.
             */
            // TaskRun();
        }
        private static void ErrorHandleAggregateException()
        {
            var parent = Task.Factory.StartNew(() =>
            {
                // We'll throw 3 exception at once using 3 child tasks:
                int[] numbers = { 0 };
                var childFactory = new TaskFactory(TaskCreationOptions.AttachedToParent,
                    TaskContinuationOptions.None);
                childFactory.StartNew(() => 5 / numbers[0]); // Division by zero - exception = DivideByZeroException
                childFactory.StartNew(() => numbers[1]); // index out of range   - exception = IndexOutOfRangeException
                childFactory.StartNew(() => { throw null; }); // null reference  - exception = NullReferenceException
            });
            try
            {
                parent.Wait();
            }
            catch (AggregateException aex)
            {
                aex.Flatten().Handle(ex =>
                {
                    if (ex is DivideByZeroException)
                    {
                        Console.WriteLine("Divide By zero");
                        return true;
                    }
                    if (ex is IndexOutOfRangeException)
                    {
                        Console.WriteLine("Index out of range");
                        return true;
                    }
                    if (ex is NullReferenceException)
                    {
                        Console.WriteLine("Null reference");
                        return true;
                    }
                    return false;
                });
            }
            Console.Read();
        }
        private static void ErrorHandleOneException()
        {
            var t1 = Task.Run(() => Print4(true, CancellationToken.None));
            try
            {
                t1.Wait();
            }
            catch (AggregateException ex)
            {
                var flattenList = ex.Flatten().InnerExceptions;
                foreach (var curEx in flattenList)
                {
                    Console.WriteLine(curEx);
                }

                // Console.WriteLine(ex);

                // any exception can be an instance of an AggregateException
                // and you'll have to deal with the hierarchical nature of AggregateException
                // ReadOnlyCollection<Exception> exs = ex.InnerExceptions;
            }
            Console.Read();
        }
        // a good wrapper for APM methods 
        private static void TestFromAsync()
        {
            FileStream fs = new FileStream(FilePath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None, 8, true);
            string content = "A quick brown fox jumps over the lazy dog";
            byte[] buffer = Encoding.Unicode.GetBytes(content);

            var writeChunk = Task.Factory.FromAsync(fs.BeginWrite, fs.EndWrite, buffer, 0, buffer.Length, null);
            writeChunk.ContinueWith(t =>
            {
                fs.Position = 0;
                var data = new byte[buffer.Length];
                var readChunk = Task<int>.Factory.FromAsync(fs.BeginRead, fs.EndRead, data, 0, data.Length, 0);
                readChunk.ContinueWith(read =>
                {
                    string readResult = Encoding.Unicode.GetString(data, 0, read.Result);
                    Console.WriteLine(readResult);
                });
            });
            Console.Read();
        }
        // --- Newer version of EAP ----------
        private static void TestAsyncEap()
        {
            WebClient wc = new WebClient();
            Task<byte[]> task = wc.DownloadDataTaskAsync(new Uri("http://www.engineerspock.com"));
            task.ContinueWith(t => { Console.WriteLine(Encoding.UTF8.GetString(t.Result)); });
            Console.Read();
        }
        // Task for write to file
        private static void TestAsyncTaskWrite()
        {
            FileStream fs = new FileStream(FilePath, FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.None, 8, true);

            string content = "A quick brown fox jumps over the lazy dog";
            byte[] data = Encoding.Unicode.GetBytes(content);

            Task task = fs.WriteAsync(data, 0, data.Length);
            task.ContinueWith(t =>
            {
                fs.Close();
                TestAsyncTaskRead();
            });
        }
        // Task to read File
        private static void TestAsyncTaskRead()
        {
            FileStream fs = new FileStream(FilePath, FileMode.OpenOrCreate,
                FileAccess.Read, FileShare.None, 8, true);

            byte[] data = new byte[1024];

            Task<int> readTask = fs.ReadAsync(data, 0, data.Length);
            readTask.ContinueWith(t =>
            {
                fs.Close();
                string content = Encoding.Unicode.GetString(data, 0, t.Result);
                Console.WriteLine($"Read Completed. Content is :{content}");
            });
        }
        /* ---------------- older eap version ---------------------------
         * wee need to download data and process its data asynchronously
         */
        public class ApmEap
        {
            public static void TestEap()
            {
                WebClient wc = new WebClient();

                Task<byte[]> task = wc.DownloadDataTaskAsync(new Uri("http://www.engineerspock.com"));
                task.ContinueWith(t => { Console.WriteLine(Encoding.UTF8.GetString(t.Result)); });

                Console.ReadKey();
            }
        }
        // web client example for cancelation token registration instead of polling if token is
        // cancelled within the method
        class webClientWrapper
        {
            private WebClient wc = new WebClient();
            private async Task LongRunningOperation(CancellationToken t)
            {
                if (!t.IsCancellationRequested)
                {
                    // register a callback for the cancel event -> good for IO bound events
                    using (CancellationTokenRegistration ctr = t.Register(() => { wc.CancelAsync(); }))
                    {
                        wc.DownloadStringAsync(new Uri("http://www.engineerspock.ocm"));
                    }
                }
            }
        }
        private static void ContinueTaskAfterDelay()
        {
            var t1 = Task.Run(() => Print3(true, CancellationToken.None));
            Task t2 = null;
            Console.WriteLine("Started t1");

            Task.Delay(5000).ContinueWith(x =>
            {
                t2 = Task.Run(() => Print3(false, CancellationToken.None));
                Console.WriteLine("Starting t2");
            });
            Console.Read();
        }
        private static void ContinueTasks()
        {
            var cts = new CancellationTokenSource();

            var t1 = Task.Run<int>(() => Print3(true, cts.Token), cts.Token);
            var t2 = Task.Run<int>(() => Print3(false, cts.Token), cts.Token);


            Task.Factory.ContinueWhenAll(new[] { t1, t2 }, tasks =>
            {
                var t1Task = tasks[0];
                var t2Task = tasks[1];
                Console.WriteLine($"t1Task:{t1Task.Result}, t1Task:{t2Task.Result}");
            });
            // calling `Wait();` on a tasks blocks the calling thread. t2 will continue fine
            // t1.Wait();
            // Wait all;  tasks to finish, will block till all finish
            // Task.WaitAll(t1, t2);
            // wait Any 
            // int result = Task.WaitAny(t1, t2); will block till any finish
            // when any ; do work when any task finishes
            // var tr = Task.WhenAny(t1, t2);
            // tr.ContinueWith(x=>{Console.WriteLine($"The id of the task which completed first = {tr.Result.Id}");});
            Console.Read();
        }
        private static void ChainingTasks()
        {
            var parentCts = new CancellationTokenSource();
            var childCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);
            var t1 = Task.Run<int>(() => Print3(true, parentCts.Token), parentCts.Token);
            // only run t3 when t1 completes successfully
            Task t2 = t1.ContinueWith(prevTask =>
            {
                Console.WriteLine($"How many numbers were processed by prev. task={prevTask.Result}");
                var t3 = Task.Run<int>(() => Print3(true, childCts.Token), childCts.Token);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            // only run t2 block when t1 fails
            t1.ContinueWith(t =>
            {
                Console.WriteLine("Finally, we are here!");
            }, TaskContinuationOptions.OnlyOnFaulted);

            // here always continues above is continue only on successful first task completion
            t1.ContinueWith(prevTask =>
            {
                Console.WriteLine($"How many numbers were processed by prev. task={prevTask.Result}");
                var t2 = Task.Run<int>(() => Print3(true, childCts.Token), childCts.Token);
            });
            Console.WriteLine("Main thread is not blocked");
            Console.Read();
        }
        private static void CancelLinkedChildTask()
        {
            var parentCts = new CancellationTokenSource();
            var childCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);
            var t1 = Task.Run<int>(() => Print3(true, parentCts.Token), parentCts.Token);
            var t2 = Task.Run<int>(() => Print3(true, childCts.Token), childCts.Token);
            // Thread.Sleep(10);

            // parentCts.Cancel();
            // childCts.Cancel();
            parentCts.CancelAfter(10);
            try
            {
                Console.WriteLine($"The first task processed: {t1.Result}");
                Console.WriteLine($"The second task processed: {t2.Result}");
            }
            catch (AggregateException ex) { }
            Console.WriteLine($"T1: {t1.Status}");
            Console.WriteLine($"T2: {t2.Status}");
            Console.Read();
        }
        private static void CancelThreadPoolTask()
        {
            var cts = new CancellationTokenSource();
            var t1 = Task.Run<int>(() => Print3(true, cts.Token), cts.Token);
            var t2 = Task.Run<int>(() => Print3(true, cts.Token), cts.Token);
            Thread.Sleep(10);

            cts.Cancel();
            try
            {
                Console.WriteLine($"The first task processed: {t1.Result}");
                Console.WriteLine($"The second task processed: {t2.Result}");
            }
            catch (AggregateException ex) { }
            Console.WriteLine($"T1: {t1.Status}");
            Console.WriteLine($"T2: {t2.Status}");
            Console.Read();
        }
        private static void TaskFactoryWithReturnType()
        {
            Task<int> t1 = Task.Factory.StartNew(() => Print2(true),
                CancellationToken.None, // can't cancel by default
                // impossible to attach child task to t1 task, runs on its own dedicated thread
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default); // default task scheduler
            Task<int> t2 = Task.Factory.StartNew(() => Print2(false));
            // get results from instances
            Console.WriteLine($"the first task processed:{t1.Result}");
            Console.WriteLine($"the second task processed:{t2.Result}");
            Console.Read();
        }
        private static void TaskFactoryStartNew()
        {
            Task.Factory.StartNew(() => Print(),
                CancellationToken.None, // can't cancel by default
                TaskCreationOptions.DenyChildAttach, // cannot create child task
                TaskScheduler.Default); // default task scheduler
            Task.Factory.StartNew(() => Print());
            Console.Read();
        }
        private static void TaskRun()
        {
            Task.Run(() => Print());
            Task.Run(() => Print());
            Console.Read();
        }
        public static int Print4(bool isEven, CancellationToken token)
        {
            // -- apart of task continuation catch for failure "Chaining Tasks" && Error Handling
            throw new InvalidOperationException();
            // -- 
            int total = 0;
            if (isEven)
            {
                for (int i = 0; i < 100; i += 2)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Cancellation requested");
                        // break; // Do not do this, the task will not get the status of Cancelled but RanToCOmpletion
                    }
                    // do this instead
                    token.ThrowIfCancellationRequested();
                    total++;
                    Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
                }
            }
            else
            {
                for (int i = 1; i < 100; i += 2)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Cancellation requested");
                    }
                    // do this instead
                    token.ThrowIfCancellationRequested();
                    total++;
                    Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
                }
            }
            return total;
        }
        public static int Print3(bool isEven, CancellationToken token)
        {
            /*
             * sleep a token but throw if canceled?
             * if (token.WaitHandle.WaitOne(2000))
            {
                token.ThrowIfCancellationRequested();
            }*/

            int total = 0;
            if (isEven)
            {
                for (int i = 0; i < 100; i += 2)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Cancellation requested");
                        // break; // Do not do this, the task will not get the status of Cancelled but RanToCOmpletion
                    }
                    // do this instead
                    token.ThrowIfCancellationRequested();
                    total++;
                    Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
                }
            }
            else
            {
                for (int i = 1; i < 100; i += 2)
                {
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("Cancellation requested");
                    }
                    // do this instead
                    token.ThrowIfCancellationRequested();
                    total++;
                    Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
                }
            }
            return total;
        }
        public static int Print2(bool isEven)
        {
            Console.WriteLine($"Is thread pool thread:{Thread.CurrentThread.IsThreadPoolThread}"); // true
            int total = 0;
            if (isEven)
            {
                for (int i = 0; i < 100; i += 2)
                {
                    total++;
                    Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
                }
            }
            else
            {
                for (int i = 1; i < 100; i += 2)
                {
                    total++;
                    Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
                }
            }
            return total;
        }
        public static void Print()
        {
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine($"Current task id={Task.CurrentId}. Value={i}");
            }
        }
    }
}
