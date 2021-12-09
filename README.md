# ObjectTcpService

Object Tcp Server

1. How to Install

![Nuget Install](/img/nuget.png);



2. How to Use

<pre>
<code>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Create Server Socket
            ObjectTCPServer objectTCPServer = new ObjectTCPServer(5000);
            objectTCPServer.Received += ObjectTCPServer_Received;

            //Create Client Socket
            Parallel.For(0, 5, i =>
            {
                Task.Run(() =>
                {
                    ObjectTcpClient objectTcpClient = new ObjectTcpClient("127.0.0.1", 5000);
                    while (true)
                    {
                        Thread.Sleep(1000);
                        String response = objectTcpClient.Request(Guid.NewGuid().ToString());
                    }
                });
            });
        }

        private void ObjectTCPServer_Received(ref ObjectTcpReceiveEvent eventArgs)
        {
            String message = eventArgs.ReceiveMessage;
            String sender = eventArgs.tcpClient.Client.RemoteEndPoint.ToString();
            Dispatcher.Invoke(() =>
            {
                ui_listbox.Items.Add($@"[{sender}] {message}");
            });
            eventArgs.ResponseMessage = "Response";
        }

      
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
</pre>
</code>
