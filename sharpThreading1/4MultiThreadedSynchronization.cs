using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;




/*
 * Monitor class to acquire exclusive locks for accessing a shared resource
 */
/*
 * ReaderWriterLock for optimizing code which is responsible for managing concurrent access in a certain scenario
 */
/*
 * Semaphore synchronization construct to limit the number of threads which can have access to a particular
 * resource simultaneously
 */
/*
 * What is SynchronizationContext and what role it plays in UI-apps
 */
/*
 * What is a deadlock and why is it so scary
 */
namespace sharpThreading1
{
    // reader writer lock slim extension ************************************************************
    public static class ReaderWriterLockSlimExt
    {
        public static ReaderLockSlimWrapper TakeReaderLock(this ReaderWriterLockSlim rwlock, TimeSpan timeout)
        {
            bool taken = false;
            try
            {
                taken = rwlock.TryEnterReadLock(timeout);
                if (taken)
                    return new ReaderLockSlimWrapper(rwlock);
                throw new TimeoutException("Failed to acquire a ReaderWriterLockSlim in time.");
            }
            catch
            {
                if (taken)
                    rwlock.ExitReadLock();
                throw;
            }
        }

        public struct ReaderLockSlimWrapper : IDisposable
        {
            private readonly ReaderWriterLockSlim _rwlock;

            public ReaderLockSlimWrapper(ReaderWriterLockSlim rwlock)
            {
                _rwlock = rwlock;
            }
            public void Dispose()
            {
                _rwlock.ExitReadLock();
            }
        }

        public static WriterLockSlimWrapper TakeWriterLock(this ReaderWriterLockSlim rwlock, TimeSpan timeout)
        {
            bool taken = false;
            try
            {
                taken = rwlock.TryEnterWriteLock(timeout);
                if (taken)
                    return new WriterLockSlimWrapper(rwlock);
                throw new TimeoutException("Failed to acquire a ReaderWriterLockSlim in time.");
            }
            catch
            {
                if (taken)
                    rwlock.ExitWriteLock();
                throw;
            }
        }
        public struct WriterLockSlimWrapper : IDisposable
        {
            private readonly ReaderWriterLockSlim _rwlock;

            public WriterLockSlimWrapper(ReaderWriterLockSlim rwlock)
            {
                _rwlock = rwlock;
            }
            public void Dispose()
            {
                _rwlock.ExitWriteLock();
            }
        }
    }
    // ********************************************************************************************

    // Lock struct used in extension **************************************************************
    public struct Lock : IDisposable
    {
        private readonly object _obj;
        public Lock(object obj)
        {
            _obj = obj;
        }
        public void Dispose()
        {
            Monitor.Enter(_obj);
        }
    }
    public static class LockExtensions
    {
        public static Lock Lock(this object obj, TimeSpan timeout)
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(obj, timeout, ref lockTaken);
                if (lockTaken)
                {
                    return new Lock(obj);
                }
                throw new TimeoutException("Failed to acquire sync object.");
            }
            catch
            {
                if (lockTaken)
                {
                    Monitor.Exit(obj);
                }
                throw;
            }
        }
    }
    //**********************************************************************************************
    public class BankCard
    {
        private decimal _moneyAmount;
        private readonly object _sync = new object();
        private decimal _credit;

        // using reader writer lock slim = useful when you application doesn't want to block readers
        // but still want to write. ALos good if most threads are reading
        private ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim(); // does not allow recursion

        public BankCard(decimal moneyAmount)
        {
            _moneyAmount = moneyAmount;
        }
        public decimal TotalMoneyAmount
        {
            // with read write lock slim extension
            get
            {
                using(_rwLock.TakeReaderLock(TimeSpan.FromSeconds(3)))
                {
                    return _moneyAmount + _credit;
                }
            }
            
            // without read write lock slim extension
            /*get
            {
                _rwLock.EnterReadLock();
                decimal result = _moneyAmount + _credit;
                _rwLock.ExitReadLock();
                return result;
            }*/


            // without reader writer lock slim
            /*get
            {
                lock (_sync)
                {
                    return _moneyAmount + _credit;
                }
            }*/
        }
        public void ReceivePayment(decimal amount)
        {
            // with slim lock extension
            using(_rwLock.TakeWriterLock(TimeSpan.FromSeconds(3)))
            {
                _moneyAmount += amount;
            }

            // with slim lock - without slim lock extension
            /*_rwLock.EnterWriteLock();
            _moneyAmount += amount;
            _rwLock.ExitWriteLock();*/

            // withouth writer lock
            /*lock (_sync)
            {
                _moneyAmount += amount;
            }*/

            // withouth lock extension
            /*bool lockTaken = false;
            try
            {
                // timeout for trying to lock
                Monitor.TryEnter(_sync, TimeSpan.FromSeconds(10), ref lockTaken);

                _moneyAmount += amount; // keep this work thread save between monitor
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_sync);
                }
            }*/
        }
        public void TransferToCard(decimal amount, BankCard recipient)
        {
            // same code as below but no timeout safety.
            /*lock(_sync)
            {
                _moneyAmount -= amount;
                recipient.ReceivePayment(amount);
            }*/
            // now you can use the lock struct much easier
            using (_sync.Lock(TimeSpan.FromSeconds(3)))
            {
                _moneyAmount -= amount;
                recipient.ReceivePayment(amount);
            }

            // below is without extension and struct
            /*bool lockTaken = false;
            try
            {
                Monitor.Enter(_sync);

                _moneyAmount -= amount;
                recipient.ReceivePayment(amount);
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_sync);
                }
            }*/
        }
    }

    class MultiThreadedSynchronization
    {
        // Semaphore example
        public static SemaphoreSlim Bouncer { get; set; }
        public MultiThreadedSynchronization()
        {
            /*
             * Semaphore - limit a resource to thread access
             */
            // Semaphore();

            /*
             * Monitor lock -- example above in bank card, lock extension and lock struct
             * 
             * examples above in bank and other extensions
             */

            /*
             * Interlocked reference swap
             */
            // AtomicSwap();

            /*
             * Run Character to similulate an issue with shared resources. Since many threads try
             * to heal and damage the same character.
             * 
             * Add Interlocked to Character class
             */
            // CharacterInterlocked();
        }
        /* ---------------------------------------------------------------------------
         * Semaphore - thread limiting
         */
        private static void Semaphore()
        {
            Bouncer = new SemaphoreSlim(3, 3); // limit here
            OpenNightClub();

            Thread.Sleep(20000);

            Console.Read();
        }
        private static void OpenNightClub()
        {
            for (int i = 1; i < 50; i ++)
            {
                // let each guest enter on own thread
                var number = i;
                Task.Run(() => Guest(number));
            }
        }
        private static void Guest(int guestNumber)
        {
            // wait to enter the nightclub (a semaphore to be released).
            Console.WriteLine("Guest {0} is waiting to enter nightclub.", guestNumber);
            Bouncer.Wait();

            // do some dancing
            Console.WriteLine("Guest {0} is doing some dancing", guestNumber);
            Thread.Sleep(500);

            // let one guest out (release one semaphore).
            Console.WriteLine("Guest {0} is leaving the nightclub.", guestNumber);
            Bouncer.Release(1);
        }

        /* ---------------------------------------------------------------------------
         * Interlocked reference swap
         */
        private static void Swap(object obj1, object obj2)
        {
            object obj1Ref = Interlocked.Exchange(ref obj1, obj2);
            Interlocked.Exchange(ref obj2, obj1Ref);

            // Non atomic swap
            //object tmp = obj1;
            //obj1 = obj2;
            //obj2 = tmp;
        }
        private static void AtomicSwap()
        {
            Character c = new Character();
            Character c2 = new Character();

            Swap(c, c2);
        }

        /* ---------------------------------------------------------------------------
        * Interlocked class to defend primitives from concurrent access
        * 
        * Add interlocked to character class and execution
        */
        public class Character
        {
            private int _armor;
            private int _health = 100;
            public int Health { 
                get => _health;
                private set => _health = value;
            }
            public int Armor {
                get => _armor;
                private set => _armor = value;
            }
            public void Hit(int damage)
            {
                // Health -= damage - Armor; not atomic
                int actualDamage = Interlocked.Add(ref damage, -Armor);
                Interlocked.Add(ref _health, -actualDamage);
            }
            public void Heal(int health)
            {
                // Health += health;
                Interlocked.Add(ref _health, health);
            }
            public void CastArmorSpell(bool isPositive)
            {
                if (isPositive)
                {
                    Interlocked.Increment(ref _armor);
                    // Armor++;// not atomic
                }
                else
                {
                    Interlocked.Decrement(ref _armor);
                    // Armor--;
                }
            }
        }
        public static void CharacterInterlocked()
        {
            Character c = new Character();
            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                Task t1 = Task.Factory.StartNew(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        c.CastArmorSpell(true);
                    }
                });
                tasks.Add(t1);

                Task t2 = Task.Factory.StartNew(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        c.CastArmorSpell(false);
                    }
                });
                tasks.Add(t2);
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"Resulting armor = {c.Armor}");
            Console.Read();
        }
    }
}
