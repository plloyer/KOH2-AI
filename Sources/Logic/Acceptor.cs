namespace Logic;

public abstract class Acceptor
{
	protected bool isListening;

	protected Multiplayer multiplayer;

	public abstract void StartAccepting();

	public bool IsListening()
	{
		return isListening;
	}

	public abstract void OnConnected(Connection connection, Multiplayer.ConnectionReason connectionReason);

	public abstract void CleanUp();
}
