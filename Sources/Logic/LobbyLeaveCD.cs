namespace Logic;

internal struct LobbyLeaveCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public string lobbyId;

	public string memberChangedId;

	public string memberIdMakingChange;

	public Common.MemberLeftReason reason;
}
