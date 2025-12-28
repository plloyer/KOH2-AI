namespace Logic;

internal struct ResetPasswordResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public string userId;
}
