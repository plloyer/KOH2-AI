namespace Logic;

public class AdoptPopulationAction : Action
{
	public AdoptPopulationAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new AdoptPopulationAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return "no_kingdom";
		}
		if (base.own_character?.GetArmy() != null)
		{
			return "leading_army";
		}
		if (kingdom.court == null)
		{
			return "no_court";
		}
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.cur_action is AdoptPopulationAction { is_active: not false } adoptPopulationAction && adoptPopulationAction.target == base.target)
			{
				return "_cleric_already_adopting";
			}
		}
		return base.Validate(quick_out);
	}

	public override bool ValidateTarget(Object target)
	{
		if (!NeedsTarget())
		{
			return true;
		}
		if (target == null)
		{
			return false;
		}
		if (def.target == "own_realm")
		{
			Castle castle = (target as Realm).castle;
			if (castle == null)
			{
				return false;
			}
			Kingdom kingdom = castle.GetKingdom();
			if (kingdom == null || kingdom != own_kingdom)
			{
				return false;
			}
			if (castle.battle != null)
			{
				return false;
			}
			Realm realm = castle.GetRealm();
			if (realm.IsOccupied() || !realm.IsDisorder() || realm.castle.battle != null)
			{
				return false;
			}
			for (int i = 0; i < kingdom.court.Count; i++)
			{
				Character character = kingdom.court[i];
				if (character != null && character.cur_action is AdoptPopulationAction { is_active: not false } adoptPopulationAction && adoptPopulationAction.target == target)
				{
					return false;
				}
			}
			return true;
		}
		return base.ValidateTarget(target);
	}

	public override void Run()
	{
		Castle castle = (base.target as Realm).castle;
		if (castle != null)
		{
			castle.GetRealm().SetDisorder(value: false);
			base.own_character.NotifyListeners("adopted_population", castle.GetRealm());
			base.Run();
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "we_share_pop_majority_religion")
		{
			Castle castle = (GetTarget(vars) as Realm).castle;
			if (castle == null)
			{
				return Value.Unknown;
			}
			Realm realm = castle.GetRealm();
			Kingdom kingdom = realm.GetKingdom();
			return realm.religion == kingdom.religion;
		}
		return base.GetVar(key, vars, as_value);
	}
}
