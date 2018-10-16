﻿using LabNation.Common;
using LabNation.DeviceInterface.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LabNation.DeviceInterface.Net
{
    static class Net
    {
        public const string SERVICE_TYPE = "_sss._tcp";
        public const string REPLY_DOMAIN = "local.";
        public const string TXT_DATA_PORT = "DATA_PORT";
        public const string VERSION = "0.0.0.1";
        public const int BUF_SIZE = 8 * 1024;
        public const int ACQUISITION_PACKET_SIZE = Constants.SZ_HDR + Constants.FETCH_SIZE_MAX;
        public const int DATA_SOCKET_BUFFER_SIZE = ACQUISITION_PACKET_SIZE * 2;
        public const int HDR_SZ = 5;
#if DEBUG
        public const int TIMEOUT_RX = 5000 * 1000;
        public const int TIMEOUT_TX = 5000 * 1000;
        public const int TIMEOUT_CONNECT = 2000 * 1000;
#else
        public const int TIMEOUT_RX = 5000;
        public const int TIMEOUT_TX = 5000;
        public const int TIMEOUT_CONNECT = 2000;
#endif
        //COMMANDS
        public enum Command
        {
            SERIAL = 0x0d,
            FLUSH = 0x0e,
            DISCONNECT = 0x0f,
            GET = 0x18,
            SET = 0x19,
            DATA = 0x1a,
            PIC_FW_VERSION = 0x1b,
            FLASH_FPGA = 0x24,
            DATA_PORT = 0x2a,
            ACQUISITION = 0x34,
            LEDE_LIST_APS = 0x40,
            LEDE_RESET = 0x41,
            LEDE_CONNECT_AP = 0x42,
            LEDE_REBOOT = 0x43,
            SERVER_VERSION = 0x50,
        }

        internal static byte[] msgHeader(this Command command, int len)
        {
            len += HDR_SZ;
            byte[] buf = new byte[len];
            buf[0] = (byte)(len);
            buf[1] = (byte)(len >> 8);
            buf[2] = (byte)(len >> 16);
            buf[3] = (byte)(len >> 24);
            buf[4] = (byte)command;

            return buf;
        }

        internal static byte[] msg(this Command command, byte[] data = null, int len = 0)
        {
            len = data == null ? 0 : len > 0 ? len : data.Length;
            byte[] buf = msgHeader(command, len);
            if (data != null && len > 0)
                Array.ConstrainedCopy(data, 0, buf, HDR_SZ, len);

            return buf;
        }

        internal class Message
        {
            public int length;
            public Command command;
            public byte[] data;

            public static Message FromBuffer(byte[] buffer, int offset, int validLength)
            {
                if (validLength < HDR_SZ)
                    return null;

                int length = (buffer[offset]) + (buffer[offset + 1] << 8) + (buffer[offset + 2] << 16) + (buffer[offset + 3] << 24);
                if (validLength < length)
                    return null;
                if (length == 0)
                    return null;

                Message m = new Message();
                m.length = length;
                m.command = (Command)buffer[offset + HDR_SZ - 1];

                if (length > HDR_SZ)
                {
                    int dataLen = length - HDR_SZ;
                    m.data = new byte[dataLen];
                    Buffer.BlockCopy(buffer, offset + HDR_SZ, m.data, 0, dataLen);
                }
                return m;
            }
        }

        internal static List<Message> ReceiveMessage(Socket socket, byte[] msgBuffer, ref int msgBufferLength)
        {
            int bytesReceived = 0;
            int bytesConsumed = 0;

            List<Message> msgList = new List<Message>();
            while (msgList.Count == 0)
            {
                try
                {

                    int triesLeft = Net.TIMEOUT_RX;
                    while (!socket.Poll(1000, SelectMode.SelectRead)) { }
                    bytesReceived = socket.Receive(msgBuffer, msgBufferLength, msgBuffer.Length - msgBufferLength, SocketFlags.None);
                }
                catch
                {
                    Logger.Info("Network connection closed unexpectedly => resetting");
                    return null; //in case of non-graceful disconnects (crash, network failure)
                }

#if DEBUGFILE
            DateTime now = DateTime.Now;
            debugFile.WriteLine(now.Second + "-" + now.Millisecond + " Bytes received:" + bytesReceived.ToString());
            debugFile.Flush();
#endif

                if (bytesReceived > msgBuffer.Length)
                    throw new Exception("TCP/IP socket buffer overflow!");

                if (bytesReceived == 0) //this would indicate a network error
                    return null;

                msgBufferLength += bytesReceived;

                // Parsing message and build list

                while (true)
                {
                    Message m = Message.FromBuffer(msgBuffer, bytesConsumed, msgBufferLength - bytesConsumed);
                    if (m == null)
                        break;

                    //Move remaining valid data to beginning
                    bytesConsumed += m.length;
                    msgList.Add(m);
                }
            }
            msgBufferLength -= bytesConsumed;
            Buffer.BlockCopy(msgBuffer, bytesConsumed, msgBuffer, 0, msgBufferLength);
            return msgList;
        }

        internal static byte[] ControllerHeader(Command cmd, ScopeController ctrl, int address, int length, byte[] data = null)
        {
            // 1 byte controller
            // 2 bytes address
            // 2 bytes length
            byte[] res;
            int len = 5;
            if (data != null)
                len += length;

            res = cmd.msgHeader(len);

            int offset = HDR_SZ;
            res[offset++] = (byte)ctrl;
            res[offset++] = (byte)(address);
            res[offset++] = (byte)(address >> 8);
            res[offset++] = (byte)(length);
            res[offset++] = (byte)(length >> 8);

            if (data != null)
                Buffer.BlockCopy(data, 0, res, offset, length);

            return res;
        }
        internal static void ParseControllerHeader(byte[] buffer, out ScopeController ctrl, out int address, out int length, out byte[] data)
        {
            ctrl = (ScopeController)buffer[0];
            address = buffer[1] + (buffer[2] << 8);
            length = buffer[3] + (buffer[4] << 8);
            int dataLength = buffer.Length - 5;
            if (dataLength > 0)
            {
                data = new byte[dataLength];
                Buffer.BlockCopy(buffer, 5, data, 0, dataLength);
            }
            else
                data = null;
        }
    }

}
