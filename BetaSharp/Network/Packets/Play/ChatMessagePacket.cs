using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class ChatMessagePacket() : Packet(PacketId.ChatMessage)
{
    public string chatMessage;

    public ChatMessagePacket(string msg) : this()
    {
        if (msg.Length > 119)
        {
            msg = msg.Substring(0, 119);
        }

        chatMessage = msg;
    }

    public override void Read(NetworkStream stream)
    {
        chatMessage = stream.ReadLongString(119);
    }

    public override void Write(NetworkStream stream)
    {
        stream.WriteLongString(chatMessage);
    }

    public override void Apply(NetHandler handler)
    {
        handler.onChatMessage(this);
    }

    public override int Size()
    {
        return chatMessage.Length;
    }
}
