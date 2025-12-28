using System.Collections.Generic;

namespace Logic;

public class AvailableUnits
{
	public class Def : Logic.Def
	{
		public string name;

		public List<Unit.Def> avaliable_types = new List<Unit.Def>();

		public List<Unit.Def> town_guard_types = new List<Unit.Def>();

		public Unit.Def marhsal;

		public Unit.Def marshal_guard;

		public Unit.Def rebel_marshal;

		public Unit.Def rebel_marshal_guard;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field.FindChild("town_guards");
			if (field != null && field.children != null && field.children.Count > 0)
			{
				for (int i = 0; i < field.children.Count; i++)
				{
					DT.Field field2 = field.children[i];
					if (!string.IsNullOrEmpty(field2.key) && !(field2.key == "town_guards") && !(field2.key == "marshals"))
					{
						Unit.Def def = game.defs.Find<Unit.Def>(field2.key);
						if (def != null)
						{
							town_guard_types.Add(def);
						}
					}
				}
			}
			if (dt_def.field.children != null && dt_def.field.children.Count > 0)
			{
				for (int j = 0; j < dt_def.field.children.Count; j++)
				{
					DT.Field field3 = dt_def.field.children[j];
					if (!string.IsNullOrEmpty(field3.key) && !(field3.key == "town_guards") && !(field3.key == "marshals"))
					{
						Unit.Def def2 = game.defs.Find<Unit.Def>(field3.key);
						if (def2 != null)
						{
							avaliable_types.Add(def2);
						}
					}
				}
			}
			DT.Field field4 = dt_def.field.FindChild("marshals");
			if (field4 != null)
			{
				marhsal = game.defs.Find<Unit.Def>(field4.GetValueStr("marshal"));
				marshal_guard = game.defs.Find<Unit.Def>(field4.GetValueStr("marshal_guard"));
				rebel_marshal = game.defs.Find<Unit.Def>(field4.GetValueStr("rebel_marshal"));
				rebel_marshal_guard = game.defs.Find<Unit.Def>(field4.GetValueStr("rebel_marshal_guard"));
			}
			return true;
		}

		public override bool Validate(Game game)
		{
			if (!IsBase())
			{
				if (avaliable_types.Count == 0)
				{
					Game.Log("Unit set " + name + " is dont have any unit options", Game.LogType.Warning);
				}
				if (town_guard_types.Count == 0)
				{
					Game.Log("Unit set " + name + " is dont have any town_gurad options", Game.LogType.Warning);
				}
				if (marhsal == null)
				{
					Game.Log("Unit set " + name + " is missign a marshal option", Game.LogType.Warning);
				}
				if (marshal_guard == null)
				{
					Game.Log("Unit set " + name + " is missign a marshal_guard option", Game.LogType.Warning);
				}
				if (rebel_marshal == null)
				{
					Game.Log("Unit set " + name + " is missign a rebel_marshal option", Game.LogType.Warning);
				}
				if (rebel_marshal_guard == null)
				{
					Game.Log("Unit set " + name + " is missign a rebel_marshal_guard option", Game.LogType.Warning);
				}
			}
			return true;
		}
	}

	public enum Source
	{
		All,
		Kingdom,
		Realm,
		COUNT
	}

	private List<Unit.Def>[] available_units_per_type;

	private List<Unit.Def> avalilable_equipment = new List<Unit.Def>();

	private List<Unit.Def> town_guard_units = new List<Unit.Def>();

	private Castle castle;

	public AvailableUnits(Castle castle)
	{
		this.castle = castle;
		Update();
	}

	public void Update()
	{
		BuildUnitSet();
		BuildEquipment();
	}

	private void BuildUnitSet()
	{
		if (available_units_per_type == null)
		{
			int num = 3;
			available_units_per_type = new List<Unit.Def>[num];
			for (int i = 0; i < num; i++)
			{
				available_units_per_type[i] = new List<Unit.Def>();
			}
		}
		int j = 0;
		for (int num2 = 3; j < num2; j++)
		{
			available_units_per_type[j].Clear();
		}
		Kingdom kingdom = castle.GetKingdom();
		Realm realm = castle.game.GetRealm(kingdom.Name);
		Realm realm2 = castle.GetRealm();
		if (kingdom.units_set != null)
		{
			Def def = castle.game.defs.Get<Def>(kingdom?.units_set);
			AddUnitSet(def, Source.Kingdom);
		}
		if (kingdom != null && kingdom.unit_types != null && kingdom.unit_types.Count > 0)
		{
			for (int k = 0; k < kingdom.unit_types.Count; k++)
			{
				AddUnit(kingdom.unit_types[k], Source.Kingdom);
			}
		}
		if (realm != null && realm.unit_types != null && realm.unit_types.Count > 0)
		{
			for (int l = 0; l < realm.unit_types.Count; l++)
			{
				AddUnit(realm.unit_types[l], Source.Kingdom);
			}
		}
		if (realm2 != null && realm2.unit_types != null && realm2.unit_types.Count > 0)
		{
			for (int m = 0; m < realm2.unit_types.Count; m++)
			{
				AddUnit(realm2.unit_types[m], Source.Realm);
			}
		}
		if (!ValidateAvailableUnits())
		{
			Def def2 = castle.game.defs.Find<Def>("DefaultUnitSet");
			if (def2 != null)
			{
				AddUnitSet(def2, Source.Kingdom);
			}
		}
		if (!ValidateAvailableUnits())
		{
			Game.Log($"Unit data for castle {this} from kingodm {kingdom} is insufficent", Game.LogType.Warning);
		}
		void AddUnit(string def_key, Source source)
		{
			Unit.Def def3 = castle.game.defs.Find<Unit.Def>(def_key);
			if (def3 != null)
			{
				AddUnitDef(def3, source);
			}
		}
		void AddUnitDef(Unit.Def unit_def, Source source)
		{
			if (unit_def != null && unit_def.ReligionEligable(realm2) && !available_units_per_type[(int)source].Contains(unit_def))
			{
				available_units_per_type[(int)source].Add(unit_def);
				List<Unit.Def> list = available_units_per_type[0];
				if (!list.Contains(unit_def))
				{
					list.Add(unit_def);
				}
			}
		}
		void AddUnitSet(Def def3, Source source)
		{
			if (def3 != null)
			{
				for (int n = 0; n < def3.avaliable_types.Count; n++)
				{
					AddUnitDef(def3.avaliable_types[n], source);
				}
				for (int num3 = 0; num3 < def3.town_guard_types.Count; num3++)
				{
					Unit.Def item = def3.town_guard_types[num3];
					if (!town_guard_units.Contains(item))
					{
						town_guard_units.Add(item);
					}
				}
			}
		}
		bool ValidateAvailableUnits()
		{
			List<Unit.Def> list = available_units_per_type[0];
			bool num3 = list != null && list.Count > 0;
			bool flag = town_guard_units.Count > 0;
			return num3 && flag;
		}
	}

	private void BuildEquipment()
	{
		avalilable_equipment = new List<Unit.Def>();
		foreach (KeyValuePair<string, Logic.Def> def2 in castle.game.defs.Get(typeof(Unit.Def)).defs)
		{
			Unit.Def def = def2.Value as Unit.Def;
			if (def.type == Unit.Type.InventoryItem && def.available)
			{
				avalilable_equipment.Add(def);
			}
		}
	}

	public bool CanBuyEquipment(Unit.Def unit)
	{
		if (avalilable_equipment == null)
		{
			return false;
		}
		if (!avalilable_equipment.Contains(unit))
		{
			return false;
		}
		return unit.buildPrerqusite.Validate(castle, castle?.army?.leader);
	}

	public bool CanBuildUnit(Unit.Def unit, bool match_prerequisites = true)
	{
		if (available_units_per_type == null)
		{
			return false;
		}
		if (!available_units_per_type[0].Contains(unit))
		{
			return false;
		}
		Realm realm = castle.GetRealm();
		if (!match_prerequisites)
		{
			return true;
		}
		if (realm.IsOccupied() || realm.IsDisorder())
		{
			return false;
		}
		return unit.buildPrerqusite.Validate(castle);
	}

	public List<Unit.Def> GetAvailableUnitTypes(Source source = Source.All)
	{
		if (source == Source.COUNT)
		{
			return null;
		}
		return available_units_per_type[(int)source];
	}

	public Unit.Def GetTownGuardDef(int level)
	{
		if (town_guard_units.Count == 0)
		{
			return null;
		}
		if (level < 0)
		{
			level = 0;
		}
		if (level >= town_guard_units.Count)
		{
			level = town_guard_units.Count - 1;
		}
		return town_guard_units[level];
	}

	public Unit.Def GetLevyDef(Unit.Type type)
	{
		for (int i = 0; i < available_units_per_type.Length; i++)
		{
			for (int j = 0; j < available_units_per_type[i].Count; j++)
			{
				Unit.Def def = available_units_per_type[i][j];
				if (def.type == type || def.secondary_type == type)
				{
					return def;
				}
			}
		}
		return null;
	}

	public Unit.Def GetMilitiaDef()
	{
		Unit.Def def = null;
		Unit.Def result = null;
		for (int i = 0; i < available_units_per_type.Length; i++)
		{
			for (int j = 0; j < available_units_per_type[i].Count; j++)
			{
				Unit.Def def2 = available_units_per_type[i][j];
				if (def2.buildPrerqusite.Validate(castle))
				{
					if (def2.field.key == "Militia" || def2.field.based_on.key == "Militia")
					{
						result = def2;
					}
					else if (def2.field.key == "AdvancedMilitia" || def2.field.based_on.key == "AdvancedMilitia")
					{
						def = def2;
					}
				}
			}
		}
		if (def != null)
		{
			return def;
		}
		return result;
	}

	public List<Unit.Def> GetAvailableEquipmentTypes()
	{
		return avalilable_equipment;
	}
}
