namespace Logic;

internal struct ChatRoomMessageCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public ulong roomId;

	public ulong userId;

	public string messageText;
}
