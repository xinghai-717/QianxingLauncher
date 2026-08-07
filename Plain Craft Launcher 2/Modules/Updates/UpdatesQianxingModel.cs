using System;
using System.Collections.Generic;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Network;
using PCL.Network.Loaders;
using PCL.Core.IO.Net.Http;

namespace PCL;

public class UpdatesQianxingModel : IUpdateSource // 千星启动器专属更新系统
{
    private const string UpdateServerBaseUrl =
        "https://serverupdate.wrh6.qzz.io/launcher";

    public string SourceName { get; set; } = "千星启动器更新源";

    public bool IsAvailable()
    {
        return true; // 或根据需要检查网络
    }

    public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
    {
        using (var response = HttpRequest.Create(GetUrl(channel, arch))
                   .SendAsync()
                   .GetAwaiter()
                   .GetResult())
        {
            var ret = (JsonObject)ModBase.GetJson(response.AsString());
            if ((int)ret["code"] != 0)
                throw new Exception("千星更新服务器返回数据不成功");
            var data = ret["data"];
            return new VersionDataModel
            {
                Source = SourceName,
                VersionCode = (int)data["version_number"],
                VersionName = (string)data["version_name"],
                Sha256 = (string)data["sha256"],
                Changelog = (string)data["release_note"]
            };
        }
    }

    public bool RefreshCache() => true;

    public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
    {
        var latest = GetLatestVersion(channel, arch);
        if (latest == null) return true;
        var latestVer = SemVer.Parse(latest.VersionName);
        if (latestVer == null) return true;
        if (currentVersion != null && latestVer > currentVersion)
            return false;
        if (currentVersion == latestVer && currentVersionCode < latest.VersionCode)
            return false;
        return true;
    }

    public VersionAnnouncementDataModel GetAnnouncementList()
    {
        throw new Exception("千星启动器暂无公告系统");
    }

    public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
    {
        var loaders = new List<ModLoader.LoaderBase>();

        // 获取下载信息（与 GetLatestVersion 类似，但提取 url）
        loaders.Add(new ModLoader.LoaderTask<int, List<DownloadFile>>(Lang.Text("Update.Task.GetDownloadInfo"), load =>
        {
            using (var response = HttpRequest.Create(GetUrl(channel, arch))
                       .SendAsync()
                       .GetAwaiter()
                       .GetResult())
            {
                var ret = (JsonObject)ModBase.GetJson(response.AsString());
                if ((int)ret["code"] != 0)
                    throw new Exception("千星更新服务器返回数据不成功");
                var data = ret["data"];
                var dlUrl = data["url"]?.ToString();
                if (string.IsNullOrWhiteSpace(dlUrl))
                    throw new Exception("千星启动器下载链接不存在");
                load.output = new List<DownloadFile> { new(new[] { dlUrl }, output) };
            }
        }));

        // 执行下载
        loaders.Add(new LoaderDownload(Lang.Text("Update.Task.DownloadUpdateFile"), new List<DownloadFile>()));
        return loaders;
    }

    private string GetUrl(UpdateChannel channel, UpdateArch arch)
    {
        var reqUrl = UpdateServerBaseUrl;
        reqUrl = reqUrl.Replace("{arch}", arch.ToString());
        reqUrl = reqUrl.Replace("{channel}", channel.ToString());
        return reqUrl;
    }
}