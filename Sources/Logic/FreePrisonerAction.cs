namespace Logic;

public class FreePrisonerAction : PrisonAction
{
	public FreePrisonerAction(Kingdom owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new FreePrisonerAction(owner as Kingdom, def);
	}

	public override void Run()
	{
		if (base.target is Character character)
		{
			character.OnPrisonActionAnalytics("freed");
			using (Game.Profile("Free prisoner"))
			{
				character.Imprison(null, recall: true, send_state: true, null, !character.IsRebel());
			}
			base.Run();
		}
	}

	public override bool ValidateTarget(Object target)
	{
		if (target == null || !target.IsValid())
		{
			return false;
		}
		if ((target as Character).IsPrisonForgivable())
		{
			return false;
		}
		return base.ValidateTarget(target);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "is_court_rebel")
		{
			return own_kingdom.special_court.Contains(base.target as Character) && (base.target as Character).IsRebel();
		}
		return base.GetVar(key, vars, as_value);
	}
}
