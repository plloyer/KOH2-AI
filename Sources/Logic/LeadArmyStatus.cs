namespace Logic;

public class LeadArmyStatus : Status
{
	public string army_status
	{
		get
		{
			Army army = base.own_character.GetArmy();
			if (army == null)
			{
				return null;
			}
			if (army.movement.IsMoving())
			{
				return "marching";
			}
			if (army.castle != null && army.battle == null)
			{
				return "stationed";
			}
			if (army.battle != null && army.battle.type == Battle.Type.Plunder)
			{
				return "pillaging";
			}
			if (army.battle != null && army.battle.type == Battle.Type.Siege && army.battle.attackers.Contains(army))
			{
				return "besieging";
			}
			if (army.battle != null && army.battle.type == Battle.Type.Siege && army.battle.defenders.Contains(army))
			{
				return "defending";
			}
			if (army.battle != null)
			{
				return "fighting";
			}
			return "idle";
		}
	}

	public LeadArmyStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new LeadArmyStatus(def);
	}

	public override bool IsAutomatic()
	{
		return true;
	}

	public override bool IsIdle()
	{
		Army army = base.own_character.GetArmy();
		if (army == null)
		{
			return true;
		}
		if (army.battle != null)
		{
			return false;
		}
		return true;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"army_status" => army_status, 
			"status_text" => "LeadArmyStatus.status_texts." + army_status, 
			"enemy_kingdom" => base.own_character.GetArmy().GetVar("battle_enemy_kingdom"), 
			_ => base.GetVar(key, vars, as_value), 
		};
	}
}
