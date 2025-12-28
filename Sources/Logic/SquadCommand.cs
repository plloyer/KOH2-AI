using System.Collections.Generic;

namespace Logic;

public abstract class SquadCommand
{
	public List<Squad> squads = new List<Squad>();

	public List<MapObject> targets = new List<MapObject>();

	public BattleAI ai;

	public float cached_priority;

	public bool adding_squad_lowers_priority;

	public bool ignore_combat_when_changing_decision;

	public MapObject target
	{
		get
		{
			if (targets.Count > 0)
			{
				return targets[0];
			}
			return null;
		}
		set
		{
			if (targets.Count == 0)
			{
				targets.Add(value);
			}
			else
			{
				targets[0] = value;
			}
		}
	}

	public override string ToString()
	{
		return base.ToString() + target;
	}

	public SquadCommand(BattleAI ai, MapObject target = null)
	{
		this.ai = ai;
		this.target = target;
	}

	public virtual bool SingleSquad()
	{
		return false;
	}

	public virtual void Reset()
	{
		for (int i = 0; i < squads.Count; i++)
		{
			squads[i].ai_command = null;
		}
		squads.Clear();
	}

	public virtual bool AddSquad(Squad squad)
	{
		if (!Validate(squad))
		{
			RemoveSquad(squad);
			return false;
		}
		if (SingleSquad() && squads.Count > 0)
		{
			return false;
		}
		if (squad.ai_command == this)
		{
			return true;
		}
		if (cached_priority < 0f)
		{
			return false;
		}
		if (squad.ai_command != null)
		{
			if (MaintainDecision(squad))
			{
				return false;
			}
			squad.ai_command.RemoveSquad(squad);
		}
		squads.Add(squad);
		squad.ai_command = this;
		return true;
	}

	protected virtual bool MaintainDecision(Squad squad)
	{
		if (!squad.ai_command.Validate(squad))
		{
			return false;
		}
		if (cached_priority <= 0f || cached_priority == squad.ai_command.cached_priority)
		{
			return true;
		}
		float num = ai.def.change_decision_ratio;
		if (!ignore_combat_when_changing_decision && squad.is_fighting)
		{
			if (squad.ai_command.target != null && squad.melee_squads.Contains(squad.ai_command.target as Squad))
			{
				num = ai.def.change_decision_ratio_in_other_combat;
			}
			else if (squad.melee_squads.Contains(target as Squad))
			{
				num = ai.def.change_decision_ratio_in_combat;
			}
		}
		if (squad.ai_command.cached_priority <= 0f || cached_priority / squad.ai_command.cached_priority >= num)
		{
			return false;
		}
		return true;
	}

	public virtual void RemoveSquad(Squad squad)
	{
		if (squad.ai_command == this)
		{
			squad.ai_command = null;
		}
		squads.Remove(squad);
	}

	public virtual void RemoveSquad(int i)
	{
		if (squads[i].ai_command == this)
		{
			squads[i].ai_command = null;
		}
		squads.RemoveAt(i);
	}

	public virtual bool Validate(Squad squad)
	{
		if (squad == null || !squad.IsValid() || squad.IsDefeated())
		{
			return false;
		}
		if (!ai.HasOwnerFlag(squad, BattleAI.EnableFlags.Commands))
		{
			return false;
		}
		if (squad.simulation.state >= BattleSimulation.Squad.State.Retreating)
		{
			return false;
		}
		return true;
	}

	public virtual bool Validate()
	{
		return true;
	}

	public virtual float Priority(Squad squad)
	{
		return 1f;
	}

	public virtual float Priority()
	{
		return 1f;
	}

	public virtual void Execute()
	{
	}
}
