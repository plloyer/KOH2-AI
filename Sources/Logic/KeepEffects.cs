using System.Collections.Generic;

namespace Logic;

public class KeepEffects : Component
{
	public class Def : Logic.Def
	{
		public float refresh_interval;

		public float refresh_interval_rand;

		public float shoot_time = 3f;

		public float min_distance = 5f;

		public float max_distance = 10f;

		public float shoot_height = 6.5f;

		public float keep_attack_const = 20f;

		public float occupied_attack_const = 20f;

		public float resilience_base_recovery = 1f;

		public float resilience_rr_penalty = 0.25f;

		public float siege_defense_base_recovery = 1f;

		public float siege_defense_recover_per_production = 1f;

		public float siege_defense_recover_boosted = 1f;

		public float overtaken_keep_effects = 25f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			refresh_interval = field.GetFloat("refresh_interval", null, refresh_interval);
			refresh_interval_rand = field.GetFloat("refresh_interval_rand", null, refresh_interval_rand);
			shoot_time = field.GetFloat("shoot_time", null, shoot_time);
			min_distance = field.GetFloat("min_distance", null, min_distance);
			max_distance = field.GetFloat("max_distance", null, max_distance);
			shoot_height = field.GetFloat("shoot_height", null, shoot_height);
			keep_attack_const = field.GetFloat("keep_attack_const", null, keep_attack_const);
			occupied_attack_const = field.GetFloat("occupied_attack_const", null, occupied_attack_const);
			resilience_base_recovery = field.GetFloat("resilience_base_recovery", null, resilience_base_recovery);
			resilience_rr_penalty = field.GetFloat("resilience_rr_penalty", null, resilience_rr_penalty);
			siege_defense_base_recovery = field.GetFloat("siege_defense_base_recovery", null, siege_defense_base_recovery);
			siege_defense_recover_per_production = field.GetFloat("siege_defense_recover_per_production", null, siege_defense_recover_per_production);
			siege_defense_recover_boosted = field.GetFloat("siege_defense_recover_boosted", null, siege_defense_recover_per_production);
			overtaken_keep_effects = field.GetFloat("overtaken_keep_effects", null, overtaken_keep_effects);
			return true;
		}
	}

	public Def def;

	private Settlement keep;

	private Army cur_target;

	private Object controller;

	public bool active_defence_recovery_boost;

	public ComputableValue resilience_condition;

	public ComputableValue siege_defense_condition;

	private List<Army> in_range = new List<Army>();

	public KeepEffects(Settlement obj)
		: base(obj)
	{
		keep = obj;
		resilience_condition = new ComputableValue(100f, 0f, base.game, 0f, 100f);
		siege_defense_condition = new ComputableValue(100f, 0f, base.game, 0f, 100f);
		controller = keep.GetRealm()?.GetKingdom();
	}

	public bool CanBeTakenOver()
	{
		return keep != null;
	}

	public bool CanBeAssaulted()
	{
		if (keep != null)
		{
			return keep.type == "Castle";
		}
		return false;
	}

	public bool IsOccupied()
	{
		return !controller.IsOwnStance(keep.GetRealm().GetKingdom());
	}

	public bool SetOccupied(Object obj, bool force = false, bool send_state = true)
	{
		if (!CanBeTakenOver())
		{
			return false;
		}
		Realm realm = keep.GetRealm();
		if (realm == null)
		{
			return false;
		}
		Kingdom kingdom = controller.GetKingdom();
		Object obj2 = realm?.controller;
		Kingdom kingdom2 = realm.GetKingdom();
		if (obj == null)
		{
			obj = obj2;
		}
		Battle battle = keep.battle;
		if (obj != controller)
		{
			if (battle != null && battle.stage < Battle.Stage.Finishing && send_state)
			{
				return false;
			}
			Kingdom kingdom3 = obj?.GetKingdom();
			if (realm == null || kingdom == null || obj2 == null)
			{
				return false;
			}
			keep.ResetResources();
			keep.StartKeepOccupyCheck();
			if (IsOccupied() && kingdom != kingdom3 && keep.type == "Keep")
			{
				if (obj is Rebellion rebellion)
				{
					rebellion.occupiedKeeps.Remove(keep);
				}
				if (obj is Crusade crusade)
				{
					crusade.occupiedKeeps.Remove(keep);
				}
				else
				{
					kingdom.occupiedKeeps.Remove(keep);
				}
			}
			if (!force && !kingdom2.IsEnemy(obj))
			{
				if (kingdom3 != null && kingdom3 != kingdom2 && kingdom3.type == Kingdom.Type.Regular)
				{
					kingdom3.AddRelationModifier(kingdom2, "rel_battle_retook_keep", obj);
				}
				controller = kingdom2;
			}
			else
			{
				if (kingdom != kingdom3 && obj2 != kingdom3 && keep.type == "Keep")
				{
					if (obj is Rebellion rebellion2)
					{
						rebellion2.occupiedKeeps.Add(keep);
					}
					else if (obj is Crusade crusade2)
					{
						crusade2.occupiedKeeps.Add(keep);
					}
					else
					{
						kingdom3?.occupiedKeeps.Add(keep);
					}
				}
				controller = obj;
			}
			realm.NotifyListeners("settlement_controlling_obj_changed");
			keep.NotifyListeners("controlling_obj_changed");
			realm.castle.population.Recalc(send_state);
			if (send_state)
			{
				keep.SendState<Settlement.OccupiedState>();
			}
			return true;
		}
		return false;
	}

	public Object GetController()
	{
		return controller;
	}

	public override void OnStart()
	{
		def = base.game.defs.GetBase<Def>();
		UpdateAfter(def.refresh_interval + base.game.Random(0f, def.refresh_interval_rand));
	}

	public bool CanAttack()
	{
		Castle castle = keep as Castle;
		if (castle != null && castle.sacked)
		{
			return false;
		}
		float cTH = GetCTH();
		if (keep.level > 0 && (castle == null || !castle.sacked) && keep.battle == null)
		{
			return cTH > 0f;
		}
		return false;
	}

	public float GetCTH(Unit unit = null)
	{
		Realm realm = keep.GetRealm();
		if (def == null || realm == null)
		{
			return 0f;
		}
		float num = def.keep_attack_const * realm.GetStat(Stats.rs_attrition_damage);
		if (GetController() != realm.controller)
		{
			num = def.keep_attack_const * def.occupied_attack_const;
		}
		if (unit != null)
		{
			num = num * 100f / ((100f + unit.defense_modified()) * (float)unit.max_size_modified());
		}
		return num;
	}

	public override void OnUpdate()
	{
		if (keep.IsAuthority())
		{
			Realm realm = keep.GetRealm();
			UpdateSiegeRates();
			keep.GetKingdom();
			bool flag = realm.HasTag("CoastalGuns") || keep.IsOccupied();
			if (CanAttack())
			{
				if (cur_target != null)
				{
					Hit();
					UpdateAfter(def.refresh_interval + base.game.Random(0f, def.refresh_interval_rand) - def.shoot_time);
					return;
				}
				in_range.Clear();
				Army army = null;
				for (int i = 0; i < realm.neighbors.Count; i++)
				{
					Realm realm2 = realm.neighbors[i];
					if (!realm2.IsSeaRealm() || flag)
					{
						ValidateRealmTargets(realm2);
					}
				}
				ValidateRealmTargets(realm);
				if (in_range.Count > 0)
				{
					army = in_range[base.game.Random(0, in_range.Count)];
				}
				if (army != null && army.IsValid())
				{
					Shoot(army);
					UpdateAfter(def.shoot_time);
					return;
				}
			}
		}
		UpdateAfter(def.refresh_interval + base.game.Random(0f, def.refresh_interval_rand));
	}

	private void ValidateRealmTargets(Realm r)
	{
		for (int i = 0; i < r.armies.Count; i++)
		{
			Army army = r.armies[i];
			if (ValidateTarget(army))
			{
				in_range.Add(army);
			}
		}
	}

	public bool ValidateTarget(Army a)
	{
		if (a == null)
		{
			return false;
		}
		if (!a.IsEnemy(keep))
		{
			return false;
		}
		if (a.battle != null)
		{
			return false;
		}
		if (a.castle != null)
		{
			return false;
		}
		float num = keep.position.Dist(a.position);
		if (num < def.max_distance && num >= def.min_distance)
		{
			return true;
		}
		return false;
	}

	public void UpdateSiegeRates()
	{
		if (!keep.IsAuthority())
		{
			return;
		}
		Realm realm = keep.GetRealm();
		if (realm != null && keep.battle == null)
		{
			bool flag = false;
			float rate = resilience_condition.GetRate();
			float num = def.resilience_base_recovery + realm.GetTotalRebellionRisk() * def.resilience_rr_penalty;
			if (num != rate)
			{
				resilience_condition.SetRate(num);
				flag = true;
			}
			if (active_defence_recovery_boost && siege_defense_condition.Get() >= siege_defense_condition.GetMax())
			{
				realm.castle?.NotifyListeners("fortification_repair_boost_complete");
				active_defence_recovery_boost = false;
				keep.SendState<Castle.BoostCastleDefenceState>();
			}
			float rate2 = siege_defense_condition.GetRate();
			float num2 = def.siege_defense_base_recovery + realm.income.Get(ResourceType.Hammers) * def.siege_defense_recover_per_production;
			num2 *= (active_defence_recovery_boost ? def.siege_defense_recover_boosted : 1f);
			if (rate2 != num2)
			{
				siege_defense_condition.SetRate(num2);
				flag = true;
			}
			if (flag)
			{
				keep.SendState<Settlement.SiegeStatsState>();
			}
		}
	}

	private void Shoot(Army army)
	{
		cur_target = army;
		keep.FireEvent("attrition_shoot", cur_target);
	}

	private void Hit()
	{
		if (cur_target == null)
		{
			return;
		}
		if (!cur_target.IsValid() || cur_target.leader == null || !ValidateTarget(cur_target))
		{
			cur_target = null;
			return;
		}
		if (cur_target.units.Count <= 1)
		{
			Character leader = cur_target.leader;
			float num = base.game.Random(0, 100);
			float stat = cur_target.leader.GetStat(Stats.cs_imprison_in_battle_chance);
			if (num <= stat)
			{
				Kingdom kingdom = keep.GetController()?.GetKingdom();
				War war = kingdom.FindWarWith(cur_target.GetKingdom());
				war?.AddActivity("KnightNeutralized", kingdom, leader.GetKingdom(), null, Battle.WarScoreKnightNeutralized(war.def, leader));
				if (kingdom != null && kingdom.type == Kingdom.Type.Regular)
				{
					cur_target.SetLeader(null);
					leader.Imprison(kingdom);
					leader.NotifyListeners("imprisoned_in_battle", keep);
				}
			}
			leader?.NotifyListeners("fatal_attrition_damage", keep);
			cur_target.Destroy();
			return;
		}
		Unit unit = cur_target.units[base.game.Random(1, cur_target.units.Count - 1)];
		float cTH = GetCTH(unit);
		if ((float)base.game.Random(0, 100) < cTH)
		{
			unit.SetDamage(unit.damage + 1f / (float)unit.def.health_segments);
			if (unit.BelowMinTroops() || unit.damage >= 1f)
			{
				cur_target.DelUnit(unit);
			}
		}
		cur_target.rebel?.SetTarget(keep);
		cur_target = null;
	}
}
