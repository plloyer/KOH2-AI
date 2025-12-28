namespace Logic;

public class ImproveRelationsAction : Action
{
	public ImproveRelationsAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ImproveRelationsAction(owner as Character, def);
	}

	public override void Prepare()
	{
		base.Prepare();
		OnMissionChangedAnalytics();
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			character.SetMissionKingdom(base.target as Kingdom);
			character.mission_kingdom.diplomats.Add(character);
			character.SetDefaultStatus<ImprovingRelationsStatus>();
			base.Run();
		}
	}
}
