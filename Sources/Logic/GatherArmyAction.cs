namespace Logic;

public class GatherArmyAction : Action
{
	public GatherArmyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new GatherArmyAction(owner as Character, def);
	}

	public override void Run()
	{
		if (base.target is Castle castle)
		{
			base.own_character.select_army_on_spawn = true;
			base.own_character.SpawnArmy(castle);
			(base.own_character?.GetArmy())?.NotifyListeners("force_select");
			base.own_character.NotifyListeners("call_to_arms");
		}
		else if (base.target is Army army)
		{
			base.own_character.select_army_on_spawn = true;
			army.SetLeader(base.own_character);
			army.NotifyListeners("force_select");
			base.own_character.NotifyListeners("call_to_arms");
		}
		else
		{
			base.own_character.NotifyListeners("call_to_arms");
			base.Run();
		}
	}

	public override float PrepareDuration()
	{
		if (base.target is Army army)
		{
			Castle castle = army?.GetKingdom()?.GetCapital()?.castle;
			if (castle != null)
			{
				return 60f * army.position.Dist(castle.position) / 1000f;
			}
		}
		return base.PrepareDuration();
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.GetArmy() != null)
		{
			return "army_spawned";
		}
		if (!base.own_character.CanLeadArmy())
		{
			return "cant_lead_army";
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!base.ValidateTarget(target))
		{
			return false;
		}
		if (target is Castle castle)
		{
			Realm realm = castle?.GetRealm();
			if (castle.battle == null && realm != null && !realm.IsOccupied())
			{
				return !realm.IsDisorder();
			}
			return false;
		}
		if (target is Army army)
		{
			return army.IsHeadless();
		}
		return false;
	}

	public override void OnApplyEnterState()
	{
		base.OnApplyEnterState();
		if (state == State.Preparing)
		{
			base.own_character.select_army_on_spawn = true;
		}
	}
}
