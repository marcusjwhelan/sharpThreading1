using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace sharpThreading1
{
    class examples
    {
        /*--------Interlocked Singleton class---------------------------------------------------------------*/
        public static class Lazy<T> where T : class, new()
        {
            private static T _instance;
            public static T Instance
            {
                get
                {
                    // if current is null, we need to create new intance
                    if (_instance == null)
                    {
                        // attempt to create, it will only set if previous was null
                        Interlocked.CompareExchange(ref _instance, new T(), (T)null);
                    }
                    return _instance;
                }
            }
        }
        // --------------------------------------------------------------------------------------------------

        /*-------TaskCompletionSource Example---------------------------------------------------------------*/
        // a task with attached methods
        //public class TaskCompletionSource<TResult>
        //{
        //    public void SetResult(TResult result);
        //    public void SetException(Exception exception);
        //    public void SetCanceled();
        //    public bool TrySetResult(TResult result);
        //    public bool TrySetException(Exception exception);
        //    public bool TrySetCanceled();
        //    public bool TrySetCanceled(CancellationToken cancellationToken);
        //}
        // prints 42 after 5 sec
        private static void TaskCompletionSourceExample1()
        {
            var tcs = new TaskCompletionSource<int>();
            new Thread(() =>
            {
                Thread.Sleep(5000);
                tcs.SetResult(42);
            })
            { IsBackground = true }.Start();

            Task<int> task = tcs.Task; // our "sub" task
            Console.WriteLine(task.Result); // 42
        }
        // This is the same but with taskcompletionssource with a request to a non pooled thread
        private static void TaskCompletionSourceExample2()
        {
            Task<TResult> Run<TResult> (Func<TResult> function)
            {
                var tcs = new TaskCompletionSource<TResult>();
                new Thread(() =>
                {
                    try
                    {
                        tcs.SetResult(function());
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }).Start();
                return tcs.Task;
            }

            Task<int> task = Run(() =>
            {
                Thread.Sleep(5000);
                return 42;
            });
        }
        // --------------------------------------------------------------------------------------------------

        /*-------MultiAsync Tasks Example-------------------------------------------------------------------*/
        // Example XML parsers. one returned task that does many tasks.
        public Task ImportXmlFilesAsync(string dataDirectory, CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (FileInfo file in new DirectoryInfo(dataDirectory).GetFiles("*.xml"))
                {
                    // single one at a time process
                    XElement doc = XElement.Load(file.FullName);
                    // InternalProcessXml(doc);
                }
            }, ct);
        }
        // Same method above with finer tuned tasks per file, parallel tasks
        public Task ImportXmlFilesAsync2(string dataDirectory, CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            {
                foreach (FileInfo file in new DirectoryInfo(dataDirectory).GetFiles("*.xml"))
                {
                    string fileToProcess = file.FullName;
                    // task for each process
                    Task.Factory.StartNew(_ =>
                    {
                        ct.ThrowIfCancellationRequested();
                        XElement doc = XElement.Load(fileToProcess);
                        InternalProcessXml(doc, ct);
                    }, ct, TaskCreationOptions.AttachedToParent);
                }
            }, ct);
        }
        private void InternalProcessXml(XElement doc, CancellationToken t)
        {

        }
        // --------------------------------------------------------------------------------------------------
    }
}
