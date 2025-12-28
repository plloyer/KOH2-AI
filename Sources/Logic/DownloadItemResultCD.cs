namespace Logic;

internal struct DownloadItemResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public string EntitlementId;

	public ulong ContentId;
}
