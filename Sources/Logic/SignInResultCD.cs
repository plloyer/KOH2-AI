namespace Logic;

public struct SignInResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public string userId;

	public bool networkingAvaliable;
}
