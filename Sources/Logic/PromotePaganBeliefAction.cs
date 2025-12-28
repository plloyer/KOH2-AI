using System.Collections.Generic;

namespace Logic;

public class PromotePaganBeliefAction : Action, IListener
{
	public PromotePaganBeliefAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PromotePaganBeliefAction(owner as Character, def);
	}

	public override string ValidateIdle()
	{
		return base.ValidateIdle();
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value>[] array = new List<Value>[1];
		List<Religion.PaganBelief> pagan_beliefs = base.game.religions.pagan.def.pagan_beliefs;
		List<Value> list = null;
		for (int i = 0; i < pagan_beliefs.Count; i++)
		{
			Religion.PaganBelief paganBelief = pagan_beliefs[i];
			if (own_kingdom.pagan_beliefs.Contains(paganBelief))
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < own_kingdom.court.Count; j++)
			{
				Character character = own_kingdom.court[j];
				if (character == null || character == base.own_character || !character.IsCleric() || character?.actions?.active == null)
				{
					continue;
				}
				for (int k = 0; k < character.actions.active.Count; k++)
				{
					Action action = character.actions.active[k];
					if (action.args != null && action is PromotePaganBeliefAction && action.GetArg(0, null) == (Value)paganBelief.name)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				AddArg(ref list, paganBelief.name, 0);
			}
		}
		array[0] = list;
		return array;
	}

	public override List<Vars> GetPossibleArgVars(List<Value> possibleTargets = null, int arg_type = 0)
	{
		if (possibleTargets == null)
		{
			return null;
		}
		Kingdom kingdom = own_kingdom;
		if (kingdom == null)
		{
			return null;
		}
		List<Vars> list = new List<Vars>(possibleTargets.Count);
		foreach (Value possibleTarget in possibleTargets)
		{
			Vars vars = new Vars(possibleTarget);
			vars.Set("argument_type", "PaganBeliefs");
			vars.Set("owner", base.own_character);
			vars.Set("belief", string.Concat("Pagan.", possibleTarget, ".name"));
			vars.Set("kingdom", kingdom);
			vars.Set("tooltip_def", "PaganBeliefTooltip");
			list.Add(vars);
		}
		return list;
	}

	public override void OnEnterState(bool send_state = true)
	{
		if (state == State.Preparing)
		{
			own_kingdom.NotifyListeners("promote_pagan_belief_started");
		}
		base.OnEnterState(send_state);
	}

	public virtual int NumPaganBeliefs()
	{
		int num = own_kingdom.pagan_beliefs.Count;
		for (int i = 0; i < own_kingdom.court.Count; i++)
		{
			Character character = own_kingdom.court[i];
			if (character == null || character == base.own_character || !character.IsCleric())
			{
				continue;
			}
			List<Action> list = character.actions?.active;
			if (list == null)
			{
				continue;
			}
			for (int j = 0; j < list.Count; j++)
			{
				Action action = list[j];
				if (action?.args != null && action is PromotePaganBeliefAction)
				{
					num++;
				}
			}
		}
		return num;
	}

	public override string Validate(bool quick_out = false)
	{
		Kingdom kingdom = own_kingdom;
		if (base.own_character == null)
		{
			return "not_a_character";
		}
		if (!kingdom.is_pagan)
		{
			return "not_pagan";
		}
		if (NumPaganBeliefs() >= base.game.religions.pagan.def.max_pagan_beliefs)
		{
			return "_too_many_beliefs";
		}
		if (base.own_character.paganBelief != null && !(this is ChangePaganBeliefAction))
		{
			return "already_promoting";
		}
		return base.Validate(quick_out);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("new_belief", string.Concat("Pagan.", args[0], ".name"));
	}

	public override void Run()
	{
		string beliefName = args[0];
		base.own_character.PromotePaganBelief(beliefName, apply_penalties: false, notify: false);
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (!(key == "current_upkeep"))
		{
			if (key == "next_upkeep")
			{
				return base.own_character.game.religions.pagan.def.CalcPaganBliefsUpkeep(own_kingdom, own_kingdom.pagan_beliefs.Count + 1);
			}
			return base.GetVar(key, vars, as_value);
		}
		float num = base.own_character.game.religions.pagan.def.CalcPaganBliefsUpkeep(own_kingdom);
		if (num != 0f)
		{
			return new Value(num);
		}
		return Value.Null;
	}
}
