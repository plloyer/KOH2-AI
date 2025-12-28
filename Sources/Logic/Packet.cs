using System;
using System.Collections.Generic;
using System.Text;

namespace Logic;

public class PacketLog
{
	private class Packet
	{
		public byte[] bytes;

		public int len;

		public string toStringCache;

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(toStringCache))
			{
				return toStringCache;
			}
			StringBuilder stringBuilder = new StringBuilder($"{len}: ");
			stringBuilder.Append(string.Join(", ", bytes));
			toStringCache = stringBuilder.ToString();
			return toStringCache;
		}
	}

	public static bool enableLog;

	private List<Packet> packets = new List<Packet>();

	public void Add(byte[] bytes, int len)
	{
		if (enableLog)
		{
			Packet packet = new Packet();
			packet.len = len;
			packet.bytes = new byte[packet.len];
			Buffer.BlockCopy(bytes, 0, packet.bytes, 0, packet.len);
			packets.Add(packet);
		}
	}
}
