using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Logic;

public class THQNOConnection : Connection
{
	private string targetId;

	public int ownChannel;

	public int targetChannel;

	private IntPtr dataPtr;

	private byte[] receiveBuffer;

	private bool listen;

	private object Lock = new object();

	private bool isConnected;

	public bool devCheatStop;

	public THQNOConnection(ConnectionType type, string targetId, int ownChannel = -1, int targetChannel = -1)
	{
		base.type = type;
		this.targetId = targetId;
		this.ownChannel = ownChannel;
		this.targetChannel = targetChannel;
		dataPtr = Marshal.AllocHGlobal(AsyncSender.BufferSize);
		isConnected = true;
		status = Status.NotConnected;
	}

	public void SetReceiveBuffer(byte[] buffer)
	{
		receiveBuffer = buffer;
	}

	public void SetOwnChannel(int ownChannel)
	{
		this.ownChannel = ownChannel;
	}

	public void SetTargetChannel(int targetChannel)
	{
		this.targetChannel = targetChannel;
	}

	public override void Connect()
	{
		isConnected = true;
		multiplayer?.OnNetworkConnect(ownChannel);
	}

	private static int ExtractMessageID(byte[] data, int length)
	{
		if (data == null)
		{
			return -2;
		}
		int num = 0;
		while (num < length)
		{
			byte num2 = data[num];
			num++;
			if (num2 < 128)
			{
				break;
			}
		}
		if (num >= length)
		{
			return -1;
		}
		return data[num];
	}

	private static string MsgIdToStr(int msg_id)
	{
		string arg = "UNKNOWN";
		try
		{
			Multiplayer.MessageId messageId = (Multiplayer.MessageId)msg_id;
			arg = messageId.ToString();
		}
		catch
		{
		}
		return $"{arg}({msg_id})";
	}

	public override bool Send(byte[] data, int length)
	{
		if (devCheatStop)
		{
			return false;
		}
		if (targetChannel < 0)
		{
			Multiplayer.Error($"{this}: sending data on channel {targetChannel}");
			return false;
		}
		int num = ExtractMessageID(data, length);
		if (targetChannel == 0 && num != 3)
		{
			Multiplayer.Error("Sending non-connect message " + MsgIdToStr(num) + " to " + GetTarget() + ":0");
		}
		else if (status == Status.Connecting && targetChannel != 0 && num != 5 && num != 4 && num != 6)
		{
			Multiplayer.Error($"Sending {MsgIdToStr(num)} to {GetTarget()}:{targetChannel} while connecting");
		}
		Marshal.Copy(data, 0, dataPtr, length);
		int level = ((multiplayer == null || multiplayer.connectionReason == Multiplayer.ConnectionReason.Meta) ? 3 : 4);
		if (Multiplayer.LogEnabled(level) && num != 6)
		{
			Multiplayer.Log($"Sending message {MsgIdToStr(num)} with length {length} to {GetTarget()}:{targetChannel}, from {this}", level);
		}
		lock (THQNORequest.LockP2P)
		{
			using (Game.Profile("Send in LockP2P", log: true, 100f))
			{
				THQNORequest.SendP2PPacket(GetTarget(), dataPtr, (uint)length, Common.P2PSendType.ReliableOrdered, (uint)targetChannel);
			}
		}
		if (Multiplayer.heartbeat_timer != null)
		{
			last_send_time = Multiplayer.heartbeat_timer.ElapsedMilliseconds;
		}
		if (multiplayer != null && multiplayer.asyncSender != null)
		{
			multiplayer.asyncSender.OnSent(length);
		}
		return true;
	}

	public override void BeginReceive(MemStream memStream)
	{
		if (ownChannel < 0)
		{
			Multiplayer.Error($"{this} is attempting to BeginReceive() on channel {ownChannel}");
		}
		lock (Lock)
		{
			using (Game.Profile("BeginReceive Lock", log: true, 100f))
			{
				if (listen)
				{
					return;
				}
				listen = true;
			}
		}
		SetReceiveBuffer(memStream.Buffer);
		new Thread(StartAcceptingWork).Start();
	}

	private void OnReceive(int len)
	{
		if (multiplayer == null)
		{
			Multiplayer.Error("OnReceive called with null multiplayer!");
		}
		else if (multiplayer.asyncReceiver == null)
		{
			Multiplayer.Error($"OnReceive called with null asyncReceiver for multiplayer {this}!");
		}
		else
		{
			multiplayer.asyncReceiver.OnReceive(len);
		}
	}

	public override void OnConnected()
	{
	}

	public override void OnDisconnected()
	{
		if (isConnected)
		{
			isConnected = false;
			listen = false;
			multiplayer = null;
			Marshal.FreeHGlobal(dataPtr);
		}
	}

	public override void Close()
	{
		THQNORequest.CloseP2PChannelWithUser(targetId, (uint)targetChannel);
	}

	public override void CloseHandshakeChannel()
	{
		THQNORequest.CloseP2PChannelWithUser(targetId, THQNOAcceptor.ServerChannel);
	}

	public override bool IsConnected()
	{
		return isConnected;
	}

	public override string GetTarget()
	{
		return targetId;
	}

	private void StartAcceptingWork()
	{
		IntPtr intPtr = Marshal.AllocHGlobal(AsyncSender.BufferSize);
		while (true)
		{
			lock (Lock)
			{
				using (Game.Profile("StartAcceptingWork Lock", log: true, 100f))
				{
					if (!listen)
					{
						Marshal.FreeHGlobal(intPtr);
						break;
					}
				}
			}
			ListenForP2PPackets(intPtr);
			Thread.Sleep(THQNOAcceptor.SleepTime);
		}
	}

	private void ListenForP2PPackets(IntPtr dest)
	{
		if (THQNORequest.devIgnoreP2P)
		{
			return;
		}
		string senderId = null;
		uint messageSize = 0u;
		uint readMessageSize = 0u;
		uint bufferSize = (uint)AsyncSender.BufferSize;
		while (IfAvailableReadP2PPacket(out senderId, out messageSize, dest, bufferSize, out readMessageSize))
		{
			if (messageSize != readMessageSize)
			{
				Multiplayer.Error("THQNO.ReadP2PPacket() message not completely read!");
				break;
			}
			if (string.IsNullOrEmpty(senderId))
			{
				Multiplayer.Error("Message not read properly!");
				break;
			}
			if (!UserIdsEqual(senderId, GetTarget()))
			{
				Multiplayer.Error("Received a message not from the Server but from somebody else!");
				break;
			}
			try
			{
				Marshal.Copy(dest, receiveBuffer, 0, (int)messageSize);
			}
			catch (Exception arg)
			{
				Multiplayer.Error($"Exception while copying to receive buffer: {arg}");
				break;
			}
			int level = ((multiplayer == null || multiplayer.connectionReason == Multiplayer.ConnectionReason.Meta) ? 3 : 4);
			if (Multiplayer.LogEnabled(level))
			{
				int num = ExtractMessageID(receiveBuffer, (int)messageSize);
				if (num != 6)
				{
					Multiplayer.Log($"Received message {MsgIdToStr(num)} with length {messageSize} from player {senderId} on channel {ownChannel} in {this}", level);
				}
			}
			OnReceive((int)readMessageSize);
		}
	}

	private bool IfAvailableReadP2PPacket(out string senderId, out uint messageSize, IntPtr dest, uint destSize, out uint readMessageSize)
	{
		lock (THQNORequest.LockP2P)
		{
			using (Game.Profile("IfAvailableReadP2PPacket LockP2P - IsP2PPacketAvailable", log: true, 100f))
			{
				if (THQNORequest.IsP2PPacketAvailable(out messageSize, (uint)ownChannel).result.Bool())
				{
					using (Game.Profile("ReadP2PPacket LockP2P - ReadP2PPacket", log: true, 100f))
					{
						senderId = THQNORequest.ReadP2PPacket(dest, destSize, out readMessageSize, (uint)ownChannel);
					}
					return true;
				}
			}
		}
		senderId = null;
		messageSize = 0u;
		readMessageSize = 0u;
		return false;
	}

	private bool UserIdsEqual(string userId1, string userId2)
	{
		if (userId1 != userId2)
		{
			int num = userId1.LastIndexOf('_');
			if (num < 0)
			{
				Multiplayer.Error("CompareUserIds() called with an invalid user id " + userId1);
				return false;
			}
			string text = userId1.Substring(num);
			int num2 = userId2.LastIndexOf('_');
			if (num2 < 0)
			{
				Multiplayer.Error("CompareUserIds() called with an invalid user id " + userId2);
				return false;
			}
			string text2 = userId2.Substring(num2);
			return text == text2;
		}
		return true;
	}

	public override string ToString()
	{
		if (multiplayer != null)
		{
			return $"THQNOConnection({multiplayer})";
		}
		return $"THQNOConnection({THQNORequest.userId}:{ownChannel} <-> {GetTarget()}:{targetChannel})";
	}
}
