using System.Collections.Generic;

namespace Logic;

public class Sunni : Religion
{
	public List<Realm> holy_lands_realms;

	public Sunni(Game game, Def def)
		: base(game, def)
	{
	}

	public override void Init(bool new_game)
	{
		base.Init(new_game);
		DT.Field field = def.field.FindChild("holy_lands_realms");
		if (field == null)
		{
			return;
		}
		int num = field.NumValues();
		if (num == 0)
		{
			return;
		}
		holy_lands_realms = new List<Realm>(num);
		for (int i = 0; i < num; i++)
		{
			string text = field.String(i);
			if (!string.IsNullOrEmpty(text))
			{
				Realm realm = game.GetRealm(text);
				if (realm == null)
				{
					Game.Log(def.field.Path(include_file: true) + ".holy_lands_realm: '" + text + "' realm not found", Game.LogType.Error);
				}
				else
				{
					holy_lands_realms.Add(realm);
				}
			}
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		base.DumpInnerState(dump, verbosity);
		if (holy_lands_realms != null && holy_lands_realms.Count > 0)
		{
			dump.OpenSection("holy_lands_realms");
			for (int i = 0; i < holy_lands_realms.Count; i++)
			{
				dump.Append(holy_lands_realms[i]?.name);
			}
			dump.CloseSection("holy_lands_realms");
		}
	}
}
