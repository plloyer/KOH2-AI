namespace Logic;

public class CancelFoodExportAction : Action
{
	public CancelFoodExportAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CancelFoodExportAction(owner as Character, def);
	}

	public override void Run()
	{
		base.owner.DelStatus<ExportingFoodStatus>();
		own_kingdom?.AddRelationModifier((base.owner as Character)?.mission_kingdom, "rel_stop_exporting_food", null);
		own_kingdom?.InvalidateIncomes();
	}
}
