using System.Collections.Generic;

namespace Logic;

public class AskForPrisonerRansom : Offer
{
	public AskForPrisonerRansom(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public AskForPrisonerRansom(Kingdom from, Kingdom to, Character target, int ransom_default = 0)
		: base(from, to, target, ransom_default)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new AskForPrisonerRansom(def, from, to);
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (!(GetArg(0).obj_val is Character character))
		{
			return "no_arg";
		}
		if (!character.GetKingdom().IsRegular())
		{
			return "no_longer_from_regular_kingdom";
		}
		if (character.IsMercenary())
		{
			return "is_mercenary";
		}
		if (character.prison_kingdom == character.GetKingdom())
		{
			return "imprisoned_in_own_kingdom";
		}
		if (!kingdom.prisoners.Contains(character))
		{
			return "prisoner_not_in_ransomer_prison";
		}
		Resource cost = character.RansomPrice();
		if (!kingdom2.resources.CanAfford(cost, 1f))
		{
			return "cant_afford";
		}
		return text;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		switch (idx)
		{
		case 1:
			if (!(GetArg(0).obj_val is Character character2))
			{
				return false;
			}
			lst.Add((int)character2.RansomPrice().Get(ResourceType.Gold));
			return true;
		default:
			return true;
		case 0:
		{
			base.GetPossibleArgValues(idx, lst);
			Kingdom kingdom = GetSourceObj() as Kingdom;
			Kingdom kingdom2 = GetTargetObj() as Kingdom;
			for (int i = 0; i < kingdom.prisoners.Count; i++)
			{
				Character character = kingdom.prisoners[i];
				if (character.GetKingdom() == kingdom2)
				{
					lst.Add(character);
				}
			}
			return lst.Count > 0;
		}
		}
	}

	public override void OnAccept()
	{
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		Character character = GetArg(0).obj_val as Character;
		Resource resource = character.RansomPrice();
		base.OnAccept();
		if (kingdom2.resources.CanAfford(resource, 1f))
		{
			kingdom.AddResources(KingdomAI.Expense.Category.Diplomacy, resource);
			kingdom2.SubResources(KingdomAI.Expense.Category.Diplomacy, resource);
			character.Imprison(null, recall: true, send_state: true, "ransomed");
			character.NotifyListeners("prisoner_ransomed");
			character?.OnPrisonActionAnalytics("ask_for_ransom_succeeded", resource, resource);
		}
	}

	public override void OnDecline()
	{
		base.OnDecline();
		GetSourceObj();
		GetTargetObj();
		Character character = GetArg(0).obj_val as Character;
		Timer.Start(character, "ransom_cooldown", character.prison_kingdom.royal_dungeon.def.ransom_cooldown_time);
		character.FireEvent("force_refresh_actions", null);
		character?.OnPrisonActionAnalytics("ask_for_ransom_failed", character?.RansomPrice());
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Kingdom kingdom = GetSourceObj() as Kingdom;
		switch (key)
		{
		case "prisoner":
			return args[0].Get<Character>();
		case "relationship_execute_mod":
		case "relationship_execute":
		case "relationship_release":
			return kingdom.royal_dungeon.GetVar(key, this, as_value);
		default:
			return base.GetVar(key, vars, as_value);
		}
	}
}
