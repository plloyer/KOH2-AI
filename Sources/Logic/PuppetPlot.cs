using System.Collections.Generic;

namespace Logic;

public class PuppetPlot : SpyPlot
{
	private List<Character> validPuppets = new List<Character>();

	public PuppetPlot(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetPlot(owner as Character, def);
	}

	public override string Validate(bool quick_out = false)
	{
		List<Character> list = GetValidPuppets();
		if (list == null || list.Count == 0)
		{
			return "no_puppets";
		}
		return base.Validate(quick_out);
	}

	public bool CheckPuppetClass(DT.Field classesField, string puppet_class)
	{
		if (classesField == null)
		{
			return true;
		}
		int num = classesField.NumValues();
		for (int i = 0; i < num; i++)
		{
			Value value = classesField.Value(i);
			if ((Value)puppet_class == value)
			{
				return true;
			}
		}
		return false;
	}

	public virtual string ValidatePuppet(Character puppet)
	{
		if (base.own_character?.puppets == null)
		{
			return "no_valid_pupets";
		}
		if (puppet == null)
		{
			return "no_puppet";
		}
		if (!puppet.IsValid())
		{
			return "no_valid";
		}
		if (puppet.IsDead())
		{
			return "puppet_is_dead";
		}
		if (puppet.IsPrisoner())
		{
			return "is_prisoner";
		}
		if (!base.own_character.puppets.Contains(puppet))
		{
			return "no_longer_a_puppet";
		}
		return "ok";
	}

	public override bool ValidateTarget(Object target)
	{
		if (def.target != null)
		{
			return base.ValidateTarget(target);
		}
		return ValidatePuppet(target as Character) == "ok";
	}

	public override bool NeedsTarget()
	{
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		List<Character> list = GetValidPuppets();
		if (list.Count == 0)
		{
			return null;
		}
		List<Object> list2 = new List<Object>();
		list2.AddRange(list);
		return list2;
	}

	public List<Character> GetValidPuppets()
	{
		validPuppets.Clear();
		Character character = base.owner as Character;
		if (character.puppets == null)
		{
			return validPuppets;
		}
		DT.Field classesField = def.field.FindChild("puppet_classes");
		for (int i = 0; i < character.puppets.Count; i++)
		{
			if (CheckPuppetClass(classesField, character.puppets[i].GetClass().name) && ValidatePuppet(character.puppets[i]) == "ok")
			{
				validPuppets.Add(character.puppets[i]);
			}
		}
		return validPuppets;
	}
}
