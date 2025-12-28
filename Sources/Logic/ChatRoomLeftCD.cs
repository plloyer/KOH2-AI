namespace Logic;

internal struct ChatRoomLeftCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public ulong roomId;

	public ulong userId;

	public ulong userMakingChange;

	public Common.ChatRoomUserLeftReason reason;
}
