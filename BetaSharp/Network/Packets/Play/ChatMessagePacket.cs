namespace BetaSharp.Network.Packets.Play;

public class ChatMessagePacket() : Packet(PacketId.ChatMessage)
{
    public string ChatMessage { get; private set; } = "";

    public static ChatMessagePacket Get(string msg)
    {
        ChatMessagePacket p = Get<ChatMessagePacket>(PacketId.ChatMessage);
        if (msg.Length > 119)
        {
            msg = msg.Substring(0, 119);
        }

        p.ChatMessage = msg;
        return p;
    }

    public override void Read(Stream stream) => ChatMessage = stream.ReadLongString(119);

    public override void Write(Stream stream) => stream.WriteLongString(ChatMessage);

    public override void Apply(NetHandler handler) => handler.onChatMessage(this);

    public override int Size() => ChatMessage.Length;
}
