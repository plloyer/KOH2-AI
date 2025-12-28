using System.Collections.Generic;

namespace Logic;

public class PuppetPlotAssassinationAction : PuppetPlot
{
	public PuppetPlotAssassinationAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetPlotAssassinationAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.GetArmy()?.rebel != null)
		{
			return "is_rebel";
		}
		return base.ValidatePuppet(puppet);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		Kingdom mission_kingdom = base.own_character.mission_kingdom;
		List<Value>[] array = new List<Value>[1]
		{
			new List<Value>()
		};
		for (int i = 0; i < mission_kingdom.royalFamily.Children.Count; i++)
		{
			Character character = mission_kingdom.royalFamily.Children[i];
			AddArg(ref array[0], character, 0);
		}
		for (int j = 0; j < mission_kingdom.court.Count; j++)
		{
			Character character2 = mission_kingdom.court[j];
			AddArg(ref array[0], character2, 0);
		}
		if (array[0].Count == 0)
		{
			return null;
		}
		return array;
	}

	public override bool ValidateArg(Value value, int def_type)
	{
		if (!(value.obj_val is Character character))
		{
			return false;
		}
		if (!character.IsAlive())
		{
			return false;
		}
		if (character.IsKing() || character.IsQueen())
		{
			return false;
		}
		if (base.own_character.puppets != null && base.own_character.puppets.Contains(character))
		{
			return false;
		}
		return base.ValidateArg(value, def_type);
	}

	public override void Run()
	{
		if (args != null && args.Count >= 1)
		{
			if (args[0].obj_val is Character character)
			{
				character.Die(new DeadStatus("assassinated", character));
			}
			base.Run();
		}
	}
}
