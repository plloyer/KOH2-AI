using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Logic.ExtensionMethods;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Kingdom, dynamic = false)]
public class Kingdom : Object
{
	public enum TradeAgreement
	{
		None,
		Trade,
		Exclusive
	}

	public enum Type
	{
		Regular,
		Faction,
		RebelFaction,
		LoyalistsFaction,
		ReligiousFaction,
		Crusade,
		Exile
	}

	public struct Upgrading
	{
		public Building.Def def;

		public Time start_time;

		public float duration;

		public Time end_time => start_time + duration;

		public override string ToString()
		{
			string text = def?.id ?? "???";
			Game game = def?.field?.dt?.context?.game;
			if (game != null)
			{
				return text + $" ({game.time - start_time:F1} / {duration:F1})";
			}
			return text + $" ({duration:F1})";
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct CacheRBS : IDisposable
	{
		public struct EndGameValidateParams
		{
			public Kingdom won;

			public Kingdom defeated;

			public Game game;
		}

		public static int depth = 0;

		public static List<Kingdom> requested = new List<Kingdom>(16);

		public static List<EndGameValidateParams> requested_end_games = new List<EndGameValidateParams>();

		public CacheRBS(string reason)
		{
			depth++;
		}

		public void Dispose()
		{
			if (--depth > 0)
			{
				return;
			}
			for (int i = 0; i < requested.Count; i++)
			{
				requested[i].RecalcBuildingStates();
			}
			for (int j = 0; j < requested_end_games.Count; j++)
			{
				EndGameValidateParams endGameValidateParams = requested_end_games[j];
				if (endGameValidateParams.game.ValidateEndGame(endGameValidateParams.won, endGameValidateParams.defeated))
				{
					break;
				}
			}
			requested.Clear();
			requested_end_games.Clear();
		}

		public static bool RequestEndGame(Kingdom won, Kingdom defeated, Game game)
		{
			if (depth <= 0)
			{
				return true;
			}
			requested_end_games.Add(new EndGameValidateParams
			{
				won = won,
				defeated = defeated,
				game = game
			});
			return false;
		}

		public static bool Request(Kingdom k, Castle origin, bool remove_abandoned)
		{
			if (depth <= 0)
			{
				return true;
			}
			if (origin != null || remove_abandoned)
			{
				Game.Log("CacheRBS non-default request while cached", Game.LogType.Warning);
				return true;
			}
			if (!requested.Contains(k))
			{
				requested.Add(k);
			}
			return false;
		}
	}

	private struct ResourceProduction
	{
		public int calculated;

		public int amount;

		public List<Building> producers;

		public List<Building> producers_completed;

		public List<Character> importers;
	}

	[Serialization.State(11)]
	public class IdState : Serialization.ObjectState
	{
		public int kingdom_id;

		public static IdState Create()
		{
			return new IdState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			return kingdom.id != kingdom.nid;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom_id = kingdom.id;
			return kingdom.id != kingdom.nid;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_id, "id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdom_id = ser.Read7BitUInt("id");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.id != kingdom_id)
			{
				obj.Warning("Changing id from " + kingdom.id + " to " + kingdom_id);
			}
			kingdom.id = kingdom_id;
		}
	}

	[Serialization.State(12)]
	public class NameAndCultureState : Serialization.ObjectState
	{
		public string kingdomName;

		public string cultureCSVKey;

		public string unitsSetCSVKey;

		public static NameAndCultureState Create()
		{
			return new NameAndCultureState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return false;
			}
			if (!(kingdom.Name != kingdom.ActiveName) && !(kingdom.culture_csv_key != kingdom.csv_field?.key))
			{
				return kingdom.units_set_csv_key != kingdom.csv_field?.key;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdomName = kingdom.ActiveName;
			cultureCSVKey = kingdom.culture_csv_key;
			unitsSetCSVKey = kingdom.units_set_csv_key;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(kingdomName, "name");
			ser.WriteStr(cultureCSVKey, "cultureCSVKey");
			ser.WriteStr(unitsSetCSVKey, "unitsSetCSVKey");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kingdomName = ser.ReadStr("name");
			cultureCSVKey = ser.ReadStr("cultureCSVKey");
			unitsSetCSVKey = ser.ReadStr("unitsSetCSVKey");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).ChangeNameAndCulture(kingdomName, cultureCSVKey, unitsSetCSVKey, send_state: false);
		}
	}

	[Serialization.State(13)]
	public class CourtState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class CourtMemberState : Serialization.ObjectSubstate
		{
			public NID characterNid;

			public CourtMemberState()
			{
			}

			public CourtMemberState(int idx, Character character)
			{
				substate_index = idx;
				characterNid = character;
			}

			public static CourtMemberState Create()
			{
				return new CourtMemberState();
			}

			public static bool IsNeeded(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.court != null)
				{
					return kingdom.court.Count > 0;
				}
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				Character character = (obj as Kingdom).court[substate_index];
				characterNid = character;
				return character != null;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteNID<Character>(characterNid, "characterNid");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				characterNid = ser.ReadNID<Character>("characterNid");
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				Character character = characterNid.Get<Character>(obj.game);
				if (character == null)
				{
					if (kingdom.court == null)
					{
						return;
					}
					Character param = kingdom.court[substate_index];
					kingdom.court[substate_index] = null;
					kingdom.NotifyListeners("del_court", param);
				}
				kingdom.AddCourtMember(character, substate_index, is_hire: false, send_state: false);
				kingdom.InvalidateIncomes();
			}
		}

		[Serialization.Substate(2)]
		public class NewKnightHiredStatus : Serialization.ObjectSubstate
		{
			public NID characterNid;

			public NewKnightHiredStatus()
			{
			}

			public NewKnightHiredStatus(int idx, Character character)
			{
				substate_index = idx;
				characterNid = character;
			}

			public static NewKnightHiredStatus Create()
			{
				return new NewKnightHiredStatus();
			}

			public static bool IsNeeded(Object obj)
			{
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				Character character = (obj as Kingdom).court[substate_index];
				characterNid = character;
				return character != null;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteNID<Character>(characterNid, "characterNid");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				characterNid = ser.ReadNID<Character>("characterNid");
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				Character character = characterNid.Get<Character>(obj.game);
				if (character != null)
				{
					kingdom.NotifyListeners("new_knight_hired", character);
				}
			}
		}

		[Serialization.Substate(3)]
		public class SpecialCourtMemberState : Serialization.ObjectSubstate
		{
			public NID characterNid;

			public SpecialCourtMemberState()
			{
			}

			public SpecialCourtMemberState(int idx, Character character)
			{
				substate_index = idx;
				characterNid = character;
			}

			public static SpecialCourtMemberState Create()
			{
				return new SpecialCourtMemberState();
			}

			public static bool IsNeeded(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.special_court != null)
				{
					return kingdom.special_court.Count > 0;
				}
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				Character character = (obj as Kingdom).special_court[substate_index];
				characterNid = character;
				return character != null;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteNID<Character>(characterNid, "characterNid");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				characterNid = ser.ReadNID<Character>("characterNid");
			}

			public override void ApplyTo(Object obj)
			{
				if (!(obj is Kingdom kingdom))
				{
					return;
				}
				Character character = characterNid.Get<Character>(obj.game);
				if (character == null)
				{
					if (kingdom.special_court == null)
					{
						return;
					}
					kingdom.special_court[substate_index] = null;
				}
				else
				{
					if (kingdom.special_court == null)
					{
						kingdom.InitCourt();
					}
					kingdom.special_court[substate_index] = character;
				}
				kingdom.NotifyListeners("court_changed");
			}
		}

		public static CourtState Create()
		{
			return new CourtState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.court == null)
			{
				return false;
			}
			for (int i = 0; i < kingdom.court.Count; i++)
			{
				if (kingdom.GetCourtOrSpecialCourtMember(i) != null)
				{
					return true;
				}
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.court.Count; i++)
			{
				Character character = kingdom.court[i];
				if (character != null)
				{
					AddSubstate(new CourtMemberState(i, character));
				}
			}
			for (int j = 0; j < kingdom.special_court.Count; j++)
			{
				Character character2 = kingdom.special_court[j];
				if (character2 != null)
				{
					AddSubstate(new SpecialCourtMemberState(j, character2));
				}
			}
			if (kingdom.court.Count > 0)
			{
				return kingdom.special_court.Count > 0;
			}
			return false;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.InitCourt();
			for (int i = 0; i < kingdom.court.Count; i++)
			{
				CourtMemberState substate = GetSubstate<CourtMemberState>(i);
				if (substate == null)
				{
					kingdom.court[i] = null;
					continue;
				}
				Character value = substate.characterNid.Get<Character>(obj.game);
				kingdom.court[i] = value;
			}
			for (int j = 0; j < kingdom.special_court.Count; j++)
			{
				SpecialCourtMemberState substate2 = GetSubstate<SpecialCourtMemberState>(j);
				if (substate2 == null)
				{
					kingdom.special_court[j] = null;
					continue;
				}
				Character value2 = substate2.characterNid.Get<Character>(obj.game);
				kingdom.special_court[j] = value2;
			}
			substates = null;
			kingdom.NotifyListeners("court_changed");
		}
	}

	[Serialization.State(14)]
	public class RoyalFamilyState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class ChildrenState : Serialization.ObjectSubstate
		{
			public List<NID> children = new List<NID>();

			public ChildrenState()
			{
			}

			public ChildrenState(int idx, List<Character> characters)
			{
				substate_index = idx;
				children = new List<NID>();
				foreach (Character character in characters)
				{
					NID item = character;
					children.Add(item);
				}
			}

			public static ChildrenState Create()
			{
				return new ChildrenState();
			}

			public static bool IsNeeded(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.royalFamily != null)
				{
					return kingdom.royalFamily.Children.Count > 0;
				}
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.royalFamily == null || kingdom.royalFamily.Children.Count <= 0)
				{
					return false;
				}
				foreach (Character child in kingdom.royalFamily.Children)
				{
					NID item = child;
					children.Add(item);
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(children.Count, "count");
				for (int i = 0; i < children.Count; i++)
				{
					NID nID = children[i];
					ser.WriteNID<Character>(nID, "nid", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				int num = ser.Read7BitUInt("count");
				for (int i = 0; i < num; i++)
				{
					children.Add(ser.ReadNID<Character>("nid", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				obj.game.defs.GetBase<RoyalFamily.Def>();
				kingdom.royalFamily.Children.Clear();
				for (int i = 0; i < children.Count; i++)
				{
					if (kingdom.royalFamily.Children.Count >= kingdom.royalFamily.MaxChildren())
					{
						break;
					}
					Character character = children[i].Get<Character>(obj.game);
					if (character == null)
					{
						Game.Log("RoyalFamily(" + kingdom.ToString() + ").Children state: Could not resolve " + children[i].ToString(), Game.LogType.Warning);
						continue;
					}
					if (character.prefered_class_def == null && kingdom.royalFamily.Sovereign != null)
					{
						character.prefered_class_def = kingdom.royalFamily.Sovereign.class_def;
					}
					kingdom.royalFamily.Children.Add(character);
				}
			}
		}

		[Serialization.Substate(2)]
		public class RelativesState : Serialization.ObjectSubstate
		{
			public List<NID> relatives = new List<NID>();

			public RelativesState()
			{
			}

			public RelativesState(int idx, List<Character> characters)
			{
				substate_index = idx;
				relatives = new List<NID>();
				foreach (Character character in characters)
				{
					NID item = character;
					relatives.Add(item);
				}
			}

			public static RelativesState Create()
			{
				return new RelativesState();
			}

			public static bool IsNeeded(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.royalFamily != null)
				{
					return kingdom.royalFamily.Relatives.Count > 0;
				}
				return false;
			}

			public override bool InitFrom(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.royalFamily != null)
				{
					foreach (Character relative in kingdom.royalFamily.Relatives)
					{
						NID item = relative;
						relatives.Add(item);
					}
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(relatives.Count, "count");
				for (int i = 0; i < relatives.Count; i++)
				{
					NID nID = relatives[i];
					ser.WriteNID<Character>(nID, "nid", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				int num = ser.Read7BitUInt("count");
				for (int i = 0; i < num; i++)
				{
					relatives.Add(ser.ReadNID<Character>("nid", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				RoyalFamily.Def def = obj.game.defs.GetBase<RoyalFamily.Def>();
				kingdom.royalFamily.Relatives.Clear();
				for (int i = 0; i < relatives.Count; i++)
				{
					if (kingdom.royalFamily.Relatives.Count >= def.max_relatives)
					{
						break;
					}
					Character character = relatives[i].Get<Character>(obj.game);
					if (character != null)
					{
						kingdom.royalFamily.Relatives.Add(character);
					}
				}
			}
		}

		private int generations_passed;

		private NID soveren_nid;

		private NID spouse_nid;

		private NID spouseFacination_nid;

		private NID heir_nid;

		public static RoyalFamilyState Create()
		{
			return new RoyalFamilyState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (obj is Kingdom { royalFamily: not null } kingdom)
			{
				if (kingdom.royalFamily.Sovereign != null)
				{
					return true;
				}
				if (kingdom.royalFamily.Spouse != null)
				{
					return true;
				}
				if (kingdom.royalFamily.SpouseFacination != null)
				{
					return true;
				}
				if (kingdom.royalFamily.Heir != null)
				{
					return true;
				}
				if (kingdom.royalFamily.Children.Count > 0)
				{
					return true;
				}
				if (kingdom.royalFamily.Relatives.Count > 0)
				{
					return true;
				}
				if (kingdom.generationsPassed != 0)
				{
					return true;
				}
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Kingdom { royalFamily: not null } kingdom))
			{
				return false;
			}
			generations_passed = kingdom.generationsPassed;
			soveren_nid = kingdom.royalFamily.Sovereign;
			spouse_nid = kingdom.royalFamily.Spouse;
			spouseFacination_nid = kingdom.royalFamily.SpouseFacination;
			heir_nid = kingdom.royalFamily.Heir;
			AddSubstate(new ChildrenState(0, kingdom.royalFamily.Children));
			AddSubstate(new RelativesState(1, kingdom.royalFamily.Relatives));
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(generations_passed, "generations_passed");
			ser.WriteNID<Character>(soveren_nid, "soveren_nid");
			ser.WriteNID<Character>(spouse_nid, "spouse_nid");
			ser.WriteNID<Character>(spouseFacination_nid, "spouseFacination_nid");
			ser.WriteNID<Character>(heir_nid, "heir_nid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			generations_passed = ser.Read7BitUInt("generations_passed");
			soveren_nid = ser.ReadNID<Character>("soveren_nid");
			spouse_nid = ser.ReadNID<Character>("spouse_nid");
			spouseFacination_nid = ser.ReadNID<Character>("spouseFacination_nid");
			heir_nid = ser.ReadNID<Character>("heir_nid");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom obj2 = obj as Kingdom;
			RoyalFamily royalFamily = obj2.royalFamily;
			Character newSovereign = soveren_nid.Get<Character>(obj.game);
			Character spouse = spouse_nid.Get<Character>(obj.game);
			Character spouseFacination = spouseFacination_nid.Get<Character>(obj.game);
			Character heir = heir_nid.Get<Character>(obj.game);
			obj2.generationsPassed = generations_passed;
			royalFamily.SetSovereign(newSovereign, send_state: false);
			royalFamily.SetSpouse(spouse, setTitle: false, send_state: false);
			royalFamily.SetSpouseFacination(spouseFacination, send_state: false);
			royalFamily.SetHeir(heir, send_state: false, from_state: true);
		}
	}

	[Serialization.State(15)]
	public class ResourcesState : Serialization.ObjectState
	{
		private Resource resources = new Resource();

		public static ResourcesState Create()
		{
			return new ResourcesState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj as Kingdom).resources.IsZero())
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			resources.Set(kingdom.resources, 1f);
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteRawStr(resources.ToString(), "resources");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			string text = ser.ReadRawStr("resources");
			resources = Resource.Parse(text) ?? resources;
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom obj2 = obj as Kingdom;
			obj2.SetResources(resources, send_state: false);
			obj2.InvalidateIncomes();
		}
	}

	[Serialization.State(16)]
	public class TaxRateState : Serialization.ObjectState
	{
		private int taxRate;

		public static TaxRateState Create()
		{
			return new TaxRateState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).taxLevel != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			taxRate = kingdom.taxLevel;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(taxRate, "taxRate");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			taxRate = ser.Read7BitUInt("taxRate");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).SetTaxRate(taxRate, send_state: false);
		}
	}

	[Serialization.State(17)]
	public class FameState : Serialization.ObjectState
	{
		private float fame;

		public static FameState Create()
		{
			return new FameState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.fameObj != null)
			{
				return kingdom.fameObj.fame_bonus != 0f;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.fameObj == null)
			{
				return false;
			}
			fame = kingdom.fameObj.fame_bonus;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(fame, "fame");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			fame = ser.ReadFloat("fame");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.fameObj != null)
			{
				kingdom.fameObj.fame_bonus = fame;
			}
		}
	}

	[Serialization.State(18)]
	public class RelationsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class KingdomRelationState : Serialization.ObjectSubstate
		{
			public float perm_relationship;

			public float temp_relationship;

			public int stance;

			public float stance_time;

			public float war_time;

			public float peace_time;

			public float alliance_time;

			public float nap_time;

			public float vassalage_time;

			public float trade_time;

			public float marriage_time;

			public float rel_time;

			public float nap_broken_king_death_time;

			public bool nap_k1_king_venerable;

			public bool nap_k2_king_venerable;

			public NID nap_broken_dead_king_kingdom;

			public KingdomRelationState()
			{
			}

			public KingdomRelationState(int idx, KingdomAndKingdomRelation rel)
			{
				substate_index = idx;
				perm_relationship = rel.perm_relationship;
				temp_relationship = rel.temp_relationship;
				stance = (int)rel.stance;
				stance_time = rel.fade_time - rel.stance_time;
				war_time = rel.fade_time - rel.war_time;
				peace_time = rel.fade_time - rel.peace_time;
				alliance_time = rel.fade_time - rel.alliance_time;
				nap_time = rel.fade_time - rel.nap_time;
				vassalage_time = rel.fade_time - rel.vassalage_time;
				trade_time = rel.fade_time - rel.trade_time;
				marriage_time = rel.fade_time - rel.marriage_time;
				rel_time = rel.fade_time - rel.last_rel_change_time;
				nap_broken_king_death_time = rel.fade_time - rel.nap_broken_king_death_time;
				nap_k1_king_venerable = rel.nap_k1_king_venerable;
				nap_k2_king_venerable = rel.nap_k2_king_venerable;
			}

			public static KingdomRelationState Create()
			{
				return new KingdomRelationState();
			}

			public override bool InitFrom(Object obj)
			{
				Kingdom obj2 = obj as Kingdom;
				Kingdom kingdom = obj2.game.GetKingdom(substate_index);
				KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(obj2, kingdom);
				perm_relationship = kingdomAndKingdomRelation.perm_relationship;
				temp_relationship = kingdomAndKingdomRelation.temp_relationship;
				stance = (int)kingdomAndKingdomRelation.stance;
				stance_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.stance_time;
				war_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.war_time;
				peace_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.peace_time;
				alliance_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.alliance_time;
				nap_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.nap_time;
				vassalage_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.vassalage_time;
				trade_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.trade_time;
				marriage_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.marriage_time;
				rel_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.last_rel_change_time;
				nap_broken_king_death_time = kingdomAndKingdomRelation.fade_time - kingdomAndKingdomRelation.nap_broken_king_death_time;
				nap_broken_dead_king_kingdom = kingdomAndKingdomRelation.nap_broken_dead_king_kingdom;
				nap_k1_king_venerable = kingdomAndKingdomRelation.nap_k1_king_venerable;
				nap_k2_king_venerable = kingdomAndKingdomRelation.nap_k2_king_venerable;
				return !kingdomAndKingdomRelation.is_default;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(stance, "stance");
				ser.WriteFloat(stance_time, "stance_time");
				ser.WriteFloat(war_time, "war_time");
				ser.WriteFloat(peace_time, "peace_time");
				ser.WriteFloat(alliance_time, "alliance_time");
				ser.WriteFloat(nap_time, "nap_time");
				ser.WriteFloat(vassalage_time, "vassalage_time");
				ser.WriteFloat(trade_time, "trade_time");
				ser.WriteFloat(marriage_time, "marriage_time");
				ser.WriteFloat(rel_time, "rel_time");
				ser.WriteFloat(perm_relationship, "perm_relationship");
				ser.WriteFloat(temp_relationship, "temp_relationship");
				ser.WriteBool(nap_k1_king_venerable, "nap_k1_king_venerable");
				ser.WriteBool(nap_k2_king_venerable, "nap_k2_king_venerable");
				ser.WriteNID<Kingdom>(nap_broken_dead_king_kingdom, "nap_broken_dead_king_kingdom");
				ser.WriteFloat(nap_broken_king_death_time, "nap_broken_king_death_time");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				stance = ser.Read7BitUInt("stance");
				stance_time = ser.ReadFloat("stance_time");
				war_time = ser.ReadFloat("war_time");
				peace_time = ser.ReadFloat("peace_time");
				alliance_time = ser.ReadFloat("alliance_time");
				nap_time = ser.ReadFloat("nap_time");
				vassalage_time = ser.ReadFloat("vassalage_time");
				trade_time = ser.ReadFloat("trade_time");
				marriage_time = ser.ReadFloat("marriage_time");
				rel_time = ser.ReadFloat("rel_time");
				perm_relationship = ser.ReadFloat("perm_relationship");
				temp_relationship = ser.ReadFloat("temp_relationship");
				nap_k1_king_venerable = ser.ReadBool("nap_k1_king_venerable");
				nap_k2_king_venerable = ser.ReadBool("nap_k2_king_venerable");
				nap_broken_dead_king_kingdom = ser.ReadNID<Kingdom>("nap_broken_dead_king_kingdom");
				nap_broken_king_death_time = ser.ReadFloat("nap_broken_king_death_time");
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				Kingdom kingdom2 = kingdom.game.GetKingdom(substate_index);
				KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2, calc_fade: false, create_if_not_found: true);
				RelationUtils.Stance num = kingdomAndKingdomRelation.stance;
				Time time = kingdom.game.time;
				kingdomAndKingdomRelation.perm_relationship = perm_relationship;
				kingdomAndKingdomRelation.temp_relationship = temp_relationship;
				kingdomAndKingdomRelation.stance = (RelationUtils.Stance)stance;
				kingdomAndKingdomRelation.stance_time = time - stance_time;
				kingdomAndKingdomRelation.war_time = time - war_time;
				kingdomAndKingdomRelation.peace_time = time - peace_time;
				kingdomAndKingdomRelation.alliance_time = time - alliance_time;
				kingdomAndKingdomRelation.nap_time = time - nap_time;
				kingdomAndKingdomRelation.vassalage_time = time - vassalage_time;
				kingdomAndKingdomRelation.trade_time = time - trade_time;
				kingdomAndKingdomRelation.marriage_time = time - marriage_time;
				kingdomAndKingdomRelation.last_rel_change_time = time - rel_time;
				kingdomAndKingdomRelation.fade_time = time;
				kingdomAndKingdomRelation.nap_broken_dead_king_kingdom = nap_broken_dead_king_kingdom.Get<Kingdom>(obj.game);
				kingdomAndKingdomRelation.nap_broken_king_death_time = time - nap_broken_king_death_time;
				kingdomAndKingdomRelation.nap_k1_king_venerable = nap_k1_king_venerable;
				kingdomAndKingdomRelation.nap_k2_king_venerable = nap_k2_king_venerable;
				kingdomAndKingdomRelation.OnChanged(kingdom, kingdom2, send_state: false);
				if (num != kingdomAndKingdomRelation.stance)
				{
					kingdom.NotifyStanceChanged(kingdom2);
				}
			}
		}

		[Serialization.Substate(2)]
		public class CommonPactsState : Serialization.ObjectSubstate
		{
			public List<NID> pacts = new List<NID>();

			public CommonPactsState()
			{
			}

			public CommonPactsState(int idx, KingdomAndKingdomRelation rel)
			{
				substate_index = idx;
			}

			public static CommonPactsState Create()
			{
				return new CommonPactsState();
			}

			public override bool InitFrom(Object obj)
			{
				Kingdom obj2 = obj as Kingdom;
				Kingdom kingdom = obj2.game.GetKingdom(substate_index);
				KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(obj2, kingdom);
				if (kingdomAndKingdomRelation.commonPacts != null)
				{
					for (int i = 0; i < kingdomAndKingdomRelation.commonPacts.Count; i++)
					{
						pacts.Add(kingdomAndKingdomRelation.commonPacts[i]);
					}
				}
				return pacts.Count > 0;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(pacts.Count, "count");
				for (int i = 0; i < pacts.Count; i++)
				{
					ser.WriteNID<Pact>(pacts[i], "pact_", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				int num = ser.Read7BitUInt("count");
				for (int i = 0; i < num; i++)
				{
					pacts.Add(ser.ReadNID<Pact>("pact_", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				Kingdom kingdom2 = kingdom.game.GetKingdom(substate_index);
				KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(kingdom, kingdom2, calc_fade: false, create_if_not_found: true);
				if (kingdomAndKingdomRelation.commonPacts == null)
				{
					kingdomAndKingdomRelation.commonPacts = new List<Pact>();
				}
				else
				{
					kingdomAndKingdomRelation.commonPacts.Clear();
				}
				for (int i = 0; i < pacts.Count; i++)
				{
					kingdomAndKingdomRelation.commonPacts.Add(pacts[i].Get<Pact>(obj.game));
				}
				kingdomAndKingdomRelation.OnChanged(kingdom, kingdom2, send_state: false);
			}
		}

		public static RelationsState Create()
		{
			return new RelationsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.relations != null)
			{
				return kingdom.relations.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.relations == null || kingdom.relations.Count == 0)
			{
				return false;
			}
			bool result = false;
			for (int i = 0; i < kingdom.id - 1; i++)
			{
				int num = i + 1;
				KingdomAndKingdomRelation kingdomAndKingdomRelation = kingdom.relations[i];
				if (kingdomAndKingdomRelation != null)
				{
					kingdomAndKingdomRelation.CalcFadeWithTime(kingdom, kingdom.game.GetKingdom(num));
					if (!kingdomAndKingdomRelation.is_default)
					{
						AddSubstate(new KingdomRelationState(num, kingdomAndKingdomRelation));
						AddSubstate(new CommonPactsState(num, kingdomAndKingdomRelation));
						result = true;
					}
				}
			}
			return result;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.relations != null)
			{
				kingdom.relations.Clear();
				KingdomAndKingdomRelation.InitRelations(kingdom);
			}
		}
	}

	[Serialization.State(19)]
	public class AdoptedIdeasState : Serialization.ObjectState
	{
		private const int num_advantages = 5;

		private const int num_ideas = 2;

		private const int count = 10;

		private string[] ideas = new string[10];

		public static AdoptedIdeasState Create()
		{
			return new AdoptedIdeasState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			return false;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			for (int i = 0; i < 10; i++)
			{
				string val = ideas[i];
				ser.WriteStr(val, "idea", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			for (int i = 0; i < 10; i++)
			{
				string text = ser.ReadStr("idea", i);
				ideas[i] = text;
			}
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.State(20)]
	public class AdoptingIdeaState : Serialization.ObjectState
	{
		private string idea;

		private string advantage;

		private int slot;

		private float elapsed;

		public static AdoptingIdeaState Create()
		{
			return new AdoptingIdeaState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			return false;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(idea, "idea");
			if (!string.IsNullOrEmpty(idea))
			{
				ser.WriteStr(advantage, "advantage");
				ser.Write7BitUInt(slot, "slot");
				ser.WriteFloat(elapsed, "elapsed");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			idea = ser.ReadStr("idea");
			if (!string.IsNullOrEmpty(idea))
			{
				advantage = ser.ReadStr("advantage");
				slot = ser.Read7BitUInt("slot");
				elapsed = ser.ReadFloat("elapsed");
			}
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.State(21)]
	public class BooksState : Serialization.ObjectState
	{
		private struct BookInfo
		{
			public string name;

			public int amount;
		}

		private List<BookInfo> books;

		public static BooksState Create()
		{
			return new BooksState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.books == null || kingdom.books.Count == 0)
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.books == null || kingdom.books.Count == 0)
			{
				return false;
			}
			books = new List<BookInfo>(kingdom.books.Count);
			for (int i = 0; i < kingdom.books.Count; i++)
			{
				Book book = kingdom.books[i];
				books.Add(new BookInfo
				{
					name = book.def.name,
					amount = book.copies
				});
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((books != null) ? books.Count : 0);
			ser.Write7BitUInt(num, "count");
			if (num != 0)
			{
				for (int i = 0; i < num; i++)
				{
					BookInfo bookInfo = books[i];
					ser.WriteStr(bookInfo.name, "name", i);
					ser.Write7BitUInt(bookInfo.amount, "amount", i);
				}
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				books = new List<BookInfo>(num);
				for (int i = 0; i < num; i++)
				{
					string name = ser.ReadStr("name", i);
					int amount = ser.Read7BitUInt("amount", i);
					books.Add(new BookInfo
					{
						name = name,
						amount = amount
					});
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.books == null)
			{
				kingdom.books = new List<Book>(books.Count);
			}
			else
			{
				for (int i = 0; i < kingdom.books.Count; i++)
				{
					kingdom.books[i].Destroy();
				}
				kingdom.books.Clear();
			}
			for (int j = 0; j < books.Count; j++)
			{
				BookInfo bookInfo = books[j];
				Book.Def def = kingdom.game.defs.Get<Book.Def>(bookInfo.name);
				kingdom.AddBook(def, bookInfo.amount, from_state: true);
			}
			kingdom.NotifyListeners("books_changed");
		}
	}

	[Serialization.State(22)]
	public class ReligionState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class PatriarchBonusesState : Serialization.ObjectSubstate
		{
			public int patriarch_bonuses_count;

			public List<string> patriarch_bonuses = new List<string>();

			public static PatriarchBonusesState Create()
			{
				return new PatriarchBonusesState();
			}

			public PatriarchBonusesState()
			{
			}

			public PatriarchBonusesState(Kingdom k)
			{
				InitFrom(k);
			}

			public static bool IsNeeded(Object obj)
			{
				return (obj as Kingdom).patriarch_bonuses != null;
			}

			public override bool InitFrom(Object obj)
			{
				substate_index = 0;
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.patriarch_bonuses == null)
				{
					return false;
				}
				patriarch_bonuses_count = kingdom.patriarch_bonuses.Count;
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					patriarch_bonuses.Add(kingdom.patriarch_bonuses[i].field.Path());
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitUInt(patriarch_bonuses_count, "count");
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					ser.WriteStr(patriarch_bonuses[i], "bonus_def_", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				patriarch_bonuses_count = ser.Read7BitUInt("count");
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					patriarch_bonuses.Add(ser.ReadStr("bonus_def_", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				Religion.Def def = kingdom.religion.def;
				kingdom.patriarch_bonuses?.Clear();
				kingdom.game?.religions?.orthodox?.DelPatriarchModifiers(kingdom);
				if (patriarch_bonuses.Count > 0 && kingdom.patriarch_bonuses == null)
				{
					kingdom.patriarch_bonuses = new List<Religion.CharacterBonus>(patriarch_bonuses_count);
				}
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					DT.Field field = obj.game.dt.Find(patriarch_bonuses[i]);
					Religion.CharacterBonus characterBonus = new Religion.CharacterBonus
					{
						field = field
					};
					Stats.Def stats_def = obj.game.defs.Get<Stats.Def>("KingdomStats");
					def.LoadMods(obj.game, field, characterBonus.mods, stats_def);
					kingdom.patriarch_bonuses.Add(characterBonus);
				}
				kingdom.game?.religions?.orthodox?.AddPatriarchModifiers(kingdom);
			}
		}

		[Serialization.Substate(2)]
		public class PatriarchCandidatesState : Serialization.ObjectSubstate
		{
			public NID cleric;

			public bool generated;

			public int patriarch_bonuses_count;

			public List<string> patriarch_bonuses = new List<string>();

			public static PatriarchCandidatesState Create()
			{
				return new PatriarchCandidatesState();
			}

			public PatriarchCandidatesState()
			{
			}

			public PatriarchCandidatesState(Kingdom k, int substate_index)
			{
				base.substate_index = substate_index;
				InitFrom(k);
			}

			public static bool IsNeeded(Object obj)
			{
				return (obj as Kingdom).patriarch_candidates != null;
			}

			public override bool InitFrom(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.patriarch_candidates == null)
				{
					return false;
				}
				if (substate_index < 0 || substate_index >= kingdom.patriarch_candidates.Count)
				{
					return false;
				}
				Orthodox.PatriarchCandidate patriarchCandidate = kingdom.patriarch_candidates[substate_index];
				cleric = patriarchCandidate.cleric;
				generated = patriarchCandidate.generated;
				if (patriarchCandidate.bonuses != null)
				{
					patriarch_bonuses_count = patriarchCandidate.bonuses.Count;
					for (int i = 0; i < patriarch_bonuses_count; i++)
					{
						patriarch_bonuses.Add(patriarchCandidate.bonuses[i].field.Path());
					}
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteNID<Character>(cleric, "cleric");
				ser.WriteBool(generated, "generated");
				ser.Write7BitUInt(patriarch_bonuses_count, "count");
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					ser.WriteStr(patriarch_bonuses[i], "bonus_def_", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				cleric = ser.ReadNID<Character>("cleric");
				generated = ser.ReadBool("generated");
				patriarch_bonuses_count = ser.Read7BitUInt("count");
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					patriarch_bonuses.Add(ser.ReadStr("bonus_def_", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				Orthodox.PatriarchCandidate patriarchCandidate = new Orthodox.PatriarchCandidate
				{
					cleric = cleric.Get<Character>(obj.game),
					generated = generated,
					bonuses = ((patriarch_bonuses_count > 0) ? new List<Religion.CharacterBonus>(patriarch_bonuses_count) : null)
				};
				Religion.Def def = obj.game.religions.orthodox.def;
				for (int i = 0; i < patriarch_bonuses_count; i++)
				{
					DT.Field field = obj.game.dt.Find(patriarch_bonuses[i]);
					Religion.CharacterBonus characterBonus = new Religion.CharacterBonus
					{
						field = field
					};
					Stats.Def stats_def = obj.game.defs.Get<Stats.Def>("KingdomStats");
					def.LoadMods(obj.game, field, characterBonus.mods, stats_def);
					patriarchCandidate.bonuses.Add(characterBonus);
				}
				if (kingdom.patriarch_candidates == null)
				{
					kingdom.patriarch_candidates = new List<Orthodox.PatriarchCandidate>();
				}
				while (kingdom.patriarch_candidates.Count <= substate_index)
				{
					kingdom.patriarch_candidates.Add(null);
				}
				kingdom.patriarch_candidates[substate_index] = patriarchCandidate;
				kingdom.NotifyListeners("patriarch_candidates_changed");
			}
		}

		[Serialization.Substate(3)]
		public class PaganBeliefsState : Serialization.ObjectSubstate
		{
			public List<string> pagan_beliefs = new List<string>();

			public static PaganBeliefsState Create()
			{
				return new PaganBeliefsState();
			}

			public PaganBeliefsState()
			{
			}

			public PaganBeliefsState(Kingdom k)
			{
				InitFrom(k);
			}

			public static bool IsNeeded(Object obj)
			{
				if ((obj as Kingdom)?.pagan_beliefs == null)
				{
					return false;
				}
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				substate_index = 1;
				Kingdom kingdom = obj as Kingdom;
				if (kingdom.pagan_beliefs == null)
				{
					return false;
				}
				for (int i = 0; i < kingdom.pagan_beliefs.Count; i++)
				{
					if (kingdom.pagan_beliefs[i] != null)
					{
						pagan_beliefs.Add(kingdom.pagan_beliefs[i].name);
					}
				}
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				int count = pagan_beliefs.Count;
				ser.Write7BitUInt(count, "count");
				for (int i = 0; i < count; i++)
				{
					ser.WriteStr(pagan_beliefs[i], "tradition_def_", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				int num = ser.Read7BitUInt("count");
				for (int i = 0; i < num; i++)
				{
					pagan_beliefs.Add(ser.ReadStr("tradition_def_", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				Kingdom kingdom = obj as Kingdom;
				kingdom.pagan_beliefs = new List<Religion.PaganBelief>(pagan_beliefs.Count);
				Religion.Def def = kingdom.game.religions.pagan.def;
				int count = pagan_beliefs.Count;
				for (int i = 0; i < count; i++)
				{
					Religion.PaganBelief item = def.FindPaganBelief(pagan_beliefs[i]);
					kingdom.pagan_beliefs.Add(item);
				}
				if (kingdom.is_pagan)
				{
					kingdom.religion.DelModifiers(kingdom);
					kingdom.religion.AddModifiers(kingdom);
				}
				kingdom.NotifyListeners("religion_changed");
			}
		}

		public string religion_def_id;

		public NID patriarch = null;

		private NID patriarch_castle = null;

		private int patriarch_slot = -1;

		public float time_of_excommunication = -1f;

		public bool subordinated = true;

		public bool caliphate;

		public int jihad_target_kid;

		public int jihad_attacker_kid;

		public NID jihad;

		public static ReligionState Create()
		{
			return new ReligionState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.religion != null)
			{
				if (kingdom.def == null)
				{
					return true;
				}
				string text = kingdom.def.GetString("religion");
				if (kingdom.religion.name != text)
				{
					return true;
				}
			}
			if (kingdom.is_catholic && kingdom.excommunicated)
			{
				return true;
			}
			if (kingdom.is_orthodox && !kingdom.subordinated)
			{
				return true;
			}
			if (kingdom.patriarch != null)
			{
				return true;
			}
			if (kingdom.caliphate && kingdom.is_muslim)
			{
				return true;
			}
			if (kingdom.jihad_attacker != null || kingdom.jihad_target != null || kingdom.jihad != null)
			{
				return true;
			}
			if (kingdom.pagan_beliefs != null)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.religion != null)
			{
				religion_def_id = kingdom.religion.def.id;
			}
			else
			{
				religion_def_id = "";
			}
			patriarch = kingdom.patriarch;
			patriarch_castle = kingdom.patriarch_castle;
			time_of_excommunication = kingdom.time_of_excommunication;
			caliphate = kingdom.caliphate;
			subordinated = kingdom.subordinated;
			jihad_target_kid = ((kingdom.jihad_target != null) ? kingdom.jihad_target.id : 0);
			jihad_attacker_kid = ((kingdom.jihad_attacker != null) ? kingdom.jihad_attacker.id : 0);
			jihad = kingdom.jihad;
			if (PatriarchBonusesState.IsNeeded(kingdom))
			{
				AddSubstate(new PatriarchBonusesState(kingdom));
			}
			if (PaganBeliefsState.IsNeeded(kingdom))
			{
				AddSubstate(new PaganBeliefsState(kingdom));
			}
			if (kingdom.patriarch_candidates != null)
			{
				for (int i = 0; i < kingdom.patriarch_candidates.Count; i++)
				{
					AddSubstate(new PatriarchCandidatesState(kingdom, i));
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(religion_def_id, "religion_def");
			ser.WriteNID<Character>(patriarch, "patriarch");
			if (religion_def_id == "Catholic")
			{
				ser.WriteFloat(time_of_excommunication, "time_of_excommunication");
			}
			if (religion_def_id == "Orthodox")
			{
				ser.WriteBool(subordinated, "subordinated");
				if (!subordinated && patriarch.nid == 0)
				{
					ser.WriteNID<Castle>(patriarch_castle, "patriarch_castle");
					ser.Write7BitSigned(patriarch_slot, "patriarch_slot");
				}
			}
			else if (religion_def_id == "Sunni" || religion_def_id == "Shia")
			{
				ser.WriteBool(caliphate, "caliphate");
				ser.Write7BitUInt(jihad_target_kid, "jihad_target_kid");
			}
			ser.WriteNID<War>(jihad, "jihad");
			ser.Write7BitUInt(jihad_attacker_kid, "jihad_attacker_kid");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			religion_def_id = ser.ReadStr("religion_def");
			patriarch = ser.ReadNID<Character>("patriarch");
			if (religion_def_id == "Catholic")
			{
				time_of_excommunication = ser.ReadFloat("time_of_excommunication");
			}
			if (religion_def_id == "Orthodox")
			{
				subordinated = ser.ReadBool("subordinated");
				if (!subordinated && patriarch.nid == 0)
				{
					patriarch_castle = ser.ReadNID<Castle>("patriarch_castle");
					patriarch_slot = ser.Read7BitSigned("patriarch_slot");
				}
			}
			else if (religion_def_id == "Sunni" || religion_def_id == "Shia")
			{
				caliphate = ser.ReadBool("caliphate");
				jihad_target_kid = ser.Read7BitUInt("jihad_target_kid");
			}
			jihad = ser.ReadNID<War>("jihad");
			jihad_attacker_kid = ser.Read7BitUInt("jihad_attacker_kid");
		}

		public override void ApplyTo(Object obj)
		{
			Religion religion = null;
			Kingdom kingdom = obj as Kingdom;
			if (religion_def_id != null && religion_def_id != "")
			{
				Religion.Def def = obj.game.defs.Find<Religion.Def>(religion_def_id);
				religion = obj.game.religions.Get(def);
			}
			kingdom.time_of_excommunication = time_of_excommunication;
			kingdom.subordinated = subordinated;
			kingdom.caliphate = caliphate;
			Religion religion2 = kingdom.religion;
			kingdom.SetReligion(religion, send_state: false);
			if (religion2 == religion && (kingdom.is_muslim || kingdom.is_orthodox))
			{
				Religion.RefreshModifiers(kingdom);
			}
			kingdom.patriarch = patriarch.Get<Character>(obj.game);
			kingdom.patriarch_castle = patriarch_castle.Get<Castle>(obj.game);
			kingdom.jihad_target = obj.game.GetKingdom(jihad_target_kid);
			kingdom.jihad_attacker = obj.game.GetKingdom(jihad_attacker_kid);
			kingdom.jihad = jihad.Get<War>(obj.game);
			kingdom.patriarch_bonuses = null;
			kingdom.patriarch_candidates = null;
			kingdom.pagan_beliefs = null;
		}
	}

	[Serialization.State(23)]
	public class CrownAuthorityState : Serialization.ObjectState
	{
		public int auth;

		public static CrownAuthorityState Create()
		{
			return new CrownAuthorityState();
		}

		public static bool IsNeeded(Object obj)
		{
			CrownAuthority component = (obj as Kingdom).GetComponent<CrownAuthority>();
			if (component == null)
			{
				return false;
			}
			int num = 0;
			return component.GetValue() != num;
		}

		public override bool InitFrom(Object obj)
		{
			CrownAuthority component = (obj as Kingdom).GetComponent<CrownAuthority>();
			if (component == null)
			{
				return false;
			}
			auth = component.GetValue();
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitSigned(auth, "crown_authority");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			auth = ser.Read7BitSigned("crown_authority");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			CrownAuthority crownAuthority = kingdom.GetComponent<CrownAuthority>();
			if (crownAuthority == null)
			{
				crownAuthority = new CrownAuthority(kingdom);
			}
			crownAuthority.SetValue(auth, send_state: false);
		}
	}

	[Serialization.State(24)]
	public class VassalState : Serialization.ObjectState
	{
		public List<int> vassal_ids = new List<int>();

		public static VassalState Create()
		{
			return new VassalState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.vassalStates.Count; i++)
			{
				vassal_ids.Add(kingdom.vassalStates[i].id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(vassal_ids.Count, "count");
			for (int i = 0; i < vassal_ids.Count; i++)
			{
				ser.Write7BitUInt(vassal_ids[i], "vassal_ids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				vassal_ids.Add(ser.Read7BitUInt("vassal_ids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			bool flag = false;
			Kingdom kingdom = obj as Kingdom;
			for (int num = kingdom.vassalStates.Count - 1; num >= 0; num--)
			{
				if (!vassal_ids.Contains(kingdom.vassalStates[num].id))
				{
					kingdom.DelVassalState(kingdom.vassalStates[num], set_sovereign: true, send_state: false);
					flag = true;
				}
			}
			for (int i = 0; i < vassal_ids.Count; i++)
			{
				bool flag2 = false;
				for (int j = 0; j < kingdom.vassalStates.Count; j++)
				{
					if (kingdom.vassalStates[j].id == vassal_ids[i])
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					kingdom.AddVassalState(kingdom.game.GetKingdom(vassal_ids[i]), set_sovereign: false, send_state: false);
					flag = true;
				}
			}
			if (kingdom.started && flag)
			{
				kingdom.NotifyListeners("vassals_changed");
			}
		}
	}

	[Serialization.State(25)]
	public class TradeRoutesState : Serialization.ObjectState
	{
		public List<int> kingdom_ids = new List<int>();

		public static TradeRoutesState Create()
		{
			return new TradeRoutesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.tradeRouteWith.Count; i++)
			{
				kingdom_ids.Add(kingdom.tradeRouteWith[i].id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_ids.Count, "count");
			for (int i = 0; i < kingdom_ids.Count; i++)
			{
				ser.Write7BitUInt(kingdom_ids[i], "kingdom_ids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				kingdom_ids.Add(ser.Read7BitUInt("kingdom_ids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.tradeRouteWith.Clear();
			for (int i = 0; i < kingdom_ids.Count; i++)
			{
				kingdom.tradeRouteWith.Add(kingdom.game.GetKingdom(kingdom_ids[i]));
			}
		}
	}

	[Serialization.State(26)]
	public class TradeAgreementsState : Serialization.ObjectState
	{
		public List<int> kingdom_ids_normal = new List<int>();

		public static TradeAgreementsState Create()
		{
			return new TradeAgreementsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.tradeAgreementsWith.Count; i++)
			{
				kingdom_ids_normal.Add(kingdom.tradeAgreementsWith[i].id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_ids_normal.Count, "count_normal");
			for (int i = 0; i < kingdom_ids_normal.Count; i++)
			{
				ser.Write7BitUInt(kingdom_ids_normal[i], "kingdom_ids_normal_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count_normal");
			for (int i = 0; i < num; i++)
			{
				kingdom_ids_normal.Add(ser.Read7BitUInt("kingdom_ids_normal_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.tradeAgreementsWith.Clear();
			for (int i = 0; i < kingdom_ids_normal.Count; i++)
			{
				kingdom.tradeAgreementsWith.Add(kingdom.game.GetKingdom(kingdom_ids_normal[i]));
			}
		}
	}

	[Serialization.State(27)]
	public class MarriageStates : Serialization.ObjectState
	{
		public List<NID> marriage_nids = new List<NID>();

		public static MarriageStates Create()
		{
			return new MarriageStates();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.marriages.Count; i++)
			{
				marriage_nids.Add(kingdom.marriages[i]);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(marriage_nids.Count, "count");
			for (int i = 0; i < marriage_nids.Count; i++)
			{
				ser.WriteNID<Marriage>(marriage_nids[i], "marriage_nids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				marriage_nids.Add(ser.ReadNID<Marriage>("marriage_nids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.marriages.Clear();
			for (int i = 0; i < marriage_nids.Count; i++)
			{
				kingdom.marriages.Add(marriage_nids[i].Get<Marriage>(kingdom.game));
			}
		}
	}

	[Serialization.State(28)]
	public class TraditionsState : Serialization.ObjectState
	{
		public List<string> traditions;

		public List<int> ranks;

		public static TraditionsState Create()
		{
			return new TraditionsState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return false;
			}
			return kingdom.NumTraditions() > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.traditions == null)
			{
				return false;
			}
			traditions = new List<string>(kingdom.traditions.Count);
			ranks = new List<int>(kingdom.traditions.Count);
			for (int i = 0; i < kingdom.traditions.Count; i++)
			{
				Tradition tradition = kingdom.traditions[i];
				string item = tradition?.def?.id ?? "";
				traditions.Add(item);
				ranks.Add(tradition?.rank ?? 0);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((traditions != null) ? traditions.Count : 0);
			ser.Write7BitUInt(num, "count");
			for (int i = 0; i < num; i++)
			{
				string val = traditions[i];
				int val2 = ranks[i];
				ser.WriteStr(val, "tradition_", i);
				ser.Write7BitUInt(val2, "rank_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				traditions = new List<string>(num);
				ranks = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					string item = ser.ReadStr("tradition_", i);
					int item2 = ser.Read7BitUInt("rank_", i);
					traditions.Add(item);
					ranks.Add(item2);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return;
			}
			kingdom.ClearTraditions();
			if (traditions != null)
			{
				for (int i = 0; i < traditions.Count; i++)
				{
					string value = traditions[i];
					int rank = ranks[i];
					Tradition.Def tdef = (string.IsNullOrEmpty(value) ? null : kingdom.game.defs.Find<Tradition.Def>(value));
					kingdom.SetTradition(i, tdef, rank, refresh: false);
				}
			}
			kingdom.RefreshTraditions(send_state: false);
		}
	}

	[Serialization.State(30)]
	public class OccupiedRealmsState : Serialization.ObjectState
	{
		private List<NID> occupiedRealms = new List<NID>();

		private int count;

		public static OccupiedRealmsState Create()
		{
			return new OccupiedRealmsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.occupiedRealms != null)
			{
				return kingdom.occupiedRealms.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			count = kingdom.occupiedRealms.Count;
			for (int i = 0; i < count; i++)
			{
				NID item = kingdom.occupiedRealms[i];
				occupiedRealms.Add(item);
			}
			return count > 0;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < count; i++)
			{
				ser.WriteNID<Realm>(occupiedRealms[i], "occupied_realm_nid_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			count = ser.Read7BitUInt("count");
			for (int i = 0; i < count; i++)
			{
				NID item = ser.ReadNID<Realm>("occupied_realm_nid_", i);
				occupiedRealms.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.occupiedRealms.Clear();
			for (int i = 0; i < count; i++)
			{
				kingdom.occupiedRealms.Add(occupiedRealms[i].Get<Realm>(obj.game));
			}
		}
	}

	[Serialization.State(31)]
	public class UsedNamesState : Serialization.ObjectState
	{
		public struct UsedName
		{
			public string name;

			public int idx;
		}

		public List<UsedName> used_names = new List<UsedName>();

		public static UsedNamesState Create()
		{
			return new UsedNamesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.royalFamily == null || kingdom.royalFamily.used_names == null)
			{
				return false;
			}
			foreach (KeyValuePair<string, int> used_name in kingdom.royalFamily.used_names)
			{
				used_names.Add(new UsedName
				{
					name = used_name.Key,
					idx = used_name.Value
				});
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int count = used_names.Count;
			ser.Write7BitUInt(count, "count");
			for (int i = 0; i < used_names.Count; i++)
			{
				ser.WriteStr(used_names[i].name, "name_", i);
				ser.Write7BitUInt(used_names[i].idx, "idx_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				UsedName item = new UsedName
				{
					name = ser.ReadStr("name_", i),
					idx = ser.Read7BitUInt("idx_", i)
				};
				used_names.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.royalFamily != null && kingdom.royalFamily.used_names != null)
			{
				if (kingdom.royalFamily.used_names != null)
				{
					kingdom.royalFamily.used_names.Clear();
				}
				else
				{
					kingdom.royalFamily.used_names = new Dictionary<string, int>();
				}
				for (int i = 0; i < used_names.Count; i++)
				{
					kingdom.royalFamily.used_names[used_names[i].name] = used_names[i].idx;
				}
			}
		}
	}

	[Serialization.State(32)]
	public class PretenderState : Serialization.ObjectState
	{
		private NID pretender;

		private NID loyal_to;

		private bool was_puppet;

		public static PretenderState Create()
		{
			return new PretenderState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom)?.royalFamily?.crownPretender != null;
		}

		public override bool InitFrom(Object obj)
		{
			RoyalFamily.Pretender pretender = (obj as Kingdom)?.royalFamily?.crownPretender;
			if (pretender == null)
			{
				return false;
			}
			this.pretender = pretender.pretender;
			loyal_to = pretender.loyal_to;
			was_puppet = pretender.was_puppet;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(pretender, "pretender");
			ser.WriteNID<Kingdom>(loyal_to, "loyal_to");
			ser.WriteBool(was_puppet, "was_puppet");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			pretender = ser.ReadNID<Character>("pretender");
			loyal_to = ser.ReadNID<Kingdom>("loyal_to");
			was_puppet = ser.ReadBool("was_puppet");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.royalFamily != null)
			{
				Character c = pretender.Get<Character>(obj.game);
				Kingdom kingdom2 = loyal_to.Get<Kingdom>(obj.game);
				kingdom.royalFamily.SetPretender(c, kingdom2, was_puppet, send_state: false);
			}
		}
	}

	[Serialization.State(33)]
	public class OpinionsState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class OpinionState : Serialization.ObjectSubstate
		{
			private Data data;

			public OpinionState()
			{
			}

			public OpinionState(int idx, Opinion opinion)
			{
				substate_index = idx;
				data = Data.CreateFull(opinion);
			}

			public static OpinionState Create()
			{
				return new OpinionState();
			}

			public override bool InitFrom(Object obj)
			{
				List<Opinion> list = (obj as Kingdom)?.opinions?.opinions;
				if (list == null || list.Count <= substate_index)
				{
					return false;
				}
				Opinion obj2 = list[substate_index];
				data = Data.CreateFull(obj2);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.WriteData(data, "opinion");
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				data = ser.ReadData("opinion");
			}

			public override void ApplyTo(Object obj)
			{
				if (obj is Kingdom kingdom && Data.RestoreObject<Opinion>(data, kingdom.game) == null)
				{
					Game.Log($"Could not load opinon {substate_index}", Game.LogType.Warning);
				}
			}
		}

		public static OpinionsState Create()
		{
			return new OpinionsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.opinions?.opinions != null)
			{
				return kingdom.opinions.opinions.Count > 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.opinions?.opinions == null || kingdom.opinions.opinions.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < kingdom.opinions.opinions.Count; i++)
			{
				Opinion opinion = kingdom.opinions.opinions[i];
				AddSubstate(new OpinionState(i, opinion));
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
		}
	}

	[Serialization.State(34)]
	public class SovereignState : Serialization.ObjectState
	{
		private NID sovereign;

		private int vassalage_type;

		public static SovereignState Create()
		{
			return new SovereignState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom)?.sovereignState != null;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return false;
			}
			sovereign = kingdom.sovereignState;
			if (kingdom.vassalage != null)
			{
				vassalage_type = (int)kingdom.vassalage.def.type;
			}
			else
			{
				vassalage_type = 0;
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Kingdom>(sovereign, "sovereign");
			ser.Write7BitUInt(vassalage_type, "vassalage");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			sovereign = ser.ReadNID<Kingdom>("sovereign");
			if (Serialization.cur_version >= 19)
			{
				vassalage_type = ser.Read7BitUInt("vassalage");
			}
			else
			{
				vassalage_type = 1;
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Kingdom kingdom)
			{
				Kingdom kingdom2 = sovereign.Get<Kingdom>(obj.game);
				if (kingdom.sovereignState != kingdom2)
				{
					kingdom.SetSovereignState(kingdom2, (Vassalage.Type)vassalage_type, set_vassal: false, send_state: false);
				}
				else
				{
					kingdom.ChangeVassalageType((Vassalage.Type)vassalage_type, send_state: false);
				}
			}
		}
	}

	[Serialization.State(35)]
	public class ImproveOpinionsDiplomatState : Serialization.ObjectState
	{
		private NID diplomat;

		public static ImproveOpinionsDiplomatState Create()
		{
			return new ImproveOpinionsDiplomatState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom)?.improveOpinionsDiplomat != null;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return false;
			}
			diplomat = kingdom.improveOpinionsDiplomat;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(diplomat, "diplomat");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			diplomat = ser.ReadNID<Character>("diplomat");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Kingdom kingdom)
			{
				kingdom.improveOpinionsDiplomat = diplomat.Get<Character>(obj.game);
			}
		}
	}

	[Serialization.State(36)]
	public class HelpWithRebelsState : Serialization.ObjectState
	{
		private List<NID> kingdoms = new List<NID>();

		private List<float> times = new List<float>();

		public static HelpWithRebelsState Create()
		{
			return new HelpWithRebelsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.ai != null)
			{
				return kingdom.ai.helpWithRebels.Count != 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Kingdom { ai: not null } kingdom))
			{
				return false;
			}
			for (int i = 0; i < kingdom.ai.helpWithRebels.Count; i++)
			{
				kingdoms.Add(kingdom.ai.helpWithRebels[i].Item1);
				times.Add(kingdom.ai.helpWithRebels[i].Item2 - obj.game.time);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdoms.Count, "count");
			for (int i = 0; i < kingdoms.Count; i++)
			{
				ser.WriteNID<Kingdom>(kingdoms[i], "kingdom", i);
				ser.WriteFloat(times[i], "time", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				kingdoms.Add(ser.ReadNID<Kingdom>("kingdom", i));
				times.Add(ser.ReadFloat("time", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Kingdom { ai: not null } kingdom)
			{
				kingdom.ai.helpWithRebels.Clear();
				for (int i = 0; i < kingdoms.Count; i++)
				{
					kingdom.AddHelpWithRebelsOf(kingdoms[i].Get<Kingdom>(obj.game), times[i], send_state: false);
				}
			}
		}
	}

	[Serialization.State(37)]
	public class PeaceTimeState : Serialization.ObjectState
	{
		private float peace_time;

		public static PeaceTimeState Create()
		{
			return new PeaceTimeState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).last_peace_time != Time.Zero;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			peace_time = kingdom.game.time - kingdom.last_peace_time;
			return kingdom.last_peace_time != Time.Zero;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(peace_time, "peace_time");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			peace_time = ser.ReadFloat("peace_time");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Kingdom kingdom)
			{
				kingdom.last_peace_time = kingdom.game.time - peace_time;
			}
		}
	}

	[Serialization.State(38)]
	public class AlliesState : Serialization.ObjectState
	{
		public List<int> kingdom_ids = new List<int>();

		public static AlliesState Create()
		{
			return new AlliesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).allies.Count != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.allies.Count; i++)
			{
				kingdom_ids.Add(kingdom.allies[i].id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_ids.Count, "count");
			for (int i = 0; i < kingdom_ids.Count; i++)
			{
				ser.Write7BitUInt(kingdom_ids[i], "kingdom_ids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				kingdom_ids.Add(ser.Read7BitUInt("kingdom_ids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.allies.Clear();
			for (int i = 0; i < kingdom_ids.Count; i++)
			{
				kingdom.allies.Add(kingdom.game.GetKingdom(kingdom_ids[i]));
			}
		}
	}

	[Serialization.State(39)]
	public class NonAgressionsState : Serialization.ObjectState
	{
		public List<int> kingdom_ids = new List<int>();

		public static NonAgressionsState Create()
		{
			return new NonAgressionsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).nonAgressions.Count != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.nonAgressions.Count; i++)
			{
				kingdom_ids.Add(kingdom.nonAgressions[i].id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kingdom_ids.Count, "count");
			for (int i = 0; i < kingdom_ids.Count; i++)
			{
				ser.Write7BitUInt(kingdom_ids[i], "kingdom_ids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			for (int i = 0; i < num; i++)
			{
				kingdom_ids.Add(ser.Read7BitUInt("kingdom_ids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.nonAgressions.Clear();
			for (int i = 0; i < kingdom_ids.Count; i++)
			{
				kingdom.nonAgressions.Add(kingdom.game.GetKingdom(kingdom_ids[i]));
			}
		}
	}

	[Serialization.State(40)]
	public class InheritancePricessesState : Serialization.ObjectState
	{
		private List<NID> princesses = new List<NID>();

		public static InheritancePricessesState Create()
		{
			return new InheritancePricessesState();
		}

		public static bool IsNeeded(Object obj)
		{
			Inheritance inheritance = (obj as Kingdom)?.GetComponent<Inheritance>();
			if (inheritance == null)
			{
				return false;
			}
			return inheritance.princesses.Count > 0;
		}

		public override bool InitFrom(Object obj)
		{
			Inheritance component = (obj as Kingdom).GetComponent<Inheritance>();
			if (component == null)
			{
				return false;
			}
			for (int i = 0; i < component.princesses.Count; i++)
			{
				princesses.Add(component.princesses[i]);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(princesses.Count, "count_princesses");
			for (int i = 0; i < princesses.Count; i++)
			{
				ser.WriteNID<Character>(princesses[i], "princesses_ids_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count_princesses");
			for (int i = 0; i < num; i++)
			{
				princesses.Add(ser.ReadNID<Character>("princesses_ids_", i));
			}
		}

		public override void ApplyTo(Object obj)
		{
			Inheritance component = (obj as Kingdom).GetComponent<Inheritance>();
			if (component != null)
			{
				component.princesses.Clear();
				for (int i = 0; i < princesses.Count; i++)
				{
					component.princesses.Add(princesses[i].Get<Character>(obj.game));
				}
			}
		}
	}

	[Serialization.State(41)]
	public class InheritanceState : Serialization.ObjectState
	{
		private NID currentPrincess = null;

		private NID currentKingdom = null;

		private List<NID> realms = new List<NID>();

		private float timeoutDelta;

		public static InheritanceState Create()
		{
			return new InheritanceState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).GetComponent<Inheritance>()?.currentPrincess != null;
		}

		public override bool InitFrom(Object obj)
		{
			Inheritance component = (obj as Kingdom).GetComponent<Inheritance>();
			currentPrincess = component.currentPrincess;
			currentKingdom = component.currentKingdom;
			for (int i = 0; i < component.realms.Count; i++)
			{
				realms.Add(component.realms[i]);
			}
			timeoutDelta = component.GetExpireTime() - obj.game.time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(currentPrincess, "currentPrincess");
			ser.WriteNID<Kingdom>(currentKingdom, "currentKingdom");
			ser.Write7BitUInt(realms.Count, "count_realms");
			for (int i = 0; i < realms.Count; i++)
			{
				ser.WriteNID<Realm>(realms[i], "realms_ids_", i);
			}
			ser.WriteFloat(timeoutDelta, "timeout_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			currentPrincess = ser.ReadNID<Character>("currentPrincess");
			currentKingdom = ser.ReadNID<Kingdom>("currentKingdom");
			int num = ser.Read7BitUInt("count_realms");
			for (int i = 0; i < num; i++)
			{
				realms.Add(ser.ReadNID<Realm>("realms_ids_", i));
			}
			timeoutDelta = ser.ReadFloat("timeout_delta");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			Inheritance component = kingdom.GetComponent<Inheritance>();
			component.realms.Clear();
			component.currentPrincess = currentPrincess.Get<Character>(obj.game);
			component.currentKingdom = currentKingdom.Get<Kingdom>(obj.game);
			for (int i = 0; i < realms.Count; i++)
			{
				component.realms.Add(realms[i].Get<Realm>(obj.game));
			}
			component.StopUpdating();
			if (timeoutDelta > 0f)
			{
				component.UpdateAfter(timeoutDelta);
			}
			if (component.currentPrincess != null)
			{
				component.currentKingdom.NotifyListeners("princess_inheritance", kingdom);
			}
		}
	}

	[Serialization.State(42)]
	public class PastWarsState : Serialization.ObjectState
	{
		private int wars_won;

		private int wars_lost;

		private float total_past_war_score;

		private int total_past_wars;

		public static PastWarsState Create()
		{
			return new PastWarsState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.wars_won == 0 && kingdom.wars_lost == 0 && kingdom.total_past_war_score == 0f)
			{
				return kingdom.total_past_wars != 0;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			wars_won = kingdom.wars_won;
			wars_lost = kingdom.wars_lost;
			total_past_war_score = kingdom.total_past_war_score;
			total_past_wars = kingdom.total_past_wars;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(wars_won, "wars_won");
			ser.Write7BitUInt(wars_lost, "wars_lost");
			ser.WriteFloat(total_past_war_score, "total_past_lead_score");
			ser.Write7BitUInt(total_past_wars, "total_past_lead_wars");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			wars_won = ser.Read7BitUInt("wars_won");
			wars_lost = ser.Read7BitUInt("wars_lost");
			total_past_war_score = ser.ReadFloat("total_past_lead_score");
			total_past_wars = ser.Read7BitUInt("total_past_lead_wars");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom obj2 = obj as Kingdom;
			obj2.wars_won = wars_won;
			obj2.wars_lost = wars_lost;
			obj2.total_past_war_score = total_past_war_score;
			obj2.total_past_wars = total_past_wars;
		}
	}

	[Serialization.State(43)]
	public class BuildingUpgradesState : Serialization.ObjectState
	{
		private struct Upgrading
		{
			public string id;

			public float elapsed;

			public float duration;
		}

		private List<string> ids;

		private List<string> planned_ids;

		private List<Upgrading> upgrading;

		public static BuildingUpgradesState Create()
		{
			return new BuildingUpgradesState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.building_upgrades == null && kingdom.planned_upgrades == null && (kingdom.upgrading == null || kingdom.upgrading.Count == 0))
			{
				return false;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.building_upgrades != null)
			{
				ids = new List<string>(kingdom.building_upgrades.Count);
				for (int i = 0; i < kingdom.building_upgrades.Count; i++)
				{
					Building.Def def = kingdom.building_upgrades[i];
					ids.Add(def.id);
				}
			}
			if (kingdom.planned_upgrades != null)
			{
				planned_ids = new List<string>(kingdom.planned_upgrades.Count);
				for (int j = 0; j < kingdom.planned_upgrades.Count; j++)
				{
					Building.Def def2 = kingdom.planned_upgrades[j];
					planned_ids.Add(def2.id);
				}
			}
			if (kingdom.upgrading != null && kingdom.upgrading.Count > 0)
			{
				this.upgrading = new List<Upgrading>(kingdom.upgrading.Count);
				for (int k = 0; k < kingdom.upgrading.Count; k++)
				{
					Kingdom.Upgrading upgrading = kingdom.upgrading[k];
					Upgrading item = new Upgrading
					{
						id = upgrading.def.id,
						elapsed = kingdom.game.time - upgrading.start_time,
						duration = upgrading.duration
					};
					this.upgrading.Add(item);
				}
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((ids != null) ? ids.Count : 0);
			ser.Write7BitUInt(num, "count");
			for (int i = 0; i < num; i++)
			{
				string val = ids[i];
				ser.WriteStr(val, "upgrade", i);
			}
			num = ((planned_ids != null) ? planned_ids.Count : 0);
			ser.Write7BitUInt(num, "planned_count");
			for (int j = 0; j < num; j++)
			{
				string val2 = planned_ids[j];
				ser.WriteStr(val2, "planned_upgrade", j);
			}
			num = ((this.upgrading != null) ? this.upgrading.Count : 0);
			ser.Write7BitUInt(num, "upgrading_count");
			for (int k = 0; k < num; k++)
			{
				Upgrading upgrading = this.upgrading[k];
				ser.WriteStr(upgrading.id, "upgrading_id", k);
				ser.WriteFloat(upgrading.elapsed, "upgrading_elapsed", k);
				ser.WriteFloat(upgrading.duration, "upgrading_duration", k);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				ids = new List<string>(num);
				for (int i = 0; i < num; i++)
				{
					string item = ser.ReadStr("upgrade", i);
					ids.Add(item);
				}
			}
			num = ser.Read7BitUInt("planned_count");
			if (num > 0)
			{
				planned_ids = new List<string>(num);
				for (int j = 0; j < num; j++)
				{
					string item2 = ser.ReadStr("planned_upgrade", j);
					planned_ids.Add(item2);
				}
			}
			num = ser.Read7BitUInt("upgrading_count");
			if (num > 0)
			{
				upgrading = new List<Upgrading>(num);
				for (int k = 0; k < num; k++)
				{
					string text = ser.ReadStr("upgrading_id", k);
					float elapsed = ser.ReadFloat("upgrading_elapsed", k);
					float duration = ser.ReadFloat("upgrading_duration", k);
					Upgrading item3 = new Upgrading
					{
						id = text,
						elapsed = elapsed,
						duration = duration
					};
					upgrading.Add(item3);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (ids == null)
			{
				kingdom.building_upgrades = null;
			}
			else
			{
				kingdom.building_upgrades = new List<Building.Def>(ids.Count);
				for (int i = 0; i < ids.Count; i++)
				{
					string text = ids[i];
					Building.Def def = kingdom.game.defs.Find<Building.Def>(text);
					if (def != null)
					{
						kingdom.building_upgrades.Add(def);
					}
				}
			}
			if (planned_ids == null)
			{
				kingdom.planned_upgrades = null;
			}
			else
			{
				kingdom.planned_upgrades = new List<Building.Def>(planned_ids.Count);
				for (int j = 0; j < planned_ids.Count; j++)
				{
					string text2 = planned_ids[j];
					Building.Def def2 = kingdom.game.defs.Find<Building.Def>(text2);
					if (def2 != null)
					{
						kingdom.planned_upgrades.Add(def2);
					}
				}
			}
			List<Kingdom.Upgrading> list = kingdom.upgrading;
			if (this.upgrading == null)
			{
				kingdom.upgrading = null;
			}
			else
			{
				kingdom.upgrading = new List<Kingdom.Upgrading>(this.upgrading.Count);
				for (int k = 0; k < this.upgrading.Count; k++)
				{
					Upgrading upgrading = this.upgrading[k];
					Kingdom.Upgrading ku = new Kingdom.Upgrading
					{
						def = kingdom.game.defs.Find<Building.Def>(upgrading.id),
						start_time = kingdom.game.time - upgrading.elapsed,
						duration = upgrading.duration
					};
					kingdom.upgrading.Add(ku);
					if (list == null || list.FindIndex((Kingdom.Upgrading oku) => oku.def == ku.def) < 0)
					{
						kingdom.NotifyListeners("upgrade_started", ku.def);
					}
				}
			}
			kingdom.RecalcBuildingStates();
			kingdom.NotifyListeners("building_upgrades_changed");
		}
	}

	[Serialization.State(44)]
	public class DiplomacyReasonsState : Serialization.ObjectState
	{
		private List<Data> reasons = new List<Data>();

		public static DiplomacyReasonsState Create()
		{
			return new DiplomacyReasonsState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).diplomacyReasons.Count != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			for (int i = 0; i < kingdom.diplomacyReasons.Count; i++)
			{
				Data data = Data.CreateFull(kingdom.diplomacyReasons[i]);
				if (data == null)
				{
					Game.Log("Could not create reason data from " + kingdom.diplomacyReasons[i], Game.LogType.Error);
				}
				reasons.Add(data);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((reasons != null) ? reasons.Count : 0);
			ser.Write7BitUInt(num, "count");
			for (int i = 0; i < num; i++)
			{
				ser.WriteData(reasons[i], "reason", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count");
			if (num > 0)
			{
				reasons = new List<Data>(num);
				for (int i = 0; i < num; i++)
				{
					Data item = ser.ReadData("reason", i);
					reasons.Add(item);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (reasons == null)
			{
				kingdom.diplomacyReasons = new List<Reason>();
				return;
			}
			kingdom.diplomacyReasons = new List<Reason>(reasons.Count);
			for (int i = 0; i < reasons.Count; i++)
			{
				Data data = reasons[i];
				Reason reason = data.GetObject(obj.game) as Reason;
				if (!data.ApplyTo(reason, obj.game))
				{
					Game.Log("Could not apply " + data.ToString() + " to " + reason.ToString(), Game.LogType.Error);
					break;
				}
				kingdom.diplomacyReasons.Add(reason);
			}
		}
	}

	[Serialization.State(45)]
	public class StabilityState : Serialization.ObjectState
	{
		public float[] categories;

		public static StabilityState Create()
		{
			return new StabilityState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).stability != null;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			categories = new float[kingdom.stability.NumCategories()];
			for (int i = 0; i < categories.Length; i++)
			{
				categories[i] = kingdom.stability.GetStability(i);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(categories.Length, "num_categories");
			for (int i = 0; i < categories.Length; i++)
			{
				ser.WriteFloat(categories[i], "category_", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("num_categories");
			categories = new float[num];
			for (int i = 0; i < categories.Length; i++)
			{
				categories[i] = ser.ReadFloat("category_", i);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			kingdom.stability.value = 0f;
			for (int i = 0; i < categories.Length && i < kingdom.stability.NumCategories(); i++)
			{
				kingdom.stability.SetCategory(categories[i], i);
				kingdom.stability.value += categories[i];
			}
		}
	}

	[Serialization.State(46)]
	public class PrestigeState : Serialization.ObjectState
	{
		private float prestige;

		public static PrestigeState Create()
		{
			return new PrestigeState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.prestigeObj != null)
			{
				return kingdom.prestigeObj.prestige != 0f;
			}
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.prestigeObj == null)
			{
				return false;
			}
			prestige = kingdom.prestigeObj.prestige;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(prestige, "prestige");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			prestige = ser.ReadFloat("prestige");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.prestigeObj != null)
			{
				kingdom.prestigeObj.prestige = prestige;
			}
		}
	}

	[Serialization.State(47)]
	public class ChallengesState : Serialization.ObjectState
	{
		private List<Data> challenges;

		public static ChallengesState Create()
		{
			return new ChallengesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom).challenges.Count != 0;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			challenges = new List<Data>(kingdom.challenges.Count);
			for (int i = 0; i < kingdom.challenges.Count; i++)
			{
				Data item = Data.CreateFull(kingdom.challenges[i]);
				challenges.Add(item);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((challenges != null) ? challenges.Count : 0);
			ser.Write7BitUInt(challenges.Count, "challenges");
			for (int i = 0; i < num; i++)
			{
				Data data = challenges[i];
				ser.WriteData(data, "challenge", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("challenges");
			if (num > 0)
			{
				challenges = new List<Data>(num);
			}
			for (int i = 0; i < num; i++)
			{
				Data item = ser.ReadData("challenge", i);
				challenges.Add(item);
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return;
			}
			for (int i = 0; i < kingdom.challenges.Count; i++)
			{
				kingdom.challenges[i].state = Challenge.State.Invalid;
			}
			if (challenges != null)
			{
				for (int j = 0; j < challenges.Count; j++)
				{
					Data.RestoreObject<Challenge>(challenges[j], kingdom.game);
				}
			}
			for (int num = kingdom.challenges.Count - 1; num >= 0; num--)
			{
				Challenge challenge = kingdom.challenges[num];
				if (challenge.state == Challenge.State.Invalid)
				{
					challenge.Deactivate();
				}
			}
			Challenge.RebindRules(kingdom);
		}
	}

	[Serialization.State(48)]
	public class BankruptcyState : Serialization.ObjectState
	{
		private float lastTimeDelta;

		private float updateTimeDelta;

		public static BankruptcyState Create()
		{
			return new BankruptcyState();
		}

		public static bool IsNeeded(Object obj)
		{
			KingdomBankruptcy kingdomBankruptcy = (obj as Kingdom)?.GetComponent<KingdomBankruptcy>();
			if (kingdomBankruptcy != null)
			{
				if (!(kingdomBankruptcy.lastBankruptcy != Time.Zero))
				{
					return kingdomBankruptcy.IsRegisteredForUpdate();
				}
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			KingdomBankruptcy kingdomBankruptcy = kingdom?.GetComponent<KingdomBankruptcy>();
			if (kingdomBankruptcy == null)
			{
				return false;
			}
			lastTimeDelta = kingdomBankruptcy.lastBankruptcy - kingdom.game.session_time;
			updateTimeDelta = kingdomBankruptcy.GetExpireTime() - obj.game.time;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(lastTimeDelta, "last_time_delta");
			ser.WriteFloat(updateTimeDelta, "update_time_delta");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			lastTimeDelta = ser.ReadFloat("last_time_delta");
			updateTimeDelta = ser.ReadFloat("update_time_delta");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			KingdomBankruptcy kingdomBankruptcy = kingdom?.GetComponent<KingdomBankruptcy>();
			if (kingdomBankruptcy != null)
			{
				kingdomBankruptcy.lastBankruptcy = kingdom.game.session_time + lastTimeDelta;
				if (updateTimeDelta > 0f)
				{
					kingdomBankruptcy.UpdateAfter(updateTimeDelta);
				}
			}
		}
	}

	[Serialization.State(49)]
	public class DefeatedByState : Serialization.ObjectState
	{
		private NID conqueror;

		public static DefeatedByState Create()
		{
			return new DefeatedByState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as Kingdom)?.defeated_by != null;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Kingdom kingdom))
			{
				return false;
			}
			conqueror = kingdom.defeated_by;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Kingdom>(conqueror, "conqueror");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			conqueror = ser.ReadNID<Kingdom>("conqueror");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Kingdom kingdom)
			{
				kingdom.SetDefeatedBy(conqueror.Get<Kingdom>(obj.game), send_state: false);
			}
		}
	}

	[Serialization.State(50)]
	public class FinancesState : Serialization.ObjectState
	{
		public Resource total_earned = new Resource();

		public Resource[] earned_by_category = new Resource[7];

		public Resource total_spent = new Resource();

		public Resource[] spent_by_category = new Resource[7];

		public static FinancesState Create()
		{
			return new FinancesState();
		}

		public static bool IsNeeded(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (!kingdom.total_earned.IsZero())
			{
				return true;
			}
			if (!kingdom.total_spent.IsZero())
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			total_earned.Set(kingdom.total_earned, 1f);
			total_spent.Set(kingdom.total_spent, 1f);
			for (int i = 0; i < 7; i++)
			{
				Resource res = kingdom.earned_by_category[i];
				earned_by_category[i] = new Resource(res);
				Resource res2 = kingdom.spent_by_category[i];
				spent_by_category[i] = new Resource(res2);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteRawStr(total_earned.ToString(), "total_earned");
			ser.WriteRawStr(total_spent.ToString(), "total_spent");
			for (int i = 0; i < 7; i++)
			{
				KingdomAI.Expense.Category category = (KingdomAI.Expense.Category)i;
				string text = category.ToString();
				Resource resource = earned_by_category[i];
				ser.WriteRawStr(resource.ToString(), "earned_" + text);
				Resource resource2 = spent_by_category[i];
				ser.WriteRawStr(resource2.ToString(), "spent_" + text);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			string text = ser.ReadRawStr("total_earned");
			total_earned = Resource.Parse(text);
			text = ser.ReadRawStr("total_spent");
			total_spent = Resource.Parse(text);
			for (int i = 0; i < 7; i++)
			{
				KingdomAI.Expense.Category category = (KingdomAI.Expense.Category)i;
				string text2 = category.ToString();
				text = ser.ReadRawStr("earned_" + text2);
				earned_by_category[i] = Resource.Parse(text);
				text = ser.ReadRawStr("spent_" + text2);
				spent_by_category[i] = Resource.Parse(text);
			}
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			KingdomAI kingdomAI = kingdom?.ai;
			kingdom.total_earned.Set(total_earned, 1f);
			kingdom.total_spent.Set(total_spent, 1f);
			for (int i = 0; i < 7; i++)
			{
				Resource res = earned_by_category[i];
				kingdom.earned_by_category[i] = new Resource(res);
				Resource res2 = spent_by_category[i];
				kingdom.spent_by_category[i] = new Resource(res2);
				if (kingdomAI?.categories != null)
				{
					kingdomAI.categories[i]?.spent?.Set(res2, 1f);
				}
			}
		}
	}

	[Serialization.State(51)]
	public class AIState : Serialization.ObjectState
	{
		public float balance_state_1 = 1f;

		public float balance_state_2 = 1f;

		public static AIState Create()
		{
			return new AIState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			balance_state_1 = kingdom.balance_factor_luck;
			balance_state_2 = kingdom.balance_factor_income;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(balance_state_1, "balance_state_1");
			ser.WriteFloat(balance_state_2, "balance_state_2");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			balance_state_1 = ser.ReadFloat("balance_state_1");
			balance_state_2 = ser.ReadFloat("balance_state_2");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			_ = kingdom?.ai;
			kingdom.balance_factor_luck = balance_state_1;
			kingdom.balance_factor_income = balance_state_2;
			if (obj.game.ai.def.big_bonus_power == kingdom.balance_factor_luck && !obj.game.ai.director.HighlyImprovedAIList.Contains(kingdom))
			{
				obj.game.ai.director.HighlyImprovedAIList.Add(kingdom);
			}
			else if (obj.game.ai.def.bonus_power == kingdom.balance_factor_luck && !obj.game.ai.director.ImprovedAIList.Contains(kingdom))
			{
				obj.game.ai.director.ImprovedAIList.Add(kingdom);
			}
		}
	}

	[Serialization.Event(27)]
	public class SpawnArmyEvent : Serialization.ObjectEvent
	{
		private Point position;

		public SpawnArmyEvent()
		{
		}

		public static SpawnArmyEvent Create()
		{
			return new SpawnArmyEvent();
		}

		public SpawnArmyEvent(Point position)
		{
			this.position = position;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WritePoint(position, "position");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			position = ser.ReadPoint("position");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			Character character = CharacterFactory.CreateCourtCandidate(kingdom.game, kingdom.id, "Marshal");
			kingdom.AddCourtMember(character);
			Army army = new Army(kingdom.game, position, character.kingdom_id);
			army.FillWithRandomUnits();
			army.SetLeader(character);
		}
	}

	[Serialization.Event(28)]
	public class ChangeTaxRateEvent : Serialization.ObjectEvent
	{
		private int taxRate;

		public ChangeTaxRateEvent()
		{
		}

		public static ChangeTaxRateEvent Create()
		{
			return new ChangeTaxRateEvent();
		}

		public ChangeTaxRateEvent(int taxRate)
		{
			this.taxRate = taxRate;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(taxRate, "taxRate");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			taxRate = ser.Read7BitUInt("taxRate");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).SetTaxRate(taxRate);
		}
	}

	[Serialization.Event(29)]
	public class TakeLoanEvent : Serialization.ObjectEvent
	{
		private float loanSize;

		public TakeLoanEvent()
		{
		}

		public static TakeLoanEvent Create()
		{
			return new TakeLoanEvent();
		}

		public TakeLoanEvent(float loanSize)
		{
			this.loanSize = loanSize;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteFloat(loanSize, "loanSize");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			loanSize = ser.ReadFloat("loanSize");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).TakeLoan(loanSize);
		}
	}

	[Serialization.Event(30)]
	public class HireOrdinaryCharacterEvent : Serialization.ObjectEvent
	{
		private int index;

		private string class_def;

		private NID target_nid;

		public HireOrdinaryCharacterEvent()
		{
		}

		public static HireOrdinaryCharacterEvent Create()
		{
			return new HireOrdinaryCharacterEvent();
		}

		public HireOrdinaryCharacterEvent(int index, string class_def, Object target)
		{
			this.index = index;
			this.class_def = class_def;
			target_nid = target;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(index, "index");
			ser.WriteStr(class_def, "class_def");
			ser.WriteNID(target_nid, "target");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			index = ser.Read7BitUInt("index");
			class_def = ser.ReadStr("class_def");
			target_nid = ser.ReadNID("target");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			Resource cost = ForHireStatus.GetCost(obj.game, kingdom, class_def);
			if (!kingdom.resources.CanAfford(cost, 1f))
			{
				return;
			}
			KingdomAI.Expense.Category category = KingdomAI.Expense.Category.Economy;
			if (!string.IsNullOrEmpty(class_def))
			{
				CharacterClass.Def def = obj.game.defs.Get<CharacterClass.Def>(class_def);
				if (def != null && def.ai_category != KingdomAI.Expense.Category.None)
				{
					category = def.ai_category;
				}
			}
			kingdom.SubResources(category, cost);
			Character character = CharacterFactory.CreateCourtCandidate(kingdom, class_def);
			kingdom.AddCourtMember(character, index, is_hire: true);
			if (character.IsMarshal())
			{
				Object obj2 = target_nid.GetObj(kingdom.game);
				character.SpawnArmy(obj2 as Castle);
			}
		}
	}

	[Serialization.Event(31)]
	public class DeleteCourtMemberEvent : Serialization.ObjectEvent
	{
		private int idx;

		private bool kill_or_throneroom = true;

		public DeleteCourtMemberEvent()
		{
		}

		public static DeleteCourtMemberEvent Create()
		{
			return new DeleteCourtMemberEvent();
		}

		public DeleteCourtMemberEvent(int index, bool kill_or_throneroom)
		{
			idx = index;
			this.kill_or_throneroom = kill_or_throneroom;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(idx, "index");
			ser.WriteBool(kill_or_throneroom, "kill_or_throneroom");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			idx = ser.Read7BitUInt("index");
			kill_or_throneroom = ser.ReadBool("kill_or_throneroom");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.court != null)
			{
				Character c = null;
				if (idx >= 0 && idx < kingdom.court.Count)
				{
					c = kingdom.court[idx];
				}
				kingdom.DelCourtMember(c, send_state: true, kill_or_throneroom);
			}
		}
	}

	[Serialization.Event(32)]
	public class CancelAdoptingIdeaEvent : Serialization.ObjectEvent
	{
		public static CancelAdoptingIdeaEvent Create()
		{
			return new CancelAdoptingIdeaEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).GetAdvantages();
		}
	}

	[Serialization.Event(33)]
	public class SetReligionEvent : Serialization.ObjectEvent
	{
		private string religion_def_id;

		public SetReligionEvent()
		{
		}

		public static SetReligionEvent Create()
		{
			return new SetReligionEvent();
		}

		public SetReligionEvent(Religion religion)
		{
			if (religion != null)
			{
				religion_def_id = religion.def.id;
			}
			else
			{
				religion_def_id = "";
			}
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(religion_def_id, "religion_def_id");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			religion_def_id = ser.ReadStr("religion_def_id");
		}

		public override void ApplyTo(Object obj)
		{
			Religion religion = null;
			Kingdom obj2 = obj as Kingdom;
			if (religion_def_id != null && religion_def_id != "")
			{
				Religion.Def def = obj.game.defs.Find<Religion.Def>(religion_def_id);
				religion = obj.game.religions.Get(def);
			}
			obj2.SetReligion(religion);
		}
	}

	[Serialization.Event(34)]
	public class IncreaseCrownAuthorityEvent : Serialization.ObjectEvent
	{
		public static IncreaseCrownAuthorityEvent Create()
		{
			return new IncreaseCrownAuthorityEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			CrownAuthority crownAuthority = kingdom.GetComponent<CrownAuthority>();
			if (crownAuthority == null)
			{
				crownAuthority = new CrownAuthority(kingdom);
			}
			crownAuthority.IncreaseValueWithGold();
		}
	}

	[Serialization.Event(35)]
	public class SetHeirEvent : Serialization.ObjectEvent
	{
		private NID character_nid;

		private string change_type;

		private string abdication_reason;

		public SetHeirEvent()
		{
		}

		public static SetHeirEvent Create()
		{
			return new SetHeirEvent();
		}

		public SetHeirEvent(Character c)
		{
			character_nid = c;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID(character_nid, "character");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			character_nid = ser.ReadNID("character");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			Character character = character_nid.Get<Character>(obj.game);
			if (character != null)
			{
				kingdom.royalFamily.SetHeir(character);
			}
		}
	}

	[Serialization.Event(36)]
	public class SwapCourteSlotsEvent : Serialization.ObjectEvent
	{
		private int idx1;

		private int idx2;

		public SwapCourteSlotsEvent()
		{
		}

		public static SwapCourteSlotsEvent Create()
		{
			return new SwapCourteSlotsEvent();
		}

		public SwapCourteSlotsEvent(int index1, int index2)
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
			Kingdom kingdom = obj as Kingdom;
			if (kingdom?.court != null)
			{
				kingdom.SwapCourtSlots(idx1, idx2);
			}
		}
	}

	[Serialization.Event(37)]
	public class AuthorityCaptureGameEvent : Serialization.ObjectEvent
	{
		private string reportName;

		public AuthorityCaptureGameEvent()
		{
		}

		public static AuthorityCaptureGameEvent Create()
		{
			return new AuthorityCaptureGameEvent();
		}

		public AuthorityCaptureGameEvent(string reportName)
		{
			this.reportName = reportName;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(reportName, "name");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			reportName = ser.ReadStr("name");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).NotifyListeners("authority_capture_game", reportName);
		}
	}

	[Serialization.Event(38)]
	public class InheritanceNextPrincessEvent : Serialization.ObjectEvent
	{
		public static InheritanceNextPrincessEvent Create()
		{
			return new InheritanceNextPrincessEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).GetComponent<Inheritance>().HandleNextPrincess();
		}
	}

	[Serialization.Event(39)]
	public class CancelUpgradeEvent : Serialization.ObjectEvent
	{
		public string def_id;

		public CancelUpgradeEvent(Building.Def def)
		{
			def_id = def?.id;
		}

		public static CancelUpgradeEvent Create()
		{
			return new CancelUpgradeEvent(null);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def_id, "def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def_id = ser.ReadStr("def");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom obj2 = obj as Kingdom;
			Building.Def def = obj2.game.defs.Find<Building.Def>(def_id);
			obj2?.CancelUpgrading(def);
		}
	}

	[Serialization.Event(40)]
	public class RemovePlannedUpgradeEvent : Serialization.ObjectEvent
	{
		public string def_id;

		public RemovePlannedUpgradeEvent(Building.Def def)
		{
			def_id = def?.id;
		}

		public static RemovePlannedUpgradeEvent Create()
		{
			return new RemovePlannedUpgradeEvent(null);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(def_id, "def");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			def_id = ser.ReadStr("def");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom obj2 = obj as Kingdom;
			Building.Def def = obj2.game.defs.Find<Building.Def>(def_id);
			obj2?.RemovePlanedUpgrade(def);
		}
	}

	[Serialization.Event(41)]
	public class AddDiplomacyReasonEvent : Serialization.ObjectEvent
	{
		private Data data;

		private int idx;

		private bool replace;

		public AddDiplomacyReasonEvent()
		{
		}

		public AddDiplomacyReasonEvent(Reason r, int idx, bool replace = false)
		{
			data = Data.CreateFull(r);
			this.idx = idx;
			this.replace = replace;
		}

		public static AddDiplomacyReasonEvent Create()
		{
			return new AddDiplomacyReasonEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteData(data, "reason");
			ser.Write7BitUInt(idx, "idx");
			ser.WriteBool(replace, "replace");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			data = ser.ReadData("reason");
			idx = ser.Read7BitUInt("idx");
			replace = ser.ReadBool("replace");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			if (kingdom.diplomacyReasons == null)
			{
				kingdom.diplomacyReasons = new List<Reason>();
			}
			if (kingdom.diplomacyReasons.Count < idx)
			{
				Game.Log($"Could not add dipomacyReason {data.ToString()} to {kingdom}. Idx out of range.", Game.LogType.Error);
				return;
			}
			Reason reason = data.GetObject(obj.game) as Reason;
			if (!data.ApplyTo(reason, obj.game))
			{
				Game.Log("Could not apply " + data.ToString() + " to " + reason.ToString(), Game.LogType.Error);
			}
			else if (replace)
			{
				kingdom.diplomacyReasons[idx] = reason;
			}
			else
			{
				kingdom.diplomacyReasons.Insert(idx, reason);
			}
		}
	}

	[Serialization.Event(42)]
	public class ChoosePatriarchEvent : Serialization.ObjectEvent
	{
		private NID cleric;

		public ChoosePatriarchEvent()
		{
		}

		public ChoosePatriarchEvent(Character cleric)
		{
			this.cleric = cleric;
		}

		public static ChoosePatriarchEvent Create()
		{
			return new ChoosePatriarchEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Character>(cleric, "cleric");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			cleric = ser.ReadNID<Character>("cleric");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			Character c = cleric.Get<Character>(kingdom.game);
			kingdom.game.religions.orthodox.PatriarchChosen(kingdom, c);
		}
	}

	[Serialization.Event(43)]
	public class InheritanceAbstainEvent : Serialization.ObjectEvent
	{
		public static InheritanceAbstainEvent Create()
		{
			return new InheritanceAbstainEvent();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
		}

		public override void ReadBody(Serialization.IReader ser)
		{
		}

		public override void ApplyTo(Object obj)
		{
			(obj as Kingdom).GetComponent<Inheritance>().Abstain();
		}
	}

	[Serialization.Event(44)]
	public class PayoffRebelion : Serialization.ObjectEvent
	{
		private NID rebellionID;

		public PayoffRebelion()
		{
		}

		public PayoffRebelion(Rebellion rebellion)
		{
			rebellionID = rebellion;
		}

		public static PayoffRebelion Create()
		{
			return new PayoffRebelion();
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteNID<Rebellion>(rebellionID, "rebellion");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			rebellionID = ser.ReadNID<Rebellion>("rebellion");
		}

		public override void ApplyTo(Object obj)
		{
			Kingdom kingdom = obj as Kingdom;
			Rebellion rebellion = rebellionID.Get<Rebellion>(kingdom.game);
			Resource resource = new Resource();
			resource.Add(ResourceType.Gold, rebellion.GetPayoffGold());
			if (!kingdom.resources.CanAfford(resource, 1f))
			{
				return;
			}
			if (rebellion.GetPayoffRealm(0) != null)
			{
				rebellion.AddOccupiedRealm(rebellion.GetPayoffRealm(0));
			}
			if (rebellion.GetPayoffRealm(1) != null)
			{
				rebellion.AddOccupiedRealm(rebellion.GetPayoffRealm(1));
			}
			if (rebellion.GetPayoffRealm(2) != null)
			{
				rebellion.AddOccupiedRealm(rebellion.GetPayoffRealm(2));
			}
			kingdom.SubResources(KingdomAI.Expense.Category.Diplomacy, resource);
			RebellionIndependence component = rebellion.GetComponent<RebellionIndependence>();
			if (component != null)
			{
				if (component.GetIndependenceRealms().Count > 0)
				{
					component.DeclareIndependence(forced: true);
				}
				else
				{
					rebellion.EndRebellion();
				}
			}
		}
	}

	public DT.Field def;

	public DT.Field csv_field;

	public string Name;

	public string ActiveName;

	public int id;

	public List<Realm> realms = new List<Realm>();

	public HashSet<Kingdom> neighbors = new HashSet<Kingdom>();

	public HashSet<Kingdom> secondaryNeighbors = new HashSet<Kingdom>();

	public List<int> kingdomDistances = new List<int>();

	public List<Realm> externalBorderRealms = new List<Realm>();

	public List<Realm> coreRealmsRaw = new List<Realm>();

	public List<Realm> historicalRealms = new List<Realm>();

	public List<Settlement> occupiedKeeps = new List<Settlement>();

	public List<Realm> occupiedRealms = new List<Realm>();

	public KingdomAI ai;

	public ApplyIncome applyIncome;

	private MercenarySpawner merc_spawner;

	public int generationsPassed;

	public float last_calculated_power;

	public float balance_factor_luck = 1f;

	public float balance_factor_income = 1f;

	public Kingdom sovereignState;

	public Vassalage vassalage;

	public Kingdom defeated_by;

	public List<Kingdom> vassalStates = new List<Kingdom>();

	public Time last_peace_time = Time.Zero;

	public Time t_last_ai_offer_time = Time.Zero;

	public List<War> wars = new List<War>();

	public List<Pact> pacts = new List<Pact>();

	public List<Pact> pacts_against = new List<Pact>();

	public List<CasusBeli> casus_beli = new List<CasusBeli>();

	public List<Character> court = new List<Character>();

	public List<Character> special_court = new List<Character>();

	public RoyalFamily royalFamily;

	public List<Marriage> marriages = new List<Marriage>();

	public List<Kingdom> nonAgressions = new List<Kingdom>();

	public List<Kingdom> allies = new List<Kingdom>();

	public List<Kingdom> tradeAgreementsWith = new List<Kingdom>();

	public Dictionary<int, float> loans = new Dictionary<int, float>();

	public List<Character> foreigners = new List<Character>();

	public Stats stats;

	public Actions actions;

	public Opinions opinions;

	public Character improveOpinionsDiplomat;

	public List<Challenge> challenges = new List<Challenge>();

	public List<Tradition> traditions;

	public List<Building.Def> building_upgrades;

	public List<Building.Def> planned_upgrades;

	public List<Upgrading> upgrading;

	public Tradition.Type[] tradition_slots_types = new Tradition.Type[6]
	{
		Tradition.Type.All,
		Tradition.Type.All,
		Tradition.Type.All,
		Tradition.Type.All,
		Tradition.Type.All,
		Tradition.Type.All
	};

	public int[] traditions_slots_tiers = new int[8] { 1, 2, 2, 3, 3, 3, 4, 4 };

	public KingdomRankingCategories rankingCategories;

	public KingdomAdvantages advantages;

	public int all_tags = -1;

	public int force_rank;

	public List<KingdomAndKingdomRelation> relations;

	public Religion religion;

	public List<Religion.StatModifier> religion_mods;

	public float time_of_excommunication = -1f;

	public bool subordinated = true;

	public Character patriarch;

	public Castle patriarch_castle;

	public List<Religion.CharacterBonus> patriarch_bonuses;

	public List<Religion.StatModifier> patriarch_mods;

	public List<Orthodox.PatriarchCandidate> patriarch_candidates;

	public Character cur_patriarch_candidate;

	public bool caliphate;

	public Kingdom jihad_target;

	public War jihad;

	public Kingdom jihad_attacker;

	public List<Religion.PaganBelief> pagan_beliefs = new List<Religion.PaganBelief>();

	public int taxLevel;

	public PerLevelValues taxRate;

	public float inflation;

	public float taxForSovereignGold;

	public float taxForSovereignBooks;

	public float taxForSovereignPiety;

	private Resource _resources = new Resource();

	public Resource total_earned = new Resource();

	public Resource[] earned_by_category = new Resource[7];

	public Resource total_spent = new Resource();

	public Resource[] spent_by_category = new Resource[7];

	public Incomes incomes;

	public Incomes upkeeps;

	private Resource _income = new Resource();

	private Resource _expenses = new Resource();

	public bool income_valid;

	public int apply_income_tick;

	public int big_tick_books = 12;

	public int big_tick_levy = 12;

	public int big_tick_gold = 12;

	public float[] wage_thresholds;

	public float goldFromPassiveTrade;

	public float goldFromMerchants;

	public float goldFromRoyalMerchants;

	public float goldFromForeignMerchants;

	public float goldFromImportantRelatives;

	public float goldFromVassals;

	public float untaxGoldFromTradeCenters;

	public float goldFromFoodExport;

	public float goldFromExcessBooks;

	public float goldFromExcessPiety;

	public float goldFromExcessLevy;

	public float goldFromExcessResources;

	public float goldFromGoods;

	public float percGoldFromJizya;

	public float taxGoldFromJizya;

	public float percCorruption;

	public float booksFromVassals;

	public float pietyFromVassals;

	public float allocatedCommerceForPassiveTrade;

	public float allocatedCommerceForTraders;

	public float allocatedCommerceForImportGoods;

	public float allocatedCommerceForImportFood;

	public float allocatedCommerceForExportFood;

	public float allocatedCommerceForBuildings;

	public float allocatedCommerceForExpeditions;

	public float faithFromClerics;

	public float booksFromImportantRelatives;

	public float foodFromImport;

	public float upkeepFoodFromExport;

	public Resource armies_upkeep = new Resource();

	public float wageGoldTotal;

	public float wageGoldForMarshals;

	public float wageGoldForDiplomats;

	public float wageGoldForSpies;

	public float wageGoldForClerics;

	public float upkeepBuildings;

	public float upkeepBribes;

	public float upkeepSupportPretender;

	public float upkeepOccupations;

	public float upkeepDisorder;

	public float upkeepHelpTheWeak;

	public float upkeepGoldFromGoodsImport;

	public float upkeepGoldFromFoodImport;

	public float upkeepJihad;

	public float upkeepPaganBeliefs;

	public List<Kingdom> tradeRouteWith = new List<Kingdom>();

	public List<Army> mercenaries = new List<Army>();

	public List<Army> armies = new List<Army>();

	public List<Army> mercenaries_in = new List<Army>();

	public List<Army> armies_in = new List<Army>();

	public List<Rebellion> rebellions = new List<Rebellion>();

	public List<Rebellion> potentialRebellions = new List<Rebellion>();

	public RoyalDungeon royal_dungeon;

	public KingdomStability stability;

	public List<Book> books = new List<Book>();

	public Fame fameObj;

	public Prestige prestigeObj;

	private Influence.Def influenceDef;

	public Kingdom inAudienceWith;

	public Character favoriteDiplomat;

	public List<Character> diplomats = new List<Character>();

	public List<Reason> diplomacyReasons = new List<Reason>();

	public Time time;

	public string culture_csv_key;

	public string culture;

	public string nobility_key = "Nobility";

	public string nobility_level = "Kingdom";

	public bool hide_type_in_name;

	public string names_key = "EnglishNames";

	public string govern_type;

	public Character.Ethnicity default_ethnicity;

	public int map_color;

	public int primary_army_color;

	public int secondary_army_color;

	public int CoAIndex;

	public string units_set_csv_key;

	public string units_set;

	public List<string> unit_types = new List<string>();

	public int wars_won;

	public int wars_lost;

	public float total_past_war_score;

	public int total_past_wars;

	public Type type;

	public float treasury_min;

	public float treasury_max;

	public float treasury_inner_min;

	public float treasury_inner_max;

	public float treasury_base;

	public float treasury_mod1;

	public float treasury_mod2;

	public float treasury_mod3;

	public float treasury_no_penalty;

	public float[] diplomatic_gold_perc;

	public float inflation_min_gold_perc;

	public float inflation_above_max_treasury_perc;

	public float corruption_min;

	public float corruption_max = 100f;

	public float prod_gold_perc_min = -100f;

	public float prod_gold_perc_max = 100f;

	public float rebel_spawns_per_event_realms_perc = 30f;

	public float loyalist_spawn_mod = 3f;

	public float ts_lvl_1;

	public float ts_lvl_2;

	public float ts_lvl_3;

	public float ts_lvl_peasant;

	public float ts_garrison_factor;

	public float wf_wars_cap;

	public float mp_levies_weight;

	public float mp_rebels_weight;

	public float mp_population_value;

	public float mp_gold_income;

	public float armyStrength;

	public float as_time_frame;

	public float trade_route_profit_exclusive_mod;

	public float trade_route_profit_relation_mod;

	public float trade_route_profit_relation_divide;

	public float trade_route_profit_relation_offset;

	public float trade_route_profit_visitor_mod;

	public float trade_route_capacity_mod;

	public float merchant_income_class_level_mod;

	public float merchant_income_base_perc;

	public float merchant_income_royal_mod;

	public float sufficient_food_base = 1f;

	public float sufficient_food_min = 0.25f;

	public float sufficient_food_max = 1f;

	public float sufficient_food_mod = 1f;

	public float sufficient_food_min_provinces = 5f;

	public float sufficient_food_max_provinces = 10f;

	public float min_provinces_hunger_stability_penalty_perc = 20f;

	public float threats_max_distance = -200f;

	public float threats_max_relationship = -200f;

	public float friends_max_distance = 3f;

	public float friends_min_relationship = 200f;

	public static bool in_RecalcBuildingStates = false;

	private static Dictionary<string, ResourceProduction> temp_resource_production = new Dictionary<string, ResourceProduction>();

	public static int building_state_recalcs;

	public static List<Castle> temp_castles_changed = new List<Castle>(1024);

	public Dictionary<string, HashSet<Building>> missing_resources = new Dictionary<string, HashSet<Building>>();

	private static List<string> tmp_resource_names = new List<string>(100);

	private static List<Building> tmp_buildings = new List<Building>(100);

	public LinkedList<(string resource, bool is_produced, List<Resource.StatModifier> mods)> resource_mods;

	public Dictionary<string, int> realm_tags;

	public Dictionary<string, Resource.Def> goods_produced = new Dictionary<string, Resource.Def>();

	public Dictionary<string, Resource.Def> goods_imported = new Dictionary<string, Resource.Def>();

	public HashSet<string> player_goods_imported_before;

	public List<Resource.Def> monopoly_goods = new List<Resource.Def>();

	private static List<Resource.Def> tmp_check_monopoly = new List<Resource.Def>(32);

	private static List<Character> tmp_characters = new List<Character>();

	private static List<Kingdom> tmp_kingdoms = new List<Kingdom>(32);

	public static bool in_AI_spend = false;

	private static Resource tmp_buildings_upkeep = new Resource();

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public Dictionary<string, ResourceInfo> resources_info;

	public bool resources_info_valid;

	public int resources_info_version;

	public List<Realm> coreRealms
	{
		get
		{
			UpdateCoreRealms();
			return coreRealmsRaw;
		}
		set
		{
			coreRealmsRaw = value;
		}
	}

	public bool is_player => Multiplayer.CurrentPlayers.GetByKingdom(id) != null;

	public bool is_local_player => game.GetLocalPlayerKingdomId() == id;

	public bool is_catholic
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.catholic;
			}
			return false;
		}
	}

	public bool is_orthodox
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.orthodox;
			}
			return false;
		}
	}

	public bool is_christian
	{
		get
		{
			if (religion != null)
			{
				return religion.def.christian;
			}
			return false;
		}
	}

	public bool is_sunni
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.sunni;
			}
			return false;
		}
	}

	public bool is_shia
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.shia;
			}
			return false;
		}
	}

	public bool is_muslim
	{
		get
		{
			if (religion != null)
			{
				return religion.def.muslim;
			}
			return false;
		}
	}

	public bool is_pagan
	{
		get
		{
			if (religion != null)
			{
				return religion == game.religions.pagan;
			}
			return false;
		}
	}

	public bool excommunicated
	{
		get
		{
			return time_of_excommunication >= 0f;
		}
		set
		{
			if (value != excommunicated && religion is Catholic catholic)
			{
				catholic.SetExcommunicatedCount(catholic.GetExcommunicatedCount() + (value ? 1 : (-1)));
			}
			time_of_excommunication = (value ? game.session_time.seconds : (-1f));
		}
	}

	public bool is_ecumenical_patriarchate
	{
		get
		{
			if (religion != null)
			{
				return game.religions.orthodox.head_kingdom == this;
			}
			return false;
		}
	}

	public Resource resources
	{
		get
		{
			_resources.Set(ResourceType.Trade, GetAvailableCommerce(), this);
			return _resources;
		}
		set
		{
			_resources = value;
		}
	}

	public Resource income
	{
		get
		{
			RecalcIncomes();
			return _income;
		}
	}

	public Resource expenses
	{
		get
		{
			RecalcIncomes();
			return _expenses;
		}
	}

	public List<Character> prisoners => royal_dungeon?.prisoners;

	public string NameKey => "tn_" + ((ActiveName != Name) ? ActiveName : Name);

	public float max_prestige
	{
		get
		{
			if (prestigeObj == null)
			{
				return 0f;
			}
			return prestigeObj.GetMaxPrestige();
		}
	}

	public float prestige
	{
		get
		{
			if (prestigeObj == null)
			{
				return 0f;
			}
			return prestigeObj.GetPrestige();
		}
	}

	public float max_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.GetMaxFame();
		}
	}

	public float base_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.GetBaseFame();
		}
	}

	public float fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.GetFame();
		}
	}

	public float realms_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.realms_fame;
		}
	}

	public float building_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.building_fame;
		}
	}

	public float rankings_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.rankings_fame;
		}
	}

	public float trade_centers_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.trade_centers_fame;
		}
	}

	public float marriages_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.marriages_fame;
		}
	}

	public float vassals_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.vassals_fame;
		}
	}

	public float produced_goods_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.produced_goods_fame;
		}
	}

	public float ecumenical_patriarch_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.ecumenical_patriarch_fame;
		}
	}

	public float caliphate_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.caliphate_fame;
		}
	}

	public float autocephaly_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.autocephaly_fame;
		}
	}

	public float non_orthodox_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.non_orthodox_fame;
		}
	}

	public float traditions_fame
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.traditions_fame;
		}
	}

	public float other_fame_bonuses
	{
		get
		{
			if (fameObj == null)
			{
				return 0f;
			}
			return fameObj.fame_bonus;
		}
	}

	public float current_fame_victory => Math.Min(fame, required_fame_victory);

	public float required_fame_victory
	{
		get
		{
			if (fameObj == null)
			{
				return float.MaxValue;
			}
			return fameObj.GetMinVicotryFame();
		}
	}

	public override Kingdom GetKingdom()
	{
		return this;
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return NameKey;
	}

	public void UpdateAIState()
	{
		if (ai == null)
		{
			return;
		}
		if (game.campaign != null)
		{
			if (game.campaign.GetPlayerIndexForKingdom(Name) >= 0)
			{
				UpdateAIState(is_player: true);
			}
			else
			{
				UpdateAIState(is_player: false);
			}
		}
		else if (Multiplayer.CurrentPlayers.KingdomIsPlayer(Name))
		{
			UpdateAIState(is_player: true, is_connected: true);
		}
		else
		{
			UpdateAIState(is_player: false, is_connected: false);
		}
	}

	public void UpdateAIState(bool is_player)
	{
		if (!is_player)
		{
			UpdateAIState(is_player: false, is_connected: false);
		}
		else if (Multiplayer.CurrentPlayers.KingdomIsPlayer(Name))
		{
			UpdateAIState(is_player: true, is_connected: true);
		}
		else
		{
			UpdateAIState(is_player: true, is_connected: false);
		}
	}

	public void UpdateAIState(bool is_player, bool is_connected)
	{
		KingdomAI.EnableFlags enable_flags;
		if (is_player)
		{
			if (is_connected)
			{
				return;
			}
			enable_flags = KingdomAI.EnableFlags.Disabled;
			if (!is_connected && game.pings != null)
			{
				float disconnected_time = (float)game.pings.TimeSinceLastPing(id) / 60000f;
				CheckFlag(KingdomAI.EnableFlags.Kingdom, Multiplayer.multiplayer_settings.AICT_Kingdom, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.HireCourt, Multiplayer.multiplayer_settings.AICT_HireCourt, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Buildings, Multiplayer.multiplayer_settings.AICT_Buildings, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Armies, Multiplayer.multiplayer_settings.AICT_Armies, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Units, Multiplayer.multiplayer_settings.AICT_Units, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Garrison, Multiplayer.multiplayer_settings.AICT_Garrison, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Characters, Multiplayer.multiplayer_settings.AICT_Characters, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Diplomacy, Multiplayer.multiplayer_settings.AICT_Diplomacy, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Wars, Multiplayer.multiplayer_settings.AICT_Wars, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Offense, Multiplayer.multiplayer_settings.AICT_Offense, disconnected_time);
				CheckFlag(KingdomAI.EnableFlags.Mercenaries, Multiplayer.multiplayer_settings.AICT_Mercenaries, disconnected_time);
			}
		}
		else
		{
			enable_flags = KingdomAI.EnableFlags.All;
		}
		if (ai.enabled != enable_flags)
		{
			Log($"UpdateAIState: {ai.enabled} -> {enable_flags}");
			ai.enabled = enable_flags;
		}
		void CheckFlag(KingdomAI.EnableFlags flag, int time_idx, float num2)
		{
			if (time_idx == 0)
			{
				enable_flags |= flag;
			}
			else if (time_idx >= 0 && time_idx <= Multiplayer.multiplayer_settings.disconnected_player_ai_times.Count)
			{
				float num = Multiplayer.multiplayer_settings.disconnected_player_ai_times[time_idx - 1];
				if (!(num2 < num))
				{
					enable_flags |= flag;
				}
			}
		}
	}

	public void SetAIState(bool enabled)
	{
		if (ai != null)
		{
			KingdomAI.EnableFlags enableFlags = (enabled ? KingdomAI.EnableFlags.All : KingdomAI.EnableFlags.Disabled);
			if (ai.enabled != enableFlags)
			{
				Log($"SetAIState: {ai.enabled} -> {enableFlags}");
				ai.enabled = enableFlags;
			}
		}
	}

	public void AssignRealm(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			Realm realm = game.GetRealm(name);
			if (realm == null)
			{
				Error("unknown realm: " + name);
			}
			else
			{
				AssignRealm(realm);
			}
		}
	}

	public void AssignRealm(Realm r)
	{
		Kingdom kingdom = game.GetKingdom(r.init_kingdom_id);
		if (kingdom != this)
		{
			kingdom?.DelRealm(r, id, ignore_victory: true);
			r.init_kingdom_id = (r.kingdom_id = id);
			r.controller = this;
			AddRealm(r);
			kingdom?.NotifyListeners("realm_deleted", r);
		}
	}

	public void ReLoadFromDef()
	{
		treasury_min = def.GetFloat("treasury_min");
		treasury_max = def.GetFloat("treasury_max");
		treasury_inner_min = def.GetFloat("treasury_inner_min");
		treasury_inner_max = def.GetFloat("treasury_inner_max");
		treasury_base = def.GetFloat("treasury_base");
		treasury_mod1 = def.GetFloat("treasury_mod1");
		treasury_mod2 = def.GetFloat("treasury_mod2");
		treasury_mod3 = def.GetFloat("treasury_mod3");
		treasury_no_penalty = def.GetFloat("treasury_no_penalty");
		DT.Field field = def.FindChild("diplomatic_gold_perc");
		int num = field?.NumValues() ?? 0;
		if (num > 0)
		{
			diplomatic_gold_perc = new float[num];
			for (int i = 0; i < num; i++)
			{
				diplomatic_gold_perc[i] = field.Float(i);
			}
		}
		else
		{
			diplomatic_gold_perc = new float[5] { 20f, 45f, 70f, 100f, 120f };
		}
		inflation_min_gold_perc = def.GetFloat("inflation_min_gold_perc");
		inflation_above_max_treasury_perc = def.GetFloat("inflation_above_max_treasury_perc", null, 10f);
		corruption_min = def.GetFloat("corruption_min");
		corruption_max = def.GetFloat("corruption_max");
		prod_gold_perc_min = def.GetFloat("prod_gold_perc_min");
		prod_gold_perc_max = def.GetFloat("prod_gold_perc_max");
		ts_lvl_1 = def.GetFloat("ts_lvl_1");
		ts_lvl_2 = def.GetFloat("ts_lvl_2");
		ts_lvl_3 = def.GetFloat("ts_lvl_3");
		ts_lvl_peasant = def.GetFloat("ts_lvl_peasant");
		ts_garrison_factor = def.GetFloat("ts_garrison_factor");
		wf_wars_cap = def.GetFloat("wf_wars_cap");
		mp_levies_weight = def.GetFloat("mp_levies_weight");
		mp_rebels_weight = def.GetFloat("mp_rebels_weight");
		mp_population_value = def.GetFloat("mp_population_value");
		mp_gold_income = def.GetFloat("mp_gold_income");
		trade_route_profit_exclusive_mod = def.GetFloat("trade_route_profit_exclusive_mod");
		trade_route_profit_relation_mod = def.GetFloat("trade_route_profit_relation_mod");
		trade_route_profit_relation_divide = def.GetFloat("trade_route_profit_relation_divide");
		trade_route_profit_relation_offset = def.GetFloat("trade_route_profit_relation_offset");
		trade_route_profit_visitor_mod = def.GetFloat("trade_route_profit_visitor_mod");
		trade_route_capacity_mod = def.GetFloat("trade_route_capacity_mod");
		merchant_income_class_level_mod = def.GetFloat("merchant_income_class_level_mod");
		merchant_income_base_perc = def.GetFloat("merchant_income_base_perc");
		merchant_income_royal_mod = def.GetFloat("merchant_income_royal_mod");
		sufficient_food_base = def.GetFloat("sufficient_food_base");
		sufficient_food_min = def.GetFloat("sufficient_food_min");
		sufficient_food_max = def.GetFloat("sufficient_food_max");
		sufficient_food_mod = def.GetFloat("sufficient_food_mod");
		sufficient_food_min_provinces = def.GetFloat("sufficient_food_min_provinces");
		sufficient_food_max_provinces = def.GetFloat("sufficient_food_max_provinces");
		min_provinces_hunger_stability_penalty_perc = def.GetFloat("min_provinces_hunger_stability_penalty_perc");
		big_tick_books = def.GetInt("big_income_ticks");
		big_tick_levy = def.GetInt("big_income_ticks");
		big_tick_gold = def.GetInt("big_income_ticks");
		threats_max_distance = def.GetInt("threats_max_distance");
		threats_max_relationship = def.GetFloat("threats_max_relationship");
		friends_max_distance = def.GetInt("friends_max_distance");
		friends_min_relationship = def.GetFloat("friends_min_relationship");
		rebel_spawns_per_event_realms_perc = def.GetFloat("rebel_spawns_per_event_realms_perc");
		loyalist_spawn_mod = def.GetFloat("loyalist_spawn_mod");
		Tradition.Def.ParseSlots(def.FindChild("traditions_slots"), ref tradition_slots_types);
		Tradition.Def.ParseSlotTiers(def.FindChild("traditions_slots_tiers"), ref traditions_slots_tiers);
	}

	private void LoadFromDef()
	{
		ReLoadFromDef();
		DT.Field field = def.FindChild("realms");
		if (field != null && field.children != null)
		{
			for (int i = 0; i < field.children.Count; i++)
			{
				string key = field.children[i].key;
				AssignRealm(key);
			}
		}
		taxRate = PerLevelValues.Parse<float>(def.FindChild("tax_rates"));
	}

	private void LoadFromCSV(string cultureCSVKey = null, string unitsSetCSVKey = null)
	{
		if (csv_field != null)
		{
			LoadCulture(cultureCSVKey);
			string value = csv_field.GetString("NobilityNames");
			if (!string.IsNullOrEmpty(value))
			{
				names_key = value;
			}
			string value2 = csv_field.GetString("NobilityTitles");
			if (!string.IsNullOrEmpty(value2))
			{
				nobility_key = value2;
			}
			string[] array = csv_field.GetString("NobilityLevel").Split('.');
			string value3 = ((array.Length != 0) ? array[0] : null);
			if (!string.IsNullOrEmpty(value3))
			{
				nobility_level = value3;
			}
			if (array.Length > 1 && array[1] == "H")
			{
				hide_type_in_name = true;
			}
			if (!names_key.EndsWith("Names", StringComparison.Ordinal))
			{
				names_key += "Names";
			}
			if (!nobility_key.EndsWith("Nobility", StringComparison.Ordinal))
			{
				nobility_key += "Nobility";
			}
			govern_type = csv_field.GetString("GovernType", null, "Monarchy");
			if (unitsSetCSVKey == null || csv_field.key == unitsSetCSVKey)
			{
				LoadUnitsSet(csv_field, send_state: false);
			}
			else
			{
				LoadUnitsSet(unitsSetCSVKey, send_state: false);
			}
		}
	}

	private void LoadCulture(string cultureCSVKey = null)
	{
		DT.Field kingdomCSV = csv_field;
		if (cultureCSVKey != null && cultureCSVKey != kingdomCSV.key)
		{
			kingdomCSV = game.GetKingdomCSV(cultureCSVKey);
		}
		string text = kingdomCSV?.GetString("Culture");
		if (string.IsNullOrEmpty(text))
		{
			Game.Log(kingdomCSV.Path(include_file: true) + ": no culture specified for kingdom '" + Name + "'", Game.LogType.Error);
			return;
		}
		if (!game.cultures.IsValid(text))
		{
			Game.Log(kingdomCSV.Path(include_file: true) + ": invalid culture '" + text + "' specified for kingdom '" + Name + "'", Game.LogType.Error);
			return;
		}
		culture_csv_key = kingdomCSV.key;
		culture = text;
		Cultures.Defaults defaults = game.cultures.GetDefaults(text);
		if (defaults == null)
		{
			Game.Log(kingdomCSV.Path(include_file: true) + ": culture '" + text + "' specified for kingdom '" + Name + "' is not present in cultures.csv", Game.LogType.Error);
			return;
		}
		if (!string.IsNullOrEmpty(defaults.NobilityNames))
		{
			names_key = defaults.NobilityNames;
		}
		if (!string.IsNullOrEmpty(defaults.NobilityTitles))
		{
			nobility_key = defaults.NobilityTitles;
		}
		if (!string.IsNullOrEmpty(defaults.UnitsSet))
		{
			units_set = defaults.UnitsSet;
		}
		default_ethnicity = defaults.Ethnicity;
	}

	public void LoadVassalage()
	{
		if ((game != null && game.rules != null && game.rules.MapIsShattered()) || IsDefeated())
		{
			return;
		}
		string text = csv_field?.GetString("VassalOf");
		if (!string.IsNullOrEmpty(text))
		{
			Kingdom kingdom = game.GetKingdom(text);
			if (kingdom != null)
			{
				kingdom.AddVassalState(this, set_sovereign: true, send_state: false);
			}
			else
			{
				Game.Log(csv_field.Path(include_file: true) + ": Unknown VassalOf kingdom: '" + text + "'", Game.LogType.Error);
			}
		}
	}

	public void LoadCapitalProvince()
	{
		string text = csv_field?.GetString("CapitalProvince");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Realm realm = game.GetRealm(text);
		if (realm == null)
		{
			Game.Log(csv_field.Path(include_file: true) + ".CapitalProvince: Invalid realm '" + text + "'", Game.LogType.Error);
			return;
		}
		int num = realms.IndexOf(realm);
		if (num >= 0 && num != 0)
		{
			realms.RemoveAt(num);
			realms.Insert(0, realm);
		}
	}

	public void LoadUnitsSet(string kingdom_csv_key, bool send_state = true)
	{
		DT.Field csv = game?.GetKingdomCSV(kingdom_csv_key);
		LoadUnitsSet(csv, send_state);
	}

	public void LoadUnitsSet(DT.Field csv, bool send_state = true)
	{
		if (csv != null)
		{
			units_set_csv_key = csv.key;
			string value = csv.GetString("UnitsSet");
			if (!string.IsNullOrEmpty(value))
			{
				game.ValidateDefID(value, "AvailableUnits", csv, "UnitsSet");
				units_set = value;
			}
			unit_types.Clear();
			Game.AddStringsToList(csv.GetString("Units"), unit_types);
			game.ValidateDefIDs(unit_types, "Unit", csv, "Units");
			if (send_state)
			{
				SendState<NameAndCultureState>();
			}
		}
	}

	public void Load()
	{
		LoadFromDef();
		LoadFromCSV();
	}

	public string GetPietyIcon(bool negative = false)
	{
		string text = "catholic";
		if (is_catholic)
		{
			text = (excommunicated ? "excommunicated" : "catholic");
		}
		if (is_orthodox)
		{
			text = (HasEcumenicalPatriarch() ? "orthodox" : (subordinated ? "subordinated" : "autocephaly"));
		}
		if (is_sunni)
		{
			text = (caliphate ? "sunni_caliphate" : "sunni");
		}
		if (is_shia)
		{
			text = (caliphate ? "shia_caliphate" : "shia");
		}
		if (is_pagan)
		{
			text = "pagan";
		}
		if (negative)
		{
			text += "_negative";
		}
		return text + "_icon";
	}

	public Realm GetCapital()
	{
		if (realms.Count == 0)
		{
			return null;
		}
		Character king = GetKing();
		if (king != null)
		{
			if (king.governed_castle != null)
			{
				return king.governed_castle.GetRealm();
			}
		}
		else
		{
			Error("King is null for " + ToString());
		}
		if (royalFamily.Children.Count > 0)
		{
			Character character = royalFamily.Children[0];
			for (int i = 1; i < royalFamily.Children.Count; i++)
			{
				if (royalFamily.Children[i].age > character.age && royalFamily.Children[i].governed_castle != null)
				{
					character = royalFamily.Children[i];
				}
			}
			if (character.governed_castle != null && !character.IsRebel())
			{
				return character.governed_castle.GetRealm();
			}
		}
		Realm realm = realms[0];
		for (int j = 1; j < realms.Count; j++)
		{
			if (realms[j].income[ResourceType.Gold] > realm.income[ResourceType.Gold])
			{
				realm = realms[j];
			}
		}
		return realm;
	}

	public float GetTaxRate()
	{
		if (taxRate != null)
		{
			return taxRate.GetFloat(taxLevel + 1);
		}
		return 0f;
	}

	public float GetTaxMul()
	{
		float num = GetTaxRate();
		float stat = GetStat(Stats.ks_tax_bonus);
		return num / 100f * (100f + stat) / 100f;
	}

	public bool IsRebelKingdom()
	{
		if (type != Type.RebelFaction && type != Type.LoyalistsFaction)
		{
			return type == Type.ReligiousFaction;
		}
		return true;
	}

	public int GetStartingRealmsCount()
	{
		int kingdomSize = game.rules.GetKingdomSize();
		if (kingdomSize != Game.CampaignRules.useDefaultKingdomSize)
		{
			return kingdomSize;
		}
		int num = 0;
		for (int i = 0; i < game.realms.Count; i++)
		{
			if (game.realms[i].init_kingdom_id == id)
			{
				num++;
			}
		}
		return num;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "num_armies":
			return armies.Count;
		case "num_mercenaries":
			return mercenaries.Count;
		case "too_many_rebellions_per_province":
			return stability != null && rebellions.Count > realms.Count / stability.def.min_provinces_per_rebellion;
		case "too_many_rebellions_per_kingdom":
			return stability != null && rebellions.Count > stability.MaxRebellionsPerKingdom();
		case "max_player_rebellions":
			return game.GetPerDifficultyInt(game.GetDevSettingsDef().max_player_rebellions, null);
		case "is_war_leader":
		{
			if (vars == null)
			{
				return false;
			}
			War war = vars.GetVar("war").Get<War>();
			if (war == null)
			{
				return false;
			}
			return war.IsLeader(this);
		}
		case "upkeep_jihad":
			return War.GetJihadUpkeep(this);
		case "obj":
			return this;
		case "kingdom":
			return this;
		case "game":
			return game;
		case "name":
			return Name;
		case "KingdomType":
		case "kingdom_type":
			return GetKingdomType();
		case "HideKingdomTypeInName":
		case "hide_kingdom_type_in_name":
			return hide_type_in_name;
		case "starting_realms_count":
			return GetStartingRealmsCount();
		case "realms_count":
			return realms.Count;
		case "id":
			return id;
		case "is_player":
			return is_player;
		case "is_ai":
			return ai != null && ai.Enabled(KingdomAI.EnableFlags.All);
		case "is_regular":
			return IsRegular();
		case "capital_realm":
			return GetCapital();
		case "king":
			return GetKing();
		case "king_name":
			return (GetKing() == null) ? "null" : GetKing().Name;
		case "king_age_str":
			return (GetKing() == null) ? "null" : GetKing().age.ToString();
		case "queen":
			return GetQueen();
		case "heir":
			return GetHeir();
		case "resources":
			return resources;
		case "total_earned":
			return total_earned;
		case "type":
			return type.ToString();
		case "income":
			return income;
		case "_income":
			return _income;
		case "expenses":
			return expenses;
		case "_expenses":
			return _expenses;
		case "incomes":
			return new Value(incomes);
		case "upkeeps":
			return new Value(upkeeps);
		case "marshal_wages":
		case "MARSHAL_WAGES":
			return wageGoldForMarshals;
		case "diplomat_wages":
		case "DIPLOMAT_WAGES":
			return wageGoldForDiplomats;
		case "spy_wages":
		case "SPY_WAGES":
			return wageGoldForSpies;
		case "cleric_wages":
		case "CLERIC_WAGES":
			return wageGoldForClerics;
		case "total_wages":
			return wageGoldTotal;
		case "building_upgrades":
		case "upgrades":
			return new Value(building_upgrades);
		case "upgrades_count":
			return (building_upgrades != null) ? building_upgrades.Count : 0;
		case "planned_upgrades":
			return new Value(planned_upgrades);
		case "upgrading":
			return new Value(upgrading);
		case "upgrading_in_same_district":
		{
			if (vars == null)
			{
				return Value.Null;
			}
			Building.Def def2 = vars.GetVar("building").Get<Building.Def>();
			return FindUpgradingInSameDistrict(def2);
		}
		case "crown_authority":
		{
			CrownAuthority crownAuthority = GetCrownAuthority();
			if (crownAuthority == null)
			{
				return 0;
			}
			return crownAuthority.GetValue();
		}
		case "authority":
			return GetCrownAuthority();
		case "is_great_power":
			return IsGreatPower();
		case "previous_great_power_ranking_position":
		{
			List<KingdomRanking.Row> list = game?.great_powers?.previous_last_rankings;
			if (list == null)
			{
				return 0;
			}
			if (game.great_powers.TopKingdoms().Count == 0)
			{
				return 0;
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].kingdom == this)
				{
					return j + 1;
				}
			}
			return 0;
		}
		case "great_power_ranking_position":
		{
			List<KingdomRanking.Row> list5 = game?.great_powers?.last_rankings;
			if (list5 == null)
			{
				return 0;
			}
			if (game.great_powers.TopKingdoms().Count == 0)
			{
				return 0;
			}
			for (int num11 = 0; num11 < list5.Count; num11++)
			{
				if (list5[num11].kingdom == this)
				{
					return num11 + 1;
				}
			}
			return 0;
		}
		case "great_power_rankings":
		{
			List<KingdomRanking.Row> list4 = game?.great_powers?.last_rankings;
			if (list4 == null)
			{
				return 0;
			}
			if (game.great_powers.TopKingdoms().Count == 0)
			{
				return 0;
			}
			return list4.Count;
		}
		case "prestige":
			return prestige;
		case "max_prestige":
			return max_prestige;
		case "fame":
			return fame;
		case "base_fame":
			return base_fame;
		case "realms_fame":
			return realms_fame;
		case "building_fame":
			return building_fame;
		case "rankings_fame":
			return rankings_fame;
		case "trade_centers_fame":
			return trade_centers_fame;
		case "marriages_fame":
			return marriages_fame;
		case "vassals_fame":
			return vassals_fame;
		case "produced_goods_fame":
			return produced_goods_fame;
		case "ecumenical_patriarch_fame":
			return ecumenical_patriarch_fame;
		case "caliphate_fame":
			return caliphate_fame;
		case "autocephaly_fame":
			return autocephaly_fame;
		case "non_orthodox_fame":
			return non_orthodox_fame;
		case "traditions_fame":
			return traditions_fame;
		case "other_fame_bonuses":
			return other_fame_bonuses;
		case "required_fame_victory":
			return required_fame_victory;
		case "tax_level":
			return taxLevel;
		case "tax_rate":
			return GetTaxRate();
		case "tax_CA":
		{
			Stat stat = stats.Find(Stats.ks_tax_rate);
			stat.CalcValue();
			List<Stat.Factor> list3 = stat?.GetFactors();
			if (list3 == null)
			{
				return 0;
			}
			for (int num10 = 0; num10 < list3.Count; num10++)
			{
				Stat.Factor factor = list3[num10];
				if (factor.mod?.GetField()?.key == "from_authority" || factor.stat?.def?.field?.key == "from_authority")
				{
					return factor.value;
				}
			}
			return 0;
		}
		case "commerce":
			return GetMaxCommerce();
		case "num_goods":
			UpdateRealmTags();
			return goods_produced.Count + goods_imported.Count;
		case "num_produced_goods":
			UpdateRealmTags();
			return goods_produced.Count;
		case "goods_produced":
			UpdateRealmTags();
			return new Value(goods_produced);
		case "goods_produced_list":
		{
			UpdateRealmTags();
			List<Resource.Def> list2 = new List<Resource.Def>(goods_produced.Count);
			foreach (KeyValuePair<string, Resource.Def> item in goods_produced)
			{
				_ = item.Key;
				Resource.Def value2 = item.Value;
				list2.Add(value2);
			}
			return new Value(list2);
		}
		case "num_monopoly_goods":
			return monopoly_goods.Count;
		case "monopoly_goods":
			return new Value(monopoly_goods);
		case "import_goods_gold_upkeep":
			return upkeepGoldFromGoodsImport;
		case "num_total_goods":
			return Resource.Def.total;
		case "trade_routes_active":
			return tradeRouteWith.Count;
		case "trade_agreements_signed":
			return tradeAgreementsWith.Count;
		case "trade_centers_influence":
			return GetTotalTradeCentreInfluence();
		case "famous_people_count":
			return GetFamousPeopleCount();
		case "books_count":
			return books.Count;
		case "avg_happiness":
			return GetAvgPopulationHappiness();
		case "kingdom_population":
			return GetTotalPopulation();
		case "kingdom_rebel_population":
			return GetRebelPopulation();
		case "merchants_count":
			return GetMerchantsCount();
		case "trading_merchants_count":
			return GetTradingMerchantsCount();
		case "foreign_merchants_count":
			return GetForeignMerchantsCount();
		case "foreign_merchants":
			return new Value(ForeignersOfClass("Merchant"));
		case "gold_from_trade_total":
			return goldFromMerchants + goldFromRoyalMerchants + goldFromPassiveTrade + goldFromForeignMerchants + goldFromFoodExport + GetGoldFromTradeCenters() + goldFromGoods;
		case "gold_from_excess_books":
			return goldFromExcessBooks;
		case "gold_from_excess_piety":
			return goldFromExcessPiety;
		case "gold_from_excess_levy":
			return goldFromExcessLevy;
		case "gold_from_excess_resources":
			return goldFromExcessResources;
		case "spies_count":
			return GetSpiesCount();
		case "foreign_spies_count":
			return GetForeignSpiesCount();
		case "foreign_spies":
			return new Value(ForeignersOfClass("Spy"));
		case "num_realms":
			return realms.Count;
		case "num_costal_realms":
			return GetCostalRealmsCount();
		case "defeated":
			return IsDefeated();
		case "offers":
			return new Value(GetComponent<Offers>());
		case "is_vassal":
			return IsVassal();
		case "vassal_of":
			return sovereignState;
		case "num_vassals":
			return vassalStates.Count;
		case "num_vassals_realms":
			return GetVassalStatesRealms();
		case "num_vassals_population":
			return GetVassalStatesPopulation();
		case "avg_relationship":
			return GetAvarageRelationshipWithEveryone();
		case "trade_agreements_count":
			return tradeAgreementsWith.Count;
		case "non_aggressions_count":
			return nonAgressions.Count;
		case "alliances_count":
			return allies.Count;
		case "marriages_count":
			return marriages.Count;
		case "pacts_count":
			return pacts.Count;
		case "affected_by_pacts":
			return pacts.Count > 0 || pacts_against.Count > 0;
		case "num_pact_lead_kingdoms":
			return PactLeadKingdomsCount();
		case "num_pacts_lead_kingdoms_realms":
			return PactLeadKingdomsRealmsCount();
		case "num_pacts_lead_kingdoms_population":
			return PactLeadKingdomsPopulationCount();
		case "num_wars":
			return wars.Count;
		case "num_wars_as_leader":
			return WarsAsLeaderCount();
		case "num_wars_as_supporter":
			return WarsAsSupporterCount();
		case "num_enemies":
			return EnemyKingdomsCount();
		case "enemy_kingdoms":
			return new Value(EnemyKingdoms());
		case "num_enemy_lands":
			return EnemyLandsCount();
		case "num_war_lead_max_kingdoms":
			return WarLeadMaxKingdoms();
		case "num_war_lead_kingdoms":
			return WarLeadKingdomsCount();
		case "num_war_lead_kingdoms_realms":
			return WarLeadKingdomsRealmsCount();
		case "num_war_lead_kingdoms_population":
			return WarLeadKingdomsPopulationCount();
		case "time_in_peace":
			return TimeInPeace();
		case "wars_won":
			return wars_won;
		case "wars_lost":
			return wars_lost;
		case "army_strength":
			return CalcArmyStrength();
		case "all_armies_full":
			return AreAllArmiesFull();
		case "num_rebels":
			return GetRebelsCount();
		case "num_prisoners":
			return (prisoners != null) ? prisoners.Count : 0;
		case "prison_capacity":
			return royal_dungeon?.GetCapacity() ?? 0f;
		case "realm_tags":
			UpdateRealmTags();
			return new Value(realm_tags);
		case "culture":
			return culture;
		case "culture_name":
			return "culture_" + culture;
		case "culture_group":
			return game.cultures.GetGroup(culture);
		case "culture_group_name":
		{
			string text6 = game.cultures.GetGroup(culture);
			return string.IsNullOrEmpty(text6) ? null : ("culture_group_" + text6);
		}
		case "religion":
			return this.religion;
		case "religion_family":
			if (is_christian)
			{
				return "christian";
			}
			if (is_muslim)
			{
				return "muslim";
			}
			if (is_pagan)
			{
				return "pagan";
			}
			return Value.Unknown;
		case "is_catholic":
			return is_catholic;
		case "is_orthodox":
			return is_orthodox;
		case "is_christian":
			return is_christian;
		case "is_sunni":
			return is_sunni;
		case "is_shia":
			return is_shia;
		case "is_muslim":
			return is_muslim;
		case "is_pagan":
			return is_pagan;
		case "cleric_title":
			if (this.religion == null)
			{
				return "Cleric.fallback_name";
			}
			return this.religion.name + ".titles.Cleric";
		case "religious_settlement_type":
			return Religion.ReligiousSettlementType(game, this.religion);
		case "religious_settlement_name":
		{
			string text5 = Religion.ReligiousSettlementType(game, this.religion);
			if (text5 == null)
			{
				return Value.Null;
			}
			return "@{" + text5 + ".name}";
		}
		case "court_members_count":
			return NumCourtMembers();
		case "num_marshals":
		case "marshals_count":
			return NumCourtMembersOfClass("Marshal");
		case "diplomats_count":
			return NumCourtMembersOfClass("Diplomat");
		case "diplomats":
			return new Value(CourtMembersOfClass("Diplomat"));
		case "foreign_diplomats_count":
			return NumForeignersOfClass("Diplomat");
		case "foreign_diplomats":
			return new Value(ForeignersOfClass("Diplomat"));
		case "clerics_count":
			return NumCourtMembersOfClass("Cleric");
		case "piety_icon":
			return GetPietyIcon();
		case "religions":
			return game.religions;
		case "excommunicated":
			return excommunicated;
		case "time_of_excommunication":
			return time_of_excommunication;
		case "excommunicated_duration":
			return (time_of_excommunication < 0f) ? 0f : (game.session_time.seconds - time_of_excommunication);
		case "subordinated":
			return subordinated;
		case "is_ecumenical_patriarchate":
			return is_ecumenical_patriarchate;
		case "is_autocephaly":
			return !subordinated && !is_ecumenical_patriarchate;
		case "pope":
			return game.religions.catholic.head;
		case "has_pope":
			return HasPope();
		case "papacy":
			return game.religions.catholic.hq_kingdom;
		case "is_papacy":
			return IsPapacy();
		case "relation_with_pope":
			return GetRelationship(game.religions.catholic.hq_kingdom);
		case "rome_kingdom":
			return game.religions.catholic.hq_realm.GetKingdom();
		case "is_rome_kingdom":
			return game.religions.catholic.hq_realm.GetKingdom() == this;
		case "relation_with_rome_kingdom":
			return GetRelationship(game.religions.catholic.hq_realm.GetKingdom());
		case "patriarch":
			return patriarch ?? cur_patriarch_candidate;
		case "ecumenical_patriarch":
			return game.religions.orthodox.head;
		case "ecumenical_patriarch_kingdom":
			return game.religions.orthodox.head_kingdom;
		case "is_ecumenical_patriarch_kingdom":
			return game.religions.orthodox.head_kingdom == this;
		case "has_ecumenical_patriarch":
			return HasEcumenicalPatriarch();
		case "byzantium":
			return game.religions.orthodox.hq_kingdom;
		case "is_byzantium":
			return game.religions.orthodox.hq_kingdom == this;
		case "constantinople_kingdom":
			return game.religions.orthodox.hq_realm.GetKingdom();
		case "is_constantinople_kingdom":
			return game.religions.orthodox.hq_realm.GetKingdom() == this;
		case "is_not_constantinople_kingdom":
			return game.religions.orthodox.hq_realm.GetKingdom() != this;
		case "relation_with_constantinople_kingdom":
			return GetRelationship(game.religions.orthodox.hq_realm.GetKingdom());
		case "is_mecca_kingdom":
			return realms.Find((Realm r) => r.name == "Al_Maqqah") != null;
		case "is_caliphate":
			return IsCaliphate();
		case "jihad_target":
			return jihad_target;
		case "jihad_attacker":
			return jihad_attacker;
		case "religion_mods_text":
			if (Religion.get_religion_mods_text == null)
			{
				return Value.Null;
			}
			return "#" + Religion.get_religion_mods_text(this);
		case "pope_bonuses_text":
			if (Religion.get_pope_bonuses_text == null)
			{
				return Value.Null;
			}
			return "#" + Religion.get_pope_bonuses_text(game.religions.catholic.head_kingdom);
		case "patriarch_bonuses_text":
			if (Religion.get_patriarch_bonuses_text == null)
			{
				return Value.Null;
			}
			return "#" + Religion.get_patriarch_bonuses_text(this);
		case "pagan_traditions_count":
			return pagan_beliefs?.Count ?? 0;
		case "perc_realms_same_religon":
			return PrecRealmsSameReligon();
		case "food":
			return (float)Math.Round(GetFood());
		case "food_produced":
			return (float)Math.Round(income[ResourceType.Food]) - foodFromImport;
		case "food_income":
			return (float)Math.Round(income[ResourceType.Food]);
		case "food_expenses":
			return (float)Math.Round(expenses[ResourceType.Food]);
		case "food_production_increase_perc":
			return GetStat(Stats.ks_food_production_perc);
		case "food_import_amount":
			return foodFromImport;
		case "food_import_gold":
			return 0f - upkeepGoldFromFoodImport;
		case "food_import_commerse_upkeep":
			return 0f - allocatedCommerceForImportFood;
		case "food_export_amount":
			return upkeepFoodFromExport;
		case "food_export_gold":
			return goldFromFoodExport;
		case "food_export_commerse_upkeep":
			return 0f - allocatedCommerceForExportFood;
		case "armies_upkeep":
			return armies_upkeep;
		case "armies_upkeep_food":
			return armies_upkeep[ResourceType.Food];
		case "armies_upkeep_gold":
			return armies_upkeep[ResourceType.Gold];
		case "garrison_upkeep":
			return GetGarrisonUpkeep();
		case "garrison_upkeep_food":
			return GetGarrisonUpkeep()[ResourceType.Food];
		case "sufficent_food":
			return GetSufficentFoodMod() * 100f;
		case "own_trade_center":
			return GetOwnTradeCentersCount();
		case "trade_centers_gold":
			return untaxGoldFromTradeCenters;
		case "num_trade_centers_zone_realms":
			return TradeCentersZoneRealmsCount();
		case "trade_centers_appeal_sum":
			return TradeCentersAppealSum();
		case "trade_centers_count":
			return GetTradeCentersCount();
		case "coastal_realms_count":
			return GetCoastalRealmsCount();
		case "holy_lands_count":
			return GetHolyLandsCount();
		case "kingdom_gold_income":
			return income.Get(ResourceType.Gold);
		case "kingdom_gold_expenses":
			return expenses.Get(ResourceType.Gold);
		case "kingdom_gold_balance":
			return income.Get(ResourceType.Gold) - expenses.Get(ResourceType.Gold);
		case "kingdom_piety_income":
			return income.Get(ResourceType.Piety);
		case "kingdom_piety_expenses":
			return expenses.Get(ResourceType.Piety);
		case "kingdom_piety_balance":
			return income.Get(ResourceType.Piety) - expenses.Get(ResourceType.Piety);
		case "kingdom_books_income":
			return income.Get(ResourceType.Books);
		case "kingdom_books_expenses":
			return expenses.Get(ResourceType.Books);
		case "kingdom_books_balance":
			return income.Get(ResourceType.Books) - expenses.Get(ResourceType.Books);
		case "kingdom_levy_income":
			return income.Get(ResourceType.Levy);
		case "rebellion_risk_average":
			return GetAvgRebellionRiskLocal();
		case "opinions":
			return opinions;
		case "challenges":
			return new Value(challenges);
		case "has_stability_factors":
			if (stability == null)
			{
				return false;
			}
			return stability.HasFactors();
		case "stability":
			return GetRebellionRiskGlobal();
		case "stability_religous_bonuses":
			return (stability == null) ? 0f : stability.GetStability("religous_bonuses");
		case "stability_taxes":
			return (stability == null) ? 0f : stability.GetStability("taxes");
		case "stability_wars":
			return (stability == null) ? 0f : stability.GetStability("wars");
		case "stability_dead_king":
			return (stability == null) ? 0f : stability.GetStability("dead_king");
		case "stability_hunger":
			return (stability == null) ? 0f : stability.GetStability("hunger");
		case "stability_crown_authority":
			return (stability == null) ? 0f : stability.GetStability("crown_authority");
		case "stability_traditions":
			return (stability == null) ? 0f : stability.GetStability("traditions");
		case "stability_stability":
			return (stability == null) ? 0f : stability.GetStability("stability");
		case "stability_cleric":
			return (stability == null) ? 0f : stability.GetStability("cleric");
		case "stability_opinions":
			return (stability == null) ? 0f : stability.GetStability("opinions");
		case "stability_rebel_leaders":
			return (stability == null) ? 0f : stability.GetStability("rebel_leaders");
		case "stability_defeated_rebels":
			return (stability == null) ? 0f : stability.GetStability("defeated_rebels");
		case "stability_rel_difference":
			return (stability == null) ? 0f : stability.GetStability("religious_differences");
		case "stability_cul_difference":
			return (stability == null) ? 0f : stability.GetStability("cultural_differences");
		case "stability_own_spies":
			return (stability == null) ? 0f : stability.GetStability("own_spies");
		case "num_rebel_armies":
			return GetRebelArmiesCount();
		case "stability_positives":
			return GetStabilityTotalPositives();
		case "stability_negatives":
			return GetStabilityTotalNegatives();
		case "is_bankrupt":
			return IsBankrupt();
		case "num_realms_with_majority":
			return GetRealmsWithMajorityCount();
		case "num_realms_without_majority":
			return realms.Count - GetRealmsWithMajorityCount();
		case "num_rebellions":
			return rebellions.Count;
		case "rebellions_strength":
			return CalcRebellionsStrength();
		case "allocated_commerce":
			return GetAllocatedCommerce();
		case "buildings_count":
			return GetNumBuildings();
		case "buildings_upkeep":
			return GetBuildingsUpkeep();
		case "buildings_commerce_upkeep":
			return allocatedCommerceForBuildings;
		case "buildings_commerce_upkeep_neg":
			return 0f - allocatedCommerceForBuildings;
		case "max_treasury":
			return GetMaxTreasury();
		case "diplomatic_gold_levels":
			return DiplomaticGoldLevels();
		case "S1":
			return GetDiplomaticGoldAmount(1);
		case "S2":
			return GetDiplomaticGoldAmount(2);
		case "S3":
			return GetDiplomaticGoldAmount(3);
		case "S4":
			return GetDiplomaticGoldAmount(4);
		case "S5":
			return GetDiplomaticGoldAmount(5);
		case "inflation":
			return inflation;
		case "inflation_perc":
			return CalcInflationPerc();
		case "avarage_army_morale":
			return GetAvarageArmyMorale();
		case "num_universities":
			return GetBuildingCount("University");
		case "king_warfare_ability":
		{
			Character king5 = GetKing();
			if (king5 != null)
			{
				return king5.GetKingAbility("Warfare");
			}
			return 0;
		}
		case "king_economy_ability":
		{
			Character king4 = GetKing();
			if (king4 != null)
			{
				return king4.GetKingAbility("Economy");
			}
			return 0;
		}
		case "king_diplomacy_ability":
		{
			Character king3 = GetKing();
			if (king3 != null)
			{
				return king3.GetKingAbility("Diplomacy");
			}
			return 0;
		}
		case "king_religion_ability":
		{
			Character king2 = GetKing();
			if (king2 != null)
			{
				return king2.GetKingAbility("Religion");
			}
			return 0;
		}
		case "king_espionage_ability":
		{
			Character king = GetKing();
			if (king != null)
			{
				return king.GetKingAbility("Espionage");
			}
			return 0;
		}
		case "upkeep_help_the_weak":
			return upkeepHelpTheWeak;
		case "has_required_victory_fame":
			return fame >= required_fame_victory;
		case "game_is_paused":
			return game.IsPaused();
		case "disconnected_time":
			if (IsAuthority())
			{
				return game.pings.TimeSinceLastPing(id);
			}
			return game.pings.TimeSinceLastPong();
		case "fame_obj":
			return fameObj;
		case "prestige_obj":
			return prestigeObj;
		case "crown_authority_obj":
			return GetCrownAuthority();
		case "is_rebel":
			return IsRebelKingdom();
		case "king_title":
			return nobility_key + ".King";
		case "queen_title":
			return nobility_key + ".Queen";
		case "generations":
			return generationsPassed;
		case "sum_court_levels":
			return SumCourtLevels();
		case "sum_princes_levels":
			return SumPrincesLevels();
		case "num_adopted_traditions":
			return NumTraditions();
		case "num_free_tradition_slots":
			return NumFreeTraditionSlots();
		case "num_disorder_realms":
			return DisorderRealmsCount();
		case "num_occupied_realms":
			return OccupiedRealmsCount();
		case "num_settlements":
			return SettlementsCount();
		case "eotw_min_great_powers":
			return game.emperorOfTheWorld?.def.min_great_powers ?? 0;
		case "eotw_enough_great_powers":
		{
			int num9 = game.emperorOfTheWorld?.def.min_great_powers ?? 0;
			return (game.great_powers?.TopKingdoms()?.Count ?? 0) >= num9;
		}
		case "average_war_score":
			return GetAverageWarScore();
		case "max_books":
			return (int)GetStat(Stats.ks_max_books);
		case "max_piety":
			return (int)GetStat(Stats.ks_max_piety);
		case "max_commerce":
			return (int)GetMaxCommerce();
		case "max_levy":
			return (int)GetStat(Stats.ks_max_levy);
		case "is_local_player":
			return is_local_player;
		case "local_player_kingdom":
			return game.GetLocalPlayerKingdom();
		case "is_ally_of_local_player":
			return IsAlly(game.GetLocalPlayerKingdom());
		case "local_player_kingdom_id":
			return game.GetLocalPlayerKingdomId();
		case "is_defeated":
			return IsDefeated();
		case "defeated_by":
			return defeated_by;
		case "random_weighted_rebellious_realm":
			return GetWeightedRebelliosRealm();
		case "corruption":
			return percCorruption;
		case "has_puppet_pope":
		{
			for (int num8 = 0; num8 < court.Count; num8++)
			{
				if (court[num8] != null && court[num8].HasPuppetPope())
				{
					return true;
				}
			}
			return false;
		}
		case "leads_all_rankings_politics":
			return LeadsAllRankings("Politics");
		case "leads_all_rankings_society":
			return LeadsAllRankings("Society");
		case "leads_all_rankings_wealth":
			return LeadsAllRankings("Wealth");
		case "leads_all_rankings_culture":
			return LeadsAllRankings("Culture");
		case "leads_all_rankings_conquest":
			return LeadsAllRankings("Conquest");
		case "leads_all_rankings":
			return LeadsAllRankings();
		case "leads_any_ranking":
			return LeadsAnyRanking();
		case "morale_from_relatives":
		{
			if (royalFamily?.Relatives == null)
			{
				return 0;
			}
			float num6 = 0f;
			for (int num7 = 0; num7 < royalFamily.Relatives.Count; num7++)
			{
				Character character4 = royalFamily.Relatives[num7];
				num6 += character4.GetStat(Stats.cs_important_relative_marshal_morale_bonus);
			}
			return num6;
		}
		case "influence_from_relatives":
		{
			if (royalFamily?.Relatives == null)
			{
				return 0;
			}
			float num4 = 0f;
			for (int num5 = 0; num5 < royalFamily.Relatives.Count; num5++)
			{
				Character character3 = royalFamily.Relatives[num5];
				num4 += character3.GetStat(Stats.cs_important_relative_diplomat_influence_bonus);
			}
			return num4;
		}
		case "espionage_defense_from_relatives":
		{
			if (royalFamily?.Relatives == null)
			{
				return 0;
			}
			float num3 = 0f;
			for (int n = 0; n < royalFamily.Relatives.Count; n++)
			{
				Character character2 = royalFamily.Relatives[n];
				num3 += character2.GetStat(Stats.cs_important_relative_spy_espionage_defense_bonus);
			}
			return num3;
		}
		case "espionage_defense_from_spy_governed_keeps":
		{
			if (court == null)
			{
				return 0;
			}
			float num2 = 0f;
			for (int l = 0; l < court.Count; l++)
			{
				Character character = court[l];
				if (character == null || !character.IsSpy() || character.governed_castle == null)
				{
					continue;
				}
				Realm realm = character.governed_castle.GetRealm();
				for (int m = 0; m < realm.settlements.Count; m++)
				{
					if (realm.settlements[m].type == "Keep")
					{
						num2 += character.GetStat(Stats.cs_spy_governor_espionage_defense_bonus_per_keep);
					}
				}
			}
			return num2;
		}
		case "has_all_advantages":
			return HasAllAdvatages();
		case "ai_expenses_log":
			return ai?.expenses_log?.ToString();
		case "num_spies":
		{
			int num = 0;
			for (int k = 0; k < court.Count; k++)
			{
				if (court[k] != null && court[k].IsSpy())
				{
					num++;
				}
			}
			return num;
		}
		case "culture_bolster_cleric":
			return GetCultureBolsterCleric();
		case "influence_bolster_diplomat":
			return GetInfluenceBolsterDiplomat();
		case "is_supporter_against_player":
		{
			Kingdom localPlayerKingdom = game.GetLocalPlayerKingdom();
			War war2 = FindWarWith(localPlayerKingdom);
			return (war2?.GetSupporters(war2.GetEnemyLeader(localPlayerKingdom)))?.Contains(this) ?? false;
		}
		case "keeps":
			return GetNumberOfKeeps();
		default:
		{
			Religion religion = game.religions.Get(key);
			if (religion != null)
			{
				return this.religion == religion;
			}
			if (stats != null)
			{
				Value var = stats.GetVar(key, vars, as_value);
				if (!var.is_unknown)
				{
					return var;
				}
			}
			string text = "has_pagan_belief_";
			if (pagan_beliefs != null && key.StartsWith(text, StringComparison.Ordinal))
			{
				string text2 = key.Substring(text.Length);
				foreach (Religion.PaganBelief pagan_belief in pagan_beliefs)
				{
					if (pagan_belief.name == text2)
					{
						return true;
					}
				}
				return false;
			}
			string text3 = "has_completed_building_";
			if (key.StartsWith(text3, StringComparison.Ordinal))
			{
				string text4 = key.Substring(text3.Length);
				Building.Def def = game.defs.Find<Building.Def>(text4);
				for (int i = 0; i < realms.Count; i++)
				{
					Building building = realms[i].castle.FindBuilding(def);
					if (building != null && building.CalcLevel() == 3)
					{
						return true;
					}
				}
				return false;
			}
			if (opinions != null)
			{
				Value var2 = opinions.GetVar(key, vars, as_value);
				if (!var2.is_unknown)
				{
					return var2;
				}
			}
			DT.Field field = game.dt.Find(key);
			if (field != null && field.def != null)
			{
				if (all_tags >= 0)
				{
					return all_tags;
				}
				UpdateRealmTags();
				if (realm_tags.TryGetValue(key, out var value))
				{
					return value;
				}
			}
			return base.GetVar(key, vars, as_value);
		}
		}
	}

	public override void OnEvent(Event evt)
	{
		switch (evt.id)
		{
		case "offer_answered":
			if (evt.param is Offer offer)
			{
				if (offer.outcomes == null && evt.outcomes != null)
				{
					offer.outcomes = evt.outcomes;
					offer.outcome_vars = evt.vars;
					offer.unique_outcomes = OutcomeDef.UniqueOutcomes(offer.outcomes);
				}
				if (offer.from == this && offer.to != null)
				{
					offer.to.NotifyListeners("offer_answered", offer);
				}
				else if (offer.to == this && offer.from != null)
				{
					offer.from.NotifyListeners("offer_answered", offer);
				}
			}
			break;
		case "unlock_achievement":
			if (evt.param is string achievement && is_local_player)
			{
				game.stats.SetAchievement(achievement);
			}
			break;
		case "achievement_proggress":
			if (evt.param is string name && evt.vars != null)
			{
				int num = evt.vars.Get("val").Int();
				if (num != 0 && is_local_player)
				{
					game.stats.IncIntStat(name, num);
				}
			}
			break;
		}
		base.OnEvent(evt);
	}

	public int RecalcBuildingStates(Castle origin = null, bool remove_abandoned = false, bool log_changed = false, bool log_timings = false)
	{
		if (!base.started || IsDefeated())
		{
			return 0;
		}
		if (game == null || game.IsUnloadingMap())
		{
			return 0;
		}
		if (!CacheRBS.Request(this, origin, remove_abandoned))
		{
			return 0;
		}
		if (in_RecalcBuildingStates)
		{
			Game.Log("RecalcBuildingStates: reentrant call!", Game.LogType.Error);
		}
		RefreshUpgrades();
		in_RecalcBuildingStates = true;
		int num = 0;
		using (Game.Profile("RecalcBuildingStates", log_timings))
		{
			using (Game.Profile("RecalcBuildingStates.InitResourceProduction", log_timings))
			{
				InitResourceProduction();
			}
			using (Game.Profile("RecalcBuildingStates.CalcBuildingStates", log_timings))
			{
				ClearTempBuildingStates();
				for (int i = 0; i < realms.Count; i++)
				{
					Castle castle = realms[i]?.castle;
					if (castle != null)
					{
						RecalcBuildingStates(castle.buildings);
						RecalcBuildingStates(castle.upgrades);
					}
				}
			}
			using (Game.Profile("RecalcBuildingStates.FinalizeBuildingStates", log_timings))
			{
				temp_castles_changed.Clear();
				if (origin != null)
				{
					temp_castles_changed.Add(origin);
				}
				num = FinalizeBuildingStates(remove_abandoned, log_changed);
			}
			using (Game.Profile("RecalcBuildingStates.InvalidateIncomes", log_timings))
			{
				if (origin != null || num != 0)
				{
					InvalidateIncomes();
				}
			}
			using (Game.Profile("RecalcBuildingStates.RecalcRebellionRisk", log_timings))
			{
				for (int j = 0; j < temp_castles_changed.Count; j++)
				{
					temp_castles_changed[j]?.GetRealm()?.rebellionRisk?.Recalc(think_rebel_pop: false, allow_rebel_spawn: false);
				}
			}
		}
		in_RecalcBuildingStates = false;
		using (Game.Profile("RecalcBuildingStates.CalcMisssingResources", log_timings))
		{
			CalcMisssingResources();
			return num;
		}
	}

	private static void ClearTempBuildingStates()
	{
		if (++building_state_recalcs == 0)
		{
			building_state_recalcs = 1;
		}
	}

	private void InitResourceProduction()
	{
		temp_resource_production.Clear();
		if (court != null)
		{
			for (int i = 0; i < court.Count; i++)
			{
				Character c = court[i];
				AddImportedGoodsAsProducer(c);
			}
		}
		if (realms == null)
		{
			return;
		}
		for (int j = 0; j < realms.Count; j++)
		{
			Castle castle = realms[j]?.castle;
			if (castle != null)
			{
				AddProducers(castle.buildings);
				AddProducers(castle.upgrades);
			}
		}
	}

	private void AddProducers(List<Building> buildings)
	{
		if (buildings == null)
		{
			return;
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			Building building = buildings[i];
			if (building != null)
			{
				AddProducer(building);
			}
		}
	}

	private void AddProducer(Building b)
	{
		if (b?.def?.produces != null)
		{
			for (int i = 0; i < b.def.produces.Count; i++)
			{
				AddProducer(b.def.produces[i].resource, b);
			}
		}
		if (b?.def?.produces_completed != null)
		{
			for (int j = 0; j < b.def.produces_completed.Count; j++)
			{
				AddCompletedProducer(b.def.produces_completed[j].resource, b);
			}
		}
	}

	private void AddProducer(string resource, Building b)
	{
		temp_resource_production.TryGetValue(resource, out var value);
		if (value.producers == null)
		{
			value.producers = new List<Building>();
		}
		value.producers.Add(b);
		temp_resource_production[resource] = value;
	}

	private void AddCompletedProducer(string resource, Building b)
	{
		temp_resource_production.TryGetValue(resource, out var value);
		if (value.producers_completed == null)
		{
			value.producers_completed = new List<Building>();
		}
		value.producers_completed.Add(b);
		temp_resource_production[resource] = value;
	}

	private void AddImporterProducer(string resource, Character c)
	{
		temp_resource_production.TryGetValue(resource, out var value);
		if (value.importers == null)
		{
			value.importers = new List<Character>();
		}
		value.importers.Add(c);
		temp_resource_production[resource] = value;
	}

	private void AddImportedGoodsAsProducer(Character c)
	{
		if (c?.importing_goods == null)
		{
			return;
		}
		for (int i = 0; i < c.importing_goods.Count; i++)
		{
			Character.ImportedGood importedGood = c.importing_goods[i];
			if (!string.IsNullOrEmpty(importedGood.name))
			{
				AddImporterProducer(importedGood.name, c);
			}
		}
	}

	private int RecalcProducerStates(string resource)
	{
		if (!temp_resource_production.TryGetValue(resource, out var value))
		{
			return 0;
		}
		if (value.calculated < 0)
		{
			Error("Infinite loop while calculating producer states of resource '" + resource + "'");
			return 0;
		}
		if (value.calculated > 0)
		{
			return value.amount;
		}
		value.calculated = -1;
		temp_resource_production[resource] = value;
		if (value.producers != null)
		{
			for (int i = 0; i < value.producers.Count; i++)
			{
				Building building = value.producers[i];
				if (RecalcBuildingState(building) == Building.State.Working)
				{
					int producesAmount = building.def.GetProducesAmount(resource);
					value.amount += producesAmount;
				}
			}
		}
		if (value.producers_completed != null)
		{
			for (int j = 0; j < value.producers_completed.Count; j++)
			{
				Building building2 = value.producers_completed[j];
				if (building2.CalcCompleted())
				{
					int producesCompletedAmount = building2.def.GetProducesCompletedAmount(resource);
					value.amount += producesCompletedAmount;
				}
			}
		}
		if (value.importers != null)
		{
			value.amount += value.importers.Count;
		}
		value.calculated = 1;
		temp_resource_production[resource] = value;
		return value.amount;
	}

	public Building.State RecalcBuildingState(Building b)
	{
		if (b.GetTempState(out var state))
		{
			if (state == Building.State.Invalid)
			{
				Error($"Infinite loop while calculating state of building {b}");
			}
			return state;
		}
		if (b.state < Building.State.Abandoned)
		{
			return b.state;
		}
		b.SetTempState(Building.State.Invalid);
		state = CalcBuildingState(b);
		b.SetTempState(state);
		return state;
	}

	public bool AddMissingResourceBuilding(string resource, Building building)
	{
		bool result = false;
		if (!missing_resources.TryGetValue(resource, out var value))
		{
			value = new HashSet<Building>();
			missing_resources.Add(resource, value);
			result = true;
		}
		if (!value.Contains(building))
		{
			value.Add(building);
		}
		return result;
	}

	public bool DelMissingResourceBuilding(string resource, Building building)
	{
		if (!missing_resources.TryGetValue(resource, out var value))
		{
			return false;
		}
		if (!value.Contains(building))
		{
			return false;
		}
		value.Remove(building);
		if (value.Count > 0)
		{
			return false;
		}
		missing_resources.Remove(resource);
		return true;
	}

	public bool DelMissingResourcesBuilding(Building building)
	{
		tmp_resource_names.Clear();
		foreach (KeyValuePair<string, HashSet<Building>> missing_resource in missing_resources)
		{
			string key = missing_resource.Key;
			tmp_resource_names.Add(key);
		}
		bool result = false;
		for (int i = 0; i < tmp_resource_names.Count; i++)
		{
			string resource = tmp_resource_names[i];
			if (DelMissingResourceBuilding(resource, building))
			{
				result = true;
			}
		}
		return result;
	}

	private void CalcMisssingResources()
	{
		bool flag = false;
		tmp_buildings.Clear();
		foreach (KeyValuePair<string, HashSet<Building>> missing_resource in missing_resources)
		{
			_ = missing_resource.Key;
			HashSet<Building> value = missing_resource.Value;
			if (value == null)
			{
				continue;
			}
			foreach (Building item in value)
			{
				if (item?.castle?.GetKingdom() != this)
				{
					tmp_buildings.Add(item);
				}
			}
		}
		for (int i = 0; i < tmp_buildings.Count; i++)
		{
			Building building = tmp_buildings[i];
			if (DelMissingResourcesBuilding(building))
			{
				flag = true;
			}
		}
		for (int j = 0; j < realms.Count; j++)
		{
			Realm realm = realms[j];
			Castle castle = realm?.castle;
			if (castle == null)
			{
				continue;
			}
			bool in_disorder = realm.IsDisorder();
			for (int k = 0; k < castle.buildings.Count; k++)
			{
				Building building2 = castle.buildings[k];
				if (CalcBuilding(building2, in_disorder))
				{
					flag = true;
				}
			}
			for (int l = 0; l < castle.upgrades.Count; l++)
			{
				Building building3 = castle.upgrades[l];
				if (CalcBuilding(building3, in_disorder))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			NotifyListeners("buildings_missing_resources_change");
		}
		bool CalcBuilding(Building building4, bool flag2)
		{
			if (building4 == null)
			{
				return false;
			}
			bool result = false;
			if (building4.state == Building.State.Stalled || building4.state == Building.State.Abandoned)
			{
				if (building4.def.requires != null)
				{
					for (int m = 0; m < building4.def.requires.Count; m++)
					{
						Building.Def.RequirementInfo requirementInfo = building4.def.requires[m];
						if (requirementInfo.type == "Religion" && !building4.def.CheckReligionRequirements(GetKingdom()))
						{
							break;
						}
						if (!(requirementInfo.type != "Resource"))
						{
							if (!flag2 && !RecalcBuildingRequirement(requirementInfo))
							{
								if (AddMissingResourceBuilding(requirementInfo.key, building4))
								{
									result = true;
								}
							}
							else if (DelMissingResourceBuilding(requirementInfo.key, building4))
							{
								result = true;
							}
						}
					}
				}
				if (building4.def.requires_or != null)
				{
					tmp_resource_names.Clear();
					bool flag3 = false;
					for (int n = 0; n < building4.def.requires_or.Count; n++)
					{
						Building.Def.RequirementInfo requirementInfo2 = building4.def.requires_or[n];
						if (requirementInfo2.type == "Religion" && !building4.def.CheckReligionRequirements(GetKingdom()))
						{
							flag3 = true;
							break;
						}
						if (!(requirementInfo2.type != "Resource"))
						{
							if (!RecalcBuildingRequirement(requirementInfo2))
							{
								tmp_resource_names.Add(requirementInfo2.key);
							}
							else
							{
								flag3 = (byte)((flag3 ? 1u : 0u) | 1u) != 0;
							}
						}
					}
					if (!flag2 && !flag3 && tmp_resource_names.Count > 0)
					{
						for (int num = 0; num < tmp_resource_names.Count; num++)
						{
							string resource = tmp_resource_names[num];
							if (AddMissingResourceBuilding(resource, building4))
							{
								result = true;
							}
						}
					}
					else if (DelMissingResourcesBuilding(building4))
					{
						result = true;
					}
				}
			}
			if (building4.state == Building.State.Working && DelMissingResourcesBuilding(building4))
			{
				result = true;
			}
			return result;
		}
	}

	private Building.State CalcBuildingState(Building b)
	{
		if (b.def.IsUpgrade())
		{
			Kingdom kingdom = b.castle?.GetKingdom();
			if (kingdom == null || !kingdom.HasBuildingUpgrade(b.def))
			{
				return Building.State.Abandoned;
			}
		}
		Building.State state = RecalcPrerequisites(b);
		if (state != Building.State.Working)
		{
			return state;
		}
		if (CalcStalled(b))
		{
			return Building.State.Stalled;
		}
		return Building.State.Working;
	}

	private Building.State RecalcPrerequisites(Building b, District.Def district)
	{
		Building.Def parent = district.GetParent();
		if (parent != null && b.castle != null)
		{
			Building building = b.castle.FindBuilding(parent);
			if (building == null)
			{
				return Building.State.Abandoned;
			}
			if (RecalcBuildingState(building) <= Building.State.Abandoned)
			{
				return Building.State.Abandoned;
			}
		}
		Building.State result = Building.State.Working;
		List<Building.Def> prerequisites = b.def.GetPrerequisites(district);
		if (prerequisites != null)
		{
			for (int i = 0; i < prerequisites.Count; i++)
			{
				Building.Def def = prerequisites[i];
				Building building2 = b.castle.FindBuilding(def);
				if (building2 == null || !building2.IsBuilt())
				{
					return Building.State.Abandoned;
				}
				switch (RecalcBuildingState(building2))
				{
				case Building.State.Abandoned:
					return Building.State.Abandoned;
				case Building.State.Working:
					continue;
				}
				result = Building.State.Stalled;
			}
		}
		List<Building.Def> prerequisitesOr = b.def.GetPrerequisitesOr(district);
		if (prerequisitesOr != null)
		{
			Building.State state = Building.State.Abandoned;
			for (int j = 0; j < prerequisitesOr.Count; j++)
			{
				Building.Def def2 = prerequisitesOr[j];
				Building building3 = b.castle.FindBuilding(def2);
				if (building3 != null && building3.IsBuilt())
				{
					Building.State state2 = RecalcBuildingState(building3);
					if (state2 > state)
					{
						state = state2;
					}
				}
			}
			switch (state)
			{
			case Building.State.Abandoned:
				return Building.State.Abandoned;
			default:
				result = Building.State.Stalled;
				break;
			case Building.State.Working:
				break;
			}
		}
		return result;
	}

	private Building.State RecalcPrerequisites(Building b)
	{
		if (b.castle == null)
		{
			return Building.State.Abandoned;
		}
		if (b.def.districts == null)
		{
			return Building.State.Working;
		}
		Building.State state = Building.State.Working;
		for (int i = 0; i < b.def.districts.Count; i++)
		{
			District.Def district = b.def.districts[i];
			Building.State state2 = RecalcPrerequisites(b, district);
			if (state2 < state)
			{
				state = state2;
			}
		}
		return state;
	}

	private bool CalcStalled(Building b)
	{
		Realm realm = b.castle.GetRealm();
		if (realm.IsOccupied() || realm.IsDisorder())
		{
			return true;
		}
		if (b.def.requires != null)
		{
			for (int i = 0; i < b.def.requires.Count; i++)
			{
				Building.Def.RequirementInfo req = b.def.requires[i];
				if (!RecalcBuildingRequirement(req))
				{
					return true;
				}
			}
		}
		if (b.def.requires_or != null)
		{
			for (int j = 0; j < b.def.requires_or.Count; j++)
			{
				Building.Def.RequirementInfo req2 = b.def.requires_or[j];
				if (RecalcBuildingRequirement(req2))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public bool HasMissingGoods()
	{
		return missing_resources.Count > 0;
	}

	public List<string> GetMissingGoods()
	{
		return new List<string>(missing_resources.Keys);
	}

	public bool HasRealm(string name)
	{
		return realms.Exists((Realm r) => r.name == name);
	}

	private bool RecalcBuildingRequirement(Building.Def.RequirementInfo req)
	{
		if (req.type != "Resource")
		{
			return true;
		}
		int num = ((all_tags >= 0) ? all_tags : RecalcProducerStates(req.key));
		if (num <= 0)
		{
			return false;
		}
		if (num < req.amount)
		{
			return false;
		}
		return true;
	}

	private void RecalcBuildingStates(List<Building> buildings)
	{
		if (buildings == null)
		{
			return;
		}
		for (int i = 0; i < buildings.Count; i++)
		{
			Building building = buildings[i];
			if (building != null)
			{
				RecalcBuildingState(building);
			}
		}
	}

	private int FinalizeBuildingStates(List<Building> buildings, bool remove_abandoned, bool log)
	{
		if (buildings == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < buildings.Count; i++)
		{
			Building building = buildings[i];
			if (building == null)
			{
				continue;
			}
			Castle castle = building.castle;
			if (castle == null || !building.GetTempState(out var state))
			{
				continue;
			}
			if (state == building.state)
			{
				if (state != Building.State.Working)
				{
					continue;
				}
				bool flag = building.def.CalcLevel(castle) != building.applied_level;
				if (!flag && !building.def.has_conditional_bonuses)
				{
					continue;
				}
				building.SetState(Building.State.TemporaryDeactivated);
				if (!flag)
				{
					building.SetState(Building.State.Working);
					InvalidateIncomes();
					continue;
				}
			}
			num++;
			if (log)
			{
				Game.Log($"{building.castle.name}.{building.def.id}: {building.state} -> {state}", Game.LogType.Message);
			}
			if (state == Building.State.Abandoned && remove_abandoned)
			{
				castle.RemoveBuilding(building, Castle.BuildingRemovalMode.Abandoned);
			}
			else if (state == Building.State.Abandoned && building.def.IsUpgrade())
			{
				castle.RemoveBuilding(building, Castle.BuildingRemovalMode.Upgrade);
			}
			else
			{
				building.SetState(state);
			}
		}
		return num;
	}

	private int FinalizeBuildingStates(bool remove_abandoned, bool log)
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			Castle castle = realms[i].castle;
			if (castle != null)
			{
				int num2 = FinalizeBuildingStates(castle.buildings, remove_abandoned, log);
				num2 += FinalizeBuildingStates(castle.upgrades, remove_abandoned, log);
				if (num2 != 0)
				{
					num += num2;
					temp_castles_changed.Add(castle);
				}
			}
		}
		for (int j = 0; j < temp_castles_changed.Count; j++)
		{
			Castle castle2 = temp_castles_changed[j];
			castle2.SendState<Castle.StructuresState>();
			castle2.NotifyListeners("structures_changed");
			castle2.GetRealm()?.RefreshTags();
		}
		return num;
	}

	public Vassalage.Def GetVassalageByType(Vassalage.Type type)
	{
		List<Vassalage.Def> defs = game.defs.GetDefs<Vassalage.Def>();
		if (defs == null)
		{
			return null;
		}
		for (int i = 0; i < defs.Count; i++)
		{
			Vassalage.Def def = defs[i];
			if (def.type == type)
			{
				return def;
			}
		}
		return null;
	}

	public Tradition GetTradition(int idx)
	{
		if (idx < 0 || traditions == null || idx >= traditions.Count)
		{
			return null;
		}
		return traditions[idx];
	}

	public bool HasTradition(string id)
	{
		Tradition.Def def = game?.defs?.Get<Tradition.Def>(id);
		if (def == null)
		{
			return false;
		}
		return HasTradition(def);
	}

	public bool HasTradition(Tradition.Def def)
	{
		if (FindTradition(def) == null)
		{
			return false;
		}
		return true;
	}

	public int GetTraditionRank(Tradition.Def def)
	{
		return FindTradition(def)?.rank ?? 0;
	}

	public Tradition FindTradition(Tradition.Def def)
	{
		if (def == null)
		{
			return null;
		}
		if (traditions == null)
		{
			return null;
		}
		for (int i = 0; i < traditions.Count; i++)
		{
			Tradition tradition = traditions[i];
			if (tradition != null && tradition.def == def)
			{
				return tradition;
			}
		}
		return null;
	}

	public bool AddTradition(Tradition.Def tdef, int rank = 1, int slot_idx = -1)
	{
		if (!CanAddTradition(tdef))
		{
			return false;
		}
		int num = ((slot_idx == -1) ? GetFreeTraditionIndex(tdef.type) : slot_idx);
		if (num < 0)
		{
			return false;
		}
		if (GetTradition(num) != null)
		{
			return false;
		}
		return SetTradition(num, tdef, rank);
	}

	public void OnAddTraditionAnalytics(Tradition.Def tdef, Resource cost)
	{
		if (Game.isLoadingSaveGame || !AssertAuthority())
		{
			return;
		}
		Character character = null;
		if (court != null)
		{
			for (int i = 0; i < court.Count; i++)
			{
				if (tdef.IsGrantedBySkill(court[i]))
				{
					character = court[i];
					break;
				}
			}
		}
		if (character != null)
		{
			Vars vars = new Vars();
			vars.Set("traditionName", tdef.name);
			vars.Set("traditionSource", character.Name);
			if (cost != null)
			{
				vars.Set("goldCost", (int)cost[ResourceType.Gold]);
				vars.Set("bookCost", (int)cost[ResourceType.Books]);
			}
			else
			{
				vars.Set("goldCost", 0);
			}
			FireEvent("analytics_tradition_added", vars, id);
		}
	}

	public bool SetTradition(int idx, Tradition.Def tdef, int rank, bool refresh = true)
	{
		if (idx < 0)
		{
			return false;
		}
		if (traditions == null || idx >= traditions.Count)
		{
			if (tdef == null)
			{
				return false;
			}
			if (traditions == null)
			{
				traditions = new List<Tradition>();
			}
			while (traditions.Count <= tradition_slots_types.Length)
			{
				traditions.Add(null);
			}
		}
		traditions[idx]?.SetOwner(null);
		Tradition tradition;
		if (tdef != null)
		{
			tradition = new Tradition(tdef, rank);
			tradition.SetOwner(this);
		}
		else
		{
			tradition = null;
		}
		traditions[idx] = tradition;
		if (refresh)
		{
			RefreshTraditions();
		}
		NotifyListeners("new_tradition", tradition);
		return true;
	}

	public void ClearTraditions()
	{
		if (traditions != null)
		{
			for (int i = 0; i < traditions.Count; i++)
			{
				traditions[i]?.SetOwner(null);
			}
			traditions = null;
		}
	}

	public void RefreshRoyalChildrenSkills()
	{
		if (!AssertAuthority() || royalFamily?.Children == null)
		{
			return;
		}
		for (int i = 0; i < royalFamily.Children.Count; i++)
		{
			Character character = royalFamily.Children[i];
			if (character.ShouldForceSkillsToMaxRank())
			{
				character.ForceSkillsToMaxRank();
			}
		}
	}

	public void RefreshTraditions(bool send_state = true)
	{
		if (traditions != null)
		{
			for (int i = 0; i < traditions.Count; i++)
			{
				traditions[i]?.SetOwner(this, refresh_court: false);
			}
		}
		RefreshCourtSkills();
		if (IsAuthority())
		{
			RefreshRoyalChildrenSkills();
		}
		if (send_state)
		{
			SendState<TraditionsState>();
		}
		NotifyListeners("traditions_changed");
	}

	public int NumTraditions(Tradition.Type type = Tradition.Type.All)
	{
		if (traditions == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < traditions.Count; i++)
		{
			Tradition tradition = traditions[i];
			if (tradition?.def != null && tradition.def.MatchTrditionType(type))
			{
				num++;
			}
		}
		return num;
	}

	public bool IsTraditionSlotUnlocked(int slot_index)
	{
		if (slot_index < 0)
		{
			return false;
		}
		int traditionSlotTier = GetTraditionSlotTier(slot_index);
		if (traditionSlotTier == 0)
		{
			return true;
		}
		int num = -1;
		for (int i = 0; i < tradition_slots_types.Length; i++)
		{
			if (GetTradition(i) != null)
			{
				num = i;
			}
		}
		if (slot_index < num)
		{
			return true;
		}
		if (IsTierFull(traditionSlotTier - 1))
		{
			return true;
		}
		return false;
		bool IsTierFull(int tier)
		{
			int num2 = 0;
			int num3 = 0;
			for (int j = 0; j < traditions_slots_tiers.Length; j++)
			{
				if (traditions_slots_tiers[j] == tier)
				{
					num2++;
					if (GetTradition(j) != null)
					{
						num3++;
					}
				}
			}
			return num2 == num3;
		}
	}

	public int NumFreeTraditionSlots(Tradition.Type type = Tradition.Type.All, bool check_fame = false)
	{
		int num = 0;
		for (int i = 0; i < tradition_slots_types.Length && IsTraditionSlotUnlocked(i); i++)
		{
			if (IsUnusedTraditionSlot(i, type))
			{
				num++;
			}
		}
		return num;
	}

	private bool IsUnusedTraditionSlot(int index, Tradition.Type type)
	{
		if (tradition_slots_types[index] == type || tradition_slots_types[index] == Tradition.Type.All)
		{
			return GetTradition(index) == null;
		}
		return false;
	}

	public int GetFreeTraditionIndex(Tradition.Type type)
	{
		if (type == Tradition.Type.Unknown)
		{
			return -1;
		}
		for (int i = 0; i < tradition_slots_types.Length; i++)
		{
			if (IsUnusedTraditionSlot(i, type))
			{
				return i;
			}
		}
		return -1;
	}

	public int GetTraditionSlotTier(int slot_index)
	{
		if (slot_index < 0)
		{
			return 0;
		}
		if (traditions_slots_tiers == null || traditions_slots_tiers.Length == 0)
		{
			return 0;
		}
		if (slot_index >= traditions_slots_tiers.Length)
		{
			slot_index = traditions_slots_tiers.Length - 1;
		}
		return traditions_slots_tiers[slot_index];
	}

	public int MaxTraditions(Tradition.Type type = Tradition.Type.All)
	{
		switch (type)
		{
		case Tradition.Type.Unknown:
			return 0;
		case Tradition.Type.All:
			return tradition_slots_types.Length;
		default:
		{
			int num = 0;
			for (int i = 0; i < tradition_slots_types.Length; i++)
			{
				if (tradition_slots_types[i] == type)
				{
					num++;
				}
			}
			return num;
		}
		}
	}

	public bool HasFreeTraditionSlot(Tradition.Type type = Tradition.Type.All)
	{
		return NumTraditions(type) < MaxTraditions(type);
	}

	public bool CanAddTradition(Tradition.Type type = Tradition.Type.All)
	{
		return GetFreeTraditionIndex(type) != -1;
	}

	public bool CanAddTradition(Tradition.Def tdef)
	{
		if (tdef == null)
		{
			return false;
		}
		if (HasTradition(tdef))
		{
			return false;
		}
		if (!CanAddTradition(tdef.type))
		{
			return false;
		}
		if (!CheckTraditionRequirements(tdef))
		{
			return false;
		}
		return true;
	}

	public bool CanUpgradeTradition(Tradition.Def tdef)
	{
		if (traditions == null)
		{
			return false;
		}
		for (int i = 0; i < traditions.Count; i++)
		{
			Tradition tradition = traditions[i];
			if (tradition != null && tradition.def == tdef)
			{
				return tradition.rank < tradition.def.max_rank;
			}
		}
		return false;
	}

	public bool CanAffordTradition(int slot_index)
	{
		if (slot_index < 0 || slot_index >= tradition_slots_types.Length)
		{
			return false;
		}
		Resource resource = null;
		Tradition tradition = GetTradition(slot_index);
		if (tradition != null)
		{
			resource = tradition.def.GetAdoptCost(this, slot_index);
		}
		if (resource == null)
		{
			Tradition.Def def = game.defs.GetBase<Tradition.Def>();
			if (def != null)
			{
				resource = def.GetAdoptCost(this, slot_index);
			}
		}
		return resources.CanAfford(resource, 1f);
	}

	public bool CheckTraditionRequirements(Tradition.Def tdef)
	{
		if (tdef == null)
		{
			return false;
		}
		if (tdef.IsGrantedBySkill(this))
		{
			return true;
		}
		return false;
	}

	public bool HasNewTraditionOptions(Tradition.Type type = Tradition.Type.All)
	{
		List<Tradition.Def> newTraditionOptions = GetNewTraditionOptions(type);
		if (newTraditionOptions != null)
		{
			return newTraditionOptions.Count > 0;
		}
		return false;
	}

	public List<Tradition.Def> GetNewTraditionOptions(Tradition.Type type = Tradition.Type.All)
	{
		if (!CanAddTradition(type))
		{
			return null;
		}
		List<Tradition.Def> defs = game.defs.GetDefs<Tradition.Def>();
		if (defs == null)
		{
			return null;
		}
		List<Tradition.Def> list = null;
		for (int i = 0; i < defs.Count; i++)
		{
			Tradition.Def def = defs[i];
			if (def.MatchTrditionType(type) && !HasTradition(def) && CheckTraditionRequirements(def))
			{
				if (list == null)
				{
					list = new List<Tradition.Def>();
				}
				if (!list.Contains(def))
				{
					list.Add(def);
				}
			}
		}
		return list;
	}

	public Building FindBuilding(Building.Def def)
	{
		if (def == null)
		{
			return null;
		}
		Building building = null;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.castle != null)
			{
				Building building2 = realm.castle.FindBuilding(def);
				if (building2 != null && (building == null || building2.state > building.state))
				{
					building = building2;
				}
			}
		}
		return building;
	}

	public Castle FindCastleToBuild(Building.Def def, bool ignore_cost = false)
	{
		if (def == null)
		{
			return null;
		}
		using (Game.Profile("Kingdom.FindCastleToBuild"))
		{
			for (int i = 0; i < realms.Count; i++)
			{
				Realm realm = realms[i];
				if (realm.castle != null && realm.castle.CanBuildBuilding(def, ignore_cost) == Castle.StructureBuildAvailability.Available)
				{
					return realm.castle;
				}
			}
		}
		return null;
	}

	public Castle.StructureBuildAvailability CalcBuildAvailability(Building.Def def, ref Castle best_castle, List<Castle> all_buildable = null, bool ignore_cost = false)
	{
		if (def == null)
		{
			return Castle.StructureBuildAvailability.MissingData;
		}
		using (Game.Profile("Kingdom.CalcBuildAvailability"))
		{
			Castle.StructureBuildAvailability structureBuildAvailability = Castle.StructureBuildAvailability.NoParentNoRequirements;
			if (best_castle != null)
			{
				structureBuildAvailability = best_castle.CanBuildBuilding(def);
			}
			for (int i = 0; i < realms.Count; i++)
			{
				Realm realm = realms[i];
				if (realm?.castle != null && realm.castle != best_castle)
				{
					Castle.StructureBuildAvailability structureBuildAvailability2 = realm.castle.CanBuildBuilding(def);
					if (best_castle == null || structureBuildAvailability2 < structureBuildAvailability)
					{
						best_castle = realm.castle;
						structureBuildAvailability = structureBuildAvailability2;
					}
					if (all_buildable != null && (structureBuildAvailability2 == Castle.StructureBuildAvailability.Available || (ignore_cost && structureBuildAvailability2 == Castle.StructureBuildAvailability.CannotAfford)))
					{
						all_buildable.Add(realm.castle);
					}
				}
			}
			return structureBuildAvailability;
		}
	}

	public bool CheckReligionRequirements(Building.Def def)
	{
		if (!CheckReligionRequirements(def.requires, or: false))
		{
			return false;
		}
		if (!CheckReligionRequirements(def.requires_or, or: true))
		{
			return false;
		}
		return true;
	}

	private bool CheckReligionRequirements(List<Building.Def.RequirementInfo> reqs, bool or)
	{
		if (reqs == null)
		{
			return true;
		}
		bool flag = false;
		for (int i = 0; i < reqs.Count; i++)
		{
			Building.Def.RequirementInfo requirementInfo = reqs[i];
			if (requirementInfo?.type != "Religion")
			{
				continue;
			}
			flag = true;
			if (religion != null && religion.HasTag(requirementInfo.key))
			{
				if (or)
				{
					return true;
				}
			}
			else if (!or)
			{
				return false;
			}
		}
		if (!or)
		{
			return true;
		}
		return !flag;
	}

	public bool CanUnlockBuildingUpgrade(Building.Def def, bool ignore_cost = false)
	{
		if (def == null || !def.IsUpgrade())
		{
			return false;
		}
		if (HasBuildingUpgrade(def))
		{
			return false;
		}
		if (FindCastleToBuild(def, ignore_cost) == null)
		{
			return false;
		}
		return true;
	}

	public bool UnlockBuildingUpgrade(Building.Def def, bool forced)
	{
		using (Game.Profile("Kingdom.UnlockBuildingUpgrade"))
		{
			if (def == null || !def.IsUpgrade())
			{
				return false;
			}
			if (HasBuildingUpgrade(def))
			{
				return false;
			}
			if (!forced)
			{
				Castle castle = FindCastleToBuild(def);
				if (castle == null)
				{
					return false;
				}
				castle.BuildBuilding(def);
				return true;
			}
			if (def.variant_of != null)
			{
				def = def.variant_of;
			}
			if (building_upgrades == null)
			{
				building_upgrades = new List<Building.Def>();
			}
			else if (building_upgrades.Contains(def))
			{
				return false;
			}
			building_upgrades.Add(def);
			SendState<BuildingUpgradesState>();
			RecalcBuildingStates();
			NotifyListeners("building_upgrade_unlocked", def);
			NotifyListeners("building_upgrades_changed");
			return true;
		}
	}

	public bool PlanBuildingUpgrade(Building.Def def)
	{
		using (Game.Profile("Kingdom.PalnBuildingUpgrade"))
		{
			if (def == null || !def.IsUpgrade())
			{
				return false;
			}
			if (HasBuildingUpgrade(def))
			{
				return false;
			}
			if (def.variant_of != null)
			{
				def = def.variant_of;
			}
			if (planned_upgrades == null)
			{
				planned_upgrades = new List<Building.Def>();
			}
			else if (planned_upgrades.Contains(def))
			{
				return false;
			}
			planned_upgrades.Add(def);
			SendState<BuildingUpgradesState>();
			NotifyListeners("planed_upgarde_changed", def);
			return true;
		}
	}

	public bool RemovePlanedUpgrade(Building.Def def)
	{
		using (Game.Profile("Kingdom.RemovePlanedUpgrade"))
		{
			if (def == null || !def.IsUpgrade())
			{
				return false;
			}
			if (def.variant_of != null)
			{
				def = def.variant_of;
			}
			if (planned_upgrades == null)
			{
				return false;
			}
			if (!planned_upgrades.Contains(def))
			{
				return false;
			}
			if (!IsAuthority())
			{
				SendEvent(new RemovePlannedUpgradeEvent(def));
				return true;
			}
			planned_upgrades.Remove(def);
			SendState<BuildingUpgradesState>();
			NotifyListeners("planed_upgarde_changed", def);
			return true;
		}
	}

	public bool RefreshUpgrades()
	{
		bool result = false;
		if (planned_upgrades != null)
		{
			for (int i = 0; i < planned_upgrades.Count; i++)
			{
				Building.Def def = planned_upgrades[i];
				if (HasBuildingUpgrade(def))
				{
					result = true;
					planned_upgrades.RemoveAt(i);
					i--;
				}
			}
		}
		if (realms != null)
		{
			for (int j = 0; j < realms.Count; j++)
			{
				Castle castle = realms[j]?.castle;
				if (castle != null && castle.RefreshUpgrades())
				{
					result = true;
				}
			}
		}
		return result;
	}

	public void OnAnalyticsUpgradeStarted(Castle castle, Building.Def upgrade_def)
	{
		if (!is_player || castle == null || Game.isLoadingSaveGame || upgrade_def == null)
		{
			return;
		}
		Vars vars = new Vars();
		vars.Set("province", castle.GetRealm().name);
		Resource cost = upgrade_def.GetCost(castle);
		if (cost != null)
		{
			vars.Set("upgradeName", upgrade_def.id);
			vars.Set("hammersCost", (int)cost[ResourceType.Hammers]);
			vars.Set("goldCost", (int)cost[ResourceType.Gold]);
			vars.Set("foodCost", (int)cost[ResourceType.Food]);
			vars.Set("bookCost", (int)cost[ResourceType.Books]);
			vars.Set("pietyCost", (int)cost[ResourceType.Piety]);
			vars.Set("tradeCost", (int)cost[ResourceType.Trade]);
			vars.Set("levyCost", (int)cost[ResourceType.Levy]);
		}
		List<District.Def> districts = upgrade_def.districts;
		List<Building.Def> list = new List<Building.Def>();
		if (districts != null)
		{
			for (int i = 0; i < districts.Count; i++)
			{
				List<Building.Def> prerequisites = upgrade_def.GetPrerequisites(districts[i]);
				if (prerequisites != null)
				{
					list.AddRange(prerequisites);
				}
			}
		}
		List<Building.Def.RequirementInfo> requires = upgrade_def.requires;
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
		FireEvent("analytics_upgrade_started", vars, id);
	}

	public void BeginUpgrade(Castle castle, Building.Def def)
	{
		if (def != null)
		{
			float num = def.cost[ResourceType.Hammers] / 10f;
			if (num <= 0f)
			{
				num = 1f;
			}
			if (upgrading == null)
			{
				upgrading = new List<Upgrading>();
			}
			Upgrading item = new Upgrading
			{
				def = def,
				start_time = game.time,
				duration = num
			};
			upgrading.Add(item);
			UpdateUpgradingTimer();
			SendState<BuildingUpgradesState>();
			NotifyListeners("upgrade_started", def);
			NotifyListeners("building_upgrades_started");
			GetKingdom()?.OnAnalyticsUpgradeStarted(castle, def);
		}
	}

	private void UpdateUpgradingTimer()
	{
		if (upgrading == null || upgrading.Count < 1)
		{
			Timer.Stop(this, "unlock_upgrade");
			return;
		}
		Time time = upgrading[0].end_time;
		for (int i = 1; i < upgrading.Count; i++)
		{
			Time end_time = upgrading[i].end_time;
			if (end_time < time)
			{
				time = end_time;
			}
		}
		float num = time - game.time;
		if (num < 0f)
		{
			Game.Log("Negative unlock_upgrade timer!", Game.LogType.Warning);
			num = 0f;
		}
		Timer.Start(this, "unlock_upgrade", num, restart: true);
	}

	public bool IsUpgrading(Building.Def def)
	{
		if (FindUpgradingIdx(def) >= 0)
		{
			return true;
		}
		return false;
	}

	private int FindUpgradingIdx(Building.Def def)
	{
		if (this.upgrading == null || def == null)
		{
			return -1;
		}
		if (def.variant_of != null)
		{
			def = def.variant_of;
		}
		for (int i = 0; i < this.upgrading.Count; i++)
		{
			Upgrading upgrading = this.upgrading[i];
			if ((upgrading.def.variant_of ?? upgrading.def) == def)
			{
				return i;
			}
		}
		return -1;
	}

	public Building.Def FindUpgradingInDistrict(District.Def district, Building.Def ignore = null)
	{
		if (district == null || this.upgrading == null)
		{
			return null;
		}
		if (ignore?.variant_of != null)
		{
			ignore = ignore.variant_of;
		}
		for (int i = 0; i < this.upgrading.Count; i++)
		{
			Upgrading upgrading = this.upgrading[i];
			Building.Def def = upgrading.def.variant_of ?? upgrading.def;
			if (def != ignore && def.districts != null && def.districts.Contains(district))
			{
				return upgrading.def;
			}
		}
		return null;
	}

	public Building.Def FindUpgradingInSameDistrict(Building.Def def, bool ignore_self = false)
	{
		if (def?.districts == null)
		{
			return null;
		}
		if (upgrading == null)
		{
			return null;
		}
		Building.Def def2 = null;
		for (int i = 0; i < def.districts.Count; i++)
		{
			District.Def district = def.districts[i];
			Building.Def def3 = FindUpgradingInDistrict(district, ignore_self ? def : null);
			if (def3 == null)
			{
				return null;
			}
			if (def2 == null)
			{
				def2 = def3;
			}
		}
		return def2;
	}

	public bool IsUpgradingElsewhere(Building.Def def)
	{
		if (FindUpgradingInSameDistrict(def, ignore_self: true) == null)
		{
			return false;
		}
		int num = FindUpgradingIdx(def);
		if (num < 0)
		{
			return false;
		}
		if (upgrading[num].def == def)
		{
			return false;
		}
		return true;
	}

	public float GetUpgradeProgress(Building.Def def)
	{
		int num = FindUpgradingIdx(def);
		if (num < 0)
		{
			return -1f;
		}
		Upgrading upgrading = this.upgrading[num];
		return (game.time - upgrading.start_time) / upgrading.duration;
	}

	public bool CancelUpgrading(Building.Def def)
	{
		int num = FindUpgradingIdx(def);
		if (num < 0)
		{
			return false;
		}
		if (!IsAuthority())
		{
			SendEvent(new CancelUpgradeEvent(def));
			return true;
		}
		Resource buildRefunds = def.GetBuildRefunds(null, this);
		upgrading.RemoveAt(num);
		UpdateUpgradingTimer();
		SendState<BuildingUpgradesState>();
		if (buildRefunds != null)
		{
			AddResources(KingdomAI.Expense.Category.Economy, buildRefunds);
		}
		NotifyListeners("upgrade_canceled", def);
		return true;
	}

	private void FinishUpgrading()
	{
		if (this.upgrading == null)
		{
			UpdateUpgradingTimer();
			return;
		}
		bool flag = false;
		for (int num = this.upgrading.Count - 1; num >= 0; num--)
		{
			Upgrading upgrading = this.upgrading[num];
			if (!(upgrading.end_time > game.time))
			{
				this.upgrading.RemoveAt(num);
				if (!UnlockBuildingUpgrade(upgrading.def, forced: true))
				{
					Game.Log("Invalid upgarde: " + upgrading, Game.LogType.Warning);
				}
				else
				{
					flag = (byte)((flag ? 1u : 0u) | 1u) != 0;
				}
			}
		}
		if (flag)
		{
			FireEvent("upgrade_finished", null);
		}
		UpdateUpgradingTimer();
	}

	public bool HasBuilding(Building.Def def)
	{
		if (def == null)
		{
			return false;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].castle.FindBuilding(def) != null)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasBuildingUpgrade(Building.Def def)
	{
		if (def == null)
		{
			return false;
		}
		if (building_upgrades == null)
		{
			return false;
		}
		if (def.variant_of != null)
		{
			def = def.variant_of;
		}
		if (building_upgrades.Contains(def))
		{
			return true;
		}
		return false;
	}

	public bool HasPlannedUpgrade(Building.Def def)
	{
		if (def == null)
		{
			return false;
		}
		if (planned_upgrades == null)
		{
			return false;
		}
		if (def.variant_of != null)
		{
			def = def.variant_of;
		}
		if (planned_upgrades.Contains(def))
		{
			return true;
		}
		return false;
	}

	public void RefreshCourtSkills()
	{
		if (court != null)
		{
			for (int i = 0; i < court.Count; i++)
			{
				court[i]?.RefreshTags();
			}
		}
	}

	public void RefreshRealmTags()
	{
		if (game != null && !game.IsUnloadingMap())
		{
			realm_tags = null;
			RefreshResourcesInfo();
			NotifyListeners("refresh_tags");
		}
	}

	public void MarkResourceModsForDeletion()
	{
		for (LinkedListNode<(string, bool, List<Resource.StatModifier>)> linkedListNode = resource_mods.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
		{
			(string, bool, List<Resource.StatModifier>) value = linkedListNode.Value;
			value.Item2 = false;
			linkedListNode.Value = value;
		}
	}

	public bool TryMarkResourceModsAsExisting(string resource)
	{
		for (LinkedListNode<(string, bool, List<Resource.StatModifier>)> linkedListNode = resource_mods.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
		{
			(string, bool, List<Resource.StatModifier>) value = linkedListNode.Value;
			if (value.Item1 == resource)
			{
				value.Item2 = true;
				linkedListNode.Value = value;
				return true;
			}
		}
		return false;
	}

	public void CreateNewResourceMods(Resource.Def rdef)
	{
		if (rdef.mdefs != null)
		{
			List<Resource.StatModifier> list = new List<Resource.StatModifier>();
			for (int i = 0; i < rdef.mdefs.Count; i++)
			{
				Resource.StatModifier statModifier = new Resource.StatModifier(rdef.mdefs[i]);
				statModifier.value = statModifier.def.CalcValue();
				list.Add(statModifier);
				stats.AddModifier(statModifier.def.stat_name, statModifier);
			}
			resource_mods.AddLast((rdef.id, true, list));
		}
	}

	public void ClearUnusedResourcesMods()
	{
		LinkedListNode<(string, bool, List<Resource.StatModifier>)> linkedListNode = resource_mods.First;
		while (linkedListNode != null)
		{
			if (!linkedListNode.Value.Item2)
			{
				LinkedListNode<(string, bool, List<Resource.StatModifier>)> linkedListNode2 = linkedListNode;
				linkedListNode = linkedListNode.Next;
				List<Resource.StatModifier> item = linkedListNode2.Value.Item3;
				for (int i = 0; i < item.Count; i++)
				{
					Resource.StatModifier statModifier = item[i];
					statModifier.stat.DelModifier(statModifier);
				}
				resource_mods.Remove(linkedListNode2);
			}
			else
			{
				linkedListNode = linkedListNode.Next;
			}
		}
	}

	public void UpdateRealmTags(bool force = false)
	{
		if (resource_mods == null)
		{
			resource_mods = new LinkedList<(string, bool, List<Resource.StatModifier>)>();
		}
		MarkResourceModsForDeletion();
		if (!force && realm_tags != null)
		{
			return;
		}
		realm_tags = new Dictionary<string, int>();
		if (building_upgrades != null)
		{
			for (int i = 0; i < building_upgrades.Count; i++)
			{
				Building.Def def = building_upgrades[i];
				realm_tags[def.id] = 1;
			}
		}
		tmp_check_monopoly.Clear();
		foreach (KeyValuePair<string, Resource.Def> item in goods_produced)
		{
			Resource.Def value = item.Value;
			if (game.kingdoms_with_resource.TryGetValue(value, out var value2))
			{
				value2.Remove(id);
				if (value2.Count == 1)
				{
					tmp_check_monopoly.Add(value);
				}
			}
		}
		goods_produced.Clear();
		goods_imported.Clear();
		monopoly_goods.Clear();
		for (int j = 0; j < realms.Count; j++)
		{
			Realm realm = realms[j];
			if (realm.IsOccupied() || realm.IsDisorder())
			{
				continue;
			}
			if (realm.tags == null)
			{
				realm.BuildTags();
			}
			for (int k = 0; k < realm.goods_produced.Count; k++)
			{
				Resource.Def def2 = realm.goods_produced[k];
				if (!goods_produced.ContainsKey(def2.id))
				{
					goods_produced.Add(def2.id, def2);
					tmp_check_monopoly.Remove(def2);
					if (!game.kingdoms_with_resource.TryGetValue(def2, out var value3))
					{
						value3 = new List<int>();
						game.kingdoms_with_resource.Add(def2, value3);
					}
					value3.Add(id);
					if (value3.Count == 1)
					{
						monopoly_goods.Add(def2);
					}
					else if (value3.Count == 2)
					{
						game.GetKingdom(value3[0])?.monopoly_goods.Remove(def2);
					}
				}
			}
			foreach (KeyValuePair<string, int> tag in realm.tags)
			{
				string key = tag.Key;
				int value4 = tag.Value;
				realm_tags.TryGetValue(key, out var value5);
				value5 += value4;
				realm_tags[key] = value5;
			}
		}
		foreach (KeyValuePair<string, Resource.Def> item2 in goods_produced)
		{
			if (!TryMarkResourceModsAsExisting(item2.Value.id))
			{
				CreateNewResourceMods(item2.Value);
			}
		}
		if (court != null)
		{
			for (int l = 0; l < court.Count; l++)
			{
				Character c = court[l];
				AddImportedGoods(c);
			}
		}
		foreach (KeyValuePair<string, Resource.Def> item3 in goods_imported)
		{
			if (!TryMarkResourceModsAsExisting(item3.Value.id))
			{
				CreateNewResourceMods(item3.Value);
			}
		}
		if (foreigners != null)
		{
			for (int m = 0; m < foreigners.Count; m++)
			{
				Character character = foreigners[m];
				if (character == null || !character.IsMerchant() || character.importing_goods == null)
				{
					continue;
				}
				for (int n = 0; n < character.importing_goods.Count; n++)
				{
					string name = character.importing_goods[n].name;
					if (!string.IsNullOrEmpty(name) && !goods_produced.ContainsKey(name))
					{
						character.GetKingdom().StopGoodImporting(name, character, "production_stopped");
					}
				}
			}
		}
		for (int num = 0; num < tmp_check_monopoly.Count; num++)
		{
			Resource.Def def3 = tmp_check_monopoly[num];
			if (!game.kingdoms_with_resource.TryGetValue(def3, out var value6) || value6 == null || value6.Count != 1)
			{
				Game.Log("Goods monopoly messed up: " + def3.id + " -> " + Object.Dump(value6), Game.LogType.Error);
				continue;
			}
			Kingdom kingdom = game.GetKingdom(value6[0]);
			if (kingdom == null || kingdom.monopoly_goods.Contains(def3))
			{
				Game.Log("Goods monopoly messed up: " + def3.id + " -> " + Object.Dump(kingdom?.monopoly_goods), Game.LogType.Error);
			}
			else
			{
				kingdom.monopoly_goods.Add(def3);
			}
		}
		ClearUnusedResourcesMods();
		RefreshAdvantages();
		NotifyListeners("update_realm_tags");
	}

	private void AddImportedGoods(Character c)
	{
		if (c?.importing_goods == null || realm_tags == null)
		{
			return;
		}
		for (int i = 0; i < c.importing_goods.Count; i++)
		{
			Character.ImportedGood importedGood = c.importing_goods[i];
			if (string.IsNullOrEmpty(importedGood.name))
			{
				continue;
			}
			realm_tags.TryGetValue(importedGood.name, out var value);
			if (value <= 0)
			{
				value++;
				realm_tags[importedGood.name] = value;
				Resource.Def def = game?.economy?.GetGoodDef(importedGood.name);
				if (def != null)
				{
					goods_imported[importedGood.name] = def;
				}
			}
			else
			{
				StopGoodImporting(importedGood.name, c, "now_produced", recalcTags: false);
			}
		}
	}

	private void StopGoodImporting(string goodName, Character c, string reason = "", bool recalcTags = true)
	{
		if (IsAuthority())
		{
			c.ImportGood(null, c.FindImportGoodSlot(goodName), 0f, recalcTags);
			Vars vars = new Vars();
			vars.Set("goodName", goodName);
			c.FireEvent("stopped_importing_good_" + reason, vars);
		}
	}

	public int GetRealmTag(string tag)
	{
		if (all_tags >= 0)
		{
			return all_tags;
		}
		UpdateRealmTags();
		realm_tags.TryGetValue(tag, out var value);
		return value;
	}

	public int GetBuildingsCurrentlyBeingBuilt(Building.Def tag)
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			Castle castle = realms[i]?.castle;
			if (castle != null)
			{
				Building.Def currentBuildingBuild = castle.GetCurrentBuildingBuild();
				if (currentBuildingBuild != null && currentBuildingBuild == tag && !castle.HasBuilding(currentBuildingBuild))
				{
					num++;
				}
			}
		}
		return num;
	}

	public Resource.Def GetProducedGoodDef(string name)
	{
		UpdateRealmTags();
		goods_produced.TryGetValue(name, out var value);
		return value;
	}

	public Resource.Def GetImportedGoodDef(string name)
	{
		UpdateRealmTags();
		goods_imported.TryGetValue(name, out var value);
		return value;
	}

	public Point CalcCenter()
	{
		Point zero = Point.Zero;
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm != null && realm.castle != null)
			{
				zero += realm.castle.position;
				num++;
			}
		}
		if (num > 0)
		{
			zero /= (float)num;
		}
		return zero;
	}

	public Realm GetNearestRealm(Point pt)
	{
		float d2best;
		return GetNearestRealm(pt, out d2best);
	}

	public Realm GetNearestRealm(Point pt, out float d2best)
	{
		Realm result = null;
		d2best = float.MaxValue;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm != null && realm.castle != null)
			{
				float num = realm.castle.position.SqrDist(pt);
				if (num < d2best)
				{
					result = realm;
					d2best = num;
				}
			}
		}
		return result;
	}

	public int GetVassalStatesRealms()
	{
		int num = 0;
		for (int i = 0; i < vassalStates.Count; i++)
		{
			num += vassalStates[i].realms.Count;
		}
		return num;
	}

	public int GetVassalStatesPopulation()
	{
		int num = 0;
		for (int i = 0; i < vassalStates.Count; i++)
		{
			num += vassalStates[i].GetTotalPopulation();
		}
		return num;
	}

	public float GetAvarageRelationshipWithEveryone()
	{
		float num = 0f;
		float num2 = 0f;
		foreach (Kingdom kingdom in game.kingdoms)
		{
			if (kingdom != this && !kingdom.IsDefeated())
			{
				KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(this, kingdom);
				RelationUtils.Stance stance = kingdomAndKingdomRelation.stance;
				float relationship = kingdomAndKingdomRelation.GetRelationship();
				float num3 = 0f;
				if (ai != null)
				{
					num3 = ai.CalcDiplomaticImportance(kingdom);
				}
				else if (stance.IsPeace() && relationship == 0f && DistanceToKingdom(kingdom) < 4)
				{
					num3 = 1f;
				}
				if (num3 != 0f)
				{
					num += relationship;
					num2 += 1f;
				}
			}
		}
		if (num2 == 0f)
		{
			return 0f;
		}
		return num / num2;
	}

	public float GetTotalFoodProduction()
	{
		float num = 0f;
		foreach (Realm realm in realms)
		{
			num += realm.income[ResourceType.Food];
		}
		return num;
	}

	public float GetFood()
	{
		return income[ResourceType.Food] - expenses[ResourceType.Food];
	}

	public void CalcArmiesUpkeep()
	{
		armies_upkeep.Clear();
		if (armies == null || armies.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			if (army != null)
			{
				Resource upkeep = army.GetUpkeep();
				armies_upkeep.Add(upkeep, 1f);
			}
		}
	}

	public Resource GetGarrisonUpkeep()
	{
		Resource resource = new Resource();
		if (realms != null && realms.Count > 0)
		{
			for (int i = 0; i < realms.Count; i++)
			{
				Realm realm = realms[i];
				resource.Add(realm.upkeepGarrison, 1f);
			}
		}
		return resource;
	}

	public float CalcBribesUpkeep()
	{
		float num = 0f;
		Vars vars = null;
		DT.Field field = game.dt.Find("BribeNobleAction.gold_upkeep");
		DT.Field field2 = game.dt.Find("BribeNobleAction.upkeep_per_puppet_increase");
		for (int i = 0; i < court.Count; i++)
		{
			int num2 = 0;
			Character character = court[i];
			if (character == null || character.puppets == null)
			{
				continue;
			}
			for (int j = 0; j < character.puppets.Count; j++)
			{
				Character val = character.puppets[j];
				float num3 = 0f;
				float num4 = 0f;
				if (field != null)
				{
					if (vars == null)
					{
						vars = new Vars();
					}
					vars.Set("puppet", val);
					vars.Set("own_character", character);
					num3 = field.Float(vars);
				}
				if (field2 != null)
				{
					if (vars == null)
					{
						vars = new Vars();
					}
					if (!vars.ContainsKey("own_character"))
					{
						vars.Set("own_character", character);
					}
					num4 = field2.Float(vars);
				}
				num += num3 + (float)num2 * num4;
				num2++;
			}
		}
		return num;
	}

	public float CalcSupportPretendersUpkeep()
	{
		float num = 0f;
		Vars vars = null;
		DT.Field field = game.dt.Find("PuppetSupportPretenderToTheThroneAction.gold_upkeep");
		if (field == null)
		{
			return 0f;
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character == null || character.puppets == null)
			{
				continue;
			}
			for (int j = 0; j < character.puppets.Count; j++)
			{
				Character character2 = character.puppets[j];
				if (character2.IsPretenderToTheThrone(this))
				{
					if (vars == null)
					{
						vars = new Vars();
					}
					vars.Set("puppet", character2);
					vars.Set("own_character", character);
					num += field.Float(vars);
				}
			}
		}
		return num;
	}

	public float CalcRuinRelationsUpkeep()
	{
		float num = 0f;
		DT.Field field = null;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character == null || !(character.cur_action is RuinRelationsAction ruinRelationsAction))
			{
				continue;
			}
			if (field == null)
			{
				field = ruinRelationsAction.def?.field?.FindChild("gold_upkeep");
				if (field == null)
				{
					break;
				}
			}
			float num2 = field.Float(ruinRelationsAction);
			if (!(num2 <= 0f))
			{
				num += num2;
			}
		}
		return num;
	}

	public float CalcSowDissentUpkeep()
	{
		float num = 0f;
		DT.Field field = null;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character == null || !(character.cur_action is SowDissentAction sowDissentAction))
			{
				continue;
			}
			if (field == null)
			{
				field = sowDissentAction.def?.field?.FindChild("gold_upkeep");
				if (field == null)
				{
					break;
				}
			}
			float num2 = field.Float(sowDissentAction);
			if (!(num2 <= 0f))
			{
				num += num2;
			}
		}
		return num;
	}

	public float CalcBolsterCultureUpkeep()
	{
		float num = 0f;
		DT.Field field = null;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character == null || !(character.cur_action is BolsterCultureAction bolsterCultureAction))
			{
				continue;
			}
			if (field == null)
			{
				field = bolsterCultureAction.def?.field?.FindChild("gold_upkeep");
				if (field == null)
				{
					break;
				}
			}
			float num2 = field.Float(bolsterCultureAction);
			if (!(num2 <= 0f))
			{
				num += num2;
			}
		}
		return num;
	}

	public float CalcBolsterInfluenceUpkeep()
	{
		float num = 0f;
		DT.Field field = null;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character == null || !(character.cur_action is BolsterInfluenceAction bolsterInfluenceAction))
			{
				continue;
			}
			if (field == null)
			{
				field = bolsterInfluenceAction.def?.field?.FindChild("gold_upkeep");
				if (field == null)
				{
					break;
				}
			}
			float num2 = field.Float(bolsterInfluenceAction);
			if (!(num2 <= 0f))
			{
				num += num2;
			}
		}
		return num;
	}

	public float CalcOccupationsUpkeep()
	{
		float num = 0f;
		int count = occupiedRealms.Count;
		if (count <= 0)
		{
			return 0f;
		}
		DT.Field field = occupiedRealms[0].def.FindChild("occupied_upkeep");
		int num2 = field.NumValues();
		if (num2 < count)
		{
			return field.Value(num2 - 1);
		}
		return field.Value(count - 1);
	}

	public float CalcDisorderUpkeep()
	{
		float num = 0f;
		if (realms.Count == 0)
		{
			return 0f;
		}
		DT.Field field = realms[0].def.FindChild("disorder_upkeep");
		int num2 = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].IsDisorder())
			{
				num2++;
			}
		}
		int num3 = field.NumValues();
		if (num3 < num2)
		{
			return field.Value(num3 - 1);
		}
		return field.Value(num2 - 1);
	}

	public float CalcHelpTheWeakUpkeep()
	{
		float num = 0f;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsCleric() && character.FindStatus<HelpTheWeakStatus>() != null)
			{
				num += character.CalcHelpTheWeakUpkeep();
			}
		}
		return num;
	}

	public int GetClericsCount()
	{
		int num = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsCleric())
			{
				num++;
			}
		}
		return num;
	}

	public float CalcJihadUpkeep()
	{
		if (!IsCaliphate() || jihad == null || !jihad.IsJihad())
		{
			return 0f;
		}
		return jihad.GetUpkeep(this);
	}

	public float GetSufficentFoodMod()
	{
		float num = sufficient_food_base;
		float num2 = income[ResourceType.Food];
		float num3 = expenses[ResourceType.Food];
		float num4 = num2 - num3;
		if (num2 < -0.001f || num2 > 0.001f)
		{
			num += sufficient_food_mod * num4 / num2;
		}
		num = Math.Min(num, sufficient_food_max);
		return Math.Max(num, sufficient_food_min);
	}

	public int GetOwnTradeCentersCount()
	{
		int num = 0;
		for (int i = 0; i < game.economy.tradeCenterRealms.Count; i++)
		{
			if (game.economy.tradeCenterRealms[i].kingdom_id == id)
			{
				num++;
			}
		}
		return num;
	}

	public int TradeCentersZoneRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < game.economy.tradeCenterRealms.Count; i++)
		{
			if (game.economy.tradeCenterRealms[i].kingdom_id == id)
			{
				TradeCenter tradeCenter = game.economy.tradeCenterRealms[i].tradeCenter;
				if (tradeCenter != null)
				{
					num += tradeCenter.belongingRealms.Count;
				}
			}
		}
		return num;
	}

	public float TradeCentersAppealSum()
	{
		float num = 0f;
		for (int i = 0; i < game.economy.tradeCenterRealms.Count; i++)
		{
			if (game.economy.tradeCenterRealms[i].kingdom_id == id)
			{
				num += game.economy.tradeCenterRealms[i].GetAppeal();
			}
		}
		return num;
	}

	public int GetHolyLandsCount()
	{
		int num = 0;
		if (is_christian && realms.Contains(game.religions.catholic.holy_lands_realm))
		{
			num++;
		}
		if (is_muslim)
		{
			for (int i = 0; i < game.religions.sunni.holy_lands_realms.Count; i++)
			{
				Realm item = game.religions.sunni.holy_lands_realms[i];
				if (realms.Contains(item))
				{
					num++;
				}
			}
			if (realms.Contains(game.religions.shia.holy_lands_realm))
			{
				num++;
			}
		}
		return num;
	}

	public int GetTradeCentersCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].IsTradeCenter())
			{
				num++;
			}
		}
		return num;
	}

	public int GetRealmsWithMajorityCount(Kingdom k = null)
	{
		if (k == null)
		{
			k = this;
		}
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].pop_majority.kingdom == k)
			{
				num++;
			}
		}
		return num;
	}

	public int NumLoyalists(Kingdom k)
	{
		if (k == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			List<Army> list = realms[i].armies;
			for (int j = 0; j < list.Count; j++)
			{
				Rebel rebel = list[j].rebel;
				if (rebel != null && rebel.loyal_to == k.id)
				{
					num++;
				}
			}
		}
		return num;
	}

	public int GetTotalPopulation()
	{
		int num = 0;
		foreach (Realm realm in realms)
		{
			if (realm.castle != null && realm.castle.population != null)
			{
				Population population = realm.castle.population;
				num += population.GetWorkers() + population.GetRebels();
			}
		}
		return num;
	}

	public int GetRebelPopulation()
	{
		int num = 0;
		foreach (Realm realm in realms)
		{
			if (realm.castle != null && realm.castle.population != null)
			{
				Population population = realm.castle.population;
				num += population.GetRebels();
			}
		}
		return num;
	}

	public int GetMerchantsCount()
	{
		int num = 0;
		foreach (Character item in court)
		{
			if (item != null && item.IsMerchant())
			{
				num++;
			}
		}
		return num;
	}

	public int GetTradingMerchantsCount()
	{
		int num = 0;
		foreach (Character item in court)
		{
			if (item != null && item.IsMerchant() && item.mission_kingdom != null)
			{
				num++;
			}
		}
		return num;
	}

	public int GetForeignMerchantsCount()
	{
		int num = 0;
		for (int i = 0; i < foreigners.Count; i++)
		{
			if (foreigners[i].IsMerchant())
			{
				num++;
			}
		}
		return num;
	}

	public Character GetMerchantIn(Kingdom k)
	{
		if (k == null)
		{
			return null;
		}
		if (court == null)
		{
			return null;
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsMerchant() && character.mission_kingdom == k)
			{
				return character;
			}
		}
		return null;
	}

	public Character GetMerchantFrom(Kingdom k)
	{
		if (k == null)
		{
			return null;
		}
		if (foreigners == null)
		{
			return null;
		}
		for (int i = 0; i < foreigners.Count; i++)
		{
			Character character = foreigners[i];
			if (character.IsMerchant() && character.GetKingdom() == k && character.mission_kingdom == this)
			{
				return character;
			}
		}
		return null;
	}

	public Character GetMerchantGoingTo(Kingdom tgt_kingdom)
	{
		if (court == null || tgt_kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsMerchant() && character.cur_action is TradeWithKingdomAction && character.cur_action.target == tgt_kingdom)
			{
				return character;
			}
		}
		return null;
	}

	public bool MayImportGood(Resource.Def rdef, Kingdom from_kingdom)
	{
		if (from_kingdom == null)
		{
			return false;
		}
		if (rdef == null || rdef.field == null)
		{
			return false;
		}
		if (rdef.Category != "Base")
		{
			return false;
		}
		if (GetImportedGoodDef(rdef.field.key) != null)
		{
			return false;
		}
		if (GetProducedGoodDef(rdef.field.key) != null)
		{
			return false;
		}
		if (from_kingdom.GetProducedGoodDef(rdef.field.key) == null)
		{
			return false;
		}
		return true;
	}

	public int GetImportableGoods(Kingdom from_kingdom, List<string> result = null)
	{
		if (from_kingdom == null)
		{
			return 0;
		}
		int num = 0;
		from_kingdom.UpdateRealmTags();
		foreach (KeyValuePair<string, Resource.Def> item in from_kingdom.goods_produced)
		{
			Resource.Def value = item.Value;
			if (MayImportGood(value, from_kingdom))
			{
				result?.Add(value.field.key);
				num++;
			}
		}
		return num;
	}

	public Character GetCultureBolsterCleric()
	{
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsCleric() && character.cur_action is BolsterCultureAction)
			{
				return character;
			}
		}
		return null;
	}

	public Character GetInfluenceBolsterDiplomat()
	{
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsDiplomat() && character.cur_action is BolsterInfluenceAction)
			{
				return character;
			}
		}
		return null;
	}

	public int GetSpiesCount()
	{
		int num = 0;
		foreach (Character item in court)
		{
			if (item != null && item.IsSpy())
			{
				num++;
			}
		}
		return num;
	}

	public int GetForeignSpiesCount()
	{
		int num = 0;
		for (int i = 0; i < foreigners.Count; i++)
		{
			if (foreigners[i].IsSpy())
			{
				num++;
			}
		}
		return num;
	}

	public void ClearRevealedSpies()
	{
		if (!IsAuthority())
		{
			return;
		}
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom.IsDefeated() || kingdom.court == null)
			{
				continue;
			}
			for (int j = 0; j < kingdom.court.Count; j++)
			{
				Character character = kingdom.court[j];
				if (character != null && character.IsSpy())
				{
					character.DelRevealedInKingdom(this);
				}
			}
		}
	}

	public Character GetSpyFrom(Kingdom k)
	{
		if (k?.court == null)
		{
			return null;
		}
		if (foreigners == null)
		{
			return null;
		}
		if (foreigners.Count < k.court.Count)
		{
			for (int i = 0; i < foreigners.Count; i++)
			{
				Character character = foreigners[i];
				if (character.IsSpy() && character.GetKingdom() == k && character.mission_kingdom == this)
				{
					return character;
				}
			}
		}
		else
		{
			for (int j = 0; j < k.court.Count; j++)
			{
				Character character2 = k.court[j];
				if (character2?.mission_kingdom == this && character2.IsSpy())
				{
					return character2;
				}
			}
		}
		return null;
	}

	public Character GetInfiltratingSpy(Kingdom tgt_kingdom)
	{
		if (court == null || tgt_kingdom == null)
		{
			return null;
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsSpy() && character.cur_action is InfiltrateKingdomAction && character.cur_action.target == tgt_kingdom)
			{
				return character;
			}
		}
		return null;
	}

	public Character GetCharacterWithActionOrStatus(string action_id, string status_id = null, Character ignore = null)
	{
		if (action_id == "")
		{
			action_id = null;
		}
		if (status_id == "")
		{
			status_id = null;
		}
		if (action_id == null && status_id == null)
		{
			return null;
		}
		if (court == null)
		{
			return null;
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null)
			{
				if (action_id != null && character.cur_action?.def?.unique_id == action_id)
				{
					return character;
				}
				if (status_id != null && character.status?.def?.id == status_id)
				{
					return character;
				}
			}
		}
		return null;
	}

	public int NumCourtMembers()
	{
		if (court == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && !character.IsDead())
			{
				num++;
			}
		}
		return num;
	}

	public int NumCourtMembersOfClass(string class_name)
	{
		if (court == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && !(character.class_def?.name != class_name) && !character.IsDead())
			{
				num++;
			}
		}
		return num;
	}

	public int NumCourtMembersOfClass(CharacterClass.Def def)
	{
		if (court == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.class_def == def)
			{
				num++;
			}
		}
		return num;
	}

	public int NumForeignersOfClass(string class_name)
	{
		int num = 0;
		for (int i = 0; i < foreigners.Count; i++)
		{
			if (!(foreigners[i]?.class_def?.name != class_name))
			{
				num++;
			}
		}
		return num;
	}

	public List<Character> CourtMembersOfClass(string class_name)
	{
		if (court == null)
		{
			return null;
		}
		List<Character> list = tmp_characters;
		list.Clear();
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (!(character?.class_def?.name != class_name))
			{
				list.Add(character);
			}
		}
		return list;
	}

	public List<Character> ForeignersOfClass(string class_name)
	{
		List<Character> list = tmp_characters;
		list.Clear();
		for (int i = 0; i < foreigners.Count; i++)
		{
			Character character = foreigners[i];
			if (!(character?.class_def?.name != class_name))
			{
				list.Add(character);
			}
		}
		return list;
	}

	public float NewCharacterWage(CharacterClass.Def def)
	{
		if (def == null)
		{
			return 0f;
		}
		if (def.name == "Merchant")
		{
			return 0f;
		}
		int num = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character?.class_def == def && (royalFamily == null || !royalFamily.IsFamilyMember(character)) && !character.IsKing() && character.prison_kingdom == null)
			{
				num++;
			}
		}
		if (wage_thresholds.Length == 0)
		{
			return 0f;
		}
		if (num + 1 >= wage_thresholds.Length)
		{
			return wage_thresholds[wage_thresholds.Length - 1];
		}
		float num2 = wage_thresholds[num];
		return wage_thresholds[num + 1] - num2;
	}

	public float GetAvgPopulationHappiness()
	{
		float num = 0f;
		foreach (Realm realm in realms)
		{
			num += realm.stats.Get("rs_happiness");
		}
		return num / (float)realms.Count;
	}

	public int GetFamousPeopleCount()
	{
		int num = 0;
		foreach (Realm realm in realms)
		{
			for (int i = 0; i < realm.settlements.Count; i++)
			{
				if (realm.settlements[i] is Village { famous_person: not null } village && village.IsActiveSettlement())
				{
					num++;
				}
			}
		}
		return num;
	}

	public void RefreshBankrupcy()
	{
		GetComponent<KingdomBankruptcy>()?.Refresh();
	}

	public bool IsBankrupt()
	{
		return GetComponent<KingdomBankruptcy>()?.IsBankrupt() ?? false;
	}

	public CrownAuthority GetCrownAuthority()
	{
		return GetComponent<CrownAuthority>();
	}

	public Offer GetOutgoingOfferTo(Kingdom k)
	{
		return GetComponent<Offers>()?.outgoing?.Find((Offer o) => o.to == k);
	}

	public Offer GetOngoingOfferWith(Kingdom k)
	{
		if (k == null)
		{
			return null;
		}
		Offer offer = GetComponent<Offers>()?.outgoing?.Find((Offer o) => o.to == k);
		if (offer != null)
		{
			return offer;
		}
		return k.GetComponent<Offers>()?.outgoing?.Find((Offer o) => o.to == this);
	}

	public Realm CalcCenterRealm()
	{
		Point pt = CalcCenter();
		return GetNearestRealm(pt);
	}

	public void ResetNeighbors()
	{
		neighbors.Clear();
		for (int i = 0; i < realms.Count; i++)
		{
			for (int j = 0; j < realms[i].logicNeighborsRestricted.Count; j++)
			{
				Realm realm = realms[i].logicNeighborsRestricted[j];
				if (id != realm.kingdom_id)
				{
					neighbors.Add(realm.GetKingdom());
				}
			}
		}
	}

	public void ResetSecondaryNeighbors()
	{
		secondaryNeighbors.Clear();
		foreach (Kingdom neighbor in neighbors)
		{
			foreach (Kingdom neighbor2 in neighbor.neighbors)
			{
				if (id != neighbor2.id && !neighbors.Contains(neighbor2))
				{
					secondaryNeighbors.Add(neighbor2);
				}
			}
		}
	}

	public bool HasNeighbor(Kingdom k)
	{
		return neighbors.Contains(k);
	}

	public bool HasSecondNeighbor(Kingdom k)
	{
		return secondaryNeighbors.Contains(k);
	}

	public void RecalculateNeighbors()
	{
		List<Kingdom> list = new List<Kingdom>();
		list.Add(this);
		ResetNeighbors();
		foreach (Kingdom neighbor in neighbors)
		{
			if (list.Contains(neighbor))
			{
				continue;
			}
			list.Add(neighbor);
			neighbor.ResetNeighbors();
			foreach (Kingdom neighbor2 in neighbor.neighbors)
			{
				if (!list.Contains(neighbor2))
				{
					list.Add(neighbor2);
					neighbor2.ResetNeighbors();
					neighbor2.ResetSecondaryNeighbors();
				}
			}
			neighbor.ResetSecondaryNeighbors();
		}
		ResetSecondaryNeighbors();
	}

	public void ZeroOutKingdomDistances()
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			if (kingdomDistances.Count == i)
			{
				kingdomDistances.Add(-1);
			}
			else
			{
				kingdomDistances[i] = -1;
			}
		}
	}

	public void RecalcKingdomDistances()
	{
		ZeroOutKingdomDistances();
		Queue<Realm> queue = new Queue<Realm>(64);
		for (int i = 0; i < game.realms.Count; i++)
		{
			Realm realm = game.realms[i];
			if (realm.kingdom_id == id)
			{
				if (realm.IsBorder())
				{
					queue.Enqueue(realm);
				}
				realm.wave_depth = 0;
			}
			else
			{
				realm.wave_depth = -1;
			}
		}
		while (queue.Count > 0)
		{
			Realm realm2 = queue.Dequeue();
			int wave_depth = realm2.wave_depth + 1;
			for (int j = 0; j < realm2.neighbors.Count; j++)
			{
				Realm realm3 = realm2.neighbors[j];
				if (realm3.wave_depth >= 0)
				{
					continue;
				}
				Kingdom kingdom = realm3.GetKingdom();
				realm3.wave_depth = wave_depth;
				queue.Enqueue(realm3);
				if (kingdom != null && kingdom.id != 0)
				{
					int num = kingdomDistances[kingdom.id - 1];
					if (num < 0)
					{
						num = realm3.wave_depth;
					}
					kingdomDistances[kingdom.id - 1] = Math.Min(realm3.wave_depth, num);
					if (kingdom.kingdomDistances.Count != game.kingdoms.Count)
					{
						kingdom.ZeroOutKingdomDistances();
					}
					kingdom.kingdomDistances[id - 1] = kingdomDistances[kingdom.id - 1];
				}
			}
		}
		game?.economy?.OnKingdomDistancesChanged(this);
	}

	public List<Realm> GetWarBorderRealms()
	{
		List<Realm> list = new List<Realm>();
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (!realm.isBorder)
			{
				continue;
			}
			for (int j = 0; j < realm.logicNeighborsRestricted.Count; j++)
			{
				Realm realm2 = realm.logicNeighborsRestricted[j];
				if (realm2.kingdom_id != realm.kingdom_id && IsEnemy(realm2.kingdom_id))
				{
					list.Add(realm);
					break;
				}
			}
		}
		return list;
	}

	public void RemoveExternalBorderRealm(Realm r)
	{
		externalBorderRealms.Remove(r);
	}

	public void AddExternalBorderRealm(Realm r)
	{
		if (!externalBorderRealms.Contains(r))
		{
			externalBorderRealms.Add(r);
		}
	}

	public Realm GetClosestRealmOfKingdom(int kid)
	{
		return game.ClosestKingomRealmToKingdom(kid, id);
	}

	public int DistanceToKingdom(int kid, int maxDepth = int.MaxValue)
	{
		return game.KingdomDistance(id, kid, maxDepth);
	}

	public int DistanceToKingdom(Kingdom k)
	{
		if (k == null || k.id == 0 || k.id - 1 >= kingdomDistances.Count || IsDefeated() || k.IsDefeated())
		{
			return int.MaxValue;
		}
		int num = kingdomDistances[k.id - 1];
		if (num == -1)
		{
			RecalcKingdomDistances();
		}
		num = kingdomDistances[k.id - 1];
		if (num < 0)
		{
			return int.MaxValue;
		}
		return num;
	}

	public override void OnDefsReloaded()
	{
		stats?.OnDefsReloaded();
		ReLoadFromDef();
		RefreshTraditions();
		RefreshAdvantages();
	}

	public override void OnInit()
	{
		if (game == null || game.state == Game.State.InLobby)
		{
			return;
		}
		using (Game.Profile("Kingdom.OnInit"))
		{
			using (Game.Profile("Create stats"))
			{
				stats = new Stats(this);
			}
			using (Game.Profile("Create actions"))
			{
				actions = new Actions(this);
			}
			using (Game.Profile("Init actions"))
			{
				InitActions();
			}
			using (Game.Profile("Create opinions"))
			{
				opinions = new Opinions(this);
			}
			using (Game.Profile("Create AI"))
			{
				ai = new KingdomAI(this);
			}
			using (Game.Profile("Create ApplyIncome"))
			{
				applyIncome = new ApplyIncome(this);
			}
			using (Game.Profile("Create Fame"))
			{
				fameObj = new Fame(this);
			}
			using (Game.Profile("Create Prestige"))
			{
				prestigeObj = new Prestige(this);
			}
			using (Game.Profile("Create MercenarySpawner"))
			{
				merc_spawner = new MercenarySpawner(this);
			}
			using (Game.Profile("Create CrownAuthority"))
			{
				if (GetComponent<CrownAuthority>() == null)
				{
					new CrownAuthority(this);
				}
			}
			using (Game.Profile("KingdomBankrupcy"))
			{
				if (GetComponent<KingdomBankruptcy>() == null)
				{
					new KingdomBankruptcy(this);
				}
			}
			using (Game.Profile("Create Inheritance"))
			{
				if (GetComponent<Inheritance>() == null)
				{
					new Inheritance(this);
				}
			}
			using (Game.Profile("Create RoyalDungeon"))
			{
				royal_dungeon = new RoyalDungeon(this);
			}
			using (Game.Profile("Create stability"))
			{
				stability = new KingdomStability(this);
				stability.Build();
			}
			using (Game.Profile("Find influence def"))
			{
				influenceDef = game.defs.Get<Influence.Def>("Influence");
			}
			using (Game.Profile("Create quests"))
			{
				new Quests(this);
			}
		}
	}

	public override void ClearAllComponents()
	{
		stats = null;
		actions = null;
		opinions = null;
		ai = null;
		applyIncome = null;
		fameObj = null;
		prestigeObj = null;
		royal_dungeon = null;
		influenceDef = null;
		stability = null;
		base.ClearAllComponents();
	}

	public override Stats GetStats()
	{
		return stats;
	}

	protected override void OnStart()
	{
		base.OnStart();
		InitVassalage();
		RecalcBuildingStates();
		UpdateRealmTags();
		RefreshAdvantages(create: true);
		if (!IsDefeated())
		{
			StartExistingKingdomUpdates();
		}
		royal_dungeon?.StartTimer();
		stability?.SpecialEvent(think_rebel: false);
	}

	protected override void OnDestroy()
	{
		Challenge.DestroyAll(this);
		base.OnDestroy();
	}

	private void StopExistingKingdomUpdates()
	{
		applyIncome?.StopUpdating();
		fameObj?.StopUpdating();
	}

	private void StartExistingKingdomUpdates()
	{
		if (game.state == Game.State.Running)
		{
			applyIncome.StartUpdating();
			fameObj.StartUpdating();
		}
	}

	public void OnNewGameAnalytics()
	{
		if (game.new_game && is_local_player && game.campaign != null && game.rules != null)
		{
			Vars vars = new Vars();
			vars.Set("multiplayer", game.IsMultiplayer().ToString());
			string text = game.rules.main_goal;
			if (text == "None")
			{
				text = "GrandCampaign";
			}
			vars.Set("gameType", text);
			string gameTypeTarget = game.rules.GetGameTypeTarget();
			if (!string.IsNullOrEmpty(gameTypeTarget))
			{
				vars.Set("gameTypeTarget", gameTypeTarget);
			}
			vars.Set("aiDifficulty", game.rules.GetVar("ai_difficulty").String("unknown"));
			string period = game.campaign.GetPeriod();
			vars.Set("startingPeriod", string.IsNullOrEmpty(period) ? "unknown" : period);
			string mapName = game.campaign.GetMapName();
			vars.Set("mapSize", string.IsNullOrEmpty(mapName) ? "unknown" : mapName);
			vars.Set("historicalKingdom", (game.rules.GetKingdomSize() == Game.CampaignRules.useDefaultKingdomSize).ToString());
			vars.Set("startingRealmsCount", GetStartingRealmsCount());
			string text2 = CampaignUtils.GetSelectedOption(game.campaign, "pick_kingdom")?.key;
			if (text2 == null)
			{
				text2 = CampaignUtils.GetPreferredCampaignOption(game.campaign, "pick_kingdom").String("unknown");
			}
			vars.Set("pickRule", text2);
			string text3 = CampaignUtils.GetSelectedOption(game.campaign, "aging_speed")?.key;
			vars.Set("agingSpeed", text3 ?? "unknown");
			vars.Set("espionageRole", game.rules.GetVar("espionage_role").String("unknown"));
			int num = -1;
			Value var = game.rules.GetVar("starting_gold");
			if (var.Int(-1) == -1)
			{
				num = (int)resources[ResourceType.Gold];
			}
			else
			{
				int num2 = game.rules.GetVar("starting_gold_multiplier").Int(1);
				num = var.Int() * num2;
			}
			vars.Set("startingGold", num);
			string text4 = game.rules.time_limit.String();
			vars.Set("timeLimit", string.IsNullOrEmpty(text4) ? "unlimited" : text4);
			NotifyListeners("analytics_new_game_started", vars);
		}
	}

	public void OnLoadGameAnalytics()
	{
		if (game.load_game != Game.LoadedGameType.Invalid && is_local_player)
		{
			Vars vars = new Vars();
			vars.Set("gameName", game.save_name);
			vars.Set("gameAction", game.load_game.ToString());
			vars.Set("menuLocation", Game.GetLoadGameLocation(game.load_game));
			NotifyListeners("analytics_game_loaded", vars);
		}
	}

	public void StartRecheckKingTimer()
	{
		if (IsAuthority())
		{
			Timer.Start(this, "recheck_king", 1f);
		}
	}

	public override void OnTimer(Timer timer)
	{
		switch (timer.name)
		{
		case "royal_dungeon_tick":
			royal_dungeon?.OnPrisonEvent();
			break;
		case "mercenary_spawn_timer":
			merc_spawner?.OnTimer();
			break;
		case "check_newborn":
			if (royalFamily != null)
			{
				if (royalFamily.CheckForNewBorn())
				{
					royalFamily.SetNextNewbornCheck(royalFamily.GetChildBirthMinTimeAfterMarriageOrBirth());
				}
				else
				{
					royalFamily.SetNextNewbornCheck(royalFamily.GetChildBirthCheckInterval());
				}
			}
			break;
		case "check_marrige":
			if (royalFamily != null)
			{
				royalFamily.CheckForMarrage();
				Timer.Start(this, "check_marrige", (float)game.Random(10, 35) + royalFamily.GetMarriageCheckInterval(), restart: true);
			}
			break;
		case "unlock_upgrade":
			FinishUpgrading();
			break;
		case "choose_patriarch_timeout":
			game.religions.orthodox.PatriarchChosen(this, null);
			break;
		case "restore_papacy_timeout":
			game.religions.catholic.RestorePapacyAnswer("timeout");
			break;
		case "recheck_king":
			if (IsDefeated() || !IsAuthority())
			{
				break;
			}
			if (royalFamily == null)
			{
				royalFamily = new RoyalFamily(this);
			}
			if (royalFamily.Sovereign != null && !royalFamily.Sovereign.IsDead())
			{
				break;
			}
			try
			{
				Game.Log($"Recreating broken king, {royalFamily.Sovereign}, for {this}", Game.LogType.Error);
				Character sovereign = royalFamily.Sovereign;
				royalFamily.Sovereign = CharacterFactory.CreateKing(this);
				DelCourtMember(sovereign);
				AddCourtMember(royalFamily.Sovereign);
				Vars vars = new Vars(royalFamily.Sovereign);
				vars.Set("isHeir", val: false);
				vars.Set("change_type", "new_dynasty");
				vars.Set("abdication_reason", "");
				vars.Set("old_sovereign", sovereign);
				FireEvent("royal_new_sovereign", vars);
				royalFamily.Sovereign.NotifyListeners("became_king");
				Vars vars2 = new Vars();
				if (sovereign != null)
				{
					vars2.Set("is_old_king_death", sovereign.IsDead() || !sovereign.IsValid());
				}
				vars2.Set("change_type", "new_dynasty");
				NotifyListeners("king_changed", vars2);
				FireEvent("generations_changed", "");
				SendState<RoyalFamilyState>();
				SendState<UsedNamesState>();
				break;
			}
			catch (Exception ex)
			{
				Game.Log("LOL, error even when recreating the old king, " + ex.ToString(), Game.LogType.Error);
				break;
			}
		default:
			base.OnTimer(timer);
			break;
		}
	}

	private void InitActions()
	{
		if (IsRegular())
		{
			actions.AddAll();
			NotifyListeners("actions_created");
		}
	}

	public void InitCourt()
	{
		if (court.Count != 0 || def == null)
		{
			return;
		}
		int num = Math.Max(1, def.GetInt("royal_court_slots", null, 10));
		wage_thresholds = new float[num + 1];
		wage_thresholds[0] = 0f;
		float num2 = 10f;
		for (int i = 0; i < num; i++)
		{
			court.Add(null);
			special_court.Add(null);
			DT.Field field = def.FindChild("wage_thresholds");
			if (field != null)
			{
				wage_thresholds[i + 1] = field.Float(i, null, num2);
			}
			else
			{
				wage_thresholds[i + 1] = num2;
			}
			num2 = Math.Max(num2, wage_thresholds[i + 1]);
		}
	}

	public void InitVassalage()
	{
		if (sovereignState != null && vassalage == null)
		{
			vassalage = new Vassalage(GetVassalageByType(Vassalage.Type.Vassal));
			vassalage.SetVasal(this);
			SendState<SovereignState>();
		}
	}

	public void AddRealm(Realm r, bool ignore_victory = false)
	{
		realms.Add(r);
		if (ai != null)
		{
			ai.refresh_realm_specialization = true;
		}
		r.stats?.SetKingdom(this);
		r.incomes?.SetKingdom(this);
		r.upkeeps?.SetKingdom(this);
		if (base.started)
		{
			if (realms.Count == 1)
			{
				StartExistingKingdomUpdates();
			}
			RefreshRealmTags();
		}
		NotifyListeners("realm_added", r);
		if (!base.started || ignore_victory || !game.ValidateEndGame(this))
		{
			for (int num = r.armies.Count - 1; num >= 0; num--)
			{
				Army a = r.armies[num];
				AddArmyIn(a, notify: false);
			}
		}
	}

	public void RemoveExternalBorderRealms(List<Realm> rs, bool check = false)
	{
		if (check)
		{
			for (int i = 0; i < rs.Count; i++)
			{
				Realm realm = rs[i];
				bool flag = true;
				for (int j = 0; j < realms.Count; j++)
				{
					if (realms[j].logicNeighborsRestricted.Contains(realm))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					RemoveExternalBorderRealm(realm);
				}
			}
		}
		else
		{
			for (int k = 0; k < rs.Count; k++)
			{
				RemoveExternalBorderRealm(rs[k]);
			}
		}
	}

	public void DelRealm(Realm r, int new_kingdom_id, bool ignore_victory = false)
	{
		realms.Remove(r);
		RemoveExternalBorderRealms(r.logicNeighborsRestricted, check: true);
		if (base.started)
		{
			RefreshRealmTags();
			InvalidateIncomes();
		}
		if (IsDefeated())
		{
			StopExistingKingdomUpdates();
			if (base.started && type == Type.Regular)
			{
				OnKingdomDestroyed(game.GetKingdom(new_kingdom_id), r, ignore_victory);
			}
		}
		for (int num = r.armies.Count - 1; num >= 0; num--)
		{
			Army a = r.armies[num];
			DelArmyIn(a);
		}
	}

	public void AddPotentialRebellion(Rebellion r, bool syncRebellion = true)
	{
		if (r != null && IsRegular())
		{
			if (!potentialRebellions.Contains(r))
			{
				potentialRebellions.Add(r);
			}
			if (syncRebellion)
			{
				r.AddPotentialAffectedKingdom(this, syncKingdom: false);
			}
		}
	}

	public void DelPotentialRebellion(Rebellion r, bool syncRebellion = true)
	{
		if (r != null)
		{
			potentialRebellions.Remove(r);
			if (syncRebellion)
			{
				r.DelPotentialAffectedKingdom(this, syncKingdom: false);
			}
		}
	}

	public void AddRebellion(Rebellion r, bool syncRebellion = true)
	{
		if (r != null)
		{
			if (!rebellions.Contains(r))
			{
				rebellions.Add(r);
				NotifyListeners("rebellions_changed");
			}
			if (syncRebellion)
			{
				r.AddAffectedKingdom(this, syncKingdom: false);
			}
		}
	}

	public void DelRebellion(Rebellion r, bool syncRebellion = true)
	{
		if (r != null)
		{
			if (rebellions.Remove(r))
			{
				NotifyListeners("rebellions_changed");
			}
			if (syncRebellion)
			{
				r.DelAffectedKingdom(this, syncKingdom: false);
			}
		}
	}

	public void OnChangedAnalytics(string evt, string old_value, string new_value)
	{
		if (IsAuthority() && !Game.isLoadingSaveGame && !string.IsNullOrEmpty(evt) && !string.IsNullOrEmpty(old_value) && !string.IsNullOrEmpty(new_value))
		{
			Vars vars = new Vars();
			vars.Set("kingdomEvent", evt);
			vars.Set("previousValue", old_value);
			vars.Set("newValue", new_value);
			FireEvent("analytics_kingdom_changed", vars, id);
		}
	}

	public Realm GetWeightedRebelliosRealm(float min_rebel_pop = 0f)
	{
		if (realms == null || realms.Count == 0)
		{
			return null;
		}
		float num = 0f;
		for (int i = 0; i < realms.Count; i++)
		{
			if (!((float)realms[i].castle.population.GetRebels() < min_rebel_pop))
			{
				num += realms[i].rebellionRisk.GetRebelliosWeight();
			}
		}
		float num2 = game.Random(0f, num);
		for (int j = 0; j < realms.Count; j++)
		{
			if (!((float)realms[j].castle.population.GetRebels() < min_rebel_pop))
			{
				float rebelliosWeight = realms[j].rebellionRisk.GetRebelliosWeight();
				if (num2 < rebelliosWeight)
				{
					return realms[j];
				}
				num2 -= rebelliosWeight;
			}
		}
		return null;
	}

	public void AddArmy(Army a, bool add_court_member = true)
	{
		if (a.IsValid())
		{
			if (!armies.Contains(a))
			{
				armies.Add(a);
			}
			if (a.IsMercenary() && !mercenaries.Contains(a))
			{
				mercenaries.Add(a);
			}
			if (add_court_member && IsAuthority())
			{
				AddCourtMember(a.leader);
			}
		}
	}

	public void DelArmy(Army a, bool del_court_member = true)
	{
		armies.Remove(a);
		DelMercenary(a);
		if (del_court_member && IsAuthority())
		{
			DelCourtMember(a.leader);
		}
	}

	public void DelMercenary(Army a)
	{
		mercenaries.Remove(a);
	}

	private void HandleArmiesOnDefeat(Kingdom destroyed_by, Realm last_realm)
	{
		if (!AssertAuthority())
		{
			return;
		}
		Character character = game.religions.catholic.crusade?.leader;
		if (character != null && character.kingdom_id == id)
		{
			game.religions.catholic.crusade.end_reason = "kingdom_defeated";
			game.religions.catholic.crusade.End();
		}
		while (armies.Count > 0)
		{
			Army army = armies[armies.Count - 1];
			armies.RemoveAt(armies.Count - 1);
			if (army == null)
			{
				continue;
			}
			bool flag = army.realm_in?.id == last_realm?.id;
			int num = 0;
			while (!flag && num < last_realm.neighbors.Count)
			{
				if (last_realm.neighbors[num].id == army.realm_in?.id)
				{
					flag = true;
				}
				num++;
			}
			Character leader = army.leader;
			if (leader != null && flag && (float)game.Random(0, 100) < loyalist_spawn_mod * (float)leader.GetClassLevel())
			{
				if (leader.IsKing())
				{
					royalFamily.Sovereign = null;
				}
				Rebel rebel = leader.TurnIntoRebel("GeneralLoyalists", "LoyalistsSpawnCondition", this, last_realm);
				rebel?.AddEnemy(destroyed_by.id);
				if (rebel == null && army.IsValid())
				{
					army.Destroy();
				}
			}
			else
			{
				DelArmy(army);
				if (army.IsValid())
				{
					army.Destroy();
				}
			}
		}
	}

	public void HandleRelationsOnDefeat(Kingdom destroyed_by)
	{
		if (!AssertAuthority())
		{
			return;
		}
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated() && kingdom != this && kingdom != destroyed_by && GetRelationship(kingdom) > RelationUtils.Def.GetLowerTreshold(RelationUtils.RelationshipType.Trusting))
			{
				destroyed_by.AddRelationModifier(kingdom, "rel_destroyed_friend", this);
			}
		}
	}

	public float GetAvarageArmyMorale()
	{
		float num = 0f;
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			num += army.GetMorale();
		}
		return num / (float)armies.Count;
	}

	public void AddArmyIn(Army a, bool notify)
	{
		AddMercenary(a);
		armies_in.Add(a);
		if (notify)
		{
			NotifyListeners("army_entered", a);
		}
	}

	public void DelArmyIn(Army a)
	{
		DelMercenaryIn(a);
		armies_in.Remove(a);
		NotifyListeners("army_exited", a);
	}

	public void AddMercenary(Army a)
	{
		if (a.IsValid() && a.IsMercenary() && !mercenaries_in.Contains(a))
		{
			mercenaries_in.Add(a);
		}
	}

	public void DelMercenaryIn(Army a)
	{
		mercenaries_in.Remove(a);
	}

	public void AddPrisoner(Character c, bool relocating = false)
	{
		if (!prisoners.Contains(c))
		{
			c.prison_kingdom = this;
			prisoners.Add(c);
			NotifyListeners("prison_changed", c);
		}
	}

	public void DelPrisoner(Character c, bool relocating = false)
	{
		if (!prisoners.Remove(c))
		{
			return;
		}
		c.prison_kingdom = null;
		NotifyListeners("prison_changed", c);
		if (!relocating)
		{
			c.NotifyListeners("released_from_prison");
		}
		bool flag = true;
		for (int i = 0; i < prisoners.Count && flag; i++)
		{
			if (prisoners[i].kingdom_id == c.kingdom_id)
			{
				flag = false;
			}
		}
		if (flag)
		{
			NotifyListeners("we_have_no_prisoners", c.GetKingdom());
		}
	}

	public int FindBookIdx(Book.Def def)
	{
		for (int i = 0; i < books.Count; i++)
		{
			if (books[i].def == def)
			{
				return i;
			}
		}
		return -1;
	}

	public Book FindBook(Book.Def def)
	{
		int num = FindBookIdx(def);
		if (num < 0)
		{
			return null;
		}
		return books[num];
	}

	public void AddBook(Book.Def def, int copies = 1, bool from_state = false)
	{
		Book book = FindBook(def);
		if (book != null)
		{
			book.copies += copies;
		}
		else
		{
			Book item = Book.Create(def, this, copies);
			books.Add(item);
		}
		NotifyListeners("books_changed");
		if (!from_state)
		{
			SendState<BooksState>();
		}
	}

	public void DelBook(Book.Def def, int copies = 1)
	{
		int num = FindBookIdx(def);
		if (num >= 0)
		{
			Book book = books[num];
			book.copies -= copies;
			if (book.copies <= 0)
			{
				books.RemoveAt(num);
				book.Destroy();
			}
			NotifyListeners("books_changed");
			SendState<BooksState>();
		}
	}

	public void AddHelpWithRebelsOf(Kingdom k, float forceTime = float.NaN, bool send_state = true)
	{
		if (k != null && !is_player)
		{
			Time item = ((!float.IsNaN(forceTime)) ? (game.time + forceTime) : (game.time + def.GetFloat("help_with_rebels_time")));
			ai.helpWithRebels.RemoveAll((Tuple<Kingdom, Time> t) => t.Item1 == k);
			ai.helpWithRebels.Add(new Tuple<Kingdom, Time>(k, item));
			if (send_state)
			{
				SendState<HelpWithRebelsState>();
			}
		}
	}

	private void ConcludeAllWars()
	{
		for (int num = wars.Count - 1; num >= 0; num--)
		{
			Kingdom enemyLeader = wars[num].GetEnemyLeader(this);
			EndWarWith(enemyLeader, enemyLeader, "conclude_all_wars");
		}
	}

	private void LeaveAllPacts()
	{
		for (int num = pacts.Count - 1; num >= 0; num--)
		{
			pacts[num].Leave(this);
		}
		for (int num2 = pacts_against.Count - 1; num2 >= 0; num2--)
		{
			pacts_against[num2].Dissolve();
		}
	}

	private void RecallAllForeighners()
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom == null || kingdom.IsDefeated() || kingdom == this)
			{
				continue;
			}
			List<Character> list = null;
			for (int j = 0; j < kingdom.court.Count; j++)
			{
				Character character = kingdom.court[j];
				if (character?.cur_action?.target as Kingdom == this || foreigners.Contains(character))
				{
					if (list == null)
					{
						list = new List<Character>();
					}
					character.Recall();
					list.Add(character);
				}
			}
			if (list != null)
			{
				Vars vars = new Vars();
				vars.Set("characters", list);
				vars.Set("kingdom", this);
				kingdom.FireEvent("kingdom_destroyed_recall_characters", vars, kingdom.id);
			}
		}
	}

	private void RenounceAllPrisoners()
	{
		for (int num = court.Count - 1; num >= 0; num--)
		{
			Character character = court[num];
			if (character != null && character.prison_kingdom != null)
			{
				character.Exile();
			}
		}
	}

	private void ClearAllVasslage()
	{
		if (sovereignState != null)
		{
			sovereignState.DelVassalState(this);
		}
		while (vassalStates.Count > 0)
		{
			DelVassalState(vassalStates[0]);
		}
	}

	private void ClearAllNonAgressions()
	{
		while (nonAgressions.Count > 0)
		{
			UnsetStance(nonAgressions[0], RelationUtils.Stance.NonAggression);
		}
	}

	private void ClearAllMarriages()
	{
		for (int i = 0; i < marriages.Count; i++)
		{
			Kingdom otherKingdom = marriages[i].GetOtherKingdom(this);
			if (GetStance(otherKingdom).IsMarriage())
			{
				UnsetStance(otherKingdom, RelationUtils.Stance.Marriage);
			}
		}
		Inheritance component = GetComponent<Inheritance>();
		component.princesses.Clear();
		component.HandleNextPrincess();
		for (int j = 0; j < game.kingdoms.Count; j++)
		{
			Kingdom kingdom = game.kingdoms[j];
			if (!kingdom.IsDefeated() && kingdom != this)
			{
				component = game.kingdoms[j]?.GetComponent<Inheritance>();
				if (component?.currentPrincess != null && component.currentPrincess.kingdom_id == id)
				{
					component.HandleNextPrincess();
				}
			}
		}
	}

	public bool IsVassal()
	{
		return sovereignState != null;
	}

	public void FreeAllKeeps(Kingdom k = null)
	{
		if (occupiedKeeps == null)
		{
			return;
		}
		for (int num = occupiedKeeps.Count - 1; num >= 0; num--)
		{
			if (occupiedKeeps[num].keep_effects != null && (k == null || occupiedKeeps[num].GetRealm().kingdom_id == k.id))
			{
				occupiedKeeps[num].keep_effects.SetOccupied(null);
			}
		}
	}

	public bool IsGreatPower()
	{
		if (game.great_powers != null)
		{
			return game.great_powers.TopKingdoms().Contains(this);
		}
		return false;
	}

	public bool IsDominated(Kingdom k = null)
	{
		if (k == this || IsDefeated())
		{
			return false;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			if (k == null)
			{
				if (realms[i].controller == this)
				{
					return false;
				}
			}
			else if (realms[i].controller != k)
			{
				return false;
			}
		}
		return true;
	}

	public bool HoldsOccupiedRealmsIn(Kingdom k)
	{
		if (k == null || k == this || IsDefeated())
		{
			return false;
		}
		for (int i = 0; i < occupiedRealms.Count; i++)
		{
			if (occupiedRealms[i].GetKingdom() == k)
			{
				return true;
			}
		}
		return false;
	}

	public void FreeAllRealms(bool send_state = true)
	{
		for (int i = 0; i < wars.Count; i++)
		{
			List<Kingdom> enemies = wars[i].GetEnemies(this);
			if (enemies != null)
			{
				int num = 0;
				while (num < enemies.Count)
				{
					FreeAllRealmsOf(enemies[i], send_state);
					i++;
				}
			}
		}
	}

	public void AddOcuppiedRealm(Realm r, bool send_state = true)
	{
		if (r != null && !occupiedRealms.Contains(r))
		{
			occupiedRealms.Add(r);
			if (send_state)
			{
				SendState<OccupiedRealmsState>();
			}
		}
	}

	public void DelOcuppiedRealm(Realm r, bool send_state = true)
	{
		if (r != null)
		{
			occupiedRealms.Remove(r);
		}
	}

	public void FreeAllRealmsOf(Kingdom k, bool send_state = true)
	{
		if (k == null)
		{
			return;
		}
		for (int i = 0; i < k.realms.Count; i++)
		{
			Realm realm = k.realms[i];
			if (realm.controller == this)
			{
				realm.SetOccupied(k, force: false, send_state);
			}
		}
	}

	public void ClaimAllOccupiedRealmsFrom(Kingdom k = null)
	{
		if (k == null)
		{
			return;
		}
		using (new CacheRBS("ClaimAllOccupiedRealmsFrom"))
		{
			for (int i = 0; i < k.realms.Count; i++)
			{
				Realm realm = k.realms[i];
				if (realm.controller == this)
				{
					realm.SetKingdom(id);
				}
			}
		}
	}

	private void ClearAllTradeAgreementsAndRoutes()
	{
		while (tradeAgreementsWith.Count > 0)
		{
			Kingdom kingdom = tradeAgreementsWith[0];
			CloseTradeRoute(kingdom);
			if (kingdom.HasTradeAgreement(this))
			{
				UnsetStance(kingdom, RelationUtils.Stance.Trade);
			}
		}
	}

	private void CancelAllOffers()
	{
		Offers component = GetComponent<Offers>();
		while (component?.incoming != null && component.incoming.Count > 0)
		{
			component.incoming[0].Cancel();
		}
		while (component?.outgoing != null && component.outgoing.Count > 0)
		{
			component.outgoing[0].Cancel();
		}
		FireEvent("close_audience", this);
	}

	private void ClearCourt()
	{
		for (int num = court.Count - 1; num >= 0; num--)
		{
			Character character = court[num];
			if (character != null && character.IsAlive())
			{
				character.Die();
			}
		}
		if (special_court != null)
		{
			for (int i = 0; i < special_court.Count; i++)
			{
				Character character2 = special_court[i];
				if (character2 != null)
				{
					DelSpecialCourtMember(character2);
				}
			}
		}
		court.Clear();
		special_court.Clear();
		SendState<CourtState>();
	}

	private void ClearTruces()
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated())
			{
				KingdomAndKingdomRelation.ClearTruceTime(this, kingdom, game);
			}
		}
	}

	public void HandlePrisonersOnDefeat(Kingdom destroyed_by)
	{
		if ((!IsDefeated() && !IsDominated()) || prisoners == null)
		{
			return;
		}
		List<Character> list = new List<Character>();
		Dictionary<Kingdom, List<Character>> dictionary = new Dictionary<Kingdom, List<Character>>();
		List<Character> list2 = new List<Character>();
		for (int num = prisoners.Count - 1; num >= 0; num--)
		{
			Character character = prisoners[num];
			Kingdom kingdom = character.GetKingdom();
			if (!character.IsOwnStance(destroyed_by) && destroyed_by.type == Type.Regular)
			{
				character.Imprison(destroyed_by, recall: true, send_state: true, "prison_kingdom_defeated");
				list2.Add(character);
				if (kingdom.IsRegular())
				{
					if (!dictionary.TryGetValue(kingdom, out var value))
					{
						value = new List<Character>();
						dictionary.Add(kingdom, value);
					}
					value.Add(character);
				}
			}
			else
			{
				character.Imprison(null, recall: true, send_state: true, "prison_kingdom_defeated");
				list.Add(character);
			}
		}
		foreach (KeyValuePair<Kingdom, List<Character>> item in dictionary)
		{
			Kingdom key = item.Key;
			List<Character> value2 = item.Value;
			Vars vars = new Vars();
			vars.Set("prisoners", value2);
			vars.Set("old_kingdom", this);
			vars.Set("new_kingdom", destroyed_by);
			key.FireEvent("prisoners_changed_kingdom", vars, key.id);
		}
		if (destroyed_by.IsRegular() && (list2.Count != 0 || list.Count != 0))
		{
			Vars vars2 = new Vars();
			if (list2.Count != 0)
			{
				vars2.Set("prisoners", list2);
			}
			if (list.Count != 0)
			{
				vars2.Set("released", list);
			}
			vars2.Set("kingdom", this);
			destroyed_by.FireEvent("prisoners_gained", vars2, destroyed_by.id);
		}
	}

	public void OnKingdomDestroyed(Kingdom destroyed_by, Realm last_realm, bool ignore_victory = false)
	{
		if (!ignore_victory && game.ValidateEndGame(destroyed_by, this))
		{
			return;
		}
		Vars vars = new Vars();
		vars.Set("destroyed_kingdom", this);
		vars.Set("destroyed_by", destroyed_by);
		if (vassalStates.Count != 0)
		{
			vars.Set("vassal_states", vassalStates);
		}
		if (sovereignState != null)
		{
			vars.Set("sovereign", sovereignState);
		}
		vars.Set("was_at_war", FindWarWith(destroyed_by) != null);
		vars.Set("was_at_war_with_local_player", FindWarWith(game.GetLocalPlayerKingdom()) != null);
		FireEvent("kingdom_destroyed", vars);
		destroyed_by?.NotifyListeners("destroyed_kingdom", vars);
		potentialRebellions?.Clear();
		if (IsAuthority())
		{
			HandleArmiesOnDefeat(destroyed_by, last_realm);
			HandleRelationsOnDefeat(destroyed_by);
			ConcludeAllWars();
			LeaveAllPacts();
			RecallAllForeighners();
			RenounceAllPrisoners();
			HandlePrisonersOnDefeat(destroyed_by);
			ClearAllVasslage();
			ClearAllNonAgressions();
			ClearAllMarriages();
			FreeAllKeeps();
			ClearAllTradeAgreementsAndRoutes();
			CancelAllOffers();
			royalFamily?.ClearFamily();
			ClearCourt();
			ClearTruces();
			if (IsGreatPower())
			{
				game.rankings.CalcOnly("FameRanking");
				game.great_powers.TopKingdoms(recalc: true);
			}
			if (Name != ActiveName)
			{
				ChangeNameAndCulture(Name);
			}
			SetDefeatedBy(destroyed_by);
		}
		if (goods_produced.Count > 0)
		{
			UpdateRealmTags(force: true);
		}
	}

	public Character HireCharacter(string class_id, int index = -1, bool for_free = false)
	{
		if (!for_free)
		{
			Resource cost = ForHireStatus.GetCost(game, this, class_id);
			if (cost != null && !resources.CanAfford(cost, 1f))
			{
				return null;
			}
		}
		Character character = CharacterFactory.CreateCourtCandidate(this, class_id);
		if (character == null)
		{
			return null;
		}
		ForHireStatus forHireStatus = new ForHireStatus(null);
		character.SetStatus(forHireStatus, send_state: false);
		if (forHireStatus.Hire(index, for_free))
		{
			return character;
		}
		character.Destroy();
		return null;
	}

	public bool AddCourtMember(Character c, int index = -1, bool is_hire = false, bool send_state = true, bool send_event = false, bool check_foreign = true)
	{
		if (c == null)
		{
			return false;
		}
		if (type != Type.Regular)
		{
			return false;
		}
		if (!c.IsAuthority() && send_state)
		{
			if (send_event)
			{
				c.SendEvent(new Character.HireCharacterEvent(index, id));
			}
			return false;
		}
		InitCourt();
		if (index == -1)
		{
			index = special_court.FindIndex((Character rebel) => rebel == c);
		}
		if (court.Count <= index)
		{
			return false;
		}
		if (c == null)
		{
			return false;
		}
		if (court.Contains(c))
		{
			return false;
		}
		int idx = index;
		DelSpecialCourtMember(c, send_state);
		if (check_foreign && c.GetKingdom() != this && IsRegular())
		{
			Game.Log($"Adding foreign character {c} to the regular court of {this}!", Game.LogType.Error);
		}
		switch (index)
		{
		case -1:
		{
			if (c.IsKing())
			{
				if (court[0] != null && !court[0].IsAlive() && court[0].IsValid())
				{
					court[0].Destroy();
				}
				court[0] = c;
				idx = 0;
				break;
			}
			int freeCourtSlotIndex = GetFreeCourtSlotIndex();
			if (freeCourtSlotIndex != -1)
			{
				court[freeCourtSlotIndex] = c;
				idx = freeCourtSlotIndex;
				break;
			}
			return false;
		}
		case 0:
			if (!c.IsKing())
			{
				Game.Log($"Try to add non-king character ({c}) at court index 0 is forbidden!", Game.LogType.Error);
				return false;
			}
			court[index] = c;
			break;
		default:
		{
			if (c.IsKing() && index != 0)
			{
				Game.Log($"Try to add a King ({c}) on slot diffrent that 0!", Game.LogType.Error);
				return false;
			}
			Character courtOrSpecialCourtMember = GetCourtOrSpecialCourtMember(index);
			if (courtOrSpecialCourtMember != null && !courtOrSpecialCourtMember.destroyed)
			{
				Game.Log($"Trying to add {c} at slot {index}, but it's occupied by {courtOrSpecialCourtMember}!", Game.LogType.Error);
				return false;
			}
			court[index] = c;
			break;
		}
		}
		Pact.RevealPacts(c);
		c.GetNewSkillOptions(null);
		c.UpdateAutomaticStatuses();
		NotifyListeners("add_court", c);
		if (is_hire)
		{
			NotifyListeners("new_knight_hired", c);
			if (send_state)
			{
				SendSubstate<CourtState.NewKnightHiredStatus>(idx);
			}
		}
		NotifyListeners("court_changed", c);
		InvalidateIncomes();
		if (send_state)
		{
			SendSubstate<CourtState.CourtMemberState>(idx);
			OnCourtChangedAnalytics("analytics_court_add", c);
		}
		return true;
	}

	public void OnCourtChangedAnalytics(string analytics_event, Character c)
	{
		if (game.IsRunning() && IsAuthority() && is_player && !string.IsNullOrEmpty(analytics_event) && c != null)
		{
			FireEvent(analytics_event, c);
		}
	}

	public bool SwapCourtSlots(int slot_idx_1, int slot_idx_2, bool send_state = true)
	{
		InitCourt();
		if (slot_idx_1 < 0 || slot_idx_1 >= court.Count)
		{
			return false;
		}
		if (slot_idx_2 < 0 || slot_idx_2 >= court.Count)
		{
			return false;
		}
		if (slot_idx_1 == slot_idx_2)
		{
			return false;
		}
		if (slot_idx_1 == 0 || slot_idx_2 == 0)
		{
			return false;
		}
		Character courtOrSpecialCourtMember = GetCourtOrSpecialCourtMember(slot_idx_1);
		Character courtOrSpecialCourtMember2 = GetCourtOrSpecialCourtMember(slot_idx_2);
		if (courtOrSpecialCourtMember == null && courtOrSpecialCourtMember2 == null)
		{
			return false;
		}
		if (!IsAuthority() && send_state)
		{
			SendEvent(new SwapCourteSlotsEvent(slot_idx_1, slot_idx_2));
			return false;
		}
		SetCourtOrSpecialCourtMember(slot_idx_1, courtOrSpecialCourtMember2);
		SetCourtOrSpecialCourtMember(slot_idx_2, courtOrSpecialCourtMember);
		if (send_state)
		{
			SendState<CourtState>();
			OnCourtChangedAnalytics("analytics_court_swap", courtOrSpecialCourtMember);
			OnCourtChangedAnalytics("analytics_court_swap", courtOrSpecialCourtMember2);
		}
		NotifyListeners("court_changed");
		return true;
	}

	public int GetFreeCourtSlotIndex()
	{
		int result = -1;
		for (int i = 1; i < court.Count; i++)
		{
			if (court[i] == null && special_court[i] == null)
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public int GetFreeCourtSlots()
	{
		int num = 0;
		for (int i = 1; i < court.Count; i++)
		{
			if (court[i] == null && special_court[i] == null)
			{
				num++;
			}
		}
		return num;
	}

	public void DelCourtMember(Character c, bool send_state = true, bool kill_or_throneroom = true)
	{
		if (send_state)
		{
			AssertAuthority();
		}
		if (c == null)
		{
			return;
		}
		int num = court.FindIndex((Character x) => x == c);
		if (num == -1)
		{
			return;
		}
		if (!IsAuthority() && send_state)
		{
			SendEvent(new DeleteCourtMemberEvent(num, kill_or_throneroom));
			return;
		}
		court[num] = null;
		NotifyListeners("del_court", c);
		OnCourtChangedAnalytics("analytics_court_delete", c);
		if (kill_or_throneroom && c.IsAlive())
		{
			if (royalFamily != null && royalFamily.IsFamilyMember(c))
			{
				c.SetStatus<AvailableForAssignmentStatus>();
			}
			else
			{
				c.Die(new DeadStatus("exile", c));
			}
		}
		NotifyListeners("court_changed", c);
		InvalidateIncomes();
		if (send_state)
		{
			SendSubstate<CourtState.CourtMemberState>(num);
		}
	}

	public bool IsCourtMember(Character c)
	{
		return court.FindIndex((Character x) => x == c) != -1;
	}

	public void AddToSpecialCourt(Character c, int index = -1, bool send_state = true)
	{
		if (send_state)
		{
			AssertAuthority();
		}
		if (c != null && special_court != null && special_court.FindIndex((Character sc) => sc == c) == -1 && (index >= 0 || (index = GetFreeCourtSlotIndex()) >= 0) && index < special_court.Count && (special_court[index] == null || court[index] == null))
		{
			special_court[index] = c;
			if (send_state)
			{
				SendSubstate<CourtState.SpecialCourtMemberState>(index);
				OnCourtChangedAnalytics("analytics_special_court_add", c);
			}
			c.SetSpecialCourtKingdom(this, send_state);
			NotifyListeners("court_changed");
		}
	}

	public void MoveToSpecialCourt(Character c, bool send_state = true)
	{
		if (send_state)
		{
			AssertAuthority();
		}
		if (c != null && special_court != null && court != null)
		{
			int index = court.FindIndex((Character x) => x == c);
			DelCourtMember(c, send_state, kill_or_throneroom: false);
			AddToSpecialCourt(c, index, send_state);
		}
	}

	public void DelSpecialCourtMember(Character c, bool send_state = true)
	{
		if (send_state)
		{
			AssertAuthority();
		}
		if (c == null || special_court == null)
		{
			return;
		}
		int num = special_court.FindIndex((Character rebel) => rebel == c);
		if (num != -1)
		{
			special_court[num] = null;
			c.SetMissionKingdom(null, send_state);
			if (send_state)
			{
				SendSubstate<CourtState.SpecialCourtMemberState>(num);
				OnCourtChangedAnalytics("analytics_special_court_delete", c);
			}
			c.SetSpecialCourtKingdom(null, send_state);
			NotifyListeners("court_changed");
		}
	}

	public Character GetCourtOrSpecialCourtMember(int i)
	{
		if (i < 0 || i >= court.Count)
		{
			return null;
		}
		Character character = court[i];
		if (character != null)
		{
			return character;
		}
		return special_court[i];
	}

	private void SetCourtOrSpecialCourtMember(int i, Character c)
	{
		if (c == null)
		{
			court[i] = null;
			special_court[i] = null;
		}
		else if (c.IsInSpecialCourt(this))
		{
			court[i] = null;
			special_court[i] = c;
		}
		else
		{
			court[i] = c;
			special_court[i] = null;
		}
	}

	public void AddVassalState(Kingdom newVassalState, bool set_sovereign = true, bool send_state = true)
	{
		if (!(!IsAuthority() && send_state) && newVassalState != null && newVassalState != this && !vassalStates.Contains(newVassalState))
		{
			if (set_sovereign)
			{
				newVassalState.SetSovereignState(this, Vassalage.Type.Vassal, set_vassal: false, send_state);
			}
			vassalStates.Add(newVassalState);
			if (IsAuthority())
			{
				newVassalState.SetStance(this, RelationUtils.Stance.AnyVassalage);
			}
			if (base.started)
			{
				NotifyListeners("vassal_added");
			}
			if (base.started && send_state)
			{
				NotifyListeners("vassals_changed");
			}
			if (send_state)
			{
				SendState<VassalState>();
			}
		}
	}

	public void DelVassalState(Kingdom vassalState, bool set_sovereign = true, bool send_state = true)
	{
		if (vassalState != null && vassalStates.Contains(vassalState))
		{
			if (is_player)
			{
				Game.Log(string.Concat("Remove vassal ", vassalState, " from ", this), Game.LogType.Message);
			}
			if (set_sovereign)
			{
				vassalState.SetSovereignState(null, Vassalage.Type.Vassal, set_vassal: false, send_state);
			}
			vassalStates.Remove(vassalState);
			if (IsAuthority())
			{
				vassalState.UnsetStance(this, RelationUtils.Stance.AnyVassalage);
			}
			if (base.started)
			{
				NotifyListeners("vassals_changed");
			}
			if (send_state)
			{
				SendState<VassalState>();
			}
		}
	}

	public void SetSovereignState(Kingdom newSovereignState, Vassalage.Type vassalType = Vassalage.Type.Vassal, bool set_vassal = true, bool send_state = true)
	{
		if (sovereignState == newSovereignState || newSovereignState == this)
		{
			return;
		}
		if (sovereignState != null)
		{
			sovereignState.DelVassalState(this, set_sovereign: false, send_state);
		}
		sovereignState = newSovereignState;
		Vassalage.Def vassalageByType = GetVassalageByType(vassalType);
		if (vassalageByType != null)
		{
			if (newSovereignState != null)
			{
				vassalage = new Vassalage(vassalageByType);
				vassalage.SetVasal(this);
			}
			else
			{
				if (vassalage != null)
				{
					vassalage.SetVasal(null);
				}
				vassalage = null;
			}
		}
		if (set_vassal)
		{
			sovereignState?.AddVassalState(this, set_sovereign: false, send_state);
		}
		if (base.started)
		{
			if (is_player)
			{
				Game.Log(string.Concat("Set Sovereign of  ", this, " to ", newSovereignState), Game.LogType.Message);
			}
			Religion.RefreshModifiers(this);
			InvalidateIncomes();
			if (sovereignState != null)
			{
				sovereignState.InvalidateIncomes();
			}
			if (newSovereignState != null && IsCaliphate())
			{
				actions.Find("AbandonCaliphateAction")?.Execute(this);
			}
			if (sovereignState != null && IsAuthority())
			{
				KingdomAndKingdomRelation.Modify("rel_kingdom_became_vassal", this, sovereignState, null);
			}
			if (send_state)
			{
				SendState<SovereignState>();
			}
			if (newSovereignState == null)
			{
				NotifyListeners("sovereign_removed");
			}
			else
			{
				NotifyListeners("sovereign_set");
			}
		}
	}

	public void ChangeVassalageType(Vassalage.Type vassalType, bool send_state = true)
	{
		Vassalage.Def vassalageByType = GetVassalageByType(vassalType);
		if (vassalageByType != null && vassalage != null && vassalage.def != vassalageByType)
		{
			vassalage.ChangeDef(vassalageByType);
		}
		InvalidateIncomes();
		if (sovereignState != null)
		{
			sovereignState.InvalidateIncomes();
		}
		if (send_state)
		{
			SendState<SovereignState>();
		}
		NotifyListeners("sovereign_set");
	}

	public void SetDefeatedBy(Kingdom kingdom, bool send_state = true)
	{
		if (defeated_by != kingdom)
		{
			defeated_by = kingdom;
			if (send_state)
			{
				SendState<DefeatedByState>();
			}
		}
	}

	public void SetTaxRate(int taxLevel, bool send_state = true)
	{
		if (taxLevel != this.taxLevel)
		{
			bool flag = taxLevel > this.taxLevel;
			int num = this.taxLevel;
			this.taxLevel = taxLevel;
			InvalidateIncomes();
			NotifyListeners("tax_rate_changed", flag);
			OnChangedAnalytics("crown_authority", num.ToString(), taxLevel.ToString());
			if (send_state)
			{
				SendState<TaxRateState>();
			}
		}
	}

	public void TakeLoan(float loanSize)
	{
		AddResources(KingdomAI.Expense.Category.Economy, ResourceType.Gold, loanSize);
		NotifyListeners("take_loan", loanSize);
	}

	public void SetReligion(Religion religion, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			SendEvent(new SetReligionEvent(religion));
		}
		else
		{
			if (this.religion == religion)
			{
				return;
			}
			Religion religion2 = this.religion;
			if (IsAuthority() && religion2 is Orthodox orthodox)
			{
				orthodox.CancelChoosePatriarch(this);
				if (HasEcumenicalPatriarch())
				{
					orthodox.DelPatriarchFromCourt(this);
					patriarch?.StopGoverning();
					patriarch?.Recall();
				}
				else
				{
					orthodox.SetPatriarch(this, null);
				}
				subordinated = true;
			}
			if (IsAuthority() && religion2 is Pagan)
			{
				for (int i = 0; i < court.Count; i++)
				{
					Character character = court[i];
					if (character != null && character.IsCleric())
					{
						character.cur_action?.Cancel();
						character.StopPromotingPaganBelief(apply_penalties: false);
					}
				}
			}
			if (this.religion != null)
			{
				this.religion.DelModifiers(this);
			}
			this.religion = religion;
			religion.AddModifiers(this);
			for (int j = 0; j < court.Count; j++)
			{
				court[j]?.OnKingdomReligionChanged();
			}
			InvalidateIncomes();
			for (int k = 0; k < realms.Count; k++)
			{
				Realm realm = realms[k];
				realm.FixPietyType();
				realm.UpdateUnitSets();
			}
			if (send_state)
			{
				SendState<ReligionState>();
			}
			if (religion2 != null && IsAuthority())
			{
				religion.ApplyReligionChangeClericOutcomes(this);
			}
			NotifyListeners("religion_changed", religion2);
			Catholic.CheckCardinalTitles(this);
			if (IsAuthority() && this.religion is Orthodox orthodox2 && HasEcumenicalPatriarch())
			{
				orthodox2.head.EnableAging(can_die: true);
				if (subordinated)
				{
					orthodox2.SetSubordinated(this, subordinated: false, orthodox2.head);
				}
				else
				{
					orthodox2.SetPatriarch(this, orthodox2.head);
				}
			}
		}
	}

	public bool AddPaganBelief(string name)
	{
		AssertAuthority();
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (!is_pagan)
		{
			return true;
		}
		Religion.PaganBelief paganBelief = game.religions.pagan.def.FindPaganBelief(name);
		if (paganBelief == null || pagan_beliefs.IndexOf(paganBelief) >= 0)
		{
			return false;
		}
		pagan_beliefs.Add(paganBelief);
		religion.DelModifiers(this);
		religion.AddModifiers(this);
		for (int i = 0; i < armies.Count; i++)
		{
			armies[i].morale.RecalcPermanentMorale(force_send: false, recalc_dist: true);
		}
		SendSubstate<ReligionState.PaganBeliefsState>(1);
		if (IsAuthority())
		{
			FireEvent("religion_changed", null);
		}
		return true;
	}

	public bool DelPaganBelief(string name)
	{
		if (!IsAuthority())
		{
			return false;
		}
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		Religion.PaganBelief paganBelief = game.religions.pagan.def.FindPaganBelief(name);
		if (paganBelief == null)
		{
			return false;
		}
		if (!pagan_beliefs.Remove(paganBelief))
		{
			return false;
		}
		religion.DelModifiers(this);
		religion.AddModifiers(this);
		for (int i = 0; i < armies.Count; i++)
		{
			armies[i].morale.RecalcPermanentMorale(force_send: false, recalc_dist: true);
		}
		SendSubstate<ReligionState.PaganBeliefsState>(1);
		if (IsAuthority())
		{
			FireEvent("religion_changed", null);
		}
		return true;
	}

	public void ChangeReligion(Religion religion)
	{
		Religion religion2 = this.religion;
		SetReligion(religion);
		religion.OnReligionChanged(this);
		RecalcBuildingStates();
		if (game?.religions?.catholic?.crusade?.leader?.GetKingdom() == this)
		{
			Crusade crusade = game.religions.catholic.crusade;
			crusade.end_reason = "religion_changed";
			crusade.End();
		}
		Vars vars = new Vars();
		vars.SetVar("kingdom_a", this);
		vars.SetVar("new_religion", religion.name);
		game.BroadcastRadioEvent("KingdomChangedReligionMessage", vars);
		OnChangedAnalytics("religion_changed", religion2.name, religion.name);
	}

	public bool IsPapacy()
	{
		return game.religions.catholic.hq_kingdom == this;
	}

	public bool HasPope()
	{
		Character head = game.religions.catholic.head;
		if (head == null)
		{
			return false;
		}
		if (IsPapacy())
		{
			return head.original_kingdom_id == id;
		}
		return head.GetSpecialCourtKingdomId() == id;
	}

	public bool HasEcumenicalPatriarch()
	{
		return game.religions.orthodox.head_kingdom == this;
	}

	public bool IsCaliphate()
	{
		if (caliphate)
		{
			return religion.def.muslim;
		}
		return false;
	}

	public bool IsLeadingJihad()
	{
		if (!IsCaliphate())
		{
			return false;
		}
		for (int i = 0; i < wars.Count; i++)
		{
			if (wars[i].IsJihad() && wars[i].IsLeader(this))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasJihadAgainst()
	{
		if (IsCaliphate())
		{
			return false;
		}
		for (int i = 0; i < wars.Count; i++)
		{
			if (wars[i].IsJihad() && wars[i].IsLeader(this))
			{
				return true;
			}
		}
		return false;
	}

	public float PrecRealmsSameReligon()
	{
		float num = 0f;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].religion == religion)
			{
				num += 1f;
			}
		}
		return 100f * num / (float)realms.Count;
	}

	public bool HasTradeCenter()
	{
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].IsTradeCenter())
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTradePort()
	{
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].castle.HasWorkingBuilding(game.defs.Get<Building.Def>("TradePort")))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAdmiralty()
	{
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].castle.HasWorkingBuilding(game.defs.Get<Building.Def>("Admiralty")))
			{
				return true;
			}
		}
		return false;
	}

	public float GetStat(StatName stat_name, bool must_exist = true)
	{
		if (stats == null)
		{
			if (must_exist && IsRegular())
			{
				Error(string.Concat("GetStat('", stat_name, "'): Stats not initialized yet!"));
			}
			return 0f;
		}
		return stats.Get(stat_name, must_exist);
	}

	public float GetStat(string stat_name, bool must_exist = true)
	{
		if (stats == null)
		{
			if (must_exist)
			{
				Error("GetStat('" + stat_name + "'): Stats not initialized yet!");
			}
			return 0f;
		}
		return stats.Get(stat_name, must_exist);
	}

	public bool IsInTruceWith(Kingdom k)
	{
		float num = RelationUtils.Def.truce_time * 60f;
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(this, k);
		if (kingdomAndKingdomRelation.peace_time == Time.Zero)
		{
			return false;
		}
		return game.time - kingdomAndKingdomRelation.peace_time < num;
	}

	public float GetRelationship(Kingdom k)
	{
		if (k == this)
		{
			return 1f;
		}
		return KingdomAndKingdomRelation.Get(this, k).GetRelationship();
	}

	public float GetRelationshipEx(Kingdom k, out float perm, out float temp)
	{
		perm = 0f;
		temp = 0f;
		if (k == this)
		{
			return 0f;
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(this, k);
		perm = kingdomAndKingdomRelation.perm_relationship;
		temp = kingdomAndKingdomRelation.temp_relationship;
		return kingdomAndKingdomRelation.GetRelationship();
	}

	public void AddRelationModifier(Kingdom k, string modifierName, IVars vars, float valueMultiplier = 1f)
	{
		if (k != null && k.type == Type.Regular && type == Type.Regular)
		{
			KingdomAndKingdomRelation.Modify(modifierName, this, k, vars, valueMultiplier);
		}
	}

	public void AddRelationship(Kingdom k, float perm, IVars vars, float valueMultiplier = 1f)
	{
		if (k != null && k.type == Type.Regular)
		{
			KingdomAndKingdomRelation.AddRelationship(this, k, perm * valueMultiplier);
		}
	}

	public void AddRelationModifier(Kingdom k, Value val, IVars vars, float valueMultiplier = 1f)
	{
		if (val.is_string)
		{
			AddRelationModifier(k, val.String(), vars, valueMultiplier);
		}
		else if (val.is_number)
		{
			AddRelationship(k, val.Float() * valueMultiplier, vars);
		}
	}

	public void AddRelationship(Religion rlg, float perm, IVars vars)
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated() && kingdom != this && kingdom.religion == rlg)
			{
				AddRelationship(kingdom, perm, vars);
			}
		}
	}

	private void SetStanceRefreshRebellions(Kingdom k)
	{
		for (int i = 0; i < potentialRebellions.Count; i++)
		{
			Rebellion rebellion = potentialRebellions[i];
			if (rebellion.IsLoyalist())
			{
				rebellion.RefreshZoneRealms();
			}
		}
		for (int j = 0; j < k.potentialRebellions.Count; j++)
		{
			Rebellion rebellion2 = k.potentialRebellions[j];
			if (rebellion2.IsLoyalist())
			{
				rebellion2.RefreshZoneRealms();
			}
		}
	}

	private void NotifyStanceChanged(Kingdom k)
	{
		string message = "stance_changed";
		if (!base.started || game.state == Game.State.LoadingMap)
		{
			message = "stance_changed_initial";
		}
		else
		{
			for (int i = 0; i < k.realms.Count; i++)
			{
				Realm realm = k.realms[i];
				realm.NotifyListeners(message, this);
				for (int j = 0; j < realm.settlements.Count; j++)
				{
					realm.settlements[j].NotifyListeners(message, this);
				}
			}
			if (k.occupiedKeeps != null)
			{
				for (int l = 0; l < k.occupiedKeeps.Count; l++)
				{
					k.occupiedKeeps[l].NotifyListeners(message, this);
				}
			}
			for (int m = 0; m < realms.Count; m++)
			{
				Realm realm2 = realms[m];
				realm2.NotifyListeners(message, k);
				for (int n = 0; n < realm2.settlements.Count; n++)
				{
					realm2.settlements[n].NotifyListeners(message, this);
				}
			}
			if (occupiedKeeps != null)
			{
				for (int num = 0; num < occupiedKeeps.Count; num++)
				{
					occupiedKeeps[num].NotifyListeners(message, this);
				}
			}
		}
		NotifyListeners(message, k);
		k.NotifyListeners(message);
		for (int num2 = 0; num2 < armies.Count; num2++)
		{
			armies[num2].morale.RecalcPermanentMorale(force_send: false, recalc_dist: true);
		}
		for (int num3 = 0; num3 < k.armies.Count; num3++)
		{
			k.armies[num3].morale.RecalcPermanentMorale(force_send: false, recalc_dist: true);
		}
	}

	private void OnUnsetStanceNonAgression(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsNonAgression() && prev_stance.IsNonAgression())
		{
			if (nonAgressions.Remove(k))
			{
				SendState<NonAgressionsState>();
			}
			if (k.nonAgressions.Remove(this))
			{
				k.SendState<NonAgressionsState>();
			}
		}
	}

	private void OnUnsetStanceAlliance(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsAlliance() && prev_stance.IsAlliance())
		{
			if (allies.Remove(k))
			{
				SendState<AlliesState>();
			}
			if (k.allies.Remove(this))
			{
				k.SendState<AlliesState>();
			}
		}
	}

	private void OnUnsetStanceTrade(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k == null || !stance.IsTrade() || !prev_stance.IsTrade())
		{
			return;
		}
		if (tradeAgreementsWith.Remove(k))
		{
			SendState<TradeAgreementsState>();
		}
		if (k.tradeAgreementsWith.Remove(this))
		{
			k.SendState<TradeAgreementsState>();
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsMerchant() && character.IsMerchant() && character.mission_kingdom == k)
			{
				character.Recall();
			}
		}
		for (int j = 0; j < k.court.Count; j++)
		{
			Character character2 = k.court[j];
			if (character2 != null && character2.IsMerchant() && character2.IsMerchant() && character2.mission_kingdom == this)
			{
				character2.Recall();
			}
		}
		bool flag = vars?.GetVar("isManual") == 1;
		if (flag)
		{
			KingdomAndKingdomRelation.Modify("rel_break_trade_manual", this, k, vars);
		}
		else
		{
			KingdomAndKingdomRelation.Modify("rel_break_trade", this, k, vars);
		}
		if (apply_consequences)
		{
			GetCrownAuthority().AddModifier("breakTrade");
		}
		CloseTradeRoute(k, flag);
		k.CloseTradeRoute(this, flag);
		NotifyListeners("break_trade", k);
		NotifyListeners("trade_broken", k);
		k.NotifyListeners("trade_broken", this);
	}

	private void OnUnsetStanceMarriage(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsMarriage() && prev_stance.IsMarriage())
		{
			marriages.RemoveAll((Marriage m) => m.kingdom_husband == k || m.kingdom_wife == k);
			SendState<MarriageStates>();
			k.marriages.RemoveAll((Marriage m) => m.kingdom_husband == this || m.kingdom_wife == this);
			k.SendState<MarriageStates>();
		}
	}

	private void UnsetStance(Kingdom k, KingdomAndKingdomRelation rel, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && (rel.stance & stance) != RelationUtils.Stance.None)
		{
			RelationUtils.Stance stance2 = rel.stance;
			KingdomAndKingdomRelation.SetStance(this, k, rel.stance & (RelationUtils.Stance.All ^ stance));
			OnUnsetStanceNonAgression(k, stance2, stance, vars, apply_consequences);
			OnUnsetStanceAlliance(k, stance2, stance, vars, apply_consequences);
			OnUnsetStanceTrade(k, stance2, stance, vars, apply_consequences);
			OnUnsetStanceMarriage(k, stance2, stance, vars, apply_consequences);
		}
	}

	public void UnsetStance(Kingdom k, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k == null)
		{
			return;
		}
		if (stance.IsWar() || stance.IsPeace() || stance.IsAlliance())
		{
			Game.Log(string.Concat("Trying to manually unset stance ", stance, " between ", Name, " and ", k.Name, ". You must use SetStance() to some of the other war stances!"), Game.LogType.Error);
			return;
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(this, k);
		if ((kingdomAndKingdomRelation.stance & stance) == 0)
		{
			return;
		}
		UnsetStance(k, kingdomAndKingdomRelation, stance, vars, apply_consequences);
		if (apply_consequences)
		{
			if (ai != null && ai.Enabled(KingdomAI.EnableFlags.Kingdom))
			{
				ai.ThinkKingdomPrisonerActions(k);
			}
			if (k.ai != null && k.ai.Enabled(KingdomAI.EnableFlags.Kingdom))
			{
				k.ai.ThinkKingdomPrisonerActions(this);
			}
		}
		SetStanceRefreshRebellions(k);
		NotifyStanceChanged(k);
	}

	private void OnSetStanceWar(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k == null || !stance.IsWar() || prev_stance.IsWar())
		{
			return;
		}
		RelationUtils.Stance stance2 = RelationUtils.Stance.None;
		if (prev_stance.IsPeace())
		{
			stance2 |= RelationUtils.Stance.Peace;
		}
		if (prev_stance.IsAlliance())
		{
			stance2 |= RelationUtils.Stance.Alliance;
		}
		if (prev_stance.IsNonAgression())
		{
			stance2 |= RelationUtils.Stance.NonAggression;
		}
		if (prev_stance.IsTrade())
		{
			stance2 |= RelationUtils.Stance.Trade;
		}
		if (prev_stance.IsMarriage())
		{
			stance2 |= RelationUtils.Stance.Marriage;
		}
		if (stance2 != RelationUtils.Stance.None)
		{
			UnsetStance(k, KingdomAndKingdomRelation.Get(this, k), stance2, vars, apply_consequences);
		}
		if (k.sovereignState == this)
		{
			DelVassalState(k);
		}
		if (sovereignState == k)
		{
			k.DelVassalState(this);
		}
		if (!apply_consequences)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < k.armies_in.Count; i++)
		{
			flag = IsOwnStance(k.armies_in[i]);
			if (flag)
			{
				break;
			}
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(this, k);
		bool flag2 = kingdomAndKingdomRelation.peace_time != Time.Zero && game.time - kingdomAndKingdomRelation.peace_time < RelationUtils.Def.truce_time * 60f;
		bool flag3 = kingdomAndKingdomRelation.GetRelationship() > RelationUtils.Def.GetLowerTreshold(RelationUtils.RelationshipType.Trusting);
		bool flag4 = k == game.religions.catholic.hq_kingdom;
		if (prev_stance.IsNonAgression())
		{
			if (apply_consequences)
			{
				GetCrownAuthority().AddModifier("breakNAP");
			}
			KingdomAndKingdomRelation.Modify("rel_break_nonaggression", this, k, vars);
		}
		if (prev_stance.IsMarriage())
		{
			KingdomAndKingdomRelation.Modify("rel_break_marriage", this, k, vars);
		}
		if (prev_stance.IsSovereign())
		{
			for (int j = 0; j < vassalStates.Count; j++)
			{
				KingdomAndKingdomRelation.Modify("rel_liege_declared_war_on_vassal", this, vassalStates[j], vars);
			}
		}
		KingdomAndKingdomRelation.Modify("rel_declare_war", this, k, vars);
		if (flag)
		{
			KingdomAndKingdomRelation.Modify("rel_ruthless_war_declaration", this, k, vars);
		}
		if (flag2)
		{
			KingdomAndKingdomRelation.Modify("rel_dishonored_truce", this, k, vars);
		}
		if (flag3)
		{
			KingdomAndKingdomRelation.Modify("rel_attacked_friend", this, k, vars);
		}
		for (int l = 0; l < game.kingdoms.Count; l++)
		{
			Kingdom kingdom = game.kingdoms[l];
			if (kingdom != null && !kingdom.IsDefeated() && kingdom != this && kingdom != k)
			{
				bool flag5 = neighbors.Contains(kingdom);
				if (flag && flag5)
				{
					KingdomAndKingdomRelation.Modify("rel_ruthless_war_declaration_neighbors", this, kingdom, vars);
				}
				if (flag2 && flag5)
				{
					KingdomAndKingdomRelation.Modify("rel_dishonored_truce_neighbors", this, kingdom, vars);
				}
				if (flag3 && GetRelationship(kingdom) > RelationUtils.Def.GetLowerTreshold(RelationUtils.RelationshipType.Trusting))
				{
					KingdomAndKingdomRelation.Modify("rel_attacked_friend_friends", this, kingdom, vars);
				}
				if (flag4 && kingdom.is_catholic && !kingdom.excommunicated)
				{
					KingdomAndKingdomRelation.Modify("rel_attacked_pope", this, kingdom, vars);
				}
			}
		}
	}

	private void OnSetStancePeace(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsPeace() && !prev_stance.IsPeace())
		{
			RelationUtils.Stance stance2 = RelationUtils.Stance.None;
			if (prev_stance.IsWar())
			{
				stance2 |= RelationUtils.Stance.War;
			}
			if (prev_stance.IsAlliance())
			{
				stance2 |= RelationUtils.Stance.Alliance;
			}
			if (stance2 != RelationUtils.Stance.None)
			{
				UnsetStance(k, KingdomAndKingdomRelation.Get(this, k), stance2, vars, apply_consequences);
			}
			if (apply_consequences && prev_stance.IsWar())
			{
				KingdomAndKingdomRelation.Modify("rel_sign_peace", this, k, vars);
			}
		}
	}

	private void OnSetStanceAlliance(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsAlliance() && !prev_stance.IsAlliance())
		{
			if (!allies.Contains(k))
			{
				allies.Add(k);
				SendState<AlliesState>();
			}
			if (!k.allies.Contains(this))
			{
				k.allies.Add(this);
				k.SendState<AlliesState>();
			}
			RelationUtils.Stance stance2 = RelationUtils.Stance.None;
			if (prev_stance.IsWar())
			{
				stance2 |= RelationUtils.Stance.War;
			}
			if (prev_stance.IsPeace())
			{
				stance2 |= RelationUtils.Stance.Peace;
			}
			if (stance2 != RelationUtils.Stance.None)
			{
				UnsetStance(k, KingdomAndKingdomRelation.Get(this, k), stance2, vars, apply_consequences);
			}
		}
	}

	private void OnSetStanceNonAgression(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsNonAgression() && !prev_stance.IsNonAgression())
		{
			if (!nonAgressions.Contains(k))
			{
				nonAgressions.Add(k);
				SendState<NonAgressionsState>();
			}
			if (!k.nonAgressions.Contains(this))
			{
				k.nonAgressions.Add(this);
				k.SendState<NonAgressionsState>();
			}
			if (apply_consequences)
			{
				KingdomAndKingdomRelation.Modify("rel_sign_nonagression", this, k, vars);
			}
		}
	}

	private void OnSetStanceTrade(Kingdom k, RelationUtils.Stance prev_stance, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k != null && stance.IsTrade() && !prev_stance.IsTrade())
		{
			if (!tradeAgreementsWith.Contains(k))
			{
				tradeAgreementsWith.Add(k);
				SendState<TradeAgreementsState>();
			}
			if (!k.tradeAgreementsWith.Contains(this))
			{
				k.tradeAgreementsWith.Add(this);
				k.SendState<TradeAgreementsState>();
			}
			if (apply_consequences)
			{
				KingdomAndKingdomRelation.Modify("rel_sign_trade", this, k, vars);
			}
		}
	}

	public void SetStance(Kingdom k, RelationUtils.Stance stance, IVars vars = null, bool apply_consequences = true)
	{
		if (k == null)
		{
			return;
		}
		KingdomAndKingdomRelation kingdomAndKingdomRelation = KingdomAndKingdomRelation.Get(this, k, calc_fade: false);
		if ((kingdomAndKingdomRelation.stance & stance) == stance)
		{
			return;
		}
		RelationUtils.Stance stance2 = KingdomAndKingdomRelation.GetStance(this, k);
		KingdomAndKingdomRelation.SetStance(this, k, kingdomAndKingdomRelation.stance | stance);
		OnSetStanceWar(k, stance2, stance, vars, apply_consequences);
		OnSetStancePeace(k, stance2, stance, vars, apply_consequences);
		OnSetStanceAlliance(k, stance2, stance, vars, apply_consequences);
		OnSetStanceNonAgression(k, stance2, stance, vars, apply_consequences);
		OnSetStanceTrade(k, stance2, stance, vars, apply_consequences);
		if (apply_consequences)
		{
			if (ai != null && ai.Enabled(KingdomAI.EnableFlags.Kingdom))
			{
				ai.ThinkKingdomPrisonerActions(k);
			}
			if (k.ai != null && k.ai.Enabled(KingdomAI.EnableFlags.Kingdom))
			{
				k.ai.ThinkKingdomPrisonerActions(this);
			}
		}
		SetStanceRefreshRebellions(k);
		NotifyStanceChanged(k);
	}

	public void UpdateTradeRouteTime(Kingdom k)
	{
		KingdomAndKingdomRelation.UpdateTradeRouteTime(this, k, game);
	}

	public Time GetTradeRouteLastUpdateTime(Kingdom k)
	{
		return KingdomAndKingdomRelation.GetTradeRouteLastUpdateTime(this, k);
	}

	public bool HasTradeAgreement(Kingdom k)
	{
		return KingdomAndKingdomRelation.HasTradeAgreement(this, k);
	}

	public List<Reason> GetRelatioReasonsFor(Kingdom k)
	{
		List<Reason> list = new List<Reason>();
		for (int i = 0; i < diplomacyReasons.Count; i++)
		{
			if (diplomacyReasons[i].IsAffectedBy(k))
			{
				list.Add(diplomacyReasons[i]);
			}
		}
		return list;
	}

	public void UpdateCoreRealms()
	{
		if (!IsAuthority())
		{
			return;
		}
		int count = coreRealmsRaw.Count;
		for (int i = 0; i < coreRealmsRaw.Count; i++)
		{
			coreRealmsRaw[i].UpdateIsCoreFor(this);
			if (count != coreRealmsRaw.Count)
			{
				i--;
				count = coreRealmsRaw.Count;
			}
		}
	}

	public void AddRelationReason(Reason r)
	{
		if (!AssertAuthority() || r == null || !is_player || r.source != this)
		{
			return;
		}
		bool flag = diplomacyReasons.Count < 1000;
		for (int i = 0; i < diplomacyReasons.Count; i++)
		{
			if (r.value > diplomacyReasons[i].value)
			{
				if (flag)
				{
					diplomacyReasons.Insert(i, r);
					game.multiplayer.SendObjEventToAllClients(this, new AddDiplomacyReasonEvent(r, i));
				}
				else
				{
					diplomacyReasons[i] = r;
					game.multiplayer.SendObjEventToAllClients(this, new AddDiplomacyReasonEvent(r, i, replace: true));
				}
				flag = false;
				break;
			}
		}
		if (flag)
		{
			diplomacyReasons.Add(r);
			game.multiplayer.SendObjEventToAllClients(this, new AddDiplomacyReasonEvent(r, diplomacyReasons.Count - 1));
		}
	}

	public void AddSupportPenalty(string penaltyName, float valueMultiplier = 1f)
	{
	}

	public bool GetRoyalMarriage(Kingdom k)
	{
		return KingdomAndKingdomRelation.GetMarriage(this, k);
	}

	public void ClearSupport(Kingdom k)
	{
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			Battle battle = army.battle;
			if (battle != null && (((battle.attacker_support?.GetKingdom().id ?? 0) == id && battle.attacker_kingdom.id == k.id) || ((battle.defender_support?.GetKingdom().id ?? 0) == id && battle.defender_kingdom.id == k.id)))
			{
				army.LeaveBattle();
			}
		}
	}

	public bool CheckVictoryCondition()
	{
		if (realms.Count >= game.landRealmsCount)
		{
			return true;
		}
		if (resources[ResourceType.Gold] >= (float)game.rules.GetVar("victory_condition_gold").int_val)
		{
			return true;
		}
		if (realms.Count >= game.rules.GetVar("victory_condition_realms").int_val)
		{
			return true;
		}
		return false;
	}

	public bool IsDefeated()
	{
		if (type != Type.Regular)
		{
			return true;
		}
		if (realms.Count == 0)
		{
			return true;
		}
		return false;
	}

	public bool IsThreat(Kingdom k)
	{
		if (k.IsDefeated())
		{
			return false;
		}
		RelationUtils.Stance stance = GetStance(k);
		if (stance.IsWar())
		{
			return false;
		}
		if (stance.IsNonAgression() || stance.IsAlliance())
		{
			return false;
		}
		if (k.sovereignState == this || sovereignState == k)
		{
			return false;
		}
		if ((float)DistanceToKingdom(k) > threats_max_distance)
		{
			return false;
		}
		if (GetRelationship(k) > threats_max_relationship)
		{
			return false;
		}
		return true;
	}

	public bool IsFriend(Kingdom k)
	{
		if (k.IsDefeated())
		{
			return false;
		}
		RelationUtils.Stance stance = GetStance(k);
		if (stance.IsWar())
		{
			return false;
		}
		if (stance.IsNonAgression() || stance.IsAlliance())
		{
			return false;
		}
		if (k.sovereignState == this || sovereignState == k)
		{
			return false;
		}
		if ((float)DistanceToKingdom(k) > friends_max_distance)
		{
			return false;
		}
		if (GetRelationship(k) < friends_min_relationship)
		{
			return false;
		}
		return true;
	}

	public override IRelationCheck GetStanceObj()
	{
		if (type == Type.Crusade)
		{
			Crusade crusade = game.religions.catholic.crusade;
			if (crusade != null)
			{
				return crusade;
			}
		}
		return this;
	}

	public override RelationUtils.Stance GetStance(Kingdom k)
	{
		if (k == null)
		{
			return RelationUtils.Stance.Peace;
		}
		if (k == this)
		{
			return RelationUtils.Stance.Own;
		}
		if (FactionUtils.CheckStance(this, k, out var stance))
		{
			return stance;
		}
		return KingdomAndKingdomRelation.GetStance(this, k);
	}

	public override RelationUtils.Stance GetStance(Crusade c)
	{
		return c.GetStance(this);
	}

	public override RelationUtils.Stance GetStance(Rebellion r)
	{
		return r.GetStance(this);
	}

	public override RelationUtils.Stance GetStance(Settlement s)
	{
		return s.GetStance(this);
	}

	private float CalcASTroopStrength()
	{
		float num = 0f;
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			float num2 = 0f;
			for (int j = 0; j < army.units.Count; j++)
			{
				Unit unit = armies[i].units[j];
				Resource cost = unit.def.cost;
				float num3 = ((cost != null) ? cost.Get(ResourceType.Gold) : 0f);
				float health = unit.health;
				float num4 = ((unit.def.field.key == "Militia") ? ts_lvl_peasant : 1f);
				num2 += num3 * health * num4;
			}
			num += num2;
		}
		for (int k = 0; k < realms.Count; k++)
		{
			Realm realm = realms[k];
			Castle castle = realm?.castle;
			if (castle != null && !realm.IsOccupied())
			{
				float num5 = 0f;
				for (int l = 0; l < castle.garrison.units.Count; l++)
				{
					Unit unit2 = castle.garrison.units[l];
					Resource cost2 = unit2.def.cost;
					float num6 = ((cost2 != null) ? cost2.Get(ResourceType.Gold) : 0f);
					float health2 = unit2.health;
					float num7 = ((unit2.def.field.key == "Militia") ? ts_lvl_peasant : 1f);
					num5 += num6 * health2 * num7;
				}
				num += num5 * ts_garrison_factor;
			}
		}
		return num;
	}

	private float CalcASMarshalsFactor()
	{
		float num = 0f;
		Character character = null;
		for (int i = 0; i < court.Count; i++)
		{
			if (court[i] != null && court[i].IsMarshal() && !court[i].IsPrisoner())
			{
				num += (float)court[i].GetClassLevel();
				character = court[i];
			}
		}
		if (character == null)
		{
			return 1f;
		}
		num /= (float)character.GetMaxClassLevel();
		return 1f + num;
	}

	private float CalcASManPower()
	{
		float num = 0f;
		for (int i = 0; i < realms.Count; i++)
		{
			Population population = realms[i]?.castle?.population;
			if (population != null)
			{
				population.Recalc();
				float num2 = population.Count(Population.Type.Worker);
				float num3 = population.Count(Population.Type.Rebel);
				num3 *= mp_rebels_weight;
				num += num2 + num3;
			}
		}
		return num * mp_population_value;
	}

	private float CalcASGoldFactor()
	{
		Game.BeginProfileSection("CalcASGoldFactor income");
		float num = income[ResourceType.Gold];
		Game.EndProfileSection("CalcASGoldFactor income");
		return resources[ResourceType.Gold] + num * mp_gold_income;
	}

	private float CalcASWarsFactor()
	{
		int count = wars.Count;
		return 1f / (1f + (float)Math.Log(1 + count));
	}

	public float CalcArmyStrength()
	{
		if (as_time_frame == (float)game.frame)
		{
			return armyStrength;
		}
		float num = CalcASTroopStrength();
		float num2 = CalcASMarshalsFactor();
		float val = CalcASManPower();
		float val2 = CalcASGoldFactor();
		armyStrength = 1500f + (num * num2 + Math.Min(val, val2));
		as_time_frame = game.frame;
		return armyStrength;
	}

	public bool AreAllArmiesFull()
	{
		for (int i = 0; i < armies.Count; i++)
		{
			Army army = armies[i];
			if (army.units.Count < army.MaxUnits() + 1)
			{
				return false;
			}
		}
		return true;
	}

	public void CalcNormalizedGoldIncome()
	{
		if (realms.Count <= 0)
		{
			return;
		}
		if (realms.Count == 1)
		{
			realms[0].normalizedGoldIncome = 1f;
			return;
		}
		float minGoldIncome = GetMinGoldIncome();
		float maxGoldIncome = GetMaxGoldIncome();
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i] != null)
			{
				realms[i].normalizedGoldIncome = (realms[i].income[ResourceType.Gold] - minGoldIncome) / (maxGoldIncome - minGoldIncome);
			}
		}
	}

	public List<Castle> SuggestCastlesToBuildIn()
	{
		List<Castle> list = new List<Castle>();
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.castle.governor != null && realm.castle.AvailableBuildingSlots() > 0 && realm.castle.ChooseBuildingToBuild(common_only: false).def != null)
			{
				list.Add(realm.castle);
			}
		}
		return list;
	}

	public float GetAverageWarScore()
	{
		float num = total_past_war_score;
		for (int i = 0; i < wars.Count; i++)
		{
			num += wars[i].GetScoreOf(this);
		}
		return num / (float)(total_past_wars + wars.Count);
	}

	public War StartWarWith(Kingdom k, War.InvolvementReason reason = War.InvolvementReason.InternalPurposes, string message_id = null, Kingdom provoker = null, bool apply_consequences = true)
	{
		if (!AssertAuthority())
		{
			return null;
		}
		if (!War.CanStart(this, k))
		{
			return null;
		}
		War war = War.Create(this, k, reason, apply_consequences);
		if (war == null)
		{
			return null;
		}
		if (message_id != null)
		{
			Vars vars = new Vars(war);
			if (provoker == null)
			{
				vars.Set("kingdom_a", this);
				vars.Set("kingdom_b", k);
			}
			else
			{
				vars.Set("kingdom_a", provoker);
				vars.Set("kingdom_b", this);
				vars.Set("kingdom_c", k);
			}
			game.BroadcastRadioEvent(message_id, vars);
			if (war.defenders.Count > 1)
			{
				vars.Set("kingdom_a", war.GetSupporters(1));
				vars.Set("kingdom_b", war.defender);
				vars.Set("kingdom_c", war.attacker);
				game.BroadcastRadioEvent("DefensivePactHonoredMessage", vars);
			}
			if (war.attackers.Count > 1)
			{
				vars.Set("kingdom_a", war.GetSupporters(0));
				vars.Set("kingdom_b", war.attacker);
				vars.Set("kingdom_c", war.defender);
				game.BroadcastRadioEvent("OffensivePactHonoredMessage", vars);
			}
		}
		return war;
	}

	public bool EndWarWith(Kingdom k, Kingdom victor, string reason = null, bool silent = false)
	{
		AssertAuthority();
		if (k == null)
		{
			return false;
		}
		if (!War.CanStop(this, k))
		{
			return false;
		}
		War war = FindWarWith(k);
		if (war == null)
		{
			return false;
		}
		war.ResetBattlesAfterWarList();
		if (war.IsLeader(this))
		{
			if (war.IsLeader(k))
			{
				war.Conclude(war.GetSide(victor), reason, silent, this);
				return true;
			}
			war.Leave(k, victor, silent, apply_consequences: true, reason);
			return true;
		}
		if (war.IsLeader(k))
		{
			war.Leave(this, victor, silent, apply_consequences: true, reason);
			return true;
		}
		return false;
	}

	public void AddWar(War war)
	{
		if (war == null)
		{
			return;
		}
		bool flag = wars.Count == 0;
		wars.Add(war);
		if (is_player)
		{
			Game.Log(string.Concat("War ", war, " added to ", this), Game.LogType.Message);
		}
		if (IsAuthority())
		{
			Stat stat = stats.Find(Stats.ks_war_exhaustion);
			bool flag2 = false;
			if (stat.all_mods != null)
			{
				for (int i = 0; i < stat.all_mods.Count; i++)
				{
					if (stat.all_mods[i].GetField().key == "WarExhaustionModifier")
					{
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2 && !Game.isLoadingSaveGame)
			{
				stat.AddModifier(new WarExhaustionModifier(game, game.defs.Get<WarExhaustionModifier.Def>("WarExhaustionModifier"), this, war));
			}
		}
		if (flag)
		{
			game.kingdoms_at_war++;
			NotifyListeners("peace_ended", war);
		}
	}

	public int PactLeadKingdomsCount()
	{
		int num = 0;
		for (int i = 0; i < pacts.Count; i++)
		{
			Pact pact = pacts[i];
			if (pact.leader != this)
			{
				continue;
			}
			for (int j = 0; j < pact.members.Count; j++)
			{
				if (pact.members[j] != this)
				{
					num++;
				}
			}
		}
		return num;
	}

	public int PactLeadKingdomsRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < pacts.Count; i++)
		{
			Pact pact = pacts[i];
			if (pact.leader != this)
			{
				continue;
			}
			for (int j = 0; j < pact.members.Count; j++)
			{
				if (pact.members[j] != this)
				{
					num += pact.members[j].realms.Count;
				}
			}
		}
		return num;
	}

	public int PactLeadKingdomsPopulationCount()
	{
		int num = 0;
		for (int i = 0; i < pacts.Count; i++)
		{
			Pact pact = pacts[i];
			if (pact.leader != this)
			{
				continue;
			}
			for (int j = 0; j < pact.members.Count; j++)
			{
				if (pact.members[j] != this)
				{
					num += pact.members[j].GetTotalPopulation();
				}
			}
		}
		return num;
	}

	public int WarsAsLeaderCount()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			if (wars[i].IsLeader(this))
			{
				num++;
			}
		}
		return num;
	}

	public int WarsAsSupporterCount()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			if (!wars[i].IsLeader(this))
			{
				num++;
			}
		}
		return num;
	}

	public List<Kingdom> EnemyKingdoms(List<Kingdom> lst = null)
	{
		if (lst == null)
		{
			tmp_kingdoms.Clear();
			lst = tmp_kingdoms;
		}
		for (int i = 0; i < wars.Count; i++)
		{
			List<Kingdom> enemies = wars[i].GetEnemies(this);
			if (enemies == null)
			{
				continue;
			}
			for (int j = 0; j < enemies.Count; j++)
			{
				Kingdom item = enemies[j];
				if (!lst.Contains(item))
				{
					lst.Add(item);
				}
			}
		}
		return lst;
	}

	public int EnemyKingdomsCount()
	{
		return EnemyKingdoms()?.Count ?? 0;
	}

	public int WarLeadMaxKingdoms()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			War war = wars[i];
			if (war.IsLeader(this))
			{
				num = Math.Max(war.GetAllies(this).Count - 1, num);
			}
		}
		return num;
	}

	public int WarLeadKingdomsCount()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			War war = wars[i];
			if (war.IsLeader(this))
			{
				num += war.GetAllies(this).Count - 1;
			}
		}
		return num;
	}

	public int WarLeadKingdomsRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			War war = wars[i];
			if (!war.IsLeader(this))
			{
				continue;
			}
			List<Kingdom> list = war.GetAllies(this);
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j] != this)
				{
					num += list[j].realms.Count;
				}
			}
		}
		return num;
	}

	public int WarLeadKingdomsPopulationCount()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			War war = wars[i];
			if (!war.IsLeader(this))
			{
				continue;
			}
			List<Kingdom> list = war.GetAllies(this);
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j] != this)
				{
					num += list[j].GetTotalPopulation();
				}
			}
		}
		return num;
	}

	public int EnemyLandsCount()
	{
		int num = 0;
		for (int i = 0; i < wars.Count; i++)
		{
			List<Kingdom> enemies = wars[i].GetEnemies(this);
			for (int j = 0; j < enemies.Count; j++)
			{
				num += enemies[j].realms.Count;
			}
		}
		return num;
	}

	public float TimeInPeace()
	{
		if (wars.Count > 0)
		{
			return 0f;
		}
		return game.time - last_peace_time;
	}

	public void RemoveWar(War war)
	{
		if (war == null)
		{
			return;
		}
		Stat stat = stats.Find("ks_war_exhaustion");
		if (stat?.all_mods != null)
		{
			for (int i = 0; i < stat.all_mods.Count; i++)
			{
				(stat.all_mods[i] as WarExhaustionModifier).Stop(stats);
			}
		}
		total_past_wars++;
		total_past_war_score += war.GetScoreOf(this);
		SendState<PastWarsState>();
		wars.Remove(war);
		if (wars.Count == 0)
		{
			game.kingdoms_at_war--;
			last_peace_time = game.time;
			SendState<PeaceTimeState>();
			NotifyListeners("peace_started");
		}
	}

	public War FindWarWith(Kingdom k)
	{
		if (k == null)
		{
			return null;
		}
		for (int i = 0; i < wars.Count; i++)
		{
			War war = wars[i];
			if (war.IsEnemy(k, this))
			{
				return war;
			}
		}
		return null;
	}

	public bool IsAllyInWar(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		for (int i = 0; i < wars.Count; i++)
		{
			if (wars[i].IsAlly(k, this))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsAllyOrTeammate(Kingdom obj)
	{
		if (IsAlly(obj))
		{
			return true;
		}
		if (IsTeammate(obj))
		{
			return true;
		}
		return false;
	}

	public bool IsTeammate(Kingdom obj)
	{
		Game.Team team = game.teams.Get(this);
		if (team != null)
		{
			return team == game.teams.Get(obj);
		}
		return false;
	}

	public List<Kingdom> GetAllSupportersAgainst(Kingdom k)
	{
		List<Kingdom> kingdoms = game.kingdoms;
		List<Kingdom> list = new List<Kingdom>();
		int num = 0;
		for (int i = 0; i < kingdoms.Count; i++)
		{
			Kingdom kingdom = kingdoms[i];
			if (kingdom == this || kingdom == k)
			{
				continue;
			}
			if (IsAlly(kingdom))
			{
				list.Add(kingdom);
			}
			else if (kingdom.FindWarWith(k) != null)
			{
				if (FindWarWith(kingdom) != null)
				{
					list.Insert(0, kingdom);
					num++;
				}
				else
				{
					list.Insert(num, kingdom);
				}
			}
		}
		return list;
	}

	private void AddCommonPacts(Pact pact)
	{
		for (int i = 0; i < pact.members.Count; i++)
		{
			Kingdom kingdom = pact.members[i];
			if (kingdom != this)
			{
				KingdomAndKingdomRelation.AddCommonPact(this, kingdom, pact);
			}
		}
	}

	public void AddPact(Pact pact)
	{
		pacts.Add(pact);
		AddCommonPacts(pact);
		NotifyListeners("add_pact", new Vars(pact));
	}

	private void DelCommonPacts(Pact pact)
	{
		for (int i = 0; i < pact.members.Count; i++)
		{
			Kingdom kingdom = pact.members[i];
			if (kingdom != this)
			{
				KingdomAndKingdomRelation.DelCommonPact(this, kingdom, pact);
			}
		}
	}

	public void DelPact(Pact pact)
	{
		pacts.Remove(pact);
		DelCommonPacts(pact);
		NotifyListeners("del_pact", new Vars(pact));
	}

	public void AddPactAgainst(Pact pact)
	{
		pacts_against.Add(pact);
		NotifyListeners("add_pact_against", pact);
	}

	public void DelPactAgainst(Pact pact)
	{
		pacts_against.Remove(pact);
		NotifyListeners("del_pact_against", pact);
	}

	public int NumPacts(Pact.Type type)
	{
		int num = 0;
		for (int i = 0; i < pacts.Count; i++)
		{
			if (pacts[i].type == type)
			{
				num++;
			}
		}
		return num;
	}

	public int GetPacts(Pact.Type type, List<Pact> lst)
	{
		int num = 0;
		for (int i = 0; i < pacts.Count; i++)
		{
			Pact pact = pacts[i];
			if (pact.type == type)
			{
				lst.Add(pact);
				num++;
			}
		}
		return num;
	}

	public int GetPactsAgainst(Pact.Type type, List<Pact> lst)
	{
		int num = 0;
		for (int i = 0; i < pacts_against.Count; i++)
		{
			Pact pact = pacts_against[i];
			if (pact.type == type && pact.IsVisibleBy(this))
			{
				lst.Add(pact);
				num++;
			}
		}
		return num;
	}

	public bool HasPactsWith(Kingdom k)
	{
		if (k == null || k == this)
		{
			return false;
		}
		for (int i = 0; i < pacts.Count; i++)
		{
			if (pacts[i].members.Contains(k))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPactsAgainst(Kingdom k)
	{
		if (k == null || k == this)
		{
			return false;
		}
		for (int i = 0; i < pacts.Count; i++)
		{
			if (pacts[i].target == k)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPactsWith(Kingdom k, Pact.Type type)
	{
		if (k == null || k == this)
		{
			return false;
		}
		for (int i = 0; i < pacts.Count; i++)
		{
			Pact pact = pacts[i];
			if (pact.type == type && pact.members.Contains(k))
			{
				return true;
			}
		}
		return false;
	}

	public int GetPositivePactsWith(Kingdom kingdom)
	{
		int num = 0;
		List<Pact> list = pacts;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].members.Contains(kingdom))
			{
				num++;
			}
		}
		return num;
	}

	public int GetNegativePactsWith(Kingdom kingdom)
	{
		int num = 0;
		List<Pact> list = kingdom.pacts;
		for (int i = 0; i < list.Count; i++)
		{
			Pact pact = list[i];
			if (pact.target == this && (pact.type != Pact.Type.Offensive || pact.revealed))
			{
				num++;
			}
		}
		return num;
	}

	public CasusBeli FindCasusBeli(Kingdom against)
	{
		if (casus_beli == null)
		{
			return null;
		}
		for (int i = 0; i < casus_beli.Count; i++)
		{
			CasusBeli casusBeli = casus_beli[i];
			if (casusBeli.against == against)
			{
				return casusBeli;
			}
		}
		return null;
	}

	public void AddCasusBeli(CasusBeli cb, bool send_state = true)
	{
		casus_beli.Add(cb);
	}

	public bool DelCasusBeli(Kingdom against, bool send_state = true)
	{
		if (casus_beli == null)
		{
			return false;
		}
		for (int i = 0; i < casus_beli.Count; i++)
		{
			if (casus_beli[i].against == against)
			{
				casus_beli.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public List<Kingdom> GetAllies()
	{
		return allies;
	}

	public Character GetFavoriteDiplomat()
	{
		return favoriteDiplomat;
	}

	private float GetMinGoldIncome()
	{
		if (this == null || realms == null || realms.Count <= 0)
		{
			return 0f;
		}
		float num = realms[0].income[ResourceType.Gold];
		for (int i = 1; i < realms.Count; i++)
		{
			float num2 = realms[i].income[ResourceType.Gold];
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	private void AddEarnedOrSpent(ResourceType rt, float amount, KingdomAI.Expense.Category category)
	{
		if (!in_AI_spend && ai != null && ai.Enabled(KingdomAI.EnableFlags.All, checkAuthority: false) && amount < 0f && rt == ResourceType.Gold && category == KingdomAI.Expense.Category.Economy)
		{
			Warning($"Unaccounted expense: {amount} {rt} for {category}.");
		}
		if (category == KingdomAI.Expense.Category.None)
		{
			Warning($"Unnaccounted resource change: {amount} {rt}");
		}
		if (amount > 0f)
		{
			total_earned.Add(rt, amount);
			if (earned_by_category[(int)category] == null)
			{
				earned_by_category[(int)category] = new Resource();
			}
			earned_by_category[(int)category].Add(rt, amount);
		}
		else if (amount < 0f)
		{
			total_spent.Add(rt, 0f - amount);
			if (spent_by_category[(int)category] == null)
			{
				spent_by_category[(int)category] = new Resource();
			}
			spent_by_category[(int)category].Add(rt, 0f - amount);
		}
	}

	private void AddEarnedOrSpent(Resource res, float mul, KingdomAI.Expense.Category category)
	{
		if (!(res == null))
		{
			AddEarnedOrSpent(ResourceType.Gold, res[ResourceType.Gold] * mul, category);
			AddEarnedOrSpent(ResourceType.Books, res[ResourceType.Books] * mul, category);
			AddEarnedOrSpent(ResourceType.Piety, res[ResourceType.Piety] * mul, category);
			AddEarnedOrSpent(ResourceType.Levy, res[ResourceType.Levy] * mul, category);
		}
	}

	public void AddResources(KingdomAI.Expense.Category category, Resource res, float mul = 1f, bool send_state = true)
	{
		resources.Add(res, mul, this);
		AddEarnedOrSpent(res, mul, category);
		NotifyListeners("resources_changed");
		if (send_state)
		{
			SendState<ResourcesState>();
		}
	}

	public void AddResources(KingdomAI.Expense.Category category, ResourceType type, float amount, bool send_state = true)
	{
		resources.Add(type, amount, this);
		AddEarnedOrSpent(type, amount, category);
		NotifyListeners("resources_changed");
		if (send_state)
		{
			SendState<ResourcesState>();
		}
	}

	public void SubResources(KingdomAI.Expense.Category category, Resource res, bool send_state = true)
	{
		if (!(res == null))
		{
			resources.Sub(res, 1f, this);
			AddEarnedOrSpent(res, -1f, category);
			NotifyListeners("resources_changed");
			if (send_state)
			{
				SendState<ResourcesState>();
			}
		}
	}

	public void SubResources(KingdomAI.Expense.Category category, ResourceType type, float amount, bool send_state = true)
	{
		resources.Sub(type, amount, this);
		AddEarnedOrSpent(type, 0f - amount, category);
		NotifyListeners("resources_changed");
		if (send_state)
		{
			SendState<ResourcesState>();
		}
	}

	public void SetResources(Resource res, bool send_state = true)
	{
		resources.Set(res, 1f, this);
		NotifyListeners("resources_changed");
		if (send_state)
		{
			SendState<ResourcesState>();
		}
	}

	public void SetResources(ResourceType type, float amount, bool send_state = true)
	{
		if (type == ResourceType.Gold)
		{
			Log("Setting gold directly!");
		}
		resources.Set(type, amount, this);
		NotifyListeners("resources_changed");
		if (send_state)
		{
			SendState<ResourcesState>();
		}
	}

	public float GetMaxTreasury()
	{
		return GetMaxGoldCapacity();
	}

	public float GetMaxGoldCapacity(float gold_income)
	{
		float num = treasury_base;
		if (gold_income > 0f)
		{
			float num2 = treasury_mod1 / (gold_income / treasury_mod2 + treasury_mod3);
			if (num2 < treasury_inner_min)
			{
				num2 = treasury_inner_min;
			}
			if (num2 > treasury_inner_max)
			{
				num2 = treasury_inner_max;
			}
			num += num2 * gold_income;
		}
		if (num < treasury_min)
		{
			num = treasury_min;
		}
		if (num > treasury_max)
		{
			num = treasury_max;
		}
		return num;
	}

	public float GetMaxGoldCapacity()
	{
		return GetMaxGoldCapacity(_income[ResourceType.Gold]);
	}

	public float GetTrasuryRatioWith(Kingdom k)
	{
		if (k == null)
		{
			return 0f;
		}
		float maxTreasury = GetMaxTreasury();
		float maxTreasury2 = k.GetMaxTreasury();
		return maxTreasury / (maxTreasury + maxTreasury2);
	}

	public int DiplomaticGoldLevels()
	{
		if (diplomatic_gold_perc == null)
		{
			return 0;
		}
		return diplomatic_gold_perc.Length;
	}

	public float GetDiplomaticGoldAmount(int level, float max_treasury = -1f)
	{
		return GetHypotheticalDiplomaticGoldAmount(level, _income[ResourceType.Gold], max_treasury);
	}

	public float GetHypotheticalDiplomaticGoldAmount(int level, float gold_income, float max_treasury)
	{
		int num = DiplomaticGoldLevels();
		if (num <= 0)
		{
			return 0f;
		}
		if (level < 1)
		{
			level = 1;
		}
		else if (level > num)
		{
			level = num;
		}
		float num2 = diplomatic_gold_perc[level - 1];
		if (max_treasury < 0f)
		{
			max_treasury = GetMaxGoldCapacity(gold_income);
		}
		float num3 = max_treasury * num2 / 100f;
		float num4 = 100f;
		float num5 = num3 % num4;
		if (num5 != 0f)
		{
			num3 += num4 - num5;
		}
		return num3;
	}

	public float GetSSum(float level, float round = 100f)
	{
		int num = DiplomaticGoldLevels();
		level = Game.clamp(level, 0f, num);
		int num2 = (int)Math.Floor(level);
		float num3 = level - (float)num2;
		if (num3 == 0f)
		{
			if (num2 == 0)
			{
				return 0f;
			}
			return GetDiplomaticGoldAmount(num2);
		}
		int num4 = num2 + 1;
		if (num4 > num)
		{
			return GetDiplomaticGoldAmount(num2);
		}
		float num5 = 0f;
		float num6 = 0f;
		float maxTreasury = GetMaxTreasury();
		if (num2 > 0)
		{
			num5 = maxTreasury * diplomatic_gold_perc[num2 - 1] / 100f;
		}
		num6 = maxTreasury * diplomatic_gold_perc[num4 - 1] / 100f;
		float num7 = num5 + (num6 - num5) * num3;
		if (round > 0f)
		{
			float num8 = num7 % round;
			if (num8 != 0f)
			{
				num7 += round - num8;
			}
		}
		return num7;
	}

	public float MaxSSumsBlend(Kingdom k, float ssum_level, float bias, float round = 100f)
	{
		if (k == null || ssum_level < 0f || ssum_level > (float)DiplomaticGoldLevels())
		{
			return 0f;
		}
		float sSum = GetSSum(ssum_level, 1f);
		float sSum2 = k.GetSSum(ssum_level, 1f);
		float num = sSum + (sSum2 - sSum) * bias;
		if (num < sSum)
		{
			num = sSum;
		}
		if (round > 0f)
		{
			float num2 = num % round;
			if (num2 != 0f)
			{
				num += round - num2;
			}
		}
		return num;
	}

	private float GetMaxGoldIncome()
	{
		if (this == null || realms == null || realms.Count <= 0)
		{
			return 0f;
		}
		float num = realms[0].income[ResourceType.Gold];
		for (int i = 1; i < realms.Count; i++)
		{
			float num2 = realms[i].income[ResourceType.Gold];
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public float CalcInflationPenalty()
	{
		float num = _income[ResourceType.Gold];
		return CalcInflationPenalty(num, resources[ResourceType.Gold]);
	}

	public float CalcInflationPerc()
	{
		return inflation / income[ResourceType.Gold] * 100f;
	}

	public float CalcInflationPenalty(float income, float cur_gold)
	{
		float num = 1f - GetStat(Stats.ks_inflation_reduction_perc) / 100f;
		float num2 = GetMaxTreasury() / num;
		if (num2 <= 0f)
		{
			return 0f;
		}
		float num3 = treasury_base + num2 * treasury_no_penalty;
		if (num3 >= num2)
		{
			return 0f;
		}
		if (cur_gold <= num3)
		{
			return 0f;
		}
		if (cur_gold > num2)
		{
			return (income + (cur_gold - num2) * inflation_above_max_treasury_perc * 0.01f) * num;
		}
		float num4 = (cur_gold - num3) / (num2 - num3);
		num4 *= num4;
		num4 = ((!(income < cur_gold * inflation_min_gold_perc)) ? (num4 * income) : (num4 * (cur_gold * inflation_min_gold_perc)));
		return num4 * num;
	}

	public void RecalcIncomeNow()
	{
		RecalcIncomes(forced: true);
	}

	public Incomes RecalcIncomesNow()
	{
		if (game == null || game.IsUnloadingMap())
		{
			return incomes;
		}
		using (new Stat.ForceCached("RecalcIncomesNow"))
		{
			ClearIncomeVars();
			CalcArmiesUpkeep();
			for (int i = 0; i < realms.Count; i++)
			{
				realms[i].OnCalcIncomes();
			}
			incomes?.Calc();
			incomes?.ToResource(_income);
			for (int j = 0; j < realms.Count; j++)
			{
				realms[j].OnIncomesCalculated();
			}
			upkeeps?.Calc();
			upkeeps?.ToResource(_expenses);
			for (int k = 0; k < realms.Count; k++)
			{
				realms[k].OnUpkeepsCalculated();
			}
		}
		return incomes;
	}

	public void InvalidateIncomes(bool force_full_recalc = true)
	{
		if (game != null && !game.IsUnloadingMap())
		{
			income_valid = false;
			incomes?.Invalidate(force_full_recalc);
			upkeeps?.Invalidate(force_full_recalc);
		}
	}

	public void RecalcIncomes(bool forced = false)
	{
		if (game?.economy?.def != null && !game.IsUnloadingMap() && (!income_valid || forced))
		{
			income_valid = true;
			RecalcIncomesNow();
			ApplyIncomeModifiers();
			income_valid = true;
			NotifyListeners("income_changed");
		}
	}

	public void ApplyIncomeModifiers()
	{
		if (_income[ResourceType.Piety] < 0f && _income[ResourceType.Piety] > -1f)
		{
			_income[ResourceType.Piety] = -1f;
		}
		if (_income[ResourceType.Piety] > 0f && _income[ResourceType.Piety] < 1f)
		{
			_income.Set(ResourceType.Piety, 1f);
		}
		for (ResourceType resourceType = ResourceType.None; resourceType < ResourceType.COUNT; resourceType++)
		{
			float num = game.economy.def.IncomeMultiplier(resourceType);
			_income[resourceType] *= num;
			num = game.economy.def.ExpenseMultiplier(resourceType);
			_expenses[resourceType] *= num;
		}
		float devSettingsFloat = game.GetDevSettingsFloat("gold_income_dev_mod", 1f);
		float devSettingsFloat2 = game.GetDevSettingsFloat("books_income_dev_mod", 1f);
		float devSettingsFloat3 = game.GetDevSettingsFloat("piety_income_dev_mod", 1f);
		float devSettingsFloat4 = game.GetDevSettingsFloat("gold_expenses_dev_mod", 1f);
		float devSettingsFloat5 = game.GetDevSettingsFloat("books_expenses_dev_mod", 1f);
		float devSettingsFloat6 = game.GetDevSettingsFloat("piety_expenses_dev_mod", 1f);
		_income[ResourceType.Gold] *= devSettingsFloat;
		_income[ResourceType.Books] *= devSettingsFloat2;
		_income[ResourceType.Piety] *= devSettingsFloat3;
		_expenses[ResourceType.Gold] *= devSettingsFloat4;
		_expenses[ResourceType.Books] *= devSettingsFloat5;
		_expenses[ResourceType.Piety] *= devSettingsFloat6;
		if (!is_player || ai.Enabled(KingdomAI.EnableFlags.All))
		{
			float num2 = game.GetAIResourcesBoost("gold") * balance_factor_income;
			float num3 = game.GetAIResourcesBoost("books") * balance_factor_income;
			float num4 = game.GetAIResourcesBoost("piety") * balance_factor_income;
			float num5 = game.GetAIResourcesBoost("food") * balance_factor_income;
			float num6 = game.GetAIResourcesBoost("levy") * balance_factor_income;
			_income[ResourceType.Gold] *= num2;
			_income[ResourceType.Books] *= num3;
			_income[ResourceType.Piety] *= num4;
			_income[ResourceType.Food] *= num5;
			_income[ResourceType.Levy] *= num6;
		}
	}

	public void ClearIncomeVars()
	{
		_income.Clear();
		_expenses.Clear();
		inflation = 0f;
		taxForSovereignGold = 0f;
		taxForSovereignBooks = 0f;
		taxForSovereignPiety = 0f;
		goldFromMerchants = 0f;
		goldFromRoyalMerchants = 0f;
		goldFromForeignMerchants = 0f;
		goldFromGoods = 0f;
		goldFromVassals = 0f;
		booksFromVassals = 0f;
		pietyFromVassals = 0f;
		untaxGoldFromTradeCenters = 0f;
		goldFromFoodExport = 0f;
		goldFromExcessBooks = 0f;
		goldFromExcessPiety = 0f;
		goldFromExcessLevy = 0f;
		goldFromExcessResources = 0f;
		percGoldFromJizya = 0f;
		taxGoldFromJizya = 0f;
		percCorruption = 0f;
		allocatedCommerceForTraders = 0f;
		allocatedCommerceForImportGoods = 0f;
		allocatedCommerceForImportFood = 0f;
		allocatedCommerceForExportFood = 0f;
		allocatedCommerceForBuildings = 0f;
		faithFromClerics = 0f;
		foodFromImport = 0f;
		upkeepFoodFromExport = 0f;
		wageGoldTotal = 0f;
		wageGoldForMarshals = 0f;
		wageGoldForDiplomats = 0f;
		wageGoldForSpies = 0f;
		wageGoldForClerics = 0f;
		upkeepBribes = 0f;
		upkeepOccupations = 0f;
		upkeepDisorder = 0f;
		upkeepHelpTheWeak = 0f;
		upkeepGoldFromGoodsImport = 0f;
		upkeepGoldFromFoodImport = 0f;
	}

	private void AddIncome(ResourceType rt, Resource income, Resource expenses, int big_tick, bool allow_negative = false, float mod = 1f)
	{
		float num = income[rt] - expenses[rt];
		if (apply_income_tick % big_tick != 0)
		{
			if (num < -1f)
			{
				num = 0f;
			}
			else if (num > 0f || (!is_player && (rt == ResourceType.Books || rt == ResourceType.Piety)))
			{
				num = mod;
			}
		}
		float num2 = resources[rt];
		if (!allow_negative && num2 + num < 0f)
		{
			num = 0f - num2;
		}
		if (num != 0f)
		{
			in_AI_spend = true;
			KingdomAI.Expense.Category category = ((num >= 0f) ? KingdomAI.Expense.Category.Economy : KingdomAI.Expense.Category.Other);
			AddResources(category, rt, num, send_state: false);
			in_AI_spend = false;
		}
	}

	public void ApplyIncome(bool send_state = true)
	{
		if (!IsAuthority())
		{
			return;
		}
		if (IsDefeated())
		{
			Game.Log("ApplyIncome called for defeated kingdom: " + Name, Game.LogType.Warning);
		}
		apply_income_tick++;
		RecalcIncomeNow();
		AddIncome(ResourceType.Gold, income, expenses, big_tick_gold);
		float num = resources[ResourceType.Books];
		AddIncome(ResourceType.Books, income, expenses, big_tick_books);
		AddIncome(ResourceType.Piety, income, expenses, big_tick_books);
		AddIncome(ResourceType.Levy, income, expenses, big_tick_levy);
		SetResources(ResourceType.Hammers, income[ResourceType.Hammers], send_state: false);
		SetResources(ResourceType.Workers, income[ResourceType.Workers], send_state: false);
		SetResources(ResourceType.WorkerSlots, income[ResourceType.WorkerSlots], send_state: false);
		float stat = GetStat(Stats.ks_max_levy);
		if (stat > 0f && resources[ResourceType.Levy] > stat)
		{
			SetResources(ResourceType.Levy, stat, send_state: false);
		}
		float stat2 = GetStat(Stats.ks_max_books);
		if (stat2 > 0f && resources[ResourceType.Books] > stat2)
		{
			SetResources(ResourceType.Books, stat2, send_state: false);
			if (num != stat2 && is_player)
			{
				NotifyListeners("max_books_reached");
			}
		}
		float stat3 = GetStat(Stats.ks_max_piety);
		if (stat3 > 0f && resources[ResourceType.Piety] > stat3)
		{
			SetResources(ResourceType.Piety, stat3, send_state: false);
		}
		SetResources(ResourceType.Food, income[ResourceType.Food] - expenses[ResourceType.Food], send_state: false);
		SetResources(ResourceType.Trade, GetAvailableCommerce(), send_state: false);
		for (int i = 0; i < realms.Count; i++)
		{
			realms[i].OnApplyIncome();
		}
		RefreshBankrupcy();
		if (send_state)
		{
			SendState<ResourcesState>();
		}
		if (!game.ValidateEndGame(this))
		{
			NotifyListeners("income_tick");
		}
	}

	public Resource GetRealmIncome()
	{
		Resource resource = new Resource();
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			resource.Add(realm.income, 1f);
		}
		return resource;
	}

	public float GetTotalTradeCentreInfluence()
	{
		float num = 0f;
		foreach (Realm realm in realms)
		{
			if (realm.IsTradeCenter())
			{
				num += realm.tradeCenter.GetTotalInfluence();
			}
		}
		return num;
	}

	public float GetTownTribute()
	{
		return GetRealmIncome().Get(ResourceType.Gold);
	}

	public float GetVassalGold()
	{
		return goldFromVassals;
	}

	public float GetVassalBooks()
	{
		return booksFromVassals;
	}

	public float GetVassalPiety()
	{
		return pietyFromVassals;
	}

	public void CalcVassalGold()
	{
		goldFromVassals = 0f;
		if (vassalStates.Count == 0)
		{
			return;
		}
		for (int i = 0; i < vassalStates.Count; i++)
		{
			vassalStates[i].RecalcIncomes();
			float num = vassalStates[i].taxForSovereignGold;
			if (num < 0f)
			{
				num = 0f;
			}
			goldFromVassals += num;
		}
	}

	public void CalcVassalBooks()
	{
		booksFromVassals = 0f;
		if (vassalStates.Count == 0)
		{
			return;
		}
		for (int i = 0; i < vassalStates.Count; i++)
		{
			vassalStates[i].RecalcIncomes();
			float num = vassalStates[i].taxForSovereignBooks;
			if (num < 0f)
			{
				num = 0f;
			}
			booksFromVassals += num;
		}
	}

	public void CalcVassalPiety()
	{
		pietyFromVassals = 0f;
		if (vassalStates.Count == 0)
		{
			return;
		}
		for (int i = 0; i < vassalStates.Count; i++)
		{
			vassalStates[i].RecalcIncomes();
			float num = vassalStates[i].taxForSovereignPiety;
			if (num < 0f)
			{
				num = 0f;
			}
			pietyFromVassals += num;
		}
	}

	public void CalcImportAndExportGold()
	{
		upkeepGoldFromGoodsImport = 0f;
		upkeepGoldFromFoodImport = 0f;
		goldFromFoodExport = 0f;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsMerchant())
			{
				float num = character.CalcImportedGoodsGoldUpkeep();
				upkeepGoldFromGoodsImport += num;
				float num2 = character.CalcImportedFoodGoldUpkeep();
				upkeepGoldFromFoodImport += num2;
				float num3 = character.CalcExportedFoodGoldGain();
				goldFromFoodExport += num3;
			}
		}
	}

	public float CalcGoldFromExcessResource(ResourceType rt, StatName max_stat)
	{
		float num = resources[rt];
		float stat = GetStat(max_stat);
		if (num < stat)
		{
			return 0f;
		}
		float untaxed_value = incomes.per_resource[(int)rt].value.untaxed_value;
		if (untaxed_value <= 0f)
		{
			return 0f;
		}
		if (game?.economy?.def?.gold_from_excess_resources_perc == null)
		{
			return 0f;
		}
		float num2 = game.economy.def.gold_from_excess_resources_perc[(int)rt];
		if (num2 <= 0f)
		{
			return 0f;
		}
		float num3 = untaxed_value * num2 * 0.01f;
		num3 = (float)Math.Ceiling(num3);
		float num4 = game.economy.def.gold_from_excess_resources_cap[(int)rt];
		if (num4 > 0f && num3 > num4)
		{
			num3 = num4;
		}
		return num3;
	}

	public void CalculateGoldFromExcessResources()
	{
		goldFromExcessBooks = CalcGoldFromExcessResource(ResourceType.Books, Stats.ks_max_books);
		goldFromExcessPiety = CalcGoldFromExcessResource(ResourceType.Piety, Stats.ks_max_piety);
		goldFromExcessLevy = CalcGoldFromExcessResource(ResourceType.Levy, Stats.ks_max_levy);
		goldFromExcessResources = goldFromExcessBooks + goldFromExcessPiety + goldFromExcessLevy;
	}

	public void CalcWages()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (court == null || wage_thresholds == null || wage_thresholds.Length <= court.Count)
		{
			wageGoldForMarshals = (wageGoldForDiplomats = (wageGoldForSpies = (wageGoldForClerics = (wageGoldTotal = 0f))));
			return;
		}
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && (royalFamily == null || !royalFamily.IsFamilyMember(character)) && !character.IsKing() && character.prison_kingdom == null)
			{
				if (character.IsMarshal() && character.IsAlive())
				{
					num4++;
				}
				if (character.IsDiplomat() && character.IsAlive())
				{
					num++;
				}
				if (character.IsSpy() && character.IsAlive())
				{
					num2++;
				}
				if (character.IsCleric() && character.IsAlive() && (!IsPapacy() || !character.IsCardinal()))
				{
					num3++;
				}
			}
		}
		float num5 = 1f - GetStat(Stats.ks_knight_wage_discount_perc) / 100f;
		wageGoldForMarshals = DT.Round(wage_thresholds[num4] * num5);
		wageGoldForDiplomats = DT.Round(wage_thresholds[num] * num5);
		wageGoldForSpies = DT.Round(wage_thresholds[num2] * num5);
		wageGoldForClerics = DT.Round(wage_thresholds[num3] * num5);
		wageGoldTotal = DT.Round(wageGoldForMarshals + wageGoldForDiplomats + wageGoldForSpies + wageGoldForClerics);
	}

	public float GetWageGoldAtCount(int count)
	{
		if (wage_thresholds == null)
		{
			return 0f;
		}
		if (wage_thresholds.Length <= count)
		{
			return 0f;
		}
		float num = 1f - GetStat(Stats.ks_knight_wage_discount_perc) / 100f;
		return DT.Round(wage_thresholds[count] * num);
	}

	public int GetKnightsOnWageCount(string className)
	{
		int num = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && (royalFamily == null || !royalFamily.IsFamilyMember(character)) && !character.IsKing() && character.prison_kingdom == null)
			{
				if (className == "Marshal" && character.IsMarshal() && character.IsAlive())
				{
					num++;
				}
				if (className == "Diplomat" && character.IsDiplomat() && character.IsAlive())
				{
					num++;
				}
				if (className == "Cleric" && character.IsCleric() && character.IsAlive())
				{
					num++;
				}
				if (className == "Spy" && character.IsSpy() && character.IsAlive())
				{
					num++;
				}
			}
		}
		return num;
	}

	public float GetKnightsWage(string class_name)
	{
		return class_name switch
		{
			"Marshal" => wageGoldForMarshals, 
			"Diplomat" => wageGoldForDiplomats, 
			"Cleric" => wageGoldForClerics, 
			"Spy" => wageGoldForSpies, 
			_ => 0f, 
		};
	}

	public void CalcSovereignTaxGold()
	{
		if (sovereignState == null)
		{
			taxForSovereignGold = 0f;
		}
		else
		{
			taxForSovereignGold = (float)Math.Max(0.0, Math.Round(_income[ResourceType.Gold] * GetStat(Stats.ks_sovereign_tax_perc) * sovereignState.GetStat(Stats.ks_vassal_tax_perc) / 10000f, 1));
		}
	}

	public void CalcSovereignTaxBooks()
	{
		if (sovereignState == null)
		{
			taxForSovereignBooks = 0f;
		}
		else
		{
			taxForSovereignBooks = (float)Math.Max(0.0, Math.Round(_income[ResourceType.Books] * GetStat(Stats.ks_vassal_tax_books_perc) / 100f, 1));
		}
	}

	public void CalcSovereignTaxPiety()
	{
		if (sovereignState == null)
		{
			taxForSovereignPiety = 0f;
		}
		else
		{
			taxForSovereignPiety = (float)Math.Max(0.0, Math.Round(_income[ResourceType.Piety] * GetStat(Stats.ks_vassal_tax_piety_perc) / 100f, 1));
		}
	}

	public float GetIncomeFromMerchants()
	{
		return goldFromMerchants;
	}

	public void CalculateGoldFromPassiveTrade()
	{
		goldFromPassiveTrade = 0f;
		for (int i = 0; i < tradeAgreementsWith.Count; i++)
		{
			Kingdom kingdom = tradeAgreementsWith[i];
			if (GetMerchantIn(kingdom) == null)
			{
				goldFromPassiveTrade += game.economy.CalcTradeProfit(this, kingdom, null, 0);
			}
		}
	}

	public void CalculateGoldFromMerchants()
	{
		goldFromMerchants = 0f;
		goldFromRoyalMerchants = 0f;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character == null)
			{
				continue;
			}
			float num = character.GetTradeProfit();
			if (character.statuses != null)
			{
				for (int j = 0; j < character.statuses.Count; j++)
				{
					if (character.statuses[j] is TradeExpeditionStatus tradeExpeditionStatus)
					{
						float profit = tradeExpeditionStatus.GetProfit();
						num += profit;
					}
				}
			}
			if (character.IsKingOrPrince())
			{
				goldFromRoyalMerchants += num;
			}
			else
			{
				goldFromMerchants += num;
			}
		}
	}

	public void CalculateGoldFromGoods()
	{
		UpdateRealmTags();
		goldFromGoods = GetStat(Stats.ks_gold_per_good) * (float)(goods_produced.Count + goods_imported.Count);
		goldFromGoods += GetStat(Stats.ks_gold_from_goods);
	}

	public void CalculateGoldFromImportantRelatives()
	{
		goldFromImportantRelatives = 0f;
		if (royalFamily?.Relatives == null)
		{
			return;
		}
		for (int i = 0; i < royalFamily.Relatives.Count; i++)
		{
			Character character = royalFamily.Relatives[i];
			if (character != null)
			{
				goldFromImportantRelatives += character.GetStat(Stats.cs_important_relative_merchant_gold_bonus);
			}
		}
	}

	public int GetCoastalRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].HasTag("Coastal"))
			{
				num++;
			}
		}
		return num;
	}

	public void CalculateGoldFromForeignMerchants()
	{
		goldFromForeignMerchants = 0f;
		if (foreigners != null)
		{
			for (int i = 0; i < foreigners.Count; i++)
			{
				float foreignTradeProfit = foreigners[i].GetForeignTradeProfit();
				goldFromForeignMerchants += foreignTradeProfit;
			}
		}
	}

	public float SumCourtLevels()
	{
		float num = 0f;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null)
			{
				num += (float)character.GetClassLevel();
			}
		}
		return num;
	}

	public float SumPrincesLevels()
	{
		float num = 0f;
		for (int i = 0; i < royalFamily.Children.Count; i++)
		{
			Character character = royalFamily.Children[i];
			if (character != null && character.IsPrince())
			{
				num += (float)character.GetClassLevel();
			}
		}
		return num;
	}

	public int DisorderRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].IsDisorder())
			{
				num++;
			}
		}
		return num;
	}

	public int OccupiedRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].IsOccupied())
			{
				num++;
			}
		}
		return num;
	}

	public int SettlementsCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			num += realms[i].GetSettlementCount("All", "realm");
		}
		return num;
	}

	public int GetNumBuildings()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm?.castle?.buildings != null)
			{
				num += realm.castle.buildings.Count;
			}
		}
		return num;
	}

	public Resource GetBuildingsUpkeep()
	{
		if (upkeepBuildings == 0f && allocatedCommerceForBuildings == 0f)
		{
			return null;
		}
		tmp_buildings_upkeep.Set(ResourceType.Gold, upkeepBuildings);
		tmp_buildings_upkeep.Set(ResourceType.Trade, allocatedCommerceForBuildings);
		return tmp_buildings_upkeep;
	}

	public float CalcAllocattedCommerce()
	{
		allocatedCommerceForPassiveTrade = 0f;
		allocatedCommerceForTraders = 0f;
		allocatedCommerceForImportGoods = 0f;
		allocatedCommerceForImportFood = 0f;
		allocatedCommerceForExportFood = 0f;
		allocatedCommerceForBuildings = 0f;
		allocatedCommerceForExpeditions = 0f;
		for (int i = 0; i < tradeAgreementsWith.Count; i++)
		{
			Kingdom kingdom = tradeAgreementsWith[i];
			if (GetMerchantIn(kingdom) == null)
			{
				allocatedCommerceForPassiveTrade += game.economy.GetTradeLevelFloat("commerce", this, kingdom, null, 0);
			}
		}
		allocatedCommerceForPassiveTrade = (float)Math.Floor(allocatedCommerceForPassiveTrade);
		for (int j = 0; j < court.Count; j++)
		{
			Character character = court[j];
			if (character != null)
			{
				float tradeCommerce = character.GetTradeCommerce();
				allocatedCommerceForTraders += tradeCommerce;
				float num = character.CalcImportedGoodsCommerce();
				allocatedCommerceForImportGoods += num;
				float num2 = character.CalcImportedFoodCommerce();
				allocatedCommerceForImportFood += num2;
				float num3 = character.CalcExportedFoodCommerce();
				allocatedCommerceForExportFood += num3;
				float num4 = character.CalcExpeditionCommerce();
				allocatedCommerceForExpeditions += num4;
			}
		}
		for (int k = 0; k < realms.Count; k++)
		{
			float num5 = realms[k].CalcBuildingsCommerceUpkeep();
			allocatedCommerceForBuildings += num5;
		}
		return GetAllocatedCommerce();
	}

	public float GetAllocatedCommerce()
	{
		return allocatedCommerceForPassiveTrade + allocatedCommerceForTraders + allocatedCommerceForImportGoods + allocatedCommerceForImportFood + allocatedCommerceForExportFood + allocatedCommerceForBuildings + allocatedCommerceForExpeditions;
	}

	public float GetMaxCommerce()
	{
		float stat = GetStat(Stats.ks_commerce);
		if (float.IsNaN(stat))
		{
			return 0f;
		}
		return stat;
	}

	public float GetAvailableCommerce()
	{
		float maxCommerce = GetMaxCommerce();
		float allocatedCommerce = GetAllocatedCommerce();
		return maxCommerce - allocatedCommerce;
	}

	public float GetGoldFromTradeCenters()
	{
		return untaxGoldFromTradeCenters;
	}

	public void CalculateUntaxedGoldFromTradeCenters()
	{
		untaxGoldFromTradeCenters = 0f;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.IsTradeCenter())
			{
				realm.tradeCenter.RecalcIncome();
				untaxGoldFromTradeCenters += realm.tradeCenter.GetGoldIncomeFromInfluencedRealms(recalcRealms: false);
			}
		}
	}

	public void CreateTradeRoute(Kingdom k)
	{
		if (k != null && !tradeRouteWith.Contains(k))
		{
			UpdateTradeRouteTime(k);
			tradeRouteWith.Add(k);
			k.tradeRouteWith.Add(this);
			SendState<TradeRoutesState>();
			k.SendState<TradeRoutesState>();
		}
	}

	public bool CloseTradeRoute(Kingdom k, bool isManual = false)
	{
		if (k == null || !tradeRouteWith.Contains(k))
		{
			return false;
		}
		UpdateTradeRouteTime(k);
		tradeRouteWith.Remove(k);
		k.tradeRouteWith.Remove(this);
		Vars vars = new Vars();
		vars.Set("isManual", val: true);
		UnsetStance(k, RelationUtils.Stance.Trade, vars);
		SendState<TradeRoutesState>();
		k.SendState<TradeRoutesState>();
		return true;
	}

	public float GetTradeRouteProfit(Kingdom k)
	{
		if (k == null)
		{
			return 0f;
		}
		float num = 0f;
		if (!tradeRouteWith.Contains(k))
		{
			num += GetTradeRouteCommerseCapacityDrain(k);
		}
		for (int i = 0; i < tradeRouteWith.Count; i++)
		{
			num += GetTradeRouteCommerseCapacityDrain(tradeRouteWith[i]);
		}
		float maxCommerce = GetMaxCommerce();
		float num2 = 1f;
		num2 = ((!(maxCommerce >= num)) ? (maxCommerce / num) : 1f);
		float num3 = (float)Math.Pow(trade_route_profit_relation_mod, (GetRelationship(k) - trade_route_profit_relation_offset) / trade_route_profit_relation_divide);
		float num4 = 1f;
		for (int j = 0; j < court.Count; j++)
		{
			Character character = court[j];
			if (character != null && character.IsMerchant() && character.mission_realm?.GetKingdom() == k)
			{
				num4 = trade_route_profit_visitor_mod;
				break;
			}
		}
		float num5 = (1f + GetStat(Stats.ks_trade_income_perc) / 100f + num4 - 2f) * num2 * num3;
		float num6 = GetTradeRouteCommerseCapacityDrain(k) * num5;
		if (num6 < 1f)
		{
			num6 = 1f;
		}
		return (float)Math.Round(num6);
	}

	public float GetTradeRouteCommerseCapacityDrain(Kingdom k)
	{
		if (k == null)
		{
			return 0f;
		}
		return (float)Math.Ceiling((decimal)k.GetMaxCommerce() * (decimal)trade_route_capacity_mod);
	}

	public float GetAllTradeRouteCommerseDrain()
	{
		float num = 0f;
		foreach (Kingdom item in tradeRouteWith)
		{
			num += GetTradeRouteCommerseCapacityDrain(item);
		}
		return num;
	}

	public bool HasCardinal()
	{
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsCardinal())
			{
				return true;
			}
		}
		return false;
	}

	public float CalcFaithFromClerics()
	{
		int num = NumCourtMembersOfClass("Cleric");
		if (num == 0)
		{
			return 0f;
		}
		float stat = GetStat(Stats.ks_piety_per_cleric);
		return (float)num * stat + CalcFaithFromCardinals();
	}

	public float CalcFaithFromCardinals()
	{
		if (!is_catholic || excommunicated)
		{
			return 0f;
		}
		Kingdom hq_kingdom = game.religions.catholic.hq_kingdom;
		float num = ((this == hq_kingdom) ? RelationUtils.Def.maxRelationship : GetRelationship(hq_kingdom));
		if (num <= 0f)
		{
			return 0f;
		}
		int num2 = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsCardinalOrPope())
			{
				num2++;
			}
		}
		num *= game.religions.catholic.cardinal_piety_per_relationship_with_papacy * (float)num2;
		return (float)Math.Round(num);
	}

	public float CalcBooksFromClerics()
	{
		int num = NumCourtMembersOfClass("Cleric");
		if (num == 0)
		{
			return 0f;
		}
		float stat = GetStat(Stats.ks_books_per_cleric);
		return (float)num * stat + CalcBooksFromCardinals();
	}

	public void CalcBooksFromImportantRelatives()
	{
		booksFromImportantRelatives = 0f;
		if (royalFamily?.Relatives == null)
		{
			return;
		}
		for (int i = 0; i < royalFamily.Relatives.Count; i++)
		{
			Character character = royalFamily.Relatives[i];
			if (character != null)
			{
				booksFromImportantRelatives += character.GetStat(Stats.cs_important_relative_cleric_books_bonus);
			}
		}
	}

	public float CalcBooksFromCardinals()
	{
		if (!is_catholic || excommunicated)
		{
			return 0f;
		}
		Kingdom hq_kingdom = game.religions.catholic.hq_kingdom;
		float num = ((this == hq_kingdom) ? RelationUtils.Def.maxRelationship : GetRelationship(hq_kingdom));
		if (num <= 0f)
		{
			return 0f;
		}
		int num2 = 0;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsCardinalOrPope())
			{
				num2++;
			}
		}
		num *= game.religions.catholic.cardinal_books_per_relationship_with_papacy * (float)num2;
		return (float)Math.Round(num);
	}

	public void CalcFoodFromImportAndExport()
	{
		foodFromImport = 0f;
		upkeepFoodFromExport = 0f;
		for (int i = 0; i < court.Count; i++)
		{
			Character character = court[i];
			if (character != null && character.IsMerchant())
			{
				ImportingFoodStatus importingFoodStatus = character.FindStatus<ImportingFoodStatus>();
				if (importingFoodStatus != null)
				{
					foodFromImport += importingFoodStatus.foodAmount;
				}
				ExportingFoodStatus exportingFoodStatus = character.FindStatus<ExportingFoodStatus>();
				if (exportingFoodStatus != null)
				{
					upkeepFoodFromExport += exportingFoodStatus.foodAmount;
				}
			}
		}
	}

	public Character GetKing()
	{
		if (royalFamily == null)
		{
			return null;
		}
		return royalFamily.Sovereign;
	}

	public Character GetQueen()
	{
		if (royalFamily == null)
		{
			return null;
		}
		return royalFamily.Spouse;
	}

	public Character GetHeir()
	{
		if (royalFamily == null)
		{
			return null;
		}
		return royalFamily.Heir;
	}

	public string GetKingdomType()
	{
		if (type != Type.Regular)
		{
			return null;
		}
		if (religion != null)
		{
			return religion.GetKingdomType(this);
		}
		return nobility_key + "." + nobility_level;
	}

	public float GetInfluenceIn(Kingdom k)
	{
		if (IsEnemy(k))
		{
			return 0f;
		}
		float num = 0f;
		num = GetStat(Stats.ks_influence);
		if (GetRoyalMarriage(k))
		{
			float num2 = 0f;
			for (int i = 0; i < marriages.Count; i++)
			{
				Marriage marriage = marriages[i];
				if (marriage.wife != null && marriage.husband != null && marriage.kingdom_wife.id == id)
				{
					num2 = ((!marriage.husband.IsKing()) ? influenceDef.princess_influence : influenceDef.queen_influence);
				}
			}
			num += num2;
		}
		Character character = k.diplomats.Find((Character c) => c.kingdom_id == id);
		if (character != null)
		{
			float num3 = Game.map_clamp(character.GetClassLevel(), 0f, 15f, 0f, influenceDef.diplomat_addition);
			num += num * num3;
		}
		if (k.sovereignState == this)
		{
			num *= influenceDef.vassal_multiplier;
		}
		List<float> distance_multipliers = influenceDef.distance_multipliers;
		int num4 = Math.Min(distance_multipliers.Count, DistanceToKingdom(k));
		num *= distance_multipliers[num4 - 1];
		List<float> religion_multipliers = influenceDef.religion_multipliers;
		int index = religion.DistTo(k.religion);
		num *= religion_multipliers[index];
		if (k.is_orthodox)
		{
			num = ((!k.subordinated) ? (num + GetStat(Stats.ks_orthodox_independent_influence)) : (num + GetStat(Stats.ks_orthodox_subordinated_influence)));
		}
		if (k == game.religions.orthodox.head_kingdom)
		{
			num += GetStat(Stats.ks_influence_in_constantinople_owner);
		}
		num /= 10f;
		return Math.Min(100f, num);
	}

	public bool IsRegular()
	{
		return type == Type.Regular;
	}

	public bool HasAllAdvatages()
	{
		KingdomAdvantages kingdomAdvantages = GetAdvantages(create: false);
		if (kingdomAdvantages == null)
		{
			return false;
		}
		return kingdomAdvantages.ValidateClaimVictory() == "ok";
	}

	public void AddPrestigeModifier(string mod_name, float valueMultiplier = 1f, Vars vars = null, bool send_state = true)
	{
		prestigeObj?.AddPrestigeModifier(mod_name, vars, valueMultiplier, send_state);
	}

	public void AddPrestige(float val, bool send_state = true)
	{
		prestigeObj?.AddPrestige(val, send_state);
	}

	public void AddFameModifier(string mod_name, float valueMultiplier = 1f, Vars vars = null, bool send_state = true)
	{
		fameObj?.AddFameModifier(mod_name, vars, valueMultiplier, send_state);
	}

	public void AddFame(float val, bool send_state = true)
	{
		fameObj?.AddFame(val, send_state);
	}

	public KingdomRankingCategories GetRankingCategories(bool create = true)
	{
		if (rankingCategories == null)
		{
			if (!create)
			{
				return null;
			}
			rankingCategories = new KingdomRankingCategories(this);
		}
		return rankingCategories;
	}

	public bool LeadsAnyRanking()
	{
		List<KingdomRanking> list = game?.rankings?.rankings;
		if (list == null)
		{
			return false;
		}
		foreach (KingdomRanking item in list)
		{
			if (item.GetFame(this) > 0 || item.GetRank(this) == 1)
			{
				return true;
			}
		}
		return false;
	}

	public bool LeadsAllRankings(string category = "all")
	{
		if (category == "all")
		{
			List<KingdomRanking> list = game?.rankings?.rankings;
			if (list == null)
			{
				return false;
			}
			foreach (KingdomRanking item in list)
			{
				if (item.GetFame(this) < item.def.max_fame || item.GetRank(this) != 1)
				{
					return false;
				}
			}
			return true;
		}
		KingdomRankingCategories kingdomRankingCategories = GetRankingCategories(create: false);
		if (kingdomRankingCategories == null)
		{
			return false;
		}
		return kingdomRankingCategories.FindByName(category)?.LeadsAllRankings(this) ?? false;
	}

	public int CalcRankingCategoriesScore()
	{
		return GetRankingCategories().CalcScore();
	}

	public KingdomAdvantages GetAdvantages(bool create = true)
	{
		if (advantages == null)
		{
			if (!create)
			{
				return null;
			}
			advantages = new KingdomAdvantages(this);
		}
		return advantages;
	}

	public void RefreshAdvantages(bool create = false)
	{
		GetAdvantages(create)?.Refresh();
	}

	public int GetCostalRealmsCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i].HasCostalCastle())
			{
				num++;
			}
		}
		return num;
	}

	public int GetBuildingCount(string buildingName)
	{
		int num = 0;
		Building.Def def = game.defs.Get<Building.Def>(buildingName);
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.castle != null && realm.castle.HasWorkingBuilding(def))
			{
				num++;
			}
		}
		return num;
	}

	public int GetRebelsCount()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			List<Army> list = realms[i].armies;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].rebel != null)
				{
					num++;
				}
			}
		}
		return num;
	}

	public bool CaresAbout(Kingdom k)
	{
		if (k == null)
		{
			return false;
		}
		if (k == this)
		{
			return false;
		}
		if (IsAllyOrVassal(k))
		{
			return true;
		}
		if (is_player && k.is_player)
		{
			return true;
		}
		if (GetRoyalMarriage(k) || HasTradeAgreement(k))
		{
			return true;
		}
		if (IsEnemy(k))
		{
			return true;
		}
		if (HasNeighbor(k) || (bool)k.GetVar("is_great_power"))
		{
			return true;
		}
		if (HasPactsWith(k))
		{
			return true;
		}
		if (HasPactsAgainst(k) || k.HasPactsAgainst(this))
		{
			return true;
		}
		if (HasStance(k, RelationUtils.Stance.NonAggression))
		{
			return true;
		}
		foreach (Character item in court)
		{
			if (item != null && (item.IsDiplomat() || item.IsSpy() || item.IsMerchant() || item.IsCleric()) && item.mission_kingdom == k)
			{
				return true;
			}
		}
		return false;
	}

	public void ChangeNameAndCulture(string new_name, string cultureCSVKey = null, string unitsSetCSVKey = null, bool send_state = true)
	{
		if (!IsAuthority() && send_state)
		{
			return;
		}
		DT.Field field = null;
		if (game.kingdoms_csv == null)
		{
			return;
		}
		for (int i = 0; i < game.kingdoms_csv.children.Count; i++)
		{
			DT.Field field2 = game.kingdoms_csv.children[i];
			if (!string.IsNullOrEmpty(field2.key) && Game.ResolveKingdomCSVName(field2, out var name, out var period) && Game.MatchPeriod(period, game.map_period) && name == new_name)
			{
				field = field2;
				break;
			}
		}
		if (field == null)
		{
			field = game?.GetRealm(id)?.csv_field;
			if (field == null)
			{
				return;
			}
		}
		ActiveName = new_name;
		csv_field = field;
		LoadFromCSV(cultureCSVKey, unitsSetCSVKey);
		string suffix = null;
		if (!string.IsNullOrEmpty(game.map_period))
		{
			suffix = game.dt.GetString("Periods." + game.map_name + "." + game.map_period);
		}
		CoAMapping coAMapping = new CoAMapping();
		coAMapping.Load(game.map_name);
		game.LoadKingdomCoAIndicesAndColors(coAMapping, this, suffix);
		if (send_state)
		{
			SendState<NameAndCultureState>();
		}
		NotifyListeners("name_changed");
	}

	public Kingdom(Game game)
		: base(game)
	{
		time = game.time;
	}

	public override string ToString()
	{
		string text = $"Kingdom {id}({Name})";
		bool flag = false;
		List<Multiplayer.PlayerData> all = Multiplayer.CurrentPlayers.GetAll();
		if (all != null && all.Count > 0)
		{
			for (int i = 0; i < all.Count; i++)
			{
				Multiplayer.PlayerData playerData = all[i];
				if (playerData.kingdomId == id)
				{
					if (!flag)
					{
						flag = true;
						text += ", pids: ";
					}
					else
					{
						text += ", ";
					}
					text += playerData.pid;
				}
			}
		}
		return text;
	}

	public override Value GetDumpStateValue()
	{
		return Name;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("resources", resources.ToString());
		dump.Append("is_defeated", IsDefeated().ToString());
		dump.Append("defeated_by", defeated_by);
		if (!IsDefeated())
		{
			dump.Append("income", Object.Dump(incomes));
		}
		dump.Append("tax_level", taxLevel);
		dump.Append("is_player", is_player.ToString());
		dump.Append("is_defeated", IsDefeated().ToString());
		dump.Append("crown_authority", GetCrownAuthority()?.GetValue());
		if (realms != null && realms.Count > 0)
		{
			dump.OpenSection("realms");
			foreach (Realm realm in realms)
			{
				dump.Append(realm?.town_name, realm);
			}
			dump.CloseSection("realms");
		}
		if (neighbors != null && neighbors.Count > 0)
		{
			dump.OpenSection("neighbors");
			foreach (Kingdom neighbor in neighbors)
			{
				dump.Append(neighbor?.ToString());
			}
			dump.CloseSection("neighbors");
		}
		if (externalBorderRealms != null && externalBorderRealms.Count > 0)
		{
			dump.OpenSection("external_border_realms");
			foreach (Realm externalBorderRealm in externalBorderRealms)
			{
				dump.Append(externalBorderRealm?.ToString());
			}
			dump.CloseSection("external_border_realms");
		}
		dump.Append("improve_opinions_diplomat", improveOpinionsDiplomat);
		if (opinions?.by_name != null && opinions.by_name.Count > 0)
		{
			dump.OpenSection("opinions");
			foreach (KeyValuePair<string, Opinion> item in opinions.by_name)
			{
				dump.Append(item.Key, item.Value?.value);
			}
			dump.CloseSection("opinions");
		}
		dump.Append("fame", fame);
		dump.OpenSection("royal_family");
		dump.Append("generations_passed", generationsPassed);
		if (royalFamily != null)
		{
			dump.Append("sovereign", royalFamily.Sovereign);
			dump.Append("spouse", royalFamily.Spouse);
			dump.Append("spouse_fascination", royalFamily.SpouseFacination);
			dump.Append("heir", royalFamily.Heir);
			dump.Append("pretender", royalFamily.crownPretender?.pretender);
			if (royalFamily.Children != null && royalFamily.Children.Count > 0)
			{
				dump.OpenSection("children");
				for (int i = 0; i < royalFamily.Children.Count; i++)
				{
					dump.Append(royalFamily.Children[i]?.ToString());
				}
				dump.CloseSection("children");
			}
			if (royalFamily.Relatives != null && royalFamily.Relatives.Count > 0)
			{
				dump.OpenSection("relatives");
				for (int j = 0; j < royalFamily.Relatives.Count; j++)
				{
					dump.Append(royalFamily.Relatives[j]?.ToString());
				}
				dump.CloseSection("relatives");
			}
		}
		dump.CloseSection("royal_family");
		if (court != null && court.Find((Character c) => c != null) != null)
		{
			dump.OpenSection("court");
			for (int num = 0; num < court.Count; num++)
			{
				Character character = court[num];
				if (character != null)
				{
					dump.Append(num.ToString(), character);
				}
			}
			dump.CloseSection("court");
		}
		if (special_court != null && special_court.Find((Character c) => c != null) != null)
		{
			dump.OpenSection("special_court");
			for (int num2 = 0; num2 < special_court.Count; num2++)
			{
				Character character2 = special_court[num2];
				if (character2 != null)
				{
					dump.Append(num2.ToString(), character2);
				}
			}
			dump.CloseSection("special_court");
		}
		if (traditions != null && traditions.Count > 0)
		{
			dump.OpenSection("traditions");
			for (int num3 = 0; num3 < traditions.Count; num3++)
			{
				dump.Append(traditions[num3]?.ToString());
			}
			dump.CloseSection("traditions");
		}
		dump.Append("religion", religion?.ToString());
		dump.Append("subordinated", subordinated.ToString());
		dump.Append("excommunicated", excommunicated.ToString());
		dump.Append("caliphate", caliphate);
		dump.Append("jihad", jihad?.ToString());
		dump.Append("jihad_attacker", jihad_attacker?.ToString());
		dump.Append("patriarch", patriarch?.ToString());
		dump.Append("patriarch_castle", patriarch_castle?.ToString());
		if (patriarch_bonuses != null && patriarch_bonuses.Count > 0)
		{
			dump.OpenSection("patriarch_bonuses");
			for (int num4 = 0; num4 < patriarch_bonuses.Count; num4++)
			{
				dump.Append(patriarch_bonuses[num4]?.ToString());
			}
			dump.CloseSection("patriarch_bonuses");
		}
		if (patriarch_mods != null && patriarch_mods.Count > 0)
		{
			dump.OpenSection("patriarch_mods");
			for (int num5 = 0; num5 < patriarch_mods.Count; num5++)
			{
				dump.Append(patriarch_mods[num5]?.ToString());
			}
			dump.CloseSection("patriarch_mods");
		}
		if (patriarch_candidates != null && patriarch_candidates.Count > 0)
		{
			dump.OpenSection("patriarch_candidates");
			for (int num6 = 0; num6 < patriarch_candidates.Count; num6++)
			{
				dump.Append(patriarch_candidates[num6]?.ToString());
			}
			dump.CloseSection("patriarch_candidates");
		}
		if (pagan_beliefs != null && pagan_beliefs.Count > 0)
		{
			dump.OpenSection("pagan_traditions");
			for (int num7 = 0; num7 < pagan_beliefs.Count; num7++)
			{
				dump.Append(pagan_beliefs[num7]?.name);
			}
			dump.CloseSection("pagan_traditions");
		}
		dump.Append("culture_group", game?.cultures?.GetGroup(culture));
		dump.Append("culture", culture?.ToString());
		dump.Append("sovereign_state", sovereignState?.Name);
		if (vassalStates != null && vassalStates.Count > 0)
		{
			dump.OpenSection("vassal_states");
			for (int num8 = 0; num8 < vassalStates.Count; num8++)
			{
				dump.Append(vassalStates[num8]?.Name);
			}
			dump.CloseSection("vassal_states");
		}
		if (ai?.helpWithRebels != null && ai.helpWithRebels.Count > 0)
		{
			dump.OpenSection("help_with_rebels");
			for (int num9 = 0; num9 < ai.helpWithRebels.Count; num9++)
			{
				dump.Append(ai.helpWithRebels[num9]?.Item1?.Name);
			}
			dump.CloseSection("help_with_rebels");
		}
		if (patriarch_bonuses != null && patriarch_bonuses.Count > 0)
		{
			dump.OpenSection("patriarch_bonuses");
			for (int num10 = 0; num10 < patriarch_bonuses.Count; num10++)
			{
				dump.Append(patriarch_bonuses[num10]?.ToString());
			}
			dump.CloseSection("patriarch_bonuses");
		}
		if (occupiedKeeps != null && occupiedKeeps.Count > 0)
		{
			dump.OpenSection("occupied_keeps");
			for (int num11 = 0; num11 < occupiedKeeps.Count; num11++)
			{
				dump.Append(num11.ToString(), occupiedKeeps[num11]);
			}
			dump.CloseSection("occupied_keeps");
		}
		if (occupiedRealms != null && occupiedRealms.Count > 0)
		{
			dump.OpenSection("occupied_realms");
			for (int num12 = 0; num12 < occupiedRealms.Count; num12++)
			{
				dump.Append(occupiedRealms[num12]?.name);
			}
			dump.CloseSection("occupied_realms");
		}
		dump.Append("last_peace_time", last_peace_time.ToString());
		if (tradeAgreementsWith != null && tradeAgreementsWith.Count > 0)
		{
			dump.OpenSection("trade_agreements");
			for (int num13 = 0; num13 < tradeAgreementsWith.Count; num13++)
			{
				dump.Append(tradeAgreementsWith[num13]?.Name);
			}
			dump.CloseSection("trade_agreements");
		}
		if (tradeRouteWith != null && tradeRouteWith.Count > 0)
		{
			dump.OpenSection("trade_routes");
			for (int num14 = 0; num14 < tradeRouteWith.Count; num14++)
			{
				dump.Append(tradeRouteWith[num14]?.Name);
			}
			dump.CloseSection("trade_routes");
		}
		if (wars != null && wars.Count > 0)
		{
			dump.OpenSection("wars");
			for (int num15 = 0; num15 < wars.Count; num15++)
			{
				dump.Append(wars[num15]?.ToString());
			}
			dump.CloseSection("wars");
		}
		if (pacts != null && pacts.Count > 0)
		{
			dump.OpenSection("pacts");
			for (int num16 = 0; num16 < pacts.Count; num16++)
			{
				dump.Append(pacts[num16]?.ToString());
			}
			dump.CloseSection("pacts");
		}
		if (pacts_against != null && pacts_against.Count > 0)
		{
			dump.OpenSection("pacts_against");
			for (int num17 = 0; num17 < pacts_against.Count; num17++)
			{
				dump.Append(pacts_against[num17]?.ToString());
			}
			dump.CloseSection("pacts_against");
		}
		if (marriages != null && marriages.Count > 0)
		{
			dump.OpenSection("marriages");
			for (int num18 = 0; num18 < marriages.Count; num18++)
			{
				dump.Append(marriages[num18]?.ToString());
			}
			dump.CloseSection("marriages");
		}
		if (nonAgressions != null && nonAgressions.Count > 0)
		{
			dump.OpenSection("non_aggressions");
			for (int num19 = 0; num19 < nonAgressions.Count; num19++)
			{
				dump.Append(nonAgressions[num19]?.Name);
			}
			dump.CloseSection("non_aggressions");
		}
		if (relations != null && relations.Count > 0)
		{
			dump.OpenSection("relations");
			for (int num20 = 0; num20 < id - 1; num20++)
			{
				Kingdom kingdom = game.GetKingdom(num20 + 1);
				KingdomAndKingdomRelation kingdomAndKingdomRelation = relations[num20];
				if (kingdomAndKingdomRelation != null)
				{
					kingdomAndKingdomRelation.CalcFadeWithTime(this, kingdom);
					if (!kingdomAndKingdomRelation.is_default)
					{
						dump.Append(kingdom?.Name, kingdomAndKingdomRelation.ToString() + ", importance: " + ai?.CalcDiplomaticImportance(kingdom) + ", truce: " + IsInTruceWith(kingdom).ToString());
					}
				}
			}
			dump.CloseSection("relations");
		}
		if (foreigners != null && foreigners.Count > 0)
		{
			dump.OpenSection("foreigners");
			for (int num21 = 0; num21 < foreigners.Count; num21++)
			{
				dump.Append(foreigners[num21]?.ToString());
			}
			dump.CloseSection("foreigners");
		}
		if (actions != null && actions.current != null)
		{
			dump.Append("current", actions.current?.def?.field?.key);
		}
		if (royal_dungeon?.prisoners != null && royal_dungeon.prisoners.Count > 0)
		{
			dump.OpenSection("prisoners");
			for (int num22 = 0; num22 < royal_dungeon.prisoners.Count; num22++)
			{
				dump.Append(royal_dungeon.prisoners[num22]?.ToString());
			}
			dump.CloseSection("prisoners");
		}
		stats?.DumpInnerState(dump, verbosity);
		base.DumpInnerState(dump, verbosity);
	}

	public float GetRebellionRiskGlobal()
	{
		if (stability == null)
		{
			return 0f;
		}
		return stability.GetStability();
	}

	public float CalcRebellionsStrength()
	{
		float num = 0f;
		for (int i = 0; i < rebellions.Count; i++)
		{
			num += rebellions[i].GetPower();
		}
		return num;
	}

	public float GetAvgRebellionRiskLocal()
	{
		List<RebellionRiskCategory.Def> categories = RebellionRiskCategory.GetCategories(game);
		float num = 0f;
		for (int i = 0; i < categories.Count; i++)
		{
			RebellionRiskCategory.Def def = categories[i];
			if (def != null && !def.isGlobal && def.index != -1)
			{
				float avgRebellionRisk = GetAvgRebellionRisk(def.index);
				num += avgRebellionRisk;
			}
		}
		return (float)Math.Ceiling(num);
	}

	public float GetAvgRebellionRiskTotal()
	{
		List<RebellionRiskCategory.Def> categories = RebellionRiskCategory.GetCategories(game);
		float num = 0f;
		for (int i = 0; i < categories.Count; i++)
		{
			RebellionRiskCategory.Def def = categories[i];
			if (def != null && def.index != -1)
			{
				float avgRebellionRisk = GetAvgRebellionRisk(def.index);
				num += avgRebellionRisk;
			}
		}
		return (float)Math.Ceiling(num);
	}

	public float GetAvgRebellionRisk(int type)
	{
		if (realms == null || realms.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < realms.Count; i++)
		{
			num += realms[i].GetRebellionRisk(type);
		}
		return (float)Math.Ceiling(num / (float)realms.Count);
	}

	public int GetRebelArmiesCount()
	{
		int num = 0;
		if (rebellions == null || rebellions.Count == 0)
		{
			return num;
		}
		for (int i = 0; i < rebellions.Count; i++)
		{
			Rebellion rebellion = rebellions[i];
			if (rebellion != null)
			{
				num += rebellion.GetArmiesCount();
			}
		}
		return num;
	}

	public int GetMaxRebelsToSpawn()
	{
		if (realms.Count < 1)
		{
			return 0;
		}
		KingdomStability kingdomStability = stability;
		if (kingdomStability == null)
		{
			return 0;
		}
		int val = (int)Math.Ceiling(rebel_spawns_per_event_realms_perc / 100f * (float)realms.Count);
		int val2 = kingdomStability.def.max_rebels_per_game - RebellionRisk.GetRebelCount(game);
		int val3 = kingdomStability.def.max_rebels_per_kingdom - kingdomStability.GetRebelCountKingdom();
		return Math.Min(val, Math.Min(val2, val3));
	}

	public float GetStabilityTotalPositives()
	{
		List<RebellionRiskCategory.Def> categories = RebellionRiskCategory.GetCategories(game);
		float num = 0f;
		for (int i = 0; i < categories.Count; i++)
		{
			RebellionRiskCategory.Def def = categories[i];
			if (def != null && def.isGlobal)
			{
				float avgRebellionRisk = GetAvgRebellionRisk(def.index);
				if (avgRebellionRisk < 0f)
				{
					num -= avgRebellionRisk;
				}
			}
		}
		return num;
	}

	public float GetStabilityTotalNegatives()
	{
		List<RebellionRiskCategory.Def> categories = RebellionRiskCategory.GetCategories(game);
		float num = 0f;
		for (int i = 0; i < categories.Count; i++)
		{
			RebellionRiskCategory.Def def = categories[i];
			if (def != null && def.isGlobal)
			{
				float avgRebellionRisk = GetAvgRebellionRisk(def.index);
				if (avgRebellionRisk > 0f)
				{
					num += avgRebellionRisk;
				}
			}
		}
		return num;
	}

	private float CalcSplitMagnitude()
	{
		DT.Field field = def.FindChild("kingdom_split")?.FindChild("magnitude");
		if (field == null)
		{
			return 0f;
		}
		float num = field.Value();
		CrownAuthority crownAuthority = GetCrownAuthority();
		DT.Field field2 = field.FindChild("crown_authority");
		num += game.Map3(crownAuthority.GetValue(), crownAuthority.Min(), 0f, crownAuthority.Max(), field2.Value(0), field2.Value(1), field2.Value(2), clamp: true);
		Opinion opinion = opinions.Find("NobilityOpinion");
		DT.Field field3 = field.FindChild("nobility_opinion");
		num += game.Map3(opinion.value, opinion.def.min_value, 0f, opinion.def.max_value, field3.Value(0), field3.Value(1), field3.Value(2), clamp: true);
		if (caliphate)
		{
			num += field.GetFloat("caliphate");
		}
		if (is_orthodox && !subordinated)
		{
			num += field.GetFloat("autocephaly");
		}
		if (royalFamily.Heir == null)
		{
			bool flag = false;
			for (int i = 0; i < court.Count; i++)
			{
				Character character = court[i];
				if (character != null && character != GetKing())
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				num += field.GetFloat("no_king_candidates");
			}
		}
		num += (float)rebellions.Count * field.GetFloat("per_rebellion");
		num += (float)wars.Count * field.GetFloat("per_war");
		num = Math.Max(field.GetFloat("min"), num);
		return Math.Min(field.GetFloat("max"), num);
	}

	private List<Realm> CalcSplitRealms()
	{
		List<Realm> list = null;
		DT.Field field = def.FindChild("kingdom_split");
		bool flag = field.GetBool("governed_provinces_can_split");
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm != null && realm != game.religions.catholic.hq_realm && realm != game.religions.orthodox.hq_realm && realm.castle?.governor != GetKing() && (flag || realm.castle.governor == null))
			{
				if (list == null)
				{
					list = new List<Realm>();
				}
				list.Add(realm);
			}
		}
		if (list == null)
		{
			return null;
		}
		list.Sort((Realm a, Realm b) => a.GetTotalRebellionRisk().CompareTo(b.GetTotalRebellionRisk()));
		float num = field.GetFloat("provinces_perc");
		int num2 = list.Count - (int)Math.Ceiling((float)list.Count * num / 100f);
		list.RemoveRange(list.Count - num2, num2);
		float val = CalcSplitMagnitude();
		Vars vars = new Vars();
		vars.Set("magnitude", val);
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			Realm realm2 = list[num3];
			if (!realm2.IsDisorder() && !realm2.IsOccupied())
			{
				vars.Set("stability", realm2.GetTotalRebellionRisk());
				float num4 = field.GetFloat("split_chance", vars);
				if (!((float)game.Random(0, 100) < num4))
				{
					list.RemoveAt(num3--);
				}
			}
		}
		list.Sort((Realm a, Realm b) => (b.castle.governor != null).CompareTo(a.castle.governor != null));
		int num5 = field.GetInt("min_remaining_realms", null, 2);
		while (list.Count > 0 && realms.Count - list.Count < num5)
		{
			list.RemoveAt(list.Count - 1);
		}
		return list;
	}

	public void SplitKingdom()
	{
		if (!AssertAuthority())
		{
			return;
		}
		List<Realm> list = CalcSplitRealms();
		if (list == null || list.Count == 0)
		{
			return;
		}
		List<Realm> list2 = new List<Realm>();
		List<Realm> list3 = new List<Realm>();
		List<Kingdom> list4 = new List<Kingdom>();
		List<Kingdom> list5 = new List<Kingdom>();
		for (int i = 0; i < list.Count; i++)
		{
			Realm realm = list[i];
			if (realm == null)
			{
				continue;
			}
			if (realm.IsOccupied() && realm.controller is Rebellion rebellion)
			{
				Character character = rebellion?.leader?.character;
				RebellionIndependence rebellionIndependence = rebellion?.GetComponent<RebellionIndependence>();
				if (rebellionIndependence == null || !rebellionIndependence.DeclareIndependence())
				{
					continue;
				}
				if (character != null && character.IsKing())
				{
					list2.Add(realm);
					list4.Add(realm.GetKingdom());
					continue;
				}
				list3.Add(realm);
				if (!list5.Contains(realm.GetKingdom()))
				{
					list5.Add(realm.GetKingdom());
				}
			}
			else
			{
				if (realm.IsOccupied())
				{
					continue;
				}
				if (!realm.IsOwnStance(this))
				{
					if (list4.Contains(realm.GetKingdom()))
					{
						list2.Add(realm);
					}
					else
					{
						list3.Add(realm);
					}
					continue;
				}
				Kingdom kingdom = null;
				for (int j = 0; j < i; j++)
				{
					Realm realm2 = list[j];
					if (realm2 != null && realm.logicNeighborsRestricted.Contains(realm2))
					{
						kingdom = realm2.GetKingdom();
						break;
					}
				}
				if (kingdom != null)
				{
					kingdom.DeclareIndependenceOrJoin(new List<Realm> { realm }, realm.castle?.governor);
					if (list4.Contains(kingdom))
					{
						list2.Add(realm);
					}
					else
					{
						list3.Add(realm);
					}
					continue;
				}
				Character king = realm.castle?.governor;
				Kingdom kingdom2 = game?.TryDeclareIndependence(new List<Realm> { realm }, king);
				if (kingdom2 == null || kingdom2 == this)
				{
					continue;
				}
				if (kingdom2.realms.Count == 1)
				{
					list2.Add(realm);
					list4.Add(kingdom2);
					continue;
				}
				list3.Add(realm);
				if (!list5.Contains(kingdom2))
				{
					list5.Add(kingdom2);
				}
			}
		}
		Vars vars = new Vars();
		vars.Set("realms", list);
		if (list2.Count > 0)
		{
			vars.Set("realms_new", list2);
		}
		if (list3.Count > 0)
		{
			vars.Set("realms_joined", list3);
		}
		if (list4.Count > 0)
		{
			vars.Set("kingdoms_new", list4);
		}
		if (list5.Count > 0)
		{
			vars.Set("kingdoms_joined", list5);
		}
		FireEvent("kingdom_split", vars);
	}

	public void JoinArmy(Army army, bool joinCourt)
	{
		if (army?.battle != null)
		{
			army.battle.Cancel(Battle.VictoryReason.Retreat);
		}
		if (army == null || army.leader == null)
		{
			return;
		}
		if (joinCourt)
		{
			if (court == null || court.Count == 0)
			{
				InitCourt();
			}
			if (GetFreeCourtSlotIndex() == -1)
			{
				army.Destroy();
				return;
			}
		}
		army.Stop();
		army.leader.GetKingdom()?.DelCourtMember(army.leader, send_state: true, kill_or_throneroom: false);
		army.leader.GetSpecialCourtKingdom()?.DelSpecialCourtMember(army.leader);
		army.leader.SetKingdom(id);
		army.SetKingdom(id);
		bool flag = false;
		for (int i = 0; i < army.units.Count; i++)
		{
			Unit unit = army.units[i];
			if (unit?.def != null && unit.def.type == Unit.Type.Noble)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			army.AddNoble();
		}
		if (army.leader.IsCrusader())
		{
			Crusade crusade = game.religions.catholic.crusade;
			AddResources(KingdomAI.Expense.Category.Military, ResourceType.Gold, crusade.wealth);
			crusade.wealth = 0f;
			crusade.SendState<Crusade.WealthState>();
			crusade.NotifyListeners("wealth_changed");
		}
		if (army.rebel != null)
		{
			army.rebel.SetArmy(null);
			army.SetRebel(null);
		}
		if (joinCourt)
		{
			AddCourtMember(army.leader);
		}
	}

	public bool DeclareIndependenceOrJoin(List<Realm> realms = null, Character king = null, List<Character> courtChars = null, List<Army> armies = null, Religion religion = null, bool go_to_war = true, bool bonusses = true)
	{
		if (!IsAuthority())
		{
			return false;
		}
		bool flag = IsDefeated();
		if (flag && (realms == null || realms.Count == 0))
		{
			return false;
		}
		StartRecheckKingTimer();
		if (king != null)
		{
			if (courtChars == null)
			{
				courtChars = new List<Character> { king };
			}
			else if (!courtChars.Contains(king))
			{
				courtChars.Add(king);
			}
		}
		if (realms != null && realms.Count > 0)
		{
			using (new CacheRBS("DeclareIndependenceOrJoin"))
			{
				for (int num = realms.Count - 1; num >= 0; num--)
				{
					Realm realm = realms[num];
					realm.SetKingdom(id);
					realm.SetDisorder(value: false);
				}
			}
		}
		if (courtChars != null)
		{
			for (int i = 0; i < courtChars.Count; i++)
			{
				Character character = courtChars[i];
				if (character == null || character.IsDead())
				{
					continue;
				}
				bool flag2 = flag && character == king;
				character.GetKingdom().DelCourtMember(character, send_state: true, kill_or_throneroom: false);
				character.GetSpecialCourtKingdom()?.DelSpecialCourtMember(character);
				character.GetMarriage()?.wife?.SetKingdom(id);
				if (!flag2)
				{
					if (court == null || court.Count == 0)
					{
						InitCourt();
					}
					if (GetFreeCourtSlotIndex() == -1)
					{
						character.Die();
						continue;
					}
				}
				character.prison_reason = null;
				Army army = character.GetArmy();
				if (army != null)
				{
					JoinArmy(army, !flag2);
					continue;
				}
				character.SetKingdom(id);
				if (!flag2)
				{
					AddCourtMember(character);
				}
			}
		}
		if (armies != null)
		{
			for (int j = 0; j < armies.Count; j++)
			{
				Army army2 = armies[j];
				if (army2 != null && army2.kingdom_id != id)
				{
					bool flag3 = flag && army2.leader == king;
					JoinArmy(army2, !flag3);
				}
			}
		}
		if (flag)
		{
			if (king == null || king.IsDead())
			{
				king = CharacterFactory.CreateKing(this);
			}
			Character character2 = null;
			if (royalFamily == null)
			{
				royalFamily = new RoyalFamily(this);
			}
			else
			{
				character2 = royalFamily.Sovereign;
			}
			royalFamily.ChangeSovereign(king, null, "independence");
			character2?.Die(new DeadStatus("unknown", character2));
			if (religion != null)
			{
				SetReligion(religion);
			}
			if (realms != null && go_to_war)
			{
				for (int k = 0; k < realms.Count; k++)
				{
					Kingdom lastOwner = realms[k].GetLastOwner();
					if (lastOwner != null && lastOwner != this && !lastOwner.IsDefeated())
					{
						StartWarWith(lastOwner, War.InvolvementReason.DeclarationOfIndependence);
					}
				}
			}
			if (royalFamily.Sovereign == null)
			{
				royalFamily = new RoyalFamily(this);
				royalFamily.ChangeSovereign(king, null, "independence");
			}
			if (realms != null)
			{
				for (int l = 0; l < realms.Count; l++)
				{
					realms[l].pop_majority.strength = 0f;
					realms[l].AdjustPopMajority(-100f, this);
					if (religion != null)
					{
						realms[l].SetReligion(religion);
					}
				}
			}
			DT.Field field = def.FindChild("on_forming_new_kingdom");
			if (bonusses && field != null)
			{
				GetCrownAuthority().SetValue(field.GetInt("crown_authority"));
				for (int m = 0; m < opinions.opinions.Count; m++)
				{
					opinions.opinions[m].Set(field.GetInt("opinions"), "new_kingdom");
				}
				SetResources(ResourceType.Gold, field.GetInt("gold"), send_state: false);
				SetResources(ResourceType.Piety, field.GetInt("piety"), send_state: false);
				SetResources(ResourceType.Books, field.GetInt("books"), send_state: false);
				total_earned.Set(resources, 1f, ResourceType.Trade);
				total_spent.Clear();
				SendState<ResourcesState>();
			}
		}
		return true;
	}

	public void SetImproveOpinionsDiplomat(Character diplomat)
	{
		if (diplomat != improveOpinionsDiplomat && (diplomat == null || diplomat.IsDiplomat()))
		{
			if (improveOpinionsDiplomat != null)
			{
				improveOpinionsDiplomat.DelStatus<ImprovingOpinionsStatus>();
			}
			improveOpinionsDiplomat = diplomat;
			improveOpinionsDiplomat?.SetStatus<ImprovingOpinionsStatus>();
			SendState<ImproveOpinionsDiplomatState>();
		}
	}

	public int GetNumberOfKeeps()
	{
		int num = 0;
		for (int i = 0; i < realms.Count; i++)
		{
			Realm realm = realms[i];
			if (realm.IsDisorder())
			{
				continue;
			}
			for (int j = 0; j < realm.settlements.Count; j++)
			{
				Settlement settlement = realm.settlements[j];
				if (settlement.IsActiveSettlement() && settlement.type == "Keep")
				{
					num++;
				}
			}
		}
		return num;
	}

	public Kingdom(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Kingdom(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}

	public Dictionary<string, ResourceInfo> GetResourcesInfo()
	{
		if (!resources_info_valid || resources_info == null)
		{
			resources_info_valid = true;
			BuildResourcesInfo();
		}
		return resources_info;
	}

	public ResourceInfo GetResourceInfo(string name, bool create_if_needed = true, bool calc_if_needed = true)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Dictionary<string, ResourceInfo> resourcesInfo = GetResourcesInfo();
		if (resourcesInfo == null)
		{
			return null;
		}
		using (Game.Profile("Kingdom.GetResourceInfo"))
		{
			ResourceInfo.dictionary_lookups++;
			resourcesInfo.TryGetValue(name, out var value);
			if (value == null)
			{
				if (!create_if_needed)
				{
					return null;
				}
				value = new ResourceInfo(this, name);
				ResourceInfo.dictionary_adds++;
				resourcesInfo.Add(name, value);
			}
			value.ClearIfOutOfDate();
			if (value.availability == ResourceInfo.Availability.Unknown && calc_if_needed)
			{
				CalcResourceAvailabilityInKingdom(value);
			}
			return value;
		}
	}

	public void RefreshResourcesInfo(bool force_full_rebuild = false)
	{
		resources_info_valid = false;
		if (force_full_rebuild)
		{
			ClearResourcesInfo(full: true);
			resources_info = null;
			BuildResourcesInfo();
		}
	}

	private void BuildResourcesInfo()
	{
		ResourceInfo.total_recalcs++;
		using (Game.Profile("Kingdom.BuildResourcesInfo"))
		{
			if (resources_info == null)
			{
				ResourceInfo.dictionary_allocs++;
				resources_info = new Dictionary<string, ResourceInfo>(512);
			}
			ClearResourcesInfo();
			InitProcersInfo();
		}
	}

	private void ClearResourcesInfo(bool full = false)
	{
		if (resources_info == null)
		{
			return;
		}
		if (++resources_info_version == 0)
		{
			resources_info_version = 1;
		}
		if (!full)
		{
			return;
		}
		using (Game.Profile("Kingdom.ClearResourcesInfo"))
		{
			foreach (KeyValuePair<string, ResourceInfo> item in resources_info)
			{
				item.Value.Clear(full);
			}
			for (int i = 0; i < realms.Count; i++)
			{
				realms[i].castle?.ClearResourcesInfo(full, invalidate_kingdom: false);
			}
		}
	}

	private void AddImportedGoodsToResInfo(Character c)
	{
		if (c?.importing_goods == null)
		{
			return;
		}
		for (int i = 0; i < c.importing_goods.Count; i++)
		{
			Character.ImportedGood importedGood = c.importing_goods[i];
			if (!string.IsNullOrEmpty(importedGood.name))
			{
				GetResourceInfo(importedGood.name, create_if_needed: true, calc_if_needed: false).importer = c;
			}
		}
	}

	private void AddImportedGoodsToResInfo()
	{
		if (court != null)
		{
			for (int i = 0; i < court.Count; i++)
			{
				Character c = court[i];
				AddImportedGoodsToResInfo(c);
			}
		}
	}

	private void InitProcersInfo()
	{
		using (Game.Profile("Kingdom.InitProcersInfo"))
		{
			AddImportedGoodsToResInfo();
			if (realms == null)
			{
				return;
			}
			for (int i = 0; i < realms.Count; i++)
			{
				Castle castle = realms[i]?.castle;
				if (castle != null)
				{
					InitProducersInfo(castle);
				}
			}
		}
	}

	private void InitProducersInfo(Castle castle)
	{
		if (castle.resources_info_valid)
		{
			return;
		}
		castle.resources_info_valid = true;
		using (Game.Profile("Kingdom.InitProducersInfo(Caslte)"))
		{
			District.Def common = District.Def.GetCommon(game);
			AddProducerInfo(castle, common);
			District.Def pF = District.Def.GetPF(game);
			AddProducerInfo(castle, pF);
			List<District.Def> buildableDistricts = castle.GetBuildableDistricts();
			if (buildableDistricts != null)
			{
				for (int i = 0; i < buildableDistricts.Count; i++)
				{
					District.Def d = buildableDistricts[i];
					AddProducerInfo(castle, d);
				}
			}
			Realm realm = castle.GetRealm();
			if (realm == null)
			{
				return;
			}
			if (realm.settlements != null)
			{
				for (int j = 0; j < realm.settlements.Count; j++)
				{
					Settlement settlement = realm.settlements[j];
					if (!(settlement is Castle) && settlement.IsActiveSettlement())
					{
						ResourceInfo.ProducerRef producer = new ResourceInfo.ProducerRef
						{
							type = "Settlement",
							name = settlement.type,
							castle = realm.castle,
							bdef = null,
							building = null
						};
						AddProducer(producer);
					}
				}
			}
			if (realm.features != null)
			{
				for (int k = 0; k < realm.features.Count; k++)
				{
					string name = realm.features[k];
					ResourceInfo.ProducerRef producer2 = new ResourceInfo.ProducerRef
					{
						type = "ProvinceFeature",
						name = name,
						castle = realm.castle,
						bdef = null,
						building = null
					};
					AddProducer(producer2);
				}
			}
		}
	}

	private void AddResourceProducer(string name, ResourceInfo.ProducerRef producer)
	{
		producer.castle.GetResourceInfo(name, create_if_needed: true, calc_if_needed: false).AddProducer(producer);
		ResourceInfo.ProducerRef producer2 = new ResourceInfo.ProducerRef
		{
			type = "Castle",
			castle = producer.castle
		};
		GetResourceInfo(name, create_if_needed: true, calc_if_needed: false).AddProducer(producer2);
	}

	private void AddProducer(ResourceInfo.ProducerRef producer)
	{
		AddResourceProducer(producer.name, producer);
	}

	private void AddProducerInfo(Castle castle, District.Def d)
	{
		using (Game.Profile("Kingdom.AddProducerInfo(Caslte, District)"))
		{
			ResourceInfo resourceInfo = castle.GetResourceInfo(d.id, create_if_needed: true, calc_if_needed: false);
			if (resourceInfo == null)
			{
				return;
			}
			resourceInfo.availability = (resourceInfo.own_availability = ResourceInfo.Availability.Available);
			ResourceInfo.ProducerRef producer = new ResourceInfo.ProducerRef
			{
				type = "District",
				name = d.id,
				castle = castle,
				bdef = null,
				building = null
			};
			AddProducer(producer);
			if (d?.buildings == null)
			{
				return;
			}
			for (int i = 0; i < d.buildings.Count; i++)
			{
				Building.Def def = d.buildings[i].def;
				if (CheckReligionRequirements(def) && castle.MayBuildBuilding(def, full_check: false))
				{
					ResourceInfo.find_buildings++;
					Building building = castle.FindBuilding(def);
					ResourceInfo.ProducerRef producer2 = new ResourceInfo.ProducerRef
					{
						type = "Building",
						name = def.id,
						castle = castle,
						bdef = def,
						building = building
					};
					AddProducer(producer2);
					AddBuildingProducer(producer2);
				}
			}
		}
	}

	private void AddBuildingProducer(ResourceInfo.ProducerRef producer)
	{
		using (Game.Profile("Kingdom.AddBuildingProducer"))
		{
			if (producer.bdef.variant_of != null)
			{
				AddResourceProducer(producer.bdef.variant_of.id, producer);
			}
			if (producer.bdef.upgrades?.buildings != null)
			{
				for (int i = 0; i < producer.bdef.upgrades.buildings.Count; i++)
				{
					Building.Def def = producer.bdef.upgrades.buildings[i].def;
					if (CheckReligionRequirements(def))
					{
						ResourceInfo.find_buildings++;
						Building building = producer.castle?.FindBuilding(def);
						ResourceInfo.ProducerRef producer2 = new ResourceInfo.ProducerRef
						{
							type = "Building",
							name = def.id,
							castle = producer.castle,
							bdef = def,
							building = building
						};
						AddProducer(producer2);
						AddBuildingProducer(producer2);
					}
				}
			}
			if (producer.bdef?.produces != null)
			{
				for (int j = 0; j < producer.bdef.produces.Count; j++)
				{
					string resource = producer.bdef.produces[j].resource;
					AddResourceProducer(resource, producer);
				}
			}
			if (producer.bdef?.produces_completed != null)
			{
				for (int k = 0; k < producer.bdef.produces_completed.Count; k++)
				{
					string resource2 = producer.bdef.produces_completed[k].resource;
					AddResourceProducer(resource2, producer);
				}
			}
		}
	}

	private void CalcResourceAvailabilityInKingdom(ResourceInfo kingdom_res)
	{
		if (kingdom_res.availability == ResourceInfo.Availability.Calculating)
		{
			Error($"Infinite loop while calculating availability for {kingdom_res}");
		}
		else
		{
			if (kingdom_res.availability != ResourceInfo.Availability.Unknown)
			{
				return;
			}
			ResourceInfo.per_kingdom_recalcs++;
			using (Game.Profile("Kingdom.CalcResourceAvailabilityInKingdom"))
			{
				kingdom_res.availability = (kingdom_res.own_availability = ResourceInfo.Availability.Calculating);
				ResourceInfo.Availability availability = ResourceInfo.Availability.Impossible;
				ResourceInfo.Availability availability2 = ResourceInfo.Availability.Impossible;
				if (kingdom_res.producers != null)
				{
					for (int i = 0; i < kingdom_res.producers.Count; i++)
					{
						ResourceInfo resourceInfo = kingdom_res.producers[i].castle.GetResourceInfo(kingdom_res.name, create_if_needed: true, calc_if_needed: false);
						CalcResourceAvailabilityInCastle(resourceInfo);
						kingdom_res.AddOption(resourceInfo.availability, resourceInfo.castle);
						kingdom_res.AddOwnOption(resourceInfo.own_availability, resourceInfo.castle);
						if (resourceInfo.availability < availability)
						{
							availability = resourceInfo.availability;
						}
						if (resourceInfo.own_availability < availability2)
						{
							availability2 = resourceInfo.own_availability;
						}
					}
				}
				kingdom_res.availability = availability;
				kingdom_res.own_availability = availability2;
				if (kingdom_res.importer != null)
				{
					kingdom_res.availability = ResourceInfo.Availability.Available;
				}
				else if (GetRealmTag(kingdom_res.name) > 0)
				{
					kingdom_res.availability = ResourceInfo.Availability.Available;
				}
			}
		}
	}

	public void CalcResourceAvailabilityInCastle(ResourceInfo castle_res)
	{
		if (castle_res.availability == ResourceInfo.Availability.Calculating)
		{
			Error($"Infinite loop while calculating availability for {castle_res}");
		}
		else
		{
			if (castle_res.availability != ResourceInfo.Availability.Unknown)
			{
				return;
			}
			if (castle_res.producers != null)
			{
				ResourceInfo.per_castle_recalcs++;
				using (Game.Profile("Kingdom.CalcResourceAvailabilityInCastle"))
				{
					castle_res.availability = ResourceInfo.Availability.Calculating;
					ResourceInfo.Availability availability = ResourceInfo.Availability.Impossible;
					ResourceInfo.Availability availability2 = ResourceInfo.Availability.Impossible;
					for (int i = 0; i < castle_res.producers.Count; i++)
					{
						ResourceInfo.ProducerRef producerRef = castle_res.producers[i];
						if (producerRef.type == "Building" && castle_res.type == "Resource" && producerRef.bdef.ProducesCompleted(castle_res.name))
						{
							CalcBuidingCompleteAvalabilty(castle_res.castle, producerRef.bdef, out var own_avail, out var avail);
							if (own_avail < availability2)
							{
								availability2 = own_avail;
							}
							if (avail < availability)
							{
								availability = avail;
							}
							continue;
						}
						if (producerRef.state >= Building.State.Working)
						{
							castle_res.availability = ResourceInfo.Availability.Available;
							castle_res.own_availability = ResourceInfo.Availability.Available;
							return;
						}
						if (!(producerRef.type != "Building"))
						{
							if (castle_res.name == producerRef.name)
							{
								castle_res.availability = CalcBuidingAvalabilty(castle_res.castle, producerRef.bdef, own: false);
								castle_res.own_availability = CalcBuidingAvalabilty(castle_res.castle, producerRef.bdef, own: true);
								return;
							}
							ResourceInfo resourceInfo = castle_res.castle.GetResourceInfo(producerRef.name);
							if (resourceInfo.availability < availability)
							{
								availability = resourceInfo.availability;
							}
							if (resourceInfo.own_availability < availability2)
							{
								availability2 = resourceInfo.own_availability;
							}
						}
					}
					castle_res.availability = availability;
					castle_res.own_availability = availability2;
					return;
				}
			}
			castle_res.availability = (castle_res.own_availability = ResourceInfo.Availability.Impossible);
		}
	}

	private ResourceInfo.Availability CalcBuidingAvalabilty(Castle castle, Building.Def bdef, bool own)
	{
		ResourceInfo.per_building_recalcs++;
		using (Game.Profile("ResourceInfo.CalcBuidingAvalabilty"))
		{
			ResourceInfo.Availability availability = CalcBuidingPrerequisitesAvalabilty(castle, bdef, own);
			if (bdef.requires != null && bdef.requires.Count > 0)
			{
				for (int i = 0; i < bdef.requires.Count; i++)
				{
					Building.Def.RequirementInfo req = bdef.requires[i];
					ResourceInfo.Availability buildingRequirementAvailability = GetBuildingRequirementAvailability(castle, req, own);
					if (buildingRequirementAvailability > availability)
					{
						availability = buildingRequirementAvailability;
						if (availability >= ResourceInfo.Availability.Impossible)
						{
							break;
						}
					}
				}
				if (availability >= ResourceInfo.Availability.Impossible)
				{
					return ResourceInfo.Availability.Impossible;
				}
			}
			if (bdef.requires_or != null && bdef.requires_or.Count > 0)
			{
				ResourceInfo.Availability availability2 = ResourceInfo.Availability.Impossible;
				for (int j = 0; j < bdef.requires_or.Count; j++)
				{
					Building.Def.RequirementInfo req2 = bdef.requires_or[j];
					ResourceInfo.Availability buildingRequirementAvailability2 = GetBuildingRequirementAvailability(castle, req2, own);
					if (buildingRequirementAvailability2 < availability2)
					{
						availability2 = buildingRequirementAvailability2;
						if (availability2 <= ResourceInfo.Availability.Available)
						{
							break;
						}
					}
				}
				if (availability2 > availability)
				{
					availability = availability2;
					if (availability >= ResourceInfo.Availability.Impossible)
					{
						return ResourceInfo.Availability.Impossible;
					}
				}
			}
			return availability;
		}
	}

	private void CalcBuidingCompleteAvalabilty(Castle castle, Building.Def bdef, out ResourceInfo.Availability own_avail, out ResourceInfo.Availability avail)
	{
		own_avail = CalcBuidingAvalabilty(castle, bdef, own: true);
		avail = CalcBuidingAvalabilty(castle, bdef, own: false);
		Kingdom kingdom = castle?.GetKingdom();
		if (kingdom == null || bdef == null || bdef.upgrades?.buildings == null || (own_avail == ResourceInfo.Availability.Impossible && avail == ResourceInfo.Availability.Impossible))
		{
			return;
		}
		for (int i = 0; i < bdef.upgrades.buildings.Count; i++)
		{
			District.Def.BuildingInfo buildingInfo = bdef.upgrades.buildings[i];
			if (kingdom.CheckReligionRequirements(buildingInfo.def))
			{
				ResourceInfo resourceInfo = kingdom.GetResourceInfo(buildingInfo.id);
				if (resourceInfo.own_availability > own_avail)
				{
					own_avail = resourceInfo.own_availability;
				}
				if (resourceInfo.availability > avail)
				{
					avail = resourceInfo.availability;
				}
				if (own_avail == ResourceInfo.Availability.Impossible && avail == ResourceInfo.Availability.Impossible)
				{
					break;
				}
			}
		}
	}

	private ResourceInfo.Availability CalcBuidingPrerequisitesAvalabilty(Castle castle, Building.Def bdef, bool own)
	{
		if (bdef.districts == null)
		{
			return ResourceInfo.Availability.DirectlyObtainable;
		}
		ResourceInfo.Availability availability = ResourceInfo.Availability.Impossible;
		for (int i = 0; i < bdef.districts.Count; i++)
		{
			District.Def district = bdef.districts[i];
			ResourceInfo.Availability availability2 = CalcBuidingPrerequisitesAvalabilty(castle, bdef, district, own);
			if (availability2 < availability)
			{
				availability = availability2;
				if (availability <= ResourceInfo.Availability.Available)
				{
					break;
				}
			}
		}
		return availability;
	}

	private ResourceInfo.Availability CalcBuidingPrerequisitesAvalabilty(Castle castle, Building.Def bdef, District.Def district, bool own)
	{
		ResourceInfo.Availability availability = ResourceInfo.Availability.DirectlyObtainable;
		Building.Def def = district?.GetParent();
		if (def != null)
		{
			ResourceInfo.Availability availability2 = castle.GetResourceInfo(def.id).GetAvailability(own);
			if (availability2 > availability)
			{
				availability = availability2;
			}
		}
		List<Building.Def> prerequisites = bdef.GetPrerequisites(district);
		if (prerequisites != null && prerequisites.Count > 0)
		{
			for (int i = 0; i < prerequisites.Count; i++)
			{
				Building.Def def2 = prerequisites[i];
				ResourceInfo.Availability availability3 = castle.GetResourceInfo(def2.id).GetAvailability(own);
				if (availability3 > availability)
				{
					availability = availability3;
					if (availability >= ResourceInfo.Availability.Impossible)
					{
						break;
					}
				}
			}
			if (availability >= ResourceInfo.Availability.Impossible)
			{
				return ResourceInfo.Availability.Impossible;
			}
		}
		List<Building.Def> prerequisitesOr = bdef.GetPrerequisitesOr(district);
		if (prerequisitesOr != null && prerequisitesOr.Count > 0)
		{
			ResourceInfo.Availability availability4 = ResourceInfo.Availability.Impossible;
			for (int j = 0; j < prerequisitesOr.Count; j++)
			{
				Building.Def def3 = prerequisitesOr[j];
				ResourceInfo.Availability availability5 = castle.GetResourceInfo(def3.id).GetAvailability(own);
				if (availability5 < availability4)
				{
					availability4 = availability5;
					if (availability4 <= ResourceInfo.Availability.Available)
					{
						break;
					}
				}
			}
			if (availability4 > availability)
			{
				availability = availability4;
				if (availability >= ResourceInfo.Availability.Impossible)
				{
					return ResourceInfo.Availability.Impossible;
				}
			}
		}
		return availability;
	}

	private ResourceInfo.Availability GetBuildingRequirementAvailability(Castle castle, Building.Def.RequirementInfo req, bool own)
	{
		if (req.type == "Citadel" || req.type == "Religion")
		{
			return ResourceInfo.Availability.DirectlyObtainable;
		}
		if (req.type == "Resource")
		{
			ResourceInfo.Availability availability = GetResourceInfo(req.key).GetAvailability(own);
			if (availability == ResourceInfo.Availability.DirectlyObtainable && castle.GetResourceInfo(req.key).GetAvailability(own) != ResourceInfo.Availability.DirectlyObtainable)
			{
				availability = ResourceInfo.Availability.IndirectlyObtainable;
			}
			return availability;
		}
		return castle.GetResourceInfo(req.key).GetAvailability(own);
	}
}
