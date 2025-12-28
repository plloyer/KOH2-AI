using System.Collections.Generic;

namespace Logic;

public sealed class HungarianAlgorithm
{
	private readonly int[,] _costMatrix;

	private int _inf;

	private int _n;

	private int[] _lx;

	private int[] _ly;

	private bool[] _s;

	private bool[] _t;

	private int[] _matchX;

	private int[] _matchY;

	private int _maxMatch;

	private int[] _slack;

	private int[] _slackx;

	private int[] _prev;

	public HungarianAlgorithm(int[,] costMatrix)
	{
		_costMatrix = costMatrix;
	}

	public int[] Run()
	{
		_n = _costMatrix.GetLength(0);
		_lx = new int[_n];
		_ly = new int[_n];
		_s = new bool[_n];
		_t = new bool[_n];
		_matchX = new int[_n];
		_matchY = new int[_n];
		_slack = new int[_n];
		_slackx = new int[_n];
		_prev = new int[_n];
		_inf = int.MaxValue;
		InitMatches();
		if (_n != _costMatrix.GetLength(1))
		{
			return null;
		}
		InitLbls();
		_maxMatch = 0;
		InitialMatching();
		Queue<int> queue = new Queue<int>();
		while (_maxMatch != _n)
		{
			queue.Clear();
			InitSt();
			int num = 0;
			int i = 0;
			int j;
			for (j = 0; j < _n; j++)
			{
				if (_matchX[j] == -1)
				{
					queue.Enqueue(j);
					num = j;
					_prev[j] = -2;
					_s[j] = true;
					break;
				}
			}
			for (int k = 0; k < _n; k++)
			{
				_slack[k] = _costMatrix[num, k] - _lx[num] - _ly[k];
				_slackx[k] = num;
			}
			do
			{
				IL_0208:
				if (queue.Count != 0)
				{
					j = queue.Dequeue();
					int num2 = _lx[j];
					for (i = 0; i < _n; i++)
					{
						if (_costMatrix[j, i] == num2 + _ly[i] && !_t[i])
						{
							if (_matchY[i] == -1)
							{
								break;
							}
							_t[i] = true;
							queue.Enqueue(_matchY[i]);
							AddToTree(_matchY[i], j);
						}
					}
					if (i >= _n)
					{
						goto IL_0208;
					}
				}
				if (i < _n)
				{
					break;
				}
				UpdateLabels();
				for (i = 0; i < _n; i++)
				{
					if (!_t[i] && _slack[i] == 0)
					{
						if (_matchY[i] == -1)
						{
							j = _slackx[i];
							break;
						}
						_t[i] = true;
						if (!_s[_matchY[i]])
						{
							queue.Enqueue(_matchY[i]);
							AddToTree(_matchY[i], _slackx[i]);
						}
					}
				}
			}
			while (i >= _n);
			_maxMatch++;
			int num3 = j;
			int num4 = i;
			while (num3 != -2)
			{
				int num5 = _matchX[num3];
				_matchY[num4] = num3;
				_matchX[num3] = num4;
				num3 = _prev[num3];
				num4 = num5;
			}
		}
		return _matchX;
	}

	private void InitMatches()
	{
		for (int i = 0; i < _n; i++)
		{
			_matchX[i] = -1;
			_matchY[i] = -1;
		}
	}

	private void InitSt()
	{
		for (int i = 0; i < _n; i++)
		{
			_s[i] = false;
			_t[i] = false;
		}
	}

	private void InitLbls()
	{
		for (int i = 0; i < _n; i++)
		{
			int num = _costMatrix[i, 0];
			for (int j = 0; j < _n; j++)
			{
				if (_costMatrix[i, j] < num)
				{
					num = _costMatrix[i, j];
				}
				if (num == 0)
				{
					break;
				}
			}
			_lx[i] = num;
		}
		for (int k = 0; k < _n; k++)
		{
			int num2 = _costMatrix[0, k] - _lx[0];
			for (int l = 0; l < _n; l++)
			{
				if (_costMatrix[l, k] - _lx[l] < num2)
				{
					num2 = _costMatrix[l, k] - _lx[l];
				}
				if (num2 == 0)
				{
					break;
				}
			}
			_ly[k] = num2;
		}
	}

	private void UpdateLabels()
	{
		int num = _inf;
		for (int i = 0; i < _n; i++)
		{
			if (!_t[i] && num > _slack[i])
			{
				num = _slack[i];
			}
		}
		for (int j = 0; j < _n; j++)
		{
			if (_s[j])
			{
				_lx[j] += num;
			}
			if (_t[j])
			{
				_ly[j] -= num;
			}
			else
			{
				_slack[j] -= num;
			}
		}
	}

	private void AddToTree(int x, int prevx)
	{
		_s[x] = true;
		_prev[x] = prevx;
		int num = _lx[x];
		for (int i = 0; i < _n; i++)
		{
			if (_costMatrix[x, i] - num - _ly[i] < _slack[i])
			{
				_slack[i] = _costMatrix[x, i] - num - _ly[i];
				_slackx[i] = x;
			}
		}
	}

	private void InitialMatching()
	{
		for (int i = 0; i < _n; i++)
		{
			for (int j = 0; j < _n; j++)
			{
				if (_costMatrix[i, j] == _lx[i] + _ly[j] && _matchY[j] == -1)
				{
					_matchX[i] = j;
					_matchY[j] = i;
					_maxMatch++;
					break;
				}
			}
		}
	}
}
