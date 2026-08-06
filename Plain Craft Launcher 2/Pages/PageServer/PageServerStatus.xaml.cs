using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Link.McPing;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.ResourceProject;
using PCL.Network;
using PCL.Network.Loaders;
using PCL.Utils;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PCL;

public partial class PageServerStatus
{
    public PageServerStatus()
    {
        InitializeComponent();
        _ = GetServerInfo();
    }

    public async void Reload()
    {
        await GetServerInfo();
        HintService.Hint(Lang.Text("Server.Status.GetStatusSuccess"), HintType.Success, false);
    }

    #region 页面切换

    /// <summary>
    ///     当前页面的编号。
    /// </summary>
    public FormMain.PageSubType pageID = FormMain.PageSubType.ServerStatus;

    #endregion

    #region 服务器状态


    private void BtnRefresh_Click(object sender, MouseButtonEventArgs e)
    {
        HintService.Hint(Lang.Text("Server.Status.WaitStatus"), log:false);
        Reload();
    }

    string serverUrl = "play.simpfun.cn:19321";

    private async Task GetServerInfo()
    {
        try
        {
            var addr = await ServerAddressResolver.GetResolvedServerAddressAsync(serverUrl);

            using (var query = McPingServiceFactory.CreateService(addr.Host, addr.Ip, addr.Port))
            {
                var ret = await query.PingAsync();

                if (ret is null) throw new Exception(Lang.Text("Tools.ServerQuery.State.NoInfo"));

                // 1. 服务器图标
                if (!string.IsNullOrEmpty(ret.Favicon))
                {
                    var bytes = Convert.FromBase64String(ret.Favicon.Replace("data:image/png;base64,", ""));
                    using (var ms = new MemoryStream(bytes))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = ms;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ServerImage.Source = bitmap;
                    }
                }

                // 2. MOTD（彩色）
                if (!string.IsNullOrEmpty(ret.Description))
                {
                    var motd = new MCMotd(ret.Description);
                    string ?coloredText = motd.description;
                    if (coloredText is not null)
                    {
                        var motdBlock = WpfMotdRenderer.Parse(coloredText);
                        DescriptionContainer.Content = motdBlock;
                    }
                }

                // 3. 在线人数 & 版本
                TxtOnlineCount.Text = ret.Players is not null ? $"{ret.Players.Online}/{ret.Players.Max}" : "N/A";
                TxtVersion.Text = ret.Version is not null ? ret.Version.Name : "未知";

                // 4. 切换显示状态
                DataPanel.Visibility = Visibility.Visible;
                WaitPanel.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "服务器查询失败", ModBase.LogLevel.Feedback);
            // 可选：显示错误状态
            Dispatcher.Invoke(() =>
            {
                WaitPanel.Visibility = Visibility.Collapsed;
                DataPanel.Visibility = Visibility.Visible;
                TxtOnlineCount.Text = "查询失败";
                TxtVersion.Text = "错误";
            });
        }
    }

    #endregion
}
