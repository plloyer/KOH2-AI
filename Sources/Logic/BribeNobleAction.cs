using System.Collections.Generic;

namespace Logic;

public class BribeNobleAction : SpyPlot
{
	public BribeNobleAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new BribeNobleAction(owner as Character, def);
	}

	public override bool NeedsTarget()
	{
		return true;
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character.mission_kingdom == null)
		{
			return "not_on_a_mission";
		}
		return base.Validate(quick_out);
	}

	private bool IsBribeable(Character c)
	{
		if (!c.IsAlive())
		{
			return false;
		}
		if (c.IsKing())
		{
			return false;
		}
		if (c.IsPrince() && base.own_character.GetStat(Stats.cs_allow_bribe_princes) == 0f)
		{
			return false;
		}
		if (c.IsPrisoner())
		{
			return false;
		}
		if (c.masters != null && c.masters.Contains(base.own_character))
		{
			return false;
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		Character character = base.own_character;
		if (character == null)
		{
			return null;
		}
		Kingdom mission_kingdom = character.mission_kingdom;
		if (mission_kingdom == null || mission_kingdom.court == null)
		{
			return null;
		}
		List<Object> list = new List<Object>();
		for (int i = 0; i < mission_kingdom.court.Count; i++)
		{
			Character character2 = mission_kingdom.court[i];
			if (character2 != null && IsBribeable(character2))
			{
				list.Add(character2);
			}
		}
		return list;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Character character))
		{
			return false;
		}
		Character character2 = base.own_character;
		if (character2 == null)
		{
			return false;
		}
		Kingdom mission_kingdom = character2.mission_kingdom;
		if (mission_kingdom == null)
		{
			return false;
		}
		if (character.GetKingdom() != mission_kingdom)
		{
			return false;
		}
		if (!IsBribeable(character))
		{
			return false;
		}
		return true;
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			if (base.target is Character character2)
			{
				character.AddPuppet(character2);
				character.NotifyListeners("bribed_noble", character2);
			}
			base.Run();
		}
	}
}
