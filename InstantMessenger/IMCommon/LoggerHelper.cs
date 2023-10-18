using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;

namespace IMCommon;

public class LoggerHelper
{
    public static void SetConsole()
    {
        InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
    }

    public static IInternalLogger GetLogger<T>()
    {
        return InternalLoggerFactory.GetInstance<T>();
    }
}
