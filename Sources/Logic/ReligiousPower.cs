namespace Logic;

public class ReligiousPower : Component
{
	public float min { get; private set; }

	public float amount { get; private set; }

	public float maxAmount { get; private set; }

	public ReligiousPower(Object obj)
		: base(obj)
	{
		maxAmount = 100f;
	}

	public void InitAmount(float amount)
	{
		this.amount = amount;
	}

	public bool ChangeAmount(float change)
	{
		float num = change + amount;
		amount = ((num < 0f) ? 0f : ((num > maxAmount) ? maxAmount : num));
		obj.NotifyListeners("religiouspower_changed", this);
		return true;
	}
}
