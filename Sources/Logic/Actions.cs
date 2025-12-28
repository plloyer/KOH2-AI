using System;
using System.Collections.Generic;

namespace Logic;

public class Actions : Component, IListener
{
	public List<Action> all;

	public List<Action> active;

	private Action _current;

	public List<Opportunity> opportunities;

	public Time tm_last_changed = Time.Zero;

	public Action current
	{
		get
		{
			return _current;
		}
		set
		{
			Action param = _current;
			_current = value;
			obj?.NotifyListeners("cur_action_changed", param);
		}
	}

	public int Count
	{
		get
		{
			if (all != null)
			{
				return all.Count;
			}
			return 0;
		}
	}

	public Action this[int i] => all[i];

	public Actions(Object obj)
		: base(obj)
	{
		obj.AddListener(this);
	}

	public Action Find(string id)
	{
		if (all == null)
		{
			return null;
		}
		for (int i = 0; i < all.Count; i++)
		{
			Action action = all[i];
			if (action.def.id == id)
			{
				return action;
			}
		}
		return null;
	}

	public Action Find(Action.Def def)
	{
		if (all == null)
		{
			return null;
		}
		for (int i = 0; i < all.Count; i++)
		{
			Action action = all[i];
			if (action.def == def)
			{
				return action;
			}
		}
		return null;
	}

	public Action FindActive(Action.Def def)
	{
		if (active == null)
		{
			return null;
		}
		for (int i = 0; i < active.Count; i++)
		{
			Action action = active[i];
			if (action?.def == def)
			{
				return action;
			}
		}
		return null;
	}

	public Action Add(Action.Def def)
	{
		if (def.owner_type != null && !def.owner_type.IsAssignableFrom(obj.rtti.type))
		{
			obj.Error("Attempting to add " + def.owner_type.Name + " action '" + def.id + "' to " + obj.GetType().Name);
			return null;
		}
		if (Find(def) != null)
		{
			return null;
		}
		Action action = Action.Create(obj, def);
		if (action == null)
		{
			return null;
		}
		if (all == null)
		{
			all = new List<Action>();
		}
		all.Add(action);
		return action;
	}

	public void AddAll()
	{
		if (all != null)
		{
			obj.Error("Actions.AddAll() called after some actions were already added");
			return;
		}
		Type type = obj.rtti.type;
		Defs.Registry registry = base.game.defs.Get(typeof(Action.Def));
		if (registry == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Def> def2 in registry.defs)
		{
			if (!(def2.Value is Action.Def def) || def.owner_type == null || !def.owner_type.IsAssignableFrom(type))
			{
				continue;
			}
			Action action = Action.Create(obj, def);
			if (action != null)
			{
				if (all == null)
				{
					all = new List<Action>();
				}
				all.Add(action);
			}
		}
	}

	public void DestroyAll()
	{
		opportunities = null;
		StopUpdating();
		if (all != null)
		{
			for (int i = 0; i < all.Count; i++)
			{
				all[i]?.Destroy();
			}
			all = null;
		}
	}

	public override void OnDestroy()
	{
		DestroyAll();
		base.OnDestroy();
	}

	public void StateChanged(Action action)
	{
		if (action.is_active)
		{
			if (active != null && active.Contains(action))
			{
				return;
			}
			if (active == null)
			{
				active = new List<Action>();
			}
			active.Add(action);
		}
		else if (active == null || !active.Remove(action))
		{
			return;
		}
		if (action.def != null && action.def.invalidate_incomes)
		{
			action.own_kingdom?.InvalidateIncomes();
		}
		obj?.SendState<Object.ActionsState>();
	}

	public void CheckActiveOpportunities(bool forced = false)
	{
		if (opportunities == null)
		{
			return;
		}
		bool flag = false;
		for (int num = opportunities.Count - 1; num >= 0; num--)
		{
			Opportunity opportunity = opportunities[num];
			if (forced || !opportunity.forced)
			{
				if (!opportunity.active)
				{
					if (opportunity.action == null)
					{
						opportunities.RemoveAt(num);
						flag = true;
					}
					float num2 = opportunity.def.Cooldown(opportunity);
					if (opportunity.last_time + num2 <= base.game.time)
					{
						opportunities.RemoveAt(num);
						flag = true;
					}
				}
				else if (ShouldDeactivate(opportunity) || !Action.ShouldBeVisible(opportunity.Validate()))
				{
					flag = true;
					opportunity.active = false;
					if (!forced)
					{
						obj.FireEvent("opportunity_lost", opportunity, obj.GetKingdom().id);
					}
					if ((forced ? 0f : opportunity.def.Cooldown(opportunity)) > 0f)
					{
						opportunity.last_time = base.game.time;
					}
					else
					{
						opportunities.RemoveAt(num);
					}
					tm_last_changed = base.game.time;
				}
			}
		}
		if (flag)
		{
			obj.SendState<Object.ActionsState>();
		}
	}

	public bool ShouldDeactivate(Opportunity opportunity)
	{
		Opportunity.Def def = opportunity.def;
		if (def == null)
		{
			return true;
		}
		if (def.min_active_time > 0f && opportunity.last_time + def.min_active_time > base.game.time)
		{
			return false;
		}
		float num = def.ChanceToDelOnTick(opportunity);
		if (num >= 100f)
		{
			return true;
		}
		if (num > 0f && base.game.Random(0f, 100f) <= num)
		{
			return true;
		}
		return false;
	}

	public void AddPermanentOpportunities()
	{
		if (all == null || !(obj is Character character) || !character.IsSpy() || character.mission_kingdom == null || !character.IsAuthority())
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < all.Count; i++)
		{
			Action action = all[i];
			if (action.def.opportunity != null && action.def.opportunity.IsPermanent() && TryActivateOpportunity(action, forced: false, silent: true, send_state: false) != null)
			{
				flag = true;
			}
		}
		if (flag)
		{
			obj.SendState<Object.ActionsState>();
		}
	}

	public void CheckForNewRumors()
	{
		if (!(obj is Character character) || !character.IsSpy() || character.mission_kingdom == null)
		{
			return;
		}
		Kingdom kingdom = character.GetKingdom();
		if (kingdom == null || !kingdom.is_player)
		{
			return;
		}
		Opportunity.ClassDef classDef = character.class_def.opportunities;
		if (classDef != null && (!(tm_last_changed != Time.Zero) || !(tm_last_changed + classDef.min_time_before_new_opportunity > base.game.time)))
		{
			List<Rumor> list = Rumor.DecideNewRumors(character);
			if (list != null)
			{
				Rumor.SpreadRumors(list);
				tm_last_changed = base.game.time;
				obj.SendState<Object.ActionsState>();
			}
		}
	}

	public void CheckForNewOpportunities()
	{
		if (all == null)
		{
			return;
		}
		Kingdom kingdom = obj?.GetKingdom();
		if (kingdom == null || kingdom.IsDefeated())
		{
			return;
		}
		Opportunity.ClassDef classDef = (obj as Character)?.class_def?.opportunities;
		if (classDef == null || (classDef.max_count > 0 && NumActiveOpportunities() >= classDef.max_count) || (tm_last_changed != Time.Zero && tm_last_changed + classDef.min_time_before_new_opportunity > base.game.time))
		{
			return;
		}
		List<Opportunity> list = null;
		for (int i = 0; i < all.Count; i++)
		{
			Action action = all[i];
			if (action.def.opportunity == null)
			{
				continue;
			}
			Opportunity opportunity = TryCreateOpportunity(action);
			if (opportunity != null)
			{
				if (list == null)
				{
					list = new List<Opportunity>();
				}
				list.Add(opportunity);
			}
		}
		if (list != null && list.Count != 0)
		{
			int index = base.game.Random(0, list.Count);
			Opportunity opportunity2 = list[index];
			ActivateOpportunity(opportunity2);
		}
	}

	public Opportunity FindOpportunity(Action action, Object target)
	{
		if (opportunities == null || action == null)
		{
			return null;
		}
		for (int i = 0; i < opportunities.Count; i++)
		{
			Opportunity opportunity = opportunities[i];
			if (opportunity.action == action && opportunity.target == target)
			{
				return opportunity;
			}
		}
		return null;
	}

	public bool DelOpportunity(Action action, Object target, List<Value> args, bool forget = false)
	{
		if (opportunities == null || action == null)
		{
			return false;
		}
		for (int i = 0; i < opportunities.Count; i++)
		{
			Opportunity opportunity = opportunities[i];
			if (MatchOpportunity(opportunity, action, target, args))
			{
				opportunity.active = false;
				float num = opportunity.def.Cooldown(opportunity);
				if (!forget && num > 0f)
				{
					opportunity.last_time = base.game.time;
				}
				else
				{
					opportunities.RemoveAt(i);
				}
				tm_last_changed = base.game.time;
				obj.SendState<Object.ActionsState>();
				obj.NotifyListeners("opportunities_changed");
				return true;
			}
		}
		return false;
	}

	private bool MatchOpportunity(Opportunity opportunity, Action action, Object target, List<Value> args)
	{
		if (opportunity == null)
		{
			return false;
		}
		if (action == null)
		{
			return false;
		}
		if (opportunity.action != action || opportunity.target != target)
		{
			return false;
		}
		if (args == opportunity.args)
		{
			return true;
		}
		if (args == null && opportunity.args != null)
		{
			return false;
		}
		if (args != null && opportunity.args == null)
		{
			return false;
		}
		if (args.Count != opportunity.args.Count)
		{
			return false;
		}
		int i = 0;
		for (int count = args.Count; i < count; i++)
		{
			if (args[i] != opportunity.args[i])
			{
				return false;
			}
		}
		return true;
	}

	public int NumOpportunities(Action action)
	{
		if (opportunities == null || action == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < opportunities.Count; i++)
		{
			if (opportunities[i].action == action)
			{
				num++;
			}
		}
		return num;
	}

	public int NumActiveOpportunities()
	{
		if (opportunities == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < opportunities.Count; i++)
		{
			if (opportunities[i].active)
			{
				num++;
			}
		}
		return num;
	}

	public bool DecideArgs(Action action, Object target, out List<Value> args)
	{
		args = null;
		Object target2 = action.target;
		action.target = target;
		if (!action.NeedsArgs())
		{
			action.target = target2;
			return true;
		}
		List<Value>[] possibleArgs = action.GetPossibleArgs();
		action.target = target2;
		if (possibleArgs == null)
		{
			return false;
		}
		foreach (List<Value> list in possibleArgs)
		{
			if (list == null || list.Count == 0)
			{
				return false;
			}
		}
		args = new List<Value>();
		foreach (List<Value> list2 in possibleArgs)
		{
			int index = base.game.Random(0, list2.Count);
			Value item = list2[index];
			args.Add(item);
		}
		return true;
	}

	public Opportunity TryCreateOpportunity(Action action, bool forced = false, List<Object> possible_targets = null)
	{
		Opportunity.Def opportunity = action.def.opportunity;
		if (opportunity == null)
		{
			return null;
		}
		Opportunity.ClassDef classDef = (base.obj as Character).class_def?.opportunities;
		if (classDef == null || (classDef.max_count > 0 && NumActiveOpportunities() >= classDef.max_count))
		{
			return null;
		}
		if (opportunity.max_targets > 0 && NumOpportunities(action) >= opportunity.max_targets)
		{
			return null;
		}
		if (!forced && !Action.ShouldBeVisible(action.Validate()))
		{
			return null;
		}
		Object obj = null;
		Opportunity opportunity2 = null;
		if (action.NeedsTarget())
		{
			List<Object> list = possible_targets ?? action.GetPossibleTargets();
			if (list == null || list.Count == 0)
			{
				return null;
			}
			int num = base.game.Random(0, list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				Object obj2 = list[(num + i) % list.Count];
				opportunity2 = FindOpportunity(action, obj2);
				if (opportunity2 == null || (forced && !opportunity2.active))
				{
					if (action.ValidateTarget(obj2))
					{
						obj = obj2;
						break;
					}
					opportunity2 = null;
				}
			}
			if (obj == null)
			{
				return null;
			}
		}
		else
		{
			opportunity2 = FindOpportunity(action, null);
			if (opportunity2 != null && (!forced || opportunity2.active))
			{
				return null;
			}
		}
		if (action.is_active && action.target == obj)
		{
			return null;
		}
		if (!DecideArgs(action, obj, out var args))
		{
			return null;
		}
		if (!forced)
		{
			float num2 = 0f;
			using (new Opportunity.TempActionArgs(action, obj, args))
			{
				num2 = opportunity.ChanceToAddOnTick(action, action.own_kingdom != null && action.own_kingdom.is_player);
			}
			if (num2 <= 0f)
			{
				return null;
			}
			if (num2 < 100f && base.game.Random(0f, 100f) > num2)
			{
				return null;
			}
		}
		return new Opportunity
		{
			action = action,
			target = obj,
			args = args,
			forced = forced
		};
	}

	public bool ActivateOpportunity(Opportunity opportunity, bool silent = false, bool send_state = true)
	{
		if (opportunity == null)
		{
			return false;
		}
		Opportunity opportunity2 = FindOpportunity(opportunity.action, opportunity.target);
		if (opportunity2 != null)
		{
			opportunities.Remove(opportunity2);
		}
		opportunity.active = true;
		opportunity.last_time = base.game.time;
		if (opportunities == null)
		{
			opportunities = new List<Opportunity>();
		}
		opportunities.Add(opportunity);
		tm_last_changed = base.game.time;
		if (send_state)
		{
			obj.SendState<Object.ActionsState>();
		}
		if (!silent)
		{
			if (send_state)
			{
				obj.FireEvent("new_opportunity", opportunity, obj.GetKingdom().id);
			}
			else
			{
				obj.NotifyListeners("new_opportunity", opportunity);
			}
		}
		return true;
	}

	public Opportunity TryActivateOpportunity(Action action, bool forced = false, bool silent = false, bool send_state = true, List<Object> possible_targets = null)
	{
		Opportunity opportunity = TryCreateOpportunity(action, forced, possible_targets);
		if (!ActivateOpportunity(opportunity, silent, send_state))
		{
			return null;
		}
		return opportunity;
	}

	private bool NeedsUpdate()
	{
		if (opportunities != null && opportunities.Count > 0)
		{
			return true;
		}
		if (MayHaveOpportunities())
		{
			return true;
		}
		return false;
	}

	public bool MayHaveOpportunities()
	{
		if (all == null)
		{
			return false;
		}
		for (int i = 0; i < all.Count; i++)
		{
			Action action = all[i];
			if (action.def.opportunity != null)
			{
				if (action.def.mission == Action.Def.MissionType.NotOnMission || action.def.mission == Action.Def.MissionType.Ignore)
				{
					return true;
				}
				if (action.own_character?.mission_kingdom != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void RescheduleOpportunities()
	{
		if (!NeedsUpdate())
		{
			StopUpdating();
			return;
		}
		Opportunity.ClassDef classDef = (obj as Character)?.class_def?.opportunities;
		if (classDef == null)
		{
			StopUpdating();
			return;
		}
		float seconds = base.game.Random(classDef.min_tick, classDef.max_tick);
		UpdateAfter(seconds);
	}

	public override void OnUpdate()
	{
		if (obj.IsAuthority())
		{
			CheckActiveOpportunities();
			CheckForNewRumors();
			CheckForNewOpportunities();
		}
		RescheduleOpportunities();
	}

	public void OnMessage(object obj, string message, object param)
	{
		if (message == "mission_kingdom_changed" && (base.obj.IsAuthority() || Game.isLoadingSaveGame))
		{
			CheckActiveOpportunities(forced: true);
			AddPermanentOpportunities();
			RescheduleOpportunities();
		}
	}
}
