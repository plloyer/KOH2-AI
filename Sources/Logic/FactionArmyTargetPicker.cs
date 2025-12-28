using System;
using System.Collections.Generic;

namespace Logic;

public abstract class FactionArmyTargetPicker
{
	public struct Target
	{
		public MapObject mapObject;

		public float attractiveness;

		public float safety_level;

		public override string ToString()
		{
			return mapObject?.ToString() + " attractivness: " + attractiveness + "threat level: " + safety_level;
		}

		public static bool CalcTargetAttractiveness(Rebel rebel, MapObject target, out float result)
		{
			if (rebel?.army == null || target == null)
			{
				result = 0f;
				return false;
			}
			float num = target.position.Dist(rebel.army.position);
			result = rebel.def.target_pick_distance / (rebel.def.target_pick_distance + num);
			if (target is Battle battle)
			{
				result *= ((!battle.is_plunder || battle.defenders.Count != 0) ? 1 : 0);
				result *= ((!battle.attackers.Contains(rebel.rebellion.leader.army) && !battle.defenders.Contains(rebel.rebellion.leader.army)) ? 1 : 100);
				if (battle.settlement != null)
				{
					bool flag = battle.settlement.IsOwnStance(rebel.army);
					string type = battle.settlement.type;
					if (!(type == "Castle"))
					{
						if (type == "Keep")
						{
							if (flag)
							{
								result *= rebel.def.target_pick_defend_keep;
							}
							else
							{
								result *= rebel.def.target_pick_attack_keep;
							}
						}
						else if (flag)
						{
							result *= rebel.def.target_pick_defend_plunder;
						}
						else
						{
							result *= rebel.def.target_pick_attack_plunder;
						}
					}
					else if (flag)
					{
						result *= rebel.def.target_pick_defend_town;
					}
					else
					{
						result *= rebel.def.target_pick_attack_town;
					}
				}
			}
			else if (target is Settlement settlement)
			{
				result *= ((!settlement.razed) ? 1 : 0);
				result *= ((!settlement.IsOwnStance(rebel)) ? 1 : 0);
				string type = settlement.type;
				if (!(type == "Castle"))
				{
					if (type == "Keep")
					{
						result *= rebel.def.target_pick_attack_keep;
					}
					else
					{
						result *= rebel.def.target_pick_attack_plunder;
					}
				}
				else
				{
					result *= rebel.def.target_pick_attack_town;
				}
				if (result > 0f)
				{
					Realm realm = settlement.GetRealm();
					for (int i = 0; i < realm.armies.Count; i++)
					{
						Army army = realm.armies[i];
						if (army != null && army.IsValid())
						{
							Rebel rebel2 = army.rebel;
							if (rebel2 != null && rebel2 != rebel && rebel2.current_target_object == settlement)
							{
								result *= 0.5f;
								break;
							}
						}
					}
				}
			}
			return true;
		}

		public static bool CalcTargetAttractiveness(Mercenary merc, MapObject target, out float result)
		{
			if (merc?.army == null || target == null)
			{
				result = 0f;
				return false;
			}
			float num = target.position.Dist(merc.army.position);
			result = merc.def.target_pick_distance / (merc.def.target_pick_distance + num);
			if (target is Army army)
			{
				result *= merc.def.target_pick_attack_army;
				float num2 = army.GetManPower();
				float num3 = merc.army.GetManPower();
				result *= Math.Min(num3 / num2 + num3, merc.def.target_pick_attack_army_manpower_ratio_max);
			}
			else if (target is Settlement settlement)
			{
				result *= ((!settlement.razed) ? 1 : 0);
				string type = settlement.type;
				if (!(type == "Castle"))
				{
					if (type == "Keep")
					{
						result *= merc.def.target_pick_attack_keep;
					}
					else
					{
						result *= merc.def.target_pick_attack_plunder;
					}
				}
				else
				{
					result *= merc.def.target_pick_attack_town;
				}
			}
			else if (target is Battle battle)
			{
				if (battle.settlement != null)
				{
					bool flag = battle.settlement.IsOwnStance(merc.army);
					string type = battle.settlement.type;
					if (!(type == "Castle"))
					{
						if (type == "Keep")
						{
							if (flag)
							{
								result *= merc.def.target_pick_defend_keep;
							}
							else
							{
								result *= merc.def.target_pick_attack_keep;
							}
						}
						else if (flag)
						{
							result *= merc.def.target_pick_defend_plunder;
						}
						else
						{
							result *= merc.def.target_pick_attack_plunder;
						}
					}
					else if (flag)
					{
						result *= merc.def.target_pick_defend_town;
					}
					else
					{
						result *= merc.def.target_pick_attack_town;
					}
				}
				Kingdom kingdom = merc.army.GetKingdom();
				result *= ((battle.attacker_kingdom == kingdom || battle.defender_kingdom == kingdom) ? merc.def.target_pick_help_own_kingdom_in_battle : 1f);
			}
			return true;
		}

		public static bool CalcTargetThreatLevel(Army army, int my_eval, MapObject target, out float result)
		{
			result = 1f;
			if (army == null)
			{
				return false;
			}
			int num = 0;
			if (target is Army army2)
			{
				num = army2.EvalStrength();
			}
			else if (target is Settlement settlement)
			{
				num = settlement.EvalStrength();
			}
			else if (target is Battle battle)
			{
				int joinSide = battle.GetJoinSide(army);
				if (joinSide >= 0 && joinSide < 2)
				{
					List<Army> armies = battle.GetArmies(joinSide);
					for (int i = 0; i < armies.Count; i++)
					{
						my_eval += armies[i].EvalStrength();
					}
					List<Army> armies2 = battle.GetArmies(1 - joinSide);
					for (int j = 0; j < armies2.Count; j++)
					{
						num += armies2[j].EvalStrength();
					}
					if (battle.settlement != null)
					{
						int num2 = battle.settlement.EvalStrength();
						if (joinSide == 0)
						{
							num += num2;
						}
						else
						{
							my_eval += num2;
						}
					}
				}
			}
			float num3 = my_eval + num;
			if (Math.Abs(num3) <= 0.0001f)
			{
				result = 1f;
			}
			else
			{
				result = (float)my_eval / num3;
			}
			return true;
		}
	}

	public static void CalcThreatLevel(Army army, Target[] targets)
	{
		int my_eval = army.EvalStrength();
		for (int i = 0; i < targets.Length; i++)
		{
			Target target = targets[i];
			Target.CalcTargetThreatLevel(army, my_eval, target.mapObject, out var result);
			target.safety_level = result;
			targets[i] = target;
		}
	}

	public static bool ChooseTarget(Army army, Target[] targets, out Target t)
	{
		if (targets == null || targets.Length == 0)
		{
			t = default(Target);
			return false;
		}
		float num = float.MinValue;
		int num2 = 0;
		for (int i = 0; i < targets.Length; i++)
		{
			Target target = targets[i];
			float num3 = target.attractiveness * target.safety_level;
			if (num3 > num)
			{
				num = num3;
				num2 = i;
			}
		}
		t = targets[num2];
		return true;
	}

	public static bool ValidateTarget(Army army, Object target)
	{
		if (target == null || !target.IsValid() || army == target)
		{
			return false;
		}
		IRelationCheck stanceObj = target.GetStanceObj();
		if (stanceObj == null)
		{
			return false;
		}
		if (target is Army)
		{
			Army army2 = target as Army;
			if (!stanceObj.IsEnemy(army))
			{
				return false;
			}
			if (army2.castle != null)
			{
				return false;
			}
			if (army2.battle != null)
			{
				return false;
			}
		}
		else if (target is Settlement)
		{
			Settlement settlement = target as Settlement;
			if (!stanceObj.IsEnemy(army))
			{
				return false;
			}
			if (!settlement.IsActiveSettlement())
			{
				return false;
			}
			if (settlement.battle != null)
			{
				return false;
			}
			if (settlement.razed)
			{
				return false;
			}
		}
		else if (target is Battle battle && !battle.CanJoin(army))
		{
			return false;
		}
		return true;
	}

	public static bool CheckIfReinforcement(Army army)
	{
		if (army.movement.IsMoving() && army.movement.ResolveFinalDestination(army.movement.path?.dst_obj) is Battle battle && battle.IsValid())
		{
			int joinSide = battle.GetJoinSide(army);
			if (joinSide >= 0 && joinSide < 2 && battle.IsReinforcement(army) && battle.GetSideKingdom(joinSide).is_player)
			{
				return true;
			}
		}
		return false;
	}
}
