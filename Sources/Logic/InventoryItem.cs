namespace Logic;

public class InventoryItem : IVars
{
	public Unit.Def def;

	public Army army;

	public IListener visuals;

	public float damage;

	public BattleSimulation.Squad simulation;

	public int battle_row = -1;

	public int battle_col = -1;

	public override string ToString()
	{
		return ((def == null) ? "unknown" : def.name) + " at " + battle_row + "," + battle_col;
	}

	public int Index()
	{
		if (army == null || army.siege_equipment == null)
		{
			return -1;
		}
		return army.siege_equipment.IndexOf(this);
	}

	public bool IsDefeated()
	{
		if (damage >= 1f)
		{
			return true;
		}
		if (simulation != null && simulation.IsDefeated())
		{
			return true;
		}
		return false;
	}

	public void SetDamage(float damage, bool send_state = true)
	{
		this.damage = damage;
		if (send_state && army != null)
		{
			army.SendSubstate<Army.UnitsState.UnitState>(Index());
		}
	}

	public Resource GetUpkeep(int idx)
	{
		return def.CalcUpkeep(army, null, idx);
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "upkeep")
		{
			return GetUpkeep(Index());
		}
		return Value.Unknown;
	}

	public void OnAssignedAnalytics(Army army, string assignAction)
	{
		Kingdom kingdom = null;
		Realm realm = null;
		int num = -1;
		if (army != null)
		{
			kingdom = army.GetKingdom();
			realm = army.realm_in;
			num = army.GetNid();
		}
		if (kingdom != null && kingdom.is_player && realm != null && num != -1 && kingdom.game.IsRunning() && def.type == Unit.Type.InventoryItem)
		{
			Vars vars = new Vars();
			vars.Set("armyID", num.ToString());
			vars.Set("unitID", ToString());
			if (army != null && army.leader != null)
			{
				vars.Set("characterName", army.leader.Name);
			}
			vars.Set("unitName", def.name);
			vars.Set("unitType", def.type.ToString());
			vars.Set("unitTier", def.tier);
			vars.Set("province", realm.name);
			vars.Set("unitLocation", "army");
			vars.Set("unitQuantity", 1);
			vars.Set("unitPower", 1);
			vars.Set("unitHealth", 1f);
			vars.Set("unitLevel", 0);
			vars.Set("unitXP", 0);
			vars.Set("assignAction", assignAction);
			kingdom.FireEvent("analytics_unit_assigned", vars, kingdom.id);
		}
	}
}
