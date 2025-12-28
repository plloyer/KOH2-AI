using System;
using System.Net;
using System.Net.Sockets;

namespace Logic;

public class TCPAcceptor : Acceptor
{
	private int port;

	private Socket socket;

	private bool socketInnited;

	public TCPAcceptor(int port, Multiplayer multiplayer)
	{
		this.port = port;
		base.multiplayer = multiplayer;
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	}

	public override void StartAccepting()
	{
		if (socket == null)
		{
			Multiplayer.Error("Trying to StartAccepting() with null socket!");
			return;
		}
		if (!socketInnited)
		{
			InitSocket();
		}
		try
		{
			socket.BeginAccept(AcceptCallback, this);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
			return;
		}
		isListening = true;
	}

	private void InitSocket()
	{
		IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
		try
		{
			socket.Bind(localEP);
			socket.Listen(100);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
			return;
		}
		socketInnited = true;
	}

	public void AcceptCallback(IAsyncResult ar)
	{
		try
		{
			TCPAcceptor tCPAcceptor = (TCPAcceptor)ar.AsyncState;
			if (!tCPAcceptor.IsListening())
			{
				return;
			}
			Socket socket = tCPAcceptor.socket.EndAccept(ar);
			TCPConnection connection = new TCPConnection(Connection.ConnectionType.Server, socket, null, tCPAcceptor);
			OnConnected(connection, Multiplayer.ConnectionReason.InGame);
		}
		catch (Exception ex)
		{
			Multiplayer.Error(ex.ToString());
		}
		StartAccepting();
	}

	public override void OnConnected(Connection connection, Multiplayer.ConnectionReason connectionReason)
	{
		multiplayer?.OnAccept(connection, connectionReason);
	}

	public override void CleanUp()
	{
		isListening = false;
		try
		{
			if (socket != null && socket.Connected)
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
		socketInnited = false;
		socket = null;
	}
}
