using System.Collections.Generic;

namespace Logic;

public class PuppetRuinPactAction : PuppetPlot
{
	public PuppetRuinPactAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetRuinPactAction(owner as Character, def);
	}

	public override string ValidatePuppet(Character puppet)
	{
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (!puppet.IsDiplomat())
		{
			return "no_puppet";
		}
		Pact pact = puppet.pact;
		if (pact == null)
		{
			return "no_pact";
		}
		if (pact.members.Count <= 1)
		{
			return "no_pact";
		}
		return base.ValidatePuppet(puppet);
	}

	public override List<Value>[] GetPossibleArgs()
	{
		List<Value>[] array = new List<Value>[1]
		{
			new List<Value>()
		};
		if (!(base.target is Character { pact: var pact }))
		{
			return null;
		}
		if (pact == null)
		{
			return null;
		}
		AddArg(ref array[0], pact, 0);
		return array;
	}

	public override void Run()
	{
		Character character = base.target as Character;
		if (character.pact != null)
		{
			character.pact.Dissolve();
		}
		base.Run();
	}
}
