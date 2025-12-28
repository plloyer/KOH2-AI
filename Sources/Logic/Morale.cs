using System;
using System.Collections.Generic;

namespace Logic;

public class Morale : Component
{
	public class Def : Logic.Def
	{
		public float max_morale = 15f;

		public float temp_morale_per_minute = 1f;

		public float max_temp_morale_difference = 15f;

		public float diminishing_returns = 10f;

		public float rebel_leader_level_morale_mod = 1f;

		public float rebel_supporter_leader_level_morale_mod = 0.3f;

		public float rebel_leader_morale_per_supporter = 1f;

		public float rebel_leader_morale_per_lieutenant = 3f;

		public float rebel_supporter_morale_per_army = 1f;

		public float rebel_morale_per_occupied_province = 3f;

		public float rebel_famous_morale_leader = 3f;

		public float rebel_famous_morale_lieutenant = 2f;

		public float rebel_famous_morale = 1f;

		public float morale_if_camping = 5f;

		public float army_morale_loss_marshal_death = 5f;

		public float starvation_kingdom_morale_penalty = 1f;

		public PerLevelValues morale_enemy_realm;

		public float morale_per_enemy_keep = -1f;

		public float morale_per_friendly_keep = 3f;

		public float morale_same_pop_majority = 5f;

		public float morale_on_battle_won_enemy = 5f;

		public float morale_on_battle_lost_enemy = -5f;

		public float morale_on_battle_won_neutral = 3f;

		public float morale_on_battle_lost_neutral = -3f;

		public float morale_on_battle_won_own_or_ally = 2f;

		public float morale_on_battle_lost_own_or_ally = -2f;

		public float morale_on_plunder_won_as_attacker = 3f;

		public float morale_on_battle_nearby_won = 3f;

		public float morale_on_battle_nearby_lost = -3f;

		public float morale_on_battle_nearby_town_lost = -5f;

		public float morale_king_died = -3f;

		public float morale_on_war_lost = -5f;

		public override bool Load(Game game)
		{
			DT.Field field = dt_def.field;
			max_morale = field.GetFloat("max_morale", null, max_morale);
			temp_morale_per_minute = field.GetFloat("temp_morale_per_minute", null, temp_morale_per_minute);
			max_temp_morale_difference = field.GetFloat("max_temp_morale_difference", null, max_temp_morale_difference);
			diminishing_returns = field.GetFloat("diminishing_returns", null, diminishing_returns);
			army_morale_loss_marshal_death = field.GetFloat("army_morale_loss_marshal_death ", null, army_morale_loss_marshal_death);
			starvation_kingdom_morale_penalty = field.GetFloat("starvation_kingdom_morale_penalty", null, starvation_kingdom_morale_penalty);
			morale_on_battle_won_enemy = field.GetFloat("morale_on_battle_won_enemy", null, morale_on_battle_won_enemy);
			morale_on_battle_lost_enemy = field.GetFloat("morale_on_battle_lost_enemy", null, morale_on_battle_lost_enemy);
			morale_on_battle_won_neutral = field.GetFloat("morale_on_battle_won_neutral", null, morale_on_battle_won_neutral);
			morale_on_battle_lost_neutral = field.GetFloat("morale_on_battle_lost_neutral", null, morale_on_battle_lost_neutral);
			morale_on_battle_won_own_or_ally = field.GetFloat("morale_on_battle_won_own_or_ally", null, morale_on_battle_won_own_or_ally);
			morale_on_battle_lost_own_or_ally = field.GetFloat("morale_on_battle_lost_own_or_ally", null, morale_on_battle_lost_own_or_ally);
			morale_on_plunder_won_as_attacker = field.GetFloat("morale_on_plunder_won_as_attacker", null, morale_on_plunder_won_as_attacker);
			morale_on_battle_nearby_won = field.GetFloat("morale_on_battle_nearby_won", null, morale_on_battle_nearby_won);
			morale_on_battle_nearby_lost = field.GetFloat("morale_on_battle_nearby_lost", null, morale_on_battle_nearby_lost);
			morale_on_battle_nearby_town_lost = field.GetFloat("morale_on_battle_nearby_town_lost", null, morale_on_battle_nearby_town_lost);
			morale_enemy_realm = PerLevelValues.Parse<float>(base.field.FindChild("morale_enemy_realm"));
			morale_per_enemy_keep = field.GetFloat("morale_per_enemy_keep", null, morale_per_enemy_keep);
			morale_per_friendly_keep = field.GetFloat("morale_per_friendly_keep", null, morale_per_friendly_keep);
			rebel_leader_level_morale_mod = field.GetFloat("rebel_leader_level_morale_mod", null, rebel_leader_level_morale_mod);
			rebel_supporter_leader_level_morale_mod = field.GetFloat("rebel_supporter_leader_level_morale_mod", null, rebel_supporter_leader_level_morale_mod);
			rebel_leader_morale_per_supporter = field.GetFloat("rebel_leader_morale_per_supporter", null, rebel_leader_morale_per_supporter);
			rebel_leader_morale_per_lieutenant = field.GetFloat("rebel_leader_morale_per_lieutenant", null, rebel_leader_morale_per_lieutenant);
			rebel_supporter_morale_per_army = field.GetFloat("rebel_supporter_morale_per_army", null, rebel_supporter_morale_per_army);
			rebel_morale_per_occupied_province = field.GetFloat("rebel_morale_per_occupied_province", null, rebel_morale_per_occupied_province);
			rebel_famous_morale_leader = field.GetFloat("rebel_famous_morale_leader", null, rebel_famous_morale_leader);
			rebel_famous_morale_lieutenant = field.GetFloat("rebel_famous_morale_lieutenant", null, rebel_famous_morale_lieutenant);
			rebel_famous_morale = field.GetFloat("rebel_famous_morale", null, rebel_famous_morale);
			morale_if_camping = field.GetFloat("morale_if_camping", null, morale_if_camping);
			morale_same_pop_majority = field.GetFloat("morale_same_pop_majority", null, morale_same_pop_majority);
			morale_king_died = field.GetFloat("morale_king_died", null, morale_king_died);
			morale_on_war_lost = field.GetFloat("morale_on_war_lost", null, morale_on_war_lost);
			return true;
		}
	}

	public struct MoraleFactor
	{
		public Stat stat;

		public Stat.Modifier mod;

		public float value;
	}

	public float permanent_morale;

	public ComputableValue temporary_morale;

	public Army army;

	public Castle castle;

	public float morale_in_own_realm;

	public float morale_in_allied_realm;

	public float morale_in_neutral_realm;

	public float morale_in_enemy_realm;

	private static List<MoraleFactor> tmp_morale_factors = new List<MoraleFactor>();

	public Def def
	{
		get
		{
			if (base.game != null)
			{
				return base.game.defs.GetBase<Def>();
			}
			return null;
		}
	}

	public Morale(Army army)
		: base(army)
	{
		this.army = army;
		castle = null;
		temporary_morale = new ComputableValue(0f, 0f, base.game, 0f - def.max_temp_morale_difference, def.max_temp_morale_difference);
		RecalcRealmMorale();
	}

	public Morale(Castle castle)
		: base(castle)
	{
		army = null;
		this.castle = castle;
		temporary_morale = new ComputableValue(0f, 0f, base.game, 0f - def.max_temp_morale_difference, def.max_temp_morale_difference);
		RecalcRealmMorale();
	}

	public void AddTemporaryMorale(float val)
	{
		float num = temporary_morale.Get();
		if (Math.Sign(val) == Math.Sign(num))
		{
			val *= 1f - Math.Abs(num) * def.diminishing_returns / 100f;
		}
		float num2 = permanent_morale + num;
		if (val > 0f)
		{
			float num3 = num2 - def.max_morale;
			if (num3 > 0f)
			{
				if (!(val > num3))
				{
					return;
				}
				temporary_morale.Add(0f - num3);
			}
		}
		if (val < 0f)
		{
			float num4 = num2;
			if (num4 < 0f)
			{
				if (!(val < num4))
				{
					return;
				}
				temporary_morale.Add(0f - num4);
			}
		}
		temporary_morale.Add(val, clamp: false);
		RecalcTemporaryMorale();
	}

	public void RecalcTemporaryMorale()
	{
		float num = temporary_morale.Get();
		float num2 = def.max_temp_morale_difference;
		float num3 = 0f - def.max_temp_morale_difference;
		float num4;
		if (army.battle != null)
		{
			num4 = 0f;
		}
		else if (num > 0f)
		{
			num4 = (0f - def.temp_morale_per_minute) / 60f;
			num2 = def.max_temp_morale_difference;
			num3 = 0f;
		}
		else if (num < 0f)
		{
			num4 = def.temp_morale_per_minute / 60f;
			num2 = 0f;
			num3 = 0f - def.max_temp_morale_difference;
		}
		else
		{
			num4 = 0f;
		}
		bool flag = false;
		if (num4 != temporary_morale.GetRate())
		{
			flag = true;
			temporary_morale.SetRate(num4);
		}
		if (num3 != temporary_morale.GetMin() || num2 != temporary_morale.GetMax())
		{
			flag = true;
			temporary_morale.SetMinMax(num3, num2);
		}
		if (flag)
		{
			if (army != null && army.IsAuthority())
			{
				army.SendState<Army.MoraleState>();
			}
			if (castle != null && castle.IsAuthority())
			{
				castle.SendState<Castle.MoraleState>();
			}
		}
	}

	private void RecalcRealmMorale()
	{
		morale_in_own_realm = 0f;
		morale_in_allied_realm = 0f;
		morale_in_neutral_realm = 0f;
		morale_in_enemy_realm = 0f;
		if (army?.realm_in == null)
		{
			return;
		}
		Kingdom kingdom = army.GetKingdom();
		if (army.realm_in == null || kingdom == null)
		{
			return;
		}
		RelationUtils.Stance stance = army.realm_in.GetStance(army);
		if (army?.game?.path_finding?.data != null && !army.game.path_finding.data.OutOfMapBounds(army.position) && army.position.paID == 0 && !army.game.path_finding.data.OutOfMapBounds(army.position) && army.game.path_finding.data.GetNode(army.position).ocean)
		{
			stance = RelationUtils.Stance.None;
		}
		if ((stance & RelationUtils.Stance.Own) != RelationUtils.Stance.None)
		{
			morale_in_own_realm += kingdom.GetStat(Stats.ks_army_morale_at_home);
			return;
		}
		if ((stance & RelationUtils.Stance.Alliance) != RelationUtils.Stance.None)
		{
			morale_in_allied_realm += kingdom.GetStat(Stats.ks_army_morale_at_allied);
			return;
		}
		if ((stance & RelationUtils.Stance.War) == 0)
		{
			morale_in_neutral_realm += kingdom.GetStat(Stats.ks_army_morale_at_neutral);
			return;
		}
		morale_in_enemy_realm += army.realm_in.GetStat(Stats.rs_morale_in_province);
		Queue<Realm> queue = new Queue<Realm>(64);
		for (int i = 0; i < base.game.realms.Count; i++)
		{
			base.game.realms[i].wave_depth = -1;
		}
		army.realm_in.wave_depth = 0;
		queue.Enqueue(army.realm_in);
		while (queue.Count > 0)
		{
			Realm realm = queue.Dequeue();
			int num = realm.wave_depth + 1;
			if (num > def.morale_enemy_realm.items.Count)
			{
				continue;
			}
			for (int j = 0; j < realm.neighbors.Count; j++)
			{
				Realm realm2 = realm.neighbors[j];
				if (realm2.wave_depth < 0)
				{
					if (realm2.GetKingdom() == kingdom)
					{
						morale_in_enemy_realm += def.morale_enemy_realm.GetFloat(num);
						return;
					}
					realm2.wave_depth = num;
					queue.Enqueue(realm2);
				}
			}
		}
	}

	public static void AddMoraleFactor(Stats stats, StatName stat_name)
	{
		Stat stat = stats?.Find(stat_name);
		if (stat != null)
		{
			float base_value = stat.base_value;
			MoraleFactor item = new MoraleFactor
			{
				stat = stat,
				mod = null,
				value = base_value
			};
			tmp_morale_factors.Add(item);
		}
	}

	public static void AddMoraleFactor(Stat stat, Stat.Modifier mod, bool include_inactive)
	{
		float num = mod.CalcValue(stat.stats, stat);
		if (num != 0f || include_inactive)
		{
			MoraleFactor item = new MoraleFactor
			{
				stat = stat,
				mod = mod,
				value = num
			};
			tmp_morale_factors.Add(item);
		}
	}

	public List<MoraleFactor> GetMoraleFactorStats()
	{
		tmp_morale_factors.Clear();
		if (army == null)
		{
			AddMoraleFactor(castle?.GetRealm()?.stats, Stats.rs_garrison_morale);
		}
		else if (army.rebel != null)
		{
			AddMoraleFactor(army.leader?.stats, Stats.cs_rebel_morale);
		}
		else if (army.leader != null)
		{
			if (army.leader.IsCrusader())
			{
				AddMoraleFactor(army.leader.stats, Stats.cs_crusader_morale);
			}
			else
			{
				AddMoraleFactor(army.leader.stats, Stats.cs_army_morale);
				if (army.leader.IsKing())
				{
					AddMoraleFactor(army.leader.stats, Stats.cs_king_army_morale);
				}
			}
		}
		return tmp_morale_factors;
	}

	public static void AddMoraleFactorMods(Stat stat, bool include_inactive)
	{
		if (stat?.all_mods != null)
		{
			for (int i = 0; i < stat.all_mods.Count; i++)
			{
				Stat.Modifier modifier = stat.all_mods[i];
				if (modifier is StatRefModifier statRefModifier)
				{
					AddMoraleFactorMods(statRefModifier.tgt_stat, include_inactive);
				}
				else
				{
					AddMoraleFactor(stat, modifier, include_inactive);
				}
			}
		}
		if (stat?.def?.global_mods != null)
		{
			for (int j = 0; j < stat.def.global_mods.Count; j++)
			{
				Stat.GlobalModifier mod = stat.def.global_mods[j];
				AddMoraleFactor(stat, mod, include_inactive);
			}
		}
	}

	public static void AddMoraleFactorMods(bool include_inactive)
	{
		int count = tmp_morale_factors.Count;
		for (int i = 0; i < count; i++)
		{
			MoraleFactor moraleFactor = tmp_morale_factors[i];
			if (moraleFactor.mod == null)
			{
				AddMoraleFactorMods(moraleFactor.stat, include_inactive);
			}
		}
	}

	public void RecalcPermanentMorale(bool force_send = false, bool recalc_dist = false)
	{
		permanent_morale = 0f;
		if (recalc_dist && army != null)
		{
			RecalcRealmMorale();
		}
		List<MoraleFactor> moraleFactorStats = GetMoraleFactorStats();
		for (int i = 0; i < moraleFactorStats.Count; i++)
		{
			permanent_morale += moraleFactorStats[i].stat.CalcValue();
		}
		permanent_morale = Math.Max(0f, Math.Min(def.max_morale, permanent_morale));
	}

	public float GetMorale(bool recalc = true)
	{
		if (recalc)
		{
			RecalcPermanentMorale();
		}
		return Math.Max(0f, Math.Min(permanent_morale + temporary_morale.Get(), def.max_morale));
	}
}
