namespace Logic;

public class CancelImportGoodAction : Action
{
	public CancelImportGoodAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CancelImportGoodAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		return "ok";
	}

	public override void Run()
	{
		base.own_character.ImportGood(null, args[0]);
		base.own_character.FireEvent("run_cancel_import_good_action", null, own_kingdom.id);
		base.Run();
	}
}
