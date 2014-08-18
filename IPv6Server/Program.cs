using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPv6Server
{
    class Program
    {
        private static TcpListener _listener;

        static void Main(string[] args)
        {
            Console.Title = "IPv6 Server";

            Console.WriteLine("Local IP Addresses:");
            var addresses = Dns.GetHostAddresses("");
            //var entrys = Dns.GetHostEntry("");
            foreach (var ipAddress in addresses)
            {
                if(ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6Teredo)
                    continue;//ignore LinkLocal and Teredo addresses

                var s = string.Format("{0}{1}", ipAddress.AddressFamily.ToString().PadRight(25), ipAddress);
                Debug.WriteLine(s);
                Console.WriteLine(s);
                //Console.WriteLine("\tIsIPv4MappedToIPv6 {0}", ipAddress.IsIPv4MappedToIPv6);
                //Console.WriteLine("\tIsIPv6LinkLocal {0}", ipAddress.IsIPv6LinkLocal);
                //Console.WriteLine("\tIsIPv6Multicast {0}", ipAddress.IsIPv6Multicast);
                //Console.WriteLine("\tIsIPv6SiteLocal {0}", ipAddress.IsIPv6SiteLocal);
                //Console.WriteLine("\tIsIPv6Teredo {0}", ipAddress.IsIPv6Teredo);




            }


            _listener = TcpListener.Create(9090);
            _listener.Start();
            
            var socket = _listener.Server;
            var family = socket.AddressFamily;
            var endpoint = _listener.LocalEndpoint;
            
            Console.WriteLine("");
            Console.WriteLine("Address family: {0}", family);
            Console.WriteLine("Listening on {0}", endpoint);
            Console.WriteLine("");

            while (true)
            {
                //block tcplistener to accept incoming connection                
                _socket = _listener.AcceptSocket();
                //start a thread to receive data
                var t = new Thread(Receive);
                t.Start();
            }
            //var lastKey = ' ';
            //while (lastKey != 'x')
            //{
            //    var t = new Thread(Accept);    
            //    t.Start(_listener);
            //    lastKey = Console.ReadKey().KeyChar;
            //}
            //Console.WriteLine("Press 'x' to exit.");
            //Console.ReadLine();
        }

        //private static void Accept(object obj)
        //{
        //    var listener = (TcpListener)obj;
        //    //block tcplistener to accept incoming connection                
        //    _socket = listener.AcceptSocket();
        //    Receive(100);
        //}

        private static Socket _socket;
        private static ManualResetEvent _sendDone = new ManualResetEvent(false);
        //private static ManualResetEvent _receiveDone = new ManualResetEvent(false);

        private static void Receive()
        {
            // Create the state object.
            var state = new StateObject(100);
            state.WorkSocket = _socket;

            // Begin receiving the data from the remote device.
            _socket.BeginReceive(state.BufferRcvd, 0, 100, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), state);
        }
        private static void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            try
            {
                // Read data from the remote device.
                int bytesRead = state.WorkSocket.EndReceive(ar);
                if (bytesRead == 0) return;

                // Get the actual data received, and put it into a dynamically sized buffer
                var buffer = new byte[bytesRead];
                Buffer.BlockCopy(state.BufferRcvd, 0, buffer, 0, bytesRead);
                
                // Echo back
                var rcvd = Encoding.ASCII.GetString(buffer);
                Console.WriteLine("Data Received: " + rcvd);                
                Send(rcvd, bytesRead);

                //var t = new Thread(Accept);
                //t.Start(_listener);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine);
            }
        }

        private static void Send(string data, int bufferSize)
        {
            // Create the state object.
            var state = new StateObject(bufferSize);
            state.WorkSocket = _socket;
            state.TextSent = data;

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            _socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), state);
        }
        private static void SendCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket from the asynchronous state object.
            var state = (StateObject)ar.AsyncState;
            try
            {
                // Complete sending the data to the remote device.
                state.WorkSocket.EndSend(ar);
                // Signal that all bytes have been sent.
                _sendDone.Set();
                Console.WriteLine("Echo Sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine);
            }

        }

        
    }


    class StateObject
    {
        public StateObject(int bufferSize)
        {
            BufferSize = bufferSize;
            BufferRcvd = new byte[bufferSize];
        }

        internal Socket WorkSocket = null;
        // Size of receive buffer.
        internal int BufferSize { get; private set; }
        // Receive buffer.        
        internal byte[] BufferRcvd { get; set; }
        // Received data string.
        internal StringBuilder TextRcvd;
        // Sent data string
        internal string TextSent;

    }
}
