using System;

namespace Logic;

internal struct ControlledStatsResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public IntPtr request;

	public IntPtr controlledStats;
}
