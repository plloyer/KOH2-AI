namespace Logic;

internal struct AuthSessionTicketResultCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public Common.APIResult result;

	public uint ticketLength;

	public string sessionTicket;
}
