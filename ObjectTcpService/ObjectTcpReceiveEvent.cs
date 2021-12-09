using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ObjectTcpService
{
    public class ObjectTcpReceiveEvent
    {
        public TcpClient tcpClient { get; set; }
        public String ReceiveMessage { get; set; }
        public String ResponseMessage { get; set; }
    }
}
