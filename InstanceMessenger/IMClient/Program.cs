using System.Net;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using IMCommon;

namespace IMClient;

class Program
{
    static public bool s_runnerable = true;

    static async Task RunClientAsync(string id, string name)
    {
        LoggerHelper.SetConsole();

        var group = new MultithreadEventLoopGroup();

        var serverIp = "127.0.0.1";
        int serverPort = 10000;

        LoginUserInfo loginInfo = new(id, name, name);
        LoginRequest request = new()
        {
            Id = loginInfo.Id,
            Name = loginInfo.Name,
            Nick = loginInfo.Nickname,
            Passwd = "1111",
            ServerIp = serverIp,
            ServerPort = serverPort
        };

        try
        {
            var bootstrap = new Bootstrap();
            bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true) // Do not buffer and send packages right away
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .Handler(new MessageClientInitializer(request));

            var bootstrapChannel = await bootstrap.ConnectAsync(IPAddress.Parse(serverIp), serverPort);

            while (s_runnerable)
            {
                if (ClientConsoleInput.INSTANCE.IsInitialized)
                {
                    var result = await ClientConsoleInput.INSTANCE.RunAsync();
                    if (!result)
                    {
                        break;
                    }
                }
            }

            await bootstrapChannel.CloseAsync().ContinueWith(e =>
            {
                Console.WriteLine("Client Closed...");
            });
        }
        finally
        {
            group.ShutdownGracefullyAsync().Wait(1000);
        }
    }

    static void Main(string[] args) => RunClientAsync(args[0], args[1]).Wait();
}
