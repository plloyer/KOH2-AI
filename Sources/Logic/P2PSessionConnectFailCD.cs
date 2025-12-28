namespace Logic;

internal struct P2PSessionConnectFailCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public string remoteID;

	public Common.P2PConnectionError P2PSessionError;
}
