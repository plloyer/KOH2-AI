using System;

namespace Logic;

internal struct AutomatchmakingQueueInfoCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public IntPtr QueueInfoResponse;
}
