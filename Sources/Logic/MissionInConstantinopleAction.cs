namespace Logic;

public class MissionInConstantinopleAction : Action
{
	public MissionInConstantinopleAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new MissionInConstantinopleAction(owner as Character, def);
	}

	public override void Run()
	{
		Character character = base.own_character;
		if (character != null)
		{
			Kingdom head_kingdom = base.game.religions.orthodox.head_kingdom;
			character.SetMissionKingdom(head_kingdom);
			character.SetDefaultStatus<OnMissionInConstantinopleStatus>();
			base.Run();
		}
	}
}
