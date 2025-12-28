using System;
using System.Net;
using System.Net.Sockets;

namespace Logic;

public class TCPConnection : Connection
{
	private Socket socket;

	private EndPoint ep;

	private TCPAcceptor tcpAcceptor;

	public TCPConnection(ConnectionType type, Socket socket = null, EndPoint ep = null, TCPAcceptor tcpAcceptor = null)
	{
		base.type = type;
		if (base.type == ConnectionType.Server)
		{
			if (ep != null)
			{
				Multiplayer.Error("Setting endpoint on a connection of type " + base.type.ToString() + "!");
			}
		}
		else if (base.type == ConnectionType.Client && tcpAcceptor != null)
		{
			Multiplayer.Error("Setting acceptor on a connection of type " + base.type.ToString() + "!");
		}
		this.ep = ep;
		this.tcpAcceptor = tcpAcceptor;
		this.socket = socket;
		if (this.socket == null)
		{
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}
	}

	public override void Connect()
	{
		try
		{
			socket.BeginConnect(ep, ConnectCallback, this);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
		}
	}

	private static void ConnectCallback(IAsyncResult ar)
	{
		TCPConnection tCPConnection = (TCPConnection)ar.AsyncState;
		try
		{
			if (tCPConnection.IsConnected())
			{
				tCPConnection.socket.EndConnect(ar);
			}
		}
		catch (Exception)
		{
			Multiplayer.Error($"{tCPConnection}: Failed to connect");
			return;
		}
		if (!tCPConnection.IsConnected())
		{
			Multiplayer.Error($"{tCPConnection}: Failed to connect");
		}
		else
		{
			tCPConnection?.multiplayer?.OnNetworkConnect();
		}
	}

	public override bool Send(byte[] data, int length)
	{
		if (!IsConnected())
		{
			return false;
		}
		try
		{
			socket.BeginSend(data, 0, length, SocketFlags.None, SendCallback, this);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
			return false;
		}
		return true;
	}

	private static void SendCallback(IAsyncResult ar)
	{
		TCPConnection tCPConnection = (TCPConnection)ar.AsyncState;
		if (ar.IsCompleted && tCPConnection.IsConnected())
		{
			int sent_len = 0;
			try
			{
				sent_len = tCPConnection.socket.EndSend(ar);
			}
			catch (Exception)
			{
			}
			tCPConnection.multiplayer.asyncSender.OnSent(sent_len);
		}
	}

	public override void BeginReceive(MemStream memStream)
	{
		if (!IsConnected())
		{
			Multiplayer.Error($"TCPConnection is not connected on BeginReceive in {multiplayer}");
			return;
		}
		try
		{
			socket.BeginReceive(memStream.Buffer, memStream.Length, AsyncSender.BufferSize - memStream.Length, SocketFlags.None, ReceiveCallback, this);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
		}
	}

	private static void ReceiveCallback(IAsyncResult ar)
	{
		TCPConnection tCPConnection = (TCPConnection)ar.AsyncState;
		if (tCPConnection.socket == null)
		{
			return;
		}
		int num = -1;
		try
		{
			if (!tCPConnection.IsConnected())
			{
				Multiplayer.Error($"{tCPConnection}: tcpConnection.IsConnected() is false on EndReceive()");
				return;
			}
			num = tCPConnection.socket.EndReceive(ar);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
			return;
		}
		if (num == 0)
		{
			tCPConnection.multiplayer.Disconnect(gotDisconnectedByPeer: true);
			return;
		}
		if (num < 0)
		{
			Multiplayer.Error($"{tCPConnection}: Something went wrong. Could not read data packet but no exception has been catched. len == {num}. Data will most likely be dropped.");
			return;
		}
		MemStream memStream = tCPConnection.multiplayer.asyncReceiver.OnReceive(num);
		if (tCPConnection.IsConnected())
		{
			tCPConnection.BeginReceive(memStream);
		}
	}

	public override void OnConnected()
	{
	}

	public override void OnDisconnected()
	{
		CleanUp();
	}

	public override void Close()
	{
	}

	public override void CloseHandshakeChannel()
	{
	}

	public override string GetTarget()
	{
		if (ep == null)
		{
			return string.Empty;
		}
		return ep.ToString();
	}

	public override bool IsConnected()
	{
		if (socket != null)
		{
			return socket.Connected;
		}
		return false;
	}

	private void CleanUp()
	{
		try
		{
			if (IsConnected())
			{
				socket?.Shutdown(SocketShutdown.Both);
			}
			socket?.Close();
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
			return;
		}
		socket = null;
	}
}
