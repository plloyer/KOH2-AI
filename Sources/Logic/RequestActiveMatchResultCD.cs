using System;

namespace Logic;

internal struct RequestActiveMatchResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public Common.MatchResultState matchResultState;

	public string matchId;

	public IntPtr oldControlledPlayerStats;

	public IntPtr oldEloScores;

	public IntPtr oldMatchRecords;

	public IntPtr newControlledPlayerStats;

	public IntPtr newEloScores;

	public IntPtr newMatchRecords;
}
