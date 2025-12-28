using System;
using System.Collections.Generic;
using Logic.ExtensionMethods;

namespace Logic;

public class RoyalFamily : Object
{
	public class Pretender
	{
		public Character pretender;

		public Kingdom loyal_to;

		public bool was_puppet;

		public Pretender(Character pretender, Kingdom loyal_to, bool was_puppet)
		{
			this.pretender = pretender;
			this.loyal_to = loyal_to;
			this.was_puppet = was_puppet;
		}
	}

	public class SovereignUncrownData
	{
		public Castle governed_castle;

		public Kingdom mission_kingdom;

		public int trade_level;

		public string pagan_belief;

		public List<Character.ImportedGood> importing_goods;

		public List<Character> puppets;

		public List<Status> expedition_statuses;
	}

	public enum FamilyRole
	{
		Sovereign,
		Spouse,
		Heir,
		Prince,
		Princess,
		MaleRelative,
		FemaleRelative,
		Pretender,
		Unrelated
	}

	public class Def : Logic.Def
	{
		public string name;

		public int max_relatives = 12;

		public float child_gender_chance_perc = 60f;

		public int max_princes = 3;

		public int max_princesses = 3;

		public int max_children = 4;

		public int[] min_children_per_age;

		public int[] max_children_per_age;

		public int child_max_age_mod = -2;

		public int max_royal_blood_power = 4;

		public int king_royal_blood_power = 4;

		public int prince_royal_blood_power = 3;

		public float royal_blood_weight_base = 5f;

		public float royal_blood_weight_power_add = 5f;

		public float pretender_weight_mod = 1.5f;

		public float prince_specialization_default_skill_weight = 1f;

		public float prince_specialization_father_skill_weight = 3f;

		public float prince_specialization_tradition_skill_weight = 2f;

		public float prince_specialization_primary_skill_weight = 2f;

		public DT.Field elder_prince_rebel_chance;

		public override bool Load(Game game)
		{
			name = dt_def.path;
			DT.Field field = dt_def.field;
			max_princes = field.GetInt("max_princes", null, max_princes);
			max_princesses = field.GetInt("max_princesses", null, max_princesses);
			max_children = field.GetInt("max_children", null, max_children);
			min_children_per_age = new int[7];
			max_children_per_age = new int[7];
			DT.Field field2 = field.FindChild("child_per_age");
			if (field2 != null)
			{
				for (int i = 0; i < 7; i++)
				{
					Character.Age age = (Character.Age)i;
					int num = field2.GetInt(string.Concat(age, ".min"));
					int num2 = field2.GetInt(string.Concat(age, ".max"));
					min_children_per_age[i] = num;
					max_children_per_age[i] = num2;
				}
			}
			child_max_age_mod = field.GetInt("child_max_age_mod", null, child_max_age_mod);
			king_royal_blood_power = field.GetInt("king_royal_blood_power", null, king_royal_blood_power);
			max_royal_blood_power = king_royal_blood_power;
			prince_royal_blood_power = field.GetInt("prince_royal_blood_power", null, prince_royal_blood_power);
			royal_blood_weight_base = field.GetFloat("royal_blood_weight_base", null, royal_blood_weight_base);
			royal_blood_weight_power_add = field.GetFloat("royal_blood_weight_power_add", null, royal_blood_weight_power_add);
			pretender_weight_mod = field.GetFloat("pretender_weight_mod", null, pretender_weight_mod);
			max_relatives = field.GetInt("max_relatives", null, max_relatives);
			child_gender_chance_perc = field.GetFloat("child_gender_chance_perc", null, child_gender_chance_perc);
			prince_specialization_default_skill_weight = field.GetFloat("prince_specialization_default_skill_weight", null, prince_specialization_default_skill_weight);
			prince_specialization_father_skill_weight = field.GetFloat("prince_specialization_father_skill_weight", null, prince_specialization_father_skill_weight);
			prince_specialization_tradition_skill_weight = field.GetFloat("prince_specialization_tradition_skill_weight", null, prince_specialization_tradition_skill_weight);
			prince_specialization_primary_skill_weight = field.GetFloat("prince_specialization_primary_skill_weight", null, prince_specialization_primary_skill_weight);
			elder_prince_rebel_chance = field.FindChild("elder_prince_rebel_chance");
			return true;
		}
	}

	private Random rnd = new Random();

	public Def def;

	public int kingdom_id;

	public Character Sovereign;

	public Character Spouse;

	public Character SpouseFacination;

	public Character Heir;

	public Pretender crownPretender;

	public List<Character> Children = new List<Character>();

	public List<Character> Relatives = new List<Character>();

	public Dictionary<string, int> used_names = new Dictionary<string, int>();

	private SovereignUncrownData sovereignUncrownData;

	public int MaxChildren()
	{
		return def.max_children;
	}

	public RoyalFamily(Kingdom kingdom)
		: base(kingdom.game)
	{
		kingdom_id = kingdom.id;
		def = game.defs.GetBase<Def>();
	}

	public override Kingdom GetKingdom()
	{
		return game.GetKingdom(kingdom_id);
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (IsAuthority() && def != null)
		{
			Kingdom kingdom = GetKingdom();
			if (kingdom != null && Timer.Find(kingdom, "check_marrige") == null)
			{
				Timer.Start(kingdom, "check_marrige", (float)game.Random(10, 35) + GetInitialMarriageCooldown());
			}
		}
	}

	public void CheckForMarrage()
	{
		if (def != null && Sovereign != null && Sovereign.IsAlive() && Sovereign.CanMarry() && Sovereign.prison_kingdom == null && Sovereign.GetArmy()?.battle == null && Sovereign.GetKingdom() != Sovereign.game?.religions?.catholic.hq_kingdom && Sovereign != Sovereign.game?.religions?.catholic?.crusade?.leader && GetKingdom().court.Find((Character c) => c?.FindStatus<SearchingForSpouseStatus>()?.character == Sovereign) == null && (Spouse == null || Spouse.IsDead()) && game.Random() * 100.0 < (double)GetMarriageChance(Sovereign))
		{
			CharacterFactory.CreateQueen(GetKingdom(), Sovereign);
		}
	}

	public int NumPrinces()
	{
		int num = 0;
		for (int i = 0; i < Children.Count; i++)
		{
			if (Children[i].IsPrince())
			{
				num++;
			}
		}
		return num;
	}

	public int NumPrincesses()
	{
		return Children.Count - NumPrinces();
	}

	public bool CheckForNewBorn(bool checkForTwins = true)
	{
		if (def == null)
		{
			return false;
		}
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || !kingdom.IsValid())
		{
			return false;
		}
		if (Sovereign == null)
		{
			return false;
		}
		if (Sovereign.prison_kingdom != null)
		{
			return false;
		}
		if (Spouse == null)
		{
			return false;
		}
		if (Children.Count >= MaxChildren())
		{
			return false;
		}
		if (rnd.NextDouble() * 100.0 < (double)GetChildBirthChance())
		{
			Character character = null;
			if (Spouse != null && Spouse.age < Character.Age.Old)
			{
				character = Spouse;
			}
			if (character == null)
			{
				return false;
			}
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].sex == Character.Sex.Male)
				{
					num2++;
				}
				else
				{
					num++;
				}
			}
			Character.Sex sex = ((!(rnd.NextDouble() * 100.0 < (double)def.child_gender_chance_perc)) ? Character.Sex.Female : Character.Sex.Male);
			if (sex == Character.Sex.Male && num2 >= def.max_princes)
			{
				sex = Character.Sex.Female;
			}
			else if (sex == Character.Sex.Female && num >= def.max_princesses)
			{
				sex = Character.Sex.Male;
			}
			Character character2 = ((sex == Character.Sex.Male) ? CharacterFactory.CreatePrince(kingdom, Sovereign, character) : CharacterFactory.CreatePrincess(kingdom, Sovereign, character));
			if (!AddChild(character2))
			{
				character2.Destroy();
				return false;
			}
			if (checkForTwins)
			{
				CheckForNewBorn(checkForTwins: false);
			}
			return true;
		}
		return false;
	}

	public void SetHeir(Character heir, bool send_state = true, bool from_state = false)
	{
		if (heir != null && !from_state && (heir.sex == Character.Sex.Female || heir.title != "Prince" || Sovereign == heir))
		{
			return;
		}
		if (!IsAuthority() && send_state)
		{
			GetKingdom().SendEvent(new Kingdom.SetHeirEvent(heir));
			return;
		}
		Heir?.NotifyListeners("title_changed");
		Heir = heir;
		Heir?.NotifyListeners("title_changed");
		NotifyListeners("royal_new_heir", heir);
		if (send_state)
		{
			GetKingdom().SendState<Kingdom.RoyalFamilyState>();
			OnFamilyChangedAnalytics(heir, "new_heir");
		}
	}

	public bool CanBeHair(Character c, bool ignore_age = false)
	{
		if (c == null)
		{
			return false;
		}
		if (!c.IsAlive())
		{
			return false;
		}
		if (c.sex != Character.Sex.Male)
		{
			return false;
		}
		if (c.age < Character.Age.Juvenile && !ignore_age)
		{
			return false;
		}
		if (c.IsRebel())
		{
			return false;
		}
		if (c.IsPatriarch())
		{
			return false;
		}
		if (c.IsPope())
		{
			return false;
		}
		return true;
	}

	public void SetPretender(Character c, Kingdom loyal_to, bool was_puppet, bool send_state = true)
	{
		if (c != null && GetCrownPretender() != null && GetCrownPretenderKingdomLoyalTo() != loyal_to)
		{
			GetCrownPretenderKingdomLoyalTo()?.NotifyListeners("pretender_replaced_by_enemy", GetCrownPretender());
		}
		crownPretender = new Pretender(c, loyal_to, was_puppet);
		if (send_state)
		{
			GetKingdom().SendState<Kingdom.PretenderState>();
		}
	}

	public float GetRoyalBloodWeight(Character c)
	{
		float num = (def.royal_blood_weight_base + (float)c.GetClassLevel()) * ((float)c.royalBloodPower + def.royal_blood_weight_power_add);
		if (c == GetCrownPretender())
		{
			num *= def.pretender_weight_mod;
		}
		return num;
	}

	public Character GetCrownPretender()
	{
		if (crownPretender == null || crownPretender.pretender == null || !crownPretender.pretender.IsAlive())
		{
			return null;
		}
		return crownPretender.pretender;
	}

	public Kingdom GetCrownPretenderKingdomLoyalTo()
	{
		if (crownPretender == null || crownPretender.pretender == null || !crownPretender.pretender.IsAlive())
		{
			return null;
		}
		return crownPretender.loyal_to;
	}

	private bool CanBecomeSovereign(Character c)
	{
		if (c == null)
		{
			return false;
		}
		if (c.age < Character.Age.Juvenile)
		{
			return false;
		}
		if (c.kingdom_id != kingdom_id)
		{
			return false;
		}
		if (!c.IsAlive())
		{
			return false;
		}
		if (c.IsKing())
		{
			return false;
		}
		if (c.IsPope())
		{
			return false;
		}
		if (c.IsRebel())
		{
			return false;
		}
		if (c.IsPatriarch())
		{
			return false;
		}
		return true;
	}

	private bool HaveSovereignCandidatesNotInPrison()
	{
		Kingdom kingdom = GetKingdom();
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (CanBecomeSovereign(character) && !character.IsPrisoner())
			{
				return true;
			}
		}
		return false;
	}

	public SovereignUncrownData GetSovereignUncrownData()
	{
		if (Sovereign == null)
		{
			return sovereignUncrownData;
		}
		if (sovereignUncrownData == null)
		{
			sovereignUncrownData = new SovereignUncrownData();
		}
		sovereignUncrownData.governed_castle = Sovereign.governed_castle;
		sovereignUncrownData.mission_kingdom = Sovereign.mission_kingdom;
		sovereignUncrownData.trade_level = Sovereign.trade_level;
		sovereignUncrownData.pagan_belief = Sovereign.paganBelief?.name;
		sovereignUncrownData.importing_goods?.Clear();
		if (Sovereign.importing_goods != null)
		{
			if (sovereignUncrownData.importing_goods == null)
			{
				sovereignUncrownData.importing_goods = new List<Character.ImportedGood>();
			}
			sovereignUncrownData.importing_goods.AddRange(Sovereign.importing_goods);
		}
		sovereignUncrownData.puppets?.Clear();
		if (Sovereign.puppets != null)
		{
			if (sovereignUncrownData.puppets == null)
			{
				sovereignUncrownData.puppets = new List<Character>();
			}
			sovereignUncrownData.puppets.AddRange(Sovereign.puppets);
		}
		sovereignUncrownData.expedition_statuses?.Clear();
		if (Sovereign.FindStatus<TradeExpeditionStatus>() != null)
		{
			if (sovereignUncrownData.expedition_statuses == null)
			{
				sovereignUncrownData.expedition_statuses = new List<Status>();
			}
			Status status;
			while ((status = Sovereign.FindStatus<TradeExpeditionStatus>()) != null)
			{
				Sovereign.DelStatus(status, destroy: false);
				sovereignUncrownData.expedition_statuses.Add(status);
			}
		}
		return sovereignUncrownData;
	}

	private void TryRebelElderBrothers()
	{
		for (int i = 0; i < Children.Count; i++)
		{
			Character character = Children[i];
			if (character != null && character.CanBeHeir() && character.age >= Heir.age && (character.age != Heir.age || !(character.next_age_check >= Heir.next_age_check)))
			{
				float num = def.elder_prince_rebel_chance.Value(character).Float();
				if ((float)game.Random(0, 100) < num)
				{
					character.TurnIntoRebel("GeneralRebels");
					Vars vars = new Vars();
					vars.Set("prince", character);
					vars.Set("heir", Heir);
					GetKingdom().FireEvent("elder_prince_rebelled", vars);
					OnFamilyChangedAnalytics(character, "prince_rebelled");
				}
			}
		}
	}

	public void Uncrown(string reason, SovereignUncrownData sovereignUncrownData, Army old_army = null)
	{
		Kingdom kingdom = GetKingdom();
		kingdom.StartRecheckKingTimer();
		if (kingdom.IsDefeated())
		{
			return;
		}
		KingdomAndKingdomRelation.FadeWithNewKing(kingdom);
		if (kingdom.stats != null)
		{
			kingdom.stats.AddModifier("ks_king_death", new FadingModifier(game, game.defs.Get<FadingModifier.Def>("KingDeathModifier"), kingdom, kingdom));
		}
		if (kingdom.IsPapacy())
		{
			return;
		}
		if (Heir != null && Heir.IsValid() && CanBecomeSovereign(Heir))
		{
			TryRebelElderBrothers();
			ChangeSovereign(Heir, sovereignUncrownData, "inherited", reason, old_army);
			Sovereign?.TryRevealMasters("PrinceBecomesKingRevealAction");
		}
		else
		{
			Character character = null;
			float num = 0f;
			bool flag = HaveSovereignCandidatesNotInPrison();
			bool flag2 = false;
			for (int i = 0; i < Relatives.Count; i++)
			{
				Character character2 = Relatives[i];
				if (CanBecomeSovereign(character2) && !(character2.IsPrisoner() && flag))
				{
					num += GetRoyalBloodWeight(character2);
					flag2 = true;
				}
			}
			if (!flag2)
			{
				for (int j = 0; j < kingdom.court.Count; j++)
				{
					Character character3 = kingdom.court[j];
					if (CanBecomeSovereign(character3) && !(character3.IsPrisoner() && flag))
					{
						num += GetRoyalBloodWeight(character3);
					}
				}
			}
			float num2 = game.Random(0f, num);
			if (!flag2)
			{
				for (int k = 0; k < kingdom.court.Count; k++)
				{
					Character character4 = kingdom.court[k];
					if (CanBecomeSovereign(character4) && !(character4.IsPrisoner() && flag))
					{
						num2 -= GetRoyalBloodWeight(character4);
						if (num2 <= 0f)
						{
							character = character4;
							break;
						}
					}
				}
			}
			if (character == null)
			{
				for (int l = 0; l < Relatives.Count; l++)
				{
					Character character5 = Relatives[l];
					if (CanBecomeSovereign(character5) && !(character5.IsPrisoner() && flag))
					{
						num2 -= GetRoyalBloodWeight(character5);
						if (num2 <= 0f)
						{
							character = character5;
							break;
						}
					}
				}
			}
			if (character != null && character.royalBloodPower > 0)
			{
				ChangeSovereign(character, sovereignUncrownData, "inherited", reason);
				RemoveRelative(character);
			}
			else
			{
				Character character6 = GetCrownPretender();
				if (character != null && character6 != null && character == character6)
				{
					PuppetPretenderTakeCrownAction obj = character.actions.Find("PuppetPretenderTakeCrownAction") as PuppetPretenderTakeCrownAction;
					obj.pretender = crownPretender;
					obj.Execute(null);
				}
				if (character == null)
				{
					character = CharacterFactory.CreateKing(GetKingdom());
					character.Start();
					character.SetStatus<AvailableForAssignmentStatus>();
				}
				kingdom.SplitKingdom();
				ChangeSovereign(character, sovereignUncrownData, "new_dynasty", reason);
				ClearRelatives();
				kingdom.GetCrownAuthority()?.AddModifier("noSuccessor");
			}
		}
		for (int m = 0; m < Relatives.Count; m++)
		{
			Character character7 = Relatives[m];
			if (character7 != null && character7.IsValid() && !character7.IsKing() && !character7.IsDead())
			{
				character7.SetRoyalBloodPower(character7.royalBloodPower - 1);
				if (character7.royalBloodPower <= 0)
				{
					RemoveRelative(character7);
					m--;
				}
			}
		}
		SetPretender(null, null, was_puppet: false);
		kingdom.ClearRevealedSpies();
		kingdom.GetCrownAuthority()?.AddModifier("kingDeath");
		for (int n = 0; n < kingdom.armies.Count; n++)
		{
			Army army = kingdom.armies[n];
			army.morale.AddTemporaryMorale(army.morale.def.morale_king_died);
		}
	}

	public void Unheir()
	{
		if (Heir == null)
		{
			return;
		}
		Character heir = Heir;
		if (Children != null && Children.Count > 0)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				Character character = Children[i];
				if (character != heir && CanBeHair(character, ignore_age: true))
				{
					SetHeir(character);
					return;
				}
			}
		}
		SetHeir(null);
	}

	private List<Kingdom> TryBreakNonAgressions()
	{
		Kingdom kingdom = GetKingdom();
		List<Kingdom> list = null;
		List<Kingdom> list2 = new List<Kingdom>(kingdom.nonAgressions);
		for (int i = 0; i < list2.Count; i++)
		{
			Kingdom kingdom2 = list2[i];
			if ((list != null && list.Contains(kingdom2)) || !KingdomAndKingdomRelation.ShouldUnsetNonAgression(kingdom, kingdom2))
			{
				continue;
			}
			if (list == null)
			{
				list = new List<Kingdom>();
			}
			list.Add(kingdom2);
			kingdom.UnsetStance(kingdom2, RelationUtils.Stance.NonAggression);
			KingdomAndKingdomRelation.SetNonAgressionBrokenKingDeath(kingdom, kingdom2);
			if (!kingdom.is_player)
			{
				Offer cachedOffer = Offer.GetCachedOffer("SignNonAggressionRenewOurKing", kingdom, kingdom2);
				cachedOffer.AI = true;
				bool flag = true;
				if (cachedOffer.Validate() == "ok" && cachedOffer.CheckThreshold("propose") && cachedOffer.Send() && cachedOffer.answer == "accept")
				{
					flag = false;
				}
				if (flag)
				{
					kingdom2.FireEvent("non_agression_ended", kingdom, kingdom2.id);
				}
			}
			else if (!kingdom2.is_player)
			{
				Offer cachedOffer2 = Offer.GetCachedOffer("SignNonAggressionRenewTheirKing", kingdom2, kingdom);
				cachedOffer2.AI = true;
				if (cachedOffer2.Validate() == "ok" && cachedOffer2.CheckThreshold("propose"))
				{
					cachedOffer2.Send();
				}
			}
			else
			{
				kingdom2.FireEvent("non_agression_ended", kingdom, kingdom2.id);
			}
			i--;
		}
		return list;
	}

	private List<Kingdom> BreakRoyalMarriages()
	{
		Kingdom kingdom = GetKingdom();
		List<Kingdom> list = null;
		List<Marriage> list2 = new List<Marriage>(kingdom.marriages);
		for (int i = 0; i < list2.Count; i++)
		{
			Marriage marriage = list2[i];
			Kingdom otherKingdom = marriage.GetOtherKingdom(kingdom);
			if ((list == null || !list.Contains(otherKingdom)) && marriage.husband != Sovereign && kingdom.GetStance(otherKingdom).IsMarriage())
			{
				kingdom.UnsetStance(otherKingdom, RelationUtils.Stance.Marriage);
				if (list == null)
				{
					list = new List<Kingdom>();
				}
				list.Add(otherKingdom);
				otherKingdom.FireEvent("royal_ties_broken", kingdom, otherKingdom.id);
			}
		}
		return list;
	}

	public void ChangeSovereign(Character newSovereign, SovereignUncrownData oldSovereignData, string change_type = "", string abdication_reason = "", Army old_army = null)
	{
		if (!AssertAuthority())
		{
			return;
		}
		Kingdom kingdom = GetKingdom();
		kingdom.StartRecheckKingTimer();
		bool flag = false;
		if (Heir == newSovereign)
		{
			flag = true;
		}
		if (Sovereign == newSovereign)
		{
			return;
		}
		bool flag2 = Relatives.Contains(newSovereign);
		bool flag3 = newSovereign.IsCardinal();
		if (flag3)
		{
			newSovereign.game.religions.catholic.DelCardinal(newSovereign, null);
		}
		if (newSovereign.sex == Character.Sex.Female)
		{
			game.Warning("Female king at " + kingdom);
		}
		kingdom.favoriteDiplomat = null;
		SpouseFacination = null;
		Character sovereign = Sovereign;
		string text = Sovereign?.GetTitle();
		Sovereign = newSovereign;
		Sovereign.BecomeKingAgeAdjust();
		Sovereign.SetTitle("King");
		Sovereign.SetRoyalBloodPower(def.king_royal_blood_power);
		Sovereign.AddStatus("HasRoyalBloodStatus");
		Sovereign.AddStatus<HasRoyalBloodStatus>();
		UpdateSovereignNameIndex();
		Sovereign.RefreshTags();
		Sovereign.GetRoyalAbilities();
		Spouse = null;
		Marriage marriage = newSovereign.GetMarriage();
		bool flag4 = false;
		if (marriage != null)
		{
			SetSpouse(marriage.wife, setTitle: true);
			Kingdom originalKingdom = marriage.wife.GetOriginalKingdom();
			if (!kingdom.GetRoyalMarriage(originalKingdom) && marriage.wife.IsRoyalChild())
			{
				flag4 = true;
				kingdom.SetStance(originalKingdom, RelationUtils.Stance.Marriage);
			}
			Vars vars = new Vars();
			vars.Set("royal_ties_restored", flag4);
			vars.Set("wife", marriage.wife);
			marriage.kingdom_wife.NotifyListeners("princess_becomes_queen", vars);
		}
		Timer.Start(kingdom, "check_marrige", GetInitialMarriageCooldown(), restart: true);
		if (!flag2)
		{
			ClearRelatives();
		}
		Inheritance component = kingdom.GetComponent<Inheritance>();
		for (int i = 0; i < Children.Count; i++)
		{
			Character character = Children[i];
			if (character == newSovereign)
			{
				continue;
			}
			character.OnSovereignChanged(newSovereign);
			if (character.title == "Prince" || character.title == "Princess")
			{
				character.SetTitle((character.sex == Character.Sex.Male) ? "Knight" : "Lady");
			}
			if (character.IsInCourt())
			{
				character.EnableAging(game.rules.KnightAging() || character.ShouldBeAging());
			}
			if (character.sex == Character.Sex.Female)
			{
				Kingdom kingdom2 = character.GetMarriage()?.kingdom_husband;
				RelationUtils.Stance stance = kingdom.GetStance(kingdom2);
				if (kingdom2 != null)
				{
					if (stance.IsMarriage() && !stance.IsWar())
					{
						component?.AddInheriancePrincess(character);
					}
				}
				else
				{
					RemoveChild(character);
					i--;
					character.Die();
				}
			}
			else if (!flag2)
			{
				AddRelative(character, send_state: false);
			}
			character.FireEvent("no_longer_in_royal_family", null);
		}
		if (component?.currentPrincess == null)
		{
			component?.HandleNextPrincess();
		}
		Children.Clear();
		Heir = null;
		CharacterFactory.PopulateRoyalFamily(Sovereign);
		if (kingdom.IsCourtMember(Sovereign))
		{
			kingdom.DelCourtMember(Sovereign, send_state: true, kill_or_throneroom: false);
		}
		kingdom.AddCourtMember(Sovereign);
		if (!Sovereign.IsGovernor())
		{
			if (oldSovereignData?.governed_castle != null && oldSovereignData.governed_castle.governor == null)
			{
				if (Sovereign.IsPrepairngToGovern(out var a))
				{
					a?.Cancel();
				}
				Sovereign.Govern(oldSovereignData.governed_castle);
			}
			else if (change_type == "new_dynasty" || change_type == "independence")
			{
				Realm capital = kingdom.GetCapital();
				if (capital?.castle != null && capital.castle.governor == null)
				{
					if (Sovereign.IsPrepairngToGovern(out var a2))
					{
						a2?.Cancel();
					}
					Sovereign.Govern(capital.castle);
				}
			}
		}
		if (oldSovereignData != null && oldSovereignData.pagan_belief != null && sovereign != null && sovereign.IsCleric())
		{
			bool flag5 = false;
			if (flag && Sovereign.IsCleric() && !Sovereign.IsPrisoner() && Sovereign.paganBelief == null)
			{
				Sovereign.PromotePaganBelief(oldSovereignData.pagan_belief, apply_penalties: false, notify: false);
				flag5 = true;
			}
			if (!flag5)
			{
				sovereign.FireEvent("stopped_promoting_pagan_belief_no_penalty", oldSovereignData.pagan_belief);
			}
		}
		if (sovereign != null && sovereign.IsMerchant())
		{
			if (oldSovereignData?.mission_kingdom != null)
			{
				if (flag && Sovereign.IsMerchant() && Sovereign.mission_kingdom == null && Sovereign.GetArmy() == null && !Sovereign.IsPrisoner())
				{
					Sovereign.SetMissionKingdom(oldSovereignData.mission_kingdom);
					Sovereign.SetTradeLevel(oldSovereignData.trade_level);
					Sovereign.SetDefaultStatus<TradingWithKingdomStatus>();
					Sovereign.GetKingdom()?.InvalidateIncomes();
					if (oldSovereignData.importing_goods != null)
					{
						for (int j = 0; j < oldSovereignData.importing_goods.Count; j++)
						{
							Character.ImportedGood importedGood = oldSovereignData.importing_goods[j];
							Sovereign.ImportGood(importedGood.name, j, importedGood.discount);
						}
					}
				}
				else
				{
					Vars vars2 = new Vars();
					vars2.Set("trade_level", oldSovereignData.trade_level);
					vars2.Set("mission_kingdom", oldSovereignData.mission_kingdom);
					kingdom.NotifyListeners("trade_stopped", vars2);
				}
			}
			if (flag && abdication_reason != "killed_in_trade_colony" && sovereignUncrownData.expedition_statuses != null && sovereignUncrownData.expedition_statuses.Count > 0)
			{
				Action action = Sovereign.FindAction("TradeExpeditionAction");
				if (action != null)
				{
					int num = action.def.field.GetInt("max_expeditions");
					int num2 = 0;
					for (int k = 0; k < Sovereign.statuses.Count; k++)
					{
						if (Sovereign.statuses[k] is TradeExpeditionStatus)
						{
							num2++;
						}
					}
					foreach (Status expedition_status in sovereignUncrownData.expedition_statuses)
					{
						if (num2 >= num)
						{
							break;
						}
						num2++;
						Sovereign.AddStatus(expedition_status);
					}
				}
			}
		}
		sovereignUncrownData?.expedition_statuses?.Clear();
		if (sovereign != null && sovereign.IsSpy() && oldSovereignData?.mission_kingdom != null && flag && Sovereign.IsSpy() && Sovereign.mission_kingdom == null && Sovereign.GetArmy() == null && !Sovereign.IsPrisoner())
		{
			Sovereign.SetMissionKingdom(oldSovereignData.mission_kingdom);
			if (oldSovereignData.puppets != null)
			{
				for (int l = 0; l < oldSovereignData.puppets.Count; l++)
				{
					Character puppet = oldSovereignData.puppets[l];
					Sovereign.AddPuppet(puppet);
				}
			}
		}
		if (sovereign != null && old_army != null && flag && Sovereign.CanLeadArmy() && Sovereign.GetArmy() == null && !sovereign.IsCrusader() && !Sovereign.IsPrisoner() && Sovereign.IsIdle() && Sovereign.mission_kingdom == null)
		{
			old_army.SetLeader(Sovereign);
		}
		List<Kingdom> list = TryBreakNonAgressions();
		List<Kingdom> list2 = BreakRoyalMarriages();
		if (flag3)
		{
			Sovereign.FireEvent("no_longer_cardinal", "king");
		}
		Vars vars3 = new Vars(Sovereign);
		vars3.Set("isHeir", flag);
		vars3.Set("change_type", change_type);
		vars3.Set("abdication_reason", abdication_reason);
		vars3.Set("old_sovereign", sovereign);
		if (list != null)
		{
			vars3.Set("nonAgressionsBroken", list);
		}
		if (list2 != null)
		{
			vars3.Set("royalMarriagesBroken", list2);
		}
		if (flag4)
		{
			vars3.Set("royalTiesRestoredKingdom", marriage.wife.GetOriginalKingdom());
		}
		kingdom.FireEvent("royal_new_sovereign", vars3);
		Sovereign.NotifyListeners("became_king");
		Vars vars4 = new Vars();
		if (sovereign != null)
		{
			vars4.Set("is_old_king_death", sovereign.IsDead() || !sovereign.IsValid());
		}
		vars4.Set("change_type", change_type);
		kingdom.NotifyListeners("king_changed", vars4);
		if (kingdom.game.real_time_total.seconds >= 0f)
		{
			kingdom.generationsPassed++;
			kingdom.FireEvent("generations_changed", abdication_reason);
		}
		kingdom.SendState<Kingdom.RoyalFamilyState>();
		kingdom.SendState<Kingdom.UsedNamesState>();
		OnFamilyChangedAnalytics(Sovereign, "sovereign_changed");
		if (sovereign != null && GetKingdom().type == Kingdom.Type.Regular && change_type != "independence" && change_type != "new_pope")
		{
			Vars vars5 = new Vars();
			vars5.SetVar("kingdom_a", kingdom);
			vars5.SetVar("old_king_title", text);
			game.BroadcastRadioEvent("ForeignNewSovereignMessage", vars5);
		}
		newSovereign.ForceSkillsToMaxRank();
		if (old_army != null && old_army.IsValid() && old_army.leader == newSovereign)
		{
			old_army.CheckSiegeEquipmentCapacity();
		}
		if (newSovereign.masters == null)
		{
			return;
		}
		foreach (Character master in newSovereign.masters)
		{
			master.FireEvent("puppet_became_king", null);
		}
	}

	public void SetSovereign(Character newSovereign, bool send_state = true)
	{
		Kingdom kingdom = GetKingdom();
		kingdom.StartRecheckKingTimer();
		if (Sovereign == newSovereign)
		{
			return;
		}
		Sovereign = newSovereign;
		if (!kingdom.IsDefeated() && Sovereign == null)
		{
			if (IsAuthority())
			{
				Warning($"Setting null King to {kingdom}!");
			}
			return;
		}
		Sovereign.StopGoverning();
		Sovereign.BecomeKingAgeAdjust();
		Sovereign.SetTitle("King");
		if (IsAuthority())
		{
			Sovereign.AddStatus<HasRoyalBloodStatus>();
		}
		UpdateSovereignNameIndex();
		Sovereign.RefreshTags();
		Sovereign.GetRoyalAbilities();
		Character spouse = newSovereign.GetSpouse();
		if (spouse != null)
		{
			Spouse = spouse;
			Spouse.SetTitle("Lady");
			UpdateQueenNameIndex();
			Spouse.EnableAging(can_die: true);
		}
		kingdom.AddCourtMember(Sovereign);
		if (send_state)
		{
			kingdom.SendState<Kingdom.RoyalFamilyState>();
			kingdom.SendState<Kingdom.UsedNamesState>();
			OnFamilyChangedAnalytics(newSovereign, "new_sovereign");
		}
		else
		{
			NotifyListeners("royal_new_sovereign");
			Sovereign.NotifyListeners("init_king");
		}
	}

	private void UpdateSovereignNameIndex()
	{
		if (IsAuthority())
		{
			string name = Sovereign.GetName();
			used_names.TryGetValue(name, out var value);
			value = ((Sovereign.name_idx <= 0) ? (value + 1) : Sovereign.name_idx);
			used_names[name] = Math.Max(value, Sovereign.name_idx);
			if (value > 1 && Sovereign.name_idx == 0)
			{
				Sovereign.name_idx = value;
				Sovereign.SendState<Character.NameState>();
			}
		}
	}

	private void UpdateQueenNameIndex()
	{
		if (IsAuthority() && Spouse != null)
		{
			string name = Spouse.GetName();
			used_names.TryGetValue(name, out var value);
			value = ((Spouse.name_idx <= 0) ? (value + 1) : Spouse.name_idx);
			used_names[name] = Math.Max(value, Spouse.name_idx);
			if (value > 1 && Spouse.name_idx == 0)
			{
				Spouse.name_idx = value;
				Spouse.SendState<Character.NameState>();
			}
		}
	}

	public FamilyRole GetRole(Character c)
	{
		if (c == Sovereign)
		{
			return FamilyRole.Sovereign;
		}
		if (c == Spouse)
		{
			return FamilyRole.Spouse;
		}
		if (c == Heir)
		{
			return FamilyRole.Heir;
		}
		if (Children.Contains(c))
		{
			if (c.sex == Character.Sex.Male)
			{
				return FamilyRole.Prince;
			}
			return FamilyRole.Princess;
		}
		if (Relatives.Contains(c))
		{
			if (c.sex == Character.Sex.Male)
			{
				return FamilyRole.MaleRelative;
			}
			return FamilyRole.FemaleRelative;
		}
		if (crownPretender?.pretender == c)
		{
			return FamilyRole.Pretender;
		}
		return FamilyRole.Unrelated;
	}

	public void OnFamilyChangedAnalytics(Character c, string familyEvent)
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom != null && kingdom.is_player && c != null && IsAuthority() && game.IsRunning() && GetRole(c) != FamilyRole.Unrelated)
		{
			Vars vars = new Vars();
			vars.Set("characterName", c.Name);
			vars.Set("relationGender", c.sex.ToString());
			vars.Set("relationPosition", GetRole(c).ToString());
			vars.Set("relationAge", (int)c.age);
			vars.Set("familyEvent", familyEvent);
			kingdom.FireEvent("analytics_royal_family_change", vars, kingdom.id);
		}
	}

	public bool AddChild(Character child, bool send_state = true, bool fire_event = true)
	{
		if (child == null)
		{
			return false;
		}
		if (Children.Count >= def.max_princes + def.max_princesses)
		{
			return false;
		}
		child.EnableAging(can_die: true);
		Children.Add(child);
		child.prefered_class_def = ((Sovereign != null) ? Sovereign.class_def : null);
		Kingdom kingdom = GetKingdom();
		if (Heir == null && child.sex == Character.Sex.Male)
		{
			SetHeir(child);
		}
		if (child.sex == Character.Sex.Male)
		{
			child.SetRoyalBloodPower(def.prince_royal_blood_power);
		}
		child.GetRoyalAbilities();
		child.AddStatus<HasRoyalBloodStatus>();
		if (send_state)
		{
			OnFamilyChangedAnalytics(child, "new_child");
			kingdom.SendSubstate<Kingdom.RoyalFamilyState.ChildrenState>(1);
		}
		kingdom.SendSubstate<Kingdom.RoyalFamilyState.ChildrenState>(1);
		if (fire_event && kingdom.ai.Enabled(KingdomAI.EnableFlags.Characters))
		{
			fire_event = false;
			if (child.sex == Character.Sex.Male && (child.class_def == null || child.class_def.IsBase()))
			{
				if (child.IsHeir())
				{
					child.SetClass(kingdom.royalFamily.Sovereign.class_def);
				}
				else
				{
					child.SetClass(game.defs.GetRandom<CharacterClass.Def>());
				}
			}
		}
		if (fire_event)
		{
			kingdom.FireEvent("royal_new_born", child);
		}
		return true;
	}

	public void RemoveChild(Character child, bool send_state = true)
	{
		if (child != null)
		{
			if (Children.Remove(child))
			{
				NotifyListeners("royal_child_remove", child);
			}
			Kingdom kingdom = GetKingdom();
			if (send_state)
			{
				OnFamilyChangedAnalytics(child, "child_removed");
				kingdom.SendSubstate<Kingdom.RoyalFamilyState.ChildrenState>(1);
			}
		}
	}

	public void SetSpouse(Character spouse, bool setTitle = false, bool send_state = true)
	{
		Character spouse2 = Spouse;
		Spouse = spouse;
		if (setTitle)
		{
			Spouse.SetTitle("Queen");
		}
		UpdateQueenNameIndex();
		Kingdom kingdom = GetKingdom();
		if (spouse2 != Spouse)
		{
			kingdom.NotifyListeners("new_queen");
		}
		if (send_state)
		{
			OnFamilyChangedAnalytics(spouse, "new_spouse");
			kingdom.SendState<Kingdom.RoyalFamilyState>();
			kingdom.SendState<Kingdom.UsedNamesState>();
		}
	}

	public void SetSpouseFacination(Character spouseFacination, bool send_state = true)
	{
		SpouseFacination = spouseFacination;
		NotifyListeners("royal_new_spouseFacination", SpouseFacination);
		if (send_state)
		{
			GetKingdom().SendState<Kingdom.RoyalFamilyState>();
			OnFamilyChangedAnalytics(spouseFacination, "spouse_fascination");
		}
	}

	public void RemoveSpouse(Character spouse, bool send_state = true)
	{
		if (spouse != null)
		{
			Spouse = null;
			NotifyListeners("royal_remove_spouse", spouse);
			if (send_state)
			{
				GetKingdom().SendState<Kingdom.RoyalFamilyState>();
			}
		}
	}

	public void ClearRelatives(bool send_state = true)
	{
		while (Relatives.Count > 0)
		{
			RemoveRelative(Relatives[0], send_state: false);
		}
		Relatives.Clear();
		if (send_state)
		{
			GetKingdom().SendState<Kingdom.RoyalFamilyState>();
		}
	}

	public void ClearFamily(bool send_state = true)
	{
		if (Sovereign != null && Sovereign.IsAlive())
		{
			Sovereign.Die();
		}
		Sovereign = null;
		Spouse = null;
		for (int num = Children.Count - 1; num >= 0; num--)
		{
			Character character = Children[num];
			if (character.IsPrince() || !character.IsMarried())
			{
				character.Die();
			}
		}
		Children.Clear();
		ClearRelatives(send_state: false);
		if (send_state)
		{
			GetKingdom().SendState<Kingdom.RoyalFamilyState>();
		}
	}

	public bool IsFamilyMember(Character c)
	{
		if (c == null)
		{
			return false;
		}
		if (c == Sovereign)
		{
			return true;
		}
		if (c == Spouse)
		{
			return true;
		}
		if (Children != null && Children.Count > 0)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i] == c)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool RemoveFamilyMember(Character c)
	{
		if (c == null)
		{
			return false;
		}
		if (c == Sovereign)
		{
			return false;
		}
		if (c == Spouse)
		{
			RemoveSpouse(c);
		}
		if (Children != null && Children.Count > 0)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i] == c)
				{
					if (Children[i] == Heir)
					{
						Unheir();
					}
					RemoveChild(c);
					return true;
				}
			}
		}
		return false;
	}

	public void AddRelative(Character c, bool send_state = true)
	{
		if (!Relatives.Contains(c))
		{
			Relatives.Add(c);
			if (send_state)
			{
				GetKingdom().SendState<Kingdom.RoyalFamilyState>();
				OnFamilyChangedAnalytics(c, "relative_added");
			}
			NotifyListeners("royal_relatives_changed");
			c.NotifyListeners("royal_relative_status_changed");
		}
	}

	public void RemoveRelative(Character c, bool send_state = true)
	{
		if (Relatives.Contains(c))
		{
			OnFamilyChangedAnalytics(c, "remove_relative");
			Relatives.Remove(c);
			if (!c.IsInCourt() && c.IsAlive() && !c.IsRebel())
			{
				c.Die();
			}
			else
			{
				c.DelStatus<HasRoyalBloodStatus>();
			}
			if (send_state)
			{
				OnFamilyChangedAnalytics(c, "relative_removed");
				GetKingdom().SendState<Kingdom.RoyalFamilyState>();
			}
			NotifyListeners("royal_relatives_changed");
			c.NotifyListeners("royal_relative_status_changed");
		}
	}

	public bool IsRelative(Character c)
	{
		if (c == null)
		{
			return false;
		}
		if (Relatives != null && Relatives.Count > 0)
		{
			for (int i = 0; i < Relatives.Count; i++)
			{
				if (Relatives[i] == c)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void SetNextNewbornCheck(float duration)
	{
		Kingdom kingdom = GetKingdom();
		if (kingdom == null || !kingdom.IsValid())
		{
			return;
		}
		Timer timer = Timer.Find(kingdom, "check_newborn");
		if (timer != null)
		{
			if (timer.duration - (game.time - timer.start_time) > duration)
			{
				return;
			}
			Timer.Stop(kingdom, "check_newborn");
		}
		Timer.Start(kingdom, "check_newborn", duration, restart: true);
	}

	public float GetChildBirthMinTimeAfterMarriageOrBirth()
	{
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		return game.Random(agingDef.child_birth_min_time_after_marriage_or_birth, agingDef.child_birth_max_time_after_marriage_or_birth);
	}

	public float GetChildBirthCheckInterval()
	{
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		return game.Random(agingDef.child_birth_check_rate_min, agingDef.child_birth_check_rate_max);
	}

	public float GetInitialMarriageCooldown()
	{
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		return game.Random(agingDef.initial_automarriage_cooldown_min, agingDef.initial_automarriage_cooldown_max);
	}

	public float GetMarriageCheckInterval()
	{
		return game.rules.GetAgingDef().marriage_check_interval;
	}

	public float GetMarriageChance(Character c)
	{
		if (c == null)
		{
			return 0f;
		}
		CharacterAge.Def agingDef = game.rules.GetAgingDef();
		if (agingDef.marriage_chance_perc == null)
		{
			return 0f;
		}
		int age = (int)c.age;
		if (agingDef.marriage_chance_perc.Length <= age)
		{
			return 0f;
		}
		return agingDef.marriage_chance_perc[age];
	}

	public float GetChildBirthChance()
	{
		return game.rules.GetAgingDef().child_birth_chance_perc;
	}

	public float GetPrinceSpecializationDefaultSkillWeight()
	{
		return def.prince_specialization_default_skill_weight;
	}

	public float GetPrinceSpecializationFatherSkillWeight()
	{
		return def.prince_specialization_father_skill_weight;
	}

	public float GetPrinceSpecializationTraditionSkillWeight()
	{
		return def.prince_specialization_tradition_skill_weight;
	}

	public float GetPrinceSpecializationPrimarySkillWeight()
	{
		return def.prince_specialization_primary_skill_weight;
	}
}
