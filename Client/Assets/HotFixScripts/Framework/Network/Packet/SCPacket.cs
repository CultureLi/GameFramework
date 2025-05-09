using Framework;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public class SCPacket : Packet
    {
        public IMessage msg;

        public static SCPacket Create(uint msgId, byte[] buffer, int bufferLen)
        {
            var packet = ReferencePool.Acquire<SCPacket>();
            packet.id = msgId;

            var type = MsgTypeIdUtility.GetMsgType(msgId);
            packet.msg = Activator.CreateInstance(type) as IMessage;

            using (var codeStream = new CodedInputStream(buffer, 0, bufferLen))
            {
                packet.msg.MergeFrom(codeStream);
            }
            return packet;
        }
    }
}
