using System.Collections.Generic;

namespace Logic;

public class TokenParser
{
	public struct Position
	{
		public int offset;

		public int line;

		public static readonly Position Start = new Position
		{
			offset = 0,
			line = 1
		};

		public string text;
	}

	public struct Token
	{
		public enum Type
		{
			EOF,
			WhiteSpace,
			NewLine,
			Comment,
			Symbol,
			Identifier,
			Number,
			String
		}

		public Type type;

		public Position pos;

		public int len;

		public Token(Type type, Position pos_start, Position pos_end)
		{
			this.type = type;
			pos = pos_start;
			len = pos_end.offset - pos_start.offset;
		}

		public override string ToString()
		{
			return $"[{pos.line}] {type}({len})" + ((pos.text == null || len <= 0 || type == Type.NewLine) ? "" : (": '" + pos.text.Substring(pos.offset, len) + "'"));
		}
	}

	public string text;

	public int txt_len;

	public List<Token> tokens;

	private Position cur_pos = Position.Start;

	public string[] symbols;

	public int cur_token_idx;

	public Token cur_token = new Token(Token.Type.EOF, Position.Start, Position.Start);

	public int num_tokens => tokens.Count;

	public string CurTokenText => TokenText(cur_token, text);

	public string CurTokenLineText => LineText(cur_token);

	public TokenParser(string text, bool read_all = true, string[] symbols = null)
	{
		Init(text, read_all, symbols);
	}

	public void Init(string text, bool read_all = true, string[] symbols = null)
	{
		this.text = text;
		txt_len = text.Length;
		if (symbols != null)
		{
			this.symbols = symbols;
		}
		cur_pos.text = text;
		if (read_all)
		{
			ReadAll();
		}
	}

	public int ReadAll()
	{
		tokens = ReadTokens(text, txt_len, symbols);
		cur_token_idx = 0;
		cur_token = GetToken(cur_token_idx);
		return tokens.Count;
	}

	public static List<Token> ReadTokens(string text, int txt_len = -1, string[] symbols = null)
	{
		if (txt_len < 0)
		{
			txt_len = text.Length;
		}
		List<Token> result = new List<Token>(txt_len / 4);
		ReadTokens(result, text, txt_len, symbols);
		return result;
	}

	public static void ReadTokens(List<Token> tokens, string text, int txt_len = -1, string[] symbols = null)
	{
		ReadTokens(tokens, text, Position.Start, txt_len, symbols);
	}

	public static void ReadTokens(List<Token> tokens, string text, Position pos, int txt_len = -1, string[] symbols = null)
	{
		pos.text = text;
		if (txt_len < 0)
		{
			txt_len = text.Length;
		}
		while (true)
		{
			Token item = ReadToken(text, ref pos, txt_len, symbols);
			if (item.type != Token.Type.EOF)
			{
				tokens.Add(item);
				continue;
			}
			break;
		}
	}

	public Token ReadToken()
	{
		return ReadToken(text, ref cur_pos, txt_len, symbols);
	}

	public static Token ReadToken(string text, ref Position pos, int txt_len = -1, string[] symbols = null)
	{
		Position pos_start = pos;
		if (txt_len < 0)
		{
			txt_len = text.Length;
		}
		if (pos.offset >= txt_len)
		{
			return new Token(Token.Type.EOF, pos_start, pos);
		}
		int num = MatchSymbol(symbols, text, pos.offset, txt_len);
		if (num >= 0)
		{
			string text2 = symbols[num];
			pos.offset += text2.Length;
			return new Token(Token.Type.Symbol, pos_start, pos);
		}
		char c = text[pos.offset];
		pos.offset++;
		switch (c)
		{
		case '\n':
		case '\r':
			pos.line++;
			if (pos.offset < txt_len && text[pos.offset] == 23 - c)
			{
				pos.offset++;
			}
			return new Token(Token.Type.NewLine, pos_start, pos);
		case '\t':
		case ' ':
			while (pos.offset < txt_len)
			{
				c = text[pos.offset];
				if (c != ' ' || c != '\t')
				{
					break;
				}
				pos.offset++;
			}
			return new Token(Token.Type.WhiteSpace, pos_start, pos);
		case '/':
			if (pos.offset >= txt_len)
			{
				return new Token(Token.Type.Symbol, pos_start, pos);
			}
			switch (text[pos.offset])
			{
			case '/':
				pos.offset++;
				while (pos.offset < txt_len)
				{
					c = text[pos.offset];
					if (c == '\n' || c == '\r')
					{
						break;
					}
					pos.offset++;
				}
				return new Token(Token.Type.Comment, pos_start, pos);
			case '*':
				pos.offset++;
				while (pos.offset < txt_len)
				{
					c = text[pos.offset];
					pos.offset++;
					switch (c)
					{
					case '\n':
					case '\r':
						pos.line++;
						if (pos.offset < txt_len && text[pos.offset] == 23 - c)
						{
							pos.offset++;
						}
						continue;
					case '*':
						break;
					default:
						continue;
					}
					c = text[pos.offset];
					pos.offset++;
					if (c == '/')
					{
						break;
					}
				}
				return new Token(Token.Type.Comment, pos_start, pos);
			default:
				return new Token(Token.Type.Symbol, pos_start, pos);
			}
		case '"':
		case '\'':
		{
			char c2 = c;
			while (pos.offset < txt_len)
			{
				c = text[pos.offset];
				if (c == '\n' || c == '\r')
				{
					break;
				}
				pos.offset++;
				if (c == c2)
				{
					break;
				}
			}
			return new Token(Token.Type.String, pos_start, pos);
		}
		default:
			if ((c >= '0' && c <= '9') || (c == '-' && pos.offset < txt_len && text[pos.offset] >= '0' && text[pos.offset] <= '9'))
			{
				while (pos.offset < txt_len)
				{
					c = text[pos.offset];
					if (c < '0' || c > '9')
					{
						break;
					}
					pos.offset++;
				}
				if (pos.offset < txt_len && text[pos.offset] == '.')
				{
					pos.offset++;
					while (pos.offset < txt_len)
					{
						c = text[pos.offset];
						if (c < '0' || c > '9')
						{
							break;
						}
						pos.offset++;
					}
				}
				return new Token(Token.Type.Number, pos_start, pos);
			}
			if (IsIdFirstChar(c))
			{
				while (pos.offset < txt_len)
				{
					c = text[pos.offset];
					if (!IsIDChar(c))
					{
						break;
					}
					pos.offset++;
				}
				return new Token(Token.Type.Identifier, pos_start, pos);
			}
			return new Token(Token.Type.Symbol, pos_start, pos);
		}
	}

	public static bool IsIdFirstChar(char c)
	{
		switch (c)
		{
		default:
			if (c >= 'A')
			{
				return c <= 'Z';
			}
			return false;
		case '_':
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
		case 'g':
		case 'h':
		case 'i':
		case 'j':
		case 'k':
		case 'l':
		case 'm':
		case 'n':
		case 'o':
		case 'p':
		case 'q':
		case 'r':
		case 's':
		case 't':
		case 'u':
		case 'v':
		case 'w':
		case 'x':
		case 'y':
		case 'z':
			return true;
		}
	}

	public static bool IsIDChar(char c)
	{
		if (!IsIdFirstChar(c))
		{
			if (c >= '0')
			{
				return c <= '9';
			}
			return false;
		}
		return true;
	}

	public static bool Match(string s, string text, int ofs, int txt_len = -1)
	{
		if (txt_len < 0)
		{
			txt_len = text.Length;
		}
		int length = s.Length;
		if (ofs + length > txt_len)
		{
			return false;
		}
		for (int i = 0; i < length; i++)
		{
			char num = text[ofs + i];
			char c = s[i];
			if (num != c)
			{
				return false;
			}
		}
		return true;
	}

	public static int MatchSymbol(string[] symbols, string text, int ofs, int txt_len = -1)
	{
		if (symbols == null)
		{
			return -1;
		}
		if (txt_len < 0)
		{
			txt_len = text.Length;
		}
		for (int i = 0; i < symbols.Length; i++)
		{
			if (Match(symbols[i], text, ofs, txt_len))
			{
				return i;
			}
		}
		return -1;
	}

	public bool Match(Token token, string s)
	{
		return Match(text, token, s);
	}

	public static bool Match(string text, Token token, string s)
	{
		if (s.Length != token.len)
		{
			return false;
		}
		return Match(s, text, token.pos.offset);
	}

	public Token GetToken(int idx)
	{
		return GetToken(tokens, idx);
	}

	public static Token GetToken(List<Token> tokens, int idx)
	{
		if (tokens == null || idx < 0 || idx >= tokens.Count)
		{
			return new Token(Token.Type.EOF, Position.Start, Position.Start);
		}
		return tokens[idx];
	}

	public bool GoTo(int token_idx)
	{
		cur_token_idx = token_idx;
		cur_token = GetToken(cur_token_idx);
		return cur_token.type != Token.Type.EOF;
	}

	public bool MoveNext(int cnt = 1, Token.Type ignore_level = Token.Type.Comment)
	{
		for (int i = 0; i < cnt; i++)
		{
			cur_token_idx = NextToken(cur_token_idx, ignore_level);
		}
		cur_token = GetToken(cur_token_idx);
		return cur_token.type != Token.Type.EOF;
	}

	public int NextToken(int idx, Token.Type ignore_level = Token.Type.Comment)
	{
		return NextToken(tokens, idx, ignore_level);
	}

	public static int NextToken(List<Token> tokens, int idx, Token.Type ignore_level = Token.Type.Comment)
	{
		if (idx < 0)
		{
			return -1;
		}
		for (idx++; idx < tokens.Count; idx++)
		{
			if (tokens[idx].type > ignore_level)
			{
				return idx;
			}
		}
		return -1;
	}

	public bool MoveBack(int cnt = 1, Token.Type ignore_level = Token.Type.Comment)
	{
		for (int i = 0; i < cnt; i++)
		{
			cur_token_idx = PrevToken(cur_token_idx, ignore_level);
		}
		cur_token = GetToken(cur_token_idx);
		return cur_token.type != Token.Type.EOF;
	}

	public int PrevToken(int idx, Token.Type ignore_level = Token.Type.Comment)
	{
		return PrevToken(tokens, idx, ignore_level);
	}

	public static int PrevToken(List<Token> tokens, int idx, Token.Type ignore_level = Token.Type.Comment)
	{
		idx--;
		while (idx >= 0 && tokens[idx].type <= ignore_level)
		{
			idx--;
		}
		return idx;
	}

	public bool AtEOF()
	{
		return cur_token.type == Token.Type.EOF;
	}

	public bool IsAt(params string[] strs)
	{
		return Match(cur_token_idx, strs);
	}

	public bool Skip(params string[] strs)
	{
		if (!IsAt(strs))
		{
			return false;
		}
		MoveNext(strs.Length);
		return true;
	}

	public bool Match(int idx, params string[] strs)
	{
		return Match(text, tokens, idx, strs);
	}

	public static bool Match(string text, List<Token> tokens, int idx, params string[] strs)
	{
		foreach (string s in strs)
		{
			Token token = GetToken(tokens, idx);
			if (token.type == Token.Type.EOF)
			{
				return false;
			}
			if (!Match(text, token, s))
			{
				return false;
			}
			idx = NextToken(tokens, idx);
		}
		return true;
	}

	public bool MatchPrev(params string[] strs)
	{
		int idx = PrevToken(cur_token_idx);
		return MatchBackwards(idx, strs);
	}

	public bool MatchBackwards(int idx, params string[] strs)
	{
		return MatchBackwards(text, tokens, idx, strs);
	}

	public static bool MatchBackwards(string text, List<Token> tokens, int idx, params string[] strs)
	{
		for (int num = strs.Length - 1; num >= 0; num--)
		{
			string s = strs[num];
			Token token = GetToken(tokens, idx);
			if (token.type == Token.Type.EOF)
			{
				return false;
			}
			if (!Match(text, token, s))
			{
				return false;
			}
			idx = PrevToken(tokens, idx);
		}
		return true;
	}

	public bool Find(params string[] strs)
	{
		int num = Find(cur_token_idx, strs);
		if (num < 0)
		{
			return false;
		}
		GoTo(num);
		return true;
	}

	public int Find(int start_idx, params string[] strs)
	{
		return Find(text, tokens, start_idx, strs);
	}

	public static int Find(string text, List<Token> tokens, int start_idx, params string[] strs)
	{
		int num = tokens.Count - strs.Length;
		for (int i = start_idx; i <= num; i++)
		{
			if (Match(text, tokens, i, strs))
			{
				return i;
			}
		}
		return -1;
	}

	public static string TokenText(Token token, string text)
	{
		return text.Substring(token.pos.offset, token.len);
	}

	public string TokenText(Token token)
	{
		return TokenText(token, text);
	}

	public static bool CalcLineTokens(out int start, out int end, List<Token> tokens, int token, int prev_lines = 0, int next_lines = 0)
	{
		start = (end = token);
		if (tokens == null || token < 0 || token >= tokens.Count)
		{
			return false;
		}
		int line = tokens[token].pos.line;
		while (start > 0 && tokens[start - 1].pos.line >= line - prev_lines)
		{
			start--;
		}
		while (end + 1 < tokens.Count && tokens[end + 1].pos.line <= line + next_lines)
		{
			end++;
		}
		return true;
	}

	public static bool CalcLineOffsets(out int start, out int end, string text, int ofs, int txt_len = -1, int prev_lines = 0, int next_lines = 0)
	{
		if (txt_len < 0)
		{
			txt_len = text.Length;
		}
		if (ofs < 0 || ofs >= txt_len)
		{
			start = (end = 0);
			return false;
		}
		start = ofs;
		while (start > 0)
		{
			char c = text[start - 1];
			if (c != '\r' && c != '\n')
			{
				start--;
				continue;
			}
			if (prev_lines == 0)
			{
				break;
			}
			start--;
			if (start > 1 && text[start - 1] == 23 - c)
			{
				start--;
			}
			prev_lines--;
		}
		end = ofs;
		while (end < txt_len)
		{
			char c2 = text[end];
			if (c2 != '\r' && c2 != '\n')
			{
				end++;
				continue;
			}
			if (next_lines == 0)
			{
				break;
			}
			end++;
			if (end < txt_len && text[end] == 23 - c2)
			{
				end++;
			}
			next_lines--;
		}
		return true;
	}

	public static string LineText(string text, int ofs, int txt_len = -1, int prev_lines = 0, int next_lines = 0)
	{
		if (!CalcLineOffsets(out var start, out var end, text, ofs, txt_len, prev_lines, next_lines))
		{
			return "";
		}
		return text.Substring(start, end - start);
	}

	public static string LineText(Position pos, string text, int txt_len = -1, int prev_lines = 0, int next_lines = 0)
	{
		return LineText(text, pos.offset, txt_len, prev_lines, next_lines);
	}

	public static string LineText(Token token, string text, int txt_len = -1, int prev_lines = 0, int next_lines = 0)
	{
		return LineText(text, token.pos.offset, txt_len, prev_lines, next_lines);
	}

	public string LineText(Token token, int prev_lines = 0, int next_lines = 0)
	{
		return LineText(token, text, txt_len, prev_lines, next_lines);
	}

	public override string ToString()
	{
		return $"'{CurTokenText}' -> [{cur_token.pos.line}] '{CurTokenLineText}'";
	}
}
