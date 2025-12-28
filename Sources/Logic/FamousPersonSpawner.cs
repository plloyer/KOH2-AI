using System.Collections.Generic;

namespace Logic;

public class FamousPersonSpawner : Component
{
	public class Def : Logic.Def
	{
		public float duration = 60f;

		public int max_famous_people = 50;

		public int available_pool = 30;

		public override bool Load(Game game)
		{
			DT.Field field = base.field;
			duration = field.GetFloat("duration", null, duration);
			max_famous_people = field.GetInt("max_famous_people", null, max_famous_people);
			available_pool = field.GetInt("available_pool", null, available_pool);
			return true;
		}
	}

	public Time next_update;

	public Def def;

	public List<FamousPerson.Def> non_available_famous_people = new List<FamousPerson.Def>();

	public List<FamousPerson.Def> all_famous_people = new List<FamousPerson.Def>();

	public List<Character> famous_people = new List<Character>();

	public List<FamousPerson.Def> GetPool()
	{
		if (all_famous_people == null)
		{
			return all_famous_people;
		}
		List<FamousPerson.Def> list = new List<FamousPerson.Def>();
		int num = def.available_pool;
		int num2 = 0;
		while (num > 0 && num2 < all_famous_people.Count)
		{
			FamousPerson.Def item = all_famous_people[num2];
			num2++;
			if (!non_available_famous_people.Contains(item))
			{
				list.Add(item);
				num--;
			}
		}
		return list;
	}

	public FamousPersonSpawner(Game game)
		: base(game)
	{
		this.def = game.defs.GetBase<Def>();
		DT.Def def = game.dt.FindDef("FamousPerson");
		if (def == null || def.defs == null || def.defs.Count < 1)
		{
			return;
		}
		for (int i = 0; i < def.defs.Count; i++)
		{
			DT.Field field = def.defs[i].field;
			if (!string.IsNullOrEmpty(field.base_path))
			{
				FamousPerson.Def item = game.defs.Get<FamousPerson.Def>(field.key);
				if (!all_famous_people.Contains(item))
				{
					all_famous_people.Add(item);
				}
			}
		}
		if (obj.IsAuthority())
		{
			SortChronologically();
			non_available_famous_people = new List<FamousPerson.Def>();
		}
	}

	public void SortChronologically()
	{
		if (all_famous_people != null && all_famous_people.Count != 0)
		{
			all_famous_people.Sort((FamousPerson.Def x, FamousPerson.Def y) => x.year_born.CompareTo(y.year_born));
		}
	}

	public override void OnUpdate()
	{
		if (obj.IsAuthority())
		{
			SpawnPeople();
			next_update = base.game.time + def.duration;
			UpdateAfter(def.duration);
		}
	}

	public void SpawnPeople()
	{
		if (all_famous_people.Count != 0)
		{
			Def def = base.game.defs.GetBase<Def>();
			if (famous_people.Count < def.max_famous_people)
			{
				List<FamousPerson.Def> pool = GetPool();
				FamousPerson.Def fdef = pool[base.game.Random(0, pool.Count - 1)];
				Village village = ChooseSpawnVillage(fdef, base.game);
				village?.SetFamous(CharacterFactory.CreateFamousPerson(village.GetKingdom(), fdef));
			}
		}
	}

	public static Village ChooseSpawnVillage(FamousPerson.Def fdef, Game game, Kingdom to_ignore = null)
	{
		int num = 0;
		List<Realm> list = new List<Realm>(game.realms);
		while (num < 100)
		{
			if (list == null || list.Count == 0)
			{
				return null;
			}
			num++;
			int index = game.Random(0, list.Count);
			Realm realm = list[index];
			list.RemoveAt(index);
			if (realm == null || realm.castle == null || (to_ignore != null && realm.kingdom_id == to_ignore.id))
			{
				continue;
			}
			for (int i = 0; i < realm.settlements.Count; i++)
			{
				if (realm.settlements[i] is Village village && village.IsActiveSettlement() && village.famous_person == null && fdef.require.Validate(village.GetRealm().castle))
				{
					return village;
				}
			}
		}
		return null;
	}
}
