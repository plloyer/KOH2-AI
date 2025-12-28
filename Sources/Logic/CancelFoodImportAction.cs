namespace Logic;

public class CancelFoodImportAction : Action
{
	public CancelFoodImportAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CancelFoodImportAction(owner as Character, def);
	}

	public override void Run()
	{
		base.owner.DelStatus<ImportingFoodStatus>();
		own_kingdom?.RecalcIncomes();
	}
}
