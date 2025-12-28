using System;

namespace Logic;

internal struct AutomatchmakingJoinResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public IntPtr MatchInitData;

	public string matchId;
}
