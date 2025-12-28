using System;

namespace Logic;

internal struct PlayerStatsResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public IntPtr request;

	public IntPtr playerStats;
}
