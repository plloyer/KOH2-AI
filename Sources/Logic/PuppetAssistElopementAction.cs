using System.Collections.Generic;

namespace Logic;

public class PuppetAssistElopementAction : PuppetPlot
{
	public PuppetAssistElopementAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetAssistElopementAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (puppet.IsRebel())
		{
			return "is_rebel";
		}
		if (own_kingdom.IsEnemy(base.own_character.mission_kingdom))
		{
			return "in_war";
		}
		return base.ValidatePuppet(puppet);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value>[] array = new List<Value>[2]
		{
			new List<Value>(),
			new List<Value>()
		};
		List<Character> children = base.own_character.mission_kingdom.royalFamily.Children;
		List<Character> children2 = own_kingdom.royalFamily.Children;
		int num = base.game.Random(0, children.Count);
		for (int i = 0; i < children.Count; i++)
		{
			Character tc = children[(i + num) % children.Count];
			if (tc != null && tc.CanMarry())
			{
				Character character = children2.Find((Character c) => c.sex != tc.sex && c.CanMarry());
				if (character != null)
				{
					AddArg(ref array[0], tc, 0);
					AddArg(ref array[1], character, 1);
				}
			}
		}
		if (array[0].Count == 0 || array[1].Count == 0)
		{
			return null;
		}
		return array;
	}

	public override void Run()
	{
		Character character = args[0].obj_val as Character;
		Character character2 = args[1].obj_val as Character;
		new Marriage(character, character2);
		own_kingdom.SetStance(base.own_character.mission_kingdom, RelationUtils.Stance.Marriage);
		base.Run();
	}
}
