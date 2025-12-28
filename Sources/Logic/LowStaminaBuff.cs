namespace Logic;

public class LowStaminaBuff : SquadBuff
{
	public LowStaminaBuff(Squad squad, Def def, DT.Field field = null)
		: base(squad, def, field)
	{
	}

	public new static SquadBuff Create(Squad owner, Def def, DT.Field field = null)
	{
		return new LowStaminaBuff(owner, def, field);
	}

	public override string DebugUIText()
	{
		return "S";
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		squad.SetStamina(0f);
	}
}
