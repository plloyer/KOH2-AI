namespace Logic;

public class PuppetArmySurrenderAction : PuppetPlot
{
	private bool isSiege;

	private bool isPuppetAttacker;

	private Realm realm;

	private Battle battle;

	public PuppetArmySurrenderAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetArmySurrenderAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (own_kingdom.GetFreeCourtSlotIndex() == -1)
		{
			return "_no_free_court_slot";
		}
		return base.Validate(quick_out);
	}

	public override string ValidatePuppet(Character puppet)
	{
		Army army = puppet?.GetArmy();
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.IsCrusader())
		{
			return "is_crusader";
		}
		if (puppet.IsRebel())
		{
			return "is_rebel";
		}
		if (army == null)
		{
			return "no_army";
		}
		if (army.battle == null)
		{
			return "not_in_a_battle";
		}
		if ((!army.battle.attacker.IsOwnStance(puppet) || army.battle.defender_kingdom != own_kingdom) && (!army.battle.defender.IsOwnStance(puppet) || army.battle.attacker_kingdom != own_kingdom))
		{
			return "not_a_battle_participant";
		}
		return base.ValidatePuppet(puppet);
	}

	public override void Prepare()
	{
		Army army = (base.target as Character)?.GetArmy();
		battle = army?.battle;
		realm = base.game.GetRealm(battle?.realm_id ?? 0);
		isPuppetAttacker = battle?.attackers?.Contains(army) ?? false;
		isSiege = battle?.settlement is Castle;
		base.Prepare();
	}

	public override void Run()
	{
		Character obj = base.target as Character;
		obj.GetArmy().battle.Cancel(Battle.VictoryReason.Surrender);
		PuppetFleeKingdom.Flee(obj, base.own_character, own_kingdom);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"captured_realm" => (isSiege && !isPuppetAttacker) ? realm : null, 
			"defended_realm" => (isSiege && isPuppetAttacker) ? realm : null, 
			"realm" => realm, 
			"goto_target" => battle, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}
}
