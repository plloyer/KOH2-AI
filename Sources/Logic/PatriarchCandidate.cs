using System.Collections.Generic;

namespace Logic;

public class Orthodox : Religion
{
	public class PatriarchCandidate : IVars
	{
		public Character cleric;

		public float eval;

		public List<CharacterBonus> bonuses;

		public bool generated;

		public Value GetVar(string key, IVars vars = null, bool as_value = true)
		{
			switch (key)
			{
			case "cleric":
				return cleric;
			case "bonuses":
				return new Value(bonuses);
			case "bonuses_text":
			{
				Kingdom kingdom = cleric.GetKingdom();
				if (Religion.get_patriarch_candidate_bonuses_text == null || kingdom == null)
				{
					return Value.Null;
				}
				return "#" + Religion.get_patriarch_candidate_bonuses_text(kingdom, this);
			}
			default:
				return Value.Unknown;
			}
		}
	}

	public Orthodox(Game game, Def def)
		: base(game, def)
	{
	}

	public override void Init(bool new_game)
	{
		base.Init(new_game);
	}

	public override void OnHQRealmChangedKingdom()
	{
		Kingdom kingdom = head_kingdom;
		Character prev = head;
		head_kingdom = hq_realm.GetKingdom();
		game.religions.SendState<Religions.HeadKingdomsState>();
		if (game.IsAuthority())
		{
			if (kingdom.patriarch_candidates != null)
			{
				PatriarchChosen(kingdom, null, notify: false);
			}
			if (head_kingdom.patriarch_candidates != null)
			{
				PatriarchChosen(head_kingdom, null, notify: false);
			}
			if (kingdom != null && kingdom.is_orthodox)
			{
				SetSubordinated(kingdom, subordinated: true);
			}
			if (kingdom != null && (!kingdom.is_orthodox || kingdom.subordinated))
			{
				SetPatriarch(kingdom, null);
			}
			if (head_kingdom.is_orthodox && head_kingdom.subordinated)
			{
				SetSubordinated(head_kingdom, subordinated: false);
			}
			else if (head_kingdom.patriarch != null)
			{
				SetHead(head_kingdom.patriarch);
			}
			else
			{
				CreatePatriarch(head_kingdom, prev, notify: false);
			}
			if (head_kingdom == hq_kingdom)
			{
				game.religions.FireEvent("orthodox_restored", null);
			}
			else
			{
				game.religions.FireEvent("orthodox_destroyed", null);
			}
		}
	}

	public void SetSubordinated(Kingdom k, bool subordinated, Character new_patriarch = null, bool check_authority = true)
	{
		if ((check_authority && !game.IsAuthority()) || k == null)
		{
			return;
		}
		if (game.IsAuthority())
		{
			CancelChoosePatriarch(k);
		}
		k.subordinated = subordinated;
		if (subordinated)
		{
			SetPatriarch(k, null, send_state: false, check_authority);
			k.SendState<Kingdom.ReligionState>();
			k.NotifyListeners("subordinated");
			return;
		}
		if (new_patriarch != null)
		{
			SetPatriarch(k, new_patriarch, send_state: true, check_authority);
		}
		else if (k.patriarch == null)
		{
			CreatePatriarch(k, null);
		}
		if (k == head_kingdom && k.patriarch != head && k.patriarch != null)
		{
			SetHead(k.patriarch);
		}
		Religion.RefreshModifiers(k);
		k.SendState<Kingdom.ReligionState>();
		k.NotifyListeners("autocephaly");
	}

	public override void SetHead(Character head, bool from_state = false)
	{
		Character character = base.head;
		base.SetHead(head, from_state);
		if (from_state)
		{
			return;
		}
		if (head != null)
		{
			Marriage marriage = head.GetMarriage();
			if (marriage != null)
			{
				head?.Divorce("became_ecumenical_patriarch");
				marriage.kingdom_wife.NotifyListeners("become_ecumenical_patriarch_divorce", marriage);
			}
			head.TryRevealMasters("ForceRevealAction");
		}
		if (character != null)
		{
			bool flag = character.IsKing() || character.IsRoyalChild() || character.IsRoyalRelative();
			if (character.IsInCourt())
			{
				character.RefreshTags();
			}
			else if (character.IsValid() && !flag)
			{
				character.Destroy();
			}
		}
		if (head_kingdom != null)
		{
			SetPatriarch(head_kingdom, head);
		}
		head?.NotifyListeners("became_ecumenical_patriarch");
	}

	public override void OnCharacterDied(Character c)
	{
		Kingdom kingdom = c.GetKingdom();
		if (!kingdom.IsRegular())
		{
			kingdom = c.GetSpecialCourtKingdom();
		}
		if (kingdom?.patriarch_candidates != null)
		{
			for (int i = 0; i < kingdom.patriarch_candidates.Count; i++)
			{
				if (kingdom.patriarch_candidates[i].cleric == c)
				{
					kingdom.patriarch_candidates.RemoveAt(i);
					kingdom.NotifyListeners("patriarch_candidates_changed");
					break;
				}
			}
		}
		if (!c.IsPatriarch() && !c.IsEcumenicalPatriarch())
		{
			base.OnCharacterDied(c);
			return;
		}
		Kingdom kingdom2 = ((c == head) ? head_kingdom : kingdom);
		if (c == head)
		{
			c.SetTitle("EcumenicalPatriarch");
			SetHead(null);
		}
		else if (kingdom2 != null)
		{
			c.SetTitle("Patriarch");
			SetPatriarch(kingdom2, null);
		}
		CreatePatriarch(kingdom2, c);
	}

	public void SetPatriarch(Kingdom kingdom, Character patriarch, bool send_state = true, bool check_authority = true)
	{
		if (check_authority && !kingdom.IsAuthority())
		{
			return;
		}
		Character patriarch2 = kingdom.patriarch;
		DelPatriarchModifiers(kingdom);
		kingdom.patriarch = patriarch;
		if (patriarch2 != null)
		{
			patriarch2.DelStatus("PatriarchStatus");
			kingdom.patriarch_castle = patriarch2.GetGovernedCastle();
			if (patriarch2 != head)
			{
				patriarch2.DelListener(this);
			}
			if (patriarch2.IsInCourt() && patriarch == null)
			{
				patriarch2.RefreshTags();
				patriarch2.NotifyListeners("is_patriarch_changed");
			}
			else if (patriarch2.IsValid() && patriarch2 != head && patriarch2.IsRoyalChild() && patriarch2.IsRoyalRelative() && patriarch2.IsInSpecialCourt())
			{
				patriarch2.Destroy();
			}
		}
		if (patriarch == null)
		{
			kingdom.patriarch_bonuses = null;
			Religion.RefreshModifiers(kingdom);
			if (send_state)
			{
				kingdom.SendState<Kingdom.ReligionState>();
			}
			kingdom.NotifyListeners("patriarch_changed");
			return;
		}
		List<CharacterBonus> list = null;
		if (kingdom.patriarch_bonuses != null)
		{
			list = kingdom.patriarch_bonuses;
		}
		else if (kingdom.patriarch_candidates != null)
		{
			for (int i = 0; i < kingdom.patriarch_candidates.Count; i++)
			{
				PatriarchCandidate patriarchCandidate = kingdom.patriarch_candidates[i];
				if (patriarchCandidate.cleric == patriarch)
				{
					list = patriarchCandidate.bonuses;
					break;
				}
			}
		}
		if (list == null)
		{
			list = new List<CharacterBonus>();
			ChooseRandomPatriarchBonuses(list, kingdom);
		}
		kingdom.patriarch_bonuses = list;
		AddPatriarchToCourt(kingdom);
		patriarch.AddStatus("PatriarchStatus");
		if (patriarch.IsHeir())
		{
			patriarch.GetKingdom().royalFamily.Unheir();
		}
		Marriage marriage = patriarch.GetMarriage();
		if (marriage != null)
		{
			patriarch.Divorce("became_patriarch");
			marriage.kingdom_wife.NotifyListeners("become_patriarch_divorce", marriage);
		}
		patriarch.RefreshTags();
		Religion.RefreshModifiers(kingdom);
		AddPatriarchModifiers(kingdom);
		kingdom.stability.SpecialEvent(think_rebel: false);
		if (send_state)
		{
			kingdom.SendState<Kingdom.ReligionState>();
		}
		kingdom.NotifyListeners("patriarch_changed");
		patriarch.NotifyListeners("is_patriarch_changed");
		if (game.IsAuthority())
		{
			patriarch.ForceSkillsToMaxRank();
		}
	}

	public void DelPatriarchFromCourt(Kingdom kingdom)
	{
		if (kingdom.patriarch != null)
		{
			kingdom.DelCourtMember(kingdom.patriarch, send_state: true, kill_or_throneroom: false);
		}
	}

	public void AddPatriarchToCourt(Kingdom kingdom)
	{
		if (kingdom.patriarch == null || !kingdom.is_orthodox || kingdom.subordinated)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character courtOrSpecialCourtMember = kingdom.GetCourtOrSpecialCourtMember(i);
			if (courtOrSpecialCourtMember == null)
			{
				continue;
			}
			if (courtOrSpecialCourtMember.IsPatriarch() && kingdom.patriarch != courtOrSpecialCourtMember)
			{
				Game.Log($"Adding {kingdom.patriarch} to court, but there is a patriarch ({courtOrSpecialCourtMember}) already!", Game.LogType.Warning);
			}
			if (courtOrSpecialCourtMember.FindStatus<DeadPatriarchStatus>() != null)
			{
				if (num == -1)
				{
					num = i;
				}
				else
				{
					Game.Log("More than one dead patriarch found!", Game.LogType.Warning);
				}
				courtOrSpecialCourtMember.Destroy();
			}
		}
		if (!kingdom.patriarch.IsInCourt(kingdom))
		{
			kingdom.AddCourtMember(kingdom.patriarch, num);
		}
	}

	public string GetPatriarchBonusType(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return null;
		}
		if (kingdom.HasEcumenicalPatriarch())
		{
			if (kingdom.is_orthodox)
			{
				return "ecumenical_patriarch";
			}
			return "non_orthodox";
		}
		if (!kingdom.is_orthodox || kingdom.subordinated)
		{
			return null;
		}
		return "independent";
	}

	private bool HasBonus(CharacterBonus bonus, string type)
	{
		if (bonus?.mods == null)
		{
			return false;
		}
		for (int i = 0; i < bonus.mods.Count; i++)
		{
			if (bonus.mods[i]?.field?.key == type)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasBonus(List<CharacterBonus> bonuses, string type)
	{
		if (bonuses == null)
		{
			return false;
		}
		for (int i = 0; i < bonuses.Count; i++)
		{
			CharacterBonus bonus = bonuses[i];
			if (HasBonus(bonus, type))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsPatriarchBonusApplicable(List<CharacterBonus> bonuses, CharacterBonus bonus, Kingdom kingdom, string type)
	{
		if (bonus == null || type == null || bonus.mods.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < bonus.mods.Count; i++)
		{
			StatModifier.Def def = bonus.mods[i];
			if (kingdom != null && !kingdom.started && def.field.type == "instant")
			{
				return false;
			}
			if (HasBonus(bonuses, def.field.key))
			{
				return false;
			}
			if (!ValidatePatriarchBonusMod(kingdom, def.field.key))
			{
				return false;
			}
			if (def.field.value.is_valid)
			{
				return true;
			}
			if (def.field.FindChild(type) != null)
			{
				return true;
			}
		}
		return false;
	}

	public void ChooseOwnClericPatriarchBonuses(List<CharacterBonus> bonuses, Kingdom kingdom, string type = null)
	{
		AddRandomPatriarchBonuses(bonuses, own: true, kingdom, type);
	}

	public void ChooseRandomPatriarchBonuses(List<CharacterBonus> bonuses, Kingdom kingdom, string type = null)
	{
		AddRandomPatriarchBonuses(bonuses, own: false, kingdom, type);
	}

	public void AddRandomPatriarchBonuses(List<CharacterBonus> bonuses, bool own, Kingdom kingdom, string type)
	{
		List<CharacterBonus> list = (own ? def.own_cleric_patriarch_bonuses : def.random_patriarch_bonuses);
		if (bonuses == null || list == null || list.Count == 0)
		{
			return;
		}
		if (type == null)
		{
			type = GetPatriarchBonusType(kingdom);
			if (type == null)
			{
				return;
			}
		}
		DT.Field field = def.field.FindChild(own ? "own_cleric_patriarch_bonuses" : "random_patriarch_bonuses");
		int num;
		if (field == null)
		{
			num = 1;
		}
		else if (field.NumValues() == 2)
		{
			int min = field.Int(0);
			int num2 = field.Int(1);
			num = game.Random(min, num2 + 1);
		}
		else
		{
			num = def.field.Int(null, 1);
		}
		if (num <= 0)
		{
			return;
		}
		List<CharacterBonus> list2 = new List<CharacterBonus>(list.Count);
		for (int i = 0; i < num; i++)
		{
			list2.Clear();
			for (int j = 0; j < list.Count; j++)
			{
				CharacterBonus characterBonus = list[j];
				if (IsPatriarchBonusApplicable(bonuses, characterBonus, kingdom, type))
				{
					list2.Add(characterBonus);
				}
			}
			if (list2.Count > 0)
			{
				int index = game.Random(0, list2.Count);
				CharacterBonus item = list2[index];
				bonuses.Add(item);
				continue;
			}
			break;
		}
	}

	public void AddPatriarchModifiers(Kingdom k)
	{
		if (k.patriarch_bonuses == null || GetPatriarchBonusType(k) == null)
		{
			return;
		}
		Stats stats = k.stats;
		for (int i = 0; i < k.patriarch_bonuses.Count; i++)
		{
			CharacterBonus characterBonus = k.patriarch_bonuses[i];
			for (int j = 0; j < characterBonus.mods.Count; j++)
			{
				StatModifier.Def def = characterBonus.mods[j];
				if (!(def.field.type != "mod"))
				{
					StatModifier statModifier = new StatModifier(def);
					stats.AddModifier(def.stat_name, statModifier);
					if (k.patriarch_mods == null)
					{
						k.patriarch_mods = new List<StatModifier>(k.patriarch_bonuses.Count);
					}
					k.patriarch_mods.Add(statModifier);
				}
			}
		}
	}

	public void ApplyPatriarchInstantBonuses(Kingdom k)
	{
		if (k.patriarch_bonuses == null)
		{
			return;
		}
		string patriarchBonusType = GetPatriarchBonusType(k);
		if (patriarchBonusType == null)
		{
			return;
		}
		for (int i = 0; i < k.patriarch_bonuses.Count; i++)
		{
			CharacterBonus characterBonus = k.patriarch_bonuses[i];
			for (int j = 0; j < characterBonus.mods.Count; j++)
			{
				StatModifier.Def def = characterBonus.mods[j];
				if (!(def.field.type != "instant"))
				{
					ApplyInstantPatriarchBonus(k, def, patriarchBonusType);
				}
			}
		}
	}

	private bool ValidatePatriarchBonusMod(Kingdom k, string key)
	{
		switch (key)
		{
		case "crown_authority":
		{
			CrownAuthority crownAuthority = k?.GetCrownAuthority();
			if (crownAuthority == null)
			{
				return false;
			}
			int value = crownAuthority.GetValue();
			int num = crownAuthority.Max();
			if (value >= num)
			{
				return false;
			}
			return true;
		}
		case "rel_change_with_papacy":
		{
			Kingdom kingdom = game.religions.catholic.hq_kingdom;
			if (kingdom == null || kingdom.IsDefeated())
			{
				return false;
			}
			return true;
		}
		case "rel_change_with_all_orthodox":
		{
			for (int i = 0; i < game.kingdoms.Count; i++)
			{
				Kingdom kingdom2 = game.kingdoms[i];
				if (kingdom2 != null && !kingdom2.IsDefeated() && kingdom2 != k && kingdom2.is_orthodox)
				{
					return true;
				}
			}
			return false;
		}
		case "ks_influence_in_constantinople_owner":
			if (k == game.religions.orthodox.head_kingdom)
			{
				return false;
			}
			return true;
		default:
			return true;
		}
	}

	private void ApplyInstantPatriarchBonus(Kingdom k, StatModifier.Def mod_def, string type)
	{
		DT.Field field = mod_def.field.FindChild(type);
		if (field == null)
		{
			field = mod_def.field;
		}
		if (!field.value.is_valid)
		{
			Game.Log(mod_def.ToString() + ": Patriarch instant bonus has no value for " + type, Game.LogType.Warning);
			return;
		}
		float num = field.Float(k);
		switch (mod_def.field.key)
		{
		case "crown_authority":
			k.GetCrownAuthority().ChangeValue((int)num);
			break;
		case "gain_gold":
			k.AddResources(KingdomAI.Expense.Category.Religion, ResourceType.Gold, num);
			break;
		case "rel_change_with_papacy":
		{
			Kingdom kingdom3 = game.religions.catholic.hq_kingdom;
			if (kingdom3 != null && !kingdom3.IsDefeated())
			{
				k.AddRelationship(kingdom3, num, this);
			}
			break;
		}
		case "rel_change_with_all_orthodox":
			k.AddRelationship(game.religions.orthodox, num, this);
			break;
		case "rebels_repent_chance_perc":
		{
			for (int l = 0; l < k.armies_in.Count; l++)
			{
				Army army = k.armies_in[l];
				if (army.rebel == null)
				{
					break;
				}
				if ((float)game.Random(0, 100) < num)
				{
					army.Destroy();
				}
			}
			break;
		}
		case "deepen_orthodox_hostilities_perc":
		{
			float num2 = num / 100f;
			for (int i = 0; i < game.kingdoms.Count; i++)
			{
				Kingdom kingdom = game.kingdoms[i];
				if (!kingdom.is_orthodox)
				{
					continue;
				}
				for (int j = i + 1; j < game.kingdoms.Count; j++)
				{
					Kingdom kingdom2 = game.kingdoms[j];
					if (kingdom2.is_orthodox)
					{
						float relationship = KingdomAndKingdomRelation.GetRelationship(kingdom, kingdom2);
						KingdomAndKingdomRelation.AddRelationship(kingdom, kingdom2, relationship * num2);
					}
				}
			}
			break;
		}
		default:
			Game.Log(mod_def.ToString() + ": Unknown instant bonus type", Game.LogType.Warning);
			break;
		}
	}

	public void DelPatriarchModifiers(Kingdom k)
	{
		if (k.patriarch_mods == null)
		{
			return;
		}
		for (int i = 0; i < k.patriarch_mods.Count; i++)
		{
			StatModifier statModifier = k.patriarch_mods[i];
			if (statModifier.stat != null)
			{
				statModifier.stat.DelModifier(statModifier);
			}
		}
		k.patriarch_mods = null;
	}

	public Character CreatePatriarch(Kingdom kingdom, Character prev, bool notify = true)
	{
		if (kingdom == null)
		{
			return null;
		}
		Vars vars = new Vars(prev?.GetName());
		if (prev != null)
		{
			vars.Set("title", prev.GetTitle());
			if (prev.name_idx > 0)
			{
				vars.Set("name_idx", prev.name_idx);
			}
		}
		vars.Set("kingdom", kingdom);
		if (!kingdom.is_player || !kingdom.started)
		{
			Character character = CreateCharacter(kingdom);
			vars.Set("replacement", character);
			vars.Set("old_title", GetTitle(character));
			vars.Set("old_name", character.GetName());
			if (character.name_idx > 0)
			{
				vars.Set("old_name_idx", character.name_idx);
			}
			PatriarchChosen(kingdom, character, notify: false);
			if (notify)
			{
				game.religions.FireEvent("new_patriarch_chosen", vars);
			}
			return character;
		}
		if (kingdom == head_kingdom)
		{
			SetHead(null);
		}
		kingdom.patriarch_candidates = GeneratePatriarchCandidates(kingdom, prev);
		kingdom.SendState<Kingdom.ReligionState>();
		Timer.Start(kingdom, "choose_patriarch_timeout", def.field.GetFloat("choose_patriarch_timeout"));
		game.religions.FireEvent("choose_patriarch", vars);
		return null;
	}

	public PatriarchCandidate GeneratePatriarchCandidate(Kingdom k)
	{
		PatriarchCandidate patriarchCandidate = new PatriarchCandidate();
		if (k.is_orthodox)
		{
			patriarchCandidate.cleric = CreateCharacter(k);
		}
		else
		{
			patriarchCandidate.cleric = CreateCharacter(hq_kingdom);
			if (k != hq_kingdom && k == head_kingdom)
			{
				patriarchCandidate.cleric.original_kingdom_id = k.id;
				patriarchCandidate.cleric.kingdom_id = k.id;
			}
		}
		patriarchCandidate.generated = true;
		return patriarchCandidate;
	}

	public List<PatriarchCandidate> GeneratePatriarchCandidates(Kingdom k, Character prev)
	{
		List<PatriarchCandidate> list = new List<PatriarchCandidate>();
		if (k.court != null && k.is_orthodox)
		{
			for (int i = 0; i < k.court.Count; i++)
			{
				Character character = k.court[i];
				if (character != null && character != prev && character.IsCleric() && !character.IsKing() && character.IsAlive())
				{
					PatriarchCandidate patriarchCandidate = new PatriarchCandidate();
					patriarchCandidate.cleric = character;
					patriarchCandidate.eval = character.GetClassLevel();
					patriarchCandidate.generated = false;
					list.Add(patriarchCandidate);
				}
			}
		}
		int num = 2;
		if (list.Count > 1)
		{
			list.Sort((PatriarchCandidate a, PatriarchCandidate b) => b.eval.CompareTo(a.eval));
		}
		if (list.Count > num)
		{
			list.RemoveRange(2, list.Count - 2);
		}
		if (list.Count == 0 && game.Random(0, 100) < 50)
		{
			list.Add(GeneratePatriarchCandidate(k));
		}
		list.Add(GeneratePatriarchCandidate(k));
		list.Add(GeneratePatriarchCandidate(k));
		string patriarchBonusType = GetPatriarchBonusType(k);
		if (patriarchBonusType != null)
		{
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				PatriarchCandidate patriarchCandidate2 = list[num2];
				patriarchCandidate2.bonuses = new List<CharacterBonus>();
				if (!patriarchCandidate2.generated)
				{
					ChooseOwnClericPatriarchBonuses(patriarchCandidate2.bonuses, k, patriarchBonusType);
				}
				ChooseRandomPatriarchBonuses(patriarchCandidate2.bonuses, k, patriarchBonusType);
			}
		}
		return list;
	}

	public void CancelChoosePatriarch(Kingdom kingdom)
	{
		Timer.Stop(kingdom, "choose_patriarch_timeout");
		if (kingdom.patriarch_candidates == null)
		{
			return;
		}
		for (int i = 0; i < kingdom.patriarch_candidates.Count; i++)
		{
			PatriarchCandidate patriarchCandidate = kingdom.patriarch_candidates[i];
			if (patriarchCandidate.generated)
			{
				patriarchCandidate.cleric.Destroy();
			}
		}
		kingdom.patriarch_candidates = null;
	}

	public void PatriarchChosen(Kingdom kingdom, Character c, bool notify = true)
	{
		if (!kingdom.IsAuthority())
		{
			kingdom.SendEvent(new Kingdom.ChoosePatriarchEvent(c));
			return;
		}
		Timer.Stop(kingdom, "choose_patriarch_timeout");
		if (kingdom.patriarch != null)
		{
			return;
		}
		Vars vars = null;
		if (notify)
		{
			vars = new Vars();
			vars.Set("kingdom", kingdom);
		}
		if (c == null)
		{
			c = ((kingdom.patriarch_candidates == null || kingdom.patriarch_candidates.Count <= 0) ? CreateCharacter(kingdom) : kingdom.patriarch_candidates[game.Random(0, kingdom.patriarch_candidates.Count)].cleric);
		}
		if (notify)
		{
			vars.Set("replacement", c);
			vars.Set("old_title", GetTitle(c));
			vars.Set("old_name", c.GetName());
			if (c.name_idx > 0)
			{
				vars.Set("old_name_idx", c.name_idx);
			}
		}
		if (kingdom == head_kingdom)
		{
			SetHead(c);
		}
		else
		{
			string text = NewName();
			if (text != null)
			{
				c.Name = text;
			}
			c.EnableAging(can_die: true);
			SetPatriarch(kingdom, c);
		}
		ApplyPatriarchInstantBonuses(kingdom);
		if (kingdom.patriarch_candidates != null)
		{
			for (int i = 0; i < kingdom.patriarch_candidates.Count; i++)
			{
				PatriarchCandidate patriarchCandidate = kingdom.patriarch_candidates[i];
				if (patriarchCandidate.generated && patriarchCandidate.cleric != c)
				{
					patriarchCandidate.cleric.Destroy();
				}
			}
			kingdom.patriarch_candidates = null;
		}
		RoyalFamily royalFamily = kingdom.royalFamily;
		if (royalFamily != null)
		{
			if (c == royalFamily.Sovereign)
			{
				royalFamily.Uncrown("chosen_as_patriarch", royalFamily.GetSovereignUncrownData());
			}
			if (c == royalFamily.Heir)
			{
				royalFamily.Unheir();
			}
		}
		if (kingdom.is_orthodox && !kingdom.subordinated && c.IsInCourt() && c.governed_castle == null && kingdom.patriarch_castle != null && kingdom.patriarch_castle.governor == null)
		{
			c.Govern(kingdom.patriarch_castle);
		}
		kingdom.patriarch_castle = c.governed_castle;
		if (notify)
		{
			game.religions.FireEvent("new_patriarch_chosen", vars);
		}
	}
}
