using System.Text;

namespace Fall2025GameClient.GameNetworking.Runtime
{
    enum MessageType : byte
    {
        Text,
        ClientId,
        UDPConnect
    }

    /*
            [body length (2)][type (1)][payload (variable)]
                                          |_______ body length ______|
     */
    internal class NetworkMessage
    {
        public const int lengthFieldSize = 2;

        public ushort BodyLength { get; private set; } // Length of Type + Payload
        public byte Type { get; private set; }
        public byte[] Payload { get; private set; }

        protected NetworkMessage(byte type, byte[] payload)
        {
            Type = type;
            Payload = payload;
            BodyLength = (ushort)(1 + payload.Length);
        }

        public static NetworkMessage FromBytes(byte[] data)
        {
            if (data.Length < 3)
                throw new ArgumentException("Data too short to be a valid NetworkMessage.");

            ushort length = BitConverter.ToUInt16(data, 0);
            byte type = data[2];
            byte[] payload = new byte[data.Length - 3];
            Array.Copy(data, 3, payload, 0, payload.Length);

            if (length != 1 + payload.Length)
                throw new ArgumentException("Length field does not match actual payload size.");

            switch ((MessageType)type)
            {
                case MessageType.Text:
                    return new TextMessage(payload);
                case MessageType.ClientId:
                    return new ClientIdMessage(payload);
                case MessageType.UDPConnect:
                    return new UDPConnectMessage(payload);
            }

            return new NetworkMessage(type, payload);
        }

        public byte[] ToByteArray()
        {
            byte[] lengthBytes = BitConverter.GetBytes(BodyLength);
            byte[] messageBytes = new byte[2 + BodyLength];
            Array.Copy(lengthBytes, 0, messageBytes, 0, 2);
            messageBytes[2] = Type;
            Array.Copy(Payload, 0, messageBytes, 3, Payload.Length);
            return messageBytes;
        }
    }

    class TextMessage : NetworkMessage
    {
        // payload [text (variable)]
        public TextMessage(string text) : base((byte)MessageType.Text, Encoding.UTF8.GetBytes(text)) { }

        public TextMessage(byte[] payloadBytes) : base((byte)MessageType.Text, payloadBytes) { }

        public string GetText()
        {
            return Encoding.UTF8.GetString(Payload);
        }
    }

    class ClientIdMessage : NetworkMessage
    {
        // payload [clientId (2)]
        public uint clientId { get => BitConverter.ToUInt32(Payload, 0); }

        public ClientIdMessage(uint _clientId)
            : base((byte)MessageType.ClientId, BitConverter.GetBytes(_clientId)) { }

        public ClientIdMessage(byte[] payloadBytes)
            : base((byte)MessageType.ClientId, payloadBytes) { }
    }

    class UDPConnectMessage : NetworkMessage
    {
        // payload [ack (1)][clientId (4)]
        public bool ack { get => Payload[0] == 1; }
        public uint clientId { get => BitConverter.ToUInt32(Payload, 1); }

        public UDPConnectMessage(bool _ack, uint _clientId)
            : base((byte)MessageType.UDPConnect, SetPayload(_ack, _clientId)) { }

        public UDPConnectMessage(byte[] payloadBytes)
            : base((byte)MessageType.UDPConnect, payloadBytes) { }

        private static byte[] SetPayload(bool _ack, uint _clientId)
        {
            byte[] _payload = new byte[5];
            _payload[0] = (byte)(_ack ? 1 : 0);
            BitConverter.GetBytes(_clientId).CopyTo(_payload, 1);
            return _payload;
        }
    }
}
