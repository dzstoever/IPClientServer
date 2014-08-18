using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace IPClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var defaultServerIP = System.Configuration.ConfigurationManager.AppSettings["DefaultServerIP"];
            if(!string.IsNullOrEmpty(defaultServerIP))
                comboBox1.Items.Add(defaultServerIP);
            comboBox1.Items.Add("2001:4870:c0ca:101::130");
            //comboBox1.Items.Add("2001:4870:c0ca:101:c13:c378:37ef:5e92");
            //comboBox1.Items.Add("2001:4870:c0ca:101:ed7d:aa3c:254e:9aa8");            
            comboBox1.Items.Add("38.101.62.130");
            comboBox1.SelectedIndex = 0;

            comboBox2.SelectedIndex = 0;

            uxSend.Enabled = false;
            label2.Text = "";
        }

        private TcpClient _client;
        private Socket _socket;

        private void uxConnect_Click(object sender, EventArgs e)
        {
             uxConnect.Enabled = false;
            try
            {
                var ip = comboBox1.Text.Trim();
                var ipFamily = comboBox2.SelectedIndex == 0
                    ? AddressFamily.InterNetworkV6
                    : AddressFamily.InterNetwork;

                if (_client != null && _client.Connected)
                {
                    _client.Close();
                    _client = null;
                }

                _client = new TcpClient(ipFamily);
                //_client.Client.DualMode = true;
                _client.Connect(ip, 9090);

                _socket = _client.Client;
                var family = _socket.AddressFamily;
                var endpoint = _socket.RemoteEndPoint;

                var s = string.Format("{0} ({1}) {2}", endpoint, family,
                    _client.Connected ? "Connected." : "Not connected.");
                richTextBox1.AppendText(s + Environment.NewLine);
                //label2.Text = s;                
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText(ex.Message + Environment.NewLine);
            }
            finally
            {
                uxConnect.Enabled = true;
                //label2.Text = _client != null && _client.Connected ? "Connected." : "Not connected.";
                
                if (_client.Connected)
                    uxSend.Enabled = true;
                
                
            }        
        }

        private void uxSend_Click(object sender, EventArgs e)
        {
            uxSend.Enabled = false;
            try
            {
                Send(textBox1.Text, 100); //send data wait for response
                Receive(100);
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText(ex.Message + Environment.NewLine);
            }
            finally
            {
                //uxSend.Enabled = true;
                //label2.Text = _client != null && _client.Connected ? "Connected." : "Not connected.";
            }
        }


        //private int _bufferSize = 8192;
        private ManualResetEvent _sendDone = new ManualResetEvent(false);
        //private ManualResetEvent _receiveDone = new ManualResetEvent(false);

        public void Send(string data, int bufferSize)
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
        private void SendCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket from the asynchronous state object.
            var state = (StateObject)ar.AsyncState;
            try
            {
                // Complete sending the data to the remote device.
                //int bytesSent = 
                state.WorkSocket.EndSend(ar);

                // Signal that all bytes have been sent.
                _sendDone.Set();

                // Bubble the event
                //OnDataSent(this, state.TextSent);
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText(ex.Message + Environment.NewLine);
            }

        }

        public void Receive(int bufferSize)
        {
            // Create the state object.
            var state = new StateObject(bufferSize);
            state.WorkSocket = _socket;

            // Begin receiving the data from the remote device.
            _socket.BeginReceive(state.BufferRcvd, 0, bufferSize, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), state);
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket from the asynchronous state object.
            var state = (StateObject)ar.AsyncState;
            try
            {
                // Read data from the remote device.
                int bytesRead = state.WorkSocket.EndReceive(ar);
                if (bytesRead == 0)
                {
                    return;
                }

                
                    // Get the actual data received, and put it into a dynamically sized buffer
                    var buffer = new byte[bytesRead];
                    Buffer.BlockCopy(state.BufferRcvd, 0, buffer, 0, bytesRead);

                    // Clear the buffer
                    state.BufferRcvd = new byte[state.BufferSize];

                    var data = Encoding.ASCII.GetString(buffer);
                    Invoke(new StringDelegate(AppendMessage), new object[] { data });

                //var connected = _client.Connected;
                //label2.Text = _client != null && _client.Connected ? "Connected." : "Not connected.";

                // Wait for more data
                //Receive(_bufferSize);
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText(ex.Message + Environment.NewLine);
            }
        }

        private delegate void StringDelegate(string data);
        private void AppendMessage(string data)
        {
            richTextBox1.AppendText("Echo Received: " + data + Environment.NewLine);
        }

    }

    class StateObject
    {
        public StateObject(int bufferSize)
        {
            BufferSize = bufferSize;
            BufferRcvd = new byte[bufferSize];
        }

        // Client socket.
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
