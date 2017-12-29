﻿using System;
using System.Net;
using System.Net.Sockets;
// ReSharper disable UseNullPropagation

namespace EdiabasLib
{
    public class EdElmWifiInterface
    {
#if Android
        public class ConnectParameterType
        {
            public ConnectParameterType(Android.Net.ConnectivityManager connectivityManager)
            {
                ConnectivityManager = connectivityManager;
            }

            public Android.Net.ConnectivityManager ConnectivityManager { get; }
        }
#endif

        protected delegate void ExecuteNetworkDelegate();

        public const string PortId = "ELM327WIFI";
        public static string ElmIp = "192.168.0.10";
        public static int ElmPort = 35000;
        protected static TcpClient TcpElmClient;
        protected static NetworkStream TcpElmStream;
        protected static int ConnectTimeout = 5000;
        protected static string ConnectPort;
        protected static object ConnectParameter;
        private static EdElmInterface _edElmInterface;

        static EdElmWifiInterface()
        {
        }

        public static EdiabasNet Ediabas { get; set; }

        public static bool InterfaceConnect(string port, object parameter)
        {
            if (TcpElmClient != null)
            {
                return true;
            }
            try
            {
                ConnectPort = port;
                ConnectParameter = parameter;
                ExecuteNetworkCommand(() =>
                {
                    TcpElmClient = new TcpClientWithTimeout(IPAddress.Parse(ElmIp), ElmPort, ConnectTimeout).Connect();
                });
                TcpElmStream = TcpElmClient.GetStream();
                _edElmInterface = new EdElmInterface(Ediabas, TcpElmStream, TcpElmStream);
                if (!_edElmInterface.Elm327Init())
                {
                    InterfaceDisconnect();
                    return false;
                }
            }
            catch (Exception)
            {
                InterfaceDisconnect();
                return false;
            }
            return true;
        }

        public static bool InterfaceDisconnect()
        {
            bool result = true;
            if (_edElmInterface != null)
            {
                _edElmInterface.Dispose();
                _edElmInterface = null;
            }

            try
            {
                if (TcpElmStream != null)
                {
                    TcpElmStream.Close();
                    TcpElmStream = null;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            try
            {
                if (TcpElmClient != null)
                {
                    TcpElmClient.Close();
                    TcpElmClient = null;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public static EdInterfaceObd.InterfaceErrorResult InterfaceSetConfig(EdInterfaceObd.Protocol protocol, int baudRate, int dataBits, EdInterfaceObd.SerialParity parity, bool allowBitBang)
        {
            if (TcpElmStream == null)
            {
                return EdInterfaceObd.InterfaceErrorResult.ConfigError;
            }
            if ((protocol != EdInterfaceObd.Protocol.Uart) ||
                (baudRate != 115200) || (dataBits != 8) || (parity != EdInterfaceObd.SerialParity.None))
            {
                return EdInterfaceObd.InterfaceErrorResult.ConfigError;
            }
            return EdInterfaceObd.InterfaceErrorResult.NoError;
        }

        public static bool InterfaceSetDtr(bool dtr)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceSetRts(bool rts)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceGetDsr(out bool dsr)
        {
            dsr = true;
            if (TcpElmStream == null)
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceSetBreak(bool enable)
        {
            return false;
        }

        public static bool InterfaceSetInterByteTime(int time)
        {
            return true;
        }

        public static bool InterfaceSetCanIds(int canTxId, int canRxId, EdInterfaceObd.CanFlags canFlags)
        {
            return true;
        }

        public static bool InterfacePurgeInBuffer()
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            if (_edElmInterface == null)
            {
                return false;
            }
            return _edElmInterface.InterfacePurgeInBuffer();
        }

        public static bool InterfaceAdapterEcho()
        {
            return false;
        }

        public static bool InterfaceHasPreciseTimeout()
        {
            return false;
        }

        public static bool InterfaceHasAutoBaudRate()
        {
            return false;
        }

        public static bool InterfaceHasAutoKwp1281()
        {
            return false;
        }

        public static bool InterfaceHasIgnitionStatus()
        {
            return false;
        }

        public static bool InterfaceSendData(byte[] sendData, int length, bool setDtr, double dtrTimeCorr)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            if (_edElmInterface == null)
            {
                return false;
            }
            if (_edElmInterface.StreamFailure)
            {
                if (Ediabas != null)
                {
                    Ediabas.LogString(EdiabasNet.EdLogLevel.Ifh, "Reconnecting");
                }
                InterfaceDisconnect();
                if (!InterfaceConnect(ConnectPort, ConnectParameter))
                {
                    _edElmInterface.StreamFailure = true;
                    return false;
                }
            }
            if (!_edElmInterface.InterfaceSendData(sendData, length, setDtr, dtrTimeCorr))
            {
                return false;
            }
            return true;
        }

        public static bool InterfaceReceiveData(byte[] receiveData, int offset, int length, int timeout, int timeoutTelEnd, EdiabasNet ediabasLog)
        {
            if (TcpElmStream == null)
            {
                return false;
            }
            if (_edElmInterface == null)
            {
                return false;
            }
            return _edElmInterface.InterfaceReceiveData(receiveData, offset, length, timeout, timeoutTelEnd, ediabasLog);
        }

        public static bool InterfaceSendPulse(UInt64 dataBits, int length, int pulseWidth, bool setDtr, bool bothLines, int autoKeyByteDelay)
        {
            return false;
        }

        protected static void ExecuteNetworkCommand(ExecuteNetworkDelegate command)
        {
#if Android
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
            {
                command();
                return;
            }
            // ReSharper disable once UsePatternMatching
            ConnectParameterType connectParameter = ConnectParameter as ConnectParameterType;
            if (connectParameter == null)
            {
                throw new Exception("No connect parameter");
            }
            Android.Net.Network bindNetwork = null;
            if (connectParameter.ConnectivityManager != null)
            {
                Android.Net.Network[] networks = connectParameter.ConnectivityManager.GetAllNetworks();
                if (networks != null)
                {
                    foreach (Android.Net.Network network in networks)
                    {
                        Android.Net.NetworkInfo networkInfo = connectParameter.ConnectivityManager.GetNetworkInfo(network);
                        if (networkInfo != null && networkInfo.IsConnected && networkInfo.Type == Android.Net.ConnectivityType.Wifi)
                        {
                            bindNetwork = network;
                            break;
                        }
                    }
                }
            }
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {
#pragma warning disable 618
                Android.Net.Network defaultNetwork = Android.Net.ConnectivityManager.ProcessDefaultNetwork;
                try
                {
                    Android.Net.ConnectivityManager.SetProcessDefaultNetwork(bindNetwork);
                    command();
                }
                finally
                {
                    Android.Net.ConnectivityManager.SetProcessDefaultNetwork(defaultNetwork);
                }
#pragma warning restore 618
                return;
            }
            Android.Net.Network boundNetwork = connectParameter.ConnectivityManager?.BoundNetworkForProcess;
            try
            {
                connectParameter.ConnectivityManager?.BindProcessToNetwork(bindNetwork);
                command();
            }
            finally
            {
                connectParameter.ConnectivityManager?.BindProcessToNetwork(boundNetwork);
            }
#else
            command();
#endif
        }
    }
}
