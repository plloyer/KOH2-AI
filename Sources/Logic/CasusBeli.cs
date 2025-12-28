namespace Logic;

public class CasusBeli : BaseObject, IVars
{
	public delegate CasusBeli CreateCasusBei(Kingdom owner, Kingdom against, string reason);

	public Kingdom owner;

	public Kingdom against;

	public string reason;

	public bool inheritable;

	public static CasusBeli Create(Kingdom owner, Kingdom against, string reason)
	{
		if (owner == null)
		{
			return null;
		}
		if (against == null)
		{
			return null;
		}
		CasusBeli casusBeli = new CasusBeli();
		casusBeli.owner = owner;
		casusBeli.against = against;
		casusBeli.reason = reason;
		Character king = owner.GetKing();
		casusBeli.inheritable = king == null || king.age >= Character.Age.Venerable;
		owner.AddCasusBeli(casusBeli);
		return casusBeli;
	}

	private static Character ChooseDiplomatForNewCB(Kingdom owner)
	{
		if (owner?.court == null)
		{
			return null;
		}
		Character result = null;
		int num = int.MaxValue;
		for (int i = 0; i < owner.court.Count; i++)
		{
			Character character = owner.court[i];
			if (character != null && character.IsDiplomat())
			{
				int num2 = 0;
				if (num >= num2)
				{
					result = character;
					num = num2;
				}
			}
		}
		return result;
	}

	public static bool ConsiderOpportunity(Kingdom owner, Kingdom against, string reason)
	{
		if (owner == null || against == null)
		{
			return false;
		}
		if (owner.IsEnemy(against))
		{
			return false;
		}
		if (owner.FindCasusBeli(against) != null)
		{
			return false;
		}
		if (ChooseDiplomatForNewCB(owner) == null)
		{
			return false;
		}
		return true;
	}

	public Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"owner" => owner, 
			"against" => against, 
			"reason" => reason, 
			_ => Value.Unknown, 
		};
	}

	public override string ToString()
	{
		return $"CasusBeli: {owner} -> {against} ({reason})";
	}
}
