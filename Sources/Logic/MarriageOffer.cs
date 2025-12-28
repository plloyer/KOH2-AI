using System.Collections.Generic;

namespace Logic;

public class MarriageOffer : Offer
{
	public MarriageOffer(Def def, Object from, Object to)
		: base(def, from, to)
	{
	}

	public MarriageOffer(Kingdom from, Kingdom to, Character c1, Character c2)
		: base(from, to, c1, c2)
	{
	}

	public new static Offer Create(Def def, Object from, Object to)
	{
		return new MarriageOffer(def, from, to);
	}

	public override bool HasValidParent()
	{
		if (!base.HasValidParent())
		{
			return false;
		}
		if (parent == null && (from as Kingdom).IsEnemy(to as Kingdom))
		{
			return false;
		}
		if (CountSimilarOffersInParent() > 0)
		{
			return false;
		}
		return true;
	}

	public override string ValidateWithoutArgs()
	{
		string text = ValidateCore();
		if (ShouldReturn(text))
		{
			return text;
		}
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		if (kingdom == base.game.religions.catholic.hq_kingdom || kingdom2 == base.game.religions.catholic.hq_kingdom)
		{
			return "talking_to_papacy";
		}
		if (!ShouldSkip(text))
		{
			for (int i = 0; i < kingdom2.royalFamily.Children.Count; i++)
			{
				Character character = kingdom2.royalFamily.Children[i];
				if (!character.CanMarry())
				{
					continue;
				}
				for (int j = 0; j < kingdom.royalFamily.Children.Count; j++)
				{
					Character character2 = kingdom.royalFamily.Children[j];
					if (character2.CanMarry() && character2.sex != character.sex)
					{
						return "ok";
					}
				}
			}
			Character king = kingdom.GetKing();
			if (king == null)
			{
				Game.Log($"Kingdom has no king: {kingdom}", Game.LogType.Error);
			}
			if (king != null && king.CanMarry())
			{
				for (int k = 0; k < kingdom2.royalFamily.Children.Count; k++)
				{
					Character character3 = kingdom2.royalFamily.Children[k];
					if (character3.CanMarry() && character3.sex != king.sex)
					{
						return "ok";
					}
				}
			}
			Character king2 = kingdom2.GetKing();
			if (king2 == null)
			{
				Game.Log($"Kingdom has no king: {kingdom2}", Game.LogType.Error);
			}
			if (king2 != null && king2.CanMarry())
			{
				for (int l = 0; l < kingdom.royalFamily.Children.Count; l++)
				{
					Character character4 = kingdom.royalFamily.Children[l];
					if (character4.CanMarry() && character4.sex != king2.sex)
					{
						return "ok";
					}
				}
			}
			text = "_no_possible_args";
		}
		return text;
	}

	public override string Validate()
	{
		string text = base.Validate();
		if (ShouldReturn(text))
		{
			return text;
		}
		Character character = args[0].obj_val as Character;
		Character character2 = args[1].obj_val as Character;
		if (character.GetKingdom() == character.game.religions.catholic.hq_kingdom)
		{
			return "kingdom_is_papacy";
		}
		if (character2.GetKingdom() == character2.game.religions.catholic.hq_kingdom)
		{
			return "kingdom_is_papacy";
		}
		if (character == null || character2 == null)
		{
			return "no_characters";
		}
		if (!character.CanMarry() || !character2.CanMarry())
		{
			return "_invalid_args";
		}
		if (character.sex == character2.sex)
		{
			return "_invalid_args";
		}
		return text;
	}

	public override bool MatchArg(int idx, Value val)
	{
		if (!(val.obj_val is Character character))
		{
			return false;
		}
		if (!base.MatchArg(idx, val))
		{
			return false;
		}
		if (character.GetKingdom() == character.game.religions.catholic.hq_kingdom)
		{
			return false;
		}
		if (!character.CanMarry())
		{
			return false;
		}
		return true;
	}

	public override bool GetPossibleArgValues(int idx, List<Value> lst)
	{
		base.GetPossibleArgValues(idx, lst);
		Kingdom kingdom = null;
		Character character = null;
		switch (idx)
		{
		case 0:
			kingdom = GetSourceObj() as Kingdom;
			break;
		case 1:
			if (args != null && args.Count != 0)
			{
				character = args[0].obj_val as Character;
			}
			kingdom = GetTargetObj() as Kingdom;
			break;
		}
		Kingdom kingdom2 = ((kingdom == to as Kingdom) ? (from as Kingdom) : (to as Kingdom));
		if (kingdom2?.GetKing() == null)
		{
			Game.Log("Kingdom " + kingdom2.Name + "[" + kingdom2.id + "]is without king!!!", Game.LogType.Warning);
			return false;
		}
		for (int i = 0; i < kingdom.royalFamily.Children.Count; i++)
		{
			Character candidate = kingdom.royalFamily.Children[i];
			if (candidate != null && candidate.CanMarry() && (character == null || candidate.sex != character.sex) && (kingdom2.royalFamily.Children.Find((Character c) => c.sex != candidate.sex && c.CanMarry()) != null || (kingdom2.GetKing().sex != candidate.sex && kingdom2.GetKing().CanMarry())))
			{
				lst.Add(candidate);
			}
		}
		Character kingCandidate = kingdom.GetKing();
		if (kingCandidate != null && kingCandidate.CanMarry() && (character == null || (character != null && kingCandidate.sex != character.sex)) && kingdom2.royalFamily.Children.Find((Character c) => c.sex != kingCandidate.sex && c.CanMarry()) != null)
		{
			lst.Add(kingCandidate);
		}
		return lst.Count > 0;
	}

	public override void OnAccept()
	{
		base.OnAccept();
		Kingdom kingdom = GetSourceObj() as Kingdom;
		Kingdom kingdom2 = GetTargetObj() as Kingdom;
		Character character = args[0].obj_val as Character;
		Character character2 = args[1].obj_val as Character;
		new Marriage(character, character2);
		kingdom.SetStance(kingdom2, RelationUtils.Stance.Marriage);
		Vars vars = new Vars();
		vars.SetVar("kingdom_a", kingdom);
		vars.SetVar("kingdom_b", kingdom2);
		kingdom.game.BroadcastRadioEvent("ForeignMarriageMessage", vars);
	}
}
