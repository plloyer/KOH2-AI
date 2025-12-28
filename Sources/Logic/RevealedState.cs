using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.Pact)]
public class Pact : Object
{
	public class Def : Logic.Def
	{
		public int max_members = 4;

		public int max_pacts_against_target = 3;

		public override bool Load(Game game)
		{
			max_members = base.field.GetInt("max_members", null, max_members);
			max_pacts_against_target = base.field.GetInt("max_pacts_against_target", null, max_pacts_against_target);
			return true;
		}
	}

	public enum Type
	{
		Defensive,
		Offensive
	}

	public delegate Pact CreatePact(Type type, Character owner, Kingdom against);

	[Serialization.State(11)]
	public class InitState : Serialization.ObjectState
	{
		public string type;

		public NID owner;

		public NID target;

		public static InitState Create()
		{
			return new InitState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			Pact pact = obj as Pact;
			type = pact.type.ToString();
			owner = pact.owner;
			target = pact.target;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteStr(type, "type");
			ser.WriteNID<Character>(owner, "owner");
			ser.WriteNID<Kingdom>(target, "target");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			type = ser.ReadStr("type");
			owner = ser.ReadNID<Character>("owner");
			target = ser.ReadNID<Kingdom>("target");
		}

		public override void ApplyTo(Object obj)
		{
			Pact pact = obj as Pact;
			Type type = ((this.type == "Offensive") ? Type.Offensive : Type.Defensive);
			Character character = owner.Get<Character>(pact.game);
			Kingdom kingdom = target.Get<Kingdom>(pact.game);
			pact.Init(type, character, kingdom);
		}
	}

	[Serialization.State(12)]
	public class SupportersState : Serialization.ObjectState
	{
		public List<int> supporters;

		public static SupportersState Create()
		{
			return new SupportersState();
		}

		public static bool IsNeeded(Object obj)
		{
			if (!(obj is Pact pact))
			{
				return false;
			}
			if (pact.members.Count > 1)
			{
				return true;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Pact pact))
			{
				return false;
			}
			if (pact.members.Count <= 1)
			{
				return false;
			}
			supporters = new List<int>(pact.members.Count - 1);
			for (int i = 1; i < pact.members.Count; i++)
			{
				Kingdom kingdom = pact.members[i];
				supporters.Add(kingdom.id);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			int num = ((supporters != null) ? supporters.Count : 0);
			ser.Write7BitUInt(num, "supporters");
			for (int i = 0; i < num; i++)
			{
				int val = supporters[i];
				ser.Write7BitUInt(val, "supporter", i);
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("supporters");
			if (num > 0)
			{
				supporters = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					int item = ser.Read7BitUInt("supporter", i);
					supporters.Add(item);
				}
			}
		}

		private void SetSupporters(Pact pact, List<int> kids)
		{
			List<Kingdom> members = pact.members;
			for (int num = members.Count - 1; num >= 1; num--)
			{
				Kingdom kingdom = members[num];
				if (kids == null || !kids.Contains(kingdom.id))
				{
					pact.Leave(kingdom, null, send_state: false);
				}
			}
			if (kids == null)
			{
				return;
			}
			for (int i = 0; i < kids.Count; i++)
			{
				int kid = kids[i];
				Kingdom kingdom2 = pact.game.GetKingdom(kid);
				if (!members.Contains(kingdom2))
				{
					pact.Join(kingdom2, send_state: false);
				}
			}
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Pact pact)
			{
				SetSupporters(pact, supporters);
			}
		}
	}

	[Serialization.State(13)]
	public class RevealedState : Serialization.ObjectState
	{
		public bool revealed;

		public static RevealedState Create()
		{
			return new RevealedState();
		}

		public static bool IsNeeded(Object obj)
		{
			return true;
		}

		public override bool InitFrom(Object obj)
		{
			if (!(obj is Pact pact))
			{
				return false;
			}
			revealed = pact.revealed;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(revealed, "revealed");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			revealed = ser.ReadBool("revealed");
		}

		public override void ApplyTo(Object obj)
		{
			if (obj is Pact pact)
			{
				pact.SetRevealed(revealed);
			}
		}
	}

	[Serialization.Event(27)]
	public class LeavePactEvent : Serialization.ObjectEvent
	{
		private int kid;

		private string reason;

		public LeavePactEvent()
		{
		}

		public static LeavePactEvent Create()
		{
			return new LeavePactEvent();
		}

		public LeavePactEvent(Kingdom k, string reason = "")
		{
			kid = k.id;
			this.reason = reason;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(kid, "kingdom");
			ser.WriteStr(reason, "reason");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			kid = ser.Read7BitUInt("kid");
			reason = ser.ReadStr("reason");
		}

		public override void ApplyTo(Object obj)
		{
			Pact obj2 = obj as Pact;
			Kingdom kingdom = obj2.game.GetKingdom(kid);
			obj2.Leave(kingdom, string.IsNullOrEmpty(reason) ? null : reason);
		}
	}

	public Def def;

	public Type type;

	public Character owner;

	public Kingdom target;

	public List<Kingdom> members = new List<Kingdom>();

	public bool revealed;

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public Kingdom leader => members[0];

	public Pact(Type type, Character owner, Kingdom target)
		: base(owner?.game)
	{
		Init(type, owner, target);
	}

	public void Init(Type type, Character owner, Kingdom target)
	{
		this.type = type;
		def = game.defs.Get<Def>((type == Type.Defensive) ? "DefensivePact" : "OffensivePact");
		this.owner = owner;
		this.target = target;
		Kingdom kingdom = owner?.GetKingdom();
		if (kingdom == null)
		{
			return;
		}
		if (type == Type.Offensive)
		{
			SetRevealed(target.court.Find((Character c) => c != null && c.IsSpy() && !c.IsPrisoner() && c.IsAlive()) != null);
		}
		members.Add(kingdom);
		kingdom.AddPact(this);
		owner.SetPact(this);
		target?.AddPactAgainst(this);
	}

	public static Pact Create(Type type, Character diplomat, Kingdom against)
	{
		if (!CanCreate(type, diplomat, against))
		{
			return null;
		}
		Pact result = new Pact(type, diplomat, against);
		switch (type)
		{
		case Type.Offensive:
			diplomat.AddStatus(new HoldingAnInvasionPlanStatus(null));
			break;
		case Type.Defensive:
			diplomat.AddStatus(new HoldingADefensivePactStatus(null));
			break;
		default:
			Game.Log("Unrecognized plot type: " + type, Game.LogType.Error);
			break;
		}
		return result;
	}

	public void Dissolve(string reason = null)
	{
		NotifyListeners("dissolved");
		Vars vars = new Vars(this);
		if (reason != null)
		{
			vars.Set("reason", reason);
		}
		leader?.FireEvent("del_pact", vars);
		Destroy();
	}

	public override Kingdom GetKingdom()
	{
		return owner?.GetKingdom();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (owner != null)
		{
			owner.DelStatus<HoldingADefensivePactStatus>();
			owner.DelStatus<HoldingAnInvasionPlanStatus>();
			owner.SetPact(null);
			if (target != null)
			{
				target.DelPactAgainst(this);
			}
			for (int i = 0; i < members.Count; i++)
			{
				members[i].DelPact(this);
			}
		}
	}

	public bool Join(Kingdom k, bool send_state = true)
	{
		if (send_state && !CanJoin(k))
		{
			return false;
		}
		members.Add(k);
		k.AddPact(this);
		if (send_state)
		{
			SendState<SupportersState>();
		}
		NotifyListeners("supporter_joined", k);
		return true;
	}

	public bool Leave(Kingdom k, string reason = null, bool send_state = true)
	{
		if (send_state && !IsAuthority())
		{
			SendEvent(new LeavePactEvent(k));
			return false;
		}
		if (IsAuthority())
		{
			for (int i = 0; i < members.Count; i++)
			{
				Kingdom kingdom = members[i];
				if (kingdom != k)
				{
					k.AddRelationModifier(kingdom, "rel_they_left_our_pact", this);
				}
			}
		}
		if (k == leader)
		{
			Dissolve(reason);
			return true;
		}
		if (!members.Remove(k))
		{
			return false;
		}
		k.DelPact(this);
		if (send_state)
		{
			SendState<SupportersState>();
		}
		NotifyListeners("supporter_left", k);
		return true;
	}

	public static Pact Find(Type type, Kingdom k, Kingdom tgt_kingdom)
	{
		if (k?.pacts == null)
		{
			return null;
		}
		for (int i = 0; i < k.pacts.Count; i++)
		{
			Pact pact = k.pacts[i];
			if (pact.type == type && pact.target == tgt_kingdom)
			{
				return pact;
			}
		}
		return null;
	}

	public static bool CanCreate(Type type, Character diplomat, Kingdom against)
	{
		if (diplomat == null || against == null || against.IsDefeated())
		{
			return false;
		}
		if (!diplomat.IsDiplomat())
		{
			return false;
		}
		if (diplomat.pact != null)
		{
			return false;
		}
		Kingdom kingdom = diplomat?.GetKingdom();
		if (kingdom == null || kingdom.IsDefeated())
		{
			return false;
		}
		if (kingdom == against)
		{
			return false;
		}
		if (kingdom.IsEnemy(against))
		{
			return false;
		}
		if (!War.CanStart(kingdom, against))
		{
			return false;
		}
		if (Find(type, kingdom, against) != null)
		{
			return false;
		}
		int num = 0;
		int num2 = 3;
		if (type == Type.Defensive)
		{
			num2 = against.game.defs.Get<Def>("DefensivePact").max_pacts_against_target;
		}
		if (type == Type.Offensive)
		{
			num2 = against.game.defs.Get<Def>("OffensivePact").max_pacts_against_target;
		}
		foreach (Pact item in against.pacts_against)
		{
			if (item.type == type)
			{
				num++;
			}
			if (num >= num2)
			{
				return false;
			}
		}
		return true;
	}

	public bool CanJoin(Kingdom k)
	{
		if (k == null || k.IsDefeated() || k == target)
		{
			return false;
		}
		if (k.IsEnemy(target))
		{
			return false;
		}
		if (!War.CanStart(k, target))
		{
			return false;
		}
		int num = MaxMembers();
		if (num > 0 && members.Count >= num)
		{
			return false;
		}
		for (int i = 0; i < members.Count; i++)
		{
			Kingdom kingdom = members[i];
			if (k == kingdom)
			{
				return false;
			}
			if (k.IsEnemy(kingdom))
			{
				return false;
			}
		}
		if (Find(type, k, target) != null)
		{
			return false;
		}
		return true;
	}

	public void Activate(War war)
	{
		NotifyListeners("activated");
		Destroy();
		bool flag = type == Type.Offensive;
		int side = ((!flag) ? 1 : 0);
		for (int i = 0; i < members.Count; i++)
		{
			Kingdom k = members[i];
			if (war.CanJoin(k, side))
			{
				war.Join(k, side, flag ? War.InvolvementReason.OffensivePactActivated : War.InvolvementReason.DefensivePactActivated, type == Type.Offensive);
			}
		}
	}

	public void SetRevealed(bool revealed = true, bool send_state = true)
	{
		if (type == Type.Offensive)
		{
			this.revealed = revealed;
			if (send_state)
			{
				SendState<RevealedState>();
			}
		}
	}

	public int MaxMembers()
	{
		if (def != null)
		{
			return def.max_members;
		}
		return -1;
	}

	public bool IsVisibleBy(Kingdom k, Character ignore_spy = null)
	{
		if (type == Type.Defensive)
		{
			return true;
		}
		if (k == null)
		{
			return false;
		}
		if (members.Contains(k))
		{
			return true;
		}
		if (k == target && revealed)
		{
			return true;
		}
		return false;
	}

	public bool IsMember(Kingdom k)
	{
		if (leader == k)
		{
			return true;
		}
		return members.Contains(k);
	}

	public static void RevealPacts(Character spy)
	{
		if (spy == null || !spy.IsSpy() || !spy.IsAuthority() || spy.IsDead() || spy.IsPrisoner() || spy.IsRebel())
		{
			return;
		}
		Kingdom kingdom = spy?.GetKingdom();
		if (kingdom?.court == null || kingdom.court.Find((Character s) => s != null && s != spy && s.IsSpy() && s.IsAlive() && !s.IsPrisoner()) != null)
		{
			return;
		}
		for (int num = 0; num < kingdom.pacts_against.Count; num++)
		{
			Pact pact = kingdom.pacts_against[num];
			if (pact != null && pact.type == Type.Offensive && !pact.revealed)
			{
				pact.SetRevealed();
				kingdom.FireEvent("reveal_pact", new Vars(pact));
			}
		}
	}

	public static void ConcealPacts(Character spy)
	{
		if (spy == null || spy.IsRebel())
		{
			return;
		}
		Kingdom kingdom = spy?.GetKingdom();
		if (kingdom?.court == null || kingdom.court.Find((Character s) => s != null && s != spy && s.IsSpy() && !s.IsPrisoner()) != null)
		{
			return;
		}
		for (int num = 0; num < kingdom.pacts_against.Count; num++)
		{
			Pact pact = kingdom.pacts_against[num];
			if (pact != null && pact.type == Type.Offensive)
			{
				kingdom.NotifyListeners("conceal_pact", new Vars(pact));
			}
		}
	}

	public override string GetNameKey(IVars vars = null, string form = "")
	{
		return $"{type}Pact.name";
	}

	public override Value GetVar(string key, IVars vars = null, bool as_value = true)
	{
		switch (key)
		{
		case "def":
			return def;
		case "type":
			return type.ToString();
		case "owner":
			return owner;
		case "target":
		case "tgt_kingdom":
			return target;
		case "leader":
		case "src_kingdom":
			return leader;
		case "members":
			return new Value(members);
		case "has_supporters":
			return members.Count > 1;
		default:
			return Value.Unknown;
		}
	}

	public override string ToString()
	{
		string text = $"{type} Pact of {owner}: ";
		for (int i = 0; i < members.Count; i++)
		{
			Kingdom kingdom = members[i];
			if (i > 0)
			{
				text += ", ";
			}
			text += kingdom.Name;
		}
		return text + " vs " + target.Name;
	}

	public Pact(Multiplayer multiplayer)
		: base(multiplayer)
	{
	}

	public static Object Create(Multiplayer multiplayer)
	{
		return new Pact(multiplayer);
	}

	public override void Load(Serialization.ObjectStates states)
	{
		base.Load(states);
	}
}
