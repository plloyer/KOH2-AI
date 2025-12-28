namespace Logic;

public class ApplyIncome : Component
{
	public ApplyIncome(Kingdom kingdom)
		: base(kingdom)
	{
	}

	public void StartUpdating()
	{
		UpdateInBatch(base.game.update_5sec);
	}

	public override void OnStart()
	{
		base.OnStart();
	}

	public override void OnUpdate()
	{
		(obj as Kingdom).ApplyIncome();
	}
}
