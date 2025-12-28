using System;

namespace Logic;

internal struct PlayerDataGetPersonalDataListResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public uint numResults;

	public IntPtr dataResponse;
}
