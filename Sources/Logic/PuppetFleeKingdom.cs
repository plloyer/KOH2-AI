using System.Collections.Generic;

namespace Logic;

public class PuppetFleeKingdom : RecallAction
{
	public PuppetFleeKingdom(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetFleeKingdom(owner as Character, def);
	}

	public static void Flee(Character puppet, Character master, Kingdom flee_to_kingdom = null, bool keep_army = false)
	{
		Def def = puppet.game.defs.Get<Def>("PuppetFleeKingdom");
		if (def != null && Action.Find(puppet, def) is PuppetFleeKingdom puppetFleeKingdom)
		{
			puppet.cur_action?.Cancel();
			puppetFleeKingdom.args = new List<Value>();
			puppetFleeKingdom.args.Add(keep_army);
			puppetFleeKingdom.Execute(flee_to_kingdom);
		}
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (!character.IsAlive())
		{
			return "dead";
		}
		if (character.prison_kingdom != null)
		{
			return "in_prison";
		}
		return "ok";
	}

	public override void Run()
	{
		Kingdom newKingdom = base.target as Kingdom;
		bool keep_army = args[0];
		base.own_character.DesertKingdom(createClone: true, newKingdom, dieIfNoNewKingdom: true, keep_army);
	}
}
