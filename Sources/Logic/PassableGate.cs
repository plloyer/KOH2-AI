using System.Collections.Generic;

namespace Logic;

public class PassableGate : MapObject
{
	public Battle battle;

	public MapObject owner;

	public int battle_side;

	public List<int> paids = new List<int>();

	public List<int> middle_paids = new List<int>();

	public List<Fortification> fortifications = new List<Fortification>();

	private bool _Open = true;

	private bool _Locked;

	public bool has_friendly_squad_under;

	public bool has_enemy_squad_under;

	public float health;

	public bool Open
	{
		get
		{
			return _Open;
		}
		set
		{
			if (_Open != value && (!value || !Locked))
			{
				_Open = value;
				ResetAreas();
				NotifyListeners("ToggleOpen");
			}
		}
	}

	public bool Locked
	{
		get
		{
			return _Locked;
		}
		set
		{
			if (_Locked != value)
			{
				_Locked = value;
				ResetAreas();
				NotifyListeners("ToggleLocked");
			}
		}
	}

	public PassableGate(Battle battle, Game game, PPos pos, MapObject owner, int battle_side)
		: base(game, pos, owner.GetKingdom().id)
	{
		this.battle = battle;
		this.owner = owner;
		this.battle_side = battle_side;
		battle.gates.Add(this);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (battle != null)
		{
			battle.gates.Remove(this);
		}
	}

	public void RefreshOpen()
	{
		Open = has_friendly_squad_under || has_enemy_squad_under;
	}

	public void ResetAreas()
	{
		for (int i = 0; i < paids.Count; i++)
		{
			PathData.PassableArea pA = game.path_finding.data.pointers.GetPA(paids[i] - 1);
			pA.battle_side = battle_side;
			if (Locked)
			{
				pA.NobodyCanEnter = true;
			}
			else if (Open)
			{
				pA.AnyoneCanEnter = true;
			}
			else
			{
				pA.FriendsCanEnter = true;
			}
			game.path_finding.data.pointers.SetPA(paids[i] - 1, pA);
		}
		if (Open)
		{
			return;
		}
		for (int j = 0; j < battle.squads.Count; j++)
		{
			Squad squad = battle.squads[j];
			if (squad.battle_side != battle_side && squad.movement.PlanningToMoveThroughPAIDs(paids))
			{
				squad.Stop();
			}
		}
	}

	public void SetHealth(float val)
	{
		health = val;
		OnHealthChanged();
	}

	public void OnHealthChanged()
	{
		if (IsDefeated())
		{
			Locked = false;
			Open = true;
			battle.fortification_destroyed = true;
		}
		NotifyListeners("Hit");
	}

	public void Toggle()
	{
		Open = !Open;
	}

	public void ToggleLock()
	{
		Locked = !Locked;
	}

	public override IRelationCheck GetStanceObj()
	{
		return null;
	}

	public bool IsDefeated()
	{
		Fortification fortification = MainFortification();
		if (fortification != null && fortification.IsDefeated())
		{
			return true;
		}
		return health <= 0f;
	}

	public Fortification MainFortification()
	{
		Fortification result = null;
		for (int i = 0; i < fortifications.Count; i++)
		{
			if (fortifications[i].def.type == Fortification.Type.Gate)
			{
				result = fortifications[i];
				break;
			}
		}
		return result;
	}
}
