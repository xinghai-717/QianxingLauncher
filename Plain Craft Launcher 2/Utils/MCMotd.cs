using System.Text;

namespace PCL.Utils;

public struct MCMotd
{
    public string? description = null;
    Players? players = null;
    Version? version = null;
    string? favicon = null;
    Mods? mods = null;
    public MCMotd(string motd)
    {
        if (!motd.StartsWith('{') || !motd.EndsWith('}'))
            description = motd.Replace('\u00A7', '¡ì');
        else
        {
            JsonElement main = JsonSerializer.Deserialize<JsonElement>(motd);
            JsonElement tmp;

            if (main.TryGetProperty("description", out tmp))
                description = new JsonText(tmp).ToString().Replace('\u00A7', '¡ì');
            else if (main.TryGetProperty("text", out tmp))
                description = new JsonText(tmp).ToString().Replace('\u00A7', '¡ì');

            if (main.TryGetProperty("players", out tmp) && tmp.ValueKind == JsonValueKind.Object)
                players = tmp.Deserialize<Players>();
            if (main.TryGetProperty("version", out tmp) && tmp.ValueKind == JsonValueKind.Object)
                version = tmp.Deserialize<Version>();
            if (main.TryGetProperty("favicon", out tmp) && tmp.ValueKind == JsonValueKind.String)
                favicon = tmp.GetString();

            if (main.TryGetProperty("modinfo", out tmp) && tmp.ValueKind == JsonValueKind.Object)
            {
                Mods mods = new Mods();
                mods.type = "unknown";
                mods.Complete = true;
                JsonElement tmp2;

                if (tmp.TryGetProperty("type", out tmp2) && tmp2.ValueKind == JsonValueKind.String)
                    mods.type = tmp2.GetString();
                if (tmp.TryGetProperty("modList", out tmp2) && tmp2.ValueKind == JsonValueKind.Array)
                {
                    int len = tmp2.GetArrayLength();

                    Mod[] modAr = new Mod[len];
                    JsonElement temp;

                    for (int i = 0; i < len; i++)
                    {
                        if (tmp2[i].TryGetProperty("modid", out temp) && temp.ValueKind == JsonValueKind.String)
                            modAr[i].name = temp.GetString();
                        if (tmp2[i].TryGetProperty("version", out temp) && temp.ValueKind == JsonValueKind.String)
                            modAr[i].marker = temp.GetString();
                    }

                    mods.mods = modAr;
                }

                this.mods = mods;
            }
            else if (main.TryGetProperty("forgeData", out tmp) && tmp.ValueKind == JsonValueKind.Object)
            {
                Mods mods = new Mods();
                mods.Complete = true;
                mods.type = "unknown";
                JsonElement tmp2;

                if (tmp.TryGetProperty("fmlNetworkVersion", out tmp2) && tmp2.ValueKind == JsonValueKind.Number)
                    mods.type = $"FML{tmp2.GetInt32()}";
                if (tmp.TryGetProperty("truncated", out tmp2) && tmp2.ValueKind == JsonValueKind.True)
                    mods.Complete = false;
                if (tmp.TryGetProperty("mods", out tmp2) && tmp2.ValueKind == JsonValueKind.Array)
                {
                    int len = tmp2.GetArrayLength();

                    Mod[] modAr = new Mod[len];
                    JsonElement temp;

                    for (int i = 0; i < len; i++)
                    {
                        if (tmp2[i].TryGetProperty("modId", out temp) && temp.ValueKind == JsonValueKind.String)
                            modAr[i].name = temp.GetString();
                        if (tmp2[i].TryGetProperty("modmarker", out temp) && temp.ValueKind == JsonValueKind.String)
                            modAr[i].marker = temp.GetString();
                    }

                    mods.mods = modAr;
                }

                this.mods = mods;
            }
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();

        builder.Append(description);
        builder.AppendLine("¡ìr");
        builder.Append(players?.ToString());
        builder.AppendLine("¡ìr");
        builder.Append(version?.ToString());
        builder.AppendLine("¡ìr");
        builder.Append(mods?.ToString());
        builder.AppendLine("¡ìr");

        return builder.ToString();
    }

    static string? removeCodes(string? str)
    {
        if (str is null)
            return null;

        StringBuilder builder = new StringBuilder();

        string[] parts = str.Split('¡ì');

        for (int i = 0; i < parts.Length; i++)
        {
            if (i == 0)
            {
                builder.Append(parts[i]);
                continue;
            }
            else if (parts[i].Length == 0)
            {
                builder.Append('¡ì');
                continue;
            }

            if ((parts[i][0] >= '0' && parts[i][0] <= '9') ||
                (parts[i][0] >= 'a' && parts[i][0] <= 'f') ||
                (parts[i][0] == 'r') ||
                (parts[i][0] == 'l') ||
                (parts[i][0] == 'm') ||
                (parts[i][0] == 'n') ||
                (parts[i][0] == 'o') ||
                (parts[i][0] == 'k'))
            {
                builder.Append(parts[i].Substring(1));
            }
            else if (parts[i][0] == '#')
            {
                builder.Append(parts[i].Substring(7));
            }
            else
            {
                builder.Append('¡ì');
                builder.Append(parts[i]);
            }
        }

        return builder.ToString();
    }

    public string NoColorCodes()
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine(removeCodes(description));
        builder.AppendLine(removeCodes(players?.ToString()));
        builder.AppendLine(removeCodes(version?.ToString()));
        builder.AppendLine(removeCodes(mods?.ToString()));

        return builder.ToString();
    }

    static string[,] Formats = new string[,]
        {
            { "¡ìr", "\x1b[0m" },
            { "¡ìn", "" },
            { "¡ìl", "\x1b[1m" },
            { "¡ìo", "" },
            { "¡ìm", "" },
            { "¡ìk", "" },
            { "¡ì0", "\x1b[0m\x1b[38;5;0m" },
            { "¡ì1", "\x1b[0m\x1b[38;5;19m" },
            { "¡ì2", "\x1b[0m\x1b[38;5;34m" },
            { "¡ì3", "\x1b[0m\x1b[38;5;37m" },
            { "¡ì4", "\x1b[0m\x1b[38;5;124m" },
            { "¡ì5", "\x1b[0m\x1b[38;5;127m" },
            { "¡ì6", "\x1b[0m\x1b[38;5;214m" },
            { "¡ì7", "\x1b[0m\x1b[38;5;248m" },
            { "¡ì8", "\x1b[0m\x1b[38;5;240m" },
            { "¡ì9", "\x1b[0m\x1b[38;5;63m" },
            { "¡ìa", "\x1b[0m\x1b[38;5;83m" },
            { "¡ìb", "\x1b[0m\x1b[38;5;87m" },
            { "¡ìc", "\x1b[0m\x1b[38;5;203m" },
            { "¡ìd", "\x1b[0m\x1b[38;5;207m" },
            { "¡ìe", "\x1b[0m\x1b[38;5;227m" },
            { "¡ìf", "\x1b[0m\x1b[38;5;15m" },


        };

    static string ColorizeText(string text)
    {
        for (int i = 0; i < Formats.GetLength(0); i++)
        {
            if (text.Contains(Formats[i, 0]))
                text = text.Replace(Formats[i, 0], Formats[i, 1]);
        }
        text += Formats[0, 1];

        return text;
    }

    public string Colorized()
    {
        return ColorizeText(ToString() + "¡ìr");
    }
}
public struct Mod
{
    public string? name { get; set; }
    public string? marker { get; set; }

    public override string ToString()
    {
        return $"{name} ({marker})";
    }
}
public struct Mods
{
    public string? type { get; set; }

    public bool Complete { get; set; }
    public Mod[]? mods { get; set; }

    public override string ToString()
    {
        return $"{type} - {mods?.Length} mods - ({(Complete ? "Complete" : "Incomplete")})";
    }
}

public struct Version
{
    public string? name { get; set; }
    public int? protocol { get; set; }

    public override string ToString()
    {
        return $"{name} ({protocol})";
    }
}

public struct PlayerSample
{
    public string? name { get; set; }
    public string? id { get; set; }
}

public struct Players
{

    public int? max { get; set; }
    public int? online { get; set; }

    public PlayerSample[]? sample { get; set; }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        if (sample is not null && sample.Length > 0)
        {
            for (int i = 0; i < Math.Min(10, sample.Length); i++)
            {
                builder.Append($"{sample[i].name} ");
            }
        }

        return $"{online}/{max} {builder}";
    }
}

struct JsonText
{

    StringBuilder value;

    public JsonText(JsonElement element, StringBuilder? builder = null)
    {
        value = builder is not null ? builder : new StringBuilder();
        extractText(element);
    }

    void extractText(JsonElement e)
    {
        JsonElement tmp;

        if (e.ValueKind == JsonValueKind.String)
        {
            value.Append(e.GetString());
        }
        else if (e.ValueKind == JsonValueKind.Object)
        {
            if (e.TryGetProperty("color", out tmp) && tmp.ValueKind == JsonValueKind.String)
            {
                string? val = tmp.GetString();

                switch (val)
                {
                    case "reset":
                        value.Append("¡ìr");
                        break;

                    case "black":
                        value.Append("¡ì0");
                        break;

                    case "dark_blue":
                        value.Append("¡ì1");
                        break;

                    case "dark_green":
                        value.Append("¡ì2");
                        break;

                    case "dark_aqua":
                        value.Append("¡ì3");
                        break;

                    case "dark_red":
                        value.Append("¡ì4");
                        break;

                    case "dark_purple":
                        value.Append("¡ì5");
                        break;

                    case "gold":
                        value.Append("¡ì6");
                        break;

                    case "gray":
                        value.Append("¡ì7");
                        break;

                    case "dark_gray":
                        value.Append("¡ì8");
                        break;

                    case "blue":
                        value.Append("¡ì9");
                        break;

                    case "green":
                        value.Append("¡ìa");
                        break;

                    case "aqua":
                        value.Append("¡ìb");
                        break;

                    case "red":
                        value.Append("¡ìc");
                        break;

                    case "light_purple":
                        value.Append("¡ìd");
                        break;

                    case "yellow":
                        value.Append("¡ìe");
                        break;

                    case "white":
                        value.Append("¡ìf");
                        break;

                    default:

                        if (val is not null && val.StartsWith('#') && val.Length == 7)
                        {
                            value.Append('¡ì');
                            value.Append(val);
                        }

                        break;
                }
            }

            if (e.TryGetProperty("bold", out tmp) && tmp.ValueKind == JsonValueKind.True)
                value.Append("¡ìl");
            if (e.TryGetProperty("italic", out tmp) && tmp.ValueKind == JsonValueKind.True)
                value.Append("¡ìo");
            if (e.TryGetProperty("underlined", out tmp) && tmp.ValueKind == JsonValueKind.True)
                value.Append("¡ìn");
            if (e.TryGetProperty("strikethrough", out tmp) && tmp.ValueKind == JsonValueKind.True)
                value.Append("¡ìm");
            if (e.TryGetProperty("obfuscated", out tmp) && tmp.ValueKind == JsonValueKind.True)
                value.Append("¡ìk");

            if (e.TryGetProperty("text", out tmp) && tmp.ValueKind == JsonValueKind.String)
                value.Append(tmp.GetString());

            if (e.TryGetProperty("extra", out tmp) && tmp.ValueKind == JsonValueKind.Array)
            {
                int len = tmp.GetArrayLength();

                for (int i = 0; i < len; i++)
                {
                    new JsonText(tmp[i], value);
                }
            }

        }
    }

    public override string ToString()
    {
        return value.ToString();
    }
}