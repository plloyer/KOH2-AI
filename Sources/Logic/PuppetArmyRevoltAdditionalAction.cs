namespace Logic;

public class PuppetArmyRevoltAdditionalAction : Action
{
	public bool wasSuccessful;

	public PuppetArmyRevoltAdditionalAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new PuppetArmyRevoltAdditionalAction(owner as Character, def);
	}

	public override string ValidateSuccessAndFail(int min_chance = 0)
	{
		return base.ValidateSuccessAndFail(min_chance);
	}

	public override void Prepare()
	{
		wasSuccessful = false;
		base.Prepare();
	}

	public override void Run()
	{
		if (base.own_character.GetArmy().castle != null)
		{
			base.own_character.GetArmy().LeaveCastle(base.own_character.GetArmy().castle.GetRandomExitPoint());
		}
		if (base.own_character.TurnIntoRebel("GeneralRebels") != null)
		{
			wasSuccessful = true;
		}
		base.Run();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"master" => GetArg(0, null), 
			"puppet" => GetArg(1, null), 
			"is_close_to_puppet" => base.game.RealmDistance(base.own_character.GetArmy().realm_in.id, (GetArg(1, null).obj_val as Character).GetArmy().realm_in.id) <= def.field.GetInt("distance_to_puppet_realms"), 
			"are_there_other_rebellions_in_realm" => base.own_character.GetArmy().realm_in.rebellions.Count > 0, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}
}
