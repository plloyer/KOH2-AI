namespace Logic;

public abstract class PopeInCourtAction : Action
{
	public override Kingdom own_kingdom => base.own_character?.GetSpecialCourtKingdom();

	public PopeInCourtAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public override Character GetVoicingCharacter()
	{
		return base.game.religions.catholic.head ?? base.own_character;
	}
}
