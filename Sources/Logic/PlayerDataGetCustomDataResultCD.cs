namespace Logic;

internal struct PlayerDataGetCustomDataResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public string userId;

	public uint customDataLength;

	public string customData;
}
