namespace Logic;

public class DiplomacyBuyABookAction : DiplomatAction
{
	public DiplomacyBuyABookAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new DiplomacyBuyABookAction(owner as Character, def);
	}

	public override void Run()
	{
		if (base.own_character != null)
		{
			Book book = base.target as Book;
			if (!(own_kingdom.resources[ResourceType.Gold] < (float)book.GetPrice()))
			{
				own_kingdom.AddBook(book.def);
				own_kingdom.SubResources(KingdomAI.Expense.Category.Diplomacy, ResourceType.Gold, book.GetPrice());
				base.Run();
			}
		}
	}

	public override string Validate(bool quick_out = false)
	{
		Character character = base.own_character;
		if (character == null)
		{
			return "not_a_character";
		}
		if (character.GetSkill("Literacy") == null)
		{
			return "no_finances_skill";
		}
		if (character.mission_kingdom == null)
		{
			return "not_in_a_kingdom";
		}
		return base.Validate(quick_out);
	}
}
