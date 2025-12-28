using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Logic.ExtensionMethods;

namespace Logic;

public class KingdomAI : BaseObject
{
	public class Expense : IPoolableClass
	{
		public enum Type
		{
			None,
			HireChacacter,
			HireArmyUnit,
			HireGarrison,
			BuildStructure,
			Upgrade,
			ExpandCity,
			UpgradeFortifications,
			IncreaseCrownAuthority,
			AdoptTradition,
			ExecuteAction,
			ExecuteOpportunity,
			HireMercenaryArmy,
			HireArmyEquipment
		}

		public enum Category
		{
			None,
			Military,
			Economy,
			Diplomacy,
			Espionage,
			Religion,
			Other,
			COUNT
		}

		public enum Priority
		{
			Low = 1,
			Normal = 10,
			High = 1000,
			Urgent = 1000000
		}

		public Kingdom kingdom;

		public Type type;

		public Category category;

		public Priority priority = Priority.Normal;

		public BaseObject defParam;

		public Object objectParam;

		public List<Value> args;

		public Resource cost = new Resource();

		public Resource kingdom_cost = new Resource();

		public Resource realm_cost = new Resource();

		public float upkeep_gold;

		public string upkeep_subcategory;

		public float eval;

		public const int MAX_EVAL = 30;

		public static ResourceType[] kingdom_resources = new ResourceType[5]
		{
			ResourceType.Gold,
			ResourceType.Books,
			ResourceType.Piety,
			ResourceType.Trade,
			ResourceType.Levy
		};

		public Game game => kingdom.game;

		public static Expense New()
		{
			return ClassPool<Expense>.New();
		}

		public void Delete()
		{
			ClassPool<Expense>.Delete(this);
		}

		public void Set(Kingdom kingdom, Type type, Category category = Category.None, Priority priority = Priority.Normal, BaseObject defParam = null, Object objectParam = null, List<Value> args = null)
		{
			this.kingdom = kingdom;
			this.type = type;
			this.category = category;
			this.priority = priority;
			this.defParam = defParam;
			this.objectParam = objectParam;
			this.args = args;
			Evaluate();
		}

		public void Evaluate()
		{
			CalcCost(GetCost());
			eval = EvaluateCost();
			CalcUpkeep();
			if (eval < 30f && priority != Priority.Urgent)
			{
				CheckUpkeepBudget();
			}
		}

		public void Set(Expense e)
		{
			kingdom = e.kingdom;
			type = e.type;
			category = e.category;
			priority = e.priority;
			defParam = e.defParam;
			objectParam = e.objectParam;
			args = e.args;
			cost.Set(e.cost, 1f);
			kingdom_cost.Set(e.kingdom_cost, 1f);
			realm_cost.Set(e.realm_cost, 1f);
			upkeep_gold = e.upkeep_gold;
			upkeep_subcategory = e.upkeep_subcategory;
			eval = e.eval;
		}

		public void OnPoolAllocated()
		{
			Set(null, Type.None);
		}

		public override string ToString()
		{
			string text = $"{type}";
			if (type == Type.None)
			{
				return text;
			}
			if (priority != Priority.Normal)
			{
				text = $"[{priority}] {text}";
			}
			text = $"[{category}] {text}";
			if (defParam != null)
			{
				text = ((!(defParam is Logic.Def def)) ? ((!(defParam is Action action)) ? $"{text} ({defParam})" : ((!(action.owner is Character character)) ? ((!(action.owner is Kingdom kingdom)) ? (text + " (" + action.def.id + ")") : (text + " (" + kingdom.Name + "." + action.def.id + ")")) : (text + " (" + character.class_def?.name + " " + character.Name + "." + action.def.id + ")"))) : (text + " (" + def.id + ")"));
			}
			if (objectParam != null)
			{
				string text2 = "in";
				if (defParam is Action)
				{
					text2 = "->";
				}
				text = ((!(objectParam is Castle castle)) ? ((!(objectParam is Kingdom kingdom2)) ? $"{text} {text2} {objectParam}" : (text + " " + text2 + " " + kingdom2.Name)) : (text + " " + text2 + " " + castle.name));
			}
			text = ((!(cost == null) && !cost.IsZero()) ? (text + $" -> {cost}") : (text + " (Free)"));
			if (upkeep_gold > 0f)
			{
				text += $", upkeep: {upkeep_gold}";
				if (!string.IsNullOrEmpty(upkeep_subcategory))
				{
					text = text + " (" + upkeep_subcategory + ")";
				}
			}
			return text;
		}

		public int CompareTo(Expense ex)
		{
			if (type == Type.None)
			{
				if (ex.type == Type.None)
				{
					return 0;
				}
				return 1;
			}
			if (ex.type == Type.None)
			{
				return -1;
			}
			int num = ex.priority.CompareTo(priority);
			if (num != 0)
			{
				return num;
			}
			num = eval.CompareTo(ex.eval);
			if (num != 0)
			{
				return num;
			}
			return 0;
		}

		public void CalcCost(Resource cost)
		{
			kingdom_cost.Clear();
			realm_cost.Clear();
			this.cost.Set(cost, 1f, ResourceType.Hammers);
			if (cost == null)
			{
				return;
			}
			for (int i = 0; i < kingdom_resources.Length; i++)
			{
				ResourceType rt = kingdom_resources[i];
				float num = cost[rt];
				if (!(num <= 0f))
				{
					kingdom_cost.Add(rt, num);
				}
			}
			realm_cost.Add(cost, 1f, kingdom_resources);
		}

		public float EvaluateCost()
		{
			float num = 0f;
			for (int i = 0; i < kingdom_resources.Length; i++)
			{
				ResourceType resourceType = kingdom_resources[i];
				if (resourceType == ResourceType.Hammers)
				{
					continue;
				}
				float num2 = kingdom_cost[resourceType];
				if (num2 <= 0f)
				{
					continue;
				}
				float num3 = kingdom.resources[resourceType];
				if (!(num3 >= num2))
				{
					if (resourceType == ResourceType.Trade)
					{
						return 30f;
					}
					float num4 = kingdom.income[resourceType] - kingdom.expenses[resourceType];
					if (resourceType == ResourceType.Gold)
					{
						num4 += kingdom.inflation;
					}
					if (num4 <= 0f)
					{
						return 30f;
					}
					int num5 = (int)((num2 - num3) / num4);
					num5 &= -8;
					if ((float)num5 > num)
					{
						num = num5;
					}
				}
			}
			if (type == Type.HireChacacter)
			{
				num *= 0.5f;
			}
			return num;
		}

		public void CheckUpkeepBudget()
		{
			if (!(upkeep_gold <= 0f) && !kingdom.ai.CheckUpkeep(upkeep_gold, category, upkeep_subcategory))
			{
				eval = 30f;
			}
		}

		public bool CanAfford()
		{
			if (!kingdom.resources.CanAfford(kingdom_cost, 1f))
			{
				return false;
			}
			return true;
		}

		public Resource GetCost()
		{
			switch (type)
			{
			case Type.None:
				return null;
			case Type.HireChacacter:
			{
				CharacterClass.Def def = defParam as CharacterClass.Def;
				return ForHireStatus.GetCost(game, kingdom, def?.id);
			}
			case Type.HireArmyUnit:
			case Type.HireGarrison:
			case Type.HireArmyEquipment:
			{
				Unit.Def def3 = defParam as Unit.Def;
				Castle castle2 = objectParam as Castle;
				if (def3 == null)
				{
					return null;
				}
				if (castle2 != null)
				{
					Army army = castle2.army;
					return castle2.GetUnitCost(def3, army);
				}
				return null;
			}
			case Type.HireMercenaryArmy:
				if (!(objectParam is Mercenary merc))
				{
					return null;
				}
				if (defParam is MercenaryMission.Def def2)
				{
					return def2.GetCost(merc, kingdom);
				}
				return null;
			case Type.BuildStructure:
			case Type.Upgrade:
			{
				if (!(defParam is Building.Def def5))
				{
					return null;
				}
				Realm realm = (objectParam as Castle)?.GetRealm();
				def5.GetCost(realm, kingdom);
				return def5.cost;
			}
			case Type.ExpandCity:
				if (!(objectParam is Castle castle))
				{
					return null;
				}
				return castle.GetExpandCost();
			case Type.UpgradeFortifications:
				if (!(objectParam is Castle castle3))
				{
					return null;
				}
				return castle3.GetFortificationsUpgradeCost();
			case Type.IncreaseCrownAuthority:
				return kingdom.GetCrownAuthority().GetCost();
			case Type.AdoptTradition:
				if (!(defParam is Tradition.Def def4))
				{
					return null;
				}
				return def4.GetAdoptCost(kingdom);
			case Type.ExecuteAction:
			{
				if (!(defParam is Action action))
				{
					return null;
				}
				Object target = objectParam;
				return action.GetCost(target);
			}
			case Type.ExecuteOpportunity:
				if (!(defParam is Opportunity opportunity))
				{
					return null;
				}
				using (new Opportunity.TempActionArgs(opportunity.action, opportunity.action.target, opportunity.args))
				{
					return opportunity.action.GetCost(opportunity.target);
				}
			default:
				return null;
			}
		}

		public void CalcUpkeep()
		{
			upkeep_gold = 0f;
			upkeep_subcategory = null;
			switch (type)
			{
			case Type.HireChacacter:
			{
				CharacterClass.Def def = defParam as CharacterClass.Def;
				upkeep_gold = kingdom.NewCharacterWage(def);
				break;
			}
			case Type.HireArmyEquipment:
			{
				Unit.Def def3 = defParam as Unit.Def;
				if (objectParam is Castle { army: { } army })
				{
					Resource resource = def3.CalcUpkeep(army, null, -1);
					upkeep_gold = resource.Get(ResourceType.Gold);
					upkeep_subcategory = "ArmyUpkeep";
				}
				break;
			}
			case Type.HireMercenaryArmy:
				if (objectParam is Mercenary merc && defParam is MercenaryMission.Def def2)
				{
					Resource upkeep2 = def2.GetUpkeep(merc, kingdom);
					upkeep_gold = upkeep2.Get(ResourceType.Gold);
					upkeep_subcategory = "ArmyUpkeep";
				}
				break;
			case Type.ExecuteAction:
				if (defParam is Action action)
				{
					Resource upkeep3 = action.GetUpkeep();
					if (!(upkeep3 == null))
					{
						upkeep_gold = upkeep3.Get(ResourceType.Gold);
						upkeep_subcategory = action.def.upkeep_subcategory;
					}
				}
				break;
			case Type.ExecuteOpportunity:
				if (defParam is Opportunity opportunity)
				{
					Resource upkeep = opportunity.action.GetUpkeep();
					if (!(upkeep == null))
					{
						upkeep_gold = upkeep.Get(ResourceType.Gold);
						upkeep_subcategory = opportunity.action.def.upkeep_subcategory;
					}
				}
				break;
			}
		}

		public bool Validate()
		{
			switch (type)
			{
			case Type.HireChacacter:
				if (kingdom.GetFreeCourtSlotIndex() < 0)
				{
					return false;
				}
				return true;
			case Type.HireArmyEquipment:
				if (!(defParam is Unit.Def def4))
				{
					return false;
				}
				if (objectParam is Castle castle5)
				{
					if (castle5.GetKingdom() != kingdom)
					{
						return false;
					}
					Army army2 = castle5.army;
					if (army2 == null)
					{
						return false;
					}
					bool flag = castle5?.available_units?.CanBuyEquipment(def4) ?? false;
					if (!flag || !castle5.CheckUnitCost(def4, castle5.GetUnitCost(def4, army2)) || !flag)
					{
						return false;
					}
					if (army2.siege_equipment.Count >= army2.MaxItems())
					{
						return false;
					}
					return true;
				}
				return false;
			case Type.HireArmyUnit:
				if (!(defParam is Unit.Def def3))
				{
					return false;
				}
				if (objectParam is Castle castle4)
				{
					if (castle4.GetKingdom() != kingdom)
					{
						return false;
					}
					Army army = castle4.army;
					if (army == null)
					{
						return false;
					}
					if (!castle4.CanHireUnit(def3, army, out var _, ignore_slot_count: true))
					{
						return false;
					}
					if (IsFull(army))
					{
						if (def3.ai_emergency_only)
						{
							return false;
						}
						if (FindUpgradableUnit(army.units) == null)
						{
							return false;
						}
					}
					return true;
				}
				if (objectParam is Mercenary mercenary)
				{
					return mercenary.IsValid();
				}
				return false;
			case Type.HireMercenaryArmy:
				if (!(objectParam is Mercenary mercenary2))
				{
					return false;
				}
				if (!mercenary2.IsValid())
				{
					return false;
				}
				if (defParam is MercenaryMission.Def def5 && def5.Validate(mercenary2, kingdom))
				{
					return true;
				}
				return false;
			case Type.HireGarrison:
			{
				Unit.Def def = defParam as Unit.Def;
				Castle castle2 = objectParam as Castle;
				if (def == null)
				{
					return false;
				}
				if (castle2 == null || castle2.GetKingdom() != kingdom)
				{
					return false;
				}
				if (castle2.garrison.CheckHire(def, check_cost: false, check_max_slots: false) != 0)
				{
					return false;
				}
				if (castle2.garrison.units.Count >= castle2.garrison.MaxSlotCount())
				{
					if (def.ai_emergency_only)
					{
						return false;
					}
					if (FindUpgradableUnit(castle2.garrison.units) == null)
					{
						return false;
					}
				}
				return true;
			}
			case Type.BuildStructure:
			case Type.Upgrade:
			{
				Building.Def def2 = defParam as Building.Def;
				Castle castle3 = objectParam as Castle;
				if (def2 == null)
				{
					return false;
				}
				if (castle3 == null || castle3.GetKingdom() != kingdom)
				{
					return false;
				}
				if (castle3.battle != null)
				{
					return false;
				}
				if (!castle3.CheckAIBuildingReservations(def2))
				{
					return false;
				}
				if (castle3.CanBuildBuilding(def2, ignore_cost: true) > Castle.StructureBuildAvailability.Available)
				{
					return false;
				}
				return true;
			}
			case Type.ExpandCity:
				if (!(objectParam is Castle castle6) || castle6.GetKingdom() != kingdom)
				{
					return false;
				}
				if (castle6.battle != null)
				{
					return false;
				}
				if (!castle6.CanExpandCity())
				{
					return false;
				}
				return true;
			case Type.UpgradeFortifications:
				if (!(objectParam is Castle castle) || castle.GetKingdom() != kingdom)
				{
					return false;
				}
				if (!castle.CanUpgradeFortification())
				{
					return false;
				}
				if (!castle.CanAffordFortificationsUpgrade())
				{
					return false;
				}
				return true;
			case Type.IncreaseCrownAuthority:
			{
				CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
				if (crownAuthority.GetValue() >= crownAuthority.Max())
				{
					return false;
				}
				return true;
			}
			case Type.AdoptTradition:
				if (!(defParam is Tradition.Def tdef))
				{
					return false;
				}
				if (!kingdom.CanAddTradition(tdef))
				{
					return false;
				}
				return true;
			case Type.ExecuteAction:
			{
				if (!(defParam is Action action))
				{
					return false;
				}
				if (!action.owner.IsValid() || action.owner.GetKingdom() != kingdom)
				{
					return false;
				}
				if (action.AIValidate() != "ok")
				{
					return false;
				}
				if (!action.ValidateTarget(objectParam))
				{
					return false;
				}
				List<Value> list = action.args;
				action.args = args;
				bool num = action.ValidateArgs();
				action.args = list;
				if (!num)
				{
					return false;
				}
				if (!action.CheckProCons())
				{
					return false;
				}
				return true;
			}
			case Type.ExecuteOpportunity:
			{
				Opportunity opportunity = defParam as Opportunity;
				if (opportunity?.action == null)
				{
					return false;
				}
				if (!opportunity.action.owner.IsValid() || opportunity.action.owner.GetKingdom() != kingdom)
				{
					return false;
				}
				if (opportunity.AIValidate() != "ok")
				{
					return false;
				}
				if (!opportunity.action.CheckProCons())
				{
					return false;
				}
				return true;
			}
			default:
				return true;
			}
		}

		public bool Spend()
		{
			switch (type)
			{
			case Type.None:
				return false;
			case Type.HireChacacter:
			{
				if (!(defParam is CharacterClass.Def def3))
				{
					return false;
				}
				Castle castle6 = null;
				if (def3.name == "Marshal")
				{
					castle6 = kingdom.ai.DecideOwnCastleForArmy(null);
					if (castle6 == null)
					{
						return true;
					}
				}
				Character character = kingdom.HireCharacter(def3.id);
				if (character == null)
				{
					return false;
				}
				if (castle6 != null)
				{
					character.SpawnArmy(castle6);
				}
				return true;
			}
			case Type.HireArmyEquipment:
				if (!(defParam is Unit.Def itemDef))
				{
					return false;
				}
				if (objectParam is Castle { army: var army } castle3)
				{
					if (army == null)
					{
						return false;
					}
					return castle3.BuyEquipments(itemDef, army, -1);
				}
				return false;
			case Type.HireArmyUnit:
			{
				Unit.Def udef = defParam as Unit.Def;
				if (udef == null)
				{
					return false;
				}
				if (objectParam is Castle { army: var army2 } castle5)
				{
					if (army2 == null)
					{
						return false;
					}
					if (IsFull(army2))
					{
						if (udef.ai_emergency_only)
						{
							return false;
						}
						float num;
						Unit unit = ChooseWorstArmyUnit(army2, out num, upgradable_only: true);
						if (unit == null)
						{
							return false;
						}
						if (udef == unit.def)
						{
							return false;
						}
						if (!castle5.CanHireUnit(udef, army2, out var _, ignore_slot_count: true))
						{
							return false;
						}
						if (castle5.garrison.units.Count >= castle5.garrison.SlotCount())
						{
							army2.DelUnit(unit);
						}
						else
						{
							army2.MoveUnitToGarrison(unit);
						}
					}
					return castle5.HireUnit(udef, army2, -1);
				}
				if (objectParam is Mercenary mercenary2)
				{
					Army army3 = mercenary2.buyers.Find((Army a) => a.kingdom_id == kingdom.id);
					if (army3 == null)
					{
						return true;
					}
					return mercenary2.Buy(mercenary2.army.units.FindIndex((Unit u) => u.def == udef), army3);
				}
				return false;
			}
			case Type.HireMercenaryArmy:
				if (objectParam is Mercenary mercenary && defParam is MercenaryMission.Def mission && mercenary.HireForKingdom(kingdom, mission))
				{
					return true;
				}
				return false;
			case Type.HireGarrison:
			{
				Unit.Def def5 = defParam as Unit.Def;
				Castle castle7 = objectParam as Castle;
				if (def5 == null)
				{
					return false;
				}
				if (castle7 == null)
				{
					return false;
				}
				if (castle7.garrison.units.Count >= castle7.garrison.SlotCount())
				{
					float num2;
					Unit unit2 = ChooseWorstGarrisonUnit(castle7, out num2, upgradable_only: true);
					if (unit2 == null)
					{
						return false;
					}
					if (def5 == unit2.def)
					{
						return false;
					}
					if (castle7.garrison.CheckHire(def5, check_cost: true, check_max_slots: false) != 0)
					{
						return false;
					}
					castle7.garrison.DelUnit(unit2);
				}
				return castle7.HireGarrisonUnit(def5);
			}
			case Type.BuildStructure:
			case Type.Upgrade:
			{
				Building.Def def2 = defParam as Building.Def;
				Castle castle2 = objectParam as Castle;
				if (def2 == null)
				{
					return false;
				}
				if (castle2 == null)
				{
					return false;
				}
				if (castle2.battle != null)
				{
					return false;
				}
				if (!castle2.CheckAIBuildingReservations(def2))
				{
					return false;
				}
				if (!castle2.BuildBuilding(def2))
				{
					return false;
				}
				return true;
			}
			case Type.ExpandCity:
			{
				Building.Def def = defParam as Building.Def;
				if (!(objectParam is Castle castle))
				{
					return false;
				}
				if (!castle.ExpandCity())
				{
					return false;
				}
				if (def != null)
				{
					castle.BuildBuilding(def);
				}
				return true;
			}
			case Type.UpgradeFortifications:
			{
				if (!(objectParam is Castle castle4))
				{
					return false;
				}
				Resource fortificationsUpgradeCost = castle4.GetFortificationsUpgradeCost();
				if (!kingdom.resources.CanAfford(fortificationsUpgradeCost, 1f, ResourceType.Hammers))
				{
					return false;
				}
				kingdom.SubResources(Category.Military, fortificationsUpgradeCost);
				castle4.UpgradeFortification();
				return true;
			}
			case Type.IncreaseCrownAuthority:
				if (!kingdom.GetCrownAuthority().IncreaseValueWithGold())
				{
					return false;
				}
				return true;
			case Type.AdoptTradition:
			{
				if (!(defParam is Tradition.Def def4))
				{
					return false;
				}
				Resource adoptCost = def4.GetAdoptCost(kingdom);
				if (!kingdom.resources.CanAfford(adoptCost, 1f))
				{
					return false;
				}
				kingdom.SubResources(Category.Economy, adoptCost);
				kingdom.AddTradition(def4);
				return true;
			}
			case Type.ExecuteAction:
			{
				Action action = defParam as Action;
				Object target = objectParam;
				if (action == null)
				{
					return false;
				}
				action.args = args;
				return action.Execute(target);
			}
			case Type.ExecuteOpportunity:
			{
				Opportunity opportunity = defParam as Opportunity;
				if (opportunity?.action == null)
				{
					return false;
				}
				opportunity.action.args = opportunity.args;
				if (!opportunity.action.HasAllArgs())
				{
					return false;
				}
				return opportunity.action.Execute(opportunity.target);
			}
			default:
				return false;
			}
		}
	}

	public class CategoryData
	{
		public class UpkeepData
		{
			public DT.Field def;

			public CategoryData category;

			public UpkeepData parent;

			public string subcategory;

			public List<string> var_mods;

			public List<IncomeModifier> mods;

			public float budget;

			public float upkeep;

			public override string ToString()
			{
				if (category == null)
				{
					return "null";
				}
				string text = category.category.ToString();
				if (!string.IsNullOrEmpty(subcategory))
				{
					text = text + "." + subcategory;
				}
				float num = category.kingdom.income[ResourceType.Gold];
				float num2 = budget / 100f * num;
				float num3 = upkeep / num * 100f;
				return text + $" {upkeep:N0} Gold ({num3:N0}%) / {num2:N0} Gold ({budget:N0}%) of {num} Gold";
			}
		}

		public Kingdom kingdom;

		public Expense.Category category;

		public float budget;

		public Resource spent = new Resource();

		public List<UpkeepData> upkeeps;

		public Expense last_expense = new Expense();

		public Expense last_upkeep_expense = new Expense();

		public Expense next_expense = new Expense();

		public float weight = 100f;

		public CategoryData(Kingdom kingdom, Expense.Category category)
		{
			this.kingdom = kingdom;
			this.category = category;
		}

		public override string ToString()
		{
			string text = "";
			UpkeepData upkeepData = FindUpkeepData(null);
			if (upkeepData != null)
			{
				float num = kingdom.income[ResourceType.Gold];
				float num2 = upkeepData.upkeep / num * 100f;
				text = $" Gold Upkeep {num2:N0}% of {upkeepData.budget:N0}%";
			}
			return $"       weight {weight:N0} --- Gold Spent {PercGoldSpent():N0}% of {BudgetPerc():N0}% --- {text}";
		}

		public UpkeepData FindUpkeepData(string subcategory)
		{
			if (upkeeps == null)
			{
				return null;
			}
			for (int i = 0; i < upkeeps.Count; i++)
			{
				UpkeepData upkeepData = upkeeps[i];
				if (upkeepData.subcategory == subcategory)
				{
					return upkeepData;
				}
			}
			return null;
		}

		public string Dump()
		{
			string text = ToString();
			if (upkeeps != null && upkeeps.Count > 0)
			{
				text += "\n       Upkeeps:";
				for (int i = 0; i < upkeeps.Count; i++)
				{
					UpkeepData arg = upkeeps[i];
					if (i > 0)
					{
						text += " +++ ";
					}
					text += $"{arg}";
				}
			}
			if (last_expense.type != Expense.Type.None)
			{
				text += $"\n         Last expense: {last_expense}";
			}
			if (next_expense.type != Expense.Type.None)
			{
				text += $"\n         Next expense: {next_expense}";
			}
			if (last_upkeep_expense.type != Expense.Type.None)
			{
				text += $"\n         Last upkeep expense: {last_upkeep_expense}";
			}
			string text2 = $"{spent}";
			if (text2.Length > 2)
			{
				text = text + "\n         Expected spent: " + text2;
			}
			if ($"{kingdom.spent_by_category[(int)category]}".Length > 2)
			{
				text = text + "\n         Actually spent: " + spent;
			}
			return text;
		}

		public float PercGoldSpent()
		{
			float num = kingdom.total_earned[ResourceType.Gold];
			if (num <= 0f)
			{
				return 0f;
			}
			if (category == Expense.Category.None)
			{
				return kingdom.resources[ResourceType.Gold] / num * 100f;
			}
			Resource resource = kingdom.spent_by_category[(int)category];
			if (resource == null)
			{
				return 0f;
			}
			return resource[ResourceType.Gold] / num * 100f;
		}

		public float BudgetPerc()
		{
			return budget;
		}
	}

	public struct OfferToPlayer
	{
		public Time t_last_offer;

		public Dictionary<string, Time> t_last_answer_per_offer;

		public int kingdom_id;
	}

	public struct GovernOption
	{
		public Character governor;

		public Castle castle;

		public float eval;

		public Resource prod_bonus;

		public override string ToString()
		{
			if (governor == null)
			{
				return "null";
			}
			string text = ((castle.governor == governor) ? "[current]" : ((governor.GetPreparingToGovernCastle() != castle) ? "" : "[preparing]"));
			if (governor.ai_selected_govern_option.castle == castle)
			{
				text += "[selected]";
			}
			return $"[{eval}]{text} {governor.class_name} {governor.Name} -> {castle.name}: {prod_bonus}";
		}

		public float Eval(Resource add_bonus = null)
		{
			if (prod_bonus != null)
			{
				prod_bonus.Clear();
			}
			prod_bonus = castle.CalcGovernorProduction(prod_bonus, governor);
			if (add_bonus != null)
			{
				prod_bonus.Add(add_bonus, 1f);
			}
			Resource weights = castle.AISpecProductionWeights(governor, check_low_food: false);
			eval = prod_bonus.Eval(weights);
			Kingdom kingdom = castle.GetKingdom();
			Realm realm = castle.GetRealm();
			if (realm != null && kingdom != null && kingdom.ai != null && kingdom.ai.personality != AIPersonality.Default && ((realm.ai_specialization == AI.ProvinceSpecialization.ReligionSpec && governor.IsCleric()) || (realm.ai_specialization == AI.ProvinceSpecialization.MilitarySpec && governor.IsMarshal()) || (realm.ai_specialization == AI.ProvinceSpecialization.TradeSpec && governor.IsMerchant())))
			{
				eval = 1.25f * eval + 2500f;
			}
			if ((governor.governed_castle ?? governor.GetPreparingToGovernCastle()) == castle)
			{
				eval += 1000f;
			}
			return eval;
		}
	}

	public class Def : Logic.Def
	{
		public float dip_imp_recent_rel_change_min_time = 5f;

		public float dip_imp_recent_rel_change_max_time = 45f;

		public float dip_imp_player_cooldown_min;

		public float dip_imp_player_cooldown_max = 300f;

		public float dip_imp_player_cooldown_power = 2f;

		public float dip_imp_neighbor = 2000f;

		public float dip_imp_second_neighbor = 500f;

		public float dip_imp_second_neighbor_max_dist = 3f;

		public float dip_imp_second_kin = 2000f;

		public float dip_imp_crusade_or_jihad = 5000f;

		public float dip_imp_ally = 5000f;

		public float dip_imp_have_trade = 250f;

		public float dip_imp_have_exclusive_trade = 750f;

		public float dip_imp_currently_trading_mod = 2f;

		public float dip_imp_player_boost_mul = 500f;

		public float dip_imp_player_boost_distance_max = 4f;

		public float dip_imp_player_recent_rel_change = 5000f;

		public float dip_imp_map_distace_min;

		public float dip_imp_map_distace_max = 10f;

		public float dip_imp_war = 10000f;

		public float dip_imp_vassal = 500f;

		public float dip_imp_liege = 1000f;

		public float dip_imp_ai_offer_min_time = 120f;

		public float dip_imp_ai_offer_pow = 2f;

		public float break_siege_estimation_strong = 70f;

		public float break_siege_strong_army_low_food = 50f;

		public float break_siege_strong_army_low_food_chance = 10f;

		public float break_siege_weak_army_no_food_chance = 10f;

		public float retreat_fight_cooldown = 30f;

		public DT.Field max_num_mercenaries;

		public int max_army_equipment = 1;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			dip_imp_ai_offer_min_time = field.GetFloat("dip_imp_ai_offer_min_time", null, dip_imp_ai_offer_min_time);
			dip_imp_ai_offer_pow = field.GetFloat("dip_imp_ai_offer_pow", null, dip_imp_liege);
			dip_imp_vassal = field.GetFloat("dip_imp_vassal", null, dip_imp_vassal);
			dip_imp_liege = field.GetFloat("dip_imp_liege", null, dip_imp_liege);
			dip_imp_recent_rel_change_min_time = field.GetFloat("dip_imp_recent_rel_change_min_time", null, dip_imp_recent_rel_change_min_time);
			dip_imp_recent_rel_change_max_time = field.GetFloat("dip_imp_recent_rel_change_max_time", null, dip_imp_recent_rel_change_max_time);
			dip_imp_player_cooldown_min = field.GetFloat("dip_imp_player_cooldown_min", null, dip_imp_player_cooldown_min);
			dip_imp_player_cooldown_max = field.GetFloat("dip_imp_player_cooldown_max", null, dip_imp_player_cooldown_max);
			dip_imp_player_cooldown_power = field.GetFloat("dip_imp_player_cooldown_power", null, dip_imp_player_cooldown_power);
			dip_imp_neighbor = field.GetFloat("dip_imp_neighbor", null, dip_imp_neighbor);
			dip_imp_second_neighbor = field.GetFloat("dip_imp_second_neighbor", null, dip_imp_second_neighbor);
			dip_imp_second_neighbor_max_dist = field.GetFloat("dip_imp_second_neighbor_max_dist", null, dip_imp_second_neighbor_max_dist);
			dip_imp_second_kin = field.GetFloat("dip_imp_second_kin", null, dip_imp_second_kin);
			dip_imp_crusade_or_jihad = field.GetFloat("dip_imp_crusade_or_jihad", null, dip_imp_crusade_or_jihad);
			dip_imp_ally = field.GetFloat("dip_imp_ally", null, dip_imp_ally);
			dip_imp_have_trade = field.GetFloat("dip_imp_have_trade", null, dip_imp_have_trade);
			dip_imp_have_exclusive_trade = field.GetFloat("dip_imp_have_exclusive_trade", null, dip_imp_have_exclusive_trade);
			dip_imp_currently_trading_mod = field.GetFloat("dip_imp_currently_trading_mod", null, dip_imp_currently_trading_mod);
			dip_imp_player_boost_distance_max = field.GetFloat("dip_imp_player_boost_distance_max", null, dip_imp_player_boost_distance_max);
			dip_imp_player_boost_mul = field.GetFloat("dip_imp_player_boost_mul", null, dip_imp_player_boost_mul);
			dip_imp_player_recent_rel_change = field.GetFloat("dip_imp_player_recent_rel_change", null, dip_imp_player_recent_rel_change);
			dip_imp_map_distace_min = field.GetFloat("dip_imp_map_distace_min", null, dip_imp_map_distace_min);
			dip_imp_map_distace_max = field.GetFloat("dip_imp_map_distace_max", null, dip_imp_map_distace_max);
			dip_imp_war = field.GetFloat("dip_imp_war", null, dip_imp_war);
			break_siege_estimation_strong = field.GetFloat("break_siege_estimation_strong", null, break_siege_estimation_strong);
			break_siege_strong_army_low_food = field.GetFloat("break_siege_strong_army_low_food", null, break_siege_strong_army_low_food);
			break_siege_strong_army_low_food_chance = field.GetFloat("break_siege_strong_army_low_food_chance", null, break_siege_strong_army_low_food_chance);
			break_siege_weak_army_no_food_chance = field.GetFloat("break_siege_weak_army_no_food_chance", null, break_siege_weak_army_no_food_chance);
			retreat_fight_cooldown = field.GetFloat("retreat_fight_cooldown", null, retreat_fight_cooldown);
			max_num_mercenaries = field.FindChild("max_num_mercenaries");
			max_army_equipment = field.GetInt("max_army_equipment", null, max_army_equipment);
			return true;
		}
	}

	[Flags]
	public enum EnableFlags
	{
		Disabled = 0,
		Kingdom = 1,
		HireCourt = 2,
		Buildings = 4,
		Armies = 8,
		Units = 0x10,
		Garrison = 0x20,
		Characters = 0x40,
		Diplomacy = 0x80,
		Wars = 0x100,
		Offense = 0x200,
		Mercenaries = 0x400,
		All = 0x7FF
	}

	public enum RealmImportance
	{
		Foreign,
		Historical,
		PreviouslyOwned,
		Core,
		Own
	}

	public enum AIPersonality
	{
		Default,
		Improved,
		AntiRebellion,
		RichArmies,
		COUNT
	}

	public struct ArmyEval
	{
		public Army army;

		public float eval;

		public float ooc_eval;

		public ArmyEval(Army army)
		{
			this.army = army;
			eval = army?.EvalStrength() ?? 0;
			if (army.battle != null)
			{
				if (army.battle.is_siege)
				{
					ooc_eval = 0f;
				}
				else
				{
					ooc_eval = eval * 0.5f;
				}
			}
			else
			{
				ooc_eval = eval;
			}
		}

		public override string ToString()
		{
			if (eval == ooc_eval)
			{
				return $"[{eval}] {army}";
			}
			return $"[{ooc_eval}/{eval}] {army}";
		}
	}

	public struct ArmiesEval
	{
		public List<ArmyEval> armies;

		public float eval;

		public float ooc_eval;

		public int Count
		{
			get
			{
				if (armies != null)
				{
					return armies.Count;
				}
				return 0;
			}
		}

		public override string ToString()
		{
			if (Count <= 0)
			{
				return "0";
			}
			if (eval == ooc_eval)
			{
				return $"{Count}: {eval}";
			}
			return $"{Count}: {ooc_eval}/{eval}";
		}

		public string Dump()
		{
			string text = ToString();
			for (int i = 0; i < Count; i++)
			{
				ArmyEval armyEval = armies[i];
				text += $"\n    {armyEval}";
			}
			return text;
		}

		public void Clear()
		{
			armies?.Clear();
			eval = 0f;
			ooc_eval = 0f;
		}

		public bool Add(Army army)
		{
			if (army == null)
			{
				return false;
			}
			if (armies == null)
			{
				armies = new List<ArmyEval>();
			}
			else if (FindIndex(army) >= 0)
			{
				return false;
			}
			ArmyEval item = new ArmyEval(army);
			armies.Add(item);
			eval += item.eval;
			ooc_eval += item.ooc_eval;
			return true;
		}

		public void Add(ArmiesEval aes)
		{
			if (aes.Count != 0)
			{
				if (armies == null)
				{
					armies = new List<ArmyEval>(aes.Count);
				}
				armies.AddRange(aes.armies);
				eval += aes.eval;
				ooc_eval += aes.ooc_eval;
			}
		}

		public bool Del(Army army)
		{
			int num = FindIndex(army);
			if (num < 0)
			{
				return false;
			}
			ArmyEval armyEval = armies[num];
			armies.RemoveAt(num);
			eval -= armyEval.eval;
			ooc_eval -= armyEval.ooc_eval;
			return true;
		}

		public int FindIndex(Army army)
		{
			if (armies == null)
			{
				return -3;
			}
			if (army == null)
			{
				return -2;
			}
			for (int i = 0; i < armies.Count; i++)
			{
				if (armies[i].army == army)
				{
					return i;
				}
			}
			return -1;
		}

		public void Sort()
		{
			armies.Sort((ArmyEval a, ArmyEval b) => b.eval.CompareTo(a.eval));
		}
	}

	public enum FriendLevel
	{
		Enemy = -1,
		Neutral,
		Friend,
		Ally,
		Own
	}

	public class Threat
	{
		public enum Level
		{
			Safe,
			Border,
			Neighbors,
			Attack,
			Invaded,
			Siege
		}

		public Kingdom kingdom;

		public Realm realm;

		public Level level;

		public FriendLevel friend_level;

		public RealmImportance importance;

		public int index;

		public ArmiesEval enemies_in;

		public ArmiesEval enemies_nearby;

		public ArmiesEval friends_in;

		public ArmiesEval ours_in;

		public ArmiesEval assigned;

		public int garrison_eval;

		public bool reinforceable_battles;

		public float min_needed;

		public float max_needed;

		public Threat(Realm r)
		{
			realm = r;
		}

		public override string ToString()
		{
			string text = ((level != Level.Attack) ? $"[{level}]" : $"[{friend_level}, {importance}] {kingdom?.Name} -> ");
			return $"{index}: {text} {realm?.castle?.name ?? realm?.name}: {assigned} / {min_needed} - {max_needed} ({enemies_in} + {enemies_nearby})";
		}

		public string Dump()
		{
			return string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(ToString() + "\nEnemies in: " + enemies_in.Dump(), "\nEnemies nearby: ", enemies_nearby.Dump()), "\nOurs in: ", ours_in.Dump()), "\nFriends in: ", friends_in.Dump()), "\nAssigned: ", assigned.Dump()), $"\nGarrison: {garrison_eval}"), $"\nReinforceable battles: {reinforceable_battles}");
		}

		public void Clear()
		{
			kingdom = null;
			level = Level.Safe;
			friend_level = FriendLevel.Neutral;
			enemies_in.Clear();
			enemies_nearby.Clear();
			friends_in.Clear();
			ours_in.Clear();
			assigned.Clear();
			garrison_eval = 0;
			reinforceable_battles = false;
			min_needed = 0f;
			max_needed = 0f;
		}

		public void RecalcEvalArmies(Kingdom k)
		{
			this.kingdom = k;
			Kingdom kingdom = realm.GetKingdom();
			for (int i = 0; i < realm.armies.Count; i++)
			{
				Army army = realm.armies[i];
				Kingdom kingdom2 = army.GetKingdom();
				if (kingdom2 == k)
				{
					ours_in.Add(army);
				}
				else if (k.IsEnemy(army))
				{
					if (kingdom == k || kingdom.IsEnemy(army))
					{
						enemies_in.Add(army);
						IncLevelTo(Level.Invaded);
					}
				}
				else if (GetFriendLevel(kingdom, kingdom2) >= FriendLevel.Friend)
				{
					friends_in.Add(army);
				}
				if (!reinforceable_battles && army.battle != null && k.ai.DecideBattleSide(army.battle) >= 0)
				{
					reinforceable_battles = true;
				}
			}
		}

		public void Recalc(Kingdom k)
		{
			Clear();
			this.kingdom = k;
			Kingdom kingdom = this.realm.GetKingdom();
			if (kingdom != k)
			{
				level = Level.Attack;
			}
			else if (this.realm.castle?.battle != null)
			{
				level = Level.Siege;
			}
			else
			{
				level = Level.Safe;
			}
			friend_level = GetFriendLevel(k, kingdom);
			importance = k.ai.CalcRealmImportance(this.realm);
			if (friend_level == FriendLevel.Enemy)
			{
				garrison_eval = EvalCastleStrength(this.realm.castle);
			}
			else
			{
				garrison_eval = 0;
			}
			for (int i = 0; i < this.realm.armies.Count; i++)
			{
				Army army = this.realm.armies[i];
				Kingdom kingdom2 = army.GetKingdom();
				if (kingdom2 == k)
				{
					ours_in.Add(army);
				}
				else if (k.IsEnemy(army))
				{
					if (kingdom == k || kingdom.IsEnemy(army))
					{
						enemies_in.Add(army);
						IncLevelTo(Level.Invaded);
					}
				}
				else if (GetFriendLevel(kingdom, kingdom2) >= FriendLevel.Friend)
				{
					friends_in.Add(army);
				}
				if (!reinforceable_battles && army.battle != null && k.ai.DecideBattleSide(army.battle) >= 0)
				{
					reinforceable_battles = true;
				}
			}
			for (int j = 0; j < this.realm.logicNeighborsAll.Count; j++)
			{
				Realm realm = this.realm.logicNeighborsAll[j];
				Kingdom kingdom3 = realm.GetKingdom();
				if (kingdom == k && kingdom3 != k)
				{
					FriendLevel friendLevel = GetFriendLevel(k, kingdom3);
					if (friendLevel == FriendLevel.Enemy)
					{
						IncLevelTo(Level.Neighbors);
					}
					else
					{
						IncLevelTo(Level.Border);
					}
					if (friendLevel != FriendLevel.Neutral && k.ai.Enabled(EnableFlags.Offense))
					{
						if (!threats.Contains(realm.attacker_threat))
						{
							realm.attacker_threat.Recalc(k);
							threats.Add(realm.attacker_threat);
						}
						enemies_nearby.Add(realm.attacker_threat.enemies_in);
						continue;
					}
				}
				for (int l = 0; l < realm.armies.Count; l++)
				{
					Army army2 = realm.armies[l];
					if (k.IsEnemy(army2))
					{
						enemies_nearby.Add(army2);
						IncLevelTo(Level.Neighbors);
					}
				}
			}
			for (int m = 0; m < this.realm.neighbors.Count; m++)
			{
				Realm realm2 = this.realm.neighbors[m];
				if (!realm2.IsSeaRealm())
				{
					continue;
				}
				for (int n = 0; n < realm2.armies.Count; n++)
				{
					Army army3 = realm2.armies[n];
					if (k.IsEnemy(army3))
					{
						enemies_nearby.Add(army3);
						IncLevelTo(Level.Neighbors);
					}
				}
			}
			if (kingdom != k)
			{
				if (this.realm.castle.battle != null && !reinforceable_battles && friend_level == FriendLevel.Enemy && GetFriendLevel(k, this.realm.castle.battle.attacker_kingdom) < FriendLevel.Friend)
				{
					min_needed = (max_needed = 0f);
				}
				else if (friend_level != FriendLevel.Enemy)
				{
					min_needed = enemies_in.eval - friends_in.eval;
					if (min_needed < 0f)
					{
						min_needed = 0f;
					}
					max_needed = min_needed * 1.5f;
				}
				else
				{
					min_needed = 1f + enemies_in.ooc_eval + enemies_nearby.ooc_eval * 0.75f;
					max_needed = 1f + ((float)garrison_eval + enemies_in.eval + enemies_nearby.eval) * 1.5f;
				}
			}
			else
			{
				min_needed = enemies_in.eval * 1.5f;
				max_needed = (enemies_in.eval + enemies_nearby.eval) * 1.5f;
				if (level > Level.Border)
				{
					threats.Add(this);
				}
			}
		}

		private void IncLevelTo(Level level)
		{
			if (this.level != Level.Attack && level > this.level)
			{
				this.level = level;
			}
		}

		public static int EvalCastleStrength(Castle castle)
		{
			Realm realm = castle?.GetRealm();
			if (realm == null)
			{
				return 0;
			}
			using (new Stat.ForceCached("EvalCastleStrength"))
			{
				Battle.Def def = castle.game.defs.Get<Battle.Def>("Siege");
				float num = 0f;
				float num2 = castle.keep_effects.siege_defense_condition.Get() / castle.keep_effects.siege_defense_condition.GetMax();
				float num3 = def.min_siege_defense_additional_defender_mod + num2 * (1f - def.min_siege_defense_additional_defender_mod);
				AvailableUnits available_units = castle.available_units;
				Unit.Def def2 = available_units?.GetMilitiaDef();
				if (def2 != null)
				{
					float militia_alive_workers_mod = def.militia_alive_workers_mod;
					int num4 = castle.population.workers + castle.population.rebels;
					int num5 = castle.population.Slots(Population.Type.Worker, check_up_to_date: false);
					float num6 = (float)(int)((float)num4 * militia_alive_workers_mod + (float)num5 * def.militia_max_population_mod + def.militia_base) * num3;
					float num7 = def2.GetMaxTroops(realm);
					int num8 = (int)Math.Ceiling(num6 / num7);
					float num9 = num6 / (float)num8 / num7;
					num += def2.strength_eval * num9 * (float)num8;
				}
				int num10 = (int)Math.Min(def.max_guard_squads, (float)(int)realm.income.Get(ResourceType.TownGuards) * num3);
				if (num10 > 0)
				{
					float stat = realm.GetStat(Stats.rs_guard_level);
					Unit.Def townGuardDef = available_units.GetTownGuardDef((int)stat);
					num += townGuardDef.strength_eval * (float)num10;
				}
				for (int i = 0; i < castle.garrison.units.Count; i++)
				{
					Unit unit = castle.garrison.units[i];
					num += unit.EvalStrength();
				}
				return 1 + (int)Math.Ceiling(num);
			}
		}

		public int CompareTo(Threat t)
		{
			int num = level.CompareTo(t.level);
			if (num != 0)
			{
				return num;
			}
			num = importance.CompareTo(t.importance);
			if (num != 0)
			{
				return num;
			}
			num = min_needed.CompareTo(t.min_needed);
			if (num != 0)
			{
				return num;
			}
			num = max_needed.CompareTo(t.max_needed);
			if (num != 0)
			{
				return num;
			}
			return 0;
		}
	}

	public CategoryData[] categories = new CategoryData[7];

	public WeightedRandom<Expense> general_expenses = new WeightedRandom<Expense>(32);

	public Expense next_build_expense = new Expense();

	public Expense next_upgrade_expense = new Expense();

	public WeightedRandom<Expense> military_expenses = new WeightedRandom<Expense>(32);

	public WeightedRandom<Expense> urgent_expenses = new WeightedRandom<Expense>(32);

	public Expense last_expense = new Expense();

	public Expense last_upkeep_expense = new Expense();

	public Expense tmp_expense = new Expense();

	public StringBuilder expenses_log;

	public List<Tuple<Kingdom, Time>> helpWithRebels = new List<Tuple<Kingdom, Time>>();

	public OfferToPlayer[] offer_to_player = new OfferToPlayer[10];

	public static List<GovernOption> govern_options = new List<GovernOption>(512);

	public static Resource tmp_res = new Resource();

	public Def def;

	public Kingdom kingdom;

	public EnableFlags enabled = EnableFlags.All;

	public EnableFlags trace_enabled;

	public EnableFlags trace_to_file_enabled;

	public string trace_file = "";

	public int general_thinks_tries;

	public int build_thinks_tries;

	public int military_thinks_tries;

	public int governor_thinks_tries;

	public int diplomacy_thinks_tries;

	public int general_thinks;

	public int build_thinks;

	public int military_thinks;

	public int governor_thinks;

	public int diplomacy_thinks;

	public AIPersonality personality;

	public bool refresh_realm_specialization = true;

	public bool original_realm;

	private string[] ExileActions = new string[4] { "ExileAction", "ForgetCharacterAction", "AbandonAction", "RecallAction" };

	public static List<Threat> threats = new List<Threat>(500);

	public static List<Mercenary> tmp_merc = new List<Mercenary>(50);

	private static List<Unit> tmp_existing_units = new List<Unit>(32);

	private static List<Unit> tmp_to_take = new List<Unit>(32);

	private static List<Unit> tmp_to_leave = new List<Unit>(32);

	private static List<Unit.Def> tmp_to_hire = new List<Unit.Def>();

	public Game game => kingdom.game;

	private void CreateCategoryData()
	{
		for (int i = 0; i < categories.Length; i++)
		{
			categories[i] = new CategoryData(kingdom, (Expense.Category)i);
		}
	}

	private void CalcBudget()
	{
		float num = 0f;
		for (int i = 0; i < 7; i++)
		{
			CategoryData obj = categories[i];
			float num2 = 0f;
			DT.Field field = game.ai.def.budget[i];
			if (field != null)
			{
				num2 = field.Float(kingdom, num2);
			}
			obj.budget = num2;
			num += num2;
		}
		float budget = categories[0].budget;
		if (num > 100f && budget > 0f && num > budget)
		{
			float num3 = (100f - budget) / (num - budget);
			for (int j = 1; j < 7; j++)
			{
				categories[j].budget *= num3;
			}
		}
		for (int k = 0; k < 7; k++)
		{
			CategoryData obj2 = categories[k];
			float budget2 = obj2.budget;
			float num4 = obj2.PercGoldSpent();
			float num5 = budget2 - num4;
			if (num5 < 0f)
			{
				num5 = 0f;
			}
			categories[k].weight = num5;
		}
		for (int l = 0; l < 7; l++)
		{
			CategoryData cat = categories[l];
			CalcUpkeeps(cat);
		}
	}

	private void CalcUpkeeps(CategoryData cat)
	{
		CreateUpkeeps(cat);
		for (int i = 0; i < cat.upkeeps.Count; i++)
		{
			CategoryData.UpkeepData ud = cat.upkeeps[i];
			CalcUpkeeps(ud);
		}
	}

	private void CalcUpkeeps(CategoryData.UpkeepData ud)
	{
		ud.budget = ud.def.Float(kingdom);
		if (ud.category.category == Expense.Category.None)
		{
			ud.upkeep = kingdom.expenses[ResourceType.Gold] - kingdom.inflation;
			return;
		}
		if (ud.category.category == Expense.Category.Other)
		{
			float num = kingdom.expenses[ResourceType.Gold] - kingdom.inflation;
			float num2 = 0f;
			for (int i = 1; i < 6; i++)
			{
				CategoryData.UpkeepData upkeepData = kingdom.ai.categories[i]?.FindUpkeepData(null);
				if (upkeepData != null)
				{
					num2 += upkeepData.upkeep;
				}
			}
			ud.upkeep = num - num2;
			return;
		}
		ud.upkeep = 0f;
		if (ud.var_mods != null)
		{
			for (int j = 0; j < ud.var_mods.Count; j++)
			{
				string key = ud.var_mods[j];
				ud.upkeep += kingdom.GetVar(key).Float();
			}
		}
		if (ud.mods == null)
		{
			return;
		}
		for (int k = 0; k < ud.mods.Count; k++)
		{
			IncomeModifier incomeModifier = ud.mods[k];
			if (!float.IsNaN(incomeModifier.value))
			{
				ud.upkeep += incomeModifier.value;
			}
		}
	}

	private void CreateUpkeeps(CategoryData cat)
	{
		if (cat.upkeeps != null)
		{
			return;
		}
		cat.upkeeps = new List<CategoryData.UpkeepData>();
		if (game?.ai?.def?.upkeeps_budget == null || game.ai.kingdom_gold_upkeep_panel_def?.field == null)
		{
			return;
		}
		DT.Field field = game.ai.def.upkeeps_budget[(int)cat.category];
		if (field == null)
		{
			return;
		}
		CategoryData.UpkeepData parent = CreateUpkeepData(field, cat, null);
		if (field.children == null)
		{
			return;
		}
		for (int i = 0; i < field.children.Count; i++)
		{
			DT.Field field2 = field.children[i];
			if (!string.IsNullOrEmpty(field2.key) && !(field2.key == "subtotal"))
			{
				CreateUpkeepData(field2, cat, parent);
			}
		}
	}

	private CategoryData.UpkeepData CreateUpkeepData(DT.Field f, CategoryData cat, CategoryData.UpkeepData parent)
	{
		CategoryData.UpkeepData upkeepData = new CategoryData.UpkeepData();
		upkeepData.def = f;
		upkeepData.category = cat;
		upkeepData.parent = parent;
		if (parent != null)
		{
			upkeepData.subcategory = f.key;
		}
		if (cat.category == Expense.Category.None || cat.category == Expense.Category.Other)
		{
			cat.upkeeps.Add(upkeepData);
			return upkeepData;
		}
		string text = f.GetString("subtotal", null, f.key);
		DT.Field field = game.ai.kingdom_gold_upkeep_panel_def.field.FindChild(text);
		if (field == null)
		{
			Game.Log(f.Path(include_file: true) + ": Unknown kingdom gold upkeep category: " + text, Game.LogType.Error);
			return null;
		}
		string text2 = ((parent == null) ? "subtotal" : "details");
		if (field.type != text2)
		{
			Game.Log(f.Path(include_file: true) + ": Wrong kingdom gold upkeep category type: '" + field.type + "' instead of '" + text2 + "'", Game.LogType.Error);
		}
		if (parent == null)
		{
			int num = field.parent.children.IndexOf(field);
			for (num++; num < field.parent.children.Count; num++)
			{
				DT.Field field2 = field.parent.children[num];
				if (!string.IsNullOrEmpty(field2.key))
				{
					if (field2.type != "details")
					{
						break;
					}
					AddUpkeepModifier(upkeepData, field2);
				}
			}
		}
		else
		{
			AddUpkeepModifier(upkeepData, field);
		}
		cat.upkeeps.Add(upkeepData);
		return upkeepData;
	}

	private void AddUpkeepModifier(CategoryData.UpkeepData ud, DT.Field ukpf)
	{
		string text = ukpf.String();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (!text.StartsWith("Kingdom.", StringComparison.Ordinal))
		{
			if (ud.var_mods == null)
			{
				ud.var_mods = new List<string>();
			}
			ud.var_mods.Add(text);
			return;
		}
		IncomeLocation incomeLocation = ud.category.kingdom.upkeeps.per_resource[1].FindLocation("Kingdom");
		if (incomeLocation == null)
		{
			return;
		}
		text = text.Substring(8);
		IncomeModifier incomeModifier = incomeLocation.FindMod(text);
		if (incomeModifier != null)
		{
			if (ud.mods == null)
			{
				ud.mods = new List<IncomeModifier>();
			}
			ud.mods.Add(incomeModifier);
		}
	}

	private void AddUpkeep(float upkeep, Expense.Category category, string subcategory)
	{
		CategoryData categoryData = categories[(int)category];
		if (categoryData != null)
		{
			CategoryData.UpkeepData ud = categoryData.FindUpkeepData(subcategory);
			AddUpkeep(upkeep, ud);
			if (subcategory != null)
			{
				ud = categoryData.FindUpkeepData(null);
				AddUpkeep(upkeep, ud);
			}
		}
		if (category != Expense.Category.None)
		{
			categoryData = categories[0];
			if (categoryData != null)
			{
				CategoryData.UpkeepData ud2 = categoryData.FindUpkeepData(null);
				AddUpkeep(upkeep, ud2);
			}
		}
	}

	private void AddUpkeep(float upkeep, CategoryData.UpkeepData ud)
	{
		if (ud != null)
		{
			ud.upkeep += upkeep;
		}
	}

	public bool CheckUpkeep(float upkeep, Expense.Category category, string subcategory = null)
	{
		CategoryData categoryData = categories[(int)category];
		if (categoryData == null)
		{
			return true;
		}
		CategoryData.UpkeepData ud = categoryData.FindUpkeepData(subcategory);
		if (!CheckUpkeep(upkeep, ud))
		{
			return false;
		}
		if (subcategory != null)
		{
			ud = categoryData.FindUpkeepData(null);
			if (!CheckUpkeep(upkeep, ud))
			{
				return false;
			}
		}
		if (category != Expense.Category.None)
		{
			categoryData = categories[0];
			if (categoryData == null)
			{
				return true;
			}
			ud = categoryData.FindUpkeepData(null);
			if (!CheckUpkeep(upkeep, ud))
			{
				return false;
			}
		}
		return true;
	}

	private bool CheckUpkeep(float upkeep, CategoryData.UpkeepData ud)
	{
		if (ud == null)
		{
			return true;
		}
		float num = kingdom.income.Get(ResourceType.Gold);
		if (upkeep > num)
		{
			return false;
		}
		float num2 = ud.budget * num / 100f;
		if (ud.upkeep + upkeep > num2)
		{
			return false;
		}
		return true;
	}

	private void ClearExpenses(WeightedRandom<Expense> expenses)
	{
		for (int i = 0; i < expenses.options.Count; i++)
		{
			expenses.options[i].val.Delete();
		}
		expenses.Clear();
	}

	private void AddExpense(WeightedRandom<Expense> expenses, Expense expense)
	{
		float weight = (30f - expense.eval) * (float)expense.priority;
		expenses.AddOption(expense, weight);
	}

	private void ConsiderExpense(Expense.Type type, BaseObject defParam, Object objectParam, Expense.Category category = Expense.Category.None, Expense.Priority priority = Expense.Priority.Normal, List<Value> args = null)
	{
		tmp_expense.Set(kingdom, type, category, priority, defParam, objectParam, args);
		ConsiderExpense(tmp_expense);
	}

	private void ConsiderExpense(Expense expense)
	{
		if (expense.type == Expense.Type.None || expense.eval >= 30f || (expense.kingdom_cost.IsZero() && SpendExpense(expense)))
		{
			return;
		}
		CategoryData categoryData = categories[(int)expense.category];
		if (expense.priority < Expense.Priority.Urgent && categoryData.weight <= 0f)
		{
			return;
		}
		CoopThread coopThread = CoopThread.current?.root;
		if (coopThread == null)
		{
			Game.Log($"ConsiderExpense called outside CoopThread: {expense}", Game.LogType.Error);
			return;
		}
		WeightedRandom<Expense> expenses;
		if (coopThread == game.ai.think_general_thread)
		{
			expenses = general_expenses;
		}
		else if (coopThread == game.ai.think_military_thread)
		{
			expenses = ((expense.priority < Expense.Priority.Urgent) ? military_expenses : urgent_expenses);
		}
		else
		{
			if (coopThread != game.ai.think_build_thread)
			{
				Game.Log($"ConsiderExpense called from unknown CoopThread({coopThread}): {expense}", Game.LogType.Error);
				return;
			}
			expenses = general_expenses;
		}
		Expense expense2 = Expense.New();
		expense2.Set(expense);
		AddExpense(expenses, expense2);
	}

	private IEnumerator SpendExpenses(WeightedRandom<Expense> expenses)
	{
		while (true)
		{
			yield return null;
			Expense expense = expenses.Choose(null, del_option: true);
			if (expense == null)
			{
				break;
			}
			CategoryData categoryData = categories[(int)expense.category];
			categoryData.next_expense.Set(expense);
			if (!expense.Validate())
			{
				expense.Delete();
				continue;
			}
			if (!SpendExpense(expense))
			{
				expense.Delete();
				continue;
			}
			categoryData.next_expense.Set(kingdom, Expense.Type.None);
			if (expense.type == next_build_expense.type)
			{
				next_build_expense.Set(kingdom, Expense.Type.None);
			}
			if (expense.type == next_upgrade_expense.type)
			{
				next_upgrade_expense.Set(kingdom, Expense.Type.None);
			}
			expense.Delete();
		}
	}

	private bool SpendExpense(Expense expense)
	{
		Kingdom.in_AI_spend = true;
		bool num = expense.Spend();
		Kingdom.in_AI_spend = false;
		if (!num)
		{
			return false;
		}
		LogSpentExpense(expense);
		if (TraceEnabled(EnableFlags.All))
		{
			Trace(EnableFlags.All, "Spent {0}", expense);
		}
		if (TraceToFileEnabled(EnableFlags.All))
		{
			TraceToFile(EnableFlags.All, "--- Time: " + game.time.ToString() + " --- Spent {0}", expense);
		}
		CategoryData categoryData = categories[(int)expense.category];
		categoryData.spent.Add(expense.kingdom_cost, 1f);
		last_expense.Set(expense);
		categoryData.last_expense.Set(expense);
		if (expense.upkeep_gold > 0f)
		{
			AddUpkeep(expense.upkeep_gold, expense.category, expense.upkeep_subcategory);
			last_upkeep_expense.Set(expense);
			categoryData.last_upkeep_expense.Set(expense);
		}
		return true;
	}

	private void LogSpentExpense(Expense expense)
	{
		if (kingdom.is_player)
		{
			if (expenses_log == null)
			{
				expenses_log = new StringBuilder(16384);
				expenses_log.AppendLine("Time;Category;Gold Cost;Gold Upkeep;Details");
			}
			float num = ((expense.cost == null) ? 0f : expense.cost[ResourceType.Gold]);
			string value = $"{game.session_time.ToHMSString()};{expense.category};{num};{expense.upkeep_gold};{expense}";
			expenses_log.AppendLine(value);
		}
	}

	public IEnumerator ThinkBuild()
	{
		if (TraceEnabled(EnableFlags.Buildings))
		{
			Trace(EnableFlags.Buildings, $"ThinkBuild({kingdom})");
		}
		if (TraceToFileEnabled(EnableFlags.Buildings))
		{
			TraceToFile(EnableFlags.Buildings, "--- Time: " + game.time.ToString() + " --- " + $"ThinkBuild({kingdom})");
		}
		Castle.ClearBuildOptions();
		int i = 0;
		while (i < kingdom.realms.Count)
		{
			Realm realm = kingdom.realms[i];
			Castle castle = realm.castle;
			if (castle != null && castle.battle == null && !realm.IsOccupied() && !realm.IsDisorder())
			{
				using (Game.Profile("Castle.AddBuildOptions"))
				{
					castle.AddBuildOptions(common_only: false, castle.AISpecProductionWeights());
				}
				if (i % 5 == 0)
				{
					yield return null;
				}
			}
			int num = i + 1;
			i = num;
		}
		if (kingdom.is_player)
		{
			Castle.SortBuildOptions();
			Castle.last_build_options.Clear();
			Castle.last_build_options.AddRange(Castle.build_options);
			Castle.last_upgrade_options.Clear();
			Castle.last_upgrade_options.AddRange(Castle.upgrade_options);
		}
		Castle.BuildOption build_opt = Castle.ChooseBuildOption(game, Castle.build_options, Castle.build_options_sum);
		yield return null;
		Castle.BuildOption upgrade_opt = Castle.ChooseBuildOption(game, Castle.upgrade_options, Castle.upgrade_options_sum);
		yield return null;
		if (TraceToFileEnabled(EnableFlags.Buildings))
		{
			string text = "--- Time: " + game.time.ToString() + " --- ChooseBuild: ";
			for (int j = 0; j < Math.Min(Castle.build_options.Count, 3); j++)
			{
				text = text + Castle.build_options[j].ToString() + "  |  ";
			}
			text = text + "--- Time: " + game.time.ToString() + " --- ChooseUpgrade: ";
			for (int k = 0; k < Math.Min(Castle.upgrade_options.Count, 3); k++)
			{
				text = text + Castle.upgrade_options[k].ToString() + "  |  ";
			}
			TraceToFile(EnableFlags.Buildings, text);
		}
		if (build_opt.def != null && upgrade_opt.def != null)
		{
			if (build_opt.priority == Expense.Priority.Urgent && upgrade_opt.priority != Expense.Priority.Urgent)
			{
				upgrade_opt.def = null;
			}
			else if (build_opt.priority != Expense.Priority.Urgent && upgrade_opt.priority == Expense.Priority.Urgent)
			{
				build_opt.def = null;
			}
		}
		if (build_opt.def != null)
		{
			if (build_opt.castle.NeedsExpandCity() && build_opt.castle.CanExpandCity())
			{
				next_build_expense.Set(kingdom, Expense.Type.ExpandCity, build_opt.def.ai_category, build_opt.priority, build_opt.def, build_opt.castle);
			}
			else
			{
				next_build_expense.Set(kingdom, Expense.Type.BuildStructure, build_opt.def.ai_category, build_opt.priority, build_opt.def, build_opt.castle);
			}
		}
		else
		{
			next_build_expense.Set(kingdom, Expense.Type.None);
		}
		if (upgrade_opt.def != null)
		{
			next_upgrade_expense.Set(kingdom, Expense.Type.Upgrade, upgrade_opt.def.ai_category, upgrade_opt.priority, upgrade_opt.def, upgrade_opt.castle);
		}
		else
		{
			next_upgrade_expense.Set(kingdom, Expense.Type.None);
		}
		if (TraceToFileEnabled(EnableFlags.Buildings))
		{
			TraceToFile(EnableFlags.Buildings, "--- Time: " + game.time.ToString() + " --- " + $"ThinkBuildEND({kingdom})");
		}
	}

	private bool ThinkDeclareWar(Kingdom k, string offer_rel_change_type = "neutral")
	{
		if (!kingdom.IsEnemy(k) && (offer_rel_change_type == "neutral" || offer_rel_change_type == "negative") && kingdom.ai.Enabled(EnableFlags.Wars))
		{
			BeginProfile("Think Declare War");
			Offer cachedOffer = Offer.GetCachedOffer("DeclareWar", kingdom, k);
			cachedOffer.AI = true;
			if (cachedOffer.Validate() == "ok" && cachedOffer.CheckThreshold("propose"))
			{
				cachedOffer.Send();
				if (k.is_player)
				{
					SetLastOfferTimeToKingdom(k, cachedOffer);
					k.t_last_ai_offer_time = game.time;
				}
				EndProfile("Think Declare War");
				return true;
			}
			EndProfile("Think Declare War");
		}
		return false;
	}

	private bool ThinkWhitePeace(Kingdom k, string offer_rel_change_type = "neutral")
	{
		if (kingdom.IsEnemy(k) && (offer_rel_change_type == "neutral" || offer_rel_change_type == "positive"))
		{
			BeginProfile("Think Offer Peace");
			Offer cachedOffer = Offer.GetCachedOffer("WhitePeaceOffer", kingdom, k);
			cachedOffer.AI = true;
			if (cachedOffer.Validate() == "ok" && cachedOffer.CheckThreshold("propose"))
			{
				cachedOffer.Send();
				if (k.is_player)
				{
					SetLastOfferTimeToKingdom(k, cachedOffer);
					k.t_last_ai_offer_time = game.time;
				}
				EndProfile("Think Offer Peace");
				return true;
			}
			EndProfile("Think Offer Peace");
		}
		return false;
	}

	private Offer GenerateRandomOffer(Kingdom k, string offer_rel_change_type = "neutral")
	{
		Offer offer = null;
		BeginProfile("Generate Random Offer");
		try
		{
			offer = OfferGenerator.instance?.TryGenerateRandomOfferHeavy("propose", kingdom, k, null, 0, forceParentArg: true, 0f, 0f, offer_rel_change_type);
		}
		catch (Exception ex)
		{
			Game.Log("Error generating AI offer: " + ex.ToString(), Game.LogType.Error);
			offer = null;
		}
		EndProfile("Generate Random Offer");
		if (offer == null || offer.def.field.key == "AskForPrisonerRansom" || offer.def.field.key == "OfferRansomPrisoner")
		{
			return null;
		}
		if (offer.def.field.key == "OfferLand")
		{
			return null;
		}
		return offer;
	}

	private IEnumerator ThinkProposeOfferThread(Kingdom k, string offer_rel_change_type = "neutral")
	{
		if (k == null || k.IsDefeated() || k.type != Kingdom.Type.Regular || kingdom.type != Kingdom.Type.Regular || ThinkDeclareWar(k, offer_rel_change_type))
		{
			yield break;
		}
		yield return null;
		if (ThinkWhitePeace(k, offer_rel_change_type))
		{
			yield break;
		}
		yield return null;
		Offer offer = GenerateRandomOffer(k, offer_rel_change_type);
		if (offer != null)
		{
			yield return null;
			if (k.is_player)
			{
				SetLastOfferTimeToKingdom(k, offer);
				k.t_last_ai_offer_time = game.time;
			}
			offer.Send();
		}
	}

	public bool ThinkProposeOfferTo(Kingdom k, string offer_rel_change_type = "neutral")
	{
		if (k == null || k.IsDefeated() || k.type != Kingdom.Type.Regular || kingdom.type != Kingdom.Type.Regular)
		{
			return false;
		}
		if (ThinkDeclareWar(k, offer_rel_change_type))
		{
			return true;
		}
		if (ThinkWhitePeace(k, offer_rel_change_type))
		{
			return true;
		}
		Offer offer = GenerateRandomOffer(k, offer_rel_change_type);
		if (offer == null)
		{
			return false;
		}
		if (k.is_player)
		{
			SetLastOfferTimeToKingdom(k, offer);
			k.t_last_ai_offer_time = game.time;
		}
		offer.Send();
		return true;
	}

	public Time GetLastOfferTimeToKingdom(Kingdom k)
	{
		if (k == null)
		{
			return Time.Zero;
		}
		int id = k.id;
		for (int i = 0; i < offer_to_player.Length; i++)
		{
			if (offer_to_player[i].kingdom_id == id)
			{
				return offer_to_player[i].t_last_offer;
			}
		}
		return Time.Zero;
	}

	public void SetLastOfferTimeToKingdom(Kingdom k, Offer offer)
	{
		if (k == null)
		{
			return;
		}
		int id = k.id;
		Time time = k.game.time;
		for (int i = 0; i < offer_to_player.Length; i++)
		{
			if (offer_to_player[i].kingdom_id == id || offer_to_player[i].kingdom_id == 0)
			{
				offer_to_player[i].t_last_offer = time;
				offer_to_player[i].kingdom_id = id;
				return;
			}
		}
		for (int j = 0; j < offer_to_player.Length; j++)
		{
			if (game.kingdoms[offer_to_player[j].kingdom_id].ai.Enabled(EnableFlags.Diplomacy))
			{
				offer_to_player[j].t_last_offer = time;
				offer_to_player[j].kingdom_id = id;
				break;
			}
		}
	}

	public int CalcDiplomaticImportance(Kingdom other)
	{
		Kingdom kingdom = this.kingdom;
		if (other == null)
		{
			return 0;
		}
		if (other == kingdom)
		{
			return 0;
		}
		if (other.IsDefeated())
		{
			return 0;
		}
		float num = 0f;
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, other, calc_fade: false);
		RelationUtils.Stance stance = kingdomAndKingdomRelation.stance;
		int num2 = kingdom.DistanceToKingdom(other);
		bool flag = kingdom.HasNeighbor(other);
		bool flag2 = kingdom.HasSecondNeighbor(other);
		bool flag3 = stance.IsWar();
		bool is_player = other.is_player;
		bool flag4 = stance.IsMarriage();
		bool flag5 = stance.IsAlliance();
		bool flag6 = kingdom.jihad_attacker == other;
		bool num3 = stance.IsTrade();
		bool flag7 = kingdom.GetMerchantFrom(other) != null;
		Crusade crusade = game.religions.catholic.crusade;
		Kingdom kingdom2 = crusade?.src_kingdom;
		bool flag8 = kingdom2 != null && kingdom2 == other && crusade.target == kingdom;
		float num4 = game.time - kingdomAndKingdomRelation.last_rel_change_time;
		bool flag9 = num4 > def.dip_imp_recent_rel_change_min_time && num4 < def.dip_imp_recent_rel_change_max_time;
		float val = game.time - Time.Zero;
		if (is_player)
		{
			val = game.time - GetLastOfferTimeToKingdom(other);
		}
		float val2 = game.time - Time.Zero;
		if (is_player)
		{
			val2 = game.time - other.t_last_ai_offer_time;
		}
		float num5 = (float)Math.Pow(Game.map_clamp(val, def.dip_imp_player_cooldown_min, def.dip_imp_player_cooldown_max, 0f, 1f), def.dip_imp_player_cooldown_power);
		float num6 = (float)Math.Pow(Game.map_clamp(val2, 0f, def.dip_imp_ai_offer_min_time, 0f, 1f), def.dip_imp_ai_offer_pow);
		bool flag10 = kingdom.sovereignState == other;
		bool flag11 = other.sovereignState == kingdom;
		num += (flag ? def.dip_imp_neighbor : 0f);
		num += ((flag2 && (float)num2 <= def.dip_imp_second_neighbor_max_dist) ? def.dip_imp_second_neighbor : 0f);
		num += (flag4 ? def.dip_imp_second_kin : 0f);
		num += ((flag6 || flag8) ? def.dip_imp_crusade_or_jihad : 0f);
		num += (flag5 ? def.dip_imp_ally : 0f);
		if (num3)
		{
			num += def.dip_imp_have_trade * (flag7 ? def.dip_imp_currently_trading_mod : 1f);
		}
		num += (flag3 ? def.dip_imp_war : 0f);
		num += (flag10 ? def.dip_imp_liege : 0f);
		num += (flag11 ? def.dip_imp_vassal : 0f);
		num += ((is_player && flag9) ? def.dip_imp_player_recent_rel_change : 0f);
		num *= ((is_player && (float)num2 <= def.dip_imp_player_boost_distance_max) ? def.dip_imp_player_boost_mul : 1f);
		num *= 1f - Game.map_clamp(num2, def.dip_imp_map_distace_min, def.dip_imp_map_distace_max, 0f, 1f);
		if (is_player)
		{
			num *= num5;
			num *= num6;
		}
		return (int)num;
	}

	public IEnumerator ThinkDiplomacy()
	{
		if (TraceEnabled(EnableFlags.Diplomacy))
		{
			Trace(EnableFlags.Diplomacy, $"ThinkDiplomacy({kingdom})");
		}
		if (TraceToFileEnabled(EnableFlags.Diplomacy))
		{
			TraceToFile(EnableFlags.Diplomacy, "--- Time: " + game.time.ToString() + " --- " + $"ThinkDiplomacy({kingdom})");
		}
		if (game.isInVideoMode)
		{
			yield break;
		}
		_ = game.time;
		BeginProfile("Choose diplomacy target");
		Kingdom target = WeightedRandom<Kingdom>.Choose(game.kingdoms, (Kingdom other) => kingdom.ai.CalcDiplomaticImportance(other));
		EndProfile("Choose diplomacy target");
		if (target != null)
		{
			yield return null;
			yield return CoopThread.Call("ThinkProposeOffer", ThinkProposeOfferThread(target));
			if (TraceToFileEnabled(EnableFlags.Diplomacy))
			{
				TraceToFile(EnableFlags.Diplomacy, "--- Time: " + game.time.ToString() + " --- " + $"ThinkDiplomacyEND({kingdom})");
			}
		}
	}

	public IEnumerator PopulateGovernOptions()
	{
		yield return null;
		int num = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Realm realm = kingdom.realms[i];
			if (realm.castle == null)
			{
				continue;
			}
			realm.castle.ai_selected_governor = null;
			if (!realm.castle.CanSetGovernor())
			{
				continue;
			}
			tmp_res.Clear();
			realm.castle.CalcGovernedBonuses(tmp_res);
			for (int j = 0; j < kingdom.court.Count; j++)
			{
				Character character = kingdom.court[j];
				if (character != null && character.CanBeGovernor() && realm.castle.CanSetGovernor(character))
				{
					GovernOption governOption = ((num >= govern_options.Count) ? default(GovernOption) : govern_options[num]);
					governOption.governor = character;
					governOption.castle = realm.castle;
					governOption.Eval(tmp_res);
					if (num < govern_options.Count)
					{
						govern_options[num] = governOption;
					}
					else
					{
						govern_options.Add(governOption);
					}
					num++;
				}
			}
		}
		if (num < govern_options.Count)
		{
			govern_options.RemoveRange(num, govern_options.Count - num);
		}
		govern_options.Sort((GovernOption a, GovernOption b) => b.eval.CompareTo(a.eval));
	}

	public IEnumerator DecideInitialGovernors()
	{
		yield return null;
		int num = 0;
		int count = kingdom.court.Count;
		for (int i = 0; i < count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.CanBeGovernor())
			{
				num++;
				character.ClearGovernOptions();
			}
		}
		int num2 = num * num;
		int num3 = 0;
		for (int j = 0; j < govern_options.Count; j++)
		{
			GovernOption item = govern_options[j];
			Character governor = item.governor;
			if (governor.govern_options == null)
			{
				continue;
			}
			int count2 = governor.govern_options.Count;
			if (count2 < num)
			{
				governor.govern_options.Add(item);
				if (governor.cur_govern_option_idx < 0 && item.castle.ai_selected_governor == null)
				{
					governor.cur_govern_option_idx = count2;
					item.castle.ai_selected_governor = governor;
				}
				num3++;
				if (num3 >= num2)
				{
					break;
				}
			}
		}
	}

	public IEnumerator AssignGovernors()
	{
		bool changed = false;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null)
			{
				Castle castle = character.ai_selected_govern_option.castle;
				if (castle != null && castle != (character.governed_castle ?? character.GetPreparingToGovernCastle()) && character.CanBeGovernor())
				{
					changed = true;
					character.Govern(castle);
				}
			}
		}
		yield return null;
		if (changed && (TraceEnabled(EnableFlags.Characters) || TraceToFileEnabled(EnableFlags.Characters)))
		{
			string text = DumpGovernors();
			Game.CopyToClipboard(text);
			if (TraceEnabled(EnableFlags.Characters))
			{
				Trace(EnableFlags.Characters, "Governor assignments:\n{0}", text);
			}
			if (TraceToFileEnabled(EnableFlags.Characters))
			{
				TraceToFile(EnableFlags.Characters, "--- Time: " + game.time.ToString() + " --- Governor assignments:\n{0}", text);
			}
		}
	}

	public string DumpGovernors()
	{
		string[,,] cells = new string[10, 10, 2];
		int[] max_width = new int[10];
		int num = 0;
		int rows = 0;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character?.govern_options != null)
			{
				Set(0, num, 0, $"{character.class_name} {character.Name} ({character.cur_govern_option_idx}: {character.ai_selected_govern_option.castle?.name})");
				Set(0, num, 1, "--------");
				for (int j = 0; j < character.govern_options.Count; j++)
				{
					GovernOption governOption = character.govern_options[j];
					string arg = ((j == character.cur_govern_option_idx) ? "*" : " ");
					Set(j + 1, num, 0, $"{arg}[{governOption.eval}] {governOption.castle.name}");
				}
				num++;
			}
		}
		StringBuilder stringBuilder = new StringBuilder(2048);
		for (int k = 0; k < rows; k++)
		{
			for (int l = 0; l <= 1; l++)
			{
				for (int m = 0; m < num; m++)
				{
					string text = cells[k, m, l];
					if (text == null)
					{
						text = "";
					}
					text = text.PadRight(max_width[m] + 2);
					stringBuilder.Append(text);
				}
				stringBuilder.AppendLine();
			}
		}
		return stringBuilder.ToString();
		void Set(int row, int col, int line, string val)
		{
			if (row >= rows)
			{
				rows = row + 1;
			}
			cells[row, col, line] = val;
			if (val.Length > max_width[col])
			{
				max_width[col] = val.Length;
			}
		}
	}

	public IEnumerator ThinkGovernors()
	{
		if (TraceToFileEnabled(EnableFlags.Kingdom))
		{
			TraceToFile(EnableFlags.Kingdom, "--- Time: " + game.time.ToString() + " --- " + $"ThinkGovernors({kingdom})");
		}
		yield return CoopThread.Call("PopulateGovernOptions", PopulateGovernOptions());
		yield return CoopThread.Call("DecideInitialGovernors", DecideInitialGovernors());
		yield return CoopThread.Call("AssignGovernors", AssignGovernors());
		if (TraceToFileEnabled(EnableFlags.Kingdom))
		{
			TraceToFile(EnableFlags.Kingdom, "--- Time: " + game.time.ToString() + " --- " + $"ThinkGovernorsEND({kingdom})");
		}
	}

	public KingdomAI(Kingdom kingdom)
	{
		this.kingdom = kingdom;
		def = game.defs.Get<Def>("KingdomAI");
		CreateCategoryData();
	}

	public void Init()
	{
		personality = AIPersonality.Improved;
	}

	public override string ToString()
	{
		return kingdom.Name + ".AI";
	}

	public bool Enabled(EnableFlags flag, bool checkAuthority = true)
	{
		if (checkAuthority && !kingdom.IsAuthority())
		{
			return false;
		}
		if (game?.ai == null)
		{
			return false;
		}
		if ((enabled & flag) == 0)
		{
			return false;
		}
		if (!game.ai.enabled)
		{
			return false;
		}
		return true;
	}

	public bool TraceEnabled(EnableFlags flag)
	{
		if ((trace_enabled & flag) == 0)
		{
			return false;
		}
		return true;
	}

	public bool TraceToFileEnabled(EnableFlags flag)
	{
		if ((trace_to_file_enabled & flag) == 0)
		{
			return false;
		}
		return true;
	}

	public void Trace(EnableFlags flag, string message, params object[] args)
	{
		if (TraceEnabled(flag))
		{
			if (args.Length != 0)
			{
				message = string.Format(message, args);
			}
			Game.Log($"{this}: {message}", Game.LogType.Message);
		}
	}

	public void TraceToFile(EnableFlags flag, string message, params object[] args)
	{
		if (TraceToFileEnabled(flag))
		{
			if (args.Length != 0)
			{
				message = string.Format(message, args);
			}
			File.AppendAllText(trace_file, $"\n{this}: {message}");
		}
	}

	public void BeginProfile(string section)
	{
		if (game.ai.profile)
		{
			Game.BeginProfileSection(section);
		}
	}

	public void EndProfile(string section)
	{
		if (game.ai.profile)
		{
			Game.EndProfileSection(section);
		}
	}

	public RealmImportance CalcRealmImportance(Realm r)
	{
		if (r.kingdom_id == kingdom.id)
		{
			return RealmImportance.Own;
		}
		if (r.IsCoreFor(kingdom))
		{
			return RealmImportance.Core;
		}
		if (r.IsPrevoiuslyOwnedBy(kingdom))
		{
			return RealmImportance.PreviouslyOwned;
		}
		if (r.IsHistoricalFor(kingdom))
		{
			return RealmImportance.Historical;
		}
		return RealmImportance.Foreign;
	}

	public IEnumerator ThinkGeneral()
	{
		if (TraceToFileEnabled(EnableFlags.Buildings))
		{
			TraceToFile(EnableFlags.Buildings, "--- Time: " + game.time.ToString() + " --- " + $"ThinkGeneral({kingdom})");
		}
		CalcBudget();
		ClearExpenses(general_expenses);
		CalcPersonality();
		if (refresh_realm_specialization)
		{
			CalcProvinceSpecializations();
			yield return null;
		}
		if (Enabled(EnableFlags.Kingdom))
		{
			ThinkKingdomPrisonerActions();
			yield return null;
		}
		if (Enabled(EnableFlags.Kingdom))
		{
			ConsiderAdoptTradition();
			yield return null;
		}
		if (Enabled(EnableFlags.HireCourt))
		{
			ConsiderHireCourt();
			yield return null;
		}
		if (Enabled(EnableFlags.HireCourt | EnableFlags.Characters))
		{
			yield return CoopThread.Call("KingdomAI.ThinkCharacters", ThinkCharacters());
		}
		if (Enabled(EnableFlags.Kingdom))
		{
			ConsiderIncreaseCrownAuthority();
			yield return null;
		}
		if (Enabled(EnableFlags.Mercenaries))
		{
			ConsiderHireMercenaryArmy();
			yield return null;
		}
		if (Enabled(EnableFlags.Kingdom))
		{
			yield return CoopThread.Call("ThinkKingdomActions", ThinkActions(kingdom));
		}
		ConsiderExpense(next_build_expense);
		ConsiderExpense(next_upgrade_expense);
		yield return CoopThread.Call("Spend general expense", SpendExpenses(general_expenses));
		if (TraceToFileEnabled(EnableFlags.Buildings))
		{
			TraceToFile(EnableFlags.Buildings, "--- Time: " + game.time.ToString() + " --- " + $"ThinkGeneralEND({kingdom})");
		}
	}

	private IEnumerator ThinkCharacters(List<Character> lst)
	{
		if (lst == null)
		{
			yield break;
		}
		int cnt = lst.Count;
		if (cnt == 0)
		{
			yield break;
		}
		int idx = game.Random(0, cnt);
		int i = 0;
		while (i < cnt)
		{
			int num = (idx + i) % cnt;
			if (num >= 0 && num < lst.Count)
			{
				Character character = lst[num];
				if (character != null)
				{
					yield return CoopThread.Call("ThinkCharacter", ThinkCharacter(character));
				}
			}
			int num2 = i + 1;
			i = num2;
		}
	}

	private IEnumerator ThinkCharacters()
	{
		if (kingdom.royalFamily != null)
		{
			yield return CoopThread.Call("ThinkKingCharacter", ThinkCharacter(kingdom.royalFamily.Sovereign));
			yield return CoopThread.Call("ThinkFamilyCharacters", ThinkCharacters(kingdom.royalFamily.Children));
			yield return CoopThread.Call("ThinkRelativeCharacters", ThinkCharacters(kingdom.royalFamily.Relatives));
		}
		yield return CoopThread.Call("ThinkCourtCharacters", ThinkCharacters(kingdom.court));
	}

	private bool ThinkAssign(Character c)
	{
		if (!(c.status is AvailableForAssignmentStatus availableForAssignmentStatus))
		{
			return false;
		}
		Castle castle = null;
		if (c.IsMarshal())
		{
			castle = kingdom.ai.DecideOwnCastleForArmy(c);
			if (castle == null)
			{
				return false;
			}
		}
		if (availableForAssignmentStatus.Assign())
		{
			if (castle != null)
			{
				c.SpawnArmy(castle);
			}
			return true;
		}
		return false;
	}

	private IEnumerator ThinkActions(Object obj)
	{
		Actions actions = obj.GetComponent<Actions>();
		if (actions == null || actions.all == null)
		{
			yield break;
		}
		int cnt = actions.all.Count;
		if (cnt == 0)
		{
			yield break;
		}
		int idx = game.Random(0, cnt);
		int i = 0;
		while (i < cnt)
		{
			yield return null;
			if (!obj.IsValid() || actions.all == null)
			{
				break;
			}
			Action action = actions.all[(idx + i) % cnt];
			if (action.def.opportunity == null)
			{
				List<Value> args = action.args;
				Object target;
				string text = action.AIThink(out target);
				List<Value> args2 = null;
				if (action.args != null)
				{
					args2 = new List<Value>(action.args);
					action.args = args;
				}
				if (text != null && text == "ok")
				{
					Expense.Category expenseCategory = action.GetExpenseCategory();
					Expense.Priority ai_expense_priority = action.def.ai_expense_priority;
					ConsiderExpense(Expense.Type.ExecuteAction, action, target, expenseCategory, ai_expense_priority, args2);
				}
			}
			int num = i + 1;
			i = num;
		}
	}

	private IEnumerator ThinkOpportunities(Object obj)
	{
		Actions component = obj.GetComponent<Actions>();
		if (component == null || component.opportunities == null)
		{
			yield break;
		}
		int count = component.opportunities.Count;
		if (count != 0)
		{
			int index = game.Random(0, count);
			Opportunity opportunity = component.opportunities[index];
			if (!(opportunity.AIValidate() != "ok"))
			{
				Expense.Category expenseCategory = opportunity.action.GetExpenseCategory();
				Expense.Priority ai_expense_priority = opportunity.action.def.ai_expense_priority;
				ConsiderExpense(Expense.Type.ExecuteOpportunity, opportunity, null, expenseCategory, ai_expense_priority);
			}
		}
	}

	private IEnumerator ThinkCharacter(Character c)
	{
		if (c == null || c.sex != Character.Sex.Male || (c.IsPope() && kingdom == c.GetKingdom()))
		{
			yield break;
		}
		if (Enabled(EnableFlags.HireCourt))
		{
			if (!c.IsAlive())
			{
				if (c.IsInCourt())
				{
					if (c.IsValid())
					{
						c.Destroy();
					}
					else
					{
						kingdom.DelCourtMember(c);
					}
				}
				yield break;
			}
			if (c.IsRebel())
			{
				ConsiderExpense(Expense.Type.ExecuteAction, c.actions?.Find("RemoveRebelFromCourtAction"), null);
				yield break;
			}
		}
		if (Enabled(EnableFlags.HireCourt))
		{
			ThinkAssign(c);
			yield return null;
		}
		c.ThinkSkills();
		c.ThinkSkills();
		yield return null;
		if (Enabled(EnableFlags.Characters))
		{
			yield return CoopThread.Call("ThinkActions", ThinkActions(c));
			yield return CoopThread.Call("ThinkOpportunities", ThinkOpportunities(c));
		}
	}

	private bool ThinkForeignPrisoners(Kingdom target_kingdom = null)
	{
		bool result = false;
		for (int num = this.kingdom.prisoners.Count - 1; num >= 0; num--)
		{
			Character character = this.kingdom.prisoners[num];
			if (target_kingdom == null || character.kingdom_id == target_kingdom.id)
			{
				Kingdom kingdom = character.GetKingdom();
				bool flag = kingdom == null || kingdom.IsDefeated() || !kingdom.IsRegular();
				bool flag2 = kingdom != null && !kingdom.is_player;
				if (!(game.time < character.next_prison_check) || target_kingdom != null || flag)
				{
					if (flag2)
					{
						character.next_prison_check = game.time + this.kingdom.royal_dungeon.def.ai_min_character_prison_time;
					}
					else
					{
						character.next_prison_check = game.time + game.Random(this.kingdom.royal_dungeon.def.ai_actions_subsequent_tick_min, this.kingdom.royal_dungeon.def.ai_actions_subsequent_tick_max);
					}
					RelationUtils.Stance stance = this.kingdom.GetStance(kingdom);
					float num2 = 0f;
					float num3 = 0f;
					if (flag)
					{
						num3 = 100f;
					}
					else if (stance.IsWar())
					{
						num2 = ((character.IsKingOrPrince() || (!character.IsMarshal() && !character.IsSpy() && character.FindStatus(game.defs.Get<Status.Def>("Dangerous")) == null)) ? this.kingdom.royal_dungeon.def.ai_war_execute_other : this.kingdom.royal_dungeon.def.ai_war_execute_marshal_spy_dangerous);
					}
					else if (stance.IsNonAgression() || stance.IsAlliance())
					{
						num3 = this.kingdom.royal_dungeon.def.ai_alliance_release;
					}
					else if (!stance.IsPeace() || flag2)
					{
						num3 = 100f;
					}
					float num4 = game.Random(0, 100);
					if (num4 < num2)
					{
						if (this.kingdom.actions.Find("KillPrisonerAction") is KillPrisonerAction killPrisonerAction)
						{
							killPrisonerAction.target = character;
							killPrisonerAction.Execute(character);
							result = true;
						}
					}
					else
					{
						num4 -= num2;
						if (num4 < num3)
						{
							string id = ((kingdom == this.kingdom) ? "FreePuppetAction" : "FreePrisonerAction");
							if (character.IsPrisonForgivable())
							{
								id = "ForgivePrisonerAction";
							}
							if (this.kingdom.actions.Find(id) is PrisonAction prisonAction)
							{
								prisonAction.target = character;
								prisonAction.Execute(character);
								result = true;
							}
						}
					}
				}
			}
		}
		return result;
	}

	private bool ThinkOwnPrisoners(Kingdom target_kingdom = null)
	{
		bool result = false;
		for (int num = kingdom.court.Count - 1; num >= 0; num--)
		{
			Character character = kingdom.court[num];
			if (character != null && character.prison_kingdom != null && (target_kingdom == null || character.prison_kingdom.id == target_kingdom.id) && (!(game.time < character.next_owner_prison_check) || target_kingdom != null))
			{
				character.next_owner_prison_check = game.time + game.Random(kingdom.royal_dungeon.def.ai_actions_subsequent_tick_min, kingdom.royal_dungeon.def.ai_actions_subsequent_tick_max);
				float num2 = 0f;
				num2 = ((!character.IsKingOrPrince()) ? kingdom.royal_dungeon.def.ai_offer_ransom : kingdom.royal_dungeon.def.ai_offer_royal_ransom);
				if ((float)game.Random(0, 100) < num2)
				{
					(character.actions.Find("OfferRansomAction") as OfferRansomAction).Run();
					result = true;
				}
			}
		}
		return result;
	}

	public bool ThinkKingdomPrisonerActions(Kingdom target_kingdom = null)
	{
		return ThinkForeignPrisoners() | ThinkOwnPrisoners();
	}

	private int CountCourtSlots(CharacterClass.Def cdef, out int characters, out int marshals, out int merchants)
	{
		int num = 0;
		characters = 0;
		marshals = 0;
		merchants = 0;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character == null)
			{
				continue;
			}
			characters++;
			if (cdef == null || character.class_def == cdef)
			{
				num++;
			}
			if (character.IsAlive())
			{
				if (character.IsMarshal())
				{
					marshals++;
				}
				else if (character.IsMerchant())
				{
					merchants++;
				}
			}
		}
		return num;
	}

	private int CountCourtSlots(CharacterClass.Def cdef)
	{
		int num = 0;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null)
			{
				if (cdef == null || character.class_def == cdef)
				{
					num++;
				}
				character.IsAlive();
			}
		}
		return num;
	}

	private bool GatherArmy(Character c)
	{
		if (c.GetArmy() != null)
		{
			return false;
		}
		Action action = c.FindAction("GatherArmyAction");
		if (action == null)
		{
			return false;
		}
		if (action.Validate() != "ok")
		{
			return false;
		}
		Castle castle = DecideOwnCastleForArmy(c);
		if (castle == null)
		{
			return false;
		}
		Kingdom.in_AI_spend = true;
		action.Execute(castle);
		Kingdom.in_AI_spend = false;
		return true;
	}

	private Character ChooseCharacterToGatherArmy()
	{
		Character result = null;
		int num = 0;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character == null || !character.IsAlive() || character.GetArmy() != null)
			{
				continue;
			}
			Action action = character.FindAction("GatherArmyAction");
			if (action != null && !(action.Validate() != "ok"))
			{
				int classLevel = character.GetClassLevel(game.ai.marshal_def);
				if (classLevel > num)
				{
					result = character;
					num = classLevel;
				}
			}
		}
		return result;
	}

	private bool CanExile(Character c)
	{
		if (c.IsKingOrPrince())
		{
			return false;
		}
		if (c.IsPatriarch() || c.IsPope() || c.IsCardinal())
		{
			return false;
		}
		if (c.IsCleric() && BlockSlotForPapalCleric(to_exile: true))
		{
			return false;
		}
		if (c.IsPrisoner())
		{
			if (game.time < c.ai_renounce_time)
			{
				return false;
			}
			return true;
		}
		if (c.IsMarshal())
		{
			return false;
		}
		if (c.GetArmy() != null)
		{
			return false;
		}
		return true;
	}

	private Character ChooseCharacterToExile()
	{
		Character result = null;
		int num = int.MaxValue;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character == null)
			{
				continue;
			}
			if (!character.IsAlive())
			{
				return character;
			}
			if (CanExile(character))
			{
				int num2 = 10 + character.GetClassLevel();
				if (character.IsPrince())
				{
					num2 *= 2;
				}
				if (character.IsPrisoner())
				{
					num2 /= 3;
				}
				else if (character.IsIdle() || character.cur_action is RecallAction)
				{
					num2 /= 2;
				}
				if (num2 < num)
				{
					result = character;
					num = num2;
				}
			}
		}
		return result;
	}

	private bool ExileCharacter(Character c)
	{
		if (c == null || !c.IsValid())
		{
			return false;
		}
		if (c.actions == null)
		{
			c.Destroy();
			return true;
		}
		if (c.cur_action is RecallAction)
		{
			return false;
		}
		for (int i = 0; i < ExileActions.Length; i++)
		{
			string name = ExileActions[i];
			Action action = c.FindAction(name);
			if (action != null && action.Execute(null))
			{
				return true;
			}
		}
		return false;
	}

	private bool ExileImprisonedMarshal()
	{
		Character character = null;
		int num = int.MaxValue;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character2 = kingdom.court[i];
			if (character2 != null && character2.IsAlive() && character2.IsPrisoner() && character2.IsMarshal() && CanExile(character2))
			{
				int num2 = 10 + character2.GetClassLevel();
				if (character2.IsPrince())
				{
					num2 *= 2;
				}
				if (num2 < num)
				{
					character = character2;
					num = num2;
				}
			}
		}
		if (character == null)
		{
			return false;
		}
		if (!ExileCharacter(character))
		{
			return false;
		}
		return true;
	}

	private bool ConsiderExilePrisoner()
	{
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.IsPrisoner() && CanExile(character))
			{
				ExileCharacter(character);
			}
		}
		return false;
	}

	private bool ConsiderHireMarshal()
	{
		Castle castle = null;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character == null || !character.IsAlive() || character.IsPrisoner())
			{
				continue;
			}
			Army army = character.GetArmy();
			if (army == null)
			{
				if (character.IsMarshal() && GatherArmy(character))
				{
					return true;
				}
				continue;
			}
			if (IsLow(army))
			{
				return false;
			}
			if (!IsFull(army))
			{
				Threat threat = GetThreat(army.tgt_realm);
				if (threat == null || threat.level < Threat.Level.Attack)
				{
					return false;
				}
			}
		}
		int characters;
		int marshals;
		int merchants;
		int num = CountCourtSlots(game.ai.marshal_def, out characters, out marshals, out merchants);
		int aIMaxCourtCount = game.ai.marshal_def.GetAIMaxCourtCount(kingdom);
		if (num >= aIMaxCourtCount)
		{
			if (!ExileImprisonedMarshal())
			{
				return false;
			}
			characters--;
		}
		if (castle == null)
		{
			castle = DecideOwnCastleForArmy(null);
			if (castle == null)
			{
				return false;
			}
		}
		int aIMinCourtCount = game.ai.marshal_def.GetAIMinCourtCount(kingdom);
		Expense.Priority priority = ((num < aIMinCourtCount) ? Expense.Priority.Urgent : Expense.Priority.High);
		if (characters >= kingdom.court.Count)
		{
			tmp_expense.Set(kingdom, Expense.Type.HireChacacter, Expense.Category.Military, priority, game.ai.marshal_def, castle);
			if (tmp_expense.eval != 0f)
			{
				return true;
			}
			CategoryData categoryData = categories[(int)tmp_expense.category];
			if (tmp_expense.priority < Expense.Priority.Urgent && categoryData.weight <= 0f)
			{
				return false;
			}
			Character character2 = ChooseCharacterToExile();
			if (character2 == null)
			{
				return true;
			}
			if (!ExileCharacter(character2))
			{
				return true;
			}
		}
		if (BlockSlotForPapalCleric())
		{
			return false;
		}
		ConsiderExpense(Expense.Type.HireChacacter, game.ai.marshal_def, castle, game.ai.marshal_def.ai_category, priority);
		return true;
	}

	private bool BlockSlotForPapalCleric(bool to_exile = false)
	{
		if (kingdom != game.religions.catholic.hq_kingdom)
		{
			return false;
		}
		int num = kingdom.GetClericsCount() - 1;
		int min_cardinals_min = game.religions.catholic.min_cardinals_min;
		if (num >= min_cardinals_min)
		{
			if (to_exile)
			{
				return num == min_cardinals_min;
			}
			return false;
		}
		return kingdom.GetFreeCourtSlots() <= min_cardinals_min - num;
	}

	private bool ConsiderHireMerchant()
	{
		int num = (int)Math.Ceiling(kingdom.GetMaxCommerce() / 10f);
		if (num > kingdom.tradeAgreementsWith.Count)
		{
			num = kingdom.tradeAgreementsWith.Count;
		}
		if (CountCourtSlots(game.ai.merchant_def) >= num)
		{
			return false;
		}
		if (BlockSlotForPapalCleric())
		{
			return false;
		}
		ConsiderExpense(Expense.Type.HireChacacter, game.ai.merchant_def, null, game.ai.merchant_def.ai_category);
		return true;
	}

	private bool ConsiderHireCleric()
	{
		if (game?.ai?.cleric_def == null)
		{
			return false;
		}
		if (kingdom.is_pagan)
		{
			int clericsCount = kingdom.GetClericsCount();
			int aIMaxCourtCount = game.ai.cleric_def.GetAIMaxCourtCount(kingdom);
			if (clericsCount >= aIMaxCourtCount)
			{
				return false;
			}
			if (CountCourtSlots(null) >= kingdom.realms.Count * game.ai.cleric_def.GetAIMaxTotalCharactersPerRealm(kingdom))
			{
				return false;
			}
			ConsiderExpense(Expense.Type.HireChacacter, game.ai.cleric_def, null, game.ai.cleric_def.ai_category);
			return true;
		}
		if (kingdom == game.religions.catholic.hq_kingdom && BlockSlotForPapalCleric())
		{
			ConsiderExpense(Expense.Type.HireChacacter, game.ai.cleric_def, null, game.ai.cleric_def.ai_category);
		}
		return false;
	}

	private bool ConsiderHireCourt()
	{
		if (ConsiderExilePrisoner())
		{
			return true;
		}
		if (ConsiderHireMarshal())
		{
			return true;
		}
		if (kingdom.GetFreeCourtSlotIndex() < 0)
		{
			return false;
		}
		if (ConsiderHireMerchant())
		{
			return true;
		}
		if (ConsiderHireCleric())
		{
			return true;
		}
		CharacterClass.Def random = game.defs.GetRandom<CharacterClass.Def>();
		if (random.name == "Marshal")
		{
			return false;
		}
		int characters;
		int marshals;
		int merchants;
		int num = CountCourtSlots(random, out characters, out marshals, out merchants);
		int aIMaxCourtCount = random.GetAIMaxCourtCount(kingdom);
		if (num >= aIMaxCourtCount)
		{
			return false;
		}
		if (personality == AIPersonality.Default && characters >= kingdom.realms.Count)
		{
			return false;
		}
		if (personality != AIPersonality.Default && characters >= kingdom.realms.Count + 3)
		{
			return false;
		}
		int num2 = aIMaxCourtCount - marshals;
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (random.name == "Merchant")
		{
			num2 = 0;
		}
		if (num2 > 0 && characters + num2 >= kingdom.court.Count)
		{
			return false;
		}
		if (personality != AIPersonality.Default)
		{
			if (characters < kingdom.realms.Count)
			{
				ConsiderExpense(Expense.Type.HireChacacter, random, null, random.ai_category, Expense.Priority.Urgent);
			}
			else
			{
				ConsiderExpense(Expense.Type.HireChacacter, random, null, random.ai_category);
			}
		}
		else
		{
			ConsiderExpense(Expense.Type.HireChacacter, random, null, random.ai_category);
		}
		return true;
	}

	private bool ConsiderIncreaseCrownAuthority()
	{
		CrownAuthority crownAuthority = kingdom.GetCrownAuthority();
		if (crownAuthority == null)
		{
			return false;
		}
		if (crownAuthority.GetValue() >= crownAuthority.Max())
		{
			return false;
		}
		Resource cost = crownAuthority.GetCost();
		if (!kingdom.resources.CanAfford(cost, 1f))
		{
			return false;
		}
		ConsiderExpense(Expense.Type.IncreaseCrownAuthority, null, null, Expense.Category.Other, Expense.Priority.Urgent);
		return true;
	}

	public void ConsiderQuests()
	{
		if (!Enabled(EnableFlags.Kingdom))
		{
			return;
		}
		Quests quests = kingdom.quests;
		if (quests == null || quests.Count == 0)
		{
			return;
		}
		for (int i = 0; i < quests.Count; i++)
		{
			Quest quest = quests[i];
			if (quest != null && quest.CheckConditions() && quest.def.ai_chance > game.Random(0f, 100f))
			{
				quest.Complete();
			}
		}
	}

	private bool ConsiderAdoptTradition()
	{
		List<Tradition.Def> newTraditionOptions = kingdom.GetNewTraditionOptions();
		if (newTraditionOptions == null || newTraditionOptions.Count == 0)
		{
			return false;
		}
		Tradition.Def def = newTraditionOptions[game.Random(0, newTraditionOptions.Count)];
		if (personality != AIPersonality.Default)
		{
			if (personality == AIPersonality.AntiRebellion)
			{
				float num = 0f;
				for (int i = 0; i < newTraditionOptions.Count; i++)
				{
					num += newTraditionOptions[i].ai_eval_stability;
				}
				float num2 = game.Random(0f, num);
				for (int j = 0; j < newTraditionOptions.Count; j++)
				{
					if (num2 < newTraditionOptions[j].ai_eval_stability)
					{
						def = newTraditionOptions[j];
						break;
					}
					num2 -= newTraditionOptions[j].ai_eval_stability;
				}
			}
			else
			{
				float num3 = 0f;
				for (int k = 0; k < newTraditionOptions.Count; k++)
				{
					num3 += newTraditionOptions[k].ai_eval;
				}
				float num4 = game.Random(0f, num3);
				for (int l = 0; l < newTraditionOptions.Count; l++)
				{
					if (num4 < newTraditionOptions[l].ai_eval)
					{
						def = newTraditionOptions[l];
						break;
					}
					num4 -= newTraditionOptions[l].ai_eval;
				}
			}
		}
		Resource adoptCost = def.GetAdoptCost(kingdom);
		if (!kingdom.resources.CanAfford(adoptCost, 1f))
		{
			return false;
		}
		if (personality != AIPersonality.Default)
		{
			ConsiderExpense(Expense.Type.AdoptTradition, def, null, Expense.Category.Economy, Expense.Priority.High);
		}
		else
		{
			ConsiderExpense(Expense.Type.AdoptTradition, def, null, Expense.Category.Economy);
		}
		return true;
	}

	private void CalcPersonality()
	{
		personality = AIPersonality.Improved;
		if (kingdom != null && kingdom.resources[ResourceType.Gold] > 10000f && kingdom.income[ResourceType.Gold] > 500f)
		{
			personality = AIPersonality.RichArmies;
		}
		if (kingdom == null || kingdom.realms == null)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			if (kingdom.realms[i].GetTotalRebellionRisk() < 0f)
			{
				num++;
			}
		}
		if (num > 0)
		{
			personality = AIPersonality.AntiRebellion;
		}
	}

	private void CalcProvinceSpecializations()
	{
		if (personality == AIPersonality.Default)
		{
			for (int i = 0; i < kingdom.realms.Count; i++)
			{
				kingdom.realms[i].ai_specialization = AI.ProvinceSpecialization.General;
			}
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int j = 0; j < kingdom.realms.Count; j++)
		{
			if (kingdom.realms[j].ai_specialization == AI.ProvinceSpecialization.MilitarySpec)
			{
				num++;
			}
			else if (kingdom.realms[j].ai_specialization == AI.ProvinceSpecialization.FoodSpec)
			{
				num2++;
			}
			else if (kingdom.realms[j].ai_specialization == AI.ProvinceSpecialization.ReligionSpec)
			{
				num3++;
			}
			else if (kingdom.realms[j].ai_specialization == AI.ProvinceSpecialization.TradeSpec)
			{
				num4++;
			}
			else
			{
				kingdom.realms[j].ai_specialization = AI.ProvinceSpecialization.GeneralSpec;
			}
		}
		int num5 = kingdom.realms.Count - num - num2 - num3 - num4;
		if (kingdom.realms.Count >= 2 && num < 1)
		{
			Realm realm = null;
			int num6 = -1000;
			for (int k = 0; k < kingdom.realms.Count; k++)
			{
				Realm realm2 = kingdom.realms[k];
				if (realm2.ai_specialization == AI.ProvinceSpecialization.GeneralSpec)
				{
					int num7 = 0;
					for (int l = 0; l < realm2.settlements.Count; l++)
					{
						num7 += realm2.settlements[l].def.ai_eval_military;
					}
					if (realm2.features.Contains("IronOre"))
					{
						num7 += 15;
					}
					if (num7 > num6)
					{
						realm = realm2;
						num6 = num7;
					}
				}
			}
			if (realm != null)
			{
				realm.ai_specialization = AI.ProvinceSpecialization.MilitarySpec;
				num++;
				num5--;
			}
		}
		if (kingdom.realms.Count >= 3 && num2 < 1)
		{
			Realm realm3 = null;
			int num8 = -5;
			for (int m = 0; m < kingdom.realms.Count; m++)
			{
				Realm realm4 = kingdom.realms[m];
				if (realm4.ai_specialization == AI.ProvinceSpecialization.GeneralSpec)
				{
					int num9 = 0;
					for (int n = 0; n < realm4.settlements.Count; n++)
					{
						num9 += realm4.settlements[n].def.ai_eval_food;
					}
					if (num9 > num8)
					{
						realm3 = realm4;
						num8 = num9;
					}
				}
			}
			if (realm3 != null)
			{
				realm3.ai_specialization = AI.ProvinceSpecialization.FoodSpec;
				num2++;
				num5--;
			}
		}
		if (kingdom.realms.Count >= 4 && num3 < 1)
		{
			Realm realm5 = null;
			int num10 = -5;
			for (int num11 = 0; num11 < kingdom.realms.Count; num11++)
			{
				Realm realm6 = kingdom.realms[num11];
				if (realm6.ai_specialization == AI.ProvinceSpecialization.GeneralSpec)
				{
					int num12 = 0;
					for (int num13 = 0; num13 < realm6.settlements.Count; num13++)
					{
						num12 += realm6.settlements[num13].def.ai_eval_religion;
					}
					if (num12 > num10)
					{
						realm5 = realm6;
						num10 = num12;
					}
				}
			}
			if (realm5 != null)
			{
				realm5.ai_specialization = AI.ProvinceSpecialization.ReligionSpec;
				num3++;
				num5--;
			}
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			Realm realm7 = null;
			int num14 = 0;
			AI.ProvinceSpecialization provinceSpecialization = AI.ProvinceSpecialization.GeneralSpec;
			for (int num15 = 0; num15 < kingdom.realms.Count; num15++)
			{
				Realm realm8 = kingdom.realms[num15];
				if (realm8.ai_specialization == AI.ProvinceSpecialization.GeneralSpec)
				{
					int num16 = 5 * (kingdom.realms.Count / 4 - num) + ((num5 == 1) ? (-7) : 0) - 2;
					int num17 = 6 * (num - num2) + ((num5 == 1) ? (-7) : 0) - 5;
					int num18 = ((num5 == 1) ? (-7) : 0) + 4 * (3 - num3);
					int num19 = ((num5 == 1) ? (-7) : 0);
					if (realm8.features.Contains("IronOre"))
					{
						num16 += 10;
					}
					for (int num20 = 0; num20 < realm8.settlements.Count; num20++)
					{
						num16 += realm8.settlements[num20].def.ai_eval_military;
						num17 += realm8.settlements[num20].def.ai_eval_food;
						num18 += realm8.settlements[num20].def.ai_eval_religion;
						num19 += realm8.settlements[num20].def.ai_eval_trade;
					}
					if (num18 > num14)
					{
						realm7 = realm8;
						num14 = num18;
						provinceSpecialization = AI.ProvinceSpecialization.ReligionSpec;
					}
					if (num19 > num14)
					{
						realm7 = realm8;
						num14 = num19;
						provinceSpecialization = AI.ProvinceSpecialization.TradeSpec;
					}
					if (num17 > num14)
					{
						realm7 = realm8;
						num14 = num17;
						provinceSpecialization = AI.ProvinceSpecialization.FoodSpec;
					}
					if (num16 > num14)
					{
						realm7 = realm8;
						num14 = num16;
						provinceSpecialization = AI.ProvinceSpecialization.MilitarySpec;
					}
				}
			}
			if (realm7 != null && num14 > 10 - 3 * num5)
			{
				realm7.ai_specialization = provinceSpecialization;
				switch (provinceSpecialization)
				{
				case AI.ProvinceSpecialization.ReligionSpec:
					num3++;
					break;
				case AI.ProvinceSpecialization.TradeSpec:
					num4++;
					break;
				case AI.ProvinceSpecialization.FoodSpec:
					num2++;
					break;
				case AI.ProvinceSpecialization.MilitarySpec:
					num++;
					break;
				}
				num5--;
				flag = true;
			}
		}
		refresh_realm_specialization = false;
	}

	public IEnumerator ThinkMilitary()
	{
		if (TraceEnabled(EnableFlags.Armies))
		{
			Trace(EnableFlags.Armies, $"ThinkMilitary({kingdom})");
		}
		if (TraceToFileEnabled(EnableFlags.Armies))
		{
			TraceToFile(EnableFlags.Armies, "--- Time: " + game.time.ToString() + " --- " + $"ThinkMilitary({kingdom})");
		}
		CalcBudget();
		ClearExpenses(military_expenses);
		ClearExpenses(urgent_expenses);
		yield return CoopThread.Call("KingdomAI.CalcThreat", CalcThreat());
		if (game.path_finding?.data != null && game.path_finding.data.initted)
		{
			if (Enabled(EnableFlags.Armies))
			{
				yield return CoopThread.Call("ThinkThreats", ThinkThreats());
			}
			if (Enabled(EnableFlags.Units | EnableFlags.Garrison))
			{
				yield return CoopThread.Call("ThinkHireUnits", ThinkHireUnits());
			}
			if (personality == AIPersonality.RichArmies && Enabled(EnableFlags.Units | EnableFlags.Garrison))
			{
				yield return CoopThread.Call("ThinkHireUnits", ThinkHireUnits());
			}
			if (Enabled(EnableFlags.Armies))
			{
				yield return CoopThread.Call("ThinkArmies", ThinkArmies());
			}
			if (urgent_expenses.options.Count > 0)
			{
				yield return CoopThread.Call("Spend urgent expenses", SpendExpenses(urgent_expenses));
			}
			else
			{
				yield return CoopThread.Call("Spend military expenses", SpendExpenses(military_expenses));
			}
			if (TraceToFileEnabled(EnableFlags.Armies))
			{
				TraceToFile(EnableFlags.Armies, "--- Time: " + game.time.ToString() + " --- " + $"ThinkMilitaryEND({kingdom})");
			}
		}
	}

	public static FriendLevel GetFriendLevel(Kingdom kingdom, Kingdom k)
	{
		if (k == null || kingdom == null)
		{
			return FriendLevel.Neutral;
		}
		if (k == kingdom)
		{
			return FriendLevel.Own;
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, k);
		if (kingdomAndKingdomRelation.stance.IsWar())
		{
			return FriendLevel.Enemy;
		}
		float relationship = kingdomAndKingdomRelation.GetRelationship();
		if (relationship <= -500f)
		{
			return FriendLevel.Neutral;
		}
		if (kingdomAndKingdomRelation.stance.Is(RelationUtils.Stance.Alliance | RelationUtils.Stance.AnyVassalage | RelationUtils.Stance.Marriage))
		{
			return FriendLevel.Ally;
		}
		if (relationship >= 500f)
		{
			return FriendLevel.Friend;
		}
		return FriendLevel.Neutral;
	}

	public int DecideBattleSide(Battle battle, Army army = null)
	{
		if (battle == null)
		{
			return -1;
		}
		Object obj = army;
		if (obj == null)
		{
			obj = kingdom;
		}
		int joinSide = battle.GetJoinSide(obj);
		if (joinSide < 0)
		{
			return -1;
		}
		Kingdom k = ((joinSide == 0) ? battle.attacker_kingdom : battle.defender_kingdom);
		FriendLevel friendLevel = GetFriendLevel(kingdom, k);
		Army army2 = battle.GetArmy(1 - joinSide);
		bool flag = ValidToFightRebel(army, army2);
		if (flag)
		{
			if (friendLevel < FriendLevel.Neutral)
			{
				return -1;
			}
		}
		else if (friendLevel < FriendLevel.Friend)
		{
			return -1;
		}
		if (battle.defender_kingdom != kingdom && !flag)
		{
			return -1;
		}
		return joinSide;
	}

	private IEnumerator CalcThreat()
	{
		yield return null;
		threats.Clear();
		int i = 0;
		while (i < kingdom.realms.Count)
		{
			kingdom.realms[i].threat.Recalc(kingdom);
			yield return null;
			int num = i + 1;
			i = num;
		}
		for (int j = 0; j < kingdom.armies.Count; j++)
		{
			Army army = kingdom.armies[j];
			army.tgt_realm = army.GetTargetRealm();
			GetThreat(army.tgt_realm)?.assigned.Add(army);
		}
		if (threats.Count > 1)
		{
			threats.Sort((Threat t1, Threat t2) => t2.CompareTo(t1));
			for (int num2 = 0; num2 < threats.Count; num2++)
			{
				threats[num2].index = num2;
			}
		}
		if (TraceEnabled(EnableFlags.Armies))
		{
			string message = DumpThreat();
			Trace(EnableFlags.Armies, message);
		}
		if (TraceToFileEnabled(EnableFlags.Armies))
		{
			string text = DumpThreat();
			TraceToFile(EnableFlags.Armies, "--- Time: " + game.time.ToString() + " --- " + text);
		}
		if (helpWithRebels == null || helpWithRebels.Count <= 0)
		{
			yield break;
		}
		for (int num3 = 0; num3 < helpWithRebels.Count; num3++)
		{
			Kingdom item = helpWithRebels[num3].Item1;
			for (int num4 = 0; num4 < item.realms.Count; num4++)
			{
				item.realms[num4].help_with_rebels_threat.RecalcEvalArmies(item);
			}
		}
	}

	public string DumpThreat()
	{
		string text = $"Threats: {threats.Count}";
		for (int i = 0; i < threats.Count; i++)
		{
			text += $"\n{threats[i]}";
		}
		return text;
	}

	public Threat GetThreat(Realm r)
	{
		if (r == null)
		{
			return null;
		}
		if (r.kingdom_id == kingdom.id)
		{
			return r.threat;
		}
		if (r.attacker_threat == null || r.attacker_threat.kingdom != kingdom)
		{
			return null;
		}
		if (threats.Contains(r.attacker_threat))
		{
			return r.attacker_threat;
		}
		r.attacker_threat.Clear();
		return null;
	}

	private bool CanReassign(Army army, Threat army_threat, Threat new_threat, ref int army_eval, int pass)
	{
		if (army_threat.reinforceable_battles)
		{
			return false;
		}
		if (army_threat.max_needed <= 0f)
		{
			return true;
		}
		if (army_threat.assigned.eval <= army_threat.min_needed)
		{
			return true;
		}
		bool flag = new_threat.index < army_threat.index;
		float num;
		if (pass != 0)
		{
			num = ((!flag) ? army_threat.max_needed : army_threat.min_needed);
		}
		else
		{
			if (flag)
			{
				return true;
			}
			num = army_threat.min_needed;
		}
		if (army_threat.assigned.eval <= num)
		{
			return false;
		}
		if (army_eval < 0)
		{
			army_eval = army.EvalStrength();
		}
		if (army_threat.assigned.eval - (float)army_eval <= num)
		{
			return false;
		}
		return true;
	}

	private bool CanAssign(Army army, Threat threat, ref int army_eval, int pass)
	{
		if (threat?.realm == null)
		{
			return false;
		}
		if (army?.realm_in == null)
		{
			return false;
		}
		if (army.battle != null)
		{
			return false;
		}
		if (army.tgt_realm == threat.realm)
		{
			return false;
		}
		if (IsLow(army))
		{
			return false;
		}
		if (threat.level < Threat.Level.Attack && !IsFull(army))
		{
			return false;
		}
		if (threat.level == Threat.Level.Attack)
		{
			if (IsLowSupplies(army))
			{
				return false;
			}
			if (army.realm_in.GetKingdom() != threat.realm.GetKingdom() && !IsFull(army))
			{
				return false;
			}
			if (TooSoonRetreat(army))
			{
				return false;
			}
		}
		if (threat.level < Threat.Level.Invaded && IsLowSupplies(army))
		{
			return false;
		}
		Threat threat2 = GetThreat(army.tgt_realm);
		if (threat2 == null)
		{
			return true;
		}
		if (!CanReassign(army, threat2, threat, ref army_eval, pass))
		{
			return false;
		}
		return true;
	}

	private bool AssignArmy(Threat threat, int pass)
	{
		Army army = null;
		float num = float.MaxValue;
		for (int i = 0; i < kingdom.armies.Count; i++)
		{
			Army army2 = kingdom.armies[i];
			int army_eval = -1;
			if (CanAssign(army2, threat, ref army_eval, pass))
			{
				float num2 = army2.position.SqrDist(threat.realm.castle.position);
				if (!(num2 >= num))
				{
					army = army2;
					num = num2;
				}
			}
		}
		if (army == null)
		{
			return false;
		}
		GetThreat(army.tgt_realm)?.assigned.Del(army);
		army.tgt_realm = threat.realm;
		threat.assigned.Add(army);
		return true;
	}

	private void ThinkThreat(Threat t, int pass)
	{
		float num = ((pass != 0) ? t.max_needed : t.min_needed);
		while (t.assigned.eval < num && AssignArmy(t, pass))
		{
		}
	}

	private IEnumerator ThinkThreats()
	{
		int pass = 0;
		while (pass <= 1)
		{
			int num;
			for (int i = 0; i < threats.Count; i = num)
			{
				Threat t = threats[i];
				ThinkThreat(t, pass);
				yield return null;
				num = i + 1;
			}
			num = pass + 1;
			pass = num;
		}
		for (int j = 0; j < kingdom.armies.Count; j++)
		{
			Army army = kingdom.armies[j];
			Threat threat = GetThreat(army.tgt_realm);
			if (threat != null && (!(threat.max_needed > 0f) || !(threat.assigned.eval >= threat.min_needed)))
			{
				threat.assigned.Del(army);
				army.tgt_realm = null;
			}
		}
	}

	private IEnumerator ThinkArmies()
	{
		int i = 0;
		while (i < kingdom.armies.Count)
		{
			Army army = kingdom.armies[i];
			ThinkArmy(army);
			yield return null;
			int num = i + 1;
			i = num;
		}
	}

	public void ThinkArmy(Army army)
	{
		if (army == null)
		{
			return;
		}
		army.ai_thinks++;
		if (army.realm_in == null)
		{
			return;
		}
		bool flag = IsArmyInOwnRealm(army);
		bool flag2 = IsFull(army);
		bool flag3 = !flag2 && IsLow(army);
		bool flag4 = IsLowSupplies(army);
		bool flag5 = HasSupplies(army);
		if (army.IsHiredMercenary())
		{
			return;
		}
		if (army.battle != null)
		{
			BeginProfile("ThinkRetreat");
			bool num = ThinkRetreat(army);
			EndProfile("ThinkRetreat");
			if (!num)
			{
				BeginProfile("ThinkBreakSiege");
				ThinkBreakSiege(army);
				EndProfile("ThinkBreakSiege");
				BeginProfile("ThinkAssaulSiege");
				ThinkAssaultSiege(army);
				EndProfile("ThinkAssaulSiege");
			}
		}
		else
		{
			if (army.IsFleeing())
			{
				return;
			}
			bool flag6 = army.ai_status == "help_with_rebels" || helpWithRebels.Count > 0;
			if (army.tgt_realm != null && !flag6)
			{
				if (ShouldWait(army))
				{
					army.SetAIStatus("wait_others");
					if (army.movement.IsMoving())
					{
						army.Stop();
					}
					return;
				}
				if (army.GetTargetRealm() != army.tgt_realm)
				{
					Send(army, army.tgt_realm.castle, (army.tgt_realm.kingdom_id == kingdom.id) ? "defend_realm" : "attack_realm");
					return;
				}
				if (army.realm_in != army.tgt_realm)
				{
					return;
				}
			}
			if (!flag3)
			{
				if (flag || Enabled(EnableFlags.Offense))
				{
					BeginProfile("ThinkFight");
					bool num2 = ThinkFight(army);
					EndProfile("ThinkFight");
					if (num2)
					{
						return;
					}
				}
				if (!flag6 && !CanRellocate(army))
				{
					if (flag && army.realm_in.castle != null && army.castle == null && army.realm_in.castle.army == null)
					{
						Send(army, army.realm_in.castle, "defend");
					}
					else
					{
						army.SetAIStatus("wait_orders");
					}
					return;
				}
			}
			if (army.castle != null && !flag5)
			{
				army.castle.ResupplyArmy(army);
				army.SetAIStatus("resupplied");
			}
			else
			{
				if (ConsiderHireMercenaries(army))
				{
					return;
				}
				if (flag4 || flag3 || (flag && (!flag2 || (!flag6 && FindUpgradableUnit(army.units) != null))))
				{
					Castle castle = DecideOwnCastleForArmy(army.leader);
					if (castle != null)
					{
						if (flag4)
						{
							if (castle.army == null || !IsLowSupplies(castle.army))
							{
								Send(army, castle, "resupply");
							}
							return;
						}
						if (castle.army == null || castle.army == army || IsFull(castle.army))
						{
							Send(army, castle, "refill");
							return;
						}
					}
				}
				if (ThinkHelpWithRebels(army))
				{
					return;
				}
				if (army.castle == null)
				{
					if (army.GetTarget() is Castle castle2 && castle2.kingdom_id == kingdom.id)
					{
						return;
					}
					Castle castle3 = FindNearestOwnCastle(army, flag);
					if (castle3 != null)
					{
						Send(army, castle3, flag ? "go_inside" : "go_home");
						return;
					}
				}
				army.SetAIStatus("idle");
			}
		}
	}

	private IEnumerator ThinkHireUnits()
	{
		int i = 0;
		while (i < kingdom.realms.Count)
		{
			Realm r = kingdom.realms[i];
			if (r.castle != null && r.castle.battle == null)
			{
				if (Enabled(EnableFlags.Units) && r.castle.army != null)
				{
					ConsiderTakeGarrison(r.castle.army);
					if (!r.IsDisorder() && !r.castle.IsOccupied())
					{
						ConsiderHireArmy(r.castle.army);
						ConsiderHireEquipment(r.castle.army);
						ConsiderHealArmy(r.castle.army);
						ConsiderHealUnits(r.castle.army);
					}
					yield return null;
				}
				if (Enabled(EnableFlags.Garrison))
				{
					ConsiderHireGarrison(r.castle);
					ConsiderUpgradeFortifications(r.castle);
					yield return null;
				}
			}
			int num = i + 1;
			i = num;
		}
	}

	public static bool IsLow(Army a)
	{
		float num = a.MaxUnits() + 1;
		float num2 = a.units.Count;
		for (int i = 0; i < a.units.Count; i++)
		{
			num2 -= a.units[i].damage;
		}
		return num2 < num / 2f;
	}

	public static bool IsFull(Army a)
	{
		return a.units.Count >= a.MaxUnits() + 1;
	}

	public static bool IsLowSupplies(Army army)
	{
		float num = army.supplies.Get();
		float max = army.supplies.GetMax();
		if (num >= max / 4f)
		{
			return false;
		}
		return true;
	}

	public static bool HasSupplies(Army army)
	{
		float num = army.supplies.Get();
		float max = army.supplies.GetMax();
		if (num < max * 3f / 4f)
		{
			return false;
		}
		return true;
	}

	public static bool HasFood(Castle c)
	{
		return c.GetFoodStorage() >= 50f;
	}

	private void CalcLosses(Battle battle, int side, out int lost_squads, out int total_squads)
	{
		lost_squads = (total_squads = 0);
		if (battle?.simulation == null)
		{
			return;
		}
		List<BattleSimulation.Squad> squads = battle.simulation.GetSquads(side);
		if (squads == null)
		{
			return;
		}
		for (int i = 0; i < squads.Count; i++)
		{
			BattleSimulation.Squad squad = squads[i];
			if (squad.unit.def.type != Unit.Type.Noble)
			{
				total_squads++;
				if (squad.IsDefeated())
				{
					lost_squads++;
				}
			}
		}
	}

	private bool ThinkRetreat(Army a)
	{
		if (a?.battle?.simulation == null)
		{
			return false;
		}
		if (!a.CanLeaveBattle())
		{
			return false;
		}
		if (!a.battle.def.AI_retreat)
		{
			return false;
		}
		int battle_side = a.battle_side;
		switch (battle_side)
		{
		default:
			return false;
		case 1:
			if (a.castle != null && !a.battle.CanRetreat(kingdom))
			{
				return false;
			}
			break;
		case 0:
			break;
		}
		a.battle.simulation.CalcTotals();
		float num = a.battle.simulation.estimation;
		if (battle_side == 0)
		{
			num = 1f - num;
		}
		if (num > a.battle.def.AI_retreat_estimation_threshold)
		{
			return false;
		}
		CalcLosses(a.battle, battle_side, out var lost_squads, out var total_squads);
		if (lost_squads < a.battle.def.AI_retreat_min_lost_units)
		{
			return false;
		}
		if (((total_squads == 0) ? 0f : ((float)lost_squads * 100f / (float)total_squads)) < a.battle.def.AI_retreat_min_lost_perc)
		{
			return false;
		}
		a.battle.DoAction("retreat", battle_side);
		return true;
	}

	private void ThinkBreakSiege(Army a)
	{
		if (a.battle.type != Battle.Type.Siege || !a.battle.defender.IsOwnStance(kingdom) || a.battle_side < 0)
		{
			return;
		}
		float num = 100f * a.battle.settlement_food_copy.Get() / a.battle.settlement_food_copy.GetMax();
		bool flag = a.battle.simulation.GetEstimation(a.battle_side) * 100f > def.break_siege_estimation_strong;
		bool flag2 = false;
		if (num <= 0f)
		{
			if (flag)
			{
				flag2 = true;
			}
			else if ((float)game.Random(0, 100) < def.break_siege_weak_army_no_food_chance)
			{
				flag2 = true;
			}
		}
		else if (flag && num <= def.break_siege_strong_army_low_food && (float)game.Random(0, 100) < def.break_siege_strong_army_low_food_chance)
		{
			flag2 = true;
		}
		if (flag2)
		{
			a.battle.BreakSiege(Battle.BreakSiegeFrom.Inside);
		}
	}

	public static void ThinkAssaultSiege(Army a)
	{
		if (a.battle.type == Battle.Type.Siege && a.battle.attacker == a && !(a.battle.simulation.estimation > 0.2f))
		{
			a.battle.Assault();
		}
	}

	private MapObject ResolveTarget(MapObject target)
	{
		if (target == null)
		{
			return null;
		}
		Army army = target as Army;
		Battle battle = ((army == null) ? (target as Battle) : army.battle);
		Settlement settlement = ((battle != null) ? battle.settlement : ((army != null) ? army.castle : (target as Settlement)));
		if (settlement != null)
		{
			return settlement;
		}
		if (battle != null)
		{
			return battle;
		}
		if (army != null)
		{
			return army;
		}
		return target;
	}

	private bool Send(Army army, MapObject target, string status, Battle battle_view_battle = null)
	{
		if (target == null)
		{
			return false;
		}
		BeginProfile("Send army");
		target = ResolveTarget(target);
		MapObject target2 = army.GetTarget();
		target2 = ResolveTarget(target2);
		Battle battle = null;
		if (target is Battle)
		{
			battle = target as Battle;
		}
		else if (target is Settlement)
		{
			battle = (target as Settlement).battle;
		}
		if (battle != null)
		{
			int joinSide = battle.GetJoinSide(army);
			if ((joinSide == 0 || joinSide == 1) && battle.ValidReinforcement(army, joinSide))
			{
				battle.AddIntendedReinforcement(army);
				army.MoveTo(target);
				army.SetAIStatus(status);
			}
		}
		if (battle_view_battle != null)
		{
			return true;
		}
		if (target2 == target || (target2 == null && target == army.castle))
		{
			army.SetAIStatus(status);
			EndProfile("Send army");
			return false;
		}
		Realm realm = game.GetRealm(target.position);
		if (realm != army.last_tgt_realm)
		{
			if (realm == army.prev_tgt_realm)
			{
				army.ai_oscillations++;
			}
			else
			{
				army.ai_oscillations = 0;
			}
			army.prev_tgt_realm = army.last_tgt_realm;
			army.last_tgt_realm = realm;
		}
		army.MoveTo(target);
		army.SetAIStatus(status);
		EndProfile("Send army");
		return true;
	}

	private bool ThinkPlunder(Army army)
	{
		if (army.units.Count <= 1)
		{
			return false;
		}
		Settlement settlement = null;
		float num = float.MaxValue;
		for (int i = 0; i < army.realm_in.settlements.Count; i++)
		{
			Settlement settlement2 = army.realm_in.settlements[i];
			if (settlement2.IsActiveSettlement() && !(settlement2 is Castle) && !settlement2.razed && settlement2.battle == null && settlement2.IsEnemy(army))
			{
				float num2 = settlement2.position.SqrDist(army.position);
				if (!(num2 >= num))
				{
					settlement = settlement2;
					num = num2;
				}
			}
		}
		if (settlement == null)
		{
			return false;
		}
		Send(army, settlement, "plunder");
		return true;
	}

	private bool TooSoonRetreat(Army army)
	{
		if (army.last_retreat_time == Time.Zero)
		{
			return false;
		}
		return game.time - army.last_retreat_time <= def.retreat_fight_cooldown;
	}

	public bool ThinkRetreatBattleview(Battle battle)
	{
		return false;
	}

	private bool ValidToFightRebel(Army army, Army target)
	{
		if (target?.rebel?.rebellion == null)
		{
			return true;
		}
		if (kingdom.rebellions.Contains(target.rebel.rebellion))
		{
			return true;
		}
		for (int i = 0; i < helpWithRebels.Count; i++)
		{
			if (helpWithRebels[i].Item1.rebellions.Contains(target.rebel.rebellion))
			{
				return true;
			}
		}
		return false;
	}

	public bool ThinkFight(Army army, Battle battle_view_battle = null)
	{
		if (army.realm_in == null || army.realm_in.castle == null || TooSoonRetreat(army))
		{
			return false;
		}
		bool flag = IsArmyInOwnRealm(army);
		bool flag2 = army.IsEnemy(army.realm_in);
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		Battle battle = null;
		float num5 = float.MaxValue;
		bool flag3 = false;
		bool flag4 = false;
		Army army2 = null;
		float num6 = float.MaxValue;
		for (int i = 0; i < army.realm_in.armies.Count; i++)
		{
			Army army3 = army.realm_in.armies[i];
			if (army3.battle != null)
			{
				int num7 = DecideBattleSide(army3.battle, army);
				if (num7 >= 0)
				{
					float num8 = army3.battle.position.SqrDist(army.position);
					bool flag5 = ((num7 == 0) ? army3.battle.attacker_kingdom : army3.battle.defender_kingdom) == kingdom;
					if ((flag5 && !flag3) || (flag5 == flag3 && num8 < num5))
					{
						battle = army3.battle;
						num5 = num8;
						flag3 = flag5;
						flag4 = army3.battle.GetArmy(num7) != null;
					}
				}
			}
			if (army3.kingdom_id == army.kingdom_id)
			{
				int num9 = army3.EvalStrength();
				num2 += (float)num9;
				if (army3.battle == null)
				{
					num4 += (float)num9;
				}
			}
			else
			{
				if (!army.IsEnemy(army3) || !ValidToFightRebel(army, army3))
				{
					continue;
				}
				int num10 = army3.EvalStrength();
				num += (float)num10;
				if (army3.battle == null)
				{
					num3 += (float)num10;
				}
				if (army3.castle == null && !army3.IsFleeing() && army3.battle == null)
				{
					float num11 = army3.position.SqrDist(army.position);
					if (!(num11 >= num6))
					{
						army2 = army3;
						num6 = num11;
					}
				}
			}
		}
		bool flag6 = num >= num2 * 1.5f;
		bool flag7 = num3 >= num2 * 1.5f;
		if (flag6)
		{
			if (flag7 && flag && (army.realm_in.castle.army == null || army.realm_in.castle.army == army))
			{
				Send(army, army.realm_in.castle, "enemies_too_strong", battle_view_battle);
				return true;
			}
			if (battle != null && flag4 && army.castle == null && !IsLow(army))
			{
				Send(army, battle, "reinforce_desperate", battle_view_battle);
				return true;
			}
			if (army.realm_in != army.tgt_realm || army.realm_in.kingdom_id == kingdom.id)
			{
				return false;
			}
			if (!flag2)
			{
				army.SetAIStatus("wait_for_battle");
				army.Stop();
				return true;
			}
		}
		if (battle != null)
		{
			Send(army, battle, "reinforce", battle_view_battle);
			return true;
		}
		if (army2 != null)
		{
			Send(army, army2, flag6 ? "attack_desperate" : "attack_army", battle_view_battle);
			return true;
		}
		if (flag2 && army.realm_in.castle != null && army.realm_in.castle.battle == null && !Battle.CanSiege(army))
		{
			int num12 = GetThreat(army.realm_in)?.garrison_eval ?? Threat.EvalCastleStrength(army.realm_in.castle);
			if ((double)num4 >= Math.Ceiling((num3 + (float)num12) * 1.5f))
			{
				Send(army, army.realm_in.castle, "attack_castle", battle_view_battle);
				return true;
			}
		}
		if (battle_view_battle == null && ThinkPlunder(army))
		{
			return true;
		}
		return false;
	}

	private bool ThinkHelpWithRebels(Army army)
	{
		ArmyEval armyEval = new ArmyEval(army);
		for (int i = 0; i < helpWithRebels.Count; i++)
		{
			if (helpWithRebels[i].Item2 < game.time)
			{
				helpWithRebels.RemoveAt(i);
				i--;
				continue;
			}
			Kingdom item = helpWithRebels[i].Item1;
			Rebel rebel = null;
			for (int j = 0; j < item.rebellions.Count; j++)
			{
				Rebellion rebellion = item.rebellions[j];
				for (int k = 0; k < rebellion.rebels.Count; k++)
				{
					Rebel rebel2 = rebellion.rebels[k];
					if (rebel2?.army?.realm_in?.help_with_rebels_threat != null && army.IsEnemy(rebel2.army))
					{
						Threat help_with_rebels_threat = rebel2.army.realm_in.help_with_rebels_threat;
						float num = help_with_rebels_threat.friends_in.eval + help_with_rebels_threat.ours_in.eval;
						if (army.realm_in != rebel2.army.realm_in)
						{
							num += armyEval.eval;
						}
						if (!(num < help_with_rebels_threat.enemies_in.eval) && (rebel == null || rebel2.army.position.SqrDist(army.position) < rebel.army.position.SqrDist(army.position)))
						{
							rebel = rebel2;
						}
					}
				}
			}
			if (rebel?.army?.realm_in != null)
			{
				army.tgt_realm = rebel.army.realm_in;
				Send(army, army.tgt_realm.castle, "help_with_rebels");
				return true;
			}
		}
		return false;
	}

	private bool CanRellocate(Army army)
	{
		Threat threat = GetThreat(army.tgt_realm);
		if (threat == null)
		{
			return true;
		}
		if (threat.level < Threat.Level.Attack)
		{
			return true;
		}
		return false;
	}

	private float EvalETA(Army army, Castle tgt)
	{
		return army.position.Dist(tgt.position);
	}

	private bool ShouldWait(Army army)
	{
		if (army.realm_in == null || army.realm_in.kingdom_id != kingdom.id)
		{
			return false;
		}
		if (army.tgt_realm?.castle == null)
		{
			return false;
		}
		if (army.tgt_realm.kingdom_id == kingdom.id)
		{
			return false;
		}
		if (!army.realm_in.HasLogicNeighbor(army.tgt_realm.GetKingdom()))
		{
			return false;
		}
		Threat threat = GetThreat(army.tgt_realm);
		if (threat == null)
		{
			return false;
		}
		if ((float)army.EvalStrength() >= threat.min_needed)
		{
			return false;
		}
		float num = EvalETA(army, army.tgt_realm.castle);
		Army army2 = army;
		float num2 = num;
		float num3 = num;
		for (int i = 0; i < kingdom.armies.Count; i++)
		{
			Army army3 = kingdom.armies[i];
			if (army3.realm_in != null && army3.tgt_realm == army.tgt_realm && army3 != army)
			{
				float num4 = EvalETA(army3, army.tgt_realm.castle);
				if (num4 < num2)
				{
					army2 = army3;
					num2 = num4;
				}
				else if (num4 > num3 && army3.battle == null)
				{
					num3 = num4;
				}
			}
		}
		if (army2.battle != null)
		{
			return false;
		}
		if (army2.realm_in.kingdom_id == army.tgt_realm.kingdom_id)
		{
			return false;
		}
		float num5 = ((army2.GetTargetRealm() == army.tgt_realm) ? 50 : 25);
		if (num3 - num2 < num5)
		{
			return false;
		}
		if (num - num2 > num5)
		{
			return false;
		}
		return true;
	}

	private bool IsArmyInOwnRealm(Army army)
	{
		return army.realm_in.kingdom_id == kingdom.id;
	}

	private int CountCompetingOwnArmies(Realm r, Kingdom k, Army army)
	{
		if (r == null)
		{
			return 0;
		}
		if (army != null && army.castle == r.castle && !IsFull(army))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < k.armies.Count; i++)
		{
			Army army2 = k.armies[i];
			if (army2 != army && army2.GetTargetRealm() == r && (army == null || !IsFull(army2) || IsFull(army)) && (army == null || !(army.position.SqrDist(r.castle.position) < army2.position.SqrDist(r.castle.position))))
			{
				num++;
			}
		}
		return num;
	}

	private float EvalOwnCastle(Castle castle, Army army, Character leader)
	{
		if (castle == null)
		{
			return 0f;
		}
		if (castle.battle != null)
		{
			return 0f;
		}
		Realm realm = castle.GetRealm();
		if (realm == null)
		{
			return 0f;
		}
		if (army != null && IsLowSupplies(army) && !HasFood(castle))
		{
			return 0f;
		}
		tmp_to_take.Clear();
		tmp_to_leave.Clear();
		float num = castle.EvalTakeGarrison(army, tmp_to_take, tmp_to_leave);
		List<Unit> list = army?.units;
		int max_units = game.ai.army_def.GetMaxUnits(leader, assume_marshal: true) + 1;
		if (tmp_to_take.Count > 0 || tmp_to_leave.Count > 0)
		{
			tmp_existing_units.Clear();
			list = tmp_existing_units;
			if (army?.units != null)
			{
				for (int i = 0; i < army.units.Count; i++)
				{
					Unit item = army.units[i];
					if (!tmp_to_leave.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			list.AddRange(tmp_to_take);
		}
		float num2 = castle.EvalHireUnits(list, max_units, null, null, army, for_garrison: false, allow_militia: true, allow_upgrades: true, float.NegativeInfinity, deterministic: true);
		int num3 = CountCompetingOwnArmies(castle.GetRealm(), kingdom, army);
		float num4 = 1f + (float)(num3 * 1000);
		float num5 = army?.position.Dist(castle.position) ?? 100f;
		if (army?.realm_in == realm && num5 < 90f)
		{
			num5 = 90f;
		}
		else if (num5 < 100f)
		{
			num5 = 100f;
		}
		else if (army?.realm_in != null)
		{
			if (army.realm_in == realm || army.realm_in.neighbors.Contains(realm))
			{
				num5 = 100f;
			}
			if (num5 > 150f && army.realm_in.logicNeighborsAll.Contains(realm))
			{
				num5 = 150f;
			}
		}
		return 1000f * (0.1f + num + num2) / (num4 * num5);
	}

	private Castle FindNearestOwnCastle(Army army, bool free_only)
	{
		Castle result = null;
		float num = float.MaxValue;
		for (int i = 0; i < kingdom.realms.Count; i++)
		{
			Castle castle = kingdom.realms[i]?.castle;
			if (castle != null && (!free_only || (castle.GetRealm() != null && !castle.GetRealm().IsOccupied() && castle.army == null)) && (castle.battle == null || castle.battle.CanJoin(army)))
			{
				float num2 = castle.position.SqrDist(army.position);
				if (num2 < num)
				{
					result = castle;
					num = num2;
				}
			}
		}
		return result;
	}

	private Castle DecideOwnCastleForArmy(Character leader)
	{
		if (kingdom.realms.Count < 2)
		{
			if (kingdom.realms.Count >= 1 && !kingdom.realms[0].IsOccupied() && kingdom.realms[0].castle?.battle == null)
			{
				return kingdom.realms[0].castle;
			}
			return null;
		}
		BeginProfile("DecideOwnCastleForArmy");
		Army army = leader?.GetArmy();
		Castle castle = null;
		using (new Stat.ForceCached("DecideOwnCastleForArmy"))
		{
			Realm realm = army?.GetTargetRealm();
			if (realm?.castle != null && realm.IsOwnStance(army))
			{
				castle = realm.castle;
			}
			if (castle == null && army?.realm_in?.castle != null && army.realm_in.IsOwnStance(army))
			{
				castle = army.realm_in.castle;
			}
			float num = EvalOwnCastle(castle, army, leader);
			int num2 = game.Random(0, kingdom.realms.Count);
			for (int i = 0; i < kingdom.realms.Count; i++)
			{
				Realm realm2 = kingdom.realms[(num2 + i) % kingdom.realms.Count];
				if (realm2.IsOccupied())
				{
					continue;
				}
				Castle castle2 = realm2?.castle;
				if (castle2 != null && castle2 != castle && castle2.battle == null)
				{
					float num3 = EvalOwnCastle(castle2, army, leader);
					if (!(num3 <= num))
					{
						castle = castle2;
						num = num3;
					}
				}
			}
			EndProfile("DecideOwnCastleForArmy");
			return castle;
		}
	}

	private bool ConsiderHireMercenaries(Army army)
	{
		if (!Enabled(EnableFlags.Mercenaries))
		{
			return false;
		}
		if (army.realm_in == null)
		{
			return false;
		}
		if (IsFull(army))
		{
			return false;
		}
		if (kingdom.GetFood() <= 0f)
		{
			return false;
		}
		for (int i = 0; i < army.realm_in.armies.Count; i++)
		{
			Mercenary mercenary = army.realm_in.armies[i].mercenary;
			if (mercenary != null && ConsiderHireMercenaries(army, mercenary))
			{
				return true;
			}
		}
		Mercenary.GetHeadlessMercenaries(kingdom, tmp_merc);
		for (int j = 0; j < tmp_merc.Count; j++)
		{
			Mercenary mercenary2 = tmp_merc[j];
			if (mercenary2 != null && ConsiderHireMercenaries(army, mercenary2))
			{
				return true;
			}
		}
		return false;
	}

	private bool ConsiderHireMercenaries(Army army, Mercenary m)
	{
		if (!m.ValidForHireAsUnit() || m.army.movement.IsMoving())
		{
			return false;
		}
		bool result = false;
		float num = 0f;
		do
		{
			Unit unit = ChooseMercenaryUnitToBuy(m, army, num);
			if (unit == null)
			{
				break;
			}
			if (!m.buyers.Contains(army))
			{
				Send(army, m.army, "go_to_mercenary");
				return true;
			}
			if (!m.Buy(unit, army))
			{
				break;
			}
			result = true;
			num += unit.def.upkeep[ResourceType.Food];
		}
		while (!IsFull(army));
		return result;
	}

	private bool CheckMercenaryUnitCost(Mercenary m, Unit unit, Army army, float food_spent)
	{
		Resource unitCost = m.GetUnitCost(unit, army);
		Resource upkeep = unit.def.upkeep;
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = kingdom.resources[resourceType];
			float num2 = kingdom.income[resourceType] - kingdom.expenses[resourceType];
			switch (resourceType)
			{
			case ResourceType.Gold:
				num2 += kingdom.inflation;
				num /= 4f;
				num2 /= 10f;
				break;
			case ResourceType.Food:
				num -= food_spent;
				num2 -= food_spent;
				break;
			}
			if (unitCost != null && unitCost[resourceType] > 0f && unitCost[resourceType] > num)
			{
				return false;
			}
			if (upkeep != null && upkeep[resourceType] > 0f && upkeep[resourceType] > num2)
			{
				return false;
			}
		}
		return true;
	}

	private Unit ChooseMercenaryUnitToBuy(Mercenary merc, Army army, float food_spent)
	{
		using (new Stat.ForceCached("ChooseMercenaryUnitToBuy"))
		{
			if (merc?.army == null || !merc.IsValid() || !merc.army.IsValid())
			{
				return null;
			}
			Unit result = null;
			float num = 0f;
			for (int i = 0; i < merc.army.units.Count; i++)
			{
				Unit unit = merc.army.units[i];
				if (unit.def.type != Unit.Type.Noble && CheckMercenaryUnitCost(merc, unit, army, food_spent))
				{
					float num2 = unit.EvalStrength();
					if (num2 > num)
					{
						result = unit;
						num = num2;
					}
				}
			}
			return result;
		}
	}

	private bool ConsiderHealArmy(Army army)
	{
		Action action = army.leader.FindAction("CampArmyAction");
		if (action == null)
		{
			return false;
		}
		if (!action.Execute(null))
		{
			return false;
		}
		return true;
	}

	private bool ConsiderHealUnits(Army army)
	{
		Action action = army.leader.FindAction("HealArmyUnitAction");
		if (action == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < army.units.Count; i++)
		{
			if (army.units[i].damage != 0f)
			{
				if (action.args == null)
				{
					action.args = new List<Value>();
				}
				else
				{
					action.args.Clear();
				}
				action.args.Add(i);
				if (action.Execute(null))
				{
					result = true;
				}
			}
		}
		action.args?.Clear();
		return result;
	}

	public static float EvalHireUnit(Unit.Def udef, Army army)
	{
		if (udef == null)
		{
			return 0f;
		}
		if (udef.type == Unit.Type.InventoryItem)
		{
			return 1f + udef.siege_damage;
		}
		return udef.strength_eval;
	}

	private void ConsiderHireArmy(Army army)
	{
		if (army?.castle != null)
		{
			Threat threat = army.realm_in.threat;
			float food = ((threat.assigned.eval < threat.min_needed) ? float.PositiveInfinity : float.NegativeInfinity);
			tmp_to_hire.Clear();
			army.castle.EvalHireUnits(army.units, army.MaxUnits() + 1, tmp_to_hire, null, army, for_garrison: false, allow_militia: true, allow_upgrades: true, food);
			for (int i = 0; i < tmp_to_hire.Count; i++)
			{
				Unit.Def defParam = tmp_to_hire[i];
				ConsiderExpense(Expense.Type.HireArmyUnit, defParam, army.castle, Expense.Category.Military, Expense.Priority.Urgent);
			}
		}
	}

	private void ConsiderHireEquipment(Army army)
	{
		if (army?.castle != null)
		{
			Threat threat = army.realm_in.threat;
			float food = ((threat.assigned.eval < threat.min_needed) ? float.PositiveInfinity : float.NegativeInfinity);
			tmp_to_hire.Clear();
			army.castle.EvalHireEquipment(army.siege_equipment, Math.Min(def.max_army_equipment, army.MaxItems()), tmp_to_hire, army, food);
			for (int i = 0; i < tmp_to_hire.Count; i++)
			{
				Unit.Def defParam = tmp_to_hire[i];
				ConsiderExpense(Expense.Type.HireArmyEquipment, defParam, army.castle, Expense.Category.Military);
			}
		}
	}

	public static Unit FindUpgradableUnit(List<Unit> existing_units, List<Unit> ignore = null)
	{
		if (existing_units == null)
		{
			return null;
		}
		for (int i = 0; i < existing_units.Count; i++)
		{
			Unit unit = existing_units[i];
			if (unit?.def != null && unit.def.type != Unit.Type.Noble && unit.def.ai_emergency_only && (ignore == null || !ignore.Contains(unit)))
			{
				return unit;
			}
		}
		return null;
	}

	public static int NumUpgradableUnits(List<Unit> existing_units)
	{
		if (existing_units == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < existing_units.Count; i++)
		{
			Unit unit = existing_units[i];
			if (unit?.def != null && unit.def.type != Unit.Type.Noble && unit.def.ai_emergency_only)
			{
				num++;
			}
		}
		return num;
	}

	public static Unit ChooseBestUnit(List<Unit> units, Army army, out float eval, List<Unit> ignore = null)
	{
		eval = 0f;
		if (units == null)
		{
			return null;
		}
		using (new Stat.ForceCached("ChooseBestUnit"))
		{
			Unit result = null;
			for (int i = 0; i < units.Count; i++)
			{
				Unit unit = units[i];
				if (unit.def.type != Unit.Type.Noble && (ignore == null || !ignore.Contains(unit)))
				{
					float num = unit.EvalStrength(army);
					if (num > eval)
					{
						result = unit;
						eval = num;
					}
				}
			}
			return result;
		}
	}

	public static Unit ChooseBestGarrisonUnit(Castle castle, Army army, out float eval, List<Unit> ignore = null)
	{
		return ChooseBestUnit(castle?.garrison?.units, army, out eval, ignore);
	}

	public static Unit ChooseBestArmyUnit(Army army, out float eval, List<Unit> ignore = null)
	{
		return ChooseBestUnit(army?.units, army, out eval, ignore);
	}

	public static Unit ChooseWorstUnit(List<Unit> units, Army army, out float eval, bool upgradable_only, List<Unit> ignore = null)
	{
		eval = float.MaxValue;
		if (units == null)
		{
			return null;
		}
		using (new Stat.ForceCached("ChooseWorstUnit"))
		{
			Unit result = null;
			for (int i = 0; i < units.Count; i++)
			{
				Unit unit = units[i];
				if (unit.def.type != Unit.Type.Noble && (!upgradable_only || unit.def.ai_emergency_only) && (ignore == null || !ignore.Contains(unit)))
				{
					float num = unit.EvalStrength(army);
					if (num < eval)
					{
						result = unit;
						eval = num;
					}
				}
			}
			return result;
		}
	}

	public static Unit ChooseWorstGarrisonUnit(Castle castle, out float eval, bool upgradable_only, List<Unit> ignore = null)
	{
		return ChooseWorstUnit(castle?.garrison?.units, null, out eval, upgradable_only, ignore);
	}

	public static Unit ChooseWorstArmyUnit(Army army, out float eval, bool upgradable_only, List<Unit> ignore = null)
	{
		return ChooseWorstUnit(army?.units, army, out eval, upgradable_only, ignore);
	}

	private void ConsiderTakeGarrison(Army army)
	{
		if (army.castle == null)
		{
			return;
		}
		while (true)
		{
			float eval;
			Unit unit = ChooseBestGarrisonUnit(army.castle, army, out eval);
			if (unit == null)
			{
				break;
			}
			if (!IsFull(army))
			{
				if (!army.MoveUnitFromGarrison(unit))
				{
					break;
				}
				continue;
			}
			float eval2;
			Unit army_unit = ChooseWorstArmyUnit(army, out eval2, upgradable_only: false);
			if (eval2 >= eval || !army.SwapUnitWithGarrison(army_unit, unit))
			{
				break;
			}
		}
	}

	private bool ConsiderHireMercenaryArmy()
	{
		if (kingdom.mercenaries_in.Count == 0)
		{
			return false;
		}
		if (this.def.max_num_mercenaries != null && kingdom.mercenaries.Count >= this.def.max_num_mercenaries.Int(kingdom))
		{
			return false;
		}
		for (int i = 0; i < kingdom.mercenaries_in.Count; i++)
		{
			Army army = kingdom.mercenaries_in[i];
			if (army?.mercenary == null)
			{
				kingdom.Error($"Missing mercenary in kingdom.mercenaries_in {army}");
			}
			else
			{
				if (!army.mercenary.ValidForHireAsArmy())
				{
					continue;
				}
				int num = game.Random(0, MercenaryMission.defs.Count);
				for (int j = 0; j < MercenaryMission.defs.Count; j++)
				{
					MercenaryMission.Def def = MercenaryMission.defs[(j + num) % MercenaryMission.defs.Count];
					if (def.Validate(army.mercenary, kingdom))
					{
						ConsiderExpense(Expense.Type.HireMercenaryArmy, def, army.mercenary, Expense.Category.Military);
						return true;
					}
				}
			}
		}
		return false;
	}

	private Expense.Priority CalcGarrisonPriority(Threat threat)
	{
		if (threat.level >= Threat.Level.Invaded)
		{
			return Expense.Priority.Urgent;
		}
		if (threat.enemies_in.Count > 0 || threat.enemies_nearby.Count > 0)
		{
			return Expense.Priority.Urgent;
		}
		if (threat.level == Threat.Level.Neighbors)
		{
			return Expense.Priority.High;
		}
		if (threat.level <= Threat.Level.Safe)
		{
			return Expense.Priority.Low;
		}
		return Expense.Priority.Normal;
	}

	private bool ConsiderHireGarrison(Castle castle)
	{
		Realm realm = castle?.GetRealm();
		if (castle == null || castle.battle != null || realm.IsOccupied() || realm.IsDisorder())
		{
			return false;
		}
		if (castle.army != null && !IsFull(castle.army))
		{
			return false;
		}
		float food = kingdom.GetFood();
		Threat threat = realm.threat;
		bool flag = threat.assigned.eval < threat.min_needed;
		if (flag)
		{
			food = float.PositiveInfinity;
		}
		else
		{
			food -= game.ai.def.food_reserve;
			if (food <= 0f)
			{
				return false;
			}
		}
		tmp_to_hire.Clear();
		castle.EvalHireUnits(castle.garrison.units, castle.garrison.SlotCount(), tmp_to_hire, null, null, !flag, allow_militia: true, allow_upgrades: true, food);
		Expense.Priority priority = CalcGarrisonPriority(threat);
		for (int i = 0; i < tmp_to_hire.Count; i++)
		{
			Unit.Def defParam = tmp_to_hire[i];
			ConsiderExpense(Expense.Type.HireGarrison, defParam, castle, Expense.Category.Military, priority);
		}
		return true;
	}

	private bool ConsiderUpgradeFortifications(Castle castle)
	{
		Realm realm = castle?.GetRealm();
		if (realm == null)
		{
			return false;
		}
		Threat threat = realm.threat;
		if (threat.level == Threat.Level.Safe || threat.level >= Threat.Level.Invaded)
		{
			return false;
		}
		if (categories[1].weight <= 0f)
		{
			return false;
		}
		Expense.Priority priority = Expense.Priority.Low;
		if (castle.fortifications.level == 0 && threat.level == Threat.Level.Neighbors)
		{
			priority = Expense.Priority.Normal;
		}
		if (priority == Expense.Priority.Low)
		{
			if (next_build_expense.category == Expense.Category.Military)
			{
				return false;
			}
			if (next_upgrade_expense.category == Expense.Category.Military)
			{
				return false;
			}
		}
		if (!castle.CanUpgradeFortification())
		{
			return false;
		}
		if (!castle.CanAffordFortificationsUpgrade())
		{
			return false;
		}
		ConsiderExpense(Expense.Type.UpgradeFortifications, null, castle, Expense.Category.Military, priority);
		return true;
	}
}
