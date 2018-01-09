#if RELEASE
    //#define DEBUG_SOCKET
#endif

#define ASYNC_DISCONNECT
//#define ASYNC_RECEIVE

//#define FIRE_EVENT_BY_BEGININVOKE


using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using NLog;

namespace MetTerminal2
{
    [ToolboxItem(true)]
    public partial class ClientSocketControl : System.ComponentModel.Component
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public const int ReceiveBufferSize = 4096;

        public ClientSocketControl()
        {
            InitializeComponent();
        }

        public ClientSocketControl(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        #region property and event
        //=====================================================================================================
        private string _RemoteIpAddress = "127.0.0.1";
        private int _IpPortNumber = 4001;

        [Category("Data"), DefaultValue(""), Description("Remote IP Address")]
        public string RemoteIpAddress { set { _RemoteIpAddress = value; } get { return (_RemoteIpAddress); } }

        [Category("Data"), DefaultValue(4001), Description("Remote TCP/IP Port")]
        public int IpPortNumber { set { _IpPortNumber = value; } get { return (_IpPortNumber); } }

        [Category("Status Message Available"), DefaultValue(null), Description("Occurs when Status Changed")]
        public event EventHandler<SocketPortMessageEventArgs> OnEventMessage;

        [Category("Data Available"), DefaultValue(null), Description("Occurs when Data is Available")]
        public event EventHandler<SocketPortDataEventArgs> OnDataAvailable;

        //=====================================================================================================
        #endregion

        private Socket _SocketClient = null;
        public Socket SocketClient
        {
            get { return (_SocketClient); }
            set
            {
                _SocketClient = value;
                if ((_SocketClient != null) && _SocketClient.Connected)
                {
                    BeginSocketThread();
                    SendSocketDataAvailableNotification = true;
                }
            }
        }

        #region SocketStatusQueue Thread
        //=====================================================================================================
        private bool SendSocketDataAvailableNotification = false;
        private bool IsThreadRunning = false;
        private const int SocketBreakUpCount = 10;
        private int CountStateTest = 0;

        private void BeginSocketThread()
        {
            if (!IsThreadRunning)
            {
                Thread Exec = new Thread(new ThreadStart(ThreadCheckSocket));
                Exec.Start();
                Thread ExecChkConn = new Thread(new ThreadStart(ThreadCheckSocketConnection));
                ExecChkConn.Start();
            }
        }

        private void ThreadCheckSocketConnection()
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            CountStateTest = 0;
            Random RandomWait = new Random();
            while ((_SocketClient != null) && _SocketClient.Connected)
            {
				if (_SocketClient.Available == 0)
				{
					try
					{
						if (_SocketClient.Poll(5, SelectMode.SelectRead))
						{
							CountStateTest++;
						}
						else
						{
							CountStateTest = 0;
						}
					}
					catch (Exception ex)
					{
						CountStateTest = SocketBreakUpCount + 1;
                        logger.Error(ex.ToString() + "\n");
					}
				}
				else
				{
					CountStateTest = 0;
				}

                if (CountStateTest > SocketBreakUpCount)
                {
                    break;
                }
                Thread.Sleep(RandomWait.Next(50));
            }

            CountStateTest = SocketBreakUpCount + 1;
        }

        private void ThreadCheckSocket()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            CountStateTest = 0;
            Random RandomWait = new Random();
            IsThreadRunning = true;

            while ((_SocketClient != null) && _SocketClient.Connected)
            {
                if (SendSocketDataAvailableNotification && (_SocketClient.Available > 0))
                {

#if FIRE_EVENT_BY_BEGININVOKE
                    if (OnDataAvailable != null) { OnDataAvailable.BeginInvoke(this, new SocketPortDataEventArgs(new SocketPortData(SocketMessageType.SocketDataAvailable, this)), null, null); }
#else
                    if (OnDataAvailable != null) { OnDataAvailable(this, new SocketPortDataEventArgs(new SocketPortData(SocketMessageType.SocketDataAvailable, this))); }
#endif
                }
                Thread.Sleep(RandomWait.Next(10));

                if (CountStateTest > SocketBreakUpCount)
                {
                    break;
                }
            }

            if ((_SocketClient != null) && _SocketClient.Connected)
            {
                _SocketClient.Close();
            }
            _SocketClient = null;

#if FIRE_EVENT_BY_BEGININVOKE
            if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.Disconnected), null, null); }
#else
            if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.Disconnected)); }
#endif

            IsThreadRunning = false;
        }
        //=====================================================================================================
        #endregion

        #region Socket Client
        //=====================================================================================================

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (_SocketClient == null) { return (false); }
                    return (_SocketClient.Connected);
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                    return (false);
                }
#else
                catch (Exception ex)
                {
                    logger.Error(ex.ToString() + "\n");
                    return (false);
                }
#endif
            }
        }

        public void Connect(string Address, int Port)
        {
            this.RemoteIpAddress = Address;
            this.IpPortNumber = Port;
            Connect();
        }

        private ManualResetEvent ConnectDone = new ManualResetEvent(true);
        private ManualResetEvent DisconnectDone = new ManualResetEvent(true);

        public void Connect()
        {
            IPAddress IP = null;
            IPAddress.TryParse(RemoteIpAddress, out IP);
            SendSocketDataAvailableNotification = false;

            if (IP == null)
            {
                try
                {
                    IPAddress[] IPS = Dns.GetHostAddresses(RemoteIpAddress);
                    if ((IPS != null) && (IPS.Length > 0)) { IP = IPS[0]; }
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                }
#else
                catch (Exception ex)
                {
                    logger.Error(ex.ToString() + "\n");
                }
#endif
            }

            if (IP != null)
            {

                SocketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SocketClient.Blocking = false;
                try
                {

#if FIRE_EVENT_BY_BEGININVOKE
                    if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.Connecting), null, null); }
#else
                    if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.Connecting)); }
#endif
                    SocketClient.ReceiveBufferSize = ClientSocketControl.ReceiveBufferSize;

                    ConnectDone.Reset();
                    SocketClient.BeginConnect(new IPEndPoint(IP, IpPortNumber), new AsyncCallback(ConnectCallBack), SocketClient);
                    ConnectDone.WaitOne();

                    if (IsConnected)
                    {
                        BeginSocketThread();
                        SendSocketDataAvailableNotification = true;

#if FIRE_EVENT_BY_BEGININVOKE
                        if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.Connected), null, null); }
#else
                        if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.Connected)); }
#endif
                    }
                    else
                    {
                        SocketClient.Close();
                        SocketClient = null;

#if FIRE_EVENT_BY_BEGININVOKE
                        if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.ConnectFailed), null, null); }
#else
                        if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.ConnectFailed)); }
#endif
                    }
                }
                catch (Exception ex)
                {
                    ConnectDone.Set();
                    SocketClient.Close();
                    SocketClient = null;
                    logger.Error(ex.ToString() + "\n");

#if FIRE_EVENT_BY_BEGININVOKE
                    if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.ConnectFailed), null, null); }
#else
                    if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.ConnectFailed)); }
#endif
                }
            }
            else
            {

#if FIRE_EVENT_BY_BEGININVOKE
                if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.ConnectFailed), null, null); }
#else
                if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.ConnectFailed)); }
#endif
            }
        }

        private void ConnectCallBack(IAsyncResult AsyncResult)
        {
            try
            {
                Socket ClientSocket = (Socket)AsyncResult.AsyncState;
                ClientSocket.EndConnect(AsyncResult);
            }
#if DEBUG_SOCKET
            catch (Exception e)
            {
                AccessoryLibrary.ShowException(e);
            }
#else
            catch (Exception ex)
            {
                logger.Error(ex.ToString() + "\n");
            }
#endif
            finally
            {
                ConnectDone.Set();
            }

        }

#if ASYNC_DISCONNECT

        public void Disconnect()
        {
            // Socket.DisconnectAsync() is new in .NET 2.0 SP1
            // not available in original .NET 2.0

            SendSocketDataAvailableNotification = false;
            if (IsConnected)
            {
                try
                {
                    DisconnectDone.Reset();
                    SocketClient.BeginDisconnect(false, new AsyncCallback(DisconnectCallBack), SocketClient);
                    DisconnectDone.WaitOne();
                    SocketClient = null;
                    //if (OnEventMessage != null) { OnEventMessage(this, new EventSocketPortMessage(SocketPortMessageType.Disconnected)); }

#if FIRE_EVENT_BY_BEGININVOKE
                    if (OnEventMessage != null) { OnEventMessage.BeginInvoke(this, new SocketPortMessageEventArgs(SocketMessageType.Disconnected), null, null); }
#else
                    if (OnEventMessage != null) { OnEventMessage(this, new SocketPortMessageEventArgs(SocketMessageType.Disconnected)); }
#endif
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                    DisconnectDone.Set();
                }
#else
                catch (Exception ex)
                {
                    DisconnectDone.Set();
                    logger.Error(ex.ToString() + "\n");
                }
#endif
            }
        }

        private void DisconnectCallBack(IAsyncResult AsyncResult)
        {
            try
            {
                Socket ClientSocket = (Socket)AsyncResult.AsyncState;
                ClientSocket.EndDisconnect(AsyncResult);
            }
#if DEBUG_SOCKET
            catch (Exception e)
            {
                AccessoryLibrary.ShowException(e);
            }
#else
            catch (Exception ex)
            {
                logger.Error(ex.ToString() + "\n");
            }
#endif
            finally
            {
                DisconnectDone.Set();
            }
        }

#else

        public void Disconnect()
        {
            SendSocketDataAvailableNotification = false;
            if (IsConnected)
            {
                try
                {
                    SocketClient.Shutdown(SocketShutdown.Both);
                    SocketClient.Disconnect(false);
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                }
#else
                catch
                {
                }
#endif
            }
        }

#endif
        //=====================================================================================================
        #endregion

        #region Write
        //=====================================================================================================
        public void WriteNonBlock(char Data) { WriteNonBlock(new Byte[] { (Byte)Data }); }
        public void WriteNonBlock(Byte Data) { WriteNonBlock(new Byte[] { Data }); }
        public void WriteNonBlock(string Data) { WriteNonBlock(Encoding.ASCII.GetBytes(Data)); }
        public void WriteNonBlock(char[] Data) { WriteNonBlock(Encoding.ASCII.GetBytes(Data)); }
        public void WriteNonBlock(Byte[] Data) { WriteNonBlock(Data, 0, Data.Length); }
        public void WriteNonBlock(Byte[] Data, int Index, int Count)
        {
            if (IsConnected)
            {
                _SocketClient.Send(Data, Index, Count, SocketFlags.None);
            }
        }

        public void WriteAsync(char Data) { WriteAsync(new Byte[] { (Byte)Data }); }
        public void WriteAsync(Byte Data) { WriteAsync(new Byte[] { Data }); }
        public void WriteAsync(string Data) { WriteAsync(Encoding.ASCII.GetBytes(Data)); }
        public void WriteAsync(char[] Data) { WriteAsync(Encoding.ASCII.GetBytes(Data)); }
        public void WriteAsync(Byte[] Data) { WriteAsync(Data, 0, Data.Length); }
        
        public void WriteAsync(Byte[] Data, int Index, int Count)
        {
            if (IsConnected)
            {
                SocketError ErrorCode;
                _SocketClient.BeginSend(Data, Index, Count, SocketFlags.None, out ErrorCode, new AsyncCallback(SendCallBack), SocketClient);
            }
        }

        private void SendCallBack(IAsyncResult AsyncResult)
        {
            try
            {
                Socket SocketClient = (Socket)AsyncResult.AsyncState;
                SocketClient.EndSend(AsyncResult);
            }
#if DEBUG_SOCKET
            catch (Exception e)
            {
                AccessoryLibrary.ShowException(e);
            }
#else
            catch (Exception ex)
            {
                logger.Error(ex.ToString() + "\n");
            }
#endif
        }

        #endregion

        #region Read
        //=====================================================================================================

        public int Available
        {
            get
            {
                try
                {
                    if ((_SocketClient != null) && (_SocketClient.Connected))
                    {
                        return (_SocketClient.Available);
                    }
                    return (0);
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                    return (0);
                }
#else
                catch (Exception ex)
                {
                    logger.Error(ex.ToString() + "\n");
                    return (0);
                }
#endif
            }
        }

        private int ReadSize = 0;

        public int Read(Byte[] Data)
        {
            ReadSize = 0;

            SendSocketDataAvailableNotification = false;
            if (IsConnected && (_SocketClient.Available > 0))
            {
                try
                {
#if ASYNC_RECEIVE
                    ReceiveDone.Reset();
                    int ByteToRead = Math.Min(Data.Length, _SocketClient.Available);
                    _SocketClient.BeginReceive(Data, 0, ByteToRead, SocketFlags.None, new AsyncCallback(ReadCallBack), _SocketClient);
                    ReceiveDone.WaitOne();
#else
                    ReadSize = _SocketClient.Receive(Data);
#endif
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                    ReceiveDone.Set();
                    ReadSize = -1;
                }
#else
                catch (Exception ex)
                {
                    ReceiveDone.Set();
                    ReadSize = -1;
                    logger.Error(ex.ToString() + "\n");
                }
#endif
            }

            /*
             * in the calling procedure
             * 
             * After calling Read()
             * ResetDataAvailableNotification() must be called again
             * to enable data notification
             * 
             */

            return (ReadSize);
        }

        private ManualResetEvent ReceiveDone = new ManualResetEvent(true);
        private void ReadCallBack(IAsyncResult AsyncResult)
        {
            try
            {
                Socket SocketClient = (Socket)AsyncResult.AsyncState;
                ReadSize = SocketClient.EndReceive(AsyncResult);
            }
#if DEBUG_SOCKET
            catch (Exception e)
            {
                AccessoryLibrary.ShowException(e);
                ReadSize = -1;
            }
#else
            catch (Exception ex)
            {
                ReadSize = -1;
                logger.Error(ex.ToString() + "\n");
            }
#endif
            finally
            {
                ReceiveDone.Set();
            }
        }

        public void DiscardInBuffer()
        {
            SendSocketDataAvailableNotification = false;
            if (IsConnected && (_SocketClient.Available > 0))
            {
                try
                {
                    Byte[] Data = new Byte[_SocketClient.Available * 2];
#if ASYNC_RECEIVE
                    ReceiveDone.Reset();
                    _SocketClient.BeginReceive(Data, 0, Data.Length, SocketFlags.None, new AsyncCallback(ReadCallBack), _SocketClient);
                    ReceiveDone.WaitOne();
#else
                    ReadSize = _SocketClient.Receive(Data);
#endif
                }
#if DEBUG_SOCKET
                catch (Exception e)
                {
                    AccessoryLibrary.ShowException(e);
                    ReceiveDone.Set();
                }
#else
                catch(Exception ex)
                {
                    ReceiveDone.Set();
                    logger.Error(ex.ToString() + "\n");
                }
#endif
                finally
                {
                    ReadSize = 0;
                }
            }
            ResetDataAvailableNotification();
        }

        public void ResetDataAvailableNotification()
        {
            try
            {
                SendSocketDataAvailableNotification = (SocketClient != null) && (SocketClient.Connected);
            }
#if DEBUG_SOCKET
            catch (Exception e)
            {
                AccessoryLibrary.ShowException(e);
                SendSocketDataAvailableNotification = false;
            }
#else
            catch (Exception ex)
            {
                SendSocketDataAvailableNotification = false;
                logger.Error(ex.ToString() + "\n");
            }
#endif
        }
        //=====================================================================================================
        #endregion
    }
}
