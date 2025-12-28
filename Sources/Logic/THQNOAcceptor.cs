using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Logic;

public class THQNOAcceptor : Acceptor
{
	public static readonly uint ServerChannel = 0u;

	public static readonly int SleepTime = 10;

	public Dictionary<string, THQNOConnection> connections = new Dictionary<string, THQNOConnection>();

	public static int lastChannelUsed = -1;

	private MemStream rx_stream;

	private MemStream tx_stream;

	private IntPtr sendDataPtr;

	private readonly object Lock = new object();

	private static Dictionary<string, int> lastConnectionsChannelsUsed = new Dictionary<string, int>();

	public THQNOAcceptor(Multiplayer multiplayer)
	{
		base.multiplayer = multiplayer;
		rx_stream = new MemStream(new byte[1024], 0, 0);
		tx_stream = new MemStream(new byte[1024]);
		sendDataPtr = Marshal.AllocHGlobal(1024);
	}

	public override void StartAccepting()
	{
		lock (Lock)
		{
			isListening = true;
		}
		new Thread(StartAcceptingWork).Start();
	}

	private void StartAcceptingWork()
	{
		IntPtr intPtr = Marshal.AllocHGlobal(AsyncSender.BufferSize);
		while (true)
		{
			lock (Lock)
			{
				if (!IsListening())
				{
					Marshal.FreeHGlobal(intPtr);
					break;
				}
			}
			ListenForP2PPackets(intPtr);
			Thread.Sleep(SleepTime);
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
				Multiplayer.Error("Cannot determine sender's id from THQNORequest.ReadP2PPacket!");
				break;
			}
			try
			{
				Marshal.Copy(dest, rx_stream.Buffer, 0, (int)messageSize);
			}
			catch (Exception)
			{
				break;
			}
			OnReceive((int)readMessageSize, senderId);
		}
	}

	private bool IfAvailableReadP2PPacket(out string senderId, out uint messageSize, IntPtr dest, uint destSize, out uint readMessageSize)
	{
		lock (THQNORequest.LockP2P)
		{
			using (Game.Profile("IfAvailableReadP2PPacket in LockP2P", log: true, 100f))
			{
				if (THQNORequest.IsP2PPacketAvailable(out messageSize, ServerChannel).result.Bool())
				{
					senderId = THQNORequest.ReadP2PPacket(dest, destSize, out readMessageSize, ServerChannel);
					return true;
				}
			}
		}
		senderId = null;
		messageSize = 0u;
		readMessageSize = 0u;
		return false;
	}

	private void OnReceive(int len, string thqnoId)
	{
		if (multiplayer != null && multiplayer.type != Multiplayer.Type.Server)
		{
			Multiplayer.Error($"Calling THQNOAcceptor.OnReceive with multiplayer of type {multiplayer.type}");
			return;
		}
		rx_stream.Length += len;
		int num = ReadMessageHeader();
		if (num == 3)
		{
			Multiplayer.ConnectionReason connectionReason = (Multiplayer.ConnectionReason)rx_stream.ReadByte();
			int num2 = rx_stream.Read7BitUInt();
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log($"Received Connect #{connectionReason.ToString()} message from {thqnoId}:{num2}", 2);
			}
			string playerHandle = Multiplayer.GetPlayerHandle(thqnoId, connectionReason);
			if (string.IsNullOrEmpty(playerHandle))
			{
				return;
			}
			if (lastConnectionsChannelsUsed.TryGetValue(playerHandle, out var value))
			{
				if (num2 > value)
				{
					SetupNewConnection(playerHandle, num2);
				}
				else
				{
					SendIncorrectChannel(playerHandle, num2, value);
				}
			}
			else
			{
				SetupNewConnection(playerHandle, num2);
			}
			rx_stream.Length = 0;
			rx_stream.Position = 0;
		}
		else
		{
			string text = "UNKNOWN";
			try
			{
				Multiplayer.MessageId messageId = (Multiplayer.MessageId)num;
				text = messageId.ToString();
			}
			catch
			{
			}
			Multiplayer.Error($"Received message {num}({text}) on channel {ServerChannel} that is not a Connect message from player with id {thqnoId}!");
		}
	}

	private int ReadMessageHeader()
	{
		int num = rx_stream.Read7BitUInt_Safe();
		if (num < 0 || num > rx_stream.Buffer.Length)
		{
			Multiplayer.Error($"Read message lenght of {num}");
			return -1;
		}
		return rx_stream.ReadByte();
	}

	private void SetupNewConnection(string player_handle, int playerChannel)
	{
		if (string.IsNullOrEmpty(player_handle))
		{
			Multiplayer.Error("SetupNewConnection() called with empty player handle");
			return;
		}
		if (lastChannelUsed < 0)
		{
			lastChannelUsed = 0;
		}
		lastChannelUsed++;
		string playerId = Multiplayer.GetPlayerId(player_handle);
		if (string.IsNullOrEmpty(playerId))
		{
			Multiplayer.Error("SetupNewConnection() cannot get player id from player handle " + player_handle);
			return;
		}
		THQNOConnection tHQNOConnection = new THQNOConnection(Connection.ConnectionType.Server, playerId, lastChannelUsed, playerChannel);
		if (lastConnectionsChannelsUsed.ContainsKey(player_handle))
		{
			lastConnectionsChannelsUsed[player_handle] = playerChannel;
		}
		else
		{
			lastConnectionsChannelsUsed.Add(player_handle, playerChannel);
		}
		if (connections.TryGetValue(player_handle, out var value))
		{
			if (Multiplayer.LogEnabled(2))
			{
				Multiplayer.Log($"Replacing old {value} with {tHQNOConnection}", 2);
			}
			if (value.multiplayer != null)
			{
				value.multiplayer.ShutDown();
			}
			connections.Remove(player_handle);
		}
		connections.Add(player_handle, tHQNOConnection);
		Multiplayer.ConnectionReason connectionReason = Multiplayer.GetConnectionReason(player_handle);
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"Setting up а new #{connectionReason.ToString()} connection: {tHQNOConnection}", 2);
		}
		OnConnected(tHQNOConnection, connectionReason);
	}

	private void SendIncorrectChannel(string player_handle, int playerChannel, int lastUsedChannel)
	{
		if (Multiplayer.LogEnabled(2))
		{
			Multiplayer.Log($"Sending invalid channel to player {player_handle}:{playerChannel}: the last used channel was {lastUsedChannel}", 2);
		}
		tx_stream.WriteByte(0);
		tx_stream.WriteByte(4);
		tx_stream.Write7BitUInt(lastUsedChannel);
		int position = tx_stream.Position;
		int num = position - 1;
		tx_stream.Buffer[0] = (byte)num;
		tx_stream.Position = 0;
		Marshal.Copy(tx_stream.Buffer, 0, sendDataPtr, position);
		string playerId = Multiplayer.GetPlayerId(player_handle);
		if (string.IsNullOrEmpty(player_handle))
		{
			Multiplayer.Error("SendInvalidChannel() - cannot extract player id from player handle " + player_handle);
			return;
		}
		lock (THQNORequest.LockP2P)
		{
			using (Game.Profile("SendIncorrectChannel in LockP2P", log: true, 100f))
			{
				THQNORequest.SendP2PPacket(playerId, sendDataPtr, (uint)position, Common.P2PSendType.ReliableOrdered, (uint)playerChannel);
			}
		}
	}

	public override void OnConnected(Connection connection, Multiplayer.ConnectionReason connectionReason)
	{
		if (multiplayer.OnAccept(connection, connectionReason) == null)
		{
			connection.OnDisconnected();
		}
	}

	public void RemoveConnection(string player_handle)
	{
		if (!connections.ContainsKey(player_handle))
		{
			Multiplayer.Error("Attempting to remove connection to player handle " + player_handle + " which is not present!");
		}
		else
		{
			connections.Remove(player_handle);
		}
	}

	public override void CleanUp()
	{
		lock (Lock)
		{
			isListening = false;
			multiplayer = null;
		}
	}
}
