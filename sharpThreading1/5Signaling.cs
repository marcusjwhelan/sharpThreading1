using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sharpThreading1
{
    // Protocol class - usually used in something like networking
    // **** Signaling example ***************************************************************
    public class BankTerminal
    {
        private readonly Protocol _protocol;
        private readonly ManualResetEventSlim _operationSignal = new ManualResetEventSlim(false);// wait will block the thread if false

        public BankTerminal(IPEndPoint endPoint)
        {
            _protocol = new Protocol(endPoint);
            _protocol.OnMessageReceived += OnMessageReceived;
        }
        private void OnMessageReceived(object sender, ProtocolMessage e)
        {
            if(e.Status == OperationStatus.Finished)
            {
                Console.WriteLine("Signaling!");
                _operationSignal.Set();
            }
        }
        public Task Purchase(decimal amount)
        {
            return Task.Run(() =>
            {
                const int purchaseOpCode = 1;
                _protocol.Send(purchaseOpCode, amount);

                Console.WriteLine("Waiting for signal.");

                _operationSignal.Wait(); // blocks thread
            });
        }
    }
    public class Protocol
    {
        private readonly IPEndPoint _endPoint;

        public Protocol(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
        }
        public void Send(int opCode, object parameters)
        {
            Task.Run(() =>
            {
                // emulating interoperation with a bank terminal device
                Console.WriteLine("Operation is in action.");
                Thread.Sleep(3000);

                OnMessageReceived?.Invoke(this, new ProtocolMessage(OperationStatus.Finished));
            });
        }
        public event EventHandler<ProtocolMessage> OnMessageReceived;
    }
    public enum OperationStatus
    {
        Finished,
        Faulted
    }
    public class ProtocolMessage
    {
        public OperationStatus Status { get; }
        public ProtocolMessage(OperationStatus status)
        {
            this.Status = status;
        }
    }
    //*****************************************************************************
    class Signaling
    {
        public Signaling()
        {


            // use barrier if you need to do work in synchronous order in the threads

            // forget it didn't understand his course lecture look at the source code
            // / signalling with autoResetEvent and Manually ResetEventSlim
            //
            //

            //
            var bt = new BankTerminal(new IPEndPoint(new IPAddress(0x2414188f), 8080));

            Task purchaseTask = bt.Purchase(100);
            purchaseTask.ContinueWith(x =>
            {
                Console.WriteLine("Operation is done!");
            });
            Console.Read();
        }
    }
}
