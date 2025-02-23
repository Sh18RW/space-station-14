using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Chat.Systems;

namespace Content.Server._CP.TTS.Systems;

public sealed partial class TTSSystem
{
    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!_isEnabled)
        {
            return;
        }
        args.Message = args.Message.Replace("+", "");
    }

    private string Sanitize(string text)
    {
        text = text.Trim();
        text = Regex.Replace(text, @"[^a-zA-Z0-9,\-+?!. ]", "");
        // text = Regex.Replace(text, @"[a-zA-Z]", ReplaceLat2Cyr, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?<![a-zA-Z])[a-zA-Zа]+?(?![a-zA-Z])", ReplaceMatchedWord, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?<=[1-90])(\.|,)(?=[1-90])", " point ");
        text = Regex.Replace(text, @"\d+", ReplaceWord2Num);
        text = text.Trim();
        return text;
    }

    private string ReplaceMatchedWord(Match word)
    {
        return WordReplacement.TryGetValue(word.Value.ToLower(), out var replace) ? replace : word.Value;
    }

    private string ReplaceWord2Num(Match word)
    {
        return !long.TryParse(word.Value, out var number) ? word.Value : NumberConverter.NumberToText(number);
    }

    private static readonly IReadOnlyDictionary<string, string> WordReplacement =
        new Dictionary<string, string>()
        {
            { "sw", "StarWark" }
        };
}

// Source: https://codelab.ru/s/csharp/digits2phrase
public static class NumberConverter
{
    private static readonly string[] Frac20Male =
    [
        "", "one", "two", "three", "four", "five", "six",
        "seven", "eight", "nine", "ten", "eleven",
        "twelve", "thirteen", "fourteen", "fifteen",
        "sixteen", "seventeen", "eighteen", "nineteen",
    ];

    private static readonly string[] Frac20Female =
    [
        "", "one", "two", "three", "four", "five", "six",
        "seven", "eight", "nine", "ten", "eleven",
        "twelve", "thirteen", "fourteen", "fifteen",
        "sixteen", "seventeen", "eighteen", "nineteen",
    ];

	private static readonly string[] Hunds =
    [
        "", "hundred", "two hundred", "three hundred", "four hundred",
		"five hundred", "six hundred", "seven hundred", "eight hundred", "nine hundred",
    ];

	private static readonly string[] Tens =
    [
        "", "ten", "twenty", "thirty", "forty", "fifty",
		"sixty", "seventy", "eighty", "ninety",
    ];

	public static string NumberToText(long value, bool male = true)
    {
        if (value >= (long)Math.Pow(10, 15))
        {
            return string.Empty;
        }

        if (value == 0)
        {
            return "ноль";
        }

        var str = new StringBuilder();

		if (value < 0)
		{
			str.Append("минус");
			value = -value;
		}

        value = AppendPeriod(value, 1000000000000, str, "trillion", "trillion", "trillion", true);
        value = AppendPeriod(value, 1000000000, str, "billion", "billion", "billion", true);
        value = AppendPeriod(value, 1000000, str, "million", "million", "million", true);
        value = AppendPeriod(value, 1000, str, "thousand", "thousand", "thousand", false);

		var hundreds = (int)(value / 100);
		if (hundreds != 0)
			AppendWithSpace(str, Hunds[hundreds]);

		var less100 = (int)(value % 100);
        var frac20 = male ? Frac20Male : Frac20Female;
		if (less100 < 20)
			AppendWithSpace(str, frac20[less100]);
		else
		{
			var tens = less100 / 10;
			AppendWithSpace(str, Tens[tens]);
			var less10 = less100 % 10;
			if (less10 != 0)
				str.Append(" " + frac20[less100%10]);
		}

		return str.ToString();
	}

	private static void AppendWithSpace(StringBuilder stringBuilder, string str)
	{
		if (stringBuilder.Length > 0)
			stringBuilder.Append(' ');
		stringBuilder.Append(str);
	}

	private static long AppendPeriod(
        long value,
        long power,
		StringBuilder str,
		string declension1,
		string declension2,
		string declension5,
		bool male)
	{
		var thousands = (int)(value / power);
		if (thousands > 0)
		{
			AppendWithSpace(str, NumberToText(thousands, male, declension1, declension2, declension5));
			return value % power;
		}
		return value;
	}

	private static string NumberToText(
        long value,
        bool male,
		string valueDeclensionFor1,
		string valueDeclensionFor2,
		string valueDeclensionFor5)
	{
		return
            NumberToText(value, male)
			+ " "
			+ GetDeclension((int)(value % 10), valueDeclensionFor1, valueDeclensionFor2, valueDeclensionFor5);
	}

	private static string GetDeclension(int val, string one, string two, string five)
	{
		var t = (val % 100 > 20) ? val % 10 : val % 20;

		switch (t)
		{
			case 1:
				return one;
			case 2:
			case 3:
			case 4:
				return two;
			default:
				return five;
		}
	}
}
