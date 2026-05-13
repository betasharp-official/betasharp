namespace BetaSharp;

public class Session(string username, string sessionId)
{
    public string Username = username;
    public string SessionToken = sessionId;
}
