using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PCL;

public partial class PageSetupAbout
{
    // PCL CE彩蛋
    private int clickCount;

    // 我的菜单
    private int xinghaiCount = 0;
    private bool wantLearn = false;

    private new bool isLoaded;

    public PageSetupAbout()
    {
        InitializeComponent();
        Loaded += PageOtherAbout_Loaded;
    }

    public ObservableCollection<GitHubContributor> Contributors { get; set; } = new();

    private void PageOtherAbout_Loaded(object sender, RoutedEventArgs e)
    {
        // 重复加载部分
        PanBack.ScrollToHome();

        // 非重复加载部分
        if (isLoaded)
            return;
        isLoaded = true;

        ItemAboutPcl.Info = ItemAboutPcl.Info.Replace("%VERSION%", ModBase.versionBaseName)
            .Replace("%VERSIONCODE%", ModBase.versionCode.ToString()).Replace("%BRANCH%", ModBase.versionBranchName)
            .Replace("%COMMIT_HASH%", ModBase.commitHashShort);

        if (!Lang.IsChineseMainland)
        {
            RowMcmod.Height = new GridLength(0);
        }

        LoadContributersAsync();
    }

    private async void LoadContributersAsync()
    {
        try
        {
            using (var response = await HttpRequest
                       .Create("https://api.github.com/xinghai-717/QianxingLauncher").SendAsync())
            {
                response.EnsureSuccessStatusCode();
                var cos = await response.AsJsonAsync<List<GitHubContributor>>(JsonCompat.SerializerOptions);
                Contributors.Clear();
                foreach (var item in cos)
                    Contributors.Add((GitHubContributor)item);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, Lang.Text("Setup.About.Error.LoadContributorsFailed"));
        }
    }

    private void ImgPCLCommunity_Click(object sender, MouseButtonEventArgs e)
    {
        ModAnimation.AniStart(new[] { ModAnimation.AaRotateTransform(sender, 360d) });
    }

    private void CopyLaodiQQ_Click(object sender, MouseButtonEventArgs e)
    {
        Clipboard.SetText("3291596227");
        HintService.Hint("复制成功!", HintType.Success);
    }

    private async void XinghaiTxt_Click(object sender, MouseButtonEventArgs e)
    {
        clickCount += 1;
        switch (clickCount)
        {
            case 20:
                {
                    HintService.Hint("点我干嘛");
                    break;
                }
            case 40:
                {
                    HintService.Hint("还点");
                    break;
                }
            case 50:
                {
                    switch (ModMain.MyMsgBox("你想知道我为啥叫这个名字吗?", "你想知道什么?", Lang.Text("Setup.About.EasterEgg.Bored.Yes"), Lang.Text("Setup.About.EasterEgg.Bored.No")))
                    {
                        case 1:
                        {
                            wantLearn = true;
                            HintService.Hint("你居然想知道我为啥叫这个名字");
                            break;
                        }
                    }
                    break;
                }
            case 70:
                {
                    HintService.Hint("没有更多了");
                    break;
                }
            case 100:
                {
                    switch (ModMain.MyMsgBox("不建议你看哦", "你想不想看我另一个头像?", "想!", "不想?"))
                    {
                        case 1:
                            {
                                HintService.Hint("头像已经换了你再看看呢");
                                await Task.Delay(5000);
                                switch (ModMain.MyMsgBox("认真选哦", "我头像好看吗?", "好看!", "不好看?"))
                                {
                                    case 1:
                                        {
                                            HintService.Hint("非常感谢你的喜欢!");
                                            break;
                                        }
                                    case 2:
                                        {
                                            HintService.Hint("e...我尊重你的评价");
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                    break;
                }
        }
    }


    private void ImgPCLLogo_Click(object sender, MouseButtonEventArgs e)
    {
        if (clickCount < 200)
        {
            clickCount += 1;
            switch (clickCount)
            {
                case 5:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.NiceClick"));
                    break;
                }
                case 15:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.StillClicking"));
                    break;
                }
                case 25:
                {
                    switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Bored.Message"), Lang.Text("Setup.About.EasterEgg.Bored.Title"), Lang.Text("Setup.About.EasterEgg.Bored.Yes"), Lang.Text("Setup.About.EasterEgg.Bored.No")))
                    {
                        case 2:
                        {
                            HintService.Hint(Lang.Text("Setup.About.EasterEgg.Bored.Response"));
                            break;
                        }
                    }

                    break;
                }
                case 50:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.Encouragement"));
                    break;
                }
                case 75:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.HiddenTheme"));
                    break;
                }
                case 100:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.StillStaring"));
                    break;
                }
                case 130:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.NothingBehind"));
                    break;
                }
                case 150:
                {
                    switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Tired.Message1"), Lang.Text("Setup.About.EasterEgg.Tired.Title1"), Lang.Text("Setup.About.EasterEgg.Tired.Exhausted"), Lang.Text("Setup.About.EasterEgg.Tired.NotTired")))
                    {
                        case 1:
                        {
                            HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.StopClicking"));
                            break;
                        }
                        case 2:
                        {
                            switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Tired.Message2"), Lang.Text("Setup.About.EasterEgg.Tired.Title2"), Lang.Text("Setup.About.EasterEgg.Tired.Exhausted"), Lang.Text("Setup.About.EasterEgg.Tired.NotTired")))
                            {
                                case 1:
                                {
                                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.StopClicking"));
                                    break;
                                }
                                case 2:
                                {
                                    switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Tired.Message3"), Lang.Text("Setup.About.EasterEgg.Tired.Title3"), Lang.Text("Setup.About.EasterEgg.Tired.Exhausted"), Lang.Text("Setup.About.EasterEgg.Tired.ReallyNotTired")))
                                    {
                                        case 1:
                                        {
                                            HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.StopClicking"));
                                            break;
                                        }
                                        case 2:
                                        {
                                            HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.FinallyGiveUp"));
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }

                            break;
                        }
                    }

                    break;
                }
                case 200:
                {
                    HintService.Hint(Lang.Text("Setup.About.EasterEgg.ClickDisabled"));
                    ImgPCLLogo.IsHitTestVisible = false;
                    return;
                }
            }

            var rand = new Random();
            var mx = rand.Next(-1, 1);
            if (mx == 0)
                mx = 1;
            var my = rand.Next(-1, 1);
            if (my == 0)
                my = 1;
            ModAnimation.AniStart(new[]
            {
                ModAnimation.AaTranslateX(sender, mx, 0), ModAnimation.AaTranslateY(sender, my, 0),
                ModAnimation.AaTranslateX(sender, -mx, 0, 100), ModAnimation.AaTranslateY(sender, -my, 0, 100)
            });
        }
    }

    public class GitHubContributor
    {
        [JsonPropertyName("login")] public string Login { get; set; }

        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }

        [JsonPropertyName("html_url")] public string HtmlUrl { get; set; }

        [JsonPropertyName("contributions")] public int Contributions { get; set; }
    }
}
