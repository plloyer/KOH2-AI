namespace Logic;

internal struct LobbyListReceivedCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public string lobbyIds;
}
