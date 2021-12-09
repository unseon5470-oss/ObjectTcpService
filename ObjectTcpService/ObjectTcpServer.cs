using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectTcpService
{
    public class ObjectTCPServer : IDisposable
    {
        private Thread listenThread = null;
        private Thread clientThread = null;
        private int port = 0;

        private ConcurrentQueue<TcpClient> clientSocketQueue = new ConcurrentQueue<TcpClient>();
        private TcpListener tcpListener = null;

        public delegate void ReceiveEvent(ref ObjectTcpReceiveEvent eventArgs);
        public event ReceiveEvent Received;

        public ObjectTCPServer(int port)
        {
            this.port = port;
            this.listenThread = new Thread(RunListenThread);
            this.clientThread = new Thread(RunClientThread);
            this.listenThread.Start();
            this.clientThread.Start();
            Received += ObjectTCPServer_Received;
        }

        private void ObjectTCPServer_Received(ref ObjectTcpReceiveEvent eventArgs)
        {
            //default;
        }

        public void Dispose()
        {
            listenThread.Abort();
            clientThread.Abort();
            listenThread = null;
            GC.SuppressFinalize(this);
        }

        private void RunListenThread()
        {

            while (true)
            {
                Thread.Sleep(1);
                try
                {
                    //TCP Listener 초기화
                    if (tcpListener == null)
                    {
                        tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
                        tcpListener.Start();
                        continue;
                    }

                    //TCP 연결 진행
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    clientSocketQueue.Enqueue(tcpClient);
                }
                catch (Exception ex)
                {
                    //리스트너 초기화후 알람발생
                    if (tcpListener != null)
                        tcpListener.Stop();
                    tcpListener = null;
                    Console.Write(ex.Message);
                }
            }
        }

        private void RunClientThread()
        {
            while (true)
            {
                Thread.Sleep(1);
                if (clientSocketQueue.Count == 0)
                    continue;

                //데이터 수신은 소켓별로 병렬로 진행한다.
                Task.Run(() =>
                {
                    TcpClient tmpTcpClient = null;
                    if (!clientSocketQueue.TryDequeue(out tmpTcpClient))
                        return;
                    try
                    {
                        List<byte> byteArr = new List<byte>();
                        bool isFrameIn = false;
                        while (true)
                        {
                            Thread.Sleep(1);
                            //5초안에 수신을 받지 못하는 경우 IOException이 발생하고 연결이 종료됨
                            NetworkStream netStream = tmpTcpClient.GetStream();
                            netStream.ReadTimeout = 5000;
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
                                            byteArr.Clear();
                                            isFrameIn = true;
                                        }
                                        //ETX Check
                                        else if (b == 0x0003)
                                        {
                                            //수신이벤트 결과를 정리함
                                            ObjectTcpReceiveEvent eventArgs = new ObjectTcpReceiveEvent();
                                            String base64 = Encoding.ASCII.GetString(byteArr.ToArray());
                                            byte[] base64Bytes = Convert.FromBase64String(base64);
                                            eventArgs.ReceiveMessage = Encoding.Unicode.GetString(base64Bytes);
                                            eventArgs.tcpClient = tmpTcpClient;
                                            eventArgs.ResponseMessage = "";

                                            Received(ref eventArgs);

                                            //리스폰 결과를 보냄
                                            Response(eventArgs.tcpClient, eventArgs.ResponseMessage);

                                            byteArr.Clear();
                                            isFrameIn = false;
                                        }
                                        else if (b == 0x0006)
                                        {

                                        }
                                        else if (isFrameIn)
                                        {
                                            byteArr.Add(b);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (tmpTcpClient == null)
                            return;
                        tmpTcpClient.Close();
                        tmpTcpClient.Dispose();
                        tmpTcpClient = null;
                        Console.WriteLine(ex.Message);
                    }
                });



            }
        }

        private void Response(TcpClient tcpClient, string responseMessage)
        {
            try
            {
                byte[] unicodeBytes = Encoding.Unicode.GetBytes(responseMessage);
                string base64String = Convert.ToBase64String(unicodeBytes);
                byte[] base64Bytes = Encoding.ASCII.GetBytes(base64String);
                tcpClient.GetStream().WriteByte(0x0002);
                tcpClient.GetStream().Write(base64Bytes, 0, base64Bytes.Length);
                tcpClient.GetStream().WriteByte(0x0003);
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

        }
    }
}
