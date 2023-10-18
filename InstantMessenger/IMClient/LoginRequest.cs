using System.Text;

namespace IMClient;

public class LoginRequest
{
    public string ServerIp { get; set; }
    public int ServerPort { get; set; }
    public string Id { get; set; }
    public string Passwd { get; set; }
    public string Name { get; set; }
    public string Nick { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ServerIp:{ServerIp}");
        sb.AppendLine($"ServerPort:{ServerPort}");
        sb.AppendLine($"Id:{Id}");
        sb.AppendLine($"Passwd:{Passwd}");
        sb.AppendLine($"Name:{Name}");
        sb.AppendLine($"Nick:{Nick}");

        return sb.ToString();
    }
}
