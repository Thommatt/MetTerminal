using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public enum ConnectType { DIRECT = 0, TCPIP, DISABLED };

    public enum SocketOperateType { None, SocketClient, SocketServer }

    public enum SocketMessageType
    {
        None, Connecting, Connected, ConnectFailed, Disconnected, RemoteHostEntry, SocketDataSent,
        SocketDataAvailable, ServerListening, ServerStop
    }

    public class ComPortInfo
    {
        public string ComPortName;
        public int BaudRate;

        public ComPortInfo()
        {
            ComPortName = "COM1";
            BaudRate = 9600;
        }

        public ComPortInfo(string ComPortName, int BaudRate)
        {
            this.ComPortName = ComPortName;
            this.BaudRate = BaudRate;
        }

        public ComPortInfo(ComPortInfo Source)
        {
            this.ComPortName = Source.ComPortName;
            this.BaudRate = Source.BaudRate;
        }
    }

    public class NetWorkInfo
    {
        public string IpAddress;
        public int IpPort;
        public string UserName;
        public string PassWord;
        public SocketOperateType OperateType;

        public NetWorkInfo()
        {
            this.IpAddress = "127.0.0.1";
            this.IpPort = 4001;
            this.UserName = string.Empty;
            this.PassWord = string.Empty;
            this.OperateType = SocketOperateType.SocketClient;
        }

        public NetWorkInfo(string IpAddress, int IpPort)
        {
            this.IpAddress = IpAddress;
            this.IpPort = IpPort;
            this.UserName = string.Empty;
            this.PassWord = string.Empty;
            this.OperateType = SocketOperateType.SocketClient;
        }

        public NetWorkInfo(string IpAddress, int IpPort, SocketOperateType OperateType)
        {
            this.IpAddress = IpAddress;
            this.IpPort = IpPort;
            this.UserName = string.Empty;
            this.PassWord = string.Empty;
            this.OperateType = OperateType;
        }

        public NetWorkInfo(string IpAddress, int IpPort, string UserName, string PassWord)
        {
            this.IpAddress = IpAddress;
            this.IpPort = IpPort;
            this.UserName = UserName;
            this.PassWord = PassWord;
            this.OperateType = SocketOperateType.SocketClient;
        }

        public NetWorkInfo(NetWorkInfo Source)
        {
            this.IpAddress = Source.IpAddress;
            this.IpPort = Source.IpPort;
            this.UserName = Source.UserName;
            this.PassWord = Source.PassWord;
            this.OperateType = Source.OperateType;
        }
    }

    public class TimeInfo
    {
        public int SendCharWait;
        public int WaitFirstByte;
        public int WaitNextByte;
        public bool UseTimeSetting;

        public TimeInfo()
        {
            this.SendCharWait = 0;
            this.WaitFirstByte = 100;
            this.WaitNextByte = 100;
            this.UseTimeSetting = false;
        }

        public TimeInfo(TimeInfo Source)
        {
            this.SendCharWait = Source.SendCharWait;
            this.WaitFirstByte = Source.WaitFirstByte;
            this.WaitNextByte = Source.WaitNextByte;
            this.UseTimeSetting = Source.UseTimeSetting;
        }
    }

    public class ConnectInfoType
    {
        public ConnectType ConnectType;

        public ComPortInfo ComPortInfo;
        public NetWorkInfo NetWorkInfo;
        public TimeInfo TimeInfo;

        public ConnectInfoType()
        {
            this.ConnectType = ConnectType.DISABLED;

            this.ComPortInfo = new ComPortInfo();
            this.NetWorkInfo = new NetWorkInfo();
            this.TimeInfo = new TimeInfo();
        }

        public ConnectInfoType(ConnectType ConnectType)
        {
            this.ConnectType = ConnectType;

            this.ComPortInfo = new ComPortInfo();
            this.NetWorkInfo = new NetWorkInfo();
            this.TimeInfo = new TimeInfo();
        }

        public ConnectInfoType(ConnectType ConnectType, string IpAddressOrComPortName, int IpPortOrComPortBaudRate)
        {
            this.ConnectType = ConnectType;

            switch (ConnectType)
            {
                case ConnectType.DIRECT:

                    this.ComPortInfo = new ComPortInfo(IpAddressOrComPortName, IpPortOrComPortBaudRate);
                    this.NetWorkInfo = new NetWorkInfo();
                    this.TimeInfo = new TimeInfo();

                    break;

                case ConnectType.TCPIP:

                    this.ComPortInfo = new ComPortInfo();
                    this.NetWorkInfo = new NetWorkInfo(IpAddressOrComPortName, IpPortOrComPortBaudRate);
                    this.TimeInfo = new TimeInfo();

                    break;

                default:

                    this.ComPortInfo = new ComPortInfo();
                    this.NetWorkInfo = new NetWorkInfo();
                    this.TimeInfo = new TimeInfo();

                    break;
            }
        }

        public ConnectInfoType(ConnectInfoType ConnectInfo)
        {
            this.ConnectType = ConnectInfo.ConnectType;

            this.ComPortInfo = new ComPortInfo(ConnectInfo.ComPortInfo);
            this.NetWorkInfo = new NetWorkInfo(ConnectInfo.NetWorkInfo);
            this.TimeInfo = new TimeInfo(ConnectInfo.TimeInfo);
        }
    }

    public class GenericAtomVariable<Type>
    {
        private Mutex MutexValue = new Mutex();
        private Type _Value;
        public Type Value
        {
            get
            {
                Type Result = default(Type);
                if (MutexValue.WaitOne())
                {
                    try
                    {
                        Result = _Value;
                    }
                    finally
                    {
                        MutexValue.ReleaseMutex();
                    }
                }
                return (Result);
            }
            set
            {
                if (MutexValue.WaitOne())
                {
                    try
                    {
                        _Value = value;
                    }
                    finally
                    {
                        MutexValue.ReleaseMutex();
                    }
                }
            }
        }



        public GenericAtomVariable(Type Value)
        {
        }
    }

    public class GenericEventArgs<ArgsType> : EventArgs
    {
        private ArgsType _Argument;
        public ArgsType Argument { get { return (_Argument); } }

        public GenericEventArgs(ArgsType Argument)
        {
            _Argument = Argument;
        }
    }

    public class KeyBoardEventArgs : GenericEventArgs<Char>
    {
        public KeyBoardEventArgs(Char Argument)
            : base(Argument)
        {
        }
    }

    public class SocketPortMessageEventArgs : GenericEventArgs<SocketMessageType>
    {
        public SocketPortMessageEventArgs(SocketMessageType Argument)
            : base(Argument)
        {
        }
    }

    public class StringEventArgs : GenericEventArgs<string>
    {
        public StringEventArgs(string Argument)
            : base(Argument)
        {
        }
    }

    public class SocketPortData
    {
        private SocketMessageType _DataType;
        public SocketMessageType DataType { get { return (_DataType); } }

        private ClientSocketControl _ClientSocketCtrl;
        public ClientSocketControl ClientSocketCtrl { get { return (_ClientSocketCtrl); } }

        public SocketPortData(SocketMessageType DataType, ClientSocketControl ClientSocketCtrl)
        {
            this._DataType = DataType;
            this._ClientSocketCtrl = ClientSocketCtrl;
        }
    }

    public class SocketPortDataEventArgs : GenericEventArgs<SocketPortData>
    {
        public SocketPortDataEventArgs(SocketPortData PortData)
            : base(PortData)
        { }
    }

    public delegate void CMD_7500_Response(String Response);  //istantiate our delegate with a response parameter
}
