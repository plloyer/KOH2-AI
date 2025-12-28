namespace Logic;

internal struct ChatRoomJoinResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public ulong roomId;
}
