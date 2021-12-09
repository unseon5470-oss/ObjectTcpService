using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectTcpService
{
    public class ObjectTcpClient : IDisposable
    {
        private string hostname;
        private int port;
        private TcpClient tcpClient = null;
        private Object lockObj = new Object();
        private Thread threadKeepAlive = null;

        public ObjectTcpClient(String hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
            threadKeepAlive = new Thread(RunThreadKeepAlive);
            threadKeepAlive.Start();
        }
        private void RunThreadKeepAlive()
        {
            while (true)
            {
                Thread.Sleep(1000);
                KeepAlivePacketSend();
            }
        }

        public String Request(string message)
        {
            return Send(message);
        }

        private String Send(String message)
        {
            byte[] sendBytes = Encoding.Unicode.GetBytes(message);
            string sendBase64 = Convert.ToBase64String(sendBytes);
            byte[] sendBuff = Encoding.ASCII.GetBytes(sendBase64);

            String result = String.Empty;
            lock (lockObj)
            {
                try
                {
                    if (tcpClient == null)
                    {
                        tcpClient = new TcpClient();
                        tcpClient.Connect(hostname, port);
                    }
                    tcpClient.GetStream().WriteByte(0x0002);
                    tcpClient.GetStream().Write(sendBuff, 0, sendBuff.Length);
                    tcpClient.GetStream().WriteByte(0x0003);
                    tcpClient.GetStream().Flush();
                    List<byte> receiveByteArr = new List<byte>();
                    bool isFrameIn = false;
                    while (true)
                    {
                        Thread.Sleep(1);
                        NetworkStream netStream = tcpClient.GetStream();
                        byte[] buff = new byte[1024];
                        if (netStream.CanRead)
                        {
                            int len = netStream.Read(buff, 0, buff.Length);
                            if (len > 0)
                            {
                                for (int i = 0; i < len; i++)
                                {
                                    byte b = buff[i];
                                    //STX Check
                                    if (b == 0x0002)
                                    {
                                        receiveByteArr.Clear();
                                        isFrameIn = true;
                                    }
                                    //ETX Check
                                    else if (b == 0x0003)
                                    {
                                        //수신이벤트 결과를 정리함
                                        String receiveBase64 = Encoding.ASCII.GetString(receiveByteArr.ToArray());
                                        byte[] receiveUnicodeBytes = Convert.FromBase64String(receiveBase64);
                                        result = Encoding.Unicode.GetString(receiveUnicodeBytes);
                                        receiveByteArr.Clear();
                                        isFrameIn = false;
                                        return result;
                                    }
                                    else if (b == 0x0006)
                                    {
                                        //keep alive signal
                                    }
                                    else if (isFrameIn)
                                    {
                                        receiveByteArr.Add(b);
                                    }
                                }
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                        tcpClient.Dispose();
                        tcpClient = null;
                    }
                    Console.WriteLine(ex.Message);
                }
                finally
                {

                }
                return result;
            }
        }

        private String KeepAlivePacketSend()
        {
            lock (lockObj)
            {
                try
                {
                    if (tcpClient == null)
                    {
                        tcpClient = new TcpClient();
                        tcpClient.Connect(hostname, port);
                    }
                    tcpClient.GetStream().WriteByte(0x006);
                    tcpClient.GetStream().Flush();
                }
                catch (Exception ex)
                {
                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                        tcpClient.Dispose();
                        tcpClient = null;
                    }
                    Console.WriteLine(ex.Message);
                }
                finally
                {

                }
                return String.Empty;
            }
        }

        public void Dispose()
        {
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient.Dispose();
                tcpClient = null;
            }
        }
    }
}
