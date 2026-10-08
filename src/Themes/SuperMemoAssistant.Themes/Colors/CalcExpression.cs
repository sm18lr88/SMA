// Evaluates the arithmetic inside CSS calc() once units are stripped: numbers, + - * / //, unary signs and parentheses.
using System.Globalization;
using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Themes.Colors;

internal static partial class CalcExpression
{
  [GeneratedRegex(@"(\d)(deg|%|px|turn|rad)")]
  private static partial Regex UnitRegex();

  [GeneratedRegex(@"^[0-9\s.+\-*/()eE]+$")]
  private static partial Regex AllowedRegex();

  /// <summary>Strips calc( and unit suffixes, then evaluates. Throws <see cref="FormatException" /> when the text is not plain arithmetic.</summary>
  public static double Evaluate(string expression)
  {
    var text = expression.Replace("calc(", "(", StringComparison.Ordinal);

    text = UnitRegex().Replace(text, "$1");

    if (!AllowedRegex().IsMatch(text))
      throw new FormatException(expression);

    var parser = new Parser(text);
    var value  = parser.ParseExpression();

    parser.SkipSpaces();

    if (!parser.AtEnd)
      throw new FormatException(expression);

    return value;
  }

  private sealed class Parser(string text)
  {
    private int _pos;

    public bool AtEnd => _pos >= text.Length;

    public void SkipSpaces()
    {
      while (_pos < text.Length && char.IsWhiteSpace(text[_pos]))
        _pos++;
    }

    public double ParseExpression()
    {
      var value = ParseTerm();

      while (true)
      {
        SkipSpaces();

        if (Peek() is '+' or '-')
        {
          var plus = text[_pos++] == '+';
          var rhs  = ParseTerm();

          value = plus ? value + rhs : value - rhs;
        }
        else
        {
          return value;
        }
      }
    }

    private double ParseTerm()
    {
      var value = ParseFactor();

      while (true)
      {
        SkipSpaces();

        if (Peek() != '*' && Peek() != '/')
          return value;

        var op = text[_pos++];

        if (op == '*' && Peek() == '*')
          throw new FormatException("power is not supported");

        var floor = op == '/' && Peek() == '/';

        if (floor)
          _pos++;

        var rhs = ParseFactor();

        if (op == '/' && rhs == 0)
          throw new DivideByZeroException();

        value = op == '*' ? value * rhs : floor ? Math.Floor(value / rhs) : value / rhs;
      }
    }

    private double ParseFactor()
    {
      SkipSpaces();

      if (Peek() is '+' or '-')
      {
        var negative = text[_pos++] == '-';
        var value    = ParseFactor();

        return negative ? -value : value;
      }

      if (Peek() == '(')
      {
        _pos++;

        var value = ParseExpression();

        SkipSpaces();

        if (Peek() != ')')
          throw new FormatException("missing )");

        _pos++;

        return value;
      }

      return ParseNumber();
    }

    private double ParseNumber()
    {
      var start  = _pos;
      var digits = 0;

      while (Peek() is >= '0' and <= '9') { _pos++; digits++; }

      var isFloat = false;

      if (Peek() == '.')
      {
        isFloat = true;
        _pos++;

        while (Peek() is >= '0' and <= '9') { _pos++; digits++; }
      }

      if (digits == 0)
        throw new FormatException("number expected");

      if (Peek() is 'e' or 'E')
      {
        _pos++;

        if (Peek() is '+' or '-')
          _pos++;

        var expDigits = 0;

        while (Peek() is >= '0' and <= '9') { _pos++; expDigits++; }

        if (expDigits == 0)
          throw new FormatException("bad exponent");

        isFloat = true;
      }

      var literal = text[start.._pos];

      // Python rejects integer literals such as 010; accept only zeros or a literal without a leading zero.
      if (!isFloat && literal.Length > 1 && literal[0] == '0' && literal.Any(c => c != '0'))
        throw new FormatException("leading zeros");

      if (Peek() is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '.')
        throw new FormatException("unexpected character after number");

      return double.Parse(literal, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private char Peek() => _pos < text.Length ? text[_pos] : '\0';
  }
}
