using System.Collections.Generic;

namespace Logic;

public class ConvertProvinceReligionAction : Action
{
	private Realm next_target;

	public ConvertProvinceReligionAction(Character owner, Def def)
		: base(owner, def)
	{
	}

	public new static Action Create(Object owner, Def def)
	{
		return new ConvertProvinceReligionAction(owner as Character, def);
	}

	public override bool ValidateTarget(Object target)
	{
		Kingdom kingdom = own_kingdom;
		Realm realm = target as Realm;
		if (kingdom == null || realm == null)
		{
			return false;
		}
		if (realm.GetKingdom() != kingdom && !kingdom.vassalStates.Contains(realm.GetKingdom()))
		{
			return false;
		}
		if (realm.religion == kingdom.religion)
		{
			return false;
		}
		if (realm.IsOccupied())
		{
			return false;
		}
		if (realm.IsDisorder())
		{
			return false;
		}
		if (realm.castle?.battle != null)
		{
			return false;
		}
		for (int i = 0; i < kingdom.court.Count; i++)
		{
			Character character = kingdom.court[i];
			if (character != null && character.cur_action is ConvertProvinceReligionAction { is_active: not false } convertProvinceReligionAction && convertProvinceReligionAction.target == target)
			{
				return false;
			}
		}
		return true;
	}

	public override Resource GetCost(Object target, IVars vars = null)
	{
		if (args != null && args.Count > 0)
		{
			return null;
		}
		return base.GetCost(target, vars);
	}

	public Realm SrcRealm()
	{
		if (args != null && args.Count > 0 && args[0].obj_val is Realm result)
		{
			return result;
		}
		return base.target as Realm;
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		if (key == "src_castle")
		{
			return SrcRealm()?.castle;
		}
		return base.GetVar(key, vars, as_value);
	}

	public override List<OutcomeDef> DecideOutcomes()
	{
		PickNextTarget();
		return base.DecideOutcomes();
	}

	public override bool ValidateOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (key == "move_to_next")
		{
			if (next_target == null)
			{
				return false;
			}
			return true;
		}
		return base.ValidateOutcome(outcome);
	}

	public override void CreateOutcomeVars()
	{
		base.CreateOutcomeVars();
		outcome_vars.Set("next_target", next_target);
	}

	public override bool ApplyOutcome(OutcomeDef outcome)
	{
		string key = outcome.key;
		if (!(key == "try_again"))
		{
			if (key == "move_to_next")
			{
				MoveToNext();
				return true;
			}
			return base.ApplyOutcome(outcome);
		}
		next_target = base.target as Realm;
		MoveToNext();
		return true;
	}

	public override void Run()
	{
		Kingdom kingdom = own_kingdom;
		Realm realm = base.target as Realm;
		if (kingdom != null && realm != null)
		{
			realm.SetReligion(kingdom.religion);
			base.own_character?.NotifyListeners("converted_realm", realm);
			base.Run();
			MoveToNext();
		}
	}

	private void PickNextTarget()
	{
		next_target = null;
		Kingdom k = own_kingdom;
		if (k == null)
		{
			return;
		}
		Realm cur_realm = base.target as Realm;
		if (cur_realm?.castle == null)
		{
			return;
		}
		Realm rStart = SrcRealm();
		int max_depth = def.field.GetInt("max_realm_dist", this, 4);
		int best_rdist = int.MaxValue;
		float best_d2 = float.MaxValue;
		base.game.RealmWave(rStart, max_depth, delegate(Realm r, Realm realm, int depth, object param, ref bool push_neighbors, ref bool stop)
		{
			Castle castle = r.castle;
			if (castle != null)
			{
				if (r.GetKingdom() != k)
				{
					push_neighbors = false;
				}
				else if (depth > best_rdist)
				{
					stop = true;
				}
				else if (ValidateTarget(r))
				{
					float num = castle.position.SqrDist(cur_realm.castle.position);
					if (depth < best_rdist || num < best_d2)
					{
						next_target = r;
						best_rdist = depth;
						best_d2 = num;
					}
				}
			}
		});
	}

	public void MoveToNext()
	{
		Realm realm = SrcRealm();
		List<Value> list = args;
		args = null;
		if (next_target == null)
		{
			return;
		}
		Cancel(manual: false, notify: false);
		string text = Validate();
		if (!(text != "ok") || !(text != "_in_progress"))
		{
			if (list == null)
			{
				list = new List<Value>(1);
			}
			if (list.Count == 0)
			{
				list.Add(realm.castle);
			}
			else
			{
				list[0] = realm.castle;
			}
			args = list;
			Execute(next_target);
			next_target = null;
		}
	}
}
