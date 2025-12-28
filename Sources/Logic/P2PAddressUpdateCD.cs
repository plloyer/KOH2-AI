namespace Logic;

internal struct P2PAddressUpdateCD
{
	private Common.CallbackType callbackType;

	public uint requestId;

	public string remoteID;

	public string address;

	public ushort port;
}
