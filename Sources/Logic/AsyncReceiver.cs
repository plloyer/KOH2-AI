using System;

namespace Logic;

public class AsyncReceiver
{
	private Connection connection;

	private MemStream rx_stream;

	private MemStream msg_rx_stream;

	public static int BufferSize = 1048576;

	public NetworkProfiler profiler;

	public PacketLog receivedBytesPacketLog = new PacketLog();

	public PacketLog receivedMessagesPacketLog = new PacketLog();

	private FileWriter logFileWriter;

	private bool logAppendNewLine;

	public AsyncReceiver(Connection connection, FileWriter logFileWriter = null, bool logAppendNewLine = true)
	{
		rx_stream = new MemStream(new byte[BufferSize], 0, 0);
		msg_rx_stream = new MemStream(new byte[BufferSize], 0, 0);
		this.connection = connection;
		if (Multiplayer.NetworkType == Multiplayer.NetworkTransportType.THQNO && connection != null && connection.type == Connection.ConnectionType.Server)
		{
			(this.connection as THQNOConnection).SetReceiveBuffer(rx_stream.Buffer);
		}
		this.logFileWriter = logFileWriter;
		this.logAppendNewLine = logAppendNewLine;
	}

	public void Receive()
	{
		connection?.BeginReceive(rx_stream);
	}

	public MemStream OnReceive(int len)
	{
		rx_stream.Length += len;
		profiler?.AddPacket(len);
		receivedBytesPacketLog.Add(rx_stream.Buffer, len);
		while (!rx_stream.AtEnd)
		{
			if (msg_rx_stream.Length <= 0)
			{
				int position = rx_stream.Position;
				int num = rx_stream.Read7BitUInt_Safe();
				if (num < 0)
				{
					rx_stream.Position = position;
					return rx_stream;
				}
				if (num <= 0 || num >= BufferSize)
				{
					string text = $"Received message with length {num}";
					Multiplayer.Error(text);
					connection?.multiplayer?.OnMsgHeaderErrorCallback(text);
					continue;
				}
				if (logFileWriter != null)
				{
					if (logAppendNewLine)
					{
						logFileWriter.WriteLine(num + " bytes");
					}
					else
					{
						logFileWriter.Write(num + " bytes");
					}
				}
				msg_rx_stream.Length = num;
				msg_rx_stream.Position = 0;
			}
			int remaining = msg_rx_stream.Remaining;
			int num2 = rx_stream.Remaining;
			if (num2 > remaining)
			{
				num2 = remaining;
			}
			if (num2 <= 0)
			{
				continue;
			}
			msg_rx_stream.CopyBytesFrom(ref rx_stream, num2);
			if (msg_rx_stream.AtEnd)
			{
				try
				{
					msg_rx_stream.Position = 0;
					profiler?.AddMessage(msg_rx_stream.Buffer, msg_rx_stream.Length);
					receivedMessagesPacketLog.Add(msg_rx_stream.Buffer, msg_rx_stream.Length);
					connection?.multiplayer.OnReceive(msg_rx_stream);
				}
				catch (Exception ex)
				{
					Multiplayer.Error("Error in AsyncReceiver onReceive: " + ex);
				}
				msg_rx_stream.Length = 0;
				msg_rx_stream.Position = 0;
			}
		}
		rx_stream.Length = 0;
		rx_stream.Position = 0;
		return rx_stream;
	}
}
