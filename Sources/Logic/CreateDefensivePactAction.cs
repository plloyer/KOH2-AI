using System.Collections.Generic;

namespace Logic;

public class CreateDefensivePactAction : Action
{
	public CreateDefensivePactAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new CreateDefensivePactAction(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		if (base.own_character?.pact != null)
		{
			return "already_has_pact";
		}
		return base.Validate(quick_out);
	}

	public override string ValidateMissionKingdom()
	{
		return "ok";
	}

	public override string ValidateIdle()
	{
		return "ok";
	}

	public override List<Object> GetPossibleTargets()
	{
		Kingdom kingdom = own_kingdom;
		List<Object> targets = new List<Object>(kingdom.neighbors.Count);
		foreach (Kingdom neighbor in kingdom.neighbors)
		{
			AddTarget(ref targets, neighbor);
		}
		return targets;
	}

	public override bool ValidateTarget(Object target)
	{
		if (!(target is Kingdom against))
		{
			return false;
		}
		if (!Pact.CanCreate(Pact.Type.Defensive, base.own_character, against))
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
			Pact.Create(Pact.Type.Defensive, character, base.target as Kingdom);
			base.Run();
		}
	}
}
