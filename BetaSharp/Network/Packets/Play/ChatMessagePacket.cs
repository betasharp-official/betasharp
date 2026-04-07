using System.Net.Sockets;

namespace BetaSharp.Network.Packets.Play;

public class ChatMessagePacket() : Packet(PacketId.ChatMessage)
{
    public string chatMessage;

    public static ChatMessagePacket Get(string msg)
    {
        var p = Get<ChatMessagePacket>(PacketId.ChatMessage);
        if (msg.Length > 119)
        {
            msg = msg.Substring(0, 119);
        }

        p.chatMessage = msg;
        return p;
    }

    public override void Read(Stream stream)
    {
        chatMessage = stream.ReadLongString(119);
    }

    public override void Write(Stream stream)
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
