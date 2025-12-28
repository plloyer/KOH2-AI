namespace Logic;

internal struct UserStatsReceivedCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public ulong gameId;

	public string userId;
}
