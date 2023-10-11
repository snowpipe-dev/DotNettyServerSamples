using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using IMCommon;

namespace IMServer;

class Program
{
    static private async Task RunServerAsync()
    {
        LoggerHelper.SetConsole();
        
        var serverPort = 10000;

        var bossGroup = new MultithreadEventLoopGroup(1); //  accepts an incoming connection
        var workerGroup = new MultithreadEventLoopGroup(2); // handles the traffic of the accepted connection once the boss accepts the connection and registers the accepted connection to the worker

        try
        {
            var bootstrap = new ServerBootstrap();

            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100) // maximum queue length for incoming connection
                .Handler(new LoggingHandler(LogLevel.INFO))
                .ChildHandler(new MessageServerInitializer());

            var bootstrapChannel = await bootstrap.BindAsync(serverPort);
            Console.ReadLine();
            await bootstrapChannel.CloseAsync();
        }
        finally
        {
            Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
        }
    }

    static void Main(string[] args) => RunServerAsync().Wait();

}
