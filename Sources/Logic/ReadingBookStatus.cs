namespace Logic;

public class ReadingBookStatus : Status
{
	public new class FullData : Status.FullData
	{
		public string book_name;

		public new static FullData Create()
		{
			return new FullData();
		}

		public override bool InitFrom(object obj)
		{
			if (!base.InitFrom(obj))
			{
				return false;
			}
			if (!(obj is ReadingBookStatus readingBookStatus))
			{
				return false;
			}
			book_name = readingBookStatus.book?.name;
			return true;
		}

		public override void Save(Serialization.IWriter ser)
		{
			base.Save(ser);
			ser.WriteStr(book_name, "book");
		}

		public override void Load(Serialization.IReader ser)
		{
			base.Load(ser);
			book_name = ser.ReadStr("book");
		}

		public override bool ApplyTo(object obj, Game game)
		{
			if (!base.ApplyTo(obj, game))
			{
				return false;
			}
			if (!(obj is ReadingBookStatus readingBookStatus))
			{
				return false;
			}
			if (string.IsNullOrEmpty(book_name))
			{
				readingBookStatus.book = null;
				return false;
			}
			readingBookStatus.book = readingBookStatus.game.defs.Get<Book.Def>(book_name);
			return readingBookStatus.book != null;
		}
	}

	public Book.Def book;

	public ReadingBookStatus(Book.Def book_def)
		: base(null)
	{
		book = book_def;
	}

	public ReadingBookStatus(Def def)
		: base(def)
	{
	}

	public new static Status Create(Def def)
	{
		return new ReadingBookStatus(def);
	}

	public override void GetProgress(out float cur, out float max)
	{
		Action action = GetAction();
		if (action == null)
		{
			cur = (max = 0f);
		}
		else
		{
			action.GetProgress(out cur, out max);
		}
	}

	public Action GetAction()
	{
		return Action.Find(owner, "ReadBookAction");
	}
}
