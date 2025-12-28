namespace Logic;

internal struct LobbyInviteCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public string userId;

	public string lobbyId;

	public bool spectate;
}
