using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Castle, dynamic = false)]
public class Castle : Settlement
{
	public struct BuildOption : IVars
	{
		public Castle castle;

		public Building.Def def;

		public float eval;

		public KingdomAI.Expense.Priority priority;

		public override string ToString()
		{
			string text = $"[{eval}] [{priority}] {this.def?.id}";
			Building.Def def = this.def?.GetFirstUpgradeOf();
			if (def != null)
			{
				text = text + " (" + def.id + ")";
			}
			if (castle != null)
			{
				text = text + " in " + castle?.name;
			}
			return text;
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return "@\\[{eval}\\] \\[{priority_text}\\] {building}[ ({upgrade_of})] in {castle}";
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "priority_text":
				return "#" + priority;
			case "eval":
				return eval;
			case "def":
			case "building":
				return def;
			case "upgrade_of":
				return def?.GetFirstUpgradeOf();
			case "castle":
			case "other_castle":
				return castle;
			case "kingdom":
				return castle?.GetKingdom();
			default:
				return Value.Unknown;
			}
		}
	}

	public struct AIBuildingReservation : IVars
	{
		public Building.Def bdef;

		public List<Building.Def> prerequisite_of;

		public override string ToString()
		{
			string id = bdef.id;
			if (prerequisite_of == null)
			{
				return id;
			}
			id += " (";
			for (int i = 0; i < prerequisite_of.Count; i++)
			{
				Building.Def def = prerequisite_of[i];
				if (i > 0)
				{
					id += ", ";
				}
				id += def.id ?? "null";
			}
			return id + ")";
		}

		public string GetNameKey(IVars vars = null, string form = "")
		{
			return "@{building}[ ({prerequisite_of})]";
		}

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "def":
			case "building":
				return bdef;
			case "prerequisite_of":
				return new Value(prerequisite_of);
			default:
				return Value.Unknown;
			}
		}
	}

	[Flags]
	public enum StructureBuildAvailability
	{
		Available = 0,
		AlreadyBuilt = 1,
		CannotAfford = 2,
		UnderConstruction = 4,
		CastleInBattle = 8,
		CastleSacked = 0x10,
		RealmOccupied = 0x20,
		MaxCountReached = 0x40,
		NoRequirements = 0x100,
		NoParent = 0x200,
		NoParentNoRequirements = 0x300,
		MissingData = 0x1000
	}

	public enum BuildingRemovalMode
	{
		Regular,
		Abandoned,
		Upgrade
	}

	public class Build
	{
		public enum State
		{
			Started,
			Finished,
			Canceled
		}

		private Castle Owner;

		public float production_cost;

		public float current_production_amount;

		public float current_progress = -1f;

		public int prefered_slot_index = -1;

		public Building.Def current_building_def;

		public Building current_building_repair;

		private State state = State.Finished;

		public bool keep_progress;

		public Build(Castle castle)
		{
			Owner = castle;
			Reset();
		}

		public override string ToString()
		{
			return string.Format("[{0}] {1} at {2}", state, current_building_def?.id ?? current_building_repair?.def?.id ?? "none", prefered_slot_index);
		}

		public State GetState()
		{
			return state;
		}

		public Building.Def GetBuild()
		{
			if (current_building_def != null)
			{
				return current_building_def;
			}
			if (current_building_repair != null)
			{
				return current_building_repair.def;
			}
			return null;
		}

		public float GetProgress()
		{
			return current_progress;
		}

		public void Update()
		{
			if (!Owner.GetRealm().game.isInVideoMode && Owner.IsAuthority() && IsBuilding())
			{
				if (current_building_repair != null)
				{
					current_production_amount += Owner.GetRealm().income[ResourceType.Hammers] * 1f * Owner.sacking_comp.def.sacking_recovery_production_mod * (Owner.quick_recovery ? Owner.sacking_comp.def.quick_recovery_mod : 1f);
				}
				else
				{
					current_production_amount += Owner.GetRealm().income[ResourceType.Hammers] * 1f;
				}
				current_progress = current_production_amount / production_cost;
				if (current_progress >= 1f)
				{
					current_progress = 1f;
					CompleteBuild();
				}
				else
				{
					Owner.SendState<BuildProgressState>();
				}
			}
		}

		private void CompleteBuild()
		{
			if (!Owner.IsAuthority())
			{
				return;
			}
			if (current_building_repair != null)
			{
				Owner.burned_buildings.Remove(current_building_repair);
				current_building_repair.SetState(Building.State.TemporaryDeactivated);
				Reset();
				state = State.Finished;
				Owner.SendState<BuildState>();
				Owner.SendState<SackedStructuresState>();
				if (Owner.burned_buildings.Count > 0)
				{
					Owner.structure_build.BeginRepair(Owner.burned_buildings[0]);
				}
				Owner.GetKingdom()?.RecalcBuildingStates(Owner);
				Owner.NotifyListeners("structures_changed");
				Owner.NotifyListeners("repair_progress");
			}
			else
			{
				Owner.OnCompleteBuild(current_building_def, prefered_slot_index);
				Reset();
				state = State.Finished;
				Owner.SendState<BuildState>();
			}
		}

		public void CancelBuild(bool send_state = true, bool check_keep_progress = true)
		{
			if (!Owner.IsAuthority() && send_state)
			{
				Owner.SendEvent(new CancelBuildEvent());
				return;
			}
			if (check_keep_progress)
			{
				keep_progress = current_building_repair != null;
			}
			if (!IsBuilding())
			{
				return;
			}
			Building.Def build = GetBuild();
			if (build != null)
			{
				Kingdom kingdom = Owner?.GetKingdom();
				if (kingdom != null)
				{
					Resource resource = new Resource(build.GetBuildRefunds());
					if (!resource.IsZero())
					{
						kingdom.AddResources(KingdomAI.Expense.Category.Economy, resource);
					}
				}
			}
			Reset();
			state = State.Canceled;
			Owner.NotifyListeners("build_canceled");
			if (send_state)
			{
				Owner.SendState<BuildState>();
			}
		}

		public void OnAnalyticsConstructionStarted()
		{
			Kingdom kingdom = Owner.GetKingdom();
			if (kingdom == null || !kingdom.is_local_player || Game.isLoadingSaveGame || current_building_def == null || current_building_def.IsUpgrade() || Owner == null)
			{
				return;
			}
			Vars vars = new Vars();
			vars.Set("province", Owner.GetRealm().name);
			vars.Set("buildingSlot", prefered_slot_index);
			vars.Set("buildingSlotsLeft", Owner.MaxBuildingSlots() - Owner.NumBuildings(include_planned: false) - 1);
			vars.Set("buildingName", current_building_def.id);
			Resource cost = current_building_def.GetCost(Owner);
			if (cost != null)
			{
				vars.Set("hammersCost", (int)cost[ResourceType.Hammers]);
				vars.Set("goldCost", (int)cost[ResourceType.Gold]);
				vars.Set("foodCost", (int)cost[ResourceType.Food]);
				vars.Set("bookCost", (int)cost[ResourceType.Books]);
				vars.Set("pietyCost", (int)cost[ResourceType.Piety]);
				vars.Set("tradeCost", (int)cost[ResourceType.Trade]);
				vars.Set("levyCost", (int)cost[ResourceType.Levy]);
			}
			List<District.Def> districts = current_building_def.districts;
			List<Building.Def> list = new List<Building.Def>();
			if (districts != null)
			{
				for (int i = 0; i < districts.Count; i++)
				{
					List<Building.Def> prerequisites = current_building_def.GetPrerequisites(districts[i]);
					if (prerequisites != null)
					{
						list.AddRange(prerequisites);
					}
				}
			}
			List<Building.Def.RequirementInfo> requires = current_building_def.requires;
			int num = (requires?.Count ?? 0) + list.Count;
			for (int j = 0; j < num; j++)
			{
				if (j < list.Count)
				{
					vars.Set($"requirement{j + 1}", list[j].id);
					continue;
				}
				int index = j - list.Count;
				vars.Set($"requirement{j + 1}", requires[index].key);
			}
			kingdom.NotifyListeners("analytics_building_started", vars);
		}

		public void BeginBuild(Building.Def building_def, int slot_index, bool send_state = true)
		{
			if (building_def != null && !IsBuilding())
			{
				current_building_def = building_def;
				if (current_building_def != null && current_building_def.cost != null)
				{
					production_cost = current_building_def.cost[ResourceType.Hammers];
				}
				else
				{
					Game.Log(Object.ToString(building_def) + " has no hammers cost", Game.LogType.Warning);
					production_cost = 1f;
				}
				current_progress = 0f;
				current_production_amount = 0f;
				prefered_slot_index = slot_index;
				state = State.Started;
				Owner.NotifyListeners("build_started", building_def);
				OnAnalyticsConstructionStarted();
				if (send_state)
				{
					Owner.SendState<BuildState>();
				}
			}
		}

		private bool IsFreeSlot(List<Building> buildings, int index)
		{
			if (buildings == null)
			{
				return true;
			}
			if (buildings.Count <= index)
			{
				return true;
			}
			if (buildings[index] == null)
			{
				return true;
			}
			return false;
		}

		public void BeginRepair(Building building, bool send_state = true)
		{
			if (building != null && !IsBuilding())
			{
				production_cost = Owner.burned_buildings[0].GetRepairCost();
				current_building_repair = building;
				if (!keep_progress)
				{
					current_progress = 0f;
					current_production_amount = 0f;
				}
				keep_progress = false;
				state = State.Started;
				Owner.NotifyListeners("build_started", building);
				if (send_state)
				{
					Owner.SendState<BuildState>();
				}
			}
		}

		public void CheckDependency(Building b)
		{
			_ = b?.def;
		}

		public void Reset()
		{
			current_building_def = null;
			current_building_repair = null;
			production_cost = -1f;
			if (!keep_progress)
			{
				current_production_amount = -1f;
				current_progress = -1f;
			}
			prefered_slot_index = -1;
		}

		public bool IsBuilding()
		{
			if (current_building_def == null)
			{
				return current_building_repair != null;
			}
			return true;
		}
	}

	[Serialization.State(31)]
	public class ArmyState : Serialization.ObjectState
	{
		public NID army_nid;

		public static ArmyState Create()
		{
			return new ArmyState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Castle).army != null;
		}

		public override bool InitFrom(Object obj)
		{
			Army army = (obj as Castle).army;
			army_nid = army;
			return army != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Army>(army_nid, "army_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			army_nid = ser.ReadNID<Army>("army_nid");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Castle).SetArmy(army_nid.Get<Army>(obj.game), send_state: false);
		}
	}

	[Serialization.State(32)]
	public class GovernorState : Serialization.ObjectState
	{
		public NID governor_nid;

		public static GovernorState Create()
		{
			return new GovernorState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Castle).governor != null;
		}

		public override bool InitFrom(Object obj)
		{
			Character governor = (obj as Castle).governor;
			governor_nid = governor;
			return governor != null;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(governor_nid, "governor_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			governor_nid = ser.ReadNID<Character>("governor_nid");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Castle).SetGovernor(governor_nid.Get<Character>(obj.game), reloadLabel: true, send_state: false);
		}
	}

	[Serialization.State(33)]
	public class StructuresState : Serialization.ObjectState
	{
		public struct BuildingData
		{
			public string def_id;

			public string state;

			public BuildingData(Building b)
			{
				def_id = b?.def?.id;
				state = b?.state.ToString();
			}
		}

		public List<BuildingData> buildings;

		public int slots_tier;

		public static StructuresState Create()
		{
			return new StructuresState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		private List<BuildingData> GetBuildingsData(List<Building> buildings)
		{
			if (buildings == null)
			{
				return null;
			}
			int count = buildings.Count;
			List<BuildingData> list = new List<BuildingData>(count);
			for (int i = 0; i < count; i++)
			{
				Building b = buildings[i];
				BuildingData item = new BuildingData(b);
				list.Add(item);
			}
			return list;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			_ = castle.buildings;
			buildings = GetBuildingsData(castle.buildings);
			slots_tier = castle.GetTier();
			return true;
		}

		private void WriteBuildingsData(Serialization.IWriter ser, List<BuildingData> buildings, string count_key, string def_id_key, string state_key)
		{
			int num = buildings?.Count ?? 0;
			ser.Write7BitUInt(num, count_key);
			for (int i = 0; i < num; i++)
			{
				BuildingData buildingData = buildings[i];
				ser.WriteStr(buildingData.def_id, def_id_key, i);
				if (!string.IsNullOrEmpty(buildingData.def_id))
				{
					ser.WriteStr(buildingData.state, state_key, i);
				}
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			WriteBuildingsData(ser, buildings, "buildings", "building", "building_state");
			ser.Write7BitUInt(slots_tier, "slots_tier");
		}

		private List<BuildingData> ReadBuildingsData(Serialization.IReader ser, string count_key, string def_id_key, string state_key)
		{
			int num = ser.Read7BitUInt(count_key);
			if (num <= 0)
			{
				return null;
			}
			List<BuildingData> list = new List<BuildingData>(num);
			for (int i = 0; i < num; i++)
			{
				BuildingData item = new BuildingData
				{
					def_id = ser.ReadStr(def_id_key, i)
				};
				if (!string.IsNullOrEmpty(item.def_id))
				{
					item.state = ser.ReadStr(state_key, i);
				}
				list.Add(item);
			}
			return list;
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			buildings = ReadBuildingsData(ser, "buildings", "building", "building_state");
			slots_tier = ser.Read7BitUInt("slots_tier");
		}

		private void OnStructuresStateAnalytics(Castle castle, List<BuildingData> data, List<Building> buildings)
		{
			if (!castle.GetKingdom().is_local_player)
			{
				return;
			}
			for (int i = 0; i < buildings.Count; i++)
			{
				Building building = buildings[i];
				if (building == null || building.def.IsUpgrade())
				{
					continue;
				}
				bool flag = false;
				for (int j = 0; j < data.Count; j++)
				{
					if (data[j].def_id == building.def.id)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					castle.OnAnalyticsBuildingRemoved(building);
				}
			}
		}

		private void ApplyBuildingsData(Castle castle, List<BuildingData> data, List<Building> buildings)
		{
			OnStructuresStateAnalytics(castle, data, buildings);
			buildings.Clear();
			if (data == null)
			{
				return;
			}
			for (int i = 0; i < data.Count; i++)
			{
				BuildingData buildingData = data[i];
				if (string.IsNullOrEmpty(buildingData.def_id))
				{
					buildings.Add(null);
					continue;
				}
				Building.Def def = castle.game.defs.Find<Building.Def>(buildingData.def_id);
				if (def == null)
				{
					buildings.Add(null);
					continue;
				}
				if (!Enum.TryParse<Building.State>(buildingData.state, out var result))
				{
					result = Building.State.Working;
				}
				if (result == Building.State.Working)
				{
					result = Building.State.TemporaryDeactivated;
				}
				Building item = new Building(castle, def, result);
				buildings.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			Realm realm = castle.GetRealm();
			realm?.DeactivateBuildings(temporary: false);
			ApplyBuildingsData(castle, buildings, castle.buildings);
			castle.SetTier(slots_tier, send_state: false);
			castle.init_structures = true;
			castle.NotifyListeners("structures_changed");
			castle.MatchResourceInfoBuildingInstances();
			realm?.RefreshTags();
			realm?.RefreshSkills(null);
			Kingdom kingdom = realm?.GetKingdom();
			if (kingdom != null && kingdom.started)
			{
				kingdom.RecalcBuildingStates();
			}
		}
	}

	[Serialization.State(34)]
	public class BuildState : Serialization.ObjectState
	{
		public Build.State state = Build.State.Finished;

		public bool isBuilding;

		public float current_progress = -1f;

		public int prefered_slot_index = -1;

		public string current_build_def_id = "";

		public string current_repair_def_id = "";

		public static BuildState Create()
		{
			return new BuildState();
		}

		public static bool IsNeeded(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.structure_build != null)
			{
				return castle.buildings != null;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.structure_build == null || castle.buildings == null)
			{
				return false;
			}
			state = castle.structure_build.GetState();
			isBuilding = castle.structure_build.IsBuilding();
			if (!isBuilding)
			{
				return false;
			}
			current_progress = castle.structure_build.current_progress;
			prefered_slot_index = castle.structure_build.prefered_slot_index;
			if (castle.structure_build.current_building_def != null)
			{
				current_build_def_id = castle.structure_build.current_building_def.dt_def.path;
			}
			if (castle.structure_build.current_building_repair != null)
			{
				current_repair_def_id = castle.structure_build.current_building_repair.def.dt_def.path;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(isBuilding, "isBuilding");
			ser.Write7BitUInt((int)state, "state");
			if (isBuilding)
			{
				ser.WriteFloat(current_progress, "current_progress");
				ser.Write7BitUInt(prefered_slot_index + 1, "prefered_slot_index");
				ser.WriteStr(current_build_def_id, "current_build_def_id");
				ser.WriteStr(current_repair_def_id, "current_repair_def_id");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			isBuilding = ser.ReadBool("isBuilding");
			state = (Build.State)ser.Read7BitUInt("state");
			if (isBuilding)
			{
				current_progress = ser.ReadFloat("current_progress");
				prefered_slot_index = ser.Read7BitUInt("prefered_slot_index") - 1;
				current_build_def_id = ser.ReadStr("current_build_def_id");
				current_repair_def_id = ser.ReadStr("current_repair_def_id");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			bool flag = true;
			if (castle.buildings == null)
			{
				return;
			}
			if (castle.structure_build == null)
			{
				castle.structure_build = new Build(castle);
			}
			else
			{
				Building.Def currentBuildingBuild = castle.GetCurrentBuildingBuild();
				if (currentBuildingBuild != null && (currentBuildingBuild.id == current_build_def_id || currentBuildingBuild.id == current_repair_def_id))
				{
					flag = false;
				}
				castle.structure_build.Reset();
			}
			if (!isBuilding)
			{
				if (state == Build.State.Canceled)
				{
					castle.NotifyListeners("build_canceled");
				}
				else
				{
					castle.NotifyListeners("build_finished");
				}
				return;
			}
			castle.structure_build.prefered_slot_index = prefered_slot_index;
			float num = 0f;
			if (current_build_def_id != "")
			{
				Building.Def def = obj.game.defs.Find<Building.Def>(current_build_def_id);
				if (def != null)
				{
					castle.structure_build.current_building_def = obj.game.defs.Find<Building.Def>(current_build_def_id);
					if (def.cost != null)
					{
						num = def.cost[ResourceType.Hammers];
					}
				}
			}
			if (current_repair_def_id != "")
			{
				Building.Def def2 = obj.game.defs.Get<Building.Def>(current_repair_def_id);
				Building current_building_repair = null;
				if (def2 != null && castle.buildings != null)
				{
					current_building_repair = castle.buildings.Find((Building x) => x != null && x.def != null && x.def.id == current_repair_def_id);
					if (def2.cost != null)
					{
						num = def2.cost[ResourceType.Hammers];
					}
				}
				castle.structure_build.current_building_repair = current_building_repair;
			}
			castle.structure_build.production_cost = num;
			castle.structure_build.current_production_amount = current_progress * num;
			castle.structure_build.current_progress = current_progress;
			if (state != Build.State.Started)
			{
				return;
			}
			if (castle.structure_build.current_building_repair == null)
			{
				if (flag)
				{
					castle.NotifyListeners("build_started", castle.structure_build.GetBuild());
					castle.structure_build.OnAnalyticsConstructionStarted();
				}
			}
			else
			{
				castle.NotifyListeners("repair_progress");
			}
		}
	}

	[Serialization.State(35)]
	public class BuildProgressState : Serialization.ObjectState
	{
		public float current_progress = -1f;

		public static BuildProgressState Create()
		{
			return new BuildProgressState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (!castle.structure_build.IsBuilding())
			{
				return false;
			}
			current_progress = castle.structure_build.current_progress;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(current_progress, "current_progress");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			current_progress = ser.ReadFloat("current_progress");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.structure_build != null)
			{
				castle.structure_build.current_progress = current_progress;
			}
		}
	}

	[Serialization.State(36)]
	public class SackedStructuresState : Serialization.ObjectState
	{
		public List<string> struct_defs = new List<string>();

		public float initial_sack_damage;

		public float sack_damage;

		public static SackedStructuresState Create()
		{
			return new SackedStructuresState();
		}

		public static bool IsNeeded(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.burned_buildings == null || castle.burned_buildings.Count == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.burned_buildings == null)
			{
				return false;
			}
			for (int i = 0; i < castle.burned_buildings.Count; i++)
			{
				struct_defs.Add(castle.burned_buildings[i].def.id);
			}
			initial_sack_damage = castle.initial_sack_damage;
			sack_damage = castle.sack_damage;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = struct_defs.Count;
			ser.Write7BitUInt(count, "count");
			if (count > 0)
			{
				for (int i = 0; i < count; i++)
				{
					ser.WriteStr(struct_defs[i], "struct_def", i);
				}
			}
			ser.WriteFloat(initial_sack_damage, "initial_sack_damage");
			ser.WriteFloat(sack_damage, "sack_damage");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					struct_defs.Add(ser.ReadStr("struct_def", i));
				}
			}
			initial_sack_damage = ser.ReadFloat("initial_sack_damage");
			sack_damage = ser.ReadFloat("sack_damage");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.burned_buildings != null)
			{
				castle.burned_buildings.Clear();
			}
			else
			{
				castle.burned_buildings = new List<Building>();
			}
			int count = struct_defs.Count;
			if (Serialization.cur_version >= 6)
			{
				castle.initial_sack_damage = 0f;
				castle.sack_damage = 0f;
				for (int i = 0; i < count; i++)
				{
					Building.Def def = obj.game.defs.Find<Building.Def>(struct_defs[i]);
					Building building = castle.FindBuilding(def);
					if (building != null && !building.IsWorking())
					{
						building.SetState(Building.State.TemporaryDeactivated);
					}
				}
				castle.sacked = false;
			}
			else
			{
				castle.initial_sack_damage = initial_sack_damage;
				castle.sack_damage = sack_damage;
				for (int j = 0; j < count; j++)
				{
					Building.Def def2 = obj.game.defs.Find<Building.Def>(struct_defs[j]);
					Building building2 = castle.FindBuilding(def2);
					if (building2 != null)
					{
						castle.burned_buildings.Add(building2);
						if (building2.IsWorking())
						{
							building2.Deactivate();
						}
					}
				}
				castle.sacked = sack_damage > 0f || castle.burned_buildings.Count != 0;
			}
			castle.NotifyListeners("repair_started");
			castle.NotifyListeners("structures_sacked");
			if (!castle.sacked)
			{
				castle.sacking_comp?.Reset();
			}
			if (!castle.sacked || sack_damage != initial_sack_damage)
			{
				castle.NotifyListeners("repair_progress");
				castle.NotifyListeners("structures_changed");
			}
		}
	}

	[Serialization.State(37)]
	public class SackDamageState : Serialization.ObjectState
	{
		public float sack_damage;

		public static SackDamageState Create()
		{
			return new SackDamageState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			sack_damage = castle.sack_damage;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(sack_damage, "sack_damage");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			sack_damage = ser.ReadFloat("sack_damage");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			castle.sack_damage = sack_damage;
			castle.sacked = sack_damage > 0f || castle.burned_buildings.Count != 0;
			castle.NotifyListeners("repair_progress");
		}
	}

	[Serialization.State(38)]
	public class PopulationState : Serialization.ObjectState
	{
		public int rebels;

		public int workers;

		public float pop_acc;

		public float rebelion_acc;

		public int rebelion_risk;

		public static PopulationState Create()
		{
			return new PopulationState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle?.population == null)
			{
				return false;
			}
			rebels = castle.population.GetRebels();
			workers = castle.population.GetWorkers();
			pop_acc = castle.population.pop_acc;
			rebelion_acc = castle.population.rebelion_acc;
			rebelion_risk = castle.rebelion_risk;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(rebels, "rebels");
			ser.Write7BitUInt(workers, "workers");
			ser.WriteFloat(pop_acc, "pop_acc");
			ser.WriteFloat(rebelion_acc, "rebelion_acc");
			ser.Write7BitUInt(rebelion_risk, "rebelion_risk");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			rebels = ser.Read7BitUInt("rebels");
			workers = ser.Read7BitUInt("workers");
			pop_acc = ser.ReadFloat("pop_acc");
			if (Serialization.cur_version >= 17)
			{
				rebelion_acc = ser.ReadFloat("rebelion_acc");
				rebelion_risk = ser.Read7BitUInt("rebelion_risk");
				return;
			}
			rebelion_acc = 1f * (float)rebels / (1f + (float)rebels + (float)workers);
			rebelion_risk = 0;
			if (rebelion_acc > 0.75f)
			{
				rebelion_risk = 2;
			}
			else if (rebelion_acc > 0.15f)
			{
				rebelion_risk = 1;
			}
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.population == null)
			{
				new Population(castle);
			}
			castle.population.rebels = rebels;
			castle.population.workers = workers;
			castle.population.pop_acc = pop_acc;
			castle.population.rebelion_acc = rebelion_acc;
			castle.population.Recalc();
			castle.NotifyListeners("population_changed");
			castle.rebelion_risk = rebelion_risk;
			castle.NotifyListeners("rebelion_risk_changed");
		}
	}

	[Serialization.State(39)]
	public class FoodState : Serialization.ObjectState
	{
		public float food;

		public static FoodState Create()
		{
			return new FoodState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			food = castle.GetFoodStorage();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(food, "last_food_value");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			food = ser.ReadFloat("last_food_value");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Castle).SetFood(food, clamp: false, send_state: false);
		}
	}

	[Serialization.State(40)]
	public class BoostCastleDefenceState : Serialization.ObjectState
	{
		private bool boosted;

		public static BoostCastleDefenceState Create()
		{
			return new BoostCastleDefenceState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle?.keep_effects == null)
			{
				return false;
			}
			boosted = castle?.keep_effects.active_defence_recovery_boost ?? false;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(boosted, "boosted");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			boosted = ser.ReadBool("boosted");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.keep_effects == null)
			{
				castle.InitKeepEffects();
			}
			if (!boosted && castle.keep_effects.active_defence_recovery_boost)
			{
				castle.NotifyListeners("fortification_repair_boost_complete");
			}
			castle.keep_effects.active_defence_recovery_boost = boosted;
		}
	}

	[Serialization.State(41)]
	public class CastleFortificationsState : Serialization.ObjectState
	{
		private int level;

		public static CastleFortificationsState Create()
		{
			return new CastleFortificationsState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Castle castle))
			{
				return false;
			}
			return castle.fortifications.level > 0;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Castle castle))
			{
				return false;
			}
			level = castle.fortifications.level;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(level, "level");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			level = ser.Read7BitUInt("level");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Castle castle)
			{
				if (castle.fortifications == null)
				{
					castle.fortifications = new Fortifications(castle, castle.game.defs.GetBase<Fortifications.Def>());
				}
				castle.fortifications.SetLevel(level);
			}
		}
	}

	[Serialization.State(42)]
	public class ForitificationUpgradeState : Serialization.ObjectState
	{
		public bool isUpgardeing;

		public float current_progress = -1f;

		public Fortifications.UpgradeState state = Fortifications.UpgradeState.Finished;

		public static ForitificationUpgradeState Create()
		{
			return new ForitificationUpgradeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Castle).fortifications != null;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.fortifications == null)
			{
				return false;
			}
			isUpgardeing = castle.fortifications.IsUpgrading();
			if (!isUpgardeing)
			{
				return false;
			}
			state = castle.fortifications.GetUpgradeState();
			current_progress = castle.fortifications.current_progress;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(isUpgardeing, "isUpgardeing");
			ser.Write7BitUInt((int)state, "state");
			if (isUpgardeing)
			{
				ser.WriteFloat(current_progress, "current_progress");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			isUpgardeing = ser.ReadBool("isUpgardeing");
			state = (Fortifications.UpgradeState)ser.Read7BitUInt("state");
			if (isUpgardeing)
			{
				current_progress = ser.ReadFloat("current_progress");
			}
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.fortifications == null)
			{
				castle.fortifications = new Fortifications(castle, castle.game.defs.GetBase<Fortifications.Def>());
			}
			if (!isUpgardeing)
			{
				if (state == Fortifications.UpgradeState.Canceled)
				{
					castle.NotifyListeners("fortification_upgrade_canceled");
				}
				else
				{
					castle.NotifyListeners("fortification_upgrade_complete");
				}
				castle.fortifications.SetUpgradeState(state);
				return;
			}
			float num = 0f;
			num = castle.fortifications.GetUpgradeCost(castle.fortifications.level + 1)[ResourceType.Hammers];
			castle.fortifications.production_cost = num;
			castle.fortifications.current_production_amount = current_progress * num;
			castle.fortifications.current_progress = current_progress;
			if (state == Fortifications.UpgradeState.Started && castle.fortifications.GetUpgradeState() != state)
			{
				castle.NotifyListeners("fortification_upgarde_started");
			}
			castle.fortifications.SetUpgradeState(state);
		}
	}

	[Serialization.State(43)]
	public class MoraleState : Serialization.ObjectState
	{
		private Data temporary_morale_data;

		public static MoraleState Create()
		{
			return new MoraleState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.morale?.temporary_morale == null)
			{
				return false;
			}
			temporary_morale_data = castle.morale.temporary_morale.CreateData();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(temporary_morale_data, "temporary_morale_data");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			temporary_morale_data = ser.ReadData("temporary_morale_data");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.morale == null)
			{
				castle.morale = new Morale(castle);
			}
			if (castle.morale.temporary_morale != null)
			{
				temporary_morale_data.ApplyTo(castle.morale.temporary_morale, castle.game);
			}
		}
	}

	[Serialization.Event(47)]
	public class BuildEvent : Serialization.ObjectEvent
	{
		public string structure_def_id;

		public int slot_index;

		public bool instant;

		public bool planned;

		public BuildEvent()
		{
		}

		public static BuildEvent Create()
		{
			return new BuildEvent();
		}

		public BuildEvent(Building.Def structure_def, int slot_index, bool instant, bool planned)
		{
			structure_def_id = structure_def.id;
			this.slot_index = slot_index;
			this.instant = instant;
			this.planned = planned;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(structure_def_id, "structure_def_id");
			ser.Write7BitUInt(slot_index + 1, "slot_index");
			ser.WriteBool(instant, "instant");
			ser.WriteBool(planned, "planned");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			structure_def_id = ser.ReadStr("structure_def_id");
			slot_index = ser.Read7BitUInt("slot_index") - 1;
			instant = ser.ReadBool("instant");
			planned = ser.ReadBool("planned");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (structure_def_id != null)
			{
				if (castle.structure_build == null)
				{
					castle.structure_build = new Build(castle);
				}
				Building.Def def = obj.game.defs.Get<Building.Def>(structure_def_id);
				if (planned)
				{
					castle.PlanBuilding(def, slot_index, instant);
				}
				else
				{
					castle.BuildBuilding(def, slot_index, instant);
				}
			}
		}
	}

	[Serialization.Event(48)]
	public class RemoveBuildingEvent : Serialization.ObjectEvent
	{
		public int slot_idx;

		public string def_id;

		public RemoveBuildingEvent()
		{
		}

		public static RemoveBuildingEvent Create()
		{
			return new RemoveBuildingEvent();
		}

		public RemoveBuildingEvent(int slot_idx, Building.Def def)
		{
			this.slot_idx = slot_idx;
			def_id = def.id;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(slot_idx, "slot_idx");
			ser.WriteStr(def_id, "def_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			slot_idx = ser.Read7BitSigned("slot_idx");
			def_id = ser.ReadStr("def_id");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.buildings == null)
			{
				return;
			}
			Kingdom kingdom = castle.GetKingdom();
			Building building = castle.GetBuilding(slot_idx);
			if (building == null || building.def?.id != def_id)
			{
				Building.Def def = obj.game.defs.Get<Building.Def>(def_id);
				if (def != null && def.IsUpgrade())
				{
					kingdom?.RemovePlanedUpgrade(def);
					return;
				}
				building = castle.FindBuilding(def);
			}
			castle.RemoveBuilding(building);
		}
	}

	[Serialization.Event(49)]
	public class CancelBuildEvent : Serialization.ObjectEvent
	{
		public static CancelBuildEvent Create()
		{
			return new CancelBuildEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle.structure_build == null)
			{
				castle.structure_build = new Build(castle);
			}
			castle.CancelBuild();
		}
	}

	[Serialization.Event(51)]
	public class ResupplyArmyEvent : Serialization.ObjectEvent
	{
		public NID army_nid;

		public static ResupplyArmyEvent Create()
		{
			return new ResupplyArmyEvent();
		}

		public ResupplyArmyEvent()
		{
		}

		public ResupplyArmyEvent(Army a)
		{
			army_nid = a;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Army>(army_nid, "army_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			army_nid = ser.ReadNID<Army>("army_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Castle obj2 = obj as Castle;
			Army army = army_nid.Get<Army>(obj.game);
			obj2.ResupplyArmy(army);
		}
	}

	[Serialization.Event(52)]
	public class HealArmyEvent : Serialization.ObjectEvent
	{
		public NID army_nid;

		public static HealArmyEvent Create()
		{
			return new HealArmyEvent();
		}

		public HealArmyEvent()
		{
		}

		public HealArmyEvent(Army a)
		{
			army_nid = a;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Army>(army_nid, "army_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			army_nid = ser.ReadNID<Army>("army_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Castle obj2 = obj as Castle;
			army_nid.Get<Army>(obj.game);
			obj2.HealArmy();
		}
	}

	[Serialization.Event(53)]
	public class HireUnitEvent : Serialization.ObjectEvent
	{
		private string def;

		private NID army_nid;

		private int slot_index;

		public HireUnitEvent()
		{
		}

		public static HireUnitEvent Create()
		{
			return new HireUnitEvent();
		}

		public HireUnitEvent(Unit.Def def, Army army, int slot_index)
		{
			this.def = def.field.key;
			army_nid = army;
			this.slot_index = slot_index + 1;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def, "def");
			ser.WriteNID<Army>(army_nid, "army_nid");
			ser.Write7BitUInt(slot_index, "slot_index");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def = ser.ReadStr("def");
			army_nid = ser.ReadNID<Army>("army_nid");
			slot_index = ser.Read7BitUInt("slot_index");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			Unit.Def def = obj.game.defs.Get<Unit.Def>(this.def);
			Army army = army_nid.Get<Army>(obj.game);
			if (army != null && def != null)
			{
				castle.HireUnit(def, army, slot_index);
			}
		}
	}

	[Serialization.Event(54)]
	public class HireGarrisonUnitEvent : Serialization.ObjectEvent
	{
		private string def;

		public HireGarrisonUnitEvent()
		{
		}

		public static HireGarrisonUnitEvent Create()
		{
			return new HireGarrisonUnitEvent();
		}

		public HireGarrisonUnitEvent(Unit.Def def)
		{
			this.def = def.field.key;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def, "def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def = ser.ReadStr("def");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			Unit.Def def = obj.game.defs.Get<Unit.Def>(this.def);
			if (def != null && castle.garrison != null)
			{
				castle.garrison.Hire(def);
			}
		}
	}

	[Serialization.Event(55)]
	public class DelGarrisonUnitEvent : Serialization.ObjectEvent
	{
		private int unit_idx = -1;

		public DelGarrisonUnitEvent()
		{
		}

		public static DelGarrisonUnitEvent Create()
		{
			return new DelGarrisonUnitEvent();
		}

		public DelGarrisonUnitEvent(int idx)
		{
			unit_idx = idx;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(unit_idx + 1, "unit_idx");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			unit_idx = ser.Read7BitUInt("unit_idx") - 1;
		}

		public override void ApplyTo(Object obj)
		{
			Garrison garrison = (obj as Castle).garrison;
			if (garrison != null && garrison.units != null)
			{
				garrison.DelUnit(unit_idx);
			}
		}
	}

	[Serialization.Event(56)]
	public class ExpandEvent : Serialization.ObjectEvent
	{
		private int tier;

		public ExpandEvent()
		{
		}

		public static ExpandEvent Create()
		{
			return new ExpandEvent();
		}

		public ExpandEvent(int new_tier)
		{
			tier = new_tier;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(tier, "tier");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			tier = ser.Read7BitUInt("tier");
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Castle castle))
			{
				return;
			}
			Kingdom kingdom = castle.GetKingdom();
			if (kingdom != null)
			{
				Resource expandCost = castle.GetExpandCost();
				if (!(expandCost != null) || kingdom.resources.CanAfford(expandCost, 1f))
				{
					kingdom.SubResources(KingdomAI.Expense.Category.Economy, expandCost);
					castle.SetTier(tier);
				}
			}
		}
	}

	[Serialization.Event(57)]
	public class BoostSiegeDefenceRepairEvent : Serialization.ObjectEvent
	{
		public BoostSiegeDefenceRepairEvent()
		{
		}

		public static BoostSiegeDefenceRepairEvent Create()
		{
			return new BoostSiegeDefenceRepairEvent();
		}

		public BoostSiegeDefenceRepairEvent(int new_tier)
		{
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Castle castle && castle.GetKingdom() != null && castle.FortificationsNeedRepair())
			{
				castle.BoostSiegeDefenceRepairs();
			}
		}
	}

	[Serialization.Event(58)]
	public class BuyEquipmentEvent : Serialization.ObjectEvent
	{
		private string def;

		private NID army_nid;

		private int slot_index;

		public BuyEquipmentEvent()
		{
		}

		public static BuyEquipmentEvent Create()
		{
			return new BuyEquipmentEvent();
		}

		public BuyEquipmentEvent(Unit.Def def, Army army, int slot_index)
		{
			this.def = def.field.key;
			army_nid = army;
			this.slot_index = slot_index + 1;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def, "def");
			ser.WriteNID<Army>(army_nid, "army_nid");
			ser.Write7BitUInt(slot_index, "slot_index");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def = ser.ReadStr("def");
			army_nid = ser.ReadNID<Army>("army_nid");
			slot_index = ser.Read7BitUInt("slot_index");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			Unit.Def def = obj.game.defs.Get<Unit.Def>(this.def);
			Army army = army_nid.Get<Army>(obj.game);
			if (army != null && def != null)
			{
				castle.BuyEquipments(def, army, slot_index);
			}
		}
	}

	[Serialization.Event(59)]
	public class FortificationUpgradeEvent : Serialization.ObjectEvent
	{
		public static FortificationUpgradeEvent Create()
		{
			return new FortificationUpgradeEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Castle castle)
			{
				if (castle.fortifications == null)
				{
					castle.fortifications = new Fortifications(castle, castle.game.defs.GetBase<Fortifications.Def>());
				}
				if (castle.CanUpgradeFortification())
				{
					castle.UpgradeFortification();
				}
			}
		}
	}

	[Serialization.Event(60)]
	public class HealGarrisonUnitsEvent : Serialization.ObjectEvent
	{
		public static HealGarrisonUnitsEvent Create()
		{
			return new HealGarrisonUnitsEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Castle castle)
			{
				castle.HealGarrisonUnits();
			}
		}
	}

	[Serialization.Event(61)]
	public class HealGarrisonUnitEvent : Serialization.ObjectEvent
	{
		private int slot_index;

		public HealGarrisonUnitEvent()
		{
		}

		public static HealGarrisonUnitEvent Create()
		{
			return new HealGarrisonUnitEvent();
		}

		public HealGarrisonUnitEvent(int slot_index)
		{
			this.slot_index = slot_index;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(slot_index, "slot_index");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			slot_index = ser.Read7BitUInt("slot_index");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Castle castle)
			{
				castle.HealGarrisonUnit(slot_index);
			}
		}
	}

	[Serialization.Event(62)]
	public class SwapBuildingSlotsEvent : Serialization.ObjectEvent
	{
		private int idx1;

		private int idx2;

		public SwapBuildingSlotsEvent()
		{
		}

		public static SwapBuildingSlotsEvent Create()
		{
			return new SwapBuildingSlotsEvent();
		}

		public SwapBuildingSlotsEvent(int index1, int index2)
		{
			idx1 = index1;
			idx2 = index2;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(idx1, "index1");
			ser.Write7BitUInt(idx2, "index2");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			idx1 = ser.Read7BitUInt("index1");
			idx2 = ser.Read7BitUInt("index2");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle?.buildings != null)
			{
				castle.SwapBuildingSlots(castle.buildings[idx1], castle.buildings[idx2]);
			}
		}
	}

	[Serialization.Event(63)]
	public class MoveBuildingToSlotEvent : Serialization.ObjectEvent
	{
		private int idx1;

		private int idx2;

		public MoveBuildingToSlotEvent()
		{
		}

		public static MoveBuildingToSlotEvent Create()
		{
			return new MoveBuildingToSlotEvent();
		}

		public MoveBuildingToSlotEvent(int index1, int index2)
		{
			idx1 = index1;
			idx2 = index2;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(idx1, "index1");
			ser.Write7BitUInt(idx2, "index2");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			idx1 = ser.Read7BitUInt("index1");
			idx2 = ser.Read7BitUInt("index2");
		}

		public override void ApplyTo(Object obj)
		{
			Castle castle = obj as Castle;
			if (castle?.buildings != null)
			{
				castle.MoveToBuildingSlot(castle.buildings[idx1], idx2);
			}
		}
	}

	[Serialization.Event(64)]
	public class TransferUnitEvent : Serialization.ObjectEvent
	{
		private int unit_idx;

		private int swap_unit_idx;

		private NID dest_nid;

		public TransferUnitEvent()
		{
		}

		public static TransferUnitEvent Create()
		{
			return new TransferUnitEvent();
		}

		public TransferUnitEvent(MapObject dest, int unit_idx, int swap_unit_idx)
		{
			this.unit_idx = unit_idx;
			this.swap_unit_idx = swap_unit_idx;
			dest_nid = dest;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(dest_nid, "dest_nid");
			ser.Write7BitSigned(unit_idx, "unit_idx");
			ser.Write7BitSigned(swap_unit_idx, "swap_unit_idx");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			dest_nid = ser.ReadNID("dest_nid");
			unit_idx = ser.Read7BitSigned("unit_idx");
			swap_unit_idx = ser.Read7BitSigned("swap_unit_idx");
		}

		public override void ApplyTo(Object obj)
		{
			Settlement obj2 = obj as Settlement;
			Unit unit = obj2.garrison.GetUnit(unit_idx);
			Unit swap_target = null;
			MapObject mapObject = dest_nid.GetObj(obj.game) as MapObject;
			if (mapObject is Castle)
			{
				swap_target = (mapObject as Castle).garrison.GetUnit(swap_unit_idx);
			}
			obj2.garrison.TransferUnit(mapObject, unit, swap_target);
		}
	}

	[Serialization.Event(65)]
	public class MergeUnitsEvent : Serialization.ObjectEvent
	{
		private int source_unit_idx;

		private int dest_unit_idx;

		private NID dest_nid;

		public MergeUnitsEvent()
		{
		}

		public static MergeUnitsEvent Create()
		{
			return new MergeUnitsEvent();
		}

		public MergeUnitsEvent(int source_unit_idx, int dest_unit_idx, MapObject target_owner)
		{
			this.source_unit_idx = source_unit_idx;
			this.dest_unit_idx = dest_unit_idx;
			dest_nid = target_owner;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(source_unit_idx, "source_unit_idx");
			ser.Write7BitUInt(dest_unit_idx, "dest_unit_idx");
			ser.WriteNID(dest_nid, "dest_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			source_unit_idx = ser.Read7BitUInt("source_unit_idx");
			dest_unit_idx = ser.Read7BitUInt("dest_unit_idx");
			dest_nid = ser.ReadNID("dest_nid");
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Settlement settlement))
			{
				return;
			}
			Object obj2 = dest_nid.GetObj(obj.game);
			if (obj2 != null)
			{
				Unit target = null;
				if (obj2 is Castle)
				{
					Castle castle = obj2 as Castle;
					target = ((castle?.garrison?.units == null || !(castle?.garrison?.units.Count > dest_unit_idx)) ? null : castle?.garrison?.units[dest_unit_idx]);
				}
				settlement.garrison.MergeUnits(settlement.garrison.units[source_unit_idx], target, send_state: true);
			}
		}
	}

	public List<AIBuildingReservation> ai_building_reservations;

	public static List<BuildOption> build_options = new List<BuildOption>();

	public static float build_options_sum = 0f;

	public static List<BuildOption> upgrade_options = new List<BuildOption>();

	public static float upgrade_options_sum = 0f;

	public static List<BuildOption> last_build_options = new List<BuildOption>();

	public static List<BuildOption> last_upgrade_options = new List<BuildOption>();

	private static Vars tmp_vars = new Vars();

	private static Resource tmp_res = new Resource();

	private static bool in_spec_eval = false;

	private static Resource tmp_weights = new Resource();

	private static List<Building.Def> tmp_directly_buildable_prerequisites = new List<Building.Def>();

	private static List<Building.Def> tmp_indirectly_buildable_prerequisites = new List<Building.Def>();

	public Character ai_selected_governor;

	public string name;

	public string customName;

	public Character governor;

	public Army army;

	public List<Building> buildings = new List<Building>();

	public List<Building> upgrades = new List<Building>();

	private List<District.Def> buildable_districts;

	public Population population;

	public Morale morale;

	public AvailableUnits available_units;

	public Fortifications fortifications;

	private int building_slots_tier;

	public int max_citadel_levels = 4;

	public int max_level;

	public int rebelion_risk;

	private DT.Field siege_defence_per_point_cost;

	public float food_storage;

	public int citadel_level = 1;

	public Build structure_build;

	private bool init_structures;

	public bool sacked;

	public float sack_damage;

	public float initial_sack_damage;

	public List<Building> burned_buildings = new List<Building>();

	public SackingRepair sacking_comp;

	public bool quick_recovery;

	public float[,] production_coef = new float[2, 9];

	private float[] coef = new float[2] { 0.25f, 1f };

	public const float update_delay = 1f;

	public const float logic_tick = 5f;

	private bool del_army;

	public Dictionary<string, bool> may_produce_resource;

	private static List<Unit.Def> tmp_unit_defs = new List<Unit.Def>(32);

	private static List<Unit> tmp_to_take = new List<Unit>(32);

	private static List<Unit> tmp_to_disband = new List<Unit>(32);

	private const int STATES_IDX = 30;

	private const int EVENTS_IDX = 46;

	public Dictionary<string, ResourceInfo> resources_info;

	public bool resources_info_valid;

	public bool match_res_info_buildings;

	public static void ClearBuildOptions()
	{
		build_options.Clear();
		build_options_sum = 0f;
		upgrade_options.Clear();
		upgrade_options_sum = 0f;
	}

	private static int CompareBuildOptions(BuildOption a, BuildOption b)
	{
		return b.eval.CompareTo(a.eval);
	}

	public static void SortBuildOptions()
	{
		build_options.Sort(CompareBuildOptions);
		upgrade_options.Sort(CompareBuildOptions);
	}

	public Resource AISpecProductionWeights(Character governor = null, bool check_low_food = true)
	{
		Realm realm = GetRealm();
		if (realm == null)
		{
			return null;
		}
		AI.ProvinceSpecialization provinceSpecialization = realm.ai_specialization;
		if (governor == null)
		{
			governor = this.governor;
		}
		Kingdom kingdom = GetKingdom();
		if ((kingdom == null || kingdom.ai == null || kingdom.ai.personality == KingdomAI.AIPersonality.Default) && governor != null && governor.IsMarshal())
		{
			provinceSpecialization = AI.ProvinceSpecialization.Military;
		}
		tmp_weights.Set(game.ai.def.province_specialization_resource_weights[(int)provinceSpecialization], 1f);
		if (governor != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default)
		{
			tmp_weights.Set(ResourceType.Rebels, 0f);
		}
		if (check_low_food)
		{
			float food = GetKingdom().GetFood();
			if (food < game.ai.def.food_reserve)
			{
				if (food < 0f)
				{
					tmp_weights.Set(ResourceType.Food, game.ai.def.critical_food_weight);
				}
				else
				{
					tmp_weights.Set(ResourceType.Food, game.ai.def.low_food_weight);
				}
			}
		}
		return tmp_weights;
	}

	public float EvalAISpec(AI.ProvinceSpecialization ps)
	{
		ClearBuildOptions();
		in_spec_eval = true;
		AddBuildOptions(common_only: false, game.ai.def.province_specialization_resource_weights[(int)ps]);
		in_spec_eval = false;
		SortBuildOptions();
		float num = 0f;
		int num2 = MaxBuildingSlots();
		for (int i = 0; i < build_options.Count && i < num2; i++)
		{
			num += build_options[i].eval;
		}
		return num;
	}

	public AI.ProvinceSpecialization CalcBestAISpecialization()
	{
		AI.ProvinceSpecialization result = AI.ProvinceSpecialization.General;
		float num = 0f;
		for (AI.ProvinceSpecialization provinceSpecialization = AI.ProvinceSpecialization.General; provinceSpecialization < AI.ProvinceSpecialization.COUNT; provinceSpecialization++)
		{
			float num2 = EvalAISpec(provinceSpecialization);
			if (num2 > num)
			{
				result = provinceSpecialization;
				num = num2;
			}
		}
		return result;
	}

	public float EvalBuild(Building.Def def, Resource production_weights)
	{
		float num = 0f;
		if (!in_spec_eval)
		{
			num = ((def.ai_eval_field != null) ? def.ai_eval_field.Float(this, 100f) : 100f);
		}
		Kingdom kingdom = GetKingdom();
		Realm realm = GetRealm();
		if (realm != null && kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default)
		{
			if (def.ai_category == KingdomAI.Expense.Category.Military && kingdom.ai.personality == KingdomAI.AIPersonality.RichArmies)
			{
				num *= 3f;
			}
			else if (def.ai_category == KingdomAI.Expense.Category.Military && realm.ai_specialization != AI.ProvinceSpecialization.MilitarySpec)
			{
				num *= 0.1f;
			}
			if (def.ai_category == KingdomAI.Expense.Category.Military && realm.ai_specialization == AI.ProvinceSpecialization.MilitarySpec)
			{
				num *= 3f;
			}
		}
		int num2 = NumBuildings(include_planned: true);
		tmp_res.Clear();
		AI.ProductionEvaluationFutureWeights productionEvaluationFutureWeights = (in_spec_eval ? AI.ProductionEvaluationFutureWeights.Full : ((num2 >= 0 && num2 < game.ai.def.production_evaluation_future_weights.Length) ? game.ai.def.production_evaluation_future_weights[num2] : AI.ProductionEvaluationFutureWeights.Default));
		if (def.IsUpgrade())
		{
			def.CalcUpgradeTotalProduction(tmp_res, GetKingdom(), 3, check_condition: true, productionEvaluationFutureWeights.immediate, productionEvaluationFutureWeights.near_future, productionEvaluationFutureWeights.far_future, 2);
		}
		else
		{
			def.CalcProduction(tmp_res, this, 3, check_condition: true, productionEvaluationFutureWeights.immediate, productionEvaluationFutureWeights.near_future, productionEvaluationFutureWeights.far_future, 2);
			if (!in_spec_eval && governor == null)
			{
				tmp_res.Mul(game.ai.def.non_governed_production_eval_mul, ResourceType.Rebels);
			}
		}
		float num3 = tmp_res[ResourceType.Rebels];
		if (num3 > 0f && def.HasPFRequirement(count_settlements: true))
		{
			float num4 = 1f;
			if (num2 >= 0 && num2 < game.ai.def.unique_PF_goods_eval_multipliers.Length)
			{
				num4 = game.ai.def.unique_PF_goods_eval_multipliers[num2];
			}
			tmp_res.Set(ResourceType.Rebels, num3 * num4);
		}
		float num5 = tmp_res.Eval(production_weights);
		if (kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default && kingdom.ai.personality != KingdomAI.AIPersonality.RichArmies)
		{
			float num6 = def.GetCost(GetRealm(), kingdom).Eval(production_weights);
			num5 = 0.25f * num5 + 0.75f * num5 * 250000f / num6;
		}
		num += num5;
		if (kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default)
		{
			if (buildable_districts != null)
			{
				for (int i = 0; i < buildable_districts.Count; i++)
				{
					District.Def def2 = buildable_districts[i];
					if (def2 == null)
					{
						continue;
					}
					for (int j = 0; j < def2.buildings.Count; j++)
					{
						District.Def.BuildingInfo buildingInfo = def2.buildings[j];
						if (buildingInfo != null && buildingInfo.prerequisites != null && buildingInfo.prerequisites.Contains(def))
						{
							num += 500f;
						}
					}
				}
			}
			District.Def common = District.Def.GetCommon(game);
			if (common != null)
			{
				for (int k = 0; k < common.buildings.Count; k++)
				{
					District.Def.BuildingInfo buildingInfo2 = common.buildings[k];
					bool flag = true;
					if (buildingInfo2.def.requires != null)
					{
						for (int l = 0; l < buildingInfo2.def.requires.Count; l++)
						{
							if (!realm.HasTag(buildingInfo2.def.requires[l].key))
							{
								flag = false;
								break;
							}
						}
					}
					if (buildingInfo2 != null && buildingInfo2.prerequisites != null && buildingInfo2.prerequisites.Contains(def) && flag)
					{
						num += 2500f;
					}
				}
			}
			District.Def pF = District.Def.GetPF(game);
			if (pF != null)
			{
				for (int m = 0; m < pF.buildings.Count; m++)
				{
					District.Def.BuildingInfo buildingInfo3 = pF.buildings[m];
					bool flag2 = true;
					if (buildingInfo3.def.requires != null)
					{
						for (int n = 0; n < buildingInfo3.def.requires.Count; n++)
						{
							if (!realm.HasTag(buildingInfo3.def.requires[n].key))
							{
								flag2 = false;
								break;
							}
						}
					}
					if (buildingInfo3 != null && buildingInfo3.prerequisites != null && buildingInfo3.prerequisites.Contains(def) && flag2)
					{
						num += 5000f;
					}
				}
			}
		}
		if (kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default)
		{
			for (int num7 = 0; num7 < def.bonuses.Count; num7++)
			{
				Building.Def.Bonuses bonuses = def.bonuses[num7];
				if (bonuses == null || bonuses.stat_mods == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, Building.Def.PerSettlementModidfiers> stat_mod in bonuses.stat_mods)
				{
					if (stat_mod.Value?.realm_mods == null)
					{
						continue;
					}
					foreach (Building.StatModifier.Def realm_mod in stat_mod.Value.realm_mods)
					{
						if (realm_mod.stat_name == "rs_happiness" || realm_mod.stat_name == "rs_stability")
						{
							int num8 = Math.Min(Math.Max(kingdom.realms.Count - 3, 1), 6);
							num += realm_mod.value * (float)num8 * (float)num8 * 500f;
						}
					}
				}
			}
		}
		if (kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default && !def.IsUpgrade() && AvailableBuildingSlots() <= 3 && kingdom.HasBuilding(def))
		{
			num *= 0.2f;
			if (AvailableBuildingSlots() <= 1)
			{
				num *= 0.2f;
			}
		}
		if (realm != null && kingdom != null && kingdom.ai != null && kingdom.ai.personality != KingdomAI.AIPersonality.Default && governor == null)
		{
			num *= 0.2f;
		}
		return num;
	}

	private bool CalcBuildablePrerequisites(Building.Def def)
	{
		if (def.districts == null)
		{
			return false;
		}
		for (int i = 0; i < def.districts.Count; i++)
		{
			District.Def district = def.districts[i];
			List<Building.Def> prerequisites = def.GetPrerequisites(district);
			if (prerequisites != null)
			{
				bool flag = true;
				int count = tmp_directly_buildable_prerequisites.Count;
				int count2 = tmp_indirectly_buildable_prerequisites.Count;
				for (int j = 0; j < prerequisites.Count; j++)
				{
					Building.Def def2 = prerequisites[j];
					if (HasBuilding(def2) || IsBuilding(def2))
					{
						continue;
					}
					switch (CanBuildBuilding(def2, ignore_cost: true, ignore_must_expand: true, ignore_under_construction: true))
					{
					case StructureBuildAvailability.Available:
						Container.AddUniqueUnsafe_Class(tmp_directly_buildable_prerequisites, def2);
						continue;
					default:
						flag = false;
						break;
					case StructureBuildAvailability.NoParent:
						if (!CalcBuildablePrerequisites(def2))
						{
							flag = false;
							break;
						}
						Container.AddUniqueUnsafe_Class(tmp_indirectly_buildable_prerequisites, def2);
						continue;
					}
					break;
				}
				if (!flag)
				{
					if (tmp_directly_buildable_prerequisites.Count > count)
					{
						tmp_directly_buildable_prerequisites.RemoveRange(count, tmp_directly_buildable_prerequisites.Count - count);
					}
					if (tmp_indirectly_buildable_prerequisites.Count > count2)
					{
						tmp_indirectly_buildable_prerequisites.RemoveRange(count2, tmp_indirectly_buildable_prerequisites.Count - count2);
					}
					continue;
				}
			}
			List<Building.Def> prerequisitesOr = def.GetPrerequisitesOr(district);
			if (prerequisitesOr != null)
			{
				bool flag2 = false;
				int count3 = tmp_directly_buildable_prerequisites.Count;
				int count4 = tmp_indirectly_buildable_prerequisites.Count;
				for (int k = 0; k < prerequisitesOr.Count; k++)
				{
					Building.Def def3 = prerequisitesOr[k];
					if (HasBuilding(def3) || IsBuilding(def3))
					{
						flag2 = true;
						break;
					}
					switch (CanBuildBuilding(def3, ignore_cost: true, ignore_must_expand: true, ignore_under_construction: true))
					{
					case StructureBuildAvailability.Available:
						Container.AddUniqueUnsafe_Class(tmp_directly_buildable_prerequisites, def3);
						flag2 = true;
						break;
					case StructureBuildAvailability.NoParent:
						if (!CalcBuildablePrerequisites(def3))
						{
							continue;
						}
						Container.AddUniqueUnsafe_Class(tmp_indirectly_buildable_prerequisites, def3);
						flag2 = true;
						break;
					default:
						continue;
					}
					break;
				}
				if (!flag2)
				{
					if (tmp_directly_buildable_prerequisites.Count > count3)
					{
						tmp_directly_buildable_prerequisites.RemoveRange(count3, tmp_directly_buildable_prerequisites.Count - count3);
					}
					if (tmp_indirectly_buildable_prerequisites.Count > count4)
					{
						tmp_indirectly_buildable_prerequisites.RemoveRange(count4, tmp_indirectly_buildable_prerequisites.Count - count4);
					}
					continue;
				}
			}
			return true;
		}
		return false;
	}

	public KingdomAI.Expense.Priority CalcPriority(Building.Def bdef)
	{
		if (bdef.ai_urgent_field == null)
		{
			return KingdomAI.Expense.Priority.Normal;
		}
		if (bdef.ai_urgent_field.Value(this).Bool())
		{
			return KingdomAI.Expense.Priority.Urgent;
		}
		return KingdomAI.Expense.Priority.Normal;
	}

	public void AddBuildOption(Building.Def bdef, Resource production_weights, float min_eval = 0f)
	{
		if (bdef == null)
		{
			return;
		}
		bool flag = bdef.IsUpgrade();
		List<BuildOption> list = (flag ? upgrade_options : build_options);
		if (HasBuilding(bdef) && !in_spec_eval)
		{
			AddBuildOptions(bdef.upgrades?.buildings, production_weights, min_eval);
			return;
		}
		KingdomAI.Expense.Priority priority = CalcPriority(bdef);
		if (!in_spec_eval && list.Count > 0 && list[0].priority == KingdomAI.Expense.Priority.Urgent && priority != KingdomAI.Expense.Priority.Urgent)
		{
			return;
		}
		StructureBuildAvailability structureBuildAvailability;
		if (in_spec_eval)
		{
			if (!GetKingdom().CheckReligionRequirements(bdef) || !MayBuildBuilding(bdef))
			{
				return;
			}
			structureBuildAvailability = StructureBuildAvailability.Available;
		}
		else
		{
			structureBuildAvailability = CanBuildBuilding(bdef, ignore_cost: true, ignore_must_expand: true, ignore_under_construction: true);
			if (structureBuildAvailability != StructureBuildAvailability.Available && structureBuildAvailability != StructureBuildAvailability.NoParent)
			{
				return;
			}
		}
		if (!in_spec_eval && priority == KingdomAI.Expense.Priority.Urgent && list.Count > 0 && list[0].priority != KingdomAI.Expense.Priority.Urgent)
		{
			list.Clear();
			if (flag)
			{
				upgrade_options_sum = 0f;
			}
			else
			{
				build_options_sum = 0f;
			}
		}
		float num = EvalBuild(bdef, production_weights);
		if (num < min_eval)
		{
			num = min_eval;
		}
		if (num <= 0f)
		{
			return;
		}
		if (!in_spec_eval && !bdef.IsUpgrade() && bdef.HasPFRequirement(count_settlements: true) && tmp_res[ResourceType.Rebels] > 0f && AddAIBuildingReservations(bdef))
		{
			AddBuildOptions(tmp_directly_buildable_prerequisites, production_weights, num);
		}
		if (structureBuildAvailability != StructureBuildAvailability.Available || !CheckAIBuildingReservations(bdef))
		{
			return;
		}
		if (priority != KingdomAI.Expense.Priority.Urgent)
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom?.ai?.categories != null && kingdom.ai.categories[(int)bdef.ai_category].weight <= 0f)
			{
				return;
			}
		}
		BuildOption buildOption = new BuildOption
		{
			castle = this,
			def = bdef,
			eval = num,
			priority = priority
		};
		for (int i = 0; i < list.Count; i++)
		{
			BuildOption buildOption2 = list[i];
			if (buildOption2.def != bdef)
			{
				continue;
			}
			if (!(num <= buildOption2.eval))
			{
				list[i] = buildOption;
				if (flag)
				{
					upgrade_options_sum += buildOption.eval - buildOption2.eval;
				}
				else
				{
					build_options_sum += buildOption.eval - buildOption2.eval;
				}
			}
			return;
		}
		list.Add(buildOption);
		if (flag)
		{
			upgrade_options_sum += buildOption.eval;
		}
		else
		{
			build_options_sum += buildOption.eval;
		}
	}

	public void AddBuildOptions(List<Building.Def> buildings, Resource production_weights, float min_eval = 0f)
	{
		if (buildings != null)
		{
			for (int i = 0; i < buildings.Count; i++)
			{
				Building.Def bdef = buildings[i];
				AddBuildOption(bdef, production_weights, min_eval);
			}
		}
	}

	public void AddBuildOptions(List<District.Def.BuildingInfo> buildings, Resource production_weights, float min_eval = 0f)
	{
		if (buildings != null)
		{
			for (int i = 0; i < buildings.Count; i++)
			{
				District.Def.BuildingInfo buildingInfo = buildings[i];
				AddBuildOption(buildingInfo.def, production_weights, min_eval);
			}
		}
	}

	public void AddBuildOptions(bool common_only, Resource production_weights)
	{
		DelUnnecessaryAIBuildingReservations();
		AddBuildOptions(District.Def.GetCommon(game)?.buildings, production_weights);
		AddBuildOptions(District.Def.GetPF(game)?.buildings, production_weights);
		if (!common_only)
		{
			List<District.Def> buildableDistricts = GetBuildableDistricts();
			for (int i = 0; i < buildableDistricts.Count; i++)
			{
				District.Def def = buildableDistricts[i];
				AddBuildOptions(def.buildings, production_weights);
			}
		}
	}

	public static BuildOption ChooseBuildOption(Game game, List<BuildOption> options, float sum)
	{
		float num = game.Random(0f, sum);
		for (int i = 0; i < options.Count; i++)
		{
			BuildOption result = options[i];
			if (result.eval > num)
			{
				return result;
			}
			num -= result.eval;
		}
		return default(BuildOption);
	}

	public BuildOption ChooseBuildingToBuild(bool common_only, Resource production_weights = null)
	{
		ClearBuildOptions();
		AddBuildOptions(common_only, production_weights);
		return ChooseBuildOption(game, build_options, build_options_sum);
	}

	public int NumAIBuildingReservations()
	{
		if (ai_building_reservations == null)
		{
			return 0;
		}
		return ai_building_reservations.Count;
	}

	public bool AddAIBuildingReservation(Building.Def bdef, Building.Def prerequisite_of)
	{
		if (ai_building_reservations == null)
		{
			ai_building_reservations = new List<AIBuildingReservation>();
		}
		for (int i = 0; i < ai_building_reservations.Count; i++)
		{
			AIBuildingReservation aIBuildingReservation = ai_building_reservations[i];
			if (aIBuildingReservation.bdef == bdef)
			{
				if (aIBuildingReservation.prerequisite_of.Contains(prerequisite_of))
				{
					return false;
				}
				aIBuildingReservation.prerequisite_of.Add(prerequisite_of);
				return true;
			}
		}
		AIBuildingReservation item = new AIBuildingReservation
		{
			bdef = bdef,
			prerequisite_of = new List<Building.Def>()
		};
		item.prerequisite_of.Add(prerequisite_of);
		ai_building_reservations.Add(item);
		return true;
	}

	public bool DelAIBuildingReservations(Building.Def bdef)
	{
		if (ai_building_reservations == null)
		{
			return false;
		}
		bool result = false;
		for (int num = ai_building_reservations.Count - 1; num >= 0; num--)
		{
			AIBuildingReservation aIBuildingReservation = ai_building_reservations[num];
			if (aIBuildingReservation.prerequisite_of.Remove(bdef))
			{
				result = true;
				if (aIBuildingReservation.prerequisite_of.Count == 0)
				{
					ai_building_reservations.RemoveAt(num);
				}
			}
		}
		return result;
	}

	public bool HasAIBuildingReservation(Building.Def bdef)
	{
		if (ai_building_reservations == null)
		{
			return false;
		}
		for (int i = 0; i < ai_building_reservations.Count; i++)
		{
			if (ai_building_reservations[i].bdef == bdef)
			{
				return true;
			}
		}
		return false;
	}

	public bool AddAIBuildingReservations(Building.Def bdef)
	{
		tmp_directly_buildable_prerequisites.Clear();
		tmp_indirectly_buildable_prerequisites.Clear();
		if (!CalcBuildablePrerequisites(bdef))
		{
			return false;
		}
		AddAIBuildingReservation(bdef, bdef);
		for (int i = 0; i < tmp_directly_buildable_prerequisites.Count; i++)
		{
			Building.Def bdef2 = tmp_directly_buildable_prerequisites[i];
			AddAIBuildingReservation(bdef2, bdef);
		}
		for (int j = 0; j < tmp_indirectly_buildable_prerequisites.Count; j++)
		{
			Building.Def bdef3 = tmp_indirectly_buildable_prerequisites[j];
			AddAIBuildingReservation(bdef3, bdef);
		}
		return true;
	}

	public bool DelUnnecessaryAIBuildingReservations()
	{
		if (ai_building_reservations == null)
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		bool result = false;
		for (int num = ai_building_reservations.Count - 1; num >= 0; num--)
		{
			AIBuildingReservation aIBuildingReservation = ai_building_reservations[num];
			if (HasBuilding(aIBuildingReservation.bdef) || IsBuilding(aIBuildingReservation.bdef))
			{
				ai_building_reservations.RemoveAt(num);
				result = true;
			}
			else if (aIBuildingReservation.prerequisite_of.Contains(aIBuildingReservation.bdef) && kingdom.GetRealmTag(aIBuildingReservation.bdef.id) > 0)
			{
				result = true;
				DelAIBuildingReservations(aIBuildingReservation.bdef);
				if (num > ai_building_reservations.Count)
				{
					num = ai_building_reservations.Count;
				}
			}
		}
		return result;
	}

	public bool CheckAIBuildingReservations(Building.Def bdef)
	{
		if (bdef.IsUpgrade())
		{
			return true;
		}
		int num = MaxBuildingSlots();
		int num2 = NumBuildings(include_planned: false);
		int num3 = NumAIBuildingReservations();
		if (num2 + num3 < num)
		{
			return true;
		}
		if (HasAIBuildingReservation(bdef))
		{
			return true;
		}
		return false;
	}

	public Resource CalcBaseProduction(Resource res = null)
	{
		Realm realm = GetRealm();
		if (realm == null)
		{
			return res;
		}
		if (res == null)
		{
			res = new Resource();
		}
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			IncomePerResource incomePerResource = realm.incomes[resourceType];
			if (incomePerResource != null && incomePerResource.children != null)
			{
				for (int i = 0; i < incomePerResource.children.Count; i++)
				{
					IncomePerResource incomePerResource2 = incomePerResource.children[i];
					res.Add(resourceType, incomePerResource2.value.base_value);
				}
			}
		}
		res.Add(ResourceType.Gold, Economy.CalcPopulationGold(realm, null));
		if (realm.IsTradeCenter())
		{
			res.Add(ResourceType.Gold, def.trade_center_gold_per_commerce * res[ResourceType.Trade]);
		}
		return res;
	}

	public Resource CalcGovernedBonuses(Resource res = null)
	{
		if (GetRealm() == null)
		{
			return res;
		}
		float num = 1f;
		float num2 = 1f;
		float num3 = 1f;
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return res;
		}
		float taxMul = kingdom.GetTaxMul();
		num = 1f + taxMul;
		num2 = taxMul;
		if (def != null)
		{
			num3 = def.no_governor_penalty * 0.01f;
		}
		tmp_res.Clear();
		CalcBaseProduction(tmp_res);
		if (res == null)
		{
			res = new Resource();
		}
		for (ResourceType resourceType = ResourceType.Gold; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num4 = tmp_res[resourceType];
			float num5;
			float num6;
			if (resourceType == ResourceType.Gold)
			{
				num5 = num;
				num6 = num2;
			}
			else
			{
				if (resourceType != ResourceType.Books && resourceType != ResourceType.Piety && resourceType != ResourceType.Food && resourceType != ResourceType.Trade)
				{
					continue;
				}
				num5 = 1f;
				num6 = num3;
			}
			float num7 = Ceil(num4 * num5);
			float num8 = Ceil(num4 * num6);
			float amount = num7 - num8;
			res.Add(resourceType, amount);
		}
		return res;
		static float Ceil(float f)
		{
			if (f < 0f)
			{
				return 0f;
			}
			return (float)Math.Ceiling(f);
		}
	}

	public float EvalGovernor(Character governor, Resource production_weights = null)
	{
		if (governor == null)
		{
			return 0f;
		}
		tmp_res.Clear();
		CalcGovernorProduction(tmp_res, governor);
		return tmp_res.Eval(production_weights);
	}

	public Resource CalcGovernorProduction(Resource res, Character governor)
	{
		if (governor == null)
		{
			return null;
		}
		tmp_res.Clear();
		tmp_vars.Clear();
		SkillsTable.SetupEvalGovernorModVars(tmp_vars, this, governor);
		SkillsTable.EnumGovernorBonuses(this, governor, CalcGovernorProductionCallback);
		Realm realm = GetRealm();
		if (realm != null && realm.IsTradeCenter())
		{
			tmp_res.Add(ResourceType.Gold, def.trade_center_gold_per_commerce * tmp_res[ResourceType.Trade]);
		}
		if (res == null)
		{
			res = new Resource();
		}
		res.Add(tmp_res, 1f);
		return res;
	}

	private static void CalcGovernorProductionCallback(Castle castle, Character governor, Skill.StatModifier.Def mdef)
	{
		Stat stat = castle?.GetRealm()?.stats?.Find(mdef.stat_name);
		if (stat == null)
		{
			return;
		}
		ResourceType resource = stat.def.GetResource();
		if (resource != ResourceType.None)
		{
			SkillsTable.GovernorModEval governorModEval = SkillsTable.EvalGovernorMod(castle, governor, mdef, tmp_vars, stat);
			if (governorModEval.active)
			{
				tmp_res.Add(resource, governorModEval.final_value);
			}
		}
	}

	public void UpdateFoodStorage()
	{
		Realm realm = GetRealm();
		if (realm != null && battle == null)
		{
			float val = realm.income[ResourceType.Food];
			AddFood(val);
		}
	}

	public float GetFoodStorage()
	{
		return food_storage;
	}

	public void AddFood(float val, bool send_state = true)
	{
		if (IsAuthority())
		{
			SetFood(food_storage + val, clamp: true, send_state);
		}
	}

	public void SetFood(float val, bool clamp, bool send_state = true)
	{
		if (clamp)
		{
			float maxFoodStorage = GetMaxFoodStorage();
			val = Game.clamp(val, 0f, maxFoodStorage);
		}
		food_storage = val;
		NotifyListeners("food_changed");
		if (IsAuthority() && send_state)
		{
			SendState<FoodState>();
		}
	}

	public override void Load(DT.Field field)
	{
		base.Load(field);
		name = field.GetString("town_name");
		if (string.IsNullOrEmpty(name))
		{
			name = field.GetString("name");
		}
		Realm realm = GetRealm();
		if (realm != null)
		{
			if (!string.IsNullOrEmpty(realm.town_name))
			{
				name = realm.town_name;
			}
			else if (!string.IsNullOrEmpty(name))
			{
				realm.town_name = name;
			}
			else
			{
				name = (realm.town_name = realm.name);
			}
		}
		coef[0] = def.field.GetFloat("rebel_coef", null, coef[0]);
		coef[1] = def.field.GetFloat("worker_coef", null, coef[1]);
		siege_defence_per_point_cost = def.field.FindChild("siege_defence_per_point_cost");
		max_citadel_levels = def.field.GetInt("max_citadel_levels", null, max_citadel_levels);
		LoadMods(game, def.field);
		CalcMaxLevel();
		SetFood(GetMaxFoodStorage() * (def.starting_food_perc / 100f), clamp: true, send_state: false);
	}

	private void LoadMods(Game game, DT.Field f)
	{
		List<string> list = f.Keys();
		for (int i = 0; i < list.Count; i++)
		{
			string path = list[i];
			DT.Field field = f.FindChild(path);
			if (field == null || !(field.type == "production_mod") || !Enum.TryParse<Population.Type>(field.key, out var result) || result == Population.Type.TOTAL)
			{
				continue;
			}
			List<string> list2 = field.Keys();
			for (int j = 0; j < list2.Count; j++)
			{
				string path2 = list2[j];
				DT.Field field2 = field.FindChild(path2);
				if (field2 != null)
				{
					ResourceType resourceType = Resource.GetType(field2.key);
					if (resourceType != ResourceType.None)
					{
						production_coef[(int)result, (int)resourceType] = field2.Value();
					}
				}
			}
		}
	}

	public float CalcUpgradedRatio(out int num_upgrades, out int max_upgrades, bool exclude_unbuildable = true)
	{
		num_upgrades = (max_upgrades = 0);
		if (buildings == null)
		{
			return 0f;
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			buildings[i].CalcUpgradedRatio(out var num_upgrades2, out var max_upgrades2, exclude_unbuildable);
			num_upgrades += num_upgrades2;
			max_upgrades += max_upgrades2;
		}
		if (max_upgrades == 0)
		{
			return 0f;
		}
		return (float)num_upgrades / (float)max_upgrades;
	}

	public float CalcUpgradedRatio()
	{
		int num_upgrades;
		int max_upgrades;
		return CalcUpgradedRatio(out num_upgrades, out max_upgrades);
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		Realm realm = GetRealm();
		switch (key)
		{
		case "castle":
			return this;
		case "realm":
			return realm;
		case "kingdom":
			return realm?.GetKingdom();
		case "name":
			return GetNameKey();
		case "town_name":
			return GetNameKey();
		case "province_name":
			return realm?.GetProvinceNameKey() ?? GetNameKey();
		case "income_gold":
			return realm.income.Get(ResourceType.Gold);
		case "garrison_upkeep_food":
			return realm.upkeepGarrison.Get(ResourceType.Food);
		case "governor":
			return governor;
		case "in_disorder":
			return realm.IsDisorder();
		case "no_governor":
			if (governor != null)
			{
				return Value.Null;
			}
			return new Value("");
		case "awaiting_governor":
			return GetPreparingGovernor() != null;
		case "prepairng_governor":
			return GetPreparingGovernor();
		case "target_governor":
			return governor ?? GetPreparingGovernor();
		case "army":
			return army;
		case "max_food":
			return GetMaxFoodStorage();
		case "cur_food":
			return (float)Math.Round(GetFoodStorage());
		case "cur_population":
			return population.Count(Population.Type.TOTAL);
		case "max_population":
			return population.Slots(Population.Type.TOTAL);
		case "rebels_pop":
			return population.Count(Population.Type.Rebel);
		case "cur_workers_pop":
			return population.Count(Population.Type.Worker);
		case "max_workers_pop":
			return population.Slots(Population.Type.Worker);
		case "repair_cost":
		{
			float num = 0f;
			for (int i = 0; i < burned_buildings.Count; i++)
			{
				num += burned_buildings[i].def.cost.Get(ResourceType.Gold);
			}
			num *= sacking_comp.def.sacking_repair_gold_coef;
			Resource resource = new Resource();
			resource.Add(ResourceType.Gold, num);
			return resource;
		}
		case "realm_levy":
			return realm.income.Get(ResourceType.Levy);
		case "realm_tier":
			return GetTier();
		case "expand_cost":
			return GetExpandCost();
		case "building_slots_per_tier":
			return GetSlotsPerTier();
		case "realm_town_guards":
			return realm.income.Get(ResourceType.TownGuards);
		case "castle_level":
			return CalcLevel();
		case "castle_max_level":
			return GetMaxLevel();
		case "siege_defence_repair_cost":
			return GetSiegeDefenceRepairCost();
		case "cur_siege_defence":
			return GetCurrentSiegeDefence();
		case "max_siege_defence":
			return GetMaxSiegeDefence();
		case "fortifications_can_upgrade":
			return CanUpgradeFortification();
		case "fortifications_upgrade_bonus":
			return fortifications.GetOnUpgradeSiegeDefenseBonus();
		case "fortifications_upgrading":
			return fortifications.IsUpgrading();
		case "fortifications_upgrade_cost":
			return fortifications.GetUpgradeCost(fortifications.level + 1);
		case "fortifications_boosted_repairs":
			return fortifications.BoostedRepairs();
		case "fortifications_fully_upgraded":
			return fortifications.level == fortifications.max_level;
		case "fortifications_can_boost_repairs":
			return fortifications.CanBoostRepairs();
		case "fortifications_repairimg":
			return fortifications.IsRepairing();
		case "fortifications_level":
			return fortifications.level;
		case "fortifications_max_level":
			return fortifications.max_level;
		case "fortifications_state":
			return fortifications.GetState();
		case "current_build":
			return GetCurrentBuildingBuild();
		case "can_replenish_garrison":
			return garrison.CanReplenish();
		case "garrison_heal_cost":
			return GetGarrisonHealCost();
		case "garrison_units":
			return new Value(garrison.units);
		case "garrison_units_count":
			return garrison.units.Count;
		case "pop_majority_foreign":
			return realm?.pop_majority.kingdom != realm?.GetKingdom();
		case "available":
		{
			Resource resource2 = new Resource(GetKingdom().resources);
			resource2.Set(ResourceType.Food, food_storage);
			resource2.Set(ResourceType.Workers, population.GetWorkers());
			return resource2;
		}
		case "has_battle":
			return battle != null;
		case "workers":
			return population.Count(Population.Type.Worker, check_up_to_date: false);
		case "worker_slots":
			return population.Slots(Population.Type.Worker, check_up_to_date: false) + population.Slots(Population.Type.Rebel, check_up_to_date: false);
		case "rebellious_population":
			return population.Count(Population.Type.Rebel, check_up_to_date: false);
		case "has_rebellious_population":
			return population.Count(Population.Type.Rebel, check_up_to_date: false) > 0;
		case "manpower":
			return GetManpower();
		case "best_ai_spec":
			return CalcBestAISpecialization().ToString();
		case "base_production":
			return CalcBaseProduction();
		case "governed_bonuses":
			return CalcGovernedBonuses();
		case "governor_bonuses":
		{
			Character character = Vars.Get<Character>(vars, "governor") ?? governor;
			return CalcGovernorProduction(null, character);
		}
		case "buildings_count":
			return buildings.Count;
		case "buildings":
			return new Value(buildings);
		case "upgrades":
			return new Value(upgrades);
		case "num_upgrades":
		{
			CalcUpgradedRatio(out var num_upgrades2, out var _);
			return num_upgrades2;
		}
		case "max_upgrades":
		{
			CalcUpgradedRatio(out var _, out var max_upgrades);
			return max_upgrades;
		}
		case "num_ai_building_reservations":
			return NumAIBuildingReservations();
		case "ai_building_reservations":
			return new Value(ai_building_reservations);
		case "upgraded_ratio":
			return CalcUpgradedRatio();
		case "safe_level":
		{
			Realm realm2 = GetRealm();
			if (realm?.threat == null)
			{
				return 5;
			}
			if (realm2.threat.level >= KingdomAI.Threat.Level.Invaded && !(realm2.threat.assigned.eval < realm2.threat.min_needed))
			{
				return 3;
			}
			return (int)(5 - realm2.threat.level);
		}
		case "threat_dump":
			return "#" + GetRealm()?.threat?.Dump();
		case "num_buildings":
			return NumBuildings(include_planned: false);
		case "max_buildings":
			return MaxBuildingSlots();
		case "available_building_slots":
			return AvailableBuildingSlots();
		case "has_planned":
			return HasPlanned();
		default:
			if (realm != null)
			{
				Value var = realm.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			return base.GetVar(key, vars, as_value);
		}
	}

	public float GetStat(StatName name)
	{
		return GetRealm()?.GetStat(name) ?? 0f;
	}

	public float GetStat(string name)
	{
		return GetRealm()?.GetStat(name) ?? 0f;
	}

	public float GetMaxFoodStorage()
	{
		if (GetRealm()?.GetKingdom() == null)
		{
			return 0f;
		}
		return GetStat(Stats.rs_max_food);
	}

	public override IRelationCheck GetStanceObj()
	{
		IRelationCheck relationCheck = GetRealm()?.GetStanceObj();
		if (relationCheck != null)
		{
			return relationCheck;
		}
		return base.GetStanceObj();
	}

	protected override void OnStart()
	{
		base.OnStart();
		InitComponents();
		GetFoodStorage();
		UpdateInBatch(game.update_1sec);
		del_army = false;
		if (army != null && !army.IsValid() && IsAuthority())
		{
			del_army = true;
		}
	}

	private void InitComponents()
	{
		if (population == null)
		{
			new Population(this);
		}
		if (available_units == null)
		{
			available_units = new AvailableUnits(this);
		}
		if (structure_build == null)
		{
			structure_build = new Build(this);
		}
		if (morale == null)
		{
			morale = new Morale(this);
		}
		if (sacking_comp == null)
		{
			sacking_comp = new SackingRepair(this);
		}
		if (fortifications == null)
		{
			fortifications = new Fortifications(this, game.defs.GetBase<Fortifications.Def>());
		}
		InitKeepEffects();
	}

	public override void OnUpdate()
	{
		if (!IsAuthority())
		{
			return;
		}
		base.OnUpdate();
		if (del_army)
		{
			del_army = false;
			if (army != null)
			{
				SetArmy(null);
			}
		}
		Game.BeginProfileSection("UpgradeSettlements");
		UpgradeSettlements();
		Game.EndProfileSection("UpgradeSettlements");
		Game.BeginProfileSection("structure_build.Update");
		structure_build.Update();
		Game.EndProfileSection("structure_build.Update");
		Game.BeginProfileSection("fortifications.Update");
		fortifications.Update();
		Game.EndProfileSection("fortifications.Update");
	}

	public void SetArmy(Army a, bool send_state = true)
	{
		army = a;
		NotifyListeners("army_changed");
		if (send_state)
		{
			SendState<ArmyState>();
		}
	}

	public void SetGovernor(Character c, bool reloadLabel = true, bool send_state = true)
	{
		if (!(!IsAuthority() && send_state))
		{
			_ = governor;
			governor = c;
			Realm realm = GetRealm();
			Kingdom kingdom = GetKingdom();
			realm?.RefreshTags();
			realm?.InvalidateIncomes();
			kingdom?.RecalcBuildingStates();
			if (c != null)
			{
				realm?.NotifyListeners("governed", c);
			}
			NotifyListeners("governor_changed", reloadLabel);
			if (send_state)
			{
				SendState<GovernorState>();
			}
			realm?.rebellionRisk?.Recalc();
			GetKingdom()?.InvalidateIncomes();
		}
	}

	public bool CanSetGovernor(Character c)
	{
		if (c != null)
		{
			if (kingdom_id != c.kingdom_id)
			{
				return false;
			}
			if (c.GetKingdom().IsDefeated())
			{
				return false;
			}
		}
		bool num = this == game.religions.catholic.hq_realm?.castle && GetKingdom() == game.religions.catholic.hq_kingdom;
		bool flag = c?.IsPope() ?? false;
		if (num != flag)
		{
			return false;
		}
		return CanSetGovernor();
	}

	public bool CanSetGovernor()
	{
		if (battle != null)
		{
			return false;
		}
		Realm realm = GetRealm();
		if (realm == null || realm.IsOccupied() || realm.IsDisorder() || kingdom_id != realm.kingdom_id)
		{
			return false;
		}
		return true;
	}

	public Character GetPreparingGovernor()
	{
		List<Character> list = GetKingdom()?.court;
		if (list == null)
		{
			return null;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Character character = list[i];
			if (character != null && character.GetPreparingToGovernCastle() == this)
			{
				return character;
			}
		}
		return null;
	}

	public void BuildRandomBuilding(bool common_only = false)
	{
		if (battle == null)
		{
			BuildOption buildOption = ChooseBuildingToBuild(common_only);
			if (buildOption.def != null)
			{
				BuildBuilding(buildOption.def, -1, instant: true);
			}
		}
	}

	public void BuildRandomCommonBuilding()
	{
		BuildRandomBuilding(common_only: true);
	}

	public void InitStructures()
	{
		if (!IsAuthority() || init_structures)
		{
			return;
		}
		init_structures = true;
		DT.Field field = def.field.FindChild("initial_buildings");
		if (field != null && field.NumValues() == 2)
		{
			int min = field.Int(0);
			int num = field.Int(1);
			for (int num2 = game.Random(min, num + 1); num2 > 0; num2--)
			{
				BuildRandomBuilding(common_only: true);
			}
			UpgradeSettlements();
		}
	}

	public static bool CheckAvailabilityFlags(StructureBuildAvailability can_build, StructureBuildAvailability flags)
	{
		return (can_build & flags) == flags;
	}

	public StructureBuildAvailability CanBuildBuilding(Building.Def def, bool ignore_cost = false, bool ignore_must_expand = false, bool ignore_under_construction = false)
	{
		StructureBuildAvailability structureBuildAvailability = StructureBuildAvailability.Available;
		if (def == null)
		{
			return StructureBuildAvailability.MissingData;
		}
		bool flag = def.IsUpgrade();
		if (!CheckMaxInstances(def, out var planned))
		{
			return StructureBuildAvailability.AlreadyBuilt;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return StructureBuildAvailability.MissingData;
		}
		if (planned == null && !flag && !CheckMaxBuildings(ignore_must_expand))
		{
			structureBuildAvailability |= StructureBuildAvailability.MaxCountReached;
		}
		if (!CheckBuildingParentBuildings(def))
		{
			structureBuildAvailability |= StructureBuildAvailability.NoParent;
		}
		if (battle != null)
		{
			structureBuildAvailability |= StructureBuildAvailability.CastleInBattle;
		}
		if (sacked)
		{
			structureBuildAvailability |= StructureBuildAvailability.CastleSacked;
		}
		Realm realm = GetRealm();
		if (realm.IsOccupied() || realm.IsDisorder())
		{
			structureBuildAvailability |= StructureBuildAvailability.RealmOccupied;
		}
		if (!kingdom.CheckReligionRequirements(def))
		{
			structureBuildAvailability |= StructureBuildAvailability.NoParentNoRequirements;
		}
		if (!CheckBuildingRequirements(def))
		{
			structureBuildAvailability |= StructureBuildAvailability.NoRequirements;
		}
		if (!flag && !ignore_under_construction && structureBuildAvailability == StructureBuildAvailability.Available && structure_build != null && structure_build.IsBuilding())
		{
			return StructureBuildAvailability.UnderConstruction;
		}
		if (flag && structureBuildAvailability == StructureBuildAvailability.Available && kingdom.FindUpgradingInSameDistrict(def) != null)
		{
			return StructureBuildAvailability.UnderConstruction;
		}
		if (ignore_cost || structureBuildAvailability != StructureBuildAvailability.Available)
		{
			return structureBuildAvailability;
		}
		Resource cost = def.GetCost(GetRealm());
		if (cost != null && !kingdom.resources.CanAfford(cost, 1f))
		{
			structureBuildAvailability |= StructureBuildAvailability.CannotAfford;
		}
		else
		{
			float num = def.CalcCommerceUpkeep(kingdom);
			if (num > 0f && kingdom.expenses[ResourceType.Trade] + num > kingdom.income[ResourceType.Trade])
			{
				structureBuildAvailability |= StructureBuildAvailability.CannotAfford;
			}
		}
		return structureBuildAvailability;
	}

	public bool MayMeetRequirement(Building.Def.RequirementInfo req)
	{
		switch (req.type)
		{
		case "Resource":
			return MayProduceResource(req.key);
		case "ProvinceFeature":
			return GetRealm().HasTag(req.key);
		case "Settlement":
			return GetRealm().HasTag(req.key, req.amount);
		case "Citadel":
			return true;
		case "Religion":
			return true;
		default:
			Game.Log("Unknown building requirement type: " + req.type, Game.LogType.Warning);
			return false;
		}
	}

	public bool HasDistrict(District.Def ddef)
	{
		if (ddef == null)
		{
			return false;
		}
		if (ddef.IsCommon())
		{
			return true;
		}
		if (ddef.IsPF())
		{
			return true;
		}
		Building.Def parent = ddef.GetParent();
		if (parent != null)
		{
			return HasWorkingBuilding(parent);
		}
		return GetBuildableDistricts().Contains(ddef);
	}

	public bool HasDistrict(Building.Def bdef)
	{
		if (bdef?.districts == null)
		{
			return false;
		}
		for (int i = 0; i < bdef.districts.Count; i++)
		{
			District.Def ddef = bdef.districts[i];
			if (HasDistrict(ddef))
			{
				return true;
			}
		}
		return false;
	}

	public bool MayBuildDistrict(District.Def ddef)
	{
		if (ddef == null)
		{
			return false;
		}
		if (ddef.IsCommon())
		{
			return true;
		}
		if (ddef.IsPF())
		{
			return true;
		}
		Building.Def parent = ddef.GetParent();
		if (parent != null)
		{
			return MayBuildBuilding(parent);
		}
		return HasDistrict(ddef);
	}

	public bool MayBuildDistrict(Building.Def bdef)
	{
		if (bdef?.districts == null)
		{
			return false;
		}
		for (int i = 0; i < bdef.districts.Count; i++)
		{
			District.Def ddef = bdef.districts[i];
			if (MayBuildDistrict(ddef))
			{
				return true;
			}
		}
		return false;
	}

	public List<District.Def> GetBuildableDistricts()
	{
		if (buildable_districts == null)
		{
			CalcBuildableDistricts();
		}
		return buildable_districts;
	}

	public void RefreshBuildableDistricts()
	{
		buildable_districts = null;
		NotifyListeners("buildable_districts_changed");
	}

	private void CalcBuildableDistricts()
	{
		using (Game.Profile("Castle.CalcBuildableDistricts"))
		{
			buildable_districts = new List<District.Def>();
			List<District.Def> defs = game.defs.GetDefs<District.Def>();
			if (defs == null)
			{
				return;
			}
			for (int i = 0; i < defs.Count; i++)
			{
				District.Def def = defs[i];
				if (!def.IsCommon() && !def.IsPF() && def.IsBuildable(this))
				{
					buildable_districts.Add(def);
				}
			}
		}
	}

	public bool MayProduceResource(string resource)
	{
		if (Building.global_resources)
		{
			return true;
		}
		if (may_produce_resource != null && may_produce_resource.TryGetValue(resource, out var value))
		{
			return value;
		}
		if (may_produce_resource == null)
		{
			may_produce_resource = new Dictionary<string, bool>();
		}
		may_produce_resource.Add(resource, value: false);
		value = CalcMayProduceResource(resource);
		may_produce_resource[resource] = value;
		return value;
	}

	private bool CalcMayProduceResource(string resource)
	{
		foreach (KeyValuePair<string, Logic.Def> def2 in game.defs.Get(typeof(Building.Def)).defs)
		{
			Building.Def def = def2.Value as Building.Def;
			if (def.Produces(resource) && MayBuildBuilding(def))
			{
				return true;
			}
		}
		return false;
	}

	public bool MayBuildBuilding(Building.Def def, bool full_check = true)
	{
		if (def == null || def.id == null)
		{
			return false;
		}
		if (GetRealm() == null)
		{
			return false;
		}
		return CalcMayBuildBuilding(def, full_check);
	}

	private bool CalcMayBuildPrerequisites(Building.Def def, District.Def district)
	{
		List<Building.Def> prerequisites = def.GetPrerequisites(district);
		if (prerequisites != null)
		{
			for (int i = 0; i < prerequisites.Count; i++)
			{
				Building.Def def2 = prerequisites[i];
				if (!MayBuildBuilding(def2))
				{
					return false;
				}
			}
		}
		List<Building.Def> prerequisitesOr = def.GetPrerequisitesOr(district);
		if (prerequisitesOr != null)
		{
			bool flag = false;
			for (int j = 0; j < prerequisitesOr.Count; j++)
			{
				Building.Def def3 = prerequisitesOr[j];
				if (MayBuildBuilding(def3))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	private bool CalcMayBuildBuilding(Building.Def def, bool full_check = true)
	{
		if (full_check && HasBuilding(def))
		{
			return true;
		}
		if (def.requires != null && def.requires.Count > 0)
		{
			for (int i = 0; i < def.requires.Count; i++)
			{
				Building.Def.RequirementInfo req = def.requires[i];
				if (!MayMeetRequirement(req))
				{
					return false;
				}
			}
		}
		if (def.requires_or != null && def.requires_or.Count > 0)
		{
			bool flag = false;
			for (int j = 0; j < def.requires_or.Count; j++)
			{
				Building.Def.RequirementInfo req2 = def.requires_or[j];
				if (MayMeetRequirement(req2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (full_check && def.districts != null)
		{
			bool flag2 = false;
			for (int k = 0; k < def.districts.Count; k++)
			{
				District.Def def2 = def.districts[k];
				if (MayBuildDistrict(def2) && CalcMayBuildPrerequisites(def, def2))
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				return false;
			}
		}
		return true;
	}

	public bool CheckBuildingRequirement(Building.Def.RequirementInfo req)
	{
		if (req.type == "Resource" && Building.global_resources)
		{
			if (Building.Def.SOFT_RESOURCE_REQUIREMENTS)
			{
				return true;
			}
			return CheckResourceRequirement(req);
		}
		if (req.type == "Citadel")
		{
			return GetCitadelLevel() >= req.amount;
		}
		if (req.type == "Religion")
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom?.religion == null)
			{
				return false;
			}
			return kingdom.religion.HasTag(req.key);
		}
		return GetRealm()?.HasTag(req.key, req.amount) ?? false;
	}

	public bool CheckResourceRequirement(Building.Def.RequirementInfo req)
	{
		if (req.type != "Resource")
		{
			return true;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		return kingdom.GetRealmTag(req.key) >= req.amount;
	}

	public bool CheckResourceRequirements(Building.Def def)
	{
		if (def?.requires != null)
		{
			for (int i = 0; i < def.requires.Count; i++)
			{
				Building.Def.RequirementInfo req = def.requires[i];
				if (!CheckResourceRequirement(req))
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool CheckBuildingRequirements(Building.Def def)
	{
		if (def.requires != null && def.requires.Count > 0)
		{
			for (int i = 0; i < def.requires.Count; i++)
			{
				Building.Def.RequirementInfo requirementInfo = def.requires[i];
				if (requirementInfo != null && !CheckBuildingRequirement(requirementInfo))
				{
					return false;
				}
			}
		}
		if (def.requires_or != null && def.requires_or.Count > 0)
		{
			bool flag = false;
			for (int j = 0; j < def.requires_or.Count; j++)
			{
				Building.Def.RequirementInfo requirementInfo2 = def.requires_or[j];
				if (requirementInfo2 != null && CheckBuildingRequirement(requirementInfo2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public bool CheckBuildingParentBuildings(Building.Def def, District.Def district)
	{
		List<Building.Def> prerequisites = def.GetPrerequisites(district);
		if (prerequisites != null)
		{
			for (int i = 0; i < prerequisites.Count; i++)
			{
				Building.Def def2 = prerequisites[i];
				if (def2 != null && !HasWorkingBuilding(def2))
				{
					return false;
				}
			}
		}
		List<Building.Def> prerequisitesOr = def.GetPrerequisitesOr(district);
		if (prerequisitesOr != null && prerequisitesOr.Count > 0)
		{
			bool flag = false;
			for (int j = 0; j < prerequisitesOr.Count; j++)
			{
				Building.Def def3 = prerequisitesOr[j];
				if (def3 != null && HasWorkingBuilding(def3))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public bool CheckBuildingParentBuildings(Building.Def def)
	{
		if (!def.buildable)
		{
			return false;
		}
		if (!HasDistrict(def))
		{
			return false;
		}
		if (GetRealm() == null)
		{
			return false;
		}
		if (def.districts == null)
		{
			return true;
		}
		for (int i = 0; i < def.districts.Count; i++)
		{
			District.Def district = def.districts[i];
			if (CheckBuildingParentBuildings(def, district))
			{
				return true;
			}
		}
		return false;
	}

	public int FindBuildingIdx(Building b)
	{
		if (buildings == null || b == null)
		{
			return -1;
		}
		return buildings.IndexOf(b);
	}

	public Building FindBuilding(Building.Def def)
	{
		if (def == null || def.id == null)
		{
			return null;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return null;
		}
		Building building = null;
		for (int i = 0; i < list.Count; i++)
		{
			Building building2 = list[i];
			if (building2 != null && building2.def != null && def == building2.def && (building == null || building2.state > building.state))
			{
				building = building2;
			}
		}
		return building;
	}

	public bool HasBuilding(Building.Def def)
	{
		if (def == null || def.id == null)
		{
			return false;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Building building = list[i];
			if (building != null && building.IsBuilt() && building.def == def)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPlannedBuilding(Building.Def def)
	{
		if (def == null || def.id == null)
		{
			return false;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Building building = list[i];
			if (building != null && building.IsPlanned() && building.def == def)
			{
				return true;
			}
		}
		return false;
	}

	public int NumInstances(Building.Def def, bool include_planned = true)
	{
		if (def == null)
		{
			return 0;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			Building building = list[i];
			if (building != null && building.def == def && (building.IsBuilt() || include_planned))
			{
				num++;
			}
		}
		return num;
	}

	public bool CheckMaxInstances(Building.Def def, out Building planned)
	{
		planned = null;
		if (def == null || def.id == null)
		{
			return false;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return true;
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			Building building = list[i];
			if (building == null || building.def != def)
			{
				continue;
			}
			if (!building.IsBuilt())
			{
				if (planned == null)
				{
					planned = building;
				}
				continue;
			}
			num++;
			if (num == def.max_instances)
			{
				return false;
			}
		}
		return true;
	}

	public bool HasWorkingBuilding(Building.Def def)
	{
		if (def == null || def.id == null)
		{
			return false;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Building building = list[i];
			if (building != null && (building.def == def || building.def.variant_of == def) && building.IsWorking())
			{
				return true;
			}
		}
		return false;
	}

	public List<Building> GetBuildings(Building.Def def)
	{
		if (def == null || !def.IsUpgrade())
		{
			return buildings;
		}
		return upgrades;
	}

	public bool HasPlanned()
	{
		return NumBuildings(include_planned: true) - NumBuildings(include_planned: false) > 0;
	}

	public int NumBuildings(bool include_planned, bool ignore_underconstruction = false)
	{
		int num = 0;
		for (int i = 0; i < buildings.Count; i++)
		{
			Building building = buildings[i];
			if (building != null)
			{
				if (!ignore_underconstruction && building.IsUnderConstruction())
				{
					num++;
				}
				else if (include_planned && building.IsPlanned())
				{
					num++;
				}
				else if (building.IsBuilt())
				{
					num++;
				}
			}
		}
		return num;
	}

	public int AvailableBuildingSlots()
	{
		Building.Def def = game.defs.GetBase<Building.Def>();
		if (def == null)
		{
			return 0;
		}
		return def.slots_base + building_slots_tier * def.slots_per_tier;
	}

	public int MaxBuildingSlots()
	{
		if (game?.rules == null)
		{
			return 0;
		}
		Building.Def def = game.defs.GetBase<Building.Def>();
		if (def == null)
		{
			return 0;
		}
		return def.slots_base + def.slots_per_tier * game.rules.max_unlockable_tiers;
	}

	public Building GetBuilding(int slot_idx)
	{
		if (buildings == null || slot_idx < 0 || slot_idx >= buildings.Count)
		{
			return null;
		}
		return buildings[slot_idx];
	}

	public Building.Def GetBuildingDef(int slot_idx, bool is_upgrade)
	{
		if (slot_idx < 0)
		{
			return null;
		}
		List<Building> list = (is_upgrade ? upgrades : buildings);
		if (list != null && slot_idx < list.Count)
		{
			Building building = list[slot_idx];
			if (building != null)
			{
				return building.def;
			}
		}
		if (!is_upgrade && structure_build.prefered_slot_index == slot_idx && structure_build.current_building_def != null)
		{
			return structure_build.current_building_def;
		}
		return null;
	}

	public bool CheckMaxBuildings(bool ignore_must_expand = false)
	{
		int num = (ignore_must_expand ? MaxBuildingSlots() : AvailableBuildingSlots());
		if (num == 0)
		{
			return false;
		}
		if (num < 0)
		{
			return true;
		}
		if (NumBuildings(include_planned: true) >= num)
		{
			return false;
		}
		return true;
	}

	public bool CheckBuildingSlotIndex(int slot_index)
	{
		if (slot_index < 0)
		{
			return true;
		}
		int num = AvailableBuildingSlots();
		if (num < 0)
		{
			return true;
		}
		if (slot_index >= num)
		{
			return false;
		}
		return true;
	}

	private void UpgradeSettlements()
	{
		if (!IsAuthority())
		{
			return;
		}
		Realm realm = GetRealm();
		if (realm == null)
		{
			return;
		}
		for (int i = 0; i < realm.settlements.Count; i++)
		{
			Settlement settlement = realm.settlements[i];
			if (!(settlement.type == "Castle") && settlement.IsActiveSettlement() && settlement is Village village)
			{
				village.settlementUpgrade?.Update();
			}
		}
	}

	public bool PlanPrerequisites(Building.Def def)
	{
		if (def.districts == null)
		{
			return true;
		}
		if (def.districts.Count != 1)
		{
			return false;
		}
		District.Def district = def.districts[0];
		List<Building.Def> prerequisites = def.GetPrerequisites(district);
		if (prerequisites == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < prerequisites.Count; i++)
		{
			Building.Def def2 = prerequisites[i];
			flag |= PlanBuilding(def2, -1, plan_prerequisites: true);
		}
		return flag;
	}

	public bool PlanBuilding(Building.Def def, int slot_idx = -1, bool plan_prerequisites = false)
	{
		if (def == null)
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		bool flag = def.IsUpgrade();
		if (!flag)
		{
			int num = AvailableBuildingSlots();
			if (slot_idx >= num)
			{
				return false;
			}
			int num2 = NumBuildings(include_planned: true, ignore_underconstruction: true);
			if (structure_build.IsBuilding())
			{
				num2++;
			}
			if (num2 >= num)
			{
				return false;
			}
			int num3 = NumInstances(def);
			if (def.max_instances > 0 && num3 >= def.max_instances)
			{
				if (plan_prerequisites)
				{
					return PlanPrerequisites(def);
				}
				return false;
			}
		}
		if (!IsAuthority())
		{
			SendEvent(new BuildEvent(def, slot_idx, instant: false, planned: true));
			if (plan_prerequisites)
			{
				PlanPrerequisites(def);
			}
			return false;
		}
		bool flag2 = true;
		if (flag)
		{
			if (kingdom == null)
			{
				return false;
			}
			if (!kingdom.PlanBuildingUpgrade(def))
			{
				flag2 = false;
			}
		}
		else if (AddBuilding(def, slot_idx, Building.State.Planned) == null)
		{
			flag2 = false;
		}
		if (plan_prerequisites)
		{
			flag2 |= PlanPrerequisites(def);
		}
		return flag2;
	}

	public bool BuildBuilding(Building.Def def, int slot_idx = -1, bool instant = false)
	{
		if (def == null)
		{
			return false;
		}
		if (sacked || battle != null)
		{
			return false;
		}
		bool flag = def.IsUpgrade();
		if (!CheckMaxInstances(def, out var planned) && planned == null)
		{
			return false;
		}
		if (!flag && planned == null && !CheckMaxBuildings())
		{
			return false;
		}
		if (ResolveBuildingSlot(def, ref slot_idx) == null || slot_idx < 0)
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new BuildEvent(def, slot_idx, instant, planned: false));
			return true;
		}
		if (!instant && !flag && structure_build != null && structure_build.IsBuilding())
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (instant)
		{
			if (flag)
			{
				kingdom.UnlockBuildingUpgrade(def, forced: true);
			}
			else
			{
				OnCompleteBuild(def, slot_idx);
			}
		}
		else
		{
			if (CanBuildBuilding(def) > StructureBuildAvailability.Available)
			{
				return false;
			}
			kingdom?.SubResources(def.ai_category, def.GetCost(GetRealm()));
			if (!flag)
			{
				structure_build.BeginBuild(def, slot_idx);
			}
			else
			{
				kingdom?.BeginUpgrade(this, def);
			}
		}
		return true;
	}

	public void SwapBuildingSlots(Building building_1, Building building_2)
	{
		if (building_1 != null && building_2 != null && building_1.castle == this && building_1.castle == building_2.castle)
		{
			int num = buildings.IndexOf(building_1);
			int num2 = buildings.IndexOf(building_2);
			if (!IsAuthority())
			{
				SendEvent(new SwapBuildingSlotsEvent(num, num2));
				return;
			}
			buildings[num] = building_2;
			buildings[num2] = building_1;
			NotifyListeners("structures_changed");
		}
	}

	public void MoveToBuildingSlot(Building building, int toIdx)
	{
		if (building == null)
		{
			if (structure_build != null && structure_build.IsBuilding())
			{
				structure_build.prefered_slot_index = toIdx;
				NotifyListeners("structures_changed");
			}
		}
		else
		{
			if (building.castle != this || (toIdx < buildings.Count && buildings[toIdx] != null))
			{
				return;
			}
			int num = buildings.IndexOf(building);
			if (!IsAuthority())
			{
				SendEvent(new MoveBuildingToSlotEvent(num, toIdx));
				return;
			}
			if (structure_build != null && structure_build.IsBuilding() && structure_build.prefered_slot_index == toIdx)
			{
				structure_build.prefered_slot_index = num;
			}
			buildings[num] = null;
			while (buildings.Count <= toIdx)
			{
				buildings.Add(null);
			}
			buildings[toIdx] = building;
			NotifyListeners("structures_changed");
		}
	}

	private bool IsPlannedBuilding(Building.Def def)
	{
		if (def == null)
		{
			return false;
		}
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i]?.def == def)
			{
				return true;
			}
		}
		return false;
	}

	public int FindEmptyIdx(List<Building> buildings)
	{
		if (buildings == null)
		{
			return 0;
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			if (buildings[i] == null)
			{
				return i;
			}
		}
		return buildings.Count;
	}

	public List<Building> ResolveBuildingSlot(Building.Def def, ref int slot_idx, bool planned = false)
	{
		bool flag = def.IsUpgrade();
		List<Building> list = GetBuildings(def);
		if (list == null)
		{
			slot_idx = -1;
			return null;
		}
		if (!flag)
		{
			Building.Def buildingDef = GetBuildingDef(slot_idx, flag);
			if (buildingDef != null)
			{
				if (buildingDef == def)
				{
					return list;
				}
				slot_idx = -1;
			}
		}
		int num = -1;
		int num2 = def.max_instances;
		for (int i = 0; i < list.Count; i++)
		{
			Building.Def buildingDef2 = GetBuildingDef(i, flag);
			if (buildingDef2 == null)
			{
				if (num < 0)
				{
					num = i;
				}
				continue;
			}
			if (buildingDef2 == def)
			{
				slot_idx = i;
				if (!planned)
				{
					Building building = list[i];
					if (building == null || !building.IsBuilt())
					{
						return list;
					}
				}
				num2--;
				if (num2 == 0)
				{
					return null;
				}
			}
			if (i == slot_idx)
			{
				slot_idx = -1;
			}
		}
		int num3 = (flag ? list.Count : AvailableBuildingSlots());
		if (slot_idx >= 0 && slot_idx < num3)
		{
			return list;
		}
		if (num < 0 && (flag || list.Count < num3))
		{
			num = list.Count;
			if (!flag && structure_build.prefered_slot_index == num && structure_build.current_building_def != null)
			{
				num++;
			}
		}
		slot_idx = num;
		return list;
	}

	public Building AddBuilding(Building.Def def, int slot_idx = -1, Building.State state = Building.State.TemporaryDeactivated)
	{
		if (def == null)
		{
			return null;
		}
		bool flag = def.IsUpgrade();
		if (!IsAuthority() && !flag)
		{
			return null;
		}
		List<Building> list = ResolveBuildingSlot(def, ref slot_idx, state == Building.State.Planned);
		if (list == null || slot_idx < 0)
		{
			return null;
		}
		Building building = ((slot_idx < list.Count) ? list[slot_idx] : null);
		if (building != null)
		{
			building.SetState(state);
		}
		else
		{
			building = new Building(this, def, state);
			while (slot_idx >= list.Count)
			{
				list.Add(null);
			}
			list[slot_idx] = building;
			building.castle = this;
			MatchResourceInfoBuildingInstances();
		}
		if (flag)
		{
			return building;
		}
		if (!building.IsBuilt())
		{
			SendState<StructuresState>();
			NotifyListeners("structures_changed", def);
			return building;
		}
		UpdateLevel();
		GetRealm()?.InvalidateIncomes();
		GetKingdom()?.RecalcBuildingStates();
		return building;
	}

	public void OnAnalyticsBuildingRemoved(Building b)
	{
		if (b != null && b.def != null && game.IsRunning() && !b.IsPlanned() && !b.def.IsUpgrade())
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom != null && kingdom.is_local_player)
			{
				int slot_idx = -1;
				ResolveBuildingSlot(b.def, ref slot_idx);
				Vars vars = new Vars();
				vars.Set("building_def", b.def);
				vars.Set("slot_index", slot_idx);
				vars.Set("slots_left", MaxBuildingSlots() - NumBuildings(include_planned: false) + 1);
				NotifyListeners("analytics_building_removed", vars);
			}
		}
	}

	public void RemoveBuilding(Building building, BuildingRemovalMode mode = BuildingRemovalMode.Regular, bool send_state = true)
	{
		if (building?.def == null)
		{
			return;
		}
		List<Building> list = GetBuildings(building.def);
		if (list == null)
		{
			return;
		}
		int num = list.IndexOf(building);
		if (num < 0)
		{
			return;
		}
		if (mode == BuildingRemovalMode.Regular && !IsAuthority())
		{
			SendEvent(new RemoveBuildingEvent(num, building.def));
			return;
		}
		if (mode != BuildingRemovalMode.Upgrade)
		{
			if (building.IsBuilt())
			{
				FireEvent("build_removed", null);
				GetKingdom()?.NotifyListeners("building_destroyed", building);
			}
		}
		else
		{
			GetKingdom().RemovePlanedUpgrade(building.def);
		}
		OnAnalyticsBuildingRemoved(building);
		list[num] = null;
		UpdateLevel();
		MatchResourceInfoBuildingInstances();
		if (!building.IsBuilt())
		{
			building.OnDestroy();
			if (send_state)
			{
				SendState<StructuresState>();
			}
			NotifyListeners("structures_changed", "removed");
			return;
		}
		if (structure_build != null)
		{
			structure_build.CheckDependency(building);
		}
		building.OnDestroy();
		if (mode != BuildingRemovalMode.Regular)
		{
			return;
		}
		if (structure_build.IsBuilding() && CanBuildBuilding(structure_build.current_building_def, ignore_cost: true) == StructureBuildAvailability.NoParent)
		{
			structure_build.CancelBuild();
		}
		Realm realm = GetRealm();
		if (realm != null)
		{
			realm.InvalidateIncomes();
			realm.RefreshSkills(building);
			realm.RefreshTags();
			realm.GetKingdom().RecalcBuildingStates(this, remove_abandoned: true);
		}
		else
		{
			if (send_state)
			{
				SendState<StructuresState>();
			}
			NotifyListeners("structures_changed", "removed");
		}
	}

	private bool AddUpgrade(Building.Def def)
	{
		if (!HasBuilding(def))
		{
			AddBuilding(def);
			return true;
		}
		return false;
	}

	private bool AddUpgrades(Building b)
	{
		if (b?.def == null)
		{
			return false;
		}
		if (b.def.upgrades?.buildings == null)
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < b.def.upgrades.buildings.Count; i++)
		{
			District.Def.BuildingInfo buildingInfo = b.def.upgrades.buildings[i];
			if (kingdom.HasBuildingUpgrade(buildingInfo.def) && AddUpgrade(buildingInfo.def))
			{
				result = true;
			}
		}
		return result;
	}

	public bool RefreshUpgrades()
	{
		bool result = false;
		Kingdom kingdom = GetKingdom();
		for (int num = upgrades.Count - 1; num >= 0; num--)
		{
			Building building = upgrades[num];
			if (building != null && !kingdom.HasBuildingUpgrade(building.def))
			{
				result = true;
				upgrades.RemoveAt(num);
			}
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			Building b = buildings[i];
			if (AddUpgrades(b))
			{
				result = true;
			}
		}
		return result;
	}

	public int GetTier()
	{
		return building_slots_tier;
	}

	public int GetMaxTier()
	{
		if (game?.rules == null)
		{
			return 0;
		}
		return game.rules.max_unlockable_tiers;
	}

	public int GetCitadelLevel()
	{
		return (int)Math.Ceiling((float)max_citadel_levels * (float)CalcLevel() / (float)GetMaxLevel());
	}

	public int GetCitadelMaxLevel()
	{
		return max_citadel_levels;
	}

	public void UpdateLevel()
	{
		int num = CalcLevel();
		if (num != base.level)
		{
			SetLevel(num);
		}
	}

	public int CalcLevel()
	{
		return 1 + NumBuildings(include_planned: false);
	}

	private void CalcMaxLevel()
	{
		max_level = 1 + MaxBuildingSlots();
	}

	public int GetMaxLevel()
	{
		return max_level;
	}

	public int GetWallLevel()
	{
		if (fortifications == null)
		{
			return 0;
		}
		return fortifications.level;
	}

	public int GetMaxWallLevel()
	{
		return fortifications?.max_level ?? 0;
	}

	public bool CanUpgradeFortification()
	{
		if (GetRealm().IsDisorder())
		{
			return false;
		}
		if (battle != null)
		{
			return false;
		}
		if (fortifications.level >= fortifications.max_level)
		{
			return false;
		}
		if (fortifications.IsUpgrading())
		{
			return false;
		}
		if (keep_effects.active_defence_recovery_boost)
		{
			return false;
		}
		if (IsOccupied())
		{
			return false;
		}
		if (!fortifications.CheckReqierments(this, fortifications.level + 1))
		{
			return false;
		}
		return true;
	}

	public bool FortificationsNeedRepair()
	{
		if (keep_effects == null)
		{
			return false;
		}
		if ((keep_effects.siege_defense_condition?.Get() ?? 100f) / 100f < 0.99f && !keep_effects.active_defence_recovery_boost)
		{
			return fortifications.CanBoostRepairs();
		}
		return false;
	}

	public Resource GetFortificationsUpgradeCost()
	{
		return fortifications.GetUpgradeCost(fortifications.level + 1);
	}

	public bool CanAffordFortificationsUpgrade()
	{
		Resource fortificationsUpgradeCost = GetFortificationsUpgradeCost();
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (fortificationsUpgradeCost != null && !kingdom.resources.CanAfford(fortificationsUpgradeCost, 1f, ResourceType.Hammers))
		{
			return false;
		}
		return true;
	}

	public void UpgradeFortification()
	{
		if (!IsAuthority())
		{
			SendEvent(new FortificationUpgradeEvent());
		}
		else
		{
			fortifications.BeginUpgrade();
		}
	}

	public bool CanAffordBoostSiegeDefenceRepairs()
	{
		Resource siegeDefenceRepairCost = GetSiegeDefenceRepairCost();
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (siegeDefenceRepairCost != null && !kingdom.resources.CanAfford(siegeDefenceRepairCost, 1f))
		{
			return false;
		}
		return true;
	}

	public void BoostSiegeDefenceRepairs(bool send_state = true)
	{
		NotifyListeners("fortification_repair_boost_started");
		if (!IsAuthority())
		{
			SendEvent(new BoostSiegeDefenceRepairEvent());
			return;
		}
		keep_effects.active_defence_recovery_boost = true;
		if (send_state)
		{
			SendState<BoostCastleDefenceState>();
		}
	}

	public float GetCurrentSiegeDefence()
	{
		float num = (keep_effects?.siege_defense_condition?.Get() ?? 100f) * 0.01f;
		return GetMaxSiegeDefence() * num;
	}

	public float GetMaxSiegeDefence()
	{
		return GetRealm()?.GetStat(Stats.rs_siege_defense, must_exist: false) ?? 0f;
	}

	public Resource GetSiegeDefenceRepairCost()
	{
		float num = keep_effects.siege_defense_condition.Get() / 100f;
		float num2 = GetRealm()?.GetStat(Stats.rs_siege_defense, must_exist: false) ?? 0f;
		int num3 = (int)(num2 - num2 * num);
		Resource resource;
		if (num3 > 0)
		{
			resource = Resource.Parse(siege_defence_per_point_cost, this, no_null: true);
			resource.Mul(num3);
		}
		else
		{
			resource = new Resource();
		}
		return resource;
	}

	public Resource GetExpandCost()
	{
		Building.Def def = game.defs.GetBase<Building.Def>();
		if (def == null)
		{
			return null;
		}
		return Resource.Parse(def.slots_expand_cost, this, no_null: true);
	}

	public int GetSlotsPerTier()
	{
		return game.defs.GetBase<Building.Def>()?.slots_per_tier ?? 0;
	}

	public bool CanExpandCity()
	{
		return building_slots_tier < GetMaxTier();
	}

	public bool NeedsExpandCity()
	{
		return !CheckMaxBuildings();
	}

	public bool ExpandCity()
	{
		if (!CanExpandCity())
		{
			return false;
		}
		Resource expandCost = GetExpandCost();
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (expandCost != null && !kingdom.resources.CanAfford(expandCost, 1f))
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new ExpandEvent(building_slots_tier + 1));
			return true;
		}
		SetTier(building_slots_tier + 1);
		kingdom.SubResources(KingdomAI.Expense.Category.Economy, expandCost);
		kingdom.NotifyListeners("city_expanded");
		return true;
	}

	public void SetTier(int tier, bool send_state = true)
	{
		if (tier >= 0 && tier <= GetMaxTier() && tier != building_slots_tier && (!send_state || IsAuthority()))
		{
			building_slots_tier = tier;
			if (send_state)
			{
				SendState<StructuresState>();
			}
			NotifyListeners("tier_changed");
		}
	}

	public bool IsBuilding()
	{
		return GetCurrentBuildingBuild() != null;
	}

	public bool IsBuilding(Building.Def building)
	{
		if (building == null)
		{
			return false;
		}
		return GetCurrentBuildingBuild() == building;
	}

	public Building.Def GetCurrentBuildingBuild()
	{
		if (structure_build == null)
		{
			return null;
		}
		return structure_build.GetBuild();
	}

	public float GetBuildPorgress()
	{
		if (structure_build == null)
		{
			return -1f;
		}
		return structure_build.GetProgress();
	}

	public void CancelBuild()
	{
		if (structure_build != null && structure_build.GetBuild() != null)
		{
			structure_build.CancelBuild();
		}
	}

	public void SetCitadelLevel(int citadel_level)
	{
		this.citadel_level = citadel_level;
		NotifyListeners("citadel_level_changed");
	}

	public float GetTaxMod()
	{
		if (population == null)
		{
			return 0f;
		}
		float num = 0f;
		float num2 = 0f;
		for (Population.Type type = Population.Type.Rebel; type < Population.Type.TOTAL; type++)
		{
			int num3 = population.Count(type);
			int num4 = population.Slots(type);
			num += (float)num3 * coef[(int)type];
			num2 += (float)num4 * coef[(int)type];
		}
		if (num == 0f || num2 == 0f)
		{
			return 0f;
		}
		return num / num2;
	}

	private void Discount(Resource cost, ResourceType rt, float bonus)
	{
		if (bonus != 0f)
		{
			float num = cost[rt];
			if (!(num <= 0f))
			{
				float num2 = num * (100f - bonus) / 100f;
				num2 = (float)Math.Ceiling(num2);
				cost[rt] = num2;
			}
		}
	}

	public Resource GetUnitCost(Unit.Def def, Army army = null)
	{
		if (army == null)
		{
			army = this.army;
		}
		population.Recalc();
		Resource resource = new Resource(def.cost);
		if (def.type == Unit.Type.InventoryItem)
		{
			if (def.progressive_cost != null && army != null)
			{
				int num = army.CountInventory(def);
				if (num >= def.progressive_cost.Count)
				{
					num = def.progressive_cost.Count - 1;
				}
				resource = def.progressive_cost[num];
			}
			float value = def.bonus_discount_perc.GetValue(null, -1, army?.leader, garrison, use_battle_bonuses: false);
			Discount(resource, ResourceType.Gold, value);
			Discount(resource, ResourceType.Food, value);
			Discount(resource, ResourceType.Levy, value);
		}
		else
		{
			resource[ResourceType.Gold] *= 1f + GetKingdom().GetStat(Stats.ks_troop_cost_increase) / 100f;
			DevSettings.Def devSettingsDef = game.GetDevSettingsDef();
			resource[ResourceType.Gold] *= devSettingsDef.unit_gold_hire_mod;
			Realm realm = GetRealm();
			float num2 = GetStat(Stats.rs_recruit_cost_bonus_perc) + def.bonus_discount_perc.GetValue(null, -1, army?.leader, garrison, use_battle_bonuses: false);
			float num3 = 0f;
			float num4 = 0f;
			if (def.is_heavy)
			{
				float stat = GetStat(Stats.rs_heavy_discount_perc);
				num2 += stat;
			}
			else if (def.tier == 1)
			{
				float stat2 = GetStat(Stats.rs_light_discount_perc);
				num2 += stat2;
			}
			num3 -= realm.GetKingdom().GetStat(Stats.ks_units_training_gold_perc);
			bool flag = realm.HasTag("peasant_start_bonus") && def.tier == 0;
			bool flag2 = false;
			if (realm.HasTag("royal_discount") && army != null)
			{
				Character leader = army.leader;
				if (leader != null && (leader.IsPrince() || leader.IsKing()))
				{
					flag = true;
					flag2 = true;
				}
			}
			if (num2 != 0f || flag || flag2 || num3 != 0f || num4 != 0f)
			{
				resource = new Resource(resource);
				if (num2 != 0f)
				{
					Discount(resource, ResourceType.Gold, num2);
					Discount(resource, ResourceType.Food, num2);
					Discount(resource, ResourceType.Levy, num2);
				}
				if (num3 != 0f)
				{
					Discount(resource, ResourceType.Gold, num3);
				}
				if (num4 != 0f)
				{
					Discount(resource, ResourceType.Levy, num4);
				}
				if (flag)
				{
					resource.Set(ResourceType.Gold, 0f);
				}
				if (flag2)
				{
					resource.Set(ResourceType.Food, 0f);
				}
			}
		}
		Unit.Def.RoundCost(resource);
		return resource;
	}

	public bool CheckUnitCost(Unit.Def def, Resource cost)
	{
		if (cost == null)
		{
			return true;
		}
		if (cost[ResourceType.Food] > GetFoodStorage())
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return false;
		}
		if (!kingdom.resources.CanAfford(cost, 1f, ResourceType.Food, ResourceType.Workers, ResourceType.Rebels))
		{
			return false;
		}
		int workers = population.GetWorkers();
		if (cost[ResourceType.Workers] > (float)workers)
		{
			return false;
		}
		return true;
	}

	public bool CheckUnitCost(Unit.Def def, Army army = null)
	{
		Resource unitCost = GetUnitCost(def, army);
		return CheckUnitCost(def, unitCost);
	}

	public Resource GetHireResources()
	{
		Kingdom kingdom = GetKingdom();
		Resource resource = ((kingdom == null) ? new Resource() : new Resource(kingdom.resources));
		population.Recalc();
		resource[ResourceType.Food] = GetFoodStorage();
		resource[ResourceType.Workers] = population.GetWorkers();
		return resource;
	}

	public bool HireGarrisonUnit(Unit.Def unitDef)
	{
		if (garrison == null)
		{
			return false;
		}
		return garrison.Hire(unitDef);
	}

	private bool AddGarrisonUnit(Unit unit)
	{
		if (garrison == null)
		{
			return false;
		}
		return garrison.AddUnit(unit);
	}

	public bool CanHireUnit(Unit.Def unitDef, Army army, out Resource cost, bool ignore_slot_count = false, bool ignore_cost = false)
	{
		cost = null;
		if (unitDef == null || army == null)
		{
			return false;
		}
		if (!ignore_slot_count && army.units.Count >= army.MaxUnits() + 1)
		{
			return false;
		}
		if (battle != null)
		{
			return false;
		}
		Realm realm = GetRealm();
		if (realm == null)
		{
			return false;
		}
		if (realm.IsDisorder())
		{
			return false;
		}
		if (unitDef.buildPrerqusite != null && !unitDef.buildPrerqusite.Validate(this))
		{
			return false;
		}
		if (!ignore_cost)
		{
			population.Recalc();
			cost = GetUnitCost(unitDef, army);
			if (!CheckUnitCost(unitDef, cost))
			{
				return false;
			}
		}
		return true;
	}

	public bool HireUnit(Unit.Def unitDef, Army army, int slot_index)
	{
		if (!CanHireUnit(unitDef, army, out var cost))
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new HireUnitEvent(unitDef, army, slot_index));
			return true;
		}
		Unit unit = army.AddUnit(unitDef, slot_index);
		if (unit == null)
		{
			return false;
		}
		unitDef.OnHiredAnalytics(army, null, cost, "castle");
		Realm realm = GetRealm();
		int num = (int)realm.GetStat(Stats.rs_initial_troops_level);
		if (unit.def.type == Unit.Type.Militia && realm.HasTag("peasant_start_bonus"))
		{
			num++;
		}
		unit.AddExperience(unit.def.experience_to_next.GetFloat(num));
		unit.AddExperience(realm.GetStat(Stats.rs_initial_troop_experience));
		if (cost != null)
		{
			int count = Math.Min((int)cost[ResourceType.Workers], population.GetWorkers());
			population.RemoveVillagers(count, Population.Type.Worker);
			AddFood(0f - cost[ResourceType.Food]);
			Kingdom kingdom = GetKingdom();
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, cost[ResourceType.Gold], send_state: false);
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Levy, cost[ResourceType.Levy], send_state: false);
			kingdom.SendState<Kingdom.ResourcesState>();
		}
		if (IsAuthority())
		{
			int num2 = unit.Index();
			if (num2 != -1)
			{
				army?.SendSubstate<Army.UnitsState.UnitState>(num2);
			}
		}
		return true;
	}

	public void HealGarrisonUnits()
	{
		if (!IsAuthority())
		{
			SendEvent(new HealGarrisonUnitsEvent());
		}
		else
		{
			if (garrison == null || garrison.units == null || garrison.units.Count == 0)
			{
				return;
			}
			Realm realm = GetRealm();
			if (realm != null && realm.IsDisorder())
			{
				return;
			}
			Kingdom kingdom = GetKingdom();
			if (kingdom == null)
			{
				return;
			}
			Resource garrisonHealCost = GetGarrisonHealCost();
			if (garrisonHealCost == null)
			{
				return;
			}
			population.Recalc();
			if (CheckUnitCost(null, garrisonHealCost))
			{
				if (garrisonHealCost != null)
				{
					int count = Math.Min((int)garrisonHealCost[ResourceType.Workers], population.GetWorkers());
					population.RemoveVillagers(count, Population.Type.Worker);
					kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, garrisonHealCost[ResourceType.Gold], send_state: false);
					kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Levy, garrisonHealCost[ResourceType.Levy], send_state: false);
					AddFood(-(int)garrisonHealCost.Get(ResourceType.Food));
					kingdom.SendState<Kingdom.ResourcesState>();
					kingdom.InvalidateIncomes();
				}
				garrison.RestUnits();
			}
		}
	}

	public Resource GetGarrisonHealCost()
	{
		if (garrison == null)
		{
			return null;
		}
		if (garrison.units == null)
		{
			return null;
		}
		Resource resource = new Resource();
		for (int i = 0; i < garrison.units.Count; i++)
		{
			Unit unit = garrison.units[i];
			if (unit != null)
			{
				resource.Add(unit.GetHealCost(rounded: false), 1f);
			}
		}
		for (int j = 0; j < 13; j++)
		{
			resource[(ResourceType)j] = (float)Math.Ceiling(resource[(ResourceType)j]);
		}
		if (resource.IsZero())
		{
			return null;
		}
		return resource;
	}

	public bool CanHealGarrisonUnit(int index)
	{
		if (index < 0)
		{
			return false;
		}
		if (garrison == null)
		{
			return false;
		}
		if (garrison.units == null)
		{
			return false;
		}
		if (garrison.units.Count == 0)
		{
			return false;
		}
		if (garrison.units.Count <= index)
		{
			return false;
		}
		Realm realm = GetRealm();
		if (realm != null && realm.IsDisorder())
		{
			return false;
		}
		return true;
	}

	public void HealGarrisonUnit(int index)
	{
		if (!CanHealGarrisonUnit(index))
		{
			return;
		}
		if (!IsAuthority())
		{
			SendEvent(new HealGarrisonUnitEvent(index));
			return;
		}
		Unit unit = garrison.units[index];
		Kingdom kingdom = GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		Resource healCost = unit.GetHealCost();
		if (healCost == null)
		{
			return;
		}
		population.Recalc();
		if (CheckUnitCost(null, healCost))
		{
			if (healCost != null)
			{
				int count = Math.Min((int)healCost[ResourceType.Workers], population.GetWorkers());
				population.RemoveVillagers(count, Population.Type.Worker);
				kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, healCost[ResourceType.Gold], send_state: false);
				kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Levy, healCost[ResourceType.Levy], send_state: false);
				AddFood(0f - healCost[ResourceType.Food]);
				kingdom.SendState<Kingdom.ResourcesState>();
				kingdom.InvalidateIncomes();
			}
			garrison.RestUnit(unit);
		}
	}

	public List<Unit.Def> GetAvailableUnitTypes(AvailableUnits.Source source = AvailableUnits.Source.All)
	{
		return available_units?.GetAvailableUnitTypes(source);
	}

	public float EvalHireUnits(List<Unit> existing_units, int max_units, List<Unit.Def> to_hire = null, List<Unit> to_disband = null, Army army = null, bool for_garrison = false, bool allow_militia = true, bool allow_upgrades = true, float food = float.NegativeInfinity, bool deterministic = false)
	{
		if (battle != null)
		{
			return 0f;
		}
		Realm realm = GetRealm();
		if (realm == null || realm.IsDisorder())
		{
			return 0f;
		}
		int num = existing_units?.Count ?? 0;
		if (num >= max_units)
		{
			if (!allow_upgrades)
			{
				return 0f;
			}
			if (KingdomAI.FindUpgradableUnit(existing_units) == null)
			{
				return 0f;
			}
		}
		float num2 = population.GetWorkers();
		if (num2 <= 0f)
		{
			return 0f;
		}
		Kingdom kingdom = GetKingdom();
		if (food == float.NegativeInfinity)
		{
			food = kingdom.GetFood();
		}
		if (!allow_upgrades && food <= 0f)
		{
			return 0f;
		}
		List<Unit.Def> availableUnitTypes = GetAvailableUnitTypes();
		if (availableUnitTypes == null)
		{
			return 0f;
		}
		tmp_unit_defs.Clear();
		tmp_unit_defs.AddRange(availableUnitTypes);
		availableUnitTypes = tmp_unit_defs;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < availableUnitTypes.Count; i++)
		{
			Unit.Def def = availableUnitTypes[i];
			if (def.buildPrerqusite != null && !def.buildPrerqusite.Validate(this))
			{
				availableUnitTypes.RemoveAt(i);
				i--;
				continue;
			}
			if (def.ai_emergency_only)
			{
				if (!allow_militia)
				{
					availableUnitTypes.RemoveAt(i);
					i--;
					continue;
				}
				num3++;
			}
			else
			{
				num4++;
			}
			if (GetUnitCost(def, army).Get(ResourceType.Workers) > num2)
			{
				if (for_garrison && !def.ai_emergency_only)
				{
					return 0f;
				}
				availableUnitTypes.RemoveAt(i);
				i--;
			}
		}
		if (num3 > 0 && num4 > 0)
		{
			for (int j = 0; j < availableUnitTypes.Count; j++)
			{
				if (availableUnitTypes[j].ai_emergency_only)
				{
					availableUnitTypes.RemoveAt(j);
					j--;
				}
			}
		}
		if (availableUnitTypes.Count <= 0)
		{
			return 0f;
		}
		using (new Stat.ForceCached("EvalHireUnits"))
		{
			WeightedRandom<Unit.Def> temp = WeightedRandom<Unit.Def>.GetTemp();
			Unit.Def def2 = null;
			float num5 = 0f;
			float num6 = 0f;
			int num7 = max_units - num;
			if (allow_upgrades)
			{
				num7 += KingdomAI.NumUpgradableUnits(existing_units);
			}
			for (int k = 0; k < availableUnitTypes.Count; k++)
			{
				Unit.Def def3 = availableUnitTypes[k];
				float num8 = KingdomAI.EvalHireUnit(def3, army);
				if (kingdom.ai != null && kingdom.ai.personality == KingdomAI.AIPersonality.RichArmies)
				{
					num8 *= num8;
				}
				if (!deterministic)
				{
					temp.AddOption(def3, num8);
					continue;
				}
				float num9 = GetUnitCost(def3, army).Get(ResourceType.Workers);
				int num10 = ((num9 <= 0f) ? num7 : ((int)(num2 / num9)));
				if (num10 <= 0)
				{
					continue;
				}
				if (num10 > num7)
				{
					num10 = num7;
				}
				if (food != float.PositiveInfinity)
				{
					float num11 = def3.upkeep.Get(ResourceType.Food);
					if (num11 > 0f)
					{
						int num12 = (int)(food / num11);
						if (num12 < num10)
						{
							num10 = num12;
						}
					}
				}
				float num13 = (float)num10 * num8;
				if (!(num13 <= num6))
				{
					def2 = def3;
					num5 = num8;
					num6 = num13;
				}
			}
			if (deterministic && def2 == null)
			{
				return 0f;
			}
			float num14 = 0f;
			while (true)
			{
				Unit unit = null;
				float eval = 0f;
				if (num >= max_units)
				{
					if (!allow_upgrades)
					{
						break;
					}
					unit = KingdomAI.ChooseWorstUnit(existing_units, army, out eval, upgradable_only: true, to_disband);
					if (unit == null)
					{
						break;
					}
				}
				Unit.Def def4;
				float num15;
				int num16;
				if (deterministic)
				{
					def4 = def2;
					num15 = num5;
					num16 = -1;
				}
				else
				{
					num16 = temp.ChooseIndex();
					if (num16 < 0)
					{
						break;
					}
					WeightedRandom<Unit.Def>.Option option = temp.options[num16];
					def4 = option.val;
					num15 = option.weight;
				}
				float num17 = GetUnitCost(def4, army).Get(ResourceType.Workers);
				if (num17 > num2)
				{
					if (deterministic)
					{
						break;
					}
					temp.DelOptionAtIndex(num16);
					continue;
				}
				if (food != float.PositiveInfinity)
				{
					float num18 = def4.upkeep.Get(ResourceType.Food);
					float num19 = unit?.def.upkeep.Get(ResourceType.Food) ?? 0f;
					if (num18 > food + num19)
					{
						if (deterministic)
						{
							break;
						}
						temp.DelOptionAtIndex(num16);
						continue;
					}
					food -= num18 - num19;
				}
				num2 -= num17;
				to_hire?.Add(def4);
				num14 += num15;
				if (unit != null)
				{
					if (to_disband == null)
					{
						tmp_to_disband.Clear();
						to_disband = tmp_to_disband;
					}
					to_disband.Add(unit);
					num14 -= eval;
				}
				else
				{
					num++;
				}
			}
			return num14;
		}
	}

	public float EvalHireEquipment(List<InventoryItem> existing_units, int max_equipment, List<Unit.Def> to_hire = null, Army army = null, float food = float.NegativeInfinity, bool deterministic = false)
	{
		if (battle != null)
		{
			return 0f;
		}
		Kingdom kingdom = GetKingdom();
		Realm realm = GetRealm();
		if (realm == null || realm.IsDisorder())
		{
			return 0f;
		}
		int num = existing_units?.Count ?? 0;
		if (num >= max_equipment)
		{
			return 0f;
		}
		if (!kingdom.ai.CheckUpkeep(0f, KingdomAI.Expense.Category.Military, "ArmyUpkeep"))
		{
			return 0f;
		}
		float num2 = population.GetWorkers();
		if (food == float.NegativeInfinity)
		{
			food = kingdom.GetFood();
		}
		List<Unit.Def> availableEquipmentTypes = GetAvailableEquipmentTypes();
		if (availableEquipmentTypes == null)
		{
			return 0f;
		}
		tmp_unit_defs.Clear();
		tmp_unit_defs.AddRange(availableEquipmentTypes);
		availableEquipmentTypes = tmp_unit_defs;
		for (int i = 0; i < availableEquipmentTypes.Count; i++)
		{
			Unit.Def def = availableEquipmentTypes[i];
			if (def.buildPrerqusite != null && !def.buildPrerqusite.Validate(this, army.leader))
			{
				availableEquipmentTypes.RemoveAt(i);
				i--;
			}
			else if (GetUnitCost(def, army).Get(ResourceType.Workers) > num2)
			{
				availableEquipmentTypes.RemoveAt(i);
				i--;
			}
		}
		if (availableEquipmentTypes.Count <= 0)
		{
			return 0f;
		}
		using (new Stat.ForceCached("EvalHireEquipment"))
		{
			WeightedRandom<Unit.Def> temp = WeightedRandom<Unit.Def>.GetTemp();
			Unit.Def def2 = null;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = max_equipment - num;
			for (int j = 0; j < availableEquipmentTypes.Count; j++)
			{
				Unit.Def def3 = availableEquipmentTypes[j];
				float num6 = KingdomAI.EvalHireUnit(def3, army);
				if (!deterministic)
				{
					temp.AddOption(def3, num6);
					continue;
				}
				float num7 = GetUnitCost(def3, army).Get(ResourceType.Workers);
				int num8 = ((num7 <= 0f) ? num5 : ((int)(num2 / num7)));
				if (num8 <= 0)
				{
					continue;
				}
				if (num8 > num5)
				{
					num8 = num5;
				}
				if (food != float.PositiveInfinity)
				{
					float num9 = def3.CalcUpkeep(army, null, -1).Get(ResourceType.Food);
					if (num9 > 0f)
					{
						int num10 = (int)(food / num9);
						if (num10 < num8)
						{
							num8 = num10;
						}
					}
				}
				float num11 = (float)num8 * num6;
				if (!(num11 <= num4))
				{
					def2 = def3;
					num3 = num6;
					num4 = num11;
				}
			}
			if (deterministic && def2 == null)
			{
				return 0f;
			}
			float num12 = 0f;
			while (num < max_equipment)
			{
				Unit.Def def4;
				float num13;
				int num14;
				if (deterministic)
				{
					def4 = def2;
					num13 = num3;
					num14 = -1;
				}
				else
				{
					num14 = temp.ChooseIndex();
					if (num14 < 0)
					{
						break;
					}
					WeightedRandom<Unit.Def>.Option option = temp.options[num14];
					def4 = option.val;
					num13 = option.weight;
				}
				float num15 = GetUnitCost(def4, army).Get(ResourceType.Workers);
				if (num15 > num2)
				{
					if (deterministic)
					{
						break;
					}
					temp.DelOptionAtIndex(num14);
					continue;
				}
				float num16 = def4.CalcUpkeep(army, null, -1).Get(ResourceType.Food);
				if (num16 > food)
				{
					if (deterministic)
					{
						break;
					}
					temp.DelOptionAtIndex(num14);
				}
				else
				{
					num2 -= num15;
					food -= num16;
					to_hire?.Add(def4);
					num12 += num13;
					num++;
				}
			}
			return num12;
		}
	}

	public float EvalTakeGarrison(Army army, List<Unit> to_take = null, List<Unit> to_leave = null)
	{
		if (garrison.units.Count == 0)
		{
			return 0f;
		}
		using (new Stat.ForceCached("EvalTakeGarrison"))
		{
			if (to_take == null)
			{
				tmp_to_take.Clear();
				to_take = tmp_to_take;
			}
			if (to_leave == null)
			{
				tmp_to_disband.Clear();
				to_leave = tmp_to_disband;
			}
			float num = 0f;
			int num2 = army?.units.Count ?? 0;
			int num3 = game.ai.army_def.GetMaxUnits(army?.leader, assume_marshal: true) + 1;
			while (true)
			{
				float eval;
				Unit unit = KingdomAI.ChooseBestGarrisonUnit(this, army, out eval, to_take);
				if (unit == null)
				{
					break;
				}
				if (num2 < num3)
				{
					to_take.Add(unit);
					num += eval;
					num2++;
					continue;
				}
				float eval2;
				Unit item = KingdomAI.ChooseWorstArmyUnit(army, out eval2, upgradable_only: false, to_leave);
				if (eval2 >= eval)
				{
					break;
				}
				to_take.Add(unit);
				to_leave.Add(item);
				num += eval - eval2;
			}
			return num;
		}
	}

	public void ResupplyArmy(Army army)
	{
		if (this.army == army && battle == null)
		{
			if (!IsAuthority())
			{
				SendEvent(new ResupplyArmyEvent(army));
			}
			else
			{
				army.resupply_action?.Execute(null);
			}
		}
	}

	public Resource GetHealCost()
	{
		Resource resource = new Resource();
		if (army == null)
		{
			return resource;
		}
		if (battle != null)
		{
			return resource;
		}
		float num = population.GetWorkers();
		float num2 = num;
		List<Unit> list = new List<Unit>(army.units);
		List<Unit> list2 = new List<Unit>();
		list.Sort(delegate(Unit unit3, Unit unit2)
		{
			int num6 = unit2.experience.CompareTo(unit3.experience);
			if (num6 != 0)
			{
				return num6;
			}
			int num7 = unit2.def.cost.Get(ResourceType.Gold).CompareTo(unit3.def.cost.Get(ResourceType.Gold));
			return (num7 != 0) ? num7 : unit2.damage.CompareTo(unit3.damage);
		});
		float num3 = 0f;
		if (list.Count > 0)
		{
			num3 = list[0].damage;
		}
		while (list.Count > 0)
		{
			Unit unit = list[0];
			if (num3 <= 0f)
			{
				list.RemoveAt(0);
				if (list.Count > 0)
				{
					num3 = list[0].damage;
				}
				continue;
			}
			float num4 = unit.def.cost.Get(ResourceType.Workers) / (float)unit.def.health_segments;
			if (num4 <= 0f || num <= 0f)
			{
				list.RemoveAt(0);
				list2.Add(unit);
				if (list.Count > 0)
				{
					num3 = list[0].damage;
				}
				continue;
			}
			num3 -= 0.2f;
			num -= num4;
			if (num3 <= 0f)
			{
				list.RemoveAt(0);
				if (list.Count > 0)
				{
					num3 = list[0].damage;
				}
			}
		}
		if (list2.Count > 0)
		{
			num3 = list2[0].damage;
		}
		if (num < 0f)
		{
			num = 0f;
		}
		int num5 = (int)Math.Ceiling(num2 - num);
		resource.Add(ResourceType.Workers, num5);
		return resource;
	}

	public void HealArmy()
	{
		if (army == null || battle != null)
		{
			return;
		}
		if (!IsAuthority())
		{
			SendEvent(new HealArmyEvent(army));
			return;
		}
		float num = population.GetWorkers();
		float num2 = num;
		List<Unit> list = new List<Unit>(army.units);
		list.Sort(delegate(Unit unit3, Unit unit2)
		{
			int num5 = unit2.experience.CompareTo(unit3.experience);
			if (num5 != 0)
			{
				return num5;
			}
			int num6 = unit2.def.cost.Get(ResourceType.Gold).CompareTo(unit3.def.cost.Get(ResourceType.Gold));
			return (num6 != 0) ? num6 : unit2.damage.CompareTo(unit3.damage);
		});
		List<Unit> list2 = new List<Unit>();
		while (list.Count > 0)
		{
			Unit unit = list[0];
			if (unit.damage <= 0f)
			{
				list.RemoveAt(0);
				continue;
			}
			float num3 = unit.def.cost.Get(ResourceType.Workers) / (float)unit.def.health_segments;
			if (num3 <= 0f || num <= 0f)
			{
				list.RemoveAt(0);
				continue;
			}
			if (!list2.Contains(unit))
			{
				list2.Add(unit);
			}
			unit.damage -= 0.2f;
			num -= num3;
			if (unit.damage <= 0f)
			{
				unit.damage = 0f;
				list.RemoveAt(0);
			}
		}
		if (num < 0f)
		{
			num = 0f;
		}
		population.RemoveVillagers((int)Math.Ceiling(num2 - num), Population.Type.Worker);
		for (int num4 = 0; num4 < list2.Count; num4++)
		{
			army.SendSubstate<Army.UnitsState.UnitState>(army.units.IndexOf(list2[num4]));
		}
		army.NotifyListeners("units_changed");
	}

	public bool BuyEquipments(Unit.Def itemDef, Army army, int slot_index)
	{
		if (itemDef == null)
		{
			return false;
		}
		if (army.siege_equipment.Count >= army.MaxItems())
		{
			return false;
		}
		Resource unitCost = GetUnitCost(itemDef, army);
		if (!CheckUnitCost(itemDef, unitCost))
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new BuyEquipmentEvent(itemDef, army, slot_index));
			return false;
		}
		if (GetKingdom() == null)
		{
			return false;
		}
		if (itemDef.buildPrerqusite != null && !itemDef.buildPrerqusite.Validate(army.castle, army.leader))
		{
			return false;
		}
		population.Recalc();
		if (army.AddInvetoryItem(itemDef, slot_index, send_state: true, send_event: true) == null)
		{
			return false;
		}
		itemDef.OnHiredAnalytics(army, null, unitCost, "castle");
		if (unitCost != null)
		{
			int count = Math.Min((int)unitCost[ResourceType.Workers], population.GetWorkers());
			population.RemoveVillagers(count, Population.Type.Worker);
			AddFood(0f - unitCost[ResourceType.Food]);
			Kingdom kingdom = GetKingdom();
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, unitCost[ResourceType.Gold]);
			kingdom.SubResources(KingdomAI.Expense.Category.Military, ResourceType.Levy, unitCost[ResourceType.Levy]);
			kingdom.InvalidateIncomes();
		}
		return true;
	}

	public float GetManpower()
	{
		int num = 0;
		if (garrison != null)
		{
			garrison.GetManpower(out var cur, out var _);
			num += cur;
		}
		if (battle != null)
		{
			List<Army> armies = battle.GetArmies(1);
			if (armies != null)
			{
				for (int i = 0; i < armies.Count; i++)
				{
					Army army = armies[i];
					if (army != null)
					{
						num += army.GetManPower();
					}
				}
			}
		}
		else
		{
			num += this.army?.GetManPower() ?? 0;
		}
		return num;
	}

	public List<Unit.Def> GetAvailableEquipmentTypes()
	{
		return available_units.GetAvailableEquipmentTypes();
	}

	public float GetStat(string stat_name, bool must_exist = true)
	{
		Realm realm = GetRealm();
		if (realm == null)
		{
			Error("GetStat('" + stat_name + "'): Castle is missing a realm!");
			return 0f;
		}
		if (realm.stats == null)
		{
			if (must_exist)
			{
				Error("GetStat('" + stat_name + "'): Stats not initialized yet!");
			}
			return 0f;
		}
		return realm.stats.Get(stat_name, must_exist);
	}

	public Castle(Game game, Point position)
		: base(game, position, "Castle")
	{
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		if (string.IsNullOrEmpty(customName))
		{
			return "tn_" + name;
		}
		return "#" + customName;
	}

	public void ChangeName(string newName, bool send_state = true)
	{
		GetRealm()?.ChangeTownName(newName, send_state);
	}

	public override string ToString()
	{
		string text = base.ToString();
		if (type != "Castle")
		{
			text = text + " (" + type.ToString() + ")";
		}
		return text + " (" + name + ")";
	}

	public int GetGarrisnoSlotsMaxCount()
	{
		if (garrison != null)
		{
			return garrison.MaxSlotCount();
		}
		return 0;
	}

	public int GetGarrisonSlotCount()
	{
		if (garrison != null)
		{
			return garrison.SlotCount();
		}
		return 0;
	}

	public void OnCompleteBuild(Building.Def def, int preferred_slot_index)
	{
		Building param = AddBuilding(def, preferred_slot_index);
		game.ValidateEndGame(GetKingdom());
		NotifyListeners("build_finished", param);
		GetKingdom()?.NotifyListeners("build_finished", param);
		GetRealm()?.rebellionRisk.Recalc(think_rebel_pop: false, allow_rebel_spawn: false);
	}

	public bool Sack(Army attacker, bool force_leave = true, bool start_repair = false)
	{
		if (!AssertAuthority())
		{
			return false;
		}
		if (force_leave)
		{
			attacker?.LeaveCastle(GetRandomExitPoint());
		}
		if (sacked)
		{
			return false;
		}
		quick_recovery = false;
		if (attacker != null && attacker.supplies != null)
		{
			float foodStorage = GetFoodStorage();
			attacker.AddSupplies(foodStorage);
		}
		SetFood(0f, clamp: false);
		burned_buildings.Clear();
		int num = (int)Math.Ceiling((float)buildings.Count * sacking_comp.def.sacking_burned_structures);
		int count = buildings.Count;
		int num2 = ((count > 0) ? game.Random(0, count - 1) : 0);
		for (int i = 0; i < count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			Building building = buildings[(i + num2) % count];
			if (building != null && building.def != null && sacking_comp != null && sacking_comp.def != null)
			{
				sack_damage += building.GetRepairCost();
				burned_buildings.Add(building);
				num--;
				building.SetState(Building.State.Damaged);
			}
		}
		Realm realm = GetRealm();
		if (burned_buildings.Count == 0)
		{
			BuildOption buildOption = ChooseBuildingToBuild(common_only: true);
			if (buildOption.def != null)
			{
				Resource cost = buildOption.def.GetCost(realm);
				if (cost != null)
				{
					float num3 = cost.Get(ResourceType.Hammers);
					sack_damage += num3 * sacking_comp.def.sacking_repair_coef;
				}
			}
		}
		initial_sack_damage = sack_damage;
		population.RemoveVillagers((int)((float)population.GetWorkers() * sacking_comp.def.sacking_worker_deaths), Population.Type.Worker);
		population.RemoveVillagers((int)((float)population.GetRebels() * sacking_comp.def.sacking_rebellious_deaths), Population.Type.Rebel);
		sacked = true;
		SendState<SackedStructuresState>();
		if (start_repair)
		{
			sacking_comp.Begin();
		}
		if (attacker != null)
		{
			realm.RecalcIncomes();
			if (attacker.leader.IsCrusader())
			{
				Crusade crusade = Crusade.Get(game);
				crusade.wealth += realm.income[ResourceType.Gold] * crusade.def.sackGoldMultiplier;
				crusade.SendState<Crusade.WealthState>();
				crusade.NotifyListeners("wealth_changed");
			}
			else if (attacker.rebel != null)
			{
				attacker.rebel.rebellion.AddWealth(realm.income.Get(ResourceType.Gold) * sacking_comp.def.sacking_gold_mul);
			}
		}
		realm.rebellionRisk.Recalc(think_rebel_pop: true);
		realm.GetKingdom()?.RecalcBuildingStates(this);
		NotifyListeners("structures_sacked");
		NotifyListeners("structures_changed");
		return true;
	}

	public float GetBaseSackDamage()
	{
		float num = 0f;
		if (burned_buildings.Count == 0)
		{
			return -1f;
		}
		for (int i = 0; i < burned_buildings.Count; i++)
		{
			Building building = burned_buildings[i];
			if (burned_buildings[i] != null && building != null && building.def != null && sacking_comp != null && sacking_comp.def != null)
			{
				num += building.GetRepairCost();
			}
		}
		return num;
	}

	public void QuickRecovery()
	{
		if (governor != null && sacked && !quick_recovery)
		{
			Resource resource = GetVar("repair_cost").Get<Resource>();
			Kingdom kingdom = GetRealm().GetKingdom();
			if (kingdom.resources.CanAfford(resource, 1f))
			{
				kingdom.SubResources(KingdomAI.Expense.Category.Economy, resource);
				quick_recovery = true;
				NotifyListeners("quick_recovery");
			}
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		base.DumpInnerState(dump, verbosity);
		dump.Append("army", army?.ToString());
		if (structure_build != null)
		{
			dump.OpenSection("structure_built");
			dump.Append("building", structure_build.current_building_def?.field?.key);
			dump.Append("current_progress", structure_build.current_progress);
			dump.Append("current_repair", structure_build.current_building_repair?.def?.field?.key);
			dump.Append("state", structure_build.GetState().ToString());
			dump.CloseSection("structure_built");
		}
		dump.Append("sacked", sacked.ToString());
		dump.Append("sack_damage", sack_damage);
		dump.Append("food_storage", GetFoodStorage());
		dump.Append("pop_rebels", population.rebels);
		dump.Append("pop_workers", population.workers);
		dump.Append("pop_acc", population.pop_acc);
		dump.Append("morale_perm", morale.permanent_morale);
		dump.Append("morale_temp", morale.temporary_morale);
	}

	public override void CalcTempDefenderStats(out int siege_defense_garrison_manpower, out int siege_defense_temp_defender_manpower, out int levy_manpower, out int levy_squads, out int excess_levy, out int town_guard_squads, out int excess_town_guard, out int worker_squads, out int excess_worker)
	{
		Realm realm = GetRealm();
		Resource resource = realm?.income;
		int levy = 0;
		int town_guards = 0;
		int workers = 0;
		if (resource != null)
		{
			levy = 0;
			town_guards = (int)resource.Get(ResourceType.TownGuards);
			workers = ((population != null) ? population.workers : 0);
		}
		if (realm != null && realm.IsDisorder())
		{
			workers = 0;
		}
		SumTempDefenders(levy, town_guards, workers, out siege_defense_garrison_manpower, out siege_defense_temp_defender_manpower, out levy_manpower, out levy_squads, out excess_levy, out town_guard_squads, out excess_town_guard, out worker_squads, out excess_worker);
	}

	public Castle(Multiplayer multiplayer)
		: base(multiplayer)
	{
		InitComponents();
	}

	public new static Object Create(Multiplayer multiplayer)
	{
		return new Castle(multiplayer);
	}

	public void ClearResourcesInfo(bool full = true, bool invalidate_kingdom = true)
	{
		if (invalidate_kingdom)
		{
			GetKingdom()?.RefreshResourcesInfo();
		}
		if (resources_info == null)
		{
			return;
		}
		if (full)
		{
			resources_info_valid = false;
		}
		foreach (KeyValuePair<string, ResourceInfo> item in resources_info)
		{
			_ = item.Key;
			item.Value.Clear(full);
		}
	}

	public ResourceInfo GetResourceInfo(string name, bool create_if_needed = true, bool calc_if_needed = true)
	{
		Kingdom kingdom = GetKingdom();
		using (Game.Profile("Castle.Get Kingdom ResourceInfo"))
		{
			kingdom?.GetResourcesInfo();
		}
		using (Game.Profile("Castle.GetResourceInfo"))
		{
			MatchResourceInfoBuildingInstances(instant: true);
			if (resources_info == null)
			{
				if (!create_if_needed)
				{
					return null;
				}
				ResourceInfo.dictionary_allocs++;
				resources_info = new Dictionary<string, ResourceInfo>(128);
			}
			ResourceInfo.dictionary_lookups++;
			resources_info.TryGetValue(name, out var value);
			if (value == null)
			{
				if (!create_if_needed)
				{
					return null;
				}
				value = new ResourceInfo(this, name);
				ResourceInfo.dictionary_adds++;
				resources_info.Add(name, value);
			}
			value.ClearIfOutOfDate();
			if (value.availability == ResourceInfo.Availability.Unknown && calc_if_needed)
			{
				kingdom?.CalcResourceAvailabilityInCastle(value);
			}
			return value;
		}
	}

	public bool MatchResourceInfoBuildingInstances(bool instant = false, bool forced = false)
	{
		if (resources_info == null)
		{
			return false;
		}
		if (!instant)
		{
			match_res_info_buildings = true;
			return false;
		}
		if (!match_res_info_buildings && !forced)
		{
			return false;
		}
		match_res_info_buildings = false;
		using (Game.Profile("MatchResourceInfoBuildingInstances"))
		{
			bool result = false;
			foreach (KeyValuePair<string, ResourceInfo> item in resources_info)
			{
				ResourceInfo value = item.Value;
				if (value.producers == null)
				{
					continue;
				}
				for (int i = 0; i < value.producers.Count; i++)
				{
					ResourceInfo.ProducerRef value2 = value.producers[i];
					if (value2.bdef != null)
					{
						Building building = FindBuilding(value2.bdef);
						if (building != value2.building)
						{
							result = true;
							value2.building = building;
							value.producers[i] = value2;
						}
					}
				}
			}
			return result;
		}
	}

	public bool DelResourcesInfoFromKingdom()
	{
		if (resources_info == null)
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom?.resources_info == null)
		{
			return false;
		}
		using (Game.Profile("Castle.DelResourcesInfoFromKingdom"))
		{
			foreach (KeyValuePair<string, ResourceInfo> item2 in resources_info)
			{
				string key = item2.Key;
				_ = item2.Value;
				ResourceInfo resourceInfo = kingdom.GetResourceInfo(key, create_if_needed: false, calc_if_needed: false);
				if (resourceInfo?.producers != null)
				{
					ResourceInfo.ProducerRef item = new ResourceInfo.ProducerRef
					{
						type = "Castle",
						castle = this
					};
					resourceInfo.producers.Remove(item);
				}
			}
		}
		return true;
	}

	public bool AddResourcesInfoToKingdom()
	{
		if (resources_info == null || !resources_info_valid)
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom?.resources_info == null)
		{
			return false;
		}
		using (Game.Profile("Castle.AddResourcesInfoToKingdom"))
		{
			foreach (KeyValuePair<string, ResourceInfo> item2 in resources_info)
			{
				string key = item2.Key;
				ResourceInfo value = item2.Value;
				if (value.producers != null && value.producers.Count != 0)
				{
					ResourceInfo resourceInfo = kingdom.GetResourceInfo(key, create_if_needed: false);
					if (resourceInfo?.producers != null)
					{
						ResourceInfo.ProducerRef item = new ResourceInfo.ProducerRef
						{
							type = "Castle",
							castle = this
						};
						resourceInfo.producers.Remove(item);
					}
				}
			}
		}
		return true;
	}
}
