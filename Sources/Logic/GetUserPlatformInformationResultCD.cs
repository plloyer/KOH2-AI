namespace Logic;

public struct GetUserPlatformInformationResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public bool canBeInvited;

	public Common.PlatformType primaryPlatform;

	public Common.PlatformType additionalPlatform;
}
