using System;
using System.Collections.Generic;

namespace Logic;

[Serialization.Object(Serialization.ObjectType.EmperorOfTheWorld, dynamic = false)]
public class EmperorOfTheWorld : Object
{
	public enum Slant
	{
		VeryAgainst,
		Against,
		SlightlyAgainst,
		SlightlyFor,
		For,
		VeryFor
	}

	public class Def : Logic.Def
	{
		public float auto_vote_min_time = 3600f;

		public float auto_vote_max_time = 7200f;

		public float auto_vote_battleview_dalay_time = 300f;

		public int min_great_powers = 5;

		public float prepare_duration = 10f;

		public float player_action_time_out = 30f;

		public float ai_action_time_out = 15f;

		public float ai_think_delay = 5f;

		public float fame_treshold_single_cadidate = 0.66f;

		public float fame_treshold_multiple_candidates = 0.66f;

		public float desired_pro_points = 50f;

		public float desired_con_points = 20f;

		public float player_fame_perc_bonus = 10f;

		public float vote_weight_precision = 5f;

		private float max_negative_weight;

		private float max_positive_weight;

		public List<float> slant_tresholds = new List<float>();

		public float notify_gained_lost_support_interval_time = 900f;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			auto_vote_min_time = field.GetFloat("auto_vote_min_time");
			auto_vote_max_time = field.GetFloat("auto_vote_max_time");
			auto_vote_battleview_dalay_time = field.GetFloat("auto_vote_battleview_dalay_time");
			min_great_powers = field.GetInt("min_great_powers");
			prepare_duration = field.GetFloat("prepare_duration");
			player_action_time_out = field.GetFloat("player_action_time_out");
			ai_action_time_out = field.GetFloat("ai_action_time_out");
			ai_think_delay = field.GetFloat("ai_think_delay");
			fame_treshold_single_cadidate = field.GetFloat("fame_treshold_single_cadidate");
			fame_treshold_multiple_candidates = field.GetFloat("fame_treshold_multiple_candidates");
			desired_pro_points = field.GetFloat("desired_pro_points");
			desired_con_points = field.GetFloat("desired_con_points");
			player_fame_perc_bonus = field.GetFloat("player_fame_perc_bonus");
			vote_weight_precision = field.GetFloat("vote_weight_precision", null, 5f);
			notify_gained_lost_support_interval_time = field.GetFloat("notify_gained_lost_support_interval_time");
			LoadSlantTresholds(field);
			return true;
		}

		private void LoadSlantTresholds(DT.Field field)
		{
			DT.Field field2 = field.FindChild("slant_tresholds_perc_from_max_weight");
			if (field2 != null)
			{
				for (int i = 0; i < field2.NumValues(); i++)
				{
					slant_tresholds.Add(field2.Int(i));
				}
			}
		}
	}

	[Serialization.State(11)]
	public class ActiveState : Serialization.ObjectState
	{
		public bool active;

		public static ActiveState Create()
		{
			return new ActiveState();
		}

		public static bool IsNeeded(Object obj)
		{
			return (obj as EmperorOfTheWorld).isVotingActive;
		}

		public override bool InitFrom(Object obj)
		{
			EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
			active = emperorOfTheWorld.isVotingActive;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(active, "isVotingActive");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			active = ser.ReadBool("isVotingActive");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as EmperorOfTheWorld).SetVotingActive(active, send_state: false);
		}
	}

	[Serialization.State(12)]
	public class VotesState : Serialization.ObjectState
	{
		[Serialization.Substate(1)]
		public class VoteState : Serialization.ObjectSubstate
		{
			public int vote;

			public List<int> vote_strengths;

			public VoteState()
			{
			}

			public VoteState(int idx, int vote, List<int> vote_strengths)
			{
				substate_index = idx;
				this.vote = vote;
				this.vote_strengths = new List<int>(vote_strengths);
			}

			public static VoteState Create()
			{
				return new VoteState();
			}

			public static bool IsNeeded(Object obj)
			{
				return true;
			}

			public override bool InitFrom(Object obj)
			{
				EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
				if (emperorOfTheWorld.votes == null || emperorOfTheWorld.votes.Count == 0 || substate_index < 0 || substate_index >= emperorOfTheWorld.votes.Count)
				{
					return false;
				}
				vote = emperorOfTheWorld.votes[substate_index];
				vote_strengths = new List<int>(emperorOfTheWorld.votes_weights[substate_index]);
				return true;
			}

			public override void WriteBody(Serialization.IWriter ser)
			{
				ser.Write7BitSigned(vote, "vote");
				ser.Write7BitUInt(vote_strengths.Count, "count");
				for (int i = 0; i < vote_strengths.Count; i++)
				{
					ser.Write7BitSigned(vote_strengths[i], "vote_strength_", i);
				}
			}

			public override void ReadBody(Serialization.IReader ser)
			{
				vote = ser.Read7BitSigned("vote");
				int num = ser.Read7BitUInt("count");
				vote_strengths = new List<int>(num);
				for (int i = 0; i < num; i++)
				{
					vote_strengths.Add(ser.Read7BitSigned("vote_strength_", i));
				}
			}

			public override void ApplyTo(Object obj)
			{
				EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
				if (substate_index < 0 || emperorOfTheWorld.votes == null || substate_index >= emperorOfTheWorld.votes.Count)
				{
					Game.Log("Error applying vote state #" + substate_index + " / " + emperorOfTheWorld.votes.Count + " to " + vote_strengths, Game.LogType.Error);
				}
				else
				{
					emperorOfTheWorld.votes[substate_index] = vote;
					emperorOfTheWorld.votes_weights[substate_index].Clear();
					emperorOfTheWorld.votes_weights[substate_index].AddRange(new List<int>(vote_strengths));
				}
			}
		}

		public List<NID> candidates = new List<NID>();

		public List<NID> voters = new List<NID>();

		public int votesCount;

		public static VotesState Create()
		{
			return new VotesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
			for (int i = 0; i < emperorOfTheWorld.candidates.Count; i++)
			{
				candidates.Add(emperorOfTheWorld.candidates[i]);
			}
			for (int j = 0; j < emperorOfTheWorld.voters.Count; j++)
			{
				voters.Add(emperorOfTheWorld.voters[j]);
			}
			for (int k = 0; k < emperorOfTheWorld.votes.Count; k++)
			{
				AddSubstate(new VoteState(k, emperorOfTheWorld.votes[k], emperorOfTheWorld.votes_weights[k]));
			}
			votesCount = emperorOfTheWorld.votes.Count;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(candidates.Count, "candidates_count");
			for (int i = 0; i < candidates.Count; i++)
			{
				ser.WriteNID<Kingdom>(candidates[i], "candidate", i);
			}
			ser.Write7BitUInt(voters.Count, "voters_count");
			for (int j = 0; j < voters.Count; j++)
			{
				ser.WriteNID<Kingdom>(voters[j], "voter", j);
			}
			ser.Write7BitUInt(voters.Count, "votes_count");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("candidates_count");
			for (int i = 0; i < num; i++)
			{
				candidates.Add(ser.ReadNID<Kingdom>("candidate", i));
			}
			int num2 = ser.Read7BitUInt("voters_count");
			for (int j = 0; j < num2; j++)
			{
				voters.Add(ser.ReadNID<Kingdom>("voter", j));
			}
			votesCount = ser.Read7BitUInt("votes_count");
		}

		public override void ApplyTo(Object obj)
		{
			EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
			emperorOfTheWorld.candidates.Clear();
			emperorOfTheWorld.voters.Clear();
			emperorOfTheWorld.votes.Clear();
			emperorOfTheWorld.votes_weights.Clear();
			for (int i = 0; i < candidates.Count; i++)
			{
				emperorOfTheWorld.candidates.Add(candidates[i].Get<Kingdom>(emperorOfTheWorld.game));
			}
			for (int j = 0; j < voters.Count; j++)
			{
				emperorOfTheWorld.voters.Add(voters[j].Get<Kingdom>(emperorOfTheWorld.game));
			}
			for (int k = 0; k < votesCount; k++)
			{
				emperorOfTheWorld.votes.Add(-1);
				emperorOfTheWorld.votes_weights.Add(new List<int>());
			}
		}
	}

	[Serialization.State(13)]
	public class VoteVariablesState : Serialization.ObjectState
	{
		private bool isVotingEnded;

		private bool isEmperorChosen;

		public static VoteVariablesState Create()
		{
			return new VoteVariablesState();
		}

		public static bool IsNeeded(Object obj)
		{
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
			isVotingEnded = emperorOfTheWorld.isVotingEnded;
			isEmperorChosen = emperorOfTheWorld.isEmperorChosen;
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(isVotingEnded, "isVotingEnded");
			ser.WriteBool(isEmperorChosen, "isEmperorChosen");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			isVotingEnded = ser.ReadBool("isVotingEnded");
			isEmperorChosen = ser.ReadBool("isEmperorChosen");
		}

		public override void ApplyTo(Object obj)
		{
			EmperorOfTheWorld obj2 = obj as EmperorOfTheWorld;
			obj2.isVotingEnded = isVotingEnded;
			obj2.isEmperorChosen = isEmperorChosen;
		}
	}

	[Serialization.State(14)]
	public class NotificatorState : Serialization.ObjectState
	{
		private Dictionary<NID, List<NID>> previouslySupporting;

		public static NotificatorState Create()
		{
			return new NotificatorState();
		}

		public static bool IsNeeded(Object obj)
		{
			EmperorOftheWorldSupportNotificator emperorOftheWorldSupportNotificator = obj?.GetComponent<EmperorOftheWorldSupportNotificator>();
			if (emperorOftheWorldSupportNotificator != null)
			{
				return emperorOftheWorldSupportNotificator.previouslySupporting.Count != 0;
			}
			return false;
		}

		public override bool InitFrom(Object obj)
		{
			EmperorOftheWorldSupportNotificator emperorOftheWorldSupportNotificator = (obj as EmperorOfTheWorld)?.GetComponent<EmperorOftheWorldSupportNotificator>();
			if (emperorOftheWorldSupportNotificator == null)
			{
				return false;
			}
			if (emperorOftheWorldSupportNotificator.previouslySupporting == null || emperorOftheWorldSupportNotificator.previouslySupporting.Count == 0)
			{
				return false;
			}
			foreach (KeyValuePair<Kingdom, List<Kingdom>> item in emperorOftheWorldSupportNotificator.previouslySupporting)
			{
				if (previouslySupporting == null)
				{
					previouslySupporting = new Dictionary<NID, List<NID>>();
				}
				List<NID> list = new List<NID>();
				for (int i = 0; i < item.Value.Count; i++)
				{
					list.Add(item.Value[i]);
				}
				previouslySupporting.Add(item.Key, list);
			}
			return true;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(previouslySupporting.Count, "count_supporters");
			int num = 0;
			foreach (KeyValuePair<NID, List<NID>> item in previouslySupporting)
			{
				ser.WriteNID<Kingdom>(item.Key, "supporter_", num);
				List<NID> value = item.Value;
				ser.Write7BitUInt(value.Count, "count_", num);
				for (int i = 0; i < value.Count; i++)
				{
					ser.WriteNID<Kingdom>(value[i], $"plr_{num}_", i);
				}
				num++;
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			int num = ser.Read7BitUInt("count_supporters");
			for (int i = 0; i < num; i++)
			{
				NID key = ser.ReadNID<Kingdom>("supporter_", i);
				int num2 = ser.Read7BitUInt("count_", i);
				List<NID> list = new List<NID>();
				if (previouslySupporting == null)
				{
					previouslySupporting = new Dictionary<NID, List<NID>>();
				}
				for (int j = 0; j < num2; j++)
				{
					list.Add(ser.ReadNID<Kingdom>($"plr_{i}_", j));
				}
				previouslySupporting.Add(key, list);
			}
		}

		public override void ApplyTo(Object obj)
		{
			EmperorOfTheWorld emperorOfTheWorld = obj as EmperorOfTheWorld;
			EmperorOftheWorldSupportNotificator emperorOftheWorldSupportNotificator = emperorOfTheWorld?.GetComponent<EmperorOftheWorldSupportNotificator>();
			if (emperorOftheWorldSupportNotificator == null || emperorOftheWorldSupportNotificator.previouslySupporting == null || emperorOftheWorldSupportNotificator.previouslySupporting.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<NID, List<NID>> item in previouslySupporting)
			{
				emperorOftheWorldSupportNotificator.previouslySupporting = new Dictionary<Kingdom, List<Kingdom>>();
				List<Kingdom> list = new List<Kingdom>();
				for (int i = 0; i < item.Value.Count; i++)
				{
					list.Add(item.Value[i].Get<Kingdom>(emperorOfTheWorld.game));
				}
				emperorOftheWorldSupportNotificator.previouslySupporting.Add(item.Key.Get<Kingdom>(emperorOfTheWorld.game), list);
			}
		}
	}

	[Serialization.Event(27)]
	public class VoteEvent : Serialization.ObjectEvent
	{
		private int voterIdx;

		private int vote;

		public VoteEvent()
		{
		}

		public static VoteEvent Create()
		{
			return new VoteEvent();
		}

		public VoteEvent(int voterIdx, int vote)
		{
			this.voterIdx = voterIdx;
			this.vote = vote;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.Write7BitUInt(voterIdx, "voterIdx");
			ser.Write7BitSigned(vote, "vote");
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			voterIdx = ser.Read7BitUInt("voterIdx");
			vote = ser.Read7BitSigned("vote");
		}

		public override void ApplyTo(Object obj)
		{
			(obj as EmperorOfTheWorld).SetPlayerVote(voterIdx, vote);
		}
	}

	[Serialization.Event(28)]
	public class StartVoteEvent : Serialization.ObjectEvent
	{
		private bool has_forced_kingdom;

		private NID kingdom_nid;

		public StartVoteEvent()
		{
		}

		public static StartVoteEvent Create()
		{
			return new StartVoteEvent();
		}

		public StartVoteEvent(Kingdom k)
		{
			Game.Log("StartVoteEvent Constructor Start", Game.LogType.Message);
			has_forced_kingdom = k != null;
			kingdom_nid = k;
			Game.Log("StartVoteEvent Constructor End", Game.LogType.Message);
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			Game.Log("StartVoteEvent WriteBody Start", Game.LogType.Message);
			ser.WriteBool(has_forced_kingdom, "has_forced_kingdom");
			if (has_forced_kingdom)
			{
				ser.WriteNID<Kingdom>(kingdom_nid, "kingdom_nid");
			}
			Game.Log("StartVoteEvent WriteBody End", Game.LogType.Message);
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			has_forced_kingdom = ser.ReadBool("has_forced_kingdom");
			if (has_forced_kingdom)
			{
				kingdom_nid = ser.ReadNID<Kingdom>("kingdom_nid");
			}
		}

		public override void ApplyTo(Object obj)
		{
			(obj as EmperorOfTheWorld).StartVote(has_forced_kingdom ? kingdom_nid.Get<Kingdom>(obj.game) : null);
		}
	}

	[Serialization.Event(29)]
	public class EndVotingEvent : Serialization.ObjectEvent
	{
		public EndVotingEvent()
		{
		}

		public static EndVotingEvent Create()
		{
			return new EndVotingEvent();
		}

		public EndVotingEvent(int voterIdx, int vote)
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
			(obj as EmperorOfTheWorld).SetEndVoting();
		}
	}

	[Serialization.Event(30)]
	public class SetEmperorOfTheWorldEvent : Serialization.ObjectEvent
	{
		private bool has_emperor;

		private NID kingdom_nid;

		public SetEmperorOfTheWorldEvent()
		{
		}

		public static SetEmperorOfTheWorldEvent Create()
		{
			return new SetEmperorOfTheWorldEvent();
		}

		public SetEmperorOfTheWorldEvent(Kingdom k)
		{
			has_emperor = k != null;
			kingdom_nid = k;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(has_emperor, "has_emperor");
			if (has_emperor)
			{
				ser.WriteNID<Kingdom>(kingdom_nid, "kingdom_nid");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			has_emperor = ser.ReadBool("has_emperor");
			if (has_emperor)
			{
				kingdom_nid = ser.ReadNID<Kingdom>("kingdom_nid");
			}
		}

		public override void ApplyTo(Object obj)
		{
			(obj as EmperorOfTheWorld).TrySetEmperorOfTheWorld(has_emperor ? kingdom_nid.Get<Kingdom>(obj.game) : null);
		}
	}

	[Serialization.Event(31)]
	public class RejectBeingEmperorEvent : Serialization.ObjectEvent
	{
		private bool has_emperor;

		private NID kingdom_nid;

		public RejectBeingEmperorEvent()
		{
		}

		public static RejectBeingEmperorEvent Create()
		{
			return new RejectBeingEmperorEvent();
		}

		public RejectBeingEmperorEvent(Kingdom k)
		{
			has_emperor = k != null;
			kingdom_nid = k;
		}

		public override void WriteBody(Serialization.IWriter ser)
		{
			ser.WriteBool(has_emperor, "has_emperor");
			if (has_emperor)
			{
				ser.WriteNID<Kingdom>(kingdom_nid, "kingdom_nid");
			}
		}

		public override void ReadBody(Serialization.IReader ser)
		{
			has_emperor = ser.ReadBool("has_emperor");
			if (has_emperor)
			{
				kingdom_nid = ser.ReadNID<Kingdom>("kingdom_nid");
			}
		}

		public override void ApplyTo(Object obj)
		{
			(obj as EmperorOfTheWorld).RejectBeingEmperor(has_emperor ? kingdom_nid.Get<Kingdom>(obj.game) : null);
		}
	}

	public Def def;

	public List<Kingdom> candidates;

	public List<Kingdom> voters;

	public List<int> votes;

	public List<List<int>> votes_weights;

	public static bool EOWEnabled = true;

	private bool isVotingActive;

	private bool isVotingEnded;

	private bool isEmperorChosen;

	private const int STATES_IDX = 10;

	private const int EVENTS_IDX = 26;

	public EmperorOfTheWorld(Game game)
		: base(game)
	{
	}

	public void Init(bool new_game)
	{
		if (nid == 0)
		{
			SetNid(1);
		}
		def = game.defs.Get<Def>("EmperorOfTheWorld");
		candidates = new List<Kingdom>(def.field.GetInt("max_candidates", null, 2));
		game.great_powers.MaxGreatPowers();
		voters = new List<Kingdom>();
		votes = new List<int>();
		votes_weights = new List<List<int>>();
		UpdateAfter(GetNextAutoVoteUpdateTime());
		new EmperorOftheWorldSupportNotificator(this);
	}

	public void Shutdown()
	{
		StopUpdating();
		candidates = null;
		voters = null;
		votes = null;
		isVotingActive = false;
		isVotingEnded = false;
		isEmperorChosen = false;
	}

	public override void OnUnloadMap()
	{
		SetNid(0, update_registry: false);
	}

	private float GetNextAutoVoteUpdateTime()
	{
		if (game.subgames.Count > 0)
		{
			return def.auto_vote_battleview_dalay_time;
		}
		return game.Random(def.auto_vote_min_time, def.auto_vote_max_time);
	}

	public bool IsVotingActive()
	{
		return isVotingActive;
	}

	private void CalcCandidates(Kingdom forcedKingdom)
	{
		candidates.Clear();
		if (forcedKingdom != null)
		{
			if (game.great_powers.TopKingdoms().Contains(forcedKingdom))
			{
				candidates.Add(forcedKingdom);
			}
		}
		else
		{
			List<Kingdom> list = game.great_powers.TopKingdoms();
			candidates.Add(list[0]);
			candidates.Add(list[1]);
		}
	}

	public bool ValidateVote(Kingdom forcedKingdom)
	{
		if (forcedKingdom != null)
		{
			if (!forcedKingdom.IsGreatPower())
			{
				return false;
			}
			if (game.IsPaused())
			{
				return false;
			}
			if (forcedKingdom.fame < forcedKingdom.required_fame_victory)
			{
				return false;
			}
			if (forcedKingdom.wars.Count > 0)
			{
				return false;
			}
		}
		if (game.great_powers.TopKingdoms().Count < def.min_great_powers)
		{
			return false;
		}
		return true;
	}

	public Slant CalcSlant(Kingdom voter, Kingdom candidate, int playerVote = -1)
	{
		float num = 100f * (float)CalcVoteWeight(voter, candidate, playerVote) / voter.fame;
		if (num < def.slant_tresholds[0])
		{
			return Slant.VeryAgainst;
		}
		if (num < def.slant_tresholds[1])
		{
			return Slant.Against;
		}
		if (num < def.slant_tresholds[2])
		{
			return Slant.SlightlyAgainst;
		}
		if (num < def.slant_tresholds[3])
		{
			return Slant.SlightlyFor;
		}
		if (num < def.slant_tresholds[4])
		{
			return Slant.For;
		}
		return Slant.VeryFor;
	}

	public ProsAndCons GetProsAndCons(Kingdom voter, Kingdom candidate)
	{
		ProsAndCons prosAndCons = ProsAndCons.Get("PC_NominateEmperorOfTheWorld", voter, candidate);
		if (prosAndCons != null)
		{
			prosAndCons.Calc();
			return prosAndCons;
		}
		return prosAndCons;
	}

	public float CalcVoteWeightBase(Kingdom voter, Kingdom candidate)
	{
		ProsAndCons prosAndCons = GetProsAndCons(voter, candidate);
		if (prosAndCons == null)
		{
			return 0f;
		}
		int pP = prosAndCons.PP;
		int cP = prosAndCons.CP;
		float num = (float)pP / def.desired_pro_points;
		float num2 = (float)cP / def.desired_con_points;
		if (pP + cP == 0)
		{
			return 0f;
		}
		float num3 = voter.fame * Math.Min(1f, num * (float)pP / (float)(pP + cP));
		float num4 = voter.fame * Math.Min(1f, num2 * (float)cP / (float)(pP + cP));
		return num3 - num4;
	}

	public int CalcVoteWeight(Kingdom voter, Kingdom candidate, int playerVote = -1)
	{
		float num = CalcVoteWeightBase(voter, candidate);
		if (voter.is_player)
		{
			float num2 = voter.fame * def.player_fame_perc_bonus / 100f;
			num = ((num >= 0f) ? ((playerVote != candidate.id) ? (0f - num2) : (num + num2)) : ((playerVote != candidate.id) ? (num - num2) : num2));
		}
		if (def.vote_weight_precision == 0f)
		{
			def.vote_weight_precision = 1f;
		}
		num /= def.vote_weight_precision;
		num = (float)Math.Round(num);
		num *= def.vote_weight_precision;
		return (int)num;
	}

	public void StartVote(Kingdom forcedKingdom = null)
	{
		if (!EOWEnabled)
		{
			return;
		}
		if (!IsAuthority())
		{
			SendEvent(new StartVoteEvent(forcedKingdom));
		}
		else
		{
			if (!ValidateVote(forcedKingdom))
			{
				return;
			}
			isVotingEnded = false;
			isEmperorChosen = false;
			CalcCandidates(forcedKingdom);
			if (candidates.Count == 0)
			{
				return;
			}
			voters.Clear();
			votes.Clear();
			votes_weights.Clear();
			List<Kingdom> collection = game.great_powers.TopKingdoms();
			voters.AddRange(collection);
			voters.Sort((Kingdom v1, Kingdom v2) => v1.fame.CompareTo(v2.fame));
			for (int num = 0; num < voters.Count; num++)
			{
				Kingdom kingdom = voters[num];
				if (candidates.Contains(kingdom))
				{
					voters.Remove(kingdom);
					num--;
					continue;
				}
				List<int> list = new List<int>(candidates.Count);
				foreach (Kingdom candidate in candidates)
				{
					list.Add(CalcVoteWeight(kingdom, candidate));
				}
				votes_weights.Add(list);
				if (kingdom.is_player)
				{
					votes.Add(-1);
					continue;
				}
				int num2 = -1;
				float num3 = -1f;
				for (int num4 = 0; num4 < candidates.Count; num4++)
				{
					Kingdom kingdom2 = candidates[num4];
					int num5 = votes_weights[num][num4];
					if (num5 > 0 && num3 < (float)num5)
					{
						num2 = kingdom2.id;
						num3 = num5;
					}
				}
				votes.Add(num2);
				if (num2 == -1)
				{
					continue;
				}
				for (int num6 = 0; num6 < candidates.Count; num6++)
				{
					if (candidates[num6].id != num2)
					{
						votes_weights[num][num6] = 0;
					}
				}
			}
			SetVotingActive(active: true);
			SendState<VotesState>();
			SendState<VoteVariablesState>();
			Vars vars = new Vars();
			vars.Set("candidates", candidates);
			vars.Set("voters", voters);
			vars.Set("votes", votes);
			vars.Set("votes_weights", new Value(votes_weights));
			candidates[0].FireEvent("start_emperor_of_the_world_vote", vars);
		}
	}

	private int GetVoterIdx(Kingdom voter)
	{
		int result = -1;
		for (int i = 0; i < voters.Count; i++)
		{
			if (voters[i] == voter)
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public void SetPlayerVote(int voterIdx, int vote)
	{
		if (voterIdx < 0)
		{
			return;
		}
		if (!IsAuthority())
		{
			SendEvent(new VoteEvent(voterIdx, vote));
		}
		else
		{
			if (isVotingEnded)
			{
				return;
			}
			votes[voterIdx] = vote;
			for (int i = 0; i < candidates.Count; i++)
			{
				if (vote != -1 && vote != candidates[i].id)
				{
					votes_weights[voterIdx][i] = 0;
				}
				else
				{
					votes_weights[voterIdx][i] = CalcVoteWeight(voters[voterIdx], candidates[i], vote);
				}
			}
			SendSubstate<VotesState.VoteState>(voterIdx);
			Vars vars = new Vars();
			vars.SetVar("voter_idx", voterIdx);
			vars.SetVar("vote", vote);
			vars.SetVar("vote_weights", new Value(votes_weights[voterIdx]));
			FireEvent("vote_updated", vars);
		}
	}

	public void SetVotingActive(bool active, bool send_state = true)
	{
		isVotingActive = active;
		if (send_state)
		{
			SendState<ActiveState>();
			if (active)
			{
				game.pause.AddRequest("EoWPause");
			}
			else if (game.IsMultiplayer())
			{
				game.pause.DelRequest("EoWPause");
			}
		}
	}

	public void RejectAIEmperor(Kingdom player, Kingdom aiEmperor)
	{
		List<Kingdom> list = game.great_powers.TopKingdoms();
		for (int i = 0; i < list.Count; i++)
		{
			Kingdom kingdom = list[i];
			if (kingdom != player)
			{
				kingdom.StartWarWith(player, War.InvolvementReason.RejectAIEmperorOfTheWorld);
			}
		}
		player.GetCrownAuthority().AddModifier("reject_ai_emperor_of_the_world");
		Vars vars = new Vars();
		vars.SetVar("player", player);
		vars.SetVar("emperor", aiEmperor);
		game.BroadcastRadioEvent("RejectAIEmperorMessage", vars);
		SetEmperorOfTheWorld(null);
	}

	public void RejectBeingEmperor(Kingdom emperor)
	{
		if (!IsAuthority())
		{
			SendEvent(new RejectBeingEmperorEvent(emperor));
			return;
		}
		emperor.GetCrownAuthority().AddModifier("reject_being_emperor_of_the_world");
		List<Kingdom> list = game.great_powers.TopKingdoms();
		for (int i = 0; i < list.Count; i++)
		{
			Kingdom kingdom = list[i];
			if (kingdom != emperor)
			{
				emperor.AddRelationModifier(kingdom, "rel_emperor_of_the_world_refuse_title", null);
			}
		}
		TrySetEmperorOfTheWorld(null, emperor);
	}

	public void SetEmperorOfTheWorld(Kingdom emperor, Kingdom rejetedBy = null)
	{
		if (!AssertAuthority())
		{
			return;
		}
		SetVotingActive(active: false);
		if (emperor != null)
		{
			game.ForceEndGame(emperor, "EmperorOfTheWorld");
			FireEvent("new_emperor_of_the_world", emperor);
			return;
		}
		FireEvent("no_new_emperor_of_the_world", emperor);
		if (rejetedBy != null)
		{
			Vars vars = new Vars();
			vars.SetVar("emperor", rejetedBy);
			game.BroadcastRadioEvent("RejectBeingEmperorMessage", vars);
		}
		else
		{
			game.BroadcastRadioEvent("NoEmperorOfTheWorldChosenMessage", null);
		}
		if (candidates.Count != 1)
		{
			return;
		}
		Kingdom kingdom = candidates[0];
		kingdom.GetCrownAuthority().AddModifier("pretender_emperor_of_the_world");
		List<Kingdom> list = new List<Kingdom>(voters.Count);
		for (int i = 0; i < voters.Count; i++)
		{
			int num = votes[i];
			Kingdom kingdom2 = voters[i];
			if (num != kingdom.id && kingdom2.StartWarWith(kingdom, War.InvolvementReason.RejectPretenderEmperorOfTheWorld) != null)
			{
				list.Add(kingdom2);
			}
		}
		Vars vars2 = new Vars();
		vars2.Set("warsWith", list);
		kingdom.FireEvent("pretender_not_chosen_emperor_of_the_world", vars2);
	}

	public int CountVotesFor(Kingdom k)
	{
		if (k == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < votes.Count; i++)
		{
			if (votes[i] == k.id)
			{
				num++;
			}
		}
		return num;
	}

	public void TrySetEmperorOfTheWorld(Kingdom emperor, Kingdom rejectedBy = null)
	{
		if (!IsAuthority())
		{
			SendEvent(new SetEmperorOfTheWorldEvent(emperor));
		}
		else
		{
			if (isEmperorChosen)
			{
				return;
			}
			isEmperorChosen = true;
			if (emperor != null)
			{
				if (emperor.is_player)
				{
					SetEmperorOfTheWorld(emperor);
				}
				else if (game?.campaign == null || !game.campaign.IsMultiplayerCampaign())
				{
					NotifyListeners("new_ai_emperor_of_the_world", emperor);
				}
				else
				{
					SetEmperorOfTheWorld(emperor);
				}
			}
			else
			{
				SetEmperorOfTheWorld(null, rejectedBy);
			}
			SendState<VoteVariablesState>();
		}
	}

	public float GetFameWinTreshold()
	{
		if (candidates.Count == 1)
		{
			return def.fame_treshold_single_cadidate;
		}
		return def.fame_treshold_multiple_candidates;
	}

	public void SetEndVoting()
	{
		if (!IsAuthority())
		{
			SendEvent(new EndVotingEvent());
		}
		else
		{
			if (isVotingEnded)
			{
				return;
			}
			isVotingEnded = true;
			float num = 0f;
			for (int i = 0; i < voters.Count; i++)
			{
				num += voters[i].fame;
			}
			for (int j = 0; j < candidates.Count; j++)
			{
				Kingdom kingdom = candidates[j];
				float num2 = 0f;
				for (int k = 0; k < votes.Count; k++)
				{
					Kingdom kingdom2 = voters[k];
					int num3 = votes[k];
					num2 += (float)votes_weights[k][j];
					if (kingdom.id == num3)
					{
						kingdom2.AddRelationModifier(kingdom, "rel_emperor_of_the_world_support_candidate", null);
					}
					else
					{
						kingdom2.AddRelationModifier(kingdom, "rel_emperor_of_the_world_dont_support_candidate", null);
					}
				}
				if (num2 > num * GetFameWinTreshold())
				{
					if (kingdom.is_player)
					{
						FireEvent("wait_for_emperor_of_the_world_response", kingdom);
					}
					else
					{
						TrySetEmperorOfTheWorld(kingdom);
					}
					return;
				}
			}
			TrySetEmperorOfTheWorld(null);
			SendState<VoteVariablesState>();
		}
	}

	public override void OnUpdate()
	{
		if (IsAuthority())
		{
			UpdateAfter(GetNextAutoVoteUpdateTime());
			if (game.subgames.Count <= 0)
			{
				StartVote();
			}
		}
	}

	public override Value GetDumpStateValue()
	{
		return Value.Null;
	}

	public override void DumpInnerState(StateDump dump, int verbosity)
	{
		dump.Append("is_voting_active", isVotingActive.ToString());
		dump.Append("is_voting_ended", isVotingEnded.ToString());
		dump.Append("is_emperor_chosen", isEmperorChosen.ToString());
		if (candidates != null && candidates.Count > 0)
		{
			dump.OpenSection("candidates");
			for (int i = 0; i < candidates.Count; i++)
			{
				dump.Append(candidates[i]?.Name);
			}
			dump.CloseSection("candidates");
		}
		if (voters != null && voters.Count > 0)
		{
			dump.OpenSection("voters");
			for (int j = 0; j < voters.Count; j++)
			{
				dump.Append(voters[j]?.Name);
			}
			dump.CloseSection("voters");
		}
		if (votes != null && votes.Count > 0)
		{
			dump.OpenSection("votes");
			for (int k = 0; k < votes.Count; k++)
			{
				dump.Append(votes[k].ToString());
			}
			dump.CloseSection("votes");
		}
	}

	protected override void OnDestroy()
	{
		if (game.emperorOfTheWorld == this)
		{
			game.emperorOfTheWorld = null;
		}
		base.OnDestroy();
	}

	public EmperorOfTheWorld(Multiplayer multiplayer)
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
}
