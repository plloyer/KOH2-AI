namespace Logic;

public class RebelAgenda
{
	public class Def : Logic.Def
	{
		public string name;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			return true;
		}
	}

	public string Name;

	public Def def;

	public RebelAgenda(Def def)
	{
		this.def = def;
	}

	private string GetLocalizeKey()
	{
		return "tn_" + Name;
	}
}
