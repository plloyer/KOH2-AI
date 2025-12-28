namespace Logic;

public class Updateable : BaseObject
{
	public Time tmNextUpdate = Time.Zero;

	public Scheduler.UpdateQueue pUpdateQueue;

	public Updateable pPrevUpdateable;

	public Updateable pNextUpdateable;

	public bool IsRegisteredForUpdate()
	{
		return pUpdateQueue != null;
	}

	public virtual void OnUpdate()
	{
	}

	public Scheduler.UpdateBatch GetBatch()
	{
		if (pUpdateQueue != null)
		{
			return pUpdateQueue.pBatch;
		}
		return null;
	}
}
