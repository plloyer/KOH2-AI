using System;
using System.Collections.Generic;

namespace Logic;

public class MoveTradeCenterAction : Action, IListener
{
	private static Queue<Realm> tmp_queue = new Queue<Realm>();

	private Realm temp_realm;

	public MoveTradeCenterAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new MoveTradeCenterAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.GetClass().name != "Merchant")
		{
			return "not_a_merchant";
		}
		if (character.GetKingdom().royalFamily.Sovereign != character)
		{
			return "not_a_king";
		}
		Realm realm = base.target as Realm;
		Realm realm2 = null;
		if (args != null && args.Count > 0)
		{
			realm2 = args[0].Get<Realm>();
		}
		if (state != State.Running)
		{
			if (realm != null && realm2 != null)
			{
				if (realm.kingdom_id != character.kingdom_id || realm2.kingdom_id != character.kingdom_id)
				{
					return "realms_no_longer_own";
				}
				if (realm.tradeCenter != null)
				{
					int min_realm_distance_between = realm.tradeCenter.def.min_realm_distance_between;
					if (realmNearTradeCentre(realm2, min_realm_distance_between - 1, realm))
					{
						return "target_realm_too_close_to_another_trade_center";
					}
				}
			}
			else if (realm != null)
			{
				if (GetPossibleArgs().Length == 0)
				{
					return "no_valid_destination";
				}
			}
			else if (GetPossibleTargets().Count == 0)
			{
				return "no_valid_trade_centres";
			}
		}
		return "ok";
	}

	public override void Prepare()
	{
		if (base.target != null && args != null && args.Count >= 1 && !(args[0] == Value.Null))
		{
			Kingdom kingdom = base.own_character.GetKingdom();
			Realm realm = base.target as Realm;
			Realm realm2 = args[0].Get<Realm>();
			Resource cost = GetCost(realm2);
			if (kingdom.resources.CanAfford(cost, 1f))
			{
				kingdom.SubResources(KingdomAI.Expense.Category.Economy, cost);
				realm.AddListener(this);
				realm2.AddListener(this);
				base.Prepare();
			}
			else
			{
				Cancel();
			}
		}
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Object> targets = new List<Object>();
		foreach (Realm tradeCenterRealm in base.game.economy.tradeCenterRealms)
		{
			if (tradeCenterRealm != null && base.own_character.GetKingdom() == tradeCenterRealm.GetKingdom())
			{
				AddTarget(ref targets, tradeCenterRealm);
			}
		}
		return targets;
	}

	public bool realmNearTradeCentre(Realm r, int withinDistance, Realm exludeTC)
	{
		for (int i = 0; i < base.game.economy.tradeCenterRealms.Count; i++)
		{
			Realm realm = base.game.economy.tradeCenterRealms[i].castle.GetRealm();
			if (realm != exludeTC && base.game.RealmDistance(realm.id, r.id, goThroughSeas: true, useLogicNeighbors: false, withinDistance) != -1)
			{
				return true;
			}
		}
		return false;
	}

	public override List<Value>[] GetPossibleArgs()
	{
		if (base.target == null)
		{
			return null;
		}
		Realm realm = base.target as Realm;
		List<Value>[] array = new List<Value>[1]
		{
			new List<Value>()
		};
		int min_realm_distance_between = realm.tradeCenter.def.min_realm_distance_between;
		int max_move_distance = realm.tradeCenter.def.max_move_distance;
		for (int i = 0; i < base.game.realms.Count; i++)
		{
			base.game.realms[i].wave_depth = -1;
		}
		tmp_queue.Clear();
		tmp_queue.Enqueue(realm);
		realm.wave_depth = 0;
		while (tmp_queue.Count > 0)
		{
			Realm realm2 = tmp_queue.Dequeue();
			int num = realm2.wave_depth + 1;
			if (num > max_move_distance)
			{
				continue;
			}
			foreach (Realm neighbor in realm2.neighbors)
			{
				if (neighbor != null && neighbor.wave_depth == -1)
				{
					neighbor.wave_depth = num;
					tmp_queue.Enqueue(neighbor);
					if (neighbor.IsValid() && !neighbor.IsSeaRealm() && neighbor.kingdom_id == base.own_character.kingdom_id && !realmNearTradeCentre(neighbor, min_realm_distance_between - 1, realm))
					{
						AddArg(ref array[0], neighbor, 0);
					}
				}
			}
		}
		if (array[0].Count == 0)
		{
			return null;
		}
		return array;
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (base.target is Realm)
		{
			List<Vars> list = new List<Vars>(possibleTargets.Count);
			if (possibleTargets == null)
			{
				return null;
			}
			{
				foreach (Value possibleTarget in possibleTargets)
				{
					temp_realm = possibleTarget.Get<Realm>();
					Resource resource = GetCost(temp_realm);
					if (resource == null)
					{
						resource = new Resource();
					}
					resource[ResourceType.Gold] = (int)Math.Round(resource[ResourceType.Gold]);
					Vars vars = new Vars();
					vars.Set("cost", resource);
					vars.Set("available", base.own_character.GetKingdom().resources);
					vars.Set("rightTextKey", "Action.costText");
					list.Add(vars);
				}
				return list;
			}
		}
		return null;
	}

	public override void Run()
	{
		Realm realm = base.target as Realm;
		Realm realm2 = args[0].Get<Realm>();
		if (base.own_character != null && realm != null && realm2 != null)
		{
			TradeCenter tradeCenter = realm.tradeCenter;
			if (tradeCenter == null)
			{
				base.game.Log("No trade center in " + realm.name);
				return;
			}
			tradeCenter.MoveTo(realm2);
			base.Run();
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "distance")
		{
			if (temp_realm == null)
			{
				if (args == null)
				{
					return Value.Unknown;
				}
				temp_realm = args[0].Get<Realm>();
			}
			return base.game.RealmDistance((base.target as Realm).id, temp_realm.id);
		}
		return base.GetVar(key, vars, as_value);
	}

	public override void OnMessage(object obj, string message, object param)
	{
		if (message == "trade_center_changed" || message == "kingdom_changed")
		{
			if (Validate() != "ok")
			{
				Cancel();
			}
		}
		else
		{
			base.OnMessage(obj, message, param);
		}
	}
}
