namespace Logic;

public class SiegeDefenseDrop : Component
{
	private Settlement settlement;

	private float refresh_interval = 15f;

	private Battle battle;

	private const float base_fortification_hit_chance = 50f;

	public SiegeDefenseDrop(Battle battle)
		: base(battle)
	{
		this.battle = battle;
		settlement = battle.settlement;
	}

	public void Begin(float random_offset = 15f)
	{
		if (settlement != null)
		{
			UpdateAfter(random_offset);
		}
	}

	public void Reset()
	{
		if (settlement != null)
		{
			StopUpdating();
			UpdateAfter(refresh_interval);
		}
	}

	public void Stop()
	{
		StopUpdating();
	}

	public override void OnUpdate()
	{
		if (settlement == null)
		{
			return;
		}
		if (!settlement.IsAuthority())
		{
			UpdateAfter(refresh_interval);
			return;
		}
		bool flag = false;
		Kingdom kingdom = battle?.attacker?.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		Character character = battle?.attacker?.leader;
		float num = 1f;
		float num2 = 0f;
		float num3 = 0f;
		float stat;
		if (character != null)
		{
			stat = character.GetStat(Stats.cs_siege_attack);
			num = character.GetStat(Stats.cs_siege_attack_perc);
			num2 = character.GetStat(Stats.cs_base_siege_damage_perc);
			num3 = character.GetStat(Stats.cs_siege_equipment_siege_damage_perc);
		}
		else
		{
			stat = kingdom.GetStat(Stats.ks_siege_attack);
			num = kingdom.GetStat(Stats.ks_siege_attack_perc);
			num2 = kingdom.GetStat(Stats.ks_base_siege_damage_perc);
			num3 = kingdom.GetStat(Stats.ks_siege_equipment_siege_damage_perc);
		}
		float num4 = (50f + stat) * (1f + num / 100f);
		float num5 = 1f;
		if (settlement.keep_effects.CanBeTakenOver() && settlement.type == "Keep")
		{
			num5 *= battle.def.keep_siege_defense_damage_mod;
		}
		if ((float)base.game.Random(0, 100) < num4)
		{
			battle.siege_defense -= battle.def.base_siege_damage * (1f + num2 / 100f) * num5;
			flag = true;
		}
		for (int i = 0; i < battle.attackers.Count; i++)
		{
			for (int j = 0; j < battle.attackers[i].siege_equipment.Count; j++)
			{
				if ((float)base.game.Random(0, 100) < num4)
				{
					battle.siege_defense -= battle.attackers[i].siege_equipment[j].def.siege_damage * (1f + num3 / 100f) * num5;
					flag = true;
				}
			}
		}
		if (battle.siege_defense < 0f)
		{
			battle.siege_defense = 0f;
		}
		if (flag)
		{
			battle.NotifyListeners("fortification_health_changed");
			battle.SendState<Battle.SiegeStatsState>();
		}
		UpdateAfter(refresh_interval);
	}
}
