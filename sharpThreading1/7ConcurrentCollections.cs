using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using sharpThreading1.Immutable;

namespace sharpThreading1
{
    // Concurrent Dictionary items ************************************************************************
    public class RemoteBookStock
    {
        public static readonly List<string> Books = new List<string> { "Clean Code", "C# in Depth",
                "C++ for Beginners", "Design Patters in C#", "Marvel Heroes" };
    }
    public class StockController
    {
        readonly ConcurrentDictionary<string, int> _stock = new ConcurrentDictionary<string, int>();

        public void BuyBook(string item, int quantity)
        {
            _stock.AddOrUpdate(item, quantity, (key, oldValue) => oldValue + quantity);
        }
        public bool TryRemoveBookFromStock(string item)
        {
            if (_stock.TryRemove(item, out int val))
            {
                Console.WriteLine($"How much was removed: {val}");
                return true;
            }
            return false;
        }
        public bool TrySellBook(string item)
        {
            bool success = false;
            _stock.AddOrUpdate(item,
                itemName => { success = false; return 0; }, // executed when the key doesnt exist
                (itemName, oldValue) =>
                {
                    if (oldValue == 0)
                    {
                        success = false;
                        return 0; // keys new value
                    }
                    else
                    {
                        success = true;
                        return oldValue - 1; // keys new value
                    }
                });
            return success;
        }
        public void DisplayStatus()
        {
            foreach(var pair in _stock)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }
        }
    }
    public class SalesManager
    {
        public string Name { get; }
        public SalesManager(string id)
        {
            Name = id;
        }
        public void StartWork(StockController stockController, TimeSpan workDay)
        {
            Random rand = new Random((int)DateTime.UtcNow.Ticks);
            DateTime start = DateTime.UtcNow;
            while(DateTime.UtcNow - start < workDay)
            {
                Thread.Sleep(rand.Next(50));
                int generatedNumber = rand.Next(10);
                bool shouldPurchase = generatedNumber % 2 == 0;
                bool shouldRemove = generatedNumber == 9;
                string itemName = RemoteBookStock.Books[rand.Next(RemoteBookStock.Books.Count)];

                if(shouldPurchase)
                {
                    int quantity = rand.Next(9) + 1;
                    stockController.BuyBook(itemName, quantity);
                    DisplayPurchase(itemName, quantity);
                }
                else if (shouldRemove)
                {
                    stockController.TryRemoveBookFromStock(itemName);
                    DisplayRemoveAttempt(itemName);
                }
                else
                {
                    bool success = stockController.TrySellBook(itemName);
                    DisplaySaleAttempt(success, itemName);
                }
            }
            Console.WriteLine("SalesManager {0} finished its work!", Name);
        }
        private void DisplayRemoveAttempt(string itemName)
        {
            Console.WriteLine("Thread {0} {1} removed {2}", Thread.CurrentThread.ManagedThreadId, Name, itemName);
        }
        private void DisplayPurchase(string itemName, int quantity)
        {
            Console.WriteLine("Thread {0}: {1} bought {2} of {3}", Thread.CurrentThread.ManagedThreadId, Name, quantity, itemName);
        }
        public void DisplaySaleAttempt(bool success, string itemName)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine(success
                ? $"Thread {threadId}: {Name} sold {itemName}"
                : $"Thread {threadId}: {Name}: Out of stock of {itemName}");
        }
    }
    // ****************************************************************************************************

    // Producer Concuser **********************************************************************************
    public class ProducerConsumerDemo
    {
        private readonly BlockingCollection<string> _cutleriesToWash =
            new BlockingCollection<string>(new ConcurrentStack<string>(), 10);
        private readonly List<string> _cutleries = new List<string>()
        {
            "Fork",
            "Spoon",
            "Plate",
            "Knife"
        };
        private readonly Random _random = new Random();
        // producer
        public void Eat(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                string nextCutlery = _cutleries[_random.Next(3)];
                _cutleriesToWash.Add(nextCutlery);
                Console.WriteLine($"+ {nextCutlery}");
                Thread.Sleep(500);
            }
        }
        // consumer
        public void Wash(CancellationToken ct)
        {
            foreach (var item in _cutleriesToWash.GetConsumingEnumerable())
            {
                ct.ThrowIfCancellationRequested();
                Console.WriteLine($"- {item}");
                Thread.Sleep(3000);
            }
        }
        // Higher level function to run producer and consumer in separate threads
        public void Run(CancellationToken ct)
        {
            Task t1 = Task.Run(() => Eat(ct), ct);
            Task t2 = Task.Run(() => Wash(ct), ct);
            try
            {
                Task.WaitAll(t1, t2);
            }
            catch (AggregateException ae)
            {

            }
        }
    }
    // ****************************************************************************************************
    class ConcurrentCollections
    {
        public ConcurrentCollections()
        {
            /*
             * Blocking Collection and Producer/Consumer pattern
             */
            ProducerConsumerDemo1();

            /*
             * ConcurrentDictionary
             */
            // ConcurrentDictionaryDemo();

            /*
             * ConcurrentBag
             */
            // ConcurrentBagDemo();

            /*
             * ConcurrentStack
             */
            // ConcurrentStackDemo();

            /*
             * ConcurrentQueue
             */
            // ConcurrentQueueDemo();

            /*
             * Building Immutable Collection
             */
            // BuildImmutableCollectionDemo();

            /*
             * Immutable Dictionary
             */
            // ImmutableDictionaryDemo();

            /*
             * Immutable Sets
             * 
             * hashsets = unsorted
             * sorted set = sorted
             */
            // SetsDemo();

            /*
             * Immutable List
             */
            // ListDemo();

            /*
             * Immutable Queue
             */
            // QueueDemo();

            /*
            * Immutable Stack
            */
            // ImmutableStack1();


            // example of immutable stack
            /*IStack<int> stack = Immutable.Stack<int>.Empty; // Empty is the singleton
            IStack<int> stack2 = stack.Push(10);
            IStack<int> stack3 = stack2.Push(20);
            foreach (var cur in stack3)
            {
                Console.WriteLine(cur);
            }*/
        }
        // Blocking Collection and Producer/Consumer pattern ----------------------------------------------
        
        static void ProducerConsumerDemo1()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            ProducerConsumerDemo pcd = new ProducerConsumerDemo();
            Task.Run(() => pcd.Run(cts.Token));
            Console.Read();
            cts.Cancel();
            Console.WriteLine("End of processing");
        }
        
        // ConcurrentDictionary ---------------------------------------------------------------------------
        static void ConcurrentDictionaryDemo()
        {
            var controller = new StockController();
            TimeSpan workDay = new TimeSpan(0, 0, 1);

            Task t1 = Task.Run(() => new SalesManager("Bob").StartWork(controller, workDay));
            Task t2 = Task.Run(() => new SalesManager("Alice").StartWork(controller, workDay));
            Task t3 = Task.Run(() => new SalesManager("Rob").StartWork(controller, workDay));

            Task.WaitAll(t1, t2, t3);
            controller.DisplayStatus();
        }
        
        // ConcurrentBag ----------------------------------------------------------------------------------
        static void ConcurrentBagDemo()
        {
            var names = new ConcurrentBag<string>();
            names.Add("Bob");
            names.Add("Alice");
            names.Add("Rob");

            Console.WriteLine($"After enqueuing, count = {names.Count}");

            string item1; //= names.Dequeue();
            bool success = names.TryTake(out item1);
            if (success)
            {
                Console.WriteLine("\nRemoving " + item1);
            }
            else
            {
                Console.WriteLine("queue was empty");
            }
            string item2;
            success = names.TryPeek(out item2);
            if (success)
            {
                Console.WriteLine("Peeking " + item2);
            }
            else
            {
                Console.WriteLine("queue was empty");
            }
            Console.WriteLine("Enumerating");
            PrintOutCollection(names);
            Console.WriteLine("\nAfter enumerating, count = " + names.Count);
        }
        // ConcurrentStack --------------------------------------------------------------------------------
        static void ConcurrentStackDemo()
        {
            var names = new ConcurrentStack<string>();
            names.Push("Bob");
            names.Push("Alice");
            names.Push("Rob");

            Console.WriteLine($"After enqueuing, count = {names.Count}");

            string item1; //= names.Dequeue();
            bool success = names.TryPop(out item1);
            if (success)
            {
                Console.WriteLine("\nRemoving " + item1);
            }
            else
            {
                Console.WriteLine("queue was empty");
            }
            string item2;
            success = names.TryPeek(out item2);
            if (success)
            {
                Console.WriteLine("Peeking " + item2);
            }
            else
            {
                Console.WriteLine("queue was empty");
            }
            Console.WriteLine("Enumerating");
            PrintOutCollection(names);
            Console.WriteLine("\nAfter enumerating, count = " + names.Count);
        }
        // ConcurrentQueue --------------------------------------------------------------------------------
        static void ConcurrentQueueDemo()
        {
            var names = new ConcurrentQueue<string>();
            names.Enqueue("Bob");
            names.Enqueue("Alice");
            names.Enqueue("Rob");

            Console.WriteLine($"After enqueuing, count = {names.Count}");

            string item1; //= names.Dequeue();
            bool success = names.TryDequeue(out item1);
            if (success)
            {
                Console.WriteLine("\nRemoving " + item1);
            }
            else
            {
                Console.WriteLine("queue was empty");
            }
            string item2;
            success = names.TryPeek(out item2);
            if (success)
            {
                Console.WriteLine("Peeking " + item2);
            }
            else
            {
                Console.WriteLine("queue was empty");
            }
            Console.WriteLine("Enumerating");
            PrintOutCollection(names);
            Console.WriteLine("\nAfter enumerating, count = " + names.Count);
        }
        // Build Immutable Collection ------------i--------------------------------------------------------
        private static readonly List<int> largeList = new List<int>(128);
        private static void GenerateList()
        {
            for(int i = 0; i < 100000; i++)
            {
                largeList.Add(i);
            }
        }
        static void BuildImmutableCollectionDemo()
        {
            /*var builder = ImmutableList.CreateBuilder<int>();
            foreach(var item in largeList)
            {
                builder.Add(item);
            }
            // or
            // builder.AddRange(largeList);
            ImmutableList<int> immutableList = builder.ToImmutable();*/
            // above is how to build a immutable list from mutalbe one. under the hood to say

            // or below one liner
            var list = largeList.ToImmutableList();
        }
        // Immutable Dictionary --------------------------------------------------------------------------
        static void ImmutableDictionaryDemo()
        {
            var dic = ImmutableDictionary<int, string>.Empty;
            dic = dic.Add(1, "John");
            dic = dic.Add(2, "Alex");
            dic = dic.Add(3, "April");

            // Display "1-John" so on in an unpredictable order.
            ItereateOverDictionary(dic);

            Console.WriteLine("Changing value of key 2 to Bob");
            dic = dic.SetItem(2, "Bob");

            ItereateOverDictionary(dic);

            var april = dic[3];
            Console.WriteLine($"Who is at key 3 = {april}");

            Console.WriteLine("Remove record where key = 2");
            dic = dic.Remove(2);

            ItereateOverDictionary(dic);
        }
        private static void ItereateOverDictionary(ImmutableDictionary<int, string> dic)
        {
            foreach (var item in dic)
            {
                Console.WriteLine(item.Key + "-" + item.Value);
            }
        }
        // Immutable Set ---------------------------------------------------------------------------------
        static void SetsDemo()
        {
            // hashsets
            Console.WriteLine("------ Hashsets ------");
            var hashSet = ImmutableHashSet<int>.Empty;
            hashSet = hashSet.Add(5);
            hashSet = hashSet.Add(10);

            // display order is unpredictable with hashsets
            PrintOutCollection(hashSet);

            Console.WriteLine("Remove 5");
            hashSet = hashSet.Remove(5);

            PrintOutCollection(hashSet);

            //  sorted set
            Console.WriteLine("------ sortedsets ------");
            var sortedSet = ImmutableSortedSet<int>.Empty;
            sortedSet = sortedSet.Add(10);
            sortedSet = sortedSet.Add(5);

            PrintOutCollection(sortedSet);

            var smallest = sortedSet[0];
            Console.WriteLine($"Smallest item:{smallest}");

            Console.WriteLine("Remove 5");
            sortedSet = sortedSet.Remove(5);

            PrintOutCollection(sortedSet);
        }
        // Immutable List --------------------------------------------------------------------------------
        static void ListDemo()
        {
            var list = ImmutableList<int>.Empty;
            list = list.Add(2);
            list = list.Add(3);
            list = list.Add(4);
            list = list.Add(5);

            PrintOutCollection(list);

            Console.WriteLine("Remove 4 and then remove at index = 2");
            list = list.Remove(4);
            list = list.RemoveAt(2);

            PrintOutCollection(list);

            Console.WriteLine("Insert 1 at 0 and 4 at 3");
            list = list.Insert(0, 1);
            list = list.Insert(3, 4);

            PrintOutCollection(list);
        }
        // Immutable Queue -------------------------------------------------------------------------------
        static void QueueDemo()
        {
            var queue = ImmutableQueue<int>.Empty;
            // queue
            queue = queue.Enqueue(1);
            queue = queue.Enqueue(2);

            PrintOutCollection(queue);

            int first = queue.Peek();
            Console.WriteLine($"Last item in queue:{first}");

            // dequeue
            queue = queue.Dequeue(out first);
            Console.WriteLine($"Last item in queue:{first}, last after dequeue: {queue.Peek()}");
        }
        // Immutable stack -------------------------------------------------------------------------------
        public static void ImmutableStack1()
        {
            StackDemo();
            Console.Read();
        }
        static void StackDemo()
        {
            var stack = ImmutableStack<int>.Empty;
            stack = stack.Push(1);
            stack = stack.Push(2);

            int last = stack.Peek();
            Console.WriteLine($"Last item: {last}");

            stack = stack.Pop(out last);

            Console.WriteLine($"Last item:{last}; last after Pop: {stack.Peek()}");
        }

        private static void PrintOutCollection<T>(IEnumerable<T> collection)
        {
            foreach(var item in collection)
            {
                Console.WriteLine(item);
            }
        }
    }
    
}
