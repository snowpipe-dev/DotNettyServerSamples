using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;

namespace IMCommon;

public class LoggerHelper
{
    static public void SetConsole()
    {
        InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
    }

    static public IInternalLogger GetLogger<T>()
    {
        return InternalLoggerFactory.GetInstance<T>();
    }
}
