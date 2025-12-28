namespace Logic;

public abstract class Connection
{
	public enum ConnectionType
	{
		Client,
		Server
	}

	public enum Status
	{
		NotConnected,
		Connecting,
		Connected
	}

	public Status status;

	public ConnectionType type;

	public Multiplayer multiplayer;

	public long last_send_time = long.MinValue;

	public abstract void Connect();

	public abstract bool Send(byte[] data, int length);

	public abstract void BeginReceive(MemStream memStream);

	public abstract string GetTarget();

	public abstract bool IsConnected();

	public abstract void OnConnected();

	public abstract void OnDisconnected();

	public abstract void Close();

	public abstract void CloseHandshakeChannel();

	public override string ToString()
	{
		if (multiplayer != null)
		{
			return $"[{status}][{type}] Connection ({multiplayer})";
		}
		return $"[{status}][{type}] Connection (to {GetTarget()})";
	}
}
