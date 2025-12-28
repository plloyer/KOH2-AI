namespace Logic;

public class Shia : Religion
{
	public Realm holy_lands_realm;

	public Shia(Game game, Def def)
		: base(game, def)
	{
	}

	public override void Init(bool new_game)
	{
		base.Init(new_game);
		string text = def.field.GetString("holy_lands_realm");
		if (!string.IsNullOrEmpty(text))
		{
			holy_lands_realm = game.GetRealm(text);
			if (holy_lands_realm == null)
			{
				Game.Log(def.field.Path(include_file: true) + ".holy_lands_realm: '" + text + "' realm not found", Game.LogType.Error);
			}
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		base.DumpInnerState(dump, verbosity);
		dump.Append("holy_lands_realm", holy_lands_realm?.name);
	}
}
