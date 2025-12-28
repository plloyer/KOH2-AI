using System.Collections.Generic;

namespace Logic;

public class MultiplayerSettings
{
	public class Def : Logic.Def
	{
		public int connection_issues_timeout = 1500;

		public int heartbeat_timeout = 5000;

		public int connect_to_player_timeout = 15000;

		public List<float> disconnected_player_ai_times = new List<float> { 5f, 10f, 15f, 20f };

		public int AICT_Kingdom = 3;

		public int AICT_HireCourt = 3;

		public int AICT_Buildings = 3;

		public int AICT_Armies = 1;

		public int AICT_Units = 2;

		public int AICT_Garrison = 2;

		public int AICT_Characters = 3;

		public int AICT_Diplomacy = 3;

		public int AICT_Wars = 4;

		public int AICT_Offense = 2;

		public int AICT_Mercenaries = 3;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			connection_issues_timeout = field.GetInt("connection_issues_timeout", null, connection_issues_timeout);
			heartbeat_timeout = field.GetInt("heartbeat_timeout", null, heartbeat_timeout);
			connect_to_player_timeout = field.GetInt("connect_to_player_timeout", null, connect_to_player_timeout);
			DT.Field field2 = field.FindChild("disconnected_player_ai_times");
			if (field2 != null)
			{
				DT.Field field3 = field2.FindChild("times");
				if (field3?.children != null)
				{
					disconnected_player_ai_times.Clear();
					int num = 1;
					while (true)
					{
						DT.Field field4 = field3.FindChild(num.ToString());
						if (field4 == null)
						{
							break;
						}
						float item = field4.Float();
						disconnected_player_ai_times.Add(item);
						num++;
					}
				}
				DT.Field field5 = field2.FindChild("components");
				if (field5 != null)
				{
					AICT_Kingdom = field5.GetInt("Kingdom", null, AICT_Kingdom);
					AICT_HireCourt = field5.GetInt("HireCourt", null, AICT_HireCourt);
					AICT_Buildings = field5.GetInt("Buildings", null, AICT_Buildings);
					AICT_Armies = field5.GetInt("Armies", null, AICT_Armies);
					AICT_Units = field5.GetInt("Units", null, AICT_Units);
					AICT_Garrison = field5.GetInt("Garrison", null, AICT_Garrison);
					AICT_Characters = field5.GetInt("Characters", null, AICT_Characters);
					AICT_Diplomacy = field5.GetInt("Diplomacy", null, AICT_Diplomacy);
					AICT_Wars = field5.GetInt("Wars", null, AICT_Wars);
					AICT_Offense = field5.GetInt("Offense", null, AICT_Offense);
					AICT_Mercenaries = field5.GetInt("Mercenaries", null, AICT_Mercenaries);
				}
			}
			return true;
		}
	}
}
