// A port of Python's difflib.SequenceMatcher (no junk function, autojunk on): the same matching blocks and opcodes for two lists of lines.
namespace SuperMemoAssistant.Themes.Editing;

internal readonly record struct DiffOp(string Tag, int A1, int A2, int B1, int B2);

internal sealed class DifflibMatcher
{
  private readonly IReadOnlyList<string>        _a;
  private readonly IReadOnlyList<string>        _b;
  private readonly Dictionary<string, List<int>> _b2j = [];

  public DifflibMatcher(IReadOnlyList<string> a, IReadOnlyList<string> b)
  {
    _a = a;
    _b = b;

    for (var i = 0; i < b.Count; i++)
    {
      if (!_b2j.TryGetValue(b[i], out var indices))
        _b2j[b[i]] = indices = [];

      indices.Add(i);
    }

    var n = b.Count;

    if (n >= 200)
    {
      var ntest = n / 100 + 1;

      foreach (var popular in _b2j.Where(kv => kv.Value.Count > ntest).Select(kv => kv.Key).ToList())
        _b2j.Remove(popular);
    }
  }

  private (int I, int J, int Size) LongestMatch(int alo, int ahi, int blo, int bhi)
  {
    int besti = alo, bestj = blo, bestsize = 0;
    var j2len = new Dictionary<int, int>();

    for (var i = alo; i < ahi; i++)
    {
      var newJ2len = new Dictionary<int, int>();

      if (_b2j.TryGetValue(_a[i], out var indices))
      {
        foreach (var j in indices)
        {
          if (j < blo)
            continue;

          if (j >= bhi)
            break;

          var k = (j2len.TryGetValue(j - 1, out var previous) ? previous : 0) + 1;

          newJ2len[j] = k;

          if (k > bestsize)
          {
            besti    = i - k + 1;
            bestj    = j - k + 1;
            bestsize = k;
          }
        }
      }

      j2len = newJ2len;
    }

    while (besti > alo && bestj > blo && _a[besti - 1] == _b[bestj - 1])
    {
      besti--;
      bestj--;
      bestsize++;
    }

    while (besti + bestsize < ahi && bestj + bestsize < bhi && _a[besti + bestsize] == _b[bestj + bestsize])
      bestsize++;

    return (besti, bestj, bestsize);
  }

  private List<(int I, int J, int Size)> MatchingBlocks()
  {
    var queue  = new Stack<(int, int, int, int)>();
    var blocks = new List<(int I, int J, int Size)>();

    queue.Push((0, _a.Count, 0, _b.Count));

    while (queue.Count > 0)
    {
      var (alo, ahi, blo, bhi) = queue.Pop();
      var (i, j, k)            = LongestMatch(alo, ahi, blo, bhi);

      if (k == 0)
        continue;

      blocks.Add((i, j, k));

      if (alo < i && blo < j)
        queue.Push((alo, i, blo, j));

      if (i + k < ahi && j + k < bhi)
        queue.Push((i + k, ahi, j + k, bhi));
    }

    blocks.Sort();

    var (i1, j1, k1) = (0, 0, 0);
    var merged       = new List<(int, int, int)>();

    foreach (var (i2, j2, k2) in blocks)
    {
      if (i1 + k1 == i2 && j1 + k1 == j2)
      {
        k1 += k2;
      }
      else
      {
        if (k1 > 0)
          merged.Add((i1, j1, k1));

        (i1, j1, k1) = (i2, j2, k2);
      }
    }

    if (k1 > 0)
      merged.Add((i1, j1, k1));

    merged.Add((_a.Count, _b.Count, 0));

    return merged;
  }

  public List<DiffOp> Opcodes()
  {
    int i = 0, j = 0;
    var answer = new List<DiffOp>();

    foreach (var (ai, bj, size) in MatchingBlocks())
    {
      var tag = i < ai && j < bj ? "replace" : i < ai ? "delete" : j < bj ? "insert" : "";

      if (tag.Length > 0)
        answer.Add(new DiffOp(tag, i, ai, j, bj));

      i = ai + size;
      j = bj + size;

      if (size > 0)
        answer.Add(new DiffOp("equal", i - size, i, j - size, j));
    }

    return answer;
  }
}
