namespace Logic;

public class KingdomFeature
{
	public class Def : Logic.Def
	{
		public string Name;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			Name = field.GetString("name", null, dt_def.path);
			return true;
		}
	}
}
