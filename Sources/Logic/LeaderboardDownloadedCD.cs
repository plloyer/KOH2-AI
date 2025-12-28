using System;

namespace Logic;

internal struct LeaderboardDownloadedCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public IntPtr leaderboardEntries;

	public uint numEntries;
}
