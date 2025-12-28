using System;
using System.Collections.Generic;
using System.Threading;

namespace Logic;

public class AsyncSender
{
	public class Buffer
	{
		public int id;

		public byte[] data;

		public int len;

		public Buffer(BufferPool bufferPool)
		{
			len = 0;
			data = bufferPool.GetBuffer();
		}
	}

	public class BufferPool
	{
		public int bufferSize = 256;

		private List<byte[]> pool;

		private int defaultPoolSize;

		private object BufferPoolLock = new object();

		public BufferPool(int bufferSizeParam = 0, int poolSize = 2)
		{
			pool = new List<byte[]>();
			if (bufferSizeParam != 0)
			{
				bufferSize = bufferSizeParam;
			}
			for (int i = 0; i < poolSize; i++)
			{
				pool.Add(new byte[bufferSize]);
			}
			defaultPoolSize = poolSize;
		}

		public byte[] GetBuffer()
		{
			lock (BufferPoolLock)
			{
				if (pool.Count > 0)
				{
					byte[] array = pool[pool.Count - 1];
					pool.RemoveAt(pool.Count - 1);
					if (array == null)
					{
						return new byte[bufferSize];
					}
					return array;
				}
			}
			return new byte[bufferSize];
		}

		public void ReturnBuffer(byte[] buffer)
		{
			if (buffer == null)
			{
				Multiplayer.Error("Trying to return a null buffer to the message buffer pool");
				return;
			}
			lock (BufferPoolLock)
			{
				Array.Clear(buffer, 0, buffer.Length);
				pool.Add(buffer);
			}
		}

		public void CleanPool()
		{
			lock (BufferPoolLock)
			{
				if (pool.Count > defaultPoolSize)
				{
					pool.RemoveRange(defaultPoolSize, pool.Count - defaultPoolSize);
				}
			}
		}
	}

	private Connection connection;

	public static int BufferSize = 1024;

	private List<Buffer> bufferQueue;

	private Buffer currentBuffer;

	private BufferPool bufferPool;

	private object Lock = new object();

	private AutoResetEvent resetEvent;

	public NetworkProfiler profiler;

	private bool isSingleplayerNetworkProfiler;

	public PacketLog sentBytesPacketLog = new PacketLog();

	public PacketLog sentMessagesPacketLog = new PacketLog();

	private int data_len;

	private FileWriter logFileWriter;

	private bool logAppendNewLine;

	private bool sending;

	private int last_id;

	public AsyncSender(Connection connection, FileWriter logFileWriter = null, bool logAppendNewLine = true, bool isSingleplayerNetworkProfiler = false)
	{
		this.connection = connection;
		bufferPool = new BufferPool(BufferSize);
		bufferQueue = new List<Buffer>();
		EnqueueNewBuffer();
		resetEvent = new AutoResetEvent(initialState: false);
		this.logFileWriter = logFileWriter;
		this.logAppendNewLine = logAppendNewLine;
		this.isSingleplayerNetworkProfiler = isSingleplayerNetworkProfiler;
	}

	public void Send(byte[] data, int len)
	{
		if (logFileWriter != null)
		{
			if (logAppendNewLine)
			{
				logFileWriter.WriteLine(len + " bytes");
			}
			else
			{
				logFileWriter.Write(len + " bytes");
			}
		}
		lock (Lock)
		{
			if (bufferQueue.Count > 1)
			{
				Game.Log("Preexisting buffers to send", Game.LogType.Error);
			}
			WriteLen(len);
			Write(data, 0, len);
		}
		profiler?.AddMessage(data, len);
		sentMessagesPacketLog?.Add(data, len);
		SendMessages();
	}

	public void ClearSendingFlag()
	{
		sending = false;
	}

	private void SendMessages()
	{
		if (!isSingleplayerNetworkProfiler && (connection == null || !connection.IsConnected()))
		{
			return;
		}
		lock (Lock)
		{
			if (sending)
			{
				return;
			}
			switch (bufferQueue.Count)
			{
			case 0:
				return;
			case 1:
				currentBuffer = null;
				break;
			}
			try
			{
				data_len = bufferQueue[0].len;
				if (data_len != 0)
				{
					profiler?.AddPacket(data_len);
					sentBytesPacketLog.Add(bufferQueue[0].data, bufferQueue[0].len);
					sending = true;
					if (!isSingleplayerNetworkProfiler)
					{
						connection.Send(bufferQueue[0].data, bufferQueue[0].len);
					}
					else
					{
						OnSent(bufferQueue[0].len);
					}
				}
			}
			catch (Exception ex)
			{
				Multiplayer.Error(ex.ToString());
			}
		}
	}

	public void OnSent(int sent_len)
	{
		lock (Lock)
		{
			int num = data_len - sent_len;
			if (num > 0 && !isSingleplayerNetworkProfiler && connection.IsConnected())
			{
				Multiplayer.Error("Message not sent completely - " + num + " bytes were not sent");
			}
			if (bufferQueue.Count > 0)
			{
				bufferPool.ReturnBuffer(bufferQueue[0].data);
				bufferQueue.RemoveAt(0);
			}
			else
			{
				Multiplayer.Error("Send queue is empty");
			}
			sending = false;
			SendMessages();
		}
	}

	private void WriteLen(int len)
	{
		byte b;
		while (true)
		{
			if (currentBuffer == null || currentBuffer.len + 1 >= currentBuffer.data.Length)
			{
				EnqueueNewBuffer();
			}
			b = (byte)(len & 0x7F);
			len >>= 7;
			if (len == 0)
			{
				break;
			}
			b |= 0x80;
			currentBuffer.data[currentBuffer.len++] = b;
		}
		currentBuffer.data[currentBuffer.len++] = b;
	}

	private void Write(byte[] data, int offset, int count)
	{
		if (currentBuffer == null)
		{
			EnqueueNewBuffer();
		}
		int len = currentBuffer.len;
		int num = currentBuffer.data.Length - len;
		int num2 = count;
		bool flag = false;
		if (count > num)
		{
			num2 = num;
			count -= num2;
			flag = true;
		}
		try
		{
			Array.Copy(data, offset, currentBuffer.data, len, num2);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
		}
		currentBuffer.len += num2;
		if (flag)
		{
			offset += num2;
			currentBuffer = null;
			Write(data, offset, count);
		}
	}

	public void CleanPool()
	{
		bufferPool.CleanPool();
	}

	private void EnqueueNewBuffer()
	{
		lock (Lock)
		{
			currentBuffer = new Buffer(bufferPool);
			currentBuffer.id = ++last_id;
			bufferQueue.Add(currentBuffer);
		}
	}

	private bool IsBufferEmpty(Buffer buffer)
	{
		return buffer.len == 0;
	}
}
