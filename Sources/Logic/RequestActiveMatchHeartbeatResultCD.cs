namespace Logic;

internal struct RequestActiveMatchHeartbeatResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public Common.MatchState matchState;

	public Common.MatchResultState matchResultState;

	public string matchId;
}
