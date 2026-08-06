using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PCL.Utils;

public static class WpfMotdRenderer
{
    private static readonly Dictionary<char, SolidColorBrush> ColorMap = new()
    {
        {'0', Brushes.Black},
        {'1', Brushes.DarkBlue},
        {'2', Brushes.DarkGreen},
        {'3', Brushes.DarkCyan},
        {'4', Brushes.DarkRed},
        {'5', Brushes.DarkMagenta},
        {'6', Brushes.Gold},
        {'7', Brushes.Gray},
        {'8', Brushes.DarkGray},
        {'9', Brushes.Blue},
        {'a', Brushes.Green},
        {'b', Brushes.Cyan},
        {'c', Brushes.Red},
        {'d', Brushes.Magenta},
        {'e', Brushes.Yellow},
        {'f', Brushes.White}
    };

    public static TextBlock Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new TextBlock();

        var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var segments = Regex.Split(input, @"(?=§)");

        // 当前样式状态
        var currentBrush = Brushes.White;
        var currentWeight = FontWeights.Normal;
        var currentStyle = FontStyles.Normal;
        bool isUnderline = false;
        bool isStrikethrough = false;

        foreach (var seg in segments)
        {
            if (string.IsNullOrEmpty(seg))
                continue;

            if (seg.StartsWith('§') && seg.Length >= 2)
            {
                char code = char.ToLower(seg[1]);
                string content = seg.Length > 2 ? seg.Substring(2) : "";

                // 处理颜色代码
                if (ColorMap.TryGetValue(code, out var brush))
                {
                    currentBrush = brush;
                }
                // 处理格式代码
                else
                {
                    switch (code)
                    {
                        case 'l': currentWeight = FontWeights.Bold; break;
                        case 'o': currentStyle = FontStyles.Italic; break;
                        case 'n': isUnderline = true; break;
                        case 'm': isStrikethrough = true; break;
                        case 'r':
                            currentBrush = Brushes.White;
                            currentWeight = FontWeights.Normal;
                            currentStyle = FontStyles.Normal;
                            isUnderline = false;
                            isStrikethrough = false;
                            break;
                        default:
                            // 未知代码忽略
                            break;
                    }
                }

                // 如果有内容，创建 Run 并应用当前样式
                if (!string.IsNullOrEmpty(content))
                {
                    var run = CreateRun(content, currentBrush, currentWeight, currentStyle, isUnderline, isStrikethrough);
                    textBlock.Inlines.Add(run);
                }
            }
            else
            {
                // 无颜色代码的普通文本
                var run = CreateRun(seg, currentBrush, currentWeight, currentStyle, isUnderline, isStrikethrough);
                textBlock.Inlines.Add(run);
            }
        }

        return textBlock;
    }

    private static Run CreateRun(string text, SolidColorBrush brush, FontWeight weight, FontStyle style, bool underline, bool strikethrough)
    {
        var run = new Run(text)
        {
            Foreground = brush,
            FontWeight = weight,
            FontStyle = style
        };

        // 动态构建装饰集合
        var decorations = new TextDecorationCollection();
        if (underline)
            decorations.Add(new TextDecoration { Location = TextDecorationLocation.Underline });
        if (strikethrough)
            decorations.Add(new TextDecoration { Location = TextDecorationLocation.Strikethrough });

        run.TextDecorations = decorations.Count > 0 ? decorations : null;
        return run;
    }
}