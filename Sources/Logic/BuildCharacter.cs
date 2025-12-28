using System;
using System.Collections.Generic;

namespace Logic;

public class BuildCharacter
{
	private static Random random = new Random();

	private Character m_Result;

	private Game m_Game;

	private string m_Name;

	private int m_NameIdx;

	private string m_ClassId;

	private int m_KingdomId;

	private int m_OriginalKingdomId;

	private string m_Title;

	private Character.Sex m_Sex;

	private Character.Age m_Age;

	private Character.Ethnicity m_Ethnicity;

	private float m_Next_Age_Offset;

	private Character mother;

	private Character father;

	private bool hasDefaultSkill;

	private bool canAge;

	private bool m_HistoricalFigure;

	private FamousPerson.Def m_FamousPerson;

	private List<Skill.Def> m_BaseSkills;

	public BuildCharacter(Game game, int kingdom_id, int original_kingdom_id, string class_id, string title = "Knight")
	{
		m_Game = game;
		m_KingdomId = kingdom_id;
		m_OriginalKingdomId = original_kingdom_id;
		m_Title = title;
		m_ClassId = class_id;
		m_Sex = ((!(random.NextDouble() < 0.5)) ? Character.Sex.Female : Character.Sex.Male);
		m_Age = Character.Age.COUNT;
	}

	public string GetName()
	{
		return m_Name;
	}

	public void SetName(string name)
	{
		m_Name = name;
	}

	public void SetNameIdx(int name_idx)
	{
		m_NameIdx = name_idx;
	}

	public void SetTitle(string title)
	{
		m_Title = title;
	}

	public void SetOriginalKingdomId(int kid)
	{
		m_OriginalKingdomId = kid;
	}

	public int GetOriginalKingdomId()
	{
		return m_OriginalKingdomId;
	}

	public Character.Sex GetSex()
	{
		return m_Sex;
	}

	public void SetSex(Character.Sex sex)
	{
		m_Sex = sex;
	}

	public void SetEthnicity(Character.Ethnicity ethnicity)
	{
		m_Ethnicity = ethnicity;
	}

	public Character.Age GetAge()
	{
		return m_Age;
	}

	public void SetAge(Character.Age age, float offset = 0f)
	{
		m_Age = age;
		m_Next_Age_Offset = offset;
	}

	public void SetFamousPerson(FamousPerson.Def def)
	{
		m_FamousPerson = def;
	}

	public void SetHistoricalFigure(bool v)
	{
		m_HistoricalFigure = v;
	}

	public void SetBaseSkills(List<Skill.Def> skills)
	{
		m_BaseSkills = skills;
	}

	public void SetClass(string class_id)
	{
		m_ClassId = class_id;
	}

	public void SetParents(Character father, Character mother)
	{
		this.father = father;
		this.mother = mother;
		if (father != null)
		{
			SetEthnicity(father.ethnicity);
		}
	}

	public void SetHasDefaultSkill(bool has)
	{
		hasDefaultSkill = has;
	}

	public void EnableAging(bool can_age)
	{
		canAge = can_age;
	}

	public Character Build()
	{
		CharacterClass.Def def = ((m_ClassId == null) ? null : m_Game.defs.Get<CharacterClass.Def>(m_ClassId));
		m_Result = new Character(m_Game, m_KingdomId, m_OriginalKingdomId, def, m_HistoricalFigure);
		m_Result.GetKingdom();
		Kingdom originalKingdom = m_Result.GetOriginalKingdom();
		m_Result.title = m_Title;
		m_Result.sex = m_Sex;
		if (m_Age == Character.Age.COUNT)
		{
			m_Age = (Character.Age)random.Next(5);
		}
		m_Result.age = m_Age;
		m_Result.ethnicity = m_Ethnicity;
		if (m_FamousPerson != null)
		{
			m_Result.famous_def = m_FamousPerson;
			m_Result.Name = m_FamousPerson.name;
			FamousPersonSpawner component = m_Game.GetComponent<FamousPersonSpawner>();
			component.famous_people.Add(m_Result);
			if (!component.non_available_famous_people.Contains(m_FamousPerson))
			{
				component.non_available_famous_people.Add(m_FamousPerson);
			}
		}
		else if (m_Name != null)
		{
			m_Result.Name = m_Name;
		}
		else if (m_Result.title == "Prince" || m_Result.title == "Princess")
		{
			m_Result.Name = CharacterFactory.GetUniqueRoyalChildName(m_Result.GetOriginalKingdom(), m_Result.sex);
		}
		else
		{
			m_Result.Name = CharacterFactory.GetCharacterName(m_Result.GetOriginalKingdom(), m_Result.sex);
		}
		m_Result.name_idx = m_NameIdx;
		if (father == null)
		{
			m_Result.ethnicity = originalKingdom.default_ethnicity;
		}
		m_Result.next_age_check = m_Game.time + m_Result.GetAgeupDuration(m_Result.age) - m_Next_Age_Offset;
		if (hasDefaultSkill)
		{
			m_Result.InitDefaultSkill();
		}
		if (m_BaseSkills != null)
		{
			if (m_Result.skills == null)
			{
				m_Result.skills = new List<Skill>();
			}
			for (int i = 0; i < m_BaseSkills.Count; i++)
			{
				Skill item = new Skill(m_BaseSkills[i], m_Result);
				m_Result.skills.Add(item);
			}
		}
		if (def != null && !m_Result.IsClasslessPrince() && (m_Result.title == "King" || m_Result.title == "Prince"))
		{
			m_Result.GenerateNewSkills(force_new: true);
		}
		m_Result.EnableAging(canAge);
		return m_Result;
	}
}
