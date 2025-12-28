namespace Logic;

public class EstablishOrderAction : Action
{
	public EstablishOrderAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new EstablishOrderAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "no_character";
		}
		Army army = character.GetArmy();
		if (army == null)
		{
			return "no_army";
		}
		if (army.castle == null)
		{
			return "no_castle";
		}
		if (army.battle != null)
		{
			return "in_battle";
		}
		if (army.castle.battle != null)
		{
			return "in_battle";
		}
		if (!army.castle.GetRealm().IsDisorder())
		{
			return "no_disorder";
		}
		return base.Validate(quick_out);
	}

	public override void Prepare()
	{
		own_kingdom.GetCrownAuthority().AddModifier("establish_order");
		Realm realm = base.own_character?.GetArmy()?.castle?.GetRealm();
		Kingdom kingdom = realm?.pop_majority.kingdom;
		own_kingdom.AddRelationModifier(kingdom, "rel_establish_order_owner", null);
		int num = def.field.GetInt("rel_drop_all_max_dist", null, -1);
		if (num > 0)
		{
			for (int i = 0; i < base.game.kingdoms.Count; i++)
			{
				Kingdom kingdom2 = base.game.kingdoms[i];
				if (kingdom2 != kingdom && !((float)own_kingdom.DistanceToKingdom(kingdom2) > (float)num))
				{
					own_kingdom.AddRelationModifier(kingdom2, "rel_establish_order_rest", null);
				}
			}
		}
		else
		{
			foreach (Kingdom neighbor in own_kingdom.neighbors)
			{
				own_kingdom.AddRelationModifier(neighbor, "rel_establish_order_rest", null);
			}
		}
		int num2 = def.field.GetInt("population_drop");
		if (num2 > 0)
		{
			realm?.castle?.population?.AddVillagers(num2, Population.Type.Worker);
		}
		else if (num2 < 0)
		{
			realm?.castle?.population?.RemoveVillagers(-num2, Population.Type.Worker);
		}
		if (realm?.stats != null)
		{
			realm.DelEstablishOrderMod();
			realm.stats.AddModifier("rs_stability_establish_order", new FadingModifier(base.game, base.game.defs.Get<FadingModifier.Def>("EstablishOrderModifier"), realm, realm));
			realm.rebellionRisk?.Recalc(think_rebel_pop: false, allow_rebel_spawn: false);
		}
		base.Prepare();
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			Realm realm = character?.GetArmy()?.castle?.GetRealm();
			if (realm != null)
			{
				realm.SetDisorder(value: false);
				character.NotifyListeners("established_order", realm);
			}
			base.Run();
		}
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "fail")
		{
			Army army = base.own_character?.GetArmy();
			Realm realm = army?.castle?.GetRealm();
			if (realm == null)
			{
				return false;
			}
			for (int num = army.castle.garrison.units.Count - 1; num >= 0; num--)
			{
				Unit unit = army.castle.garrison.units[num];
				if (army.units.Count < army.MaxUnits())
				{
					army.MoveUnitFromGarrison(unit);
				}
				else
				{
					army.castle.garrison.DelUnit(unit);
				}
			}
			Rebel rebel = realm.rebellionRisk.ForceRebel();
			rebel.Start();
			realm.SetOccupied(rebel.rebellion);
			return true;
		}
		return base.ApplyOutcome(outcome);
	}

	public override Kingdom CalcTargetKingdom(Object target)
	{
		return (base.own_character?.GetArmy()?.castle?.GetRealm())?.pop_majority.kingdom;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "realm")
		{
			return base.own_character?.GetArmy()?.castle?.GetRealm();
		}
		return base.GetVar(key, vars, as_value);
	}
}
