using System;
using System.Collections.Generic;

namespace Logic;

public class GoOnAJourneyAction : CharacterOpportunity
{
	private static List<Realm> tmp_realms = new List<Realm>(128);

	public Realm starting_realm;

	private List<Object> last_possible_targets;

	private int last_seed;

	private Realm next_realm;

	private bool succeeded;

	public Realm target_realm => (base.target as Castle)?.GetRealm();

	public GoOnAJourneyAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new GoOnAJourneyAction(owner as Character, def);
	}

	public override bool NeedsTarget()
	{
		return true;
	}

	private int CalcSeed()
	{
		return base.own_character.GetNid(generateNid: false) + (int)(base.game.session_time.milliseconds / 60000);
	}

	private bool CachedPossibleTargetsValid()
	{
		if (last_possible_targets == null)
		{
			return false;
		}
		if (CalcSeed() != last_seed)
		{
			return false;
		}
		for (int i = 0; i < last_possible_targets.Count; i++)
		{
			Object obj = last_possible_targets[i];
			if (!ValidateTarget(obj))
			{
				return false;
			}
		}
		return true;
	}

	public override List<Object> GetPossibleTargets()
	{
		if (CachedPossibleTargetsValid())
		{
			return last_possible_targets;
		}
		List<Object> list = new List<Object>();
		Kingdom src_kingdom = own_kingdom;
		int minValue = def.field.GetInt("min_targets", this, 2);
		int num = def.field.GetInt("max_targets", this, 4);
		int min_dist = def.field.GetInt("min_tgt_dist", this, 5);
		int max_dist = def.field.GetInt("max_tgt_dist", this, 10);
		tmp_realms.Clear();
		Realm rStart = src_kingdom.realms[0];
		base.game.RealmWave(rStart, -1, delegate(Realm r, Realm realm2, int depth, object param, ref bool push_neighbors, ref bool stop)
		{
			Kingdom kingdom = r.GetKingdom();
			if (kingdom != src_kingdom)
			{
				r.wave_eval += 1f;
				if (src_kingdom.IsEnemy(kingdom))
				{
					push_neighbors = false;
				}
				else if (r.castle != null)
				{
					if (r.wave_eval > (float)max_dist)
					{
						push_neighbors = false;
					}
					else if (!(r.wave_eval < (float)min_dist))
					{
						tmp_realms.Add(r);
					}
				}
			}
		}, false, use_logic_neighbors: true);
		int seed = CalcSeed();
		last_possible_targets = list;
		last_seed = seed;
		if (tmp_realms.Count == 0)
		{
			return list;
		}
		Random random = new Random(seed);
		int num2 = random.Next(minValue, num + 1);
		if (num2 > tmp_realms.Count)
		{
			num2 = tmp_realms.Count;
		}
		for (int num3 = 0; num3 < num2; num3++)
		{
			int index = random.Next(0, tmp_realms.Count);
			Realm realm = tmp_realms[index];
			list.Add(realm.castle);
		}
		return list;
	}

	public override bool ValidateTarget(Object target)
	{
		Kingdom kingdom = own_kingdom;
		Realm realm = (target as Castle)?.GetRealm();
		if (realm == null)
		{
			return false;
		}
		Kingdom kingdom2 = realm.GetKingdom();
		if (kingdom2 == kingdom)
		{
			return false;
		}
		if (kingdom.IsEnemy(kingdom2))
		{
			return false;
		}
		return true;
	}

	private Realm TraceBack(Realm tgt_realm, Realm src_realm, Kingdom src_kingdom)
	{
		Realm result = tgt_realm;
		Realm wave_prev = tgt_realm.wave_prev;
		while (wave_prev != null && wave_prev != src_realm && (src_realm == null || wave_prev.GetKingdom() != src_kingdom))
		{
			result = wave_prev;
			wave_prev = wave_prev.wave_prev;
		}
		return result;
	}

	private Realm PickStartingRealm(Kingdom src_kingdom, Realm tgt_realm)
	{
		Realm realm = src_kingdom.realms[0];
		base.game.RealmWave(realm, -1, delegate(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
		{
			if (r.GetKingdom() != null && r == tgt_realm)
			{
				stop = true;
			}
		}, false, use_logic_neighbors: true);
		return TraceBack(tgt_realm, realm, src_kingdom);
	}

	private Realm PickNextRealm(Kingdom src_kingdom, Realm cur_realm, Realm tgt_realm)
	{
		if (cur_realm == null)
		{
			return PickStartingRealm(src_kingdom, tgt_realm);
		}
		base.game.RealmWave(cur_realm, -1, delegate(Realm r, Realm rStart, int depth, object param, ref bool push_neighbors, ref bool stop)
		{
			Kingdom kingdom = r.GetKingdom();
			if (kingdom != null)
			{
				if (kingdom == src_kingdom && r != rStart)
				{
					push_neighbors = false;
				}
				else if (r == tgt_realm)
				{
					stop = true;
				}
			}
		}, false, use_logic_neighbors: true);
		return TraceBack(tgt_realm, cur_realm, src_kingdom);
	}

	public override void OnEnterState(bool send_state = true)
	{
		if (state == State.Inactive && base.owner.IsAuthority())
		{
			if (base.own_character?.mission_realm != null)
			{
				base.own_character.SetMissionRealm(null);
			}
			starting_realm = null;
		}
		base.OnEnterState(send_state);
	}

	public override void Prepare()
	{
		if (base.own_character.mission_realm == null)
		{
			starting_realm = PickStartingRealm(own_kingdom, target_realm);
			base.own_character.SetMissionRealm(starting_realm);
		}
		base.Prepare();
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "cur_realm":
			return base.own_character.mission_realm;
		case "next_realm":
			return next_realm;
		case "tgt_realm":
			return target_realm;
		case "prepare_voice_line":
			if (base.own_character.mission_realm != starting_realm)
			{
				return Value.Null;
			}
			return base.GetVar(key, vars, as_value);
		default:
			return base.GetVar(key, vars, as_value);
		}
	}

	public override List<OutcomeDef> DecideOutcomes()
	{
		if (base.own_character.mission_realm == target_realm)
		{
			next_realm = null;
		}
		else
		{
			next_realm = PickNextRealm(own_kingdom, base.own_character.mission_realm, target_realm);
		}
		return base.DecideOutcomes();
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("next_realm", next_realm);
		outcome_vars.Set("mission_kingdom", base.own_character?.mission_realm?.GetKingdom());
	}

	public override void ApplyOutcomes()
	{
		succeeded = false;
		base.ApplyOutcomes();
		if (succeeded && next_realm != null)
		{
			base.own_character.SetMissionRealm(next_realm);
			SetState(State.Preparing);
		}
	}

	public override void Run()
	{
		succeeded = true;
		if (next_realm == null)
		{
			base.own_character?.NotifyListeners("journey_ended_success");
		}
		base.Run();
	}
}
