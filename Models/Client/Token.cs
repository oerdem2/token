

namespace AuthServer.Models.Client;

public class Token
{
    public string type { get; set; }
    public string duration { get; set; }
    public List<string> claims { get; set; }
}
