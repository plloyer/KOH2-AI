using System.Collections.Generic;

namespace Logic;

public class Catholic : Religion
{
	public int min_cardinals_min = 2;

	public int min_cardinals_max = 4;

	public int max_cardinals = 7;

	public int min_cardinal_relationship_with_papacy_min;

	public int min_cardinal_relationship_with_papacy_max;

	public int eval_papacy_cardinal_own_relation = 500;

	public int min_cardinal_class_level;

	public float min_cardinals_update_interval = 60f;

	public float max_cardinals_update_interval = 120f;

	public float min_cardinals_game_start_update = 1800f;

	public float max_cardinals_game_start_update = 1800f;

	public float demote_cardinal_chance = 10f;

	public float cardinal_piety_per_relationship_with_papacy = 0.01f;

	public float cardinal_books_per_relationship_with_papacy = 0.01f;

	public DT.Field papacy_csv;

	public Realm holy_lands_realm;

	public Time next_excommunicate = Time.Zero;

	public Time next_enemy_excommunicate = Time.Zero;

	public Time next_ask_crusade_funds = Time.Zero;

	public Time next_ask_court_cleric = Time.Zero;

	public Crusade crusade;

	public int crusades_count;

	public Time last_crusade_end = Time.Zero;

	public Kingdom just_refused_crusade;

	public List<Character> cardinals = new List<Character>();

	public List<CharacterBonus> pope_bonuses = new List<CharacterBonus>();

	public List<StatModifier> pope_modifiers = new List<StatModifier>();

	private string restore_papacy_reason;

	private int kingdom_to_update = 1;

	private int think_type;

	private int excommunicated_count;

	public Catholic(Game game, Def def)
		: base(game, def)
	{
	}

	public override void Init(bool new_game)
	{
		base.Init(new_game);
		DT.Field field = def.field.FindChild("min_cardinals");
		if (field != null)
		{
			min_cardinals_min = field.Int(0, this, min_cardinals_min);
			min_cardinals_max = field.Int(1, this, min_cardinals_min);
		}
		max_cardinals = def.field.GetInt("max_cardinals", this, max_cardinals);
		DT.Field field2 = def.field.FindChild("min_cardinal_relationship_with_papacy");
		if (field2 != null)
		{
			min_cardinal_relationship_with_papacy_min = field2.Int(0, this, min_cardinal_relationship_with_papacy_min);
			min_cardinal_relationship_with_papacy_max = field2.Int(1, this, min_cardinal_relationship_with_papacy_min);
		}
		eval_papacy_cardinal_own_relation = def.field.GetInt("eval_papacy_cardinal_own_relation", this, eval_papacy_cardinal_own_relation);
		min_cardinal_class_level = def.field.GetInt("min_cardinal_class_level", this, min_cardinal_class_level);
		DT.Field field3 = def.field.FindChild("cardinals_update_interval");
		if (field3 != null)
		{
			min_cardinals_update_interval = field3.Float(0, this, min_cardinals_update_interval);
			max_cardinals_update_interval = field3.Float(1, this, min_cardinals_update_interval);
		}
		DT.Field field4 = def.field.FindChild("cardinals_game_start_update");
		if (field4 != null)
		{
			min_cardinals_game_start_update = field4.Float(0, this, min_cardinals_game_start_update);
			max_cardinals_game_start_update = field4.Float(1, this, min_cardinals_game_start_update);
		}
		demote_cardinal_chance = def.field.GetFloat("demote_cardinal_chance", this, demote_cardinal_chance);
		cardinal_piety_per_relationship_with_papacy = def.field.GetFloat("cardinal_piety_per_relationship_with_papacy", null, cardinal_piety_per_relationship_with_papacy);
		cardinal_books_per_relationship_with_papacy = def.field.GetFloat("cardinal_books_per_relationship_with_papacy", null, cardinal_books_per_relationship_with_papacy);
		string text = def.field.GetString("holy_lands_realm");
		if (!string.IsNullOrEmpty(text))
		{
			holy_lands_realm = game.GetRealm(text);
			if (holy_lands_realm == null)
			{
				Game.Log(def.field.Path(include_file: true) + ".holy_lands_realm: '" + text + "' realm not found", Game.LogType.Error);
			}
		}
		excommunicated_count = 0;
		if (new_game)
		{
			last_crusade_end = game.time + 0.001f;
		}
	}

	public override void InitCharacters(bool new_game)
	{
		base.InitCharacters(new_game);
		if (new_game && game.IsAuthority())
		{
			InitCardinals();
			StartCardinalsUpdateTimer(GetCardinalsGameStartUpdateTimer());
		}
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		return key switch
		{
			"holy_lands_realm" => holy_lands_realm, 
			"lead_crusade_offer" => LeadCrusadeOffer.Get(game), 
			"excommunicated_count" => excommunicated_count, 
			_ => base.GetVar(key, vars, as_value), 
		};
	}

	public void PopeLeave()
	{
		if (head != null)
		{
			Kingdom specialCourtKingdom = head.GetSpecialCourtKingdom();
			if (specialCourtKingdom != null && specialCourtKingdom != hq_kingdom)
			{
				specialCourtKingdom.DelSpecialCourtMember(head);
				DelPopeModifiers(head_kingdom);
				head_kingdom = hq_kingdom;
				AddPopeModifiers(head_kingdom);
				head.original_kingdom_id = hq_kingdom.id;
				head.SendState<Character.OriginState>();
				game.religions.SendState<Religions.HeadKingdomsState>();
			}
		}
	}

	public override Character CreateHead(Kingdom kingdom, bool initial = false)
	{
		return CharacterFactory.CreatePope(game, kingdom, initial);
	}

	public Character ChooseNewPope(Kingdom force_kingdom = null, Kingdom exclude_kingdom = null)
	{
		WeightedRandom<Character> temp = WeightedRandom<Character>.GetTemp();
		for (int i = 0; i < cardinals.Count; i++)
		{
			Character character = cardinals[i];
			Kingdom kingdom = character.GetKingdom();
			if ((force_kingdom == null || kingdom == force_kingdom) && kingdom != exclude_kingdom)
			{
				string validate_result;
				float num = EvaluateCardinalElect(character, out validate_result);
				if (!(num < 0f))
				{
					temp.AddOption(character, num);
				}
			}
		}
		return temp.Choose();
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

	private bool ValidatePopeBonusMod(Kingdom k, string key)
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

	public bool IsPopeBonusApplicable(List<CharacterBonus> bonuses, CharacterBonus bonus, Kingdom kingdom)
	{
		if (bonus == null || bonus.mods.Count == 0)
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
			if (!ValidatePopeBonusMod(kingdom, def.field.key))
			{
				return false;
			}
			if (def.field.value.is_valid)
			{
				return true;
			}
		}
		return false;
	}

	public void ChooseRandomPopeBonuses(List<CharacterBonus> bonuses, Kingdom kingdom)
	{
		List<CharacterBonus> random_pope_bonuses = def.random_pope_bonuses;
		if (bonuses == null || random_pope_bonuses == null || random_pope_bonuses.Count == 0)
		{
			return;
		}
		DT.Field field = def.field.FindChild("random_pope_bonuses");
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
		List<CharacterBonus> list = new List<CharacterBonus>(random_pope_bonuses.Count);
		for (int i = 0; i < num; i++)
		{
			list.Clear();
			for (int j = 0; j < random_pope_bonuses.Count; j++)
			{
				CharacterBonus characterBonus = random_pope_bonuses[j];
				if (IsPopeBonusApplicable(bonuses, characterBonus, kingdom))
				{
					list.Add(characterBonus);
				}
			}
			if (list.Count > 0)
			{
				int index = game.Random(0, list.Count);
				CharacterBonus item = list[index];
				bonuses.Add(item);
				continue;
			}
			break;
		}
	}

	public void AddPopeModifiers(Kingdom k)
	{
		if (pope_bonuses == null || pope_bonuses.Count == 0)
		{
			return;
		}
		Stats stats = k.stats;
		for (int i = 0; i < pope_bonuses.Count; i++)
		{
			CharacterBonus characterBonus = pope_bonuses[i];
			for (int j = 0; j < characterBonus.mods.Count; j++)
			{
				StatModifier.Def def = characterBonus.mods[j];
				if (!(def.field.type != "mod"))
				{
					StatModifier statModifier = new StatModifier(def);
					stats.AddModifier(def.stat_name, statModifier);
					if (pope_modifiers == null)
					{
						pope_modifiers = new List<StatModifier>(k.patriarch_bonuses.Count);
					}
					pope_modifiers.Add(statModifier);
				}
			}
		}
	}

	public void DelPopeModifiers(Kingdom k)
	{
		if (pope_bonuses == null)
		{
			return;
		}
		for (int i = 0; i < pope_modifiers.Count; i++)
		{
			StatModifier statModifier = pope_modifiers[i];
			if (statModifier.stat != null)
			{
				statModifier.stat.DelModifier(statModifier);
			}
		}
		pope_modifiers.Clear();
	}

	public void StartPopeConvertRealmsTimer()
	{
		DT.Field field = def.field.FindChild("pope_convert_realms");
		if (field != null)
		{
			int min = field.GetInt("min_interval");
			int num = field.GetInt("max_interval");
			int num2 = game.Random(min, num + 1);
			Timer.Start(head, "pope_convert_religions", num2, restart: true);
		}
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
				head.Divorce("became_pope");
				marriage.kingdom_wife.FireEvent("become_pope_divorce", marriage);
			}
			if (head.GetKingdom() == hq_kingdom)
			{
				head.TryRevealMasters("ForceRevealAction");
			}
		}
		character?.DelStatus("PopeStatus");
		Timer.Stop(character, "pope_convert_religions");
		DelPopeModifiers(head_kingdom);
		head_kingdom = head?.GetKingdom();
		if (head != null)
		{
			if (head_kingdom == hq_kingdom)
			{
				hq_kingdom.DelCourtMember(head, send_state: true, kill_or_throneroom: false);
			}
			head.original_kingdom_id = head.kingdom_id;
			head.SetKingdom(hq_kingdom.id);
			hq_kingdom?.royalFamily?.ChangeSovereign(head, null, "new_pope");
			head.Govern(hq_realm?.castle);
			if (pope_bonuses == null)
			{
				pope_bonuses = new List<CharacterBonus>();
			}
			else
			{
				pope_bonuses.Clear();
			}
			ChooseRandomPopeBonuses(pope_bonuses, head_kingdom);
			AddPopeModifiers(head_kingdom);
			if (head_kingdom != null && head_kingdom != hq_kingdom)
			{
				head_kingdom.MoveToSpecialCourt(head, !from_state);
			}
			head.AddStatus("PopeStatus");
			StartPopeConvertRealmsTimer();
			CalcNextEnemyExcommunicateTime(resetCooldown: true);
			RemoveInvalidCardinals();
			InitCardinals();
			head.NotifyMasters("became_pope", head);
		}
		game.religions.SendState<Religions.HeadKingdomsState>();
	}

	public override void OnHeadDied()
	{
		game.religions.NotifyListeners("pope_died");
		if (hq_kingdom.IsDefeated())
		{
			return;
		}
		Vars vars = new Vars(head);
		head.SetTitle("Pope");
		vars.Set("title", head.GetTitle());
		Character character = ChooseNewPope();
		if (character != null)
		{
			vars.Set("replacement", character);
			vars.Set("old_title", GetTitle(character));
			vars.Set("old_name", character.GetName());
			if (character.governed_castle != null)
			{
				vars.Set("governed_castle", character.governed_castle);
			}
			if (character.name_idx > 0)
			{
				vars.Set("old_name_idx", character.name_idx);
			}
			character.StopGoverning();
			SetHead(character);
			game.religions.FireEvent("new_pope_chosen", vars);
		}
		else
		{
			character = CreateHead(hq_kingdom);
			vars.Set("replacement", character);
			vars.Set("old_title", "Catholic.titles.Cleric");
			vars.Set("old_name", character.GetName());
			if (character.name_idx > 0)
			{
				vars.Set("old_name_idx", character.name_idx);
			}
			character.StopGoverning();
			SetHead(character);
			game.religions.FireEvent("new_pope_created", vars);
		}
		character.GetSpecialCourtKingdom()?.FireEvent("cleric_elected_pope", character);
	}

	public override void OnHQRealmChangedKingdom()
	{
		if (!game.IsAuthority())
		{
			return;
		}
		Kingdom kingdom = hq_realm.GetKingdom();
		Kingdom lastOwner = hq_realm.GetLastOwner();
		Timer.Stop(lastOwner, "restore_papacy_timeout");
		if (kingdom == hq_kingdom)
		{
			if (head == null)
			{
				Vars vars = new Vars();
				Kingdom force_kingdom = null;
				Kingdom exclude_kingdom = null;
				switch (restore_papacy_reason)
				{
				case "restore":
				case "action":
					exclude_kingdom = lastOwner;
					break;
				case "puppet":
					force_kingdom = lastOwner;
					break;
				}
				Character character = ChooseNewPope(force_kingdom, exclude_kingdom);
				if (character == null)
				{
					character = CreateHead(hq_kingdom);
					vars.Set("old_title", "Catholic.titles.Cleric");
				}
				else
				{
					vars.Set("old_title", GetTitle(character));
				}
				vars.Set("old_name", character.GetName());
				if (character.name_idx > 0)
				{
					vars.Set("old_name_idx", character.name_idx);
				}
				vars.Set("pope", character);
				vars.Set("kingdom", lastOwner);
				vars.Set("old_kingdom", character.GetKingdom());
				SetHead(character);
				InitCardinals();
				game.religions.FireEvent("papacy_restored", vars);
			}
		}
		else if (head == null)
		{
			game.religions.FireEvent("papacy_lost", lastOwner);
			AskRestorePapacy();
		}
		else
		{
			DestroyPapacy(kingdom);
			UnExcommunicateAll();
			Vars vars = new Vars(head.GetName());
			if (head.name_idx > 0)
			{
				vars.Set("name_idx", head.name_idx);
			}
			vars.Set("old_title", GetTitle(head));
			vars.Set("kingdom", kingdom);
			vars.Set("old_kingdom", head.GetKingdom());
			Character character2 = head;
			SetHead(null);
			character2.Die(new DeadStatus("killed", character2));
			head_kingdom = kingdom;
			game.religions.SendState<Religions.HeadKingdomsState>();
			game.religions.FireEvent("papacy_destroyed", vars);
		}
	}

	public void DestroyPapacy(Kingdom destroyedBy)
	{
		if (hq_kingdom == null)
		{
			return;
		}
		game.TryDeclareIndependence(hq_kingdom.realms);
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom = game.kingdoms[i];
			if (kingdom != null && !kingdom.IsDefeated() && kingdom != destroyedBy && kingdom != hq_kingdom && kingdom.is_catholic && !kingdom.excommunicated)
			{
				destroyedBy.AddRelationModifier(kingdom, "rel_destroyed_papacy", null);
			}
		}
		if (crusade != null)
		{
			crusade.end_reason = "papacy_destroyed";
			crusade.End();
		}
	}

	public void UnExcommunicate(Kingdom k)
	{
		if (k.excommunicated)
		{
			k.excommunicated = false;
			if (!k.IsDefeated() && k.is_catholic)
			{
				k.SendState<Kingdom.ReligionState>();
				Religion.RefreshModifiers(k);
				k.FireEvent("religion_changed", null);
				k.FireEvent("unexcommunicated", null);
			}
		}
	}

	public void UnExcommunicateAll()
	{
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom k = game.kingdoms[i];
			UnExcommunicate(k);
		}
	}

	private void AskRestorePapacy()
	{
		Kingdom kingdom = hq_realm.GetKingdom();
		if (kingdom.is_player)
		{
			Timer.Start(kingdom, "restore_papacy_timeout", def.field.GetFloat("restore_papacy_timeout"));
			game.religions.FireEvent("ask_restore_papacy", kingdom);
		}
		else
		{
			AIDecideRestorePapacy(kingdom);
		}
	}

	public void AIDecideRestorePapacy(Kingdom k)
	{
		if (game.IsAuthority())
		{
			if (k.is_catholic)
			{
				RestorePapacyAnswer("restore");
			}
			else
			{
				RestorePapacyAnswer("keep");
			}
		}
	}

	public void RestorePapacyAnswer(string answer)
	{
		if (!game.IsAuthority())
		{
			game.religions.SendEvent(new Religions.RestorePapcyEvent(answer));
			return;
		}
		Kingdom kingdom = hq_realm.GetKingdom();
		Timer.Stop(kingdom, "restore_papacy_timeout");
		if (!(answer == "restore"))
		{
			if (answer == "puppet")
			{
				RestorePapacy("puppet");
			}
			return;
		}
		Action action = kingdom.actions.Find("RestorePapacyAction");
		if (action != null && action.Validate() == "ok")
		{
			action.Execute(null);
		}
		else
		{
			RestorePapacy("restore");
		}
	}

	public void RestorePapacy(string reason)
	{
		Kingdom kingdom = hq_realm.GetKingdom();
		restore_papacy_reason = reason;
		hq_realm.SetKingdom(hq_kingdom.id);
		hq_realm.SetDisorder(value: false);
		kingdom.NotifyListeners("restored_papacy");
		restore_papacy_reason = null;
		for (int i = 0; i < game.kingdoms.Count; i++)
		{
			Kingdom kingdom2 = game.kingdoms[i];
			if (kingdom2 != null && !kingdom2.IsDefeated() && kingdom2 != kingdom && kingdom2.is_catholic && !kingdom2.excommunicated)
			{
				kingdom.AddRelationModifier(kingdom2, "rel_restored_papacy", null);
			}
		}
	}

	public override void OnUpdate()
	{
		if (head == null || !head.IsValid() || !head.IsAuthority() || game.kingdoms == null || game.kingdoms.Count == 0)
		{
			return;
		}
		if (kingdom_to_update > game.kingdoms.Count)
		{
			kingdom_to_update = 0;
		}
		Kingdom kingdom = game.kingdoms[kingdom_to_update];
		if (kingdom != null && !kingdom.IsDefeated())
		{
			ThinkAboutKingdom(kingdom);
			if (++think_type <= 3)
			{
				return;
			}
			think_type = 0;
		}
		kingdom_to_update = (kingdom_to_update + 1) % game.kingdoms.Count;
	}

	private void InitCardinals()
	{
		StartCardinalsUpdateTimer(GetCardinalsUpdateTimer());
		if (hq_kingdom == null || hq_kingdom.IsDefeated())
		{
			return;
		}
		hq_kingdom?.InitCourt();
		int num = game.Random(min_cardinals_min, min_cardinals_max + 1);
		while (cardinals.Count < num)
		{
			Character character = CreateCardinal(hq_kingdom);
			if (character != null)
			{
				AddCardinal(character);
				continue;
			}
			break;
		}
	}

	private float GetCardinalsGameStartUpdateTimer()
	{
		return game.Random(min_cardinals_game_start_update, max_cardinals_game_start_update);
	}

	private float GetCardinalsUpdateTimer()
	{
		return game.Random(min_cardinals_update_interval, max_cardinals_update_interval);
	}

	private void StartCardinalsUpdateTimer(float time)
	{
		Timer.Start(game.religions, "update_cardinals", time, restart: true);
	}

	public void UpdateCardinals()
	{
		StartCardinalsUpdateTimer(GetCardinalsUpdateTimer());
		if (hq_kingdom != null && hq_kingdom.IsAuthority() && !hq_kingdom.IsDefeated())
		{
			ConsiderNewCardinal();
		}
	}

	public void RemoveInvalidCardinals()
	{
		bool flag = false;
		for (int num = cardinals.Count - 1; num >= 0; num--)
		{
			Character character = cardinals[num];
			string validate_result;
			float num2 = EvaluateCardinalElect(character, out validate_result, ignore_rel: true, ignore_class_level: true);
			if (!(validate_result == "ok"))
			{
				flag = true;
				cardinals.Remove(character);
				if (character.IsAlive())
				{
					if (character.title == "Cardinal" && num2 <= -100f)
					{
						character.SetTitle(null);
						character.FireEvent("no_longer_cardinal", validate_result);
					}
					else
					{
						character.FireEvent("no_longer_cardinal_elect", validate_result);
					}
				}
			}
		}
		if (flag)
		{
			game.religions.SendState<Religions.CharactersState>();
			game.religions.NotifyListeners("cardinals_changed");
		}
	}

	public static string ValidateCardinal(Character c)
	{
		if (c == null)
		{
			return null;
		}
		if (!c.IsAlive())
		{
			return "dead";
		}
		if (!c.IsCleric())
		{
			return "not_a_cleric";
		}
		if (c.IsKing())
		{
			return "king";
		}
		if (c.IsRebel())
		{
			return "rebel";
		}
		if (c.IsPope())
		{
			return "pope";
		}
		if (c.IsMarried())
		{
			return "married";
		}
		Kingdom kingdom = c.GetKingdom();
		if (kingdom == null || kingdom.IsDefeated())
		{
			return "no_kingdom";
		}
		if (!kingdom.is_catholic)
		{
			return "not_cathiolic";
		}
		if (kingdom.excommunicated)
		{
			return "excommunicated";
		}
		Kingdom kingdom2 = kingdom.game.religions.catholic.hq_kingdom;
		if (kingdom2 == null || kingdom2.IsDefeated())
		{
			return "no_papacy";
		}
		if (kingdom2.IsEnemy(kingdom))
		{
			return "enemy_of_papacy";
		}
		return "ok";
	}

	public float EvaluateCardinalElect(Character c, out string validate_result, bool ignore_rel = false, bool ignore_class_level = false)
	{
		validate_result = ValidateCardinal(c);
		if (validate_result != "ok")
		{
			return -100f;
		}
		Kingdom kingdom = c.GetKingdom();
		if (kingdom == null || kingdom.IsDefeated())
		{
			validate_result = "no_kingdom";
			return -9f;
		}
		ignore_class_level &= kingdom == hq_kingdom;
		int num = c.GetClassLevel();
		if (num < min_cardinal_class_level && !ignore_class_level)
		{
			validate_result = "low_class_level";
			return -10f;
		}
		if (ignore_class_level && num <= 0)
		{
			num = 1;
		}
		float num2;
		if (kingdom == hq_kingdom)
		{
			num2 = eval_papacy_cardinal_own_relation;
		}
		else
		{
			int num3 = (c.IsCardinalElect() ? min_cardinal_relationship_with_papacy_min : min_cardinal_relationship_with_papacy_max);
			num2 = hq_kingdom.GetRelationship(kingdom);
			if (num2 < (float)num3)
			{
				validate_result = "bad_relations";
				return -1f;
			}
		}
		int maxClassLevel = c.GetMaxClassLevel();
		float num4 = (float)num / (float)maxClassLevel;
		return num2 * num4;
	}

	public static void CheckCardinalTitle(Character c)
	{
		if (c == null)
		{
			return;
		}
		if (!c.IsAlive())
		{
			c.game?.religions?.catholic?.DelCardinal(c, null);
		}
		else if (!(c.title != "Cardinal"))
		{
			string text = ValidateCardinal(c);
			if (!(text == "ok"))
			{
				c.SetTitle(null);
				c.RefreshTags();
				c.FireEvent("no_longer_cardinal", text);
				c.game.religions.catholic.DelCardinal(c, null);
			}
		}
	}

	public static void CheckCardinalTitles(Kingdom k)
	{
		for (int i = 0; i < k.court.Count; i++)
		{
			CheckCardinalTitle(k.court[i]);
		}
	}

	private Character GetWorstCardinal()
	{
		if (cardinals.Count < max_cardinals)
		{
			return null;
		}
		Character character = null;
		float num = float.MaxValue;
		for (int num2 = cardinals.Count - 1; num2 >= 0; num2--)
		{
			Character character2 = cardinals[num2];
			string validate_result;
			float num3 = EvaluateCardinalElect(character2, out validate_result, ignore_rel: true, ignore_class_level: true);
			if (num3 < 0f)
			{
				DelCardinal(character2, validate_result);
				character = null;
				num = 0f;
			}
			else if (num3 < num)
			{
				character = character2;
				num = num3;
			}
		}
		if (character == null)
		{
			return null;
		}
		if (character.GetKingdom() == hq_kingdom || game.Random(0f, 100f) < demote_cardinal_chance)
		{
			return character;
		}
		return null;
	}

	private void ConsiderNewCardinal()
	{
		if (hq_kingdom == null || hq_kingdom.IsDefeated())
		{
			return;
		}
		Character character = null;
		if (cardinals.Count >= max_cardinals)
		{
			character = GetWorstCardinal();
			if (character == null)
			{
				return;
			}
		}
		Character character2 = null;
		using (Game.Profile("Choose New Cardinal"))
		{
			WeightedRandom<Character> temp = WeightedRandom<Character>.GetTemp();
			for (int i = 0; i < 3; i++)
			{
				for (int j = 1; j < game.kingdoms.Count; j++)
				{
					Kingdom kingdom = game.kingdoms[j];
					if (kingdom?.court == null || kingdom.IsDefeated() || !kingdom.is_catholic || kingdom.excommunicated)
					{
						continue;
					}
					if (kingdom != hq_kingdom)
					{
						float relationship = hq_kingdom.GetRelationship(kingdom);
						if (i == 0 && relationship < (float)min_cardinal_relationship_with_papacy_max)
						{
							continue;
						}
					}
					for (int k = 0; k < kingdom.court.Count; k++)
					{
						Character character3 = kingdom.court[k];
						if (character3 != null && !character3.IsCardinalElect())
						{
							string validate_result;
							float weight = EvaluateCardinalElect(character3, out validate_result, i > 0, i > 1);
							temp.AddOption(character3, weight);
						}
					}
				}
				if (temp.options.Count > 0)
				{
					break;
				}
			}
			character2 = temp.Choose();
		}
		if (character2 == null)
		{
			if (cardinals.Count >= min_cardinals_min)
			{
				return;
			}
			character2 = CreateCardinal(hq_kingdom);
		}
		if (character != null)
		{
			if (EvaluateCardinalElect(character2, out var _) <= EvaluateCardinalElect(character, out var _))
			{
				return;
			}
			DelCardinal(character, "demoted");
		}
		AddCardinal(character2);
	}

	public Character CreateCardinal(Kingdom k)
	{
		if (k == null)
		{
			return null;
		}
		if (k.GetFreeCourtSlotIndex() < 0)
		{
			return null;
		}
		Character character = CreateCharacter(k);
		if (character == null)
		{
			return null;
		}
		if (!character.IsInCourt())
		{
			k.AddCourtMember(character);
		}
		while (character.GetClassLevel() < min_cardinal_class_level && character.ThinkSkills(all: true, for_free: true))
		{
		}
		return character;
	}

	public void AddCardinal(Character c)
	{
		if (c != null)
		{
			cardinals.Add(c);
			if (c.title == null || c.title == "Knight")
			{
				c.SetTitle("Cardinal");
			}
			if (c.IsHeir())
			{
				c.GetKingdom().royalFamily.Unheir();
			}
			c.RefreshTags();
			game.religions.SendState<Religions.CharactersState>();
			game.religions.NotifyListeners("cardinals_changed");
			c.FireEvent("became_cardinal_elect", null);
		}
	}

	public void DelCardinal(Character c, string reason)
	{
		if (cardinals.Remove(c))
		{
			c.RefreshTags();
			game.religions.SendState<Religions.CharactersState>();
			game.religions.NotifyListeners("cardinals_changed");
			if (c.IsAlive() && c.IsCardinal())
			{
				c.FireEvent("no_longer_cardinal_elect", reason);
			}
		}
	}

	private void ThinkAboutKingdom(Kingdom k)
	{
		switch (think_type)
		{
		case 0:
			ConsiderExcommunicate(k);
			break;
		case 1:
			ConsiderCrusadeAgainst(k);
			break;
		case 2:
			ConsiderAskCrusadeFunds(k);
			break;
		case 3:
			ConsiderOfferCourtCleric(k);
			break;
		}
	}

	public void CalcNextEnemyExcommunicateTime(bool resetCooldown = false)
	{
		if (resetCooldown)
		{
			next_enemy_excommunicate = game.session_time;
			return;
		}
		DT.Field field = def.field.FindChild("enemy_excommunicate_cooldown");
		float num = 60f;
		if (field != null)
		{
			float num2 = field.Float(0, null, num);
			float num3 = field.Float(1);
			num = ((!(num3 > num2)) ? num2 : game.Random(num2, num3));
		}
		next_enemy_excommunicate = game.session_time + num;
		game.religions.SendState<Religions.CatholicCooldowns>();
	}

	public void CalcNextExcommunicateTime()
	{
		DT.Field field = def.field.FindChild("excommunicate_cooldown");
		float num = 60f;
		if (field != null)
		{
			float num2 = field.Float(0, null, num);
			float num3 = field.Float(1);
			num = ((!(num3 > num2)) ? num2 : game.Random(num2, num3));
		}
		next_excommunicate = game.session_time + num;
		game.religions.SendState<Religions.CatholicCooldowns>();
	}

	public void ConsiderExcommunicate(Kingdom k, bool ignore_cooldown = false)
	{
		Action action = head?.actions?.Find("ExcommunicateAction");
		if (action == null || !action.ValidateTarget(k))
		{
			return;
		}
		if (!ignore_cooldown)
		{
			if (game.session_time < next_excommunicate)
			{
				return;
			}
			if (next_excommunicate == Time.Zero)
			{
				CalcNextExcommunicateTime();
				return;
			}
		}
		ProsAndCons prosAndCons = ProsAndCons.Get("PC_Excommunicate", hq_kingdom, k);
		if (prosAndCons != null)
		{
			prosAndCons.Calc();
			if (!prosAndCons.CheckThreshold("propose"))
			{
				return;
			}
		}
		action.Execute(k);
	}

	public int GetExcommunicatedCount()
	{
		return excommunicated_count;
	}

	public void SetExcommunicatedCount(int value, bool send_state = true)
	{
		excommunicated_count = value;
		if (send_state)
		{
			game.religions.SendState<Religions.ExcommunicatedCountState>();
		}
	}

	public void Excommunicate(Kingdom k)
	{
		if (k.IsAuthority())
		{
			CalcNextExcommunicateTime();
			k.excommunicated = true;
			k.SendState<Kingdom.ReligionState>();
			CheckCardinalTitles(k);
			Religion.RefreshModifiers(k);
			k.FireEvent("excommunicated", null);
		}
	}

	public void ConsiderCrusadeAgainst(Kingdom k)
	{
		if (Crusade.IsOnCooldown(game, null) || Crusade.ValidateTarget(k, null) != "ok" || Crusade.ValidateNew(game, null) != "ok")
		{
			return;
		}
		ProsAndCons prosAndCons = ProsAndCons.Get("PC_Crusade", hq_kingdom, k);
		if (prosAndCons != null)
		{
			prosAndCons.Calc();
			if (!prosAndCons.CheckThreshold("propose"))
			{
				return;
			}
		}
		Crusade.Start(k, null);
	}

	private void CalcNextAskCrusadeFundsTime()
	{
		DT.Field field = def.field.FindChild("ask_crusade_funds_cooldown");
		float num = 60f;
		if (field != null)
		{
			float num2 = field.Float(0, null, 60f);
			float num3 = field.Float(1);
			num = ((!(num3 > num2)) ? num2 : game.Random(num2, num3));
		}
		next_ask_crusade_funds = game.session_time + num;
		game.religions.SendState<Religions.CatholicCooldowns>();
	}

	public void ConsiderAskCrusadeFunds(Kingdom k, bool ignore_cooldown = false)
	{
		if (!k.is_catholic)
		{
			return;
		}
		if (!ignore_cooldown)
		{
			if (game.session_time < next_ask_crusade_funds)
			{
				return;
			}
			if (next_ask_crusade_funds == Time.Zero)
			{
				CalcNextAskCrusadeFundsTime();
				return;
			}
		}
		Offer cachedOffer = Offer.GetCachedOffer("DemandFundCrusade", hq_kingdom, k);
		if (cachedOffer != null && OfferGenerator.instance.FillOfferArgs("propose", cachedOffer) && cachedOffer.Validate() == "ok")
		{
			cachedOffer.Send();
			CalcNextAskCrusadeFundsTime();
		}
	}

	private void CalcNextOfferCourtClericTime()
	{
		DT.Field field = def.field.FindChild("offer_court_cleric_cooldown");
		float num = 60f;
		if (field != null)
		{
			float num2 = field.Float(0, null, 60f);
			float num3 = field.Float(1);
			num = ((!(num3 > num2)) ? num2 : game.Random(num2, num3));
		}
		next_ask_court_cleric = game.session_time + num;
		game.religions.SendState<Religions.CatholicCooldowns>();
	}

	public void ConsiderOfferCourtCleric(Kingdom k, bool ignore_cooldown = false)
	{
		if (!k.is_catholic)
		{
			return;
		}
		if (!ignore_cooldown)
		{
			if (game.session_time < next_ask_court_cleric)
			{
				return;
			}
			if (next_ask_court_cleric == Time.Zero)
			{
				CalcNextOfferCourtClericTime();
				return;
			}
		}
		Offer cachedOffer = Offer.GetCachedOffer("OfferCourtCleric", hq_kingdom, k);
		if (cachedOffer != null && cachedOffer.Validate() == "ok" && cachedOffer.CheckThreshold("propose"))
		{
			cachedOffer.Send();
			CalcNextOfferCourtClericTime();
		}
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		base.DumpInnerState(dump, verbosity);
		dump.Append("holy_lands_realm", holy_lands_realm?.name);
		dump.Append("crusades_count", crusades_count);
		dump.Append("last_crusade_end", last_crusade_end.ToString());
	}
}
