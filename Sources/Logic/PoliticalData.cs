using System.Collections.Generic;

namespace Logic;

public class PoliticalData
{
	private struct RealmInfo
	{
		public string realm;

		public string custom_town_name;

		public string kingdom;
	}

	private struct KingdomInfo
	{
		public string kingdom;

		public string religion;

		public string vassal_of;
	}

	public string map_name;

	public string period;

	public int session_time;

	private List<RealmInfo> realms;

	private List<KingdomInfo> kingdoms;

	public override string ToString()
	{
		return $"MapData({map_name}/{period} @{session_time}sec): realms: {Container.Count(realms)}, kingdoms: {Container.Count(kingdoms)}";
	}

	public void InitFrom(Game game)
	{
		map_name = game.map_name;
		period = game.map_period;
		session_time = (int)game.session_time.seconds;
		realms = new List<RealmInfo>(game.realms.Count);
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (realm != null && (realm.kingdom_id != realm.init_kingdom_id || !string.IsNullOrEmpty(realm.custom_town_name)))
			{
				RealmInfo item = new RealmInfo
				{
					realm = realm.name,
					kingdom = realm.GetKingdom()?.Name,
					custom_town_name = realm.custom_town_name
				};
				realms.Add(item);
			}
		}
		kingdoms = new List<KingdomInfo>(game.kingdoms.Count);
		for (int j = 0; j < game.kingdoms.Count; j++)
		{
			Kingdom kingdom = game.kingdoms[j];
			if (kingdom != null && !kingdom.IsDefeated())
			{
				string text = "";
				string text2 = "";
				if (kingdom.csv_field != null)
				{
					text = kingdom.csv_field.GetString("Religion") ?? "";
					text2 = kingdom.csv_field.GetString("VassalOf") ?? "";
				}
				string text3 = EncodeKingdomReligion(kingdom) ?? "";
				string text4 = kingdom.sovereignState?.Name ?? "";
				if (text3 == text)
				{
					text3 = null;
				}
				if (text4 == text2)
				{
					text4 = null;
				}
				if (text3 != null || text4 != null)
				{
					KingdomInfo item2 = new KingdomInfo
					{
						kingdom = kingdom.Name,
						religion = text3,
						vassal_of = text4
					};
					kingdoms.Add(item2);
				}
			}
		}
		static string EncodeKingdomReligion(Kingdom k)
		{
			if (k?.religion == null)
			{
				return null;
			}
			string text5 = k.religion.name;
			if (k.is_catholic && k.excommunicated)
			{
				text5 += ".E";
			}
			else if (k.is_orthodox && !k.subordinated)
			{
				text5 += ".I";
			}
			else if (k.is_muslim && k.caliphate)
			{
				text5 += ".C";
			}
			return text5;
		}
	}

	public void WriteBody(Serialization.IWriter ser)
	{
		ser.WriteStr(map_name, "map_name");
		ser.WriteStr(period, "period");
		ser.Write7BitUInt(session_time, "session_time");
		int num = ((realms != null) ? realms.Count : 0);
		ser.Write7BitUInt(num, "realms");
		for (int i = 0; i < num; i++)
		{
			RealmInfo realmInfo = realms[i];
			ser.WriteStr(realmInfo.realm, "realm", i);
			ser.WriteStr(realmInfo.custom_town_name, "custom_town_name", i);
			ser.WriteStr(realmInfo.kingdom, "realm_kingdom", i);
		}
		int num2 = ((kingdoms != null) ? kingdoms.Count : 0);
		ser.Write7BitUInt(num2, "kingdoms");
		for (int j = 0; j < num2; j++)
		{
			KingdomInfo kingdomInfo = kingdoms[j];
			ser.WriteStr(kingdomInfo.kingdom, "kingdom", j);
			ser.WriteStr(kingdomInfo.religion, "kingdom_religion", j);
			ser.WriteStr(kingdomInfo.vassal_of, "vassal_of", j);
		}
	}

	public void ReadBody(Serialization.IReader ser)
	{
		map_name = ser.ReadStr("map_name");
		period = ser.ReadStr("period");
		session_time = ser.Read7BitUInt("session_time");
		int num = ser.Read7BitUInt("realms");
		realms = new List<RealmInfo>(num);
		for (int i = 0; i < num; i++)
		{
			string realm = ser.ReadStr("realm", i);
			string custom_town_name = ser.ReadStr("custom_town_name", i);
			string kingdom = ser.ReadStr("realm_kingdom", i);
			RealmInfo item = new RealmInfo
			{
				realm = realm,
				custom_town_name = custom_town_name,
				kingdom = kingdom
			};
			realms.Add(item);
		}
		int num2 = ser.Read7BitUInt("kingdoms");
		kingdoms = new List<KingdomInfo>(num2);
		for (int j = 0; j < num2; j++)
		{
			string kingdom2 = ser.ReadStr("kingdom", j);
			string religion = ser.ReadStr("kingdom_religion", j);
			string vassal_of = ser.ReadStr("vassal_of", j);
			KingdomInfo item2 = new KingdomInfo
			{
				kingdom = kingdom2,
				religion = religion,
				vassal_of = vassal_of
			};
			kingdoms.Add(item2);
		}
	}

	public void ApplyTo(Game game)
	{
		if (realms != null)
		{
			for (int i = 0; i < realms.Count; i++)
			{
				RealmInfo realmInfo = realms[i];
				Realm realm = game.GetRealm(realmInfo.realm);
				if (realm != null)
				{
					realm.ChangeTownName(realmInfo.custom_town_name);
					Kingdom kingdom = game.GetKingdom(realmInfo.kingdom);
					if (kingdom != null)
					{
						realm.SetKingdom(kingdom.id, ignore_victory: true, check_cancel_battle: false, via_diplomacy: true, send_state: false);
					}
				}
			}
		}
		if (kingdoms == null)
		{
			return;
		}
		for (int j = 0; j < kingdoms.Count; j++)
		{
			KingdomInfo kingdomInfo = kingdoms[j];
			Kingdom kingdom2 = game.GetKingdom(kingdomInfo.kingdom);
			if (kingdom2 != null)
			{
				if (kingdomInfo.religion != null)
				{
					CampaignUtils.SetKingdomReligion(kingdom2, kingdomInfo.religion);
				}
				if (kingdomInfo.vassal_of != null)
				{
					Kingdom kingdom3 = game.GetKingdom(kingdomInfo.vassal_of);
					kingdom2.SetSovereignState(kingdom3);
				}
			}
		}
	}

	public DT.Field Save()
	{
		DT.Field field = new DT.Field(null);
		field.key = "political_data";
		field.SetValue("map_name", DT.Enquote(map_name));
		field.SetValue("period", DT.Enquote(period));
		field.SetValue("session_time", session_time.ToString());
		if (realms != null)
		{
			DT.Field field2 = field.AddChild("realms");
			for (int i = 0; i < realms.Count; i++)
			{
				RealmInfo realmInfo = realms[i];
				DT.Field field3 = field2.AddChild(realmInfo.realm);
				field3.value_str = DT.Enquote(realmInfo.kingdom);
				if (realmInfo.custom_town_name != null)
				{
					field3.SetValue("custom_town_name", DT.Enquote(realmInfo.custom_town_name));
				}
			}
		}
		if (kingdoms != null)
		{
			DT.Field field4 = field.AddChild("kingdoms");
			for (int j = 0; j < kingdoms.Count; j++)
			{
				KingdomInfo kingdomInfo = kingdoms[j];
				DT.Field field5 = field4.AddChild(kingdomInfo.kingdom);
				if (kingdomInfo.religion != null)
				{
					field5.SetValue("religion", DT.Enquote(kingdomInfo.religion));
				}
				if (kingdomInfo.vassal_of != null)
				{
					field5.SetValue("vassal_of", DT.Enquote(kingdomInfo.vassal_of));
				}
			}
		}
		return field;
	}

	public void Load(string file_path)
	{
		DT.Field field = DT.Parser.LoadFieldFromFile(file_path, "political_data");
		if (field == null)
		{
			map_name = null;
			period = null;
			session_time = 0;
			realms = null;
			kingdoms = null;
			return;
		}
		map_name = field.GetString("map_name");
		period = field.GetString("period");
		session_time = field.GetInt("session_time");
		DT.Field field2 = field.FindChild("realms");
		if (field2?.children != null)
		{
			realms = new List<RealmInfo>(field2.children.Count);
			for (int i = 0; i < field2.children.Count; i++)
			{
				DT.Field field3 = field2.children[i];
				if (!string.IsNullOrEmpty(field3.key))
				{
					RealmInfo item = new RealmInfo
					{
						realm = field3.key,
						custom_town_name = field3.GetString("custom_town_name", null, null),
						kingdom = field3.String()
					};
					realms.Add(item);
				}
			}
		}
		else
		{
			realms = null;
		}
		DT.Field field4 = field.FindChild("kingdoms");
		if (field4?.children != null)
		{
			kingdoms = new List<KingdomInfo>(field4.children.Count);
			for (int j = 0; j < field4.children.Count; j++)
			{
				DT.Field field5 = field4.children[j];
				if (!string.IsNullOrEmpty(field5.key))
				{
					KingdomInfo item2 = new KingdomInfo
					{
						kingdom = field5.key,
						religion = field5.GetString("religion", null, null),
						vassal_of = field5.GetString("vassal_of", null, null)
					};
					kingdoms.Add(item2);
				}
			}
		}
		else
		{
			kingdoms = null;
		}
	}
}
