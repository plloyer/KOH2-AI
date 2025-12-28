namespace Logic;

public class GovernModifiers
{
	public class Def : Logic.Def
	{
		public int notGoverningRepeatTime;

		public int notGoverningRepeatTimePlusMinus;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			notGoverningRepeatTime = field.GetInt("notGoverningRepeatTime");
			notGoverningRepeatTimePlusMinus = field.GetInt("notGoverningRepeatTimePlusMinus");
			return true;
		}
	}
}
