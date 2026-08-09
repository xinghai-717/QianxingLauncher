using Microsoft.Extensions.Logging;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.ResourceProject;
using PCL.Network;
using PCL.Network.Loaders;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PCL;

public partial class PageServerUpdate
{
    string[]? mods = null;
    string[]? names = null;
    string[]? urls = null;
    
    public PageServerUpdate()
    {
        InitializeComponent();
        if (ModInstanceList.McMcInstanceSelected is null)
        {
            BtnDownload.Visibility = Visibility.Visible;
            BtnUpdateMod.Visibility = Visibility.Collapsed;
        }
        else
        {
            BtnDownload.Visibility = Visibility.Collapsed;
            BtnUpdateMod.Visibility = Visibility.Visible;
        }
            
        _ = UpdateUIAsync();
    }

    public async void Reload()
    {
        HintService.Hint(Lang.Text("Server.Update.WaitStatus"), log: false);
        if (ModInstanceList.McMcInstanceSelected is null)
        {
            BtnDownload.Visibility = Visibility.Visible;
            BtnUpdateMod.Visibility = Visibility.Collapsed;
        } 
        else
        { 
            BtnDownload.Visibility = Visibility.Collapsed;
            BtnUpdateMod.Visibility = Visibility.Visible;
        }
            
        await UpdateUIAsync();
    }

    private void BtnRefresh_Click(object sender, MouseButtonEventArgs e)
    {
        if (ModInstanceList.McMcInstanceSelected is null)
        {
            BtnDownload.Visibility = Visibility.Visible;
            BtnUpdateMod.Visibility = Visibility.Collapsed;
        } 
        else
        { 
            BtnDownload.Visibility = Visibility.Collapsed;
            BtnUpdateMod.Visibility = Visibility.Visible;
        }
        Reload();
    }

    public static string ComputeFileSHA1(string filePath)
    {
        using (var sha1 = SHA1.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hash = sha1.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public static bool VerifyFileSHA1(string filePath, string expectedSHA1)
    {
        if (!File.Exists(filePath)) return false;
        string actualSHA1 = ComputeFileSHA1(filePath);
        return string.Equals(actualSHA1, expectedSHA1, StringComparison.OrdinalIgnoreCase);
    }

    private async void BtnDownload_Click(object sender, MouseButtonEventArgs e)
    {
        Dispatcher.Invoke(() => HintService.Hint("正在准备下载，请稍等片刻...", HintType.Info,false));

        var response = await HttpRequest.Create("https://serverupdate.wrh6.qzz.io/mrpack").SendAsync(); // 假设 SendAsync 返回 Task<HttpResponse>

        string jsonString = response.AsString();
        var jsonNode = JsonNode.Parse(jsonString);
        if (jsonNode == null)
        {
            ModBase.Log("JSON 解析失败", ModBase.LogLevel.Msgbox);
            return;
        }

        string? url = jsonNode["data"]?["url"]?.GetValue<string>();
        string? sha1 = jsonNode["data"]?["sha1"]?.GetValue<string>();

        if (url == null)
        {
            ModBase.Log("获取整合包地址失败", ModBase.LogLevel.Feedback);
            return;
        }

        if (sha1 == null)
        {
            ModBase.Log("未获取到SHA1", ModBase.LogLevel.Feedback);
            return;
        }

        var uri = new Uri(url);
        string fileName = "modpack.mrpack";

        //string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        var folder = ModBase.exePath; // 确保路径以反斜杠结尾
        if (!folder.EndsWith(@"\")) folder += @"\";

        //string extractPath = folder+nameWithoutExt;

        var loader = PageToolsTest.StartCustomDownload(url, fileName, folder, txt : "千星整合包安装");

        if (loader == null) return;

        // 等待完成
        var tcs = new TaskCompletionSource<bool>();

        loader.OnStateChanged = (s) =>
        {
            if (s.State == ModBase.LoadState.Finished)
            {
                if (!VerifyFileSHA1(folder + fileName, sha1))
                {
                    ModBase.Log($"哈希校验失败", ModBase.LogLevel.Feedback);
                    return;
                }

                //HintService.Hint("下载完成！开始解压整合包", HintType.Success);
                //ZipFile.ExtractToDirectory(folder + fileName, extractPath, overwriteFiles:true);
                //File.Delete(folder + fileName);
                ModBase.RunInNewThread(() =>
                {
                    try
                    {
                        var loader = ModModpack.ModpackInstall(
                            file: folder+fileName,
                            instanceName: "ThousandStars",      // 可选，不提供则会弹窗让用户输入
                            logo: null,                     // 可选，实例图标路径
                            resourceId: null,               // 可选，CurseForge/Modrinth 项目 ID
                            isOnlineInstall: false,          // 是否在线安装（若 true 不自动跳转任务管理器）
                            isQianxing : true
                        );

                        if (loader != null)
                        {
                            loader.OnStateChanged = (s) =>
                            {
                                if (s.State == ModBase.LoadState.Finished || s.State == ModBase.LoadState.Failed || s.State == ModBase.LoadState.Aborted)
                                    //Directory.Delete(extractPath, true);
                                    File.Delete(folder + fileName);
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log("安装整合包失败!", ModBase.LogLevel.Feedback);
                    }
                });
            } 
            else if (s.State == ModBase.LoadState.Failed)
                ModBase.Log("下载失败!", ModBase.LogLevel.Feedback);
        };
    }

    private async Task UpdateUIAsync()
    {
        try
        {
            // 在后台线程执行网络请求
            var response = await HttpRequest.Create("https://serverupdate.wrh6.qzz.io/mod")
                .SendAsync(); // 假设 SendAsync 返回 Task<HttpResponse>

            string jsonString = response.AsString();
            var jsonNode = JsonNode.Parse(jsonString);
            if (jsonNode == null)
            {
                ModBase.Log("JSON 解析失败", ModBase.LogLevel.Msgbox);
                return;
            }

            string? version = jsonNode["version"]?.GetValue<string>();
            string? fabric = jsonNode["fabric"]?.GetValue<string>();
            var modsNode = jsonNode["mods"] as JsonArray;
            var namesNode = jsonNode["names"] as JsonArray;
            var urlsNode = jsonNode["urls"] as JsonArray;
            mods = modsNode?.Select(x => x.GetValue<string>()).ToArray() ?? Array.Empty<string>();
            names = namesNode?.Select(x => x.GetValue<string>()).ToArray() ?? Array.Empty<string>();
            urls = urlsNode?.Select(x => x.GetValue<string>()).ToArray() ?? Array.Empty<string>();

            // 空值处理
            version = string.IsNullOrEmpty(version) ? "1.0.0" : version;
            fabric = string.IsNullOrEmpty(fabric) ? "unknown" : fabric;

            // 回到 UI 线程更新控件
            Dispatcher.Invoke(() =>
            {
                TxtServerInfo.Text = $"服务器版本:{version}\nfabric推荐版本:{fabric}";
                TxtServerInfo.Visibility = Visibility.Visible;
                TxtWaitStatus.Visibility = Visibility.Collapsed;
            });
        }
        catch (Exception ex)
        {
            ModBase.Log($"更新 UI 失败: {ex.Message}", ModBase.LogLevel.Msgbox);
        }
    }

    /// <summary>
    /// 获取当前实例中所有已安装资源（Mod、资源包等）的非空 Comp.Id 列表（去重）。
    /// 如果只想获取 Mod 类型的资源，可传入参数限定。
    /// </summary>
    /// <summary>
    /// 获取当前选中实例的已安装模组 ID 列表（仅 Mod 类型，去重）
    /// </summary>
    private async Task<List<string>> GetInstalledModIdsAsync()
    {
        try
        {
            var instance = ModInstanceList.McMcInstanceSelected;
            if (instance == null)
                return new List<string>();

            // 确保实例信息已加载（填充 Info）
            if (!instance.IsLoaded)
                instance.Load();

            // 根据实例信息构建加载器列表
            var loaders = new List<ModComp.CompLoaderType>();
            if (instance.Info.HasForge) loaders.Add(ModComp.CompLoaderType.Forge);
            if (instance.Info.HasNeoForge) loaders.Add(ModComp.CompLoaderType.NeoForge);
            if (instance.Info.HasFabric || instance.Info.HasLegacyFabric) loaders.Add(ModComp.CompLoaderType.Fabric);
            if (instance.Info.HasQuilt) loaders.Add(ModComp.CompLoaderType.Quilt);
            if (instance.Info.HasLiteLoader) loaders.Add(ModComp.CompLoaderType.LiteLoader);

            var loader = ModLocalComp.compResourceListLoader;
            var data = new ModLocalComp.CompLocalLoaderData
            {
                gameVersion = instance,
                compPath = instance.PathIndie + @"mods\",
                compType = ModComp.CompType.Mod,
                loaders = loaders
            };

            ModLoader.LoaderFolderRun(loader, data.compPath, ModLoader.LoaderFolderRunType.ForceRun, loaderInput: data);

            while (loader.State == ModBase.LoadState.Loading)
                await Task.Delay(50);

            if (loader.State != ModBase.LoadState.Finished || loader.output == null)
                return new List<string>();

            return loader.output
                .Where(entry => !entry.IsFolder && entry.Comp != null && !string.IsNullOrEmpty(entry.Comp.Id))
                .Select(entry => entry.Comp.Id)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "获取已安装模组列表失败");
            return new List<string>();
        }
    }

    public static bool inUpdate = false;

    private async void BtnUpdateMod_Click(object sender, MouseButtonEventArgs e)
    {
        if (inUpdate)
        {
            HintService.Hint("正在同步中，请勿重复点击", HintType.Warning);
            return;
        }

        if ((mods == null || mods.Length == 0)||(urls == null || urls.Length == 0)||(names == null || names.Length == 0))
        {
            HintService.Hint("未获取到模组列表或模组列表为空!", HintType.Error);
            return;
        }

        inUpdate = true;
        HintService.Hint("正在准备同步mod...", HintType.Info);

        await Task.Run(async () =>
        {
            bool anyStarted = false;
            try
            {
                var modIds = await GetInstalledModIdsAsync();

                for (int i = 0; i < mods.Length; i++)
                {
                    string projectId = mods[i];
                    if (modIds.Contains(projectId))
                        continue;

                    anyStarted = true;
                    var ids = new List<string> { projectId };
                    bool isCurseForge = ModComp.CompRequest.IsFromCurseForge(projectId);
                    var file = ModFileHelper.GetLatestModFile(projectId, isCurseForge, "1.21.1", ModComp.CompLoaderType.Fabric);
                    var projects = await ModComp.CompRequest.GetCompProjectsByIdsAsync(ids);
                    var project = projects.FirstOrDefault();
                    var cachedFolder = new Dictionary<ModComp.CompType, string>();
                    DownloadModResourceAuto(file, project, cachedFolder, i == mods.Length - 1);
                }

                for (int i = 0;i < urls.Length; i++)
                {
                    anyStarted = true;
                    string url = urls[i];
                    var uri = new Uri(url);
                    DownloadModFromUrl(url,names[i],isLast : i==urls.Length-1);
                }

                // 如果没有任何任务启动，立即重置状态
                if (!anyStarted)
                {
                    inUpdate = false;
                    ModBase.RunInUi(() => HintService.Hint("所有mod同步完成", HintType.Success));
                }
                // 否则，由最后一个任务的 finally 重置
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "同步模组失败");
                ModBase.RunInUi(() => HintService.Hint("同步失败，请查看日志", HintType.Error));
            }
        });
    }

    #region 页面切换
    public FormMain.PageSubType pageID = FormMain.PageSubType.ServerUpdate;
    #endregion

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 标准化版本号（用于兼容性比较）
    /// </summary>
    private static string NormalizeVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return version;

        return version
            .Replace("∞", "infinite")
            .Replace("Combat Test 7c", "1.16_combat-3")
            .ToLowerInvariant();
    }

    /// <summary>
    /// 判断 Fabric API 是否兼容指定的原版版本
    /// </summary>
    private bool IsFabricApiCompatible(ModComp.CompFile api, string vanillaName)
    {
        if (api?.RawGameVersions == null || string.IsNullOrEmpty(vanillaName))
            return false;

        try
        {
            var normalized = NormalizeVersion(vanillaName);
            return api.RawGameVersions.Any(v =>
                string.Equals(v, normalized, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, $"判断 Fabric API 版本适配性出错: {api.DisplayName}, {vanillaName}",ModBase.LogLevel.Msgbox);
            return false;
        }
    }

    /// <summary>
    /// 异步等待加载器完成（支持超时）
    /// </summary>
    private async Task<bool> WaitForLoaderAsync(dynamic loader, int timeoutMs = 30000)
    {
        if (loader == null)
            return false;

        if (loader.State == ModBase.LoadState.Finished)
            return true;

        if (loader.State == ModBase.LoadState.Waiting)
            loader.Start();

        int elapsed = 0;
        while (loader.State == ModBase.LoadState.Loading && elapsed < timeoutMs)
        {
            await Task.Delay(100);
            elapsed += 100;
        }
        return loader.State == ModBase.LoadState.Finished;
    }

    // ==================== 核心安装逻辑 ====================
    /// <summary>
    /// 通过直链下载模组到当前或指定实例
    /// </summary>
    public static void DownloadModFromUrl(string url, string fileName, McInstance? targetInstance = null, bool isLast = false)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(fileName))
            return;

        // 自动选择实例
        if (targetInstance == null)
        {
            targetInstance = ModInstanceList.McMcInstanceSelected;
            if (targetInstance == null)
            {
                HintService.Hint("未选中任何实例，请先选择一个实例", HintType.Error);
                return;
            }
        }

        string targetFolder = Path.Combine(targetInstance.PathIndie, "mods");
        Directory.CreateDirectory(targetFolder);

        // 如果文件已存在，自动重命名（类似 DownloadModResourceAuto 的处理）
        string targetPath = Path.Combine(targetFolder, fileName);
        int counter = 1;
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);
        while (File.Exists(targetPath))
        {
            targetPath = Path.Combine(targetFolder, $"{baseName} ({counter}){ext}");
            counter++;
        }

        // 启动下载（复用 PCL 的下载功能）
        var loader = PageToolsTest.StartCustomDownload(url, Path.GetFileName(targetPath), targetFolder, txt : "模组下载");

        if (loader == null) return;

        // 等待完成
        var tcs = new TaskCompletionSource<bool>();

        loader.OnStateChanged = (s) =>
        {
            if (s.State == ModBase.LoadState.Finished || s.State == ModBase.LoadState.Failed || s.State == ModBase.LoadState.Aborted)
            {
                if (isLast)
                {
                    inUpdate = false;
                    ModBase.RunInUi(() => HintService.Hint("所有mod同步完成", HintType.Success));
                }
            }
        };
    }

    private async Task AutoInstallLatestFabricAsync()
    {
        // 1. 等待加载器完成
        bool fabricApiLoaded = await WaitForLoaderAsync(ModDownload.dlFabricApiLoader);
        bool clientListLoaded = await WaitForLoaderAsync(ModDownload.dlClientListLoader);

        if (!fabricApiLoaded || !clientListLoaded)
        {
            HintService.Hint("加载版本列表超时，请检查网络连接",HintType.Warning);
            return;
        }

        // 2. 获取原版版本列表（直接通过 Value 属性访问）
        JsonArray versionsArray = null;
        try
        {
            var output = ModDownload.dlClientListLoader?.output;
            if (output?.Value is JsonObject jsonObj)
            {
                versionsArray = jsonObj["versions"] as JsonArray;
            }
            else
            {
                // 如果 Value 不是 JsonObject（保底，实际上不会发生）
                HintService.Hint("版本列表数据格式异常",HintType.Error);
                return;
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "解析版本列表失败", ModBase.LogLevel.Msgbox);
            HintService.Hint("解析版本列表失败", HintType.Error, false);
            return;
        }

        if (versionsArray == null || versionsArray.Count == 0)
        {
            HintService.Hint("无法获取版本列表", HintType.Error);
            return;
        }

        // 3. 查找 1.21.1 的 JSON URL
        string targetVersion = "1.21.1";
        JsonNode versionJson = null;
        foreach (var item in versionsArray)
        {
            var obj = item as JsonObject;
            if (obj != null && obj["id"]?.GetValue<string>() == targetVersion)
            {
                versionJson = obj;
                break;
            }
        }
        var minecraftUrl = versionJson?["url"]?.GetValue<string>();

        if (string.IsNullOrEmpty(minecraftUrl))
        {
            HintService.Hint($"未找到原版 {targetVersion} 的下载信息");
            return;
        }

        // 4. 获取兼容的最新 Fabric API
        var compatibleApis = ModDownload.dlFabricApiLoader.output
            .Where(api => IsFabricApiCompatible(api, targetVersion))
            .OrderByDescending(api => api.ReleaseDate)
            .ToList();

        if (!compatibleApis.Any())
        {
            HintService.Hint("未找到兼容的 Fabric API", HintType.Error);
            return;
        }
        var latestFabricApi = compatibleApis.First();

        // 5. Fabric 加载器版本（暂时硬编码，后续可动态获取）
        string fabricLoaderVersion = "0.19.3"; // 可替换为动态获取

        // 6. 构造安装请求
        var instanceName = $"qianxing";
        var request = new ModDownloadLib.McInstallRequest
        {
            targetInstanceName = instanceName,
            targetInstanceFolder = $@"{ModFolder.mcFolderSelected}versions\{instanceName}\",
            minecraftJson = minecraftUrl,
            minecraftName = targetVersion,
            fabricVersion = fabricLoaderVersion,
            fabricApi = latestFabricApi,
            // 其他参数均为 null
            optiFineEntry = null,
            forgeEntry = null,
            neoForgeEntry = null,
            cleanroomEntry = null,
            optiFabric = null,
            liteLoaderEntry = null,
            labyModChannel = null,
            labyModCommitRef = null,
            legacyFabricVersion = null,
            legacyFabricApi = null
        };

        // 7. 后台线程开始安装
        ModBase.RunInNewThread(() =>
        {
            if (!ModDownloadLib.McInstall(request)) 
                Dispatcher.Invoke(() => HintService.Hint("安装失败，请查看日志",HintType.Error));
        });
    }

    public static class ModFileHelper
    {
        /// <summary>
        /// 根据模组项目 ID 获取指定 Minecraft 版本的最新模组文件
        /// </summary>
        /// <param name="projectId">模组项目 ID（CurseForge 或 Modrinth）</param>
        /// <param name="fromCurseForge">是否为 CurseForge 源（true=CurseForge，false=Modrinth）</param>
        /// <param name="targetVersion">目标 Minecraft 版本（如 "1.21.1"），必填</param>
        /// <param name="targetLoader">目标加载器（可选），如 Forge / Fabric / NeoForge 等</param>
        /// <returns>匹配的 ModComp.CompFile，若未找到则返回 null</returns>
        public static ModComp.CompFile GetLatestModFile(string projectId, bool fromCurseForge, string targetVersion, ModComp.CompLoaderType? targetLoader = null)
        {
            try
            {
                // 1. 获取该项目的所有文件
                var allFiles = ModComp.CompFilesGet(projectId, fromCurseForge);
                if (allFiles == null || allFiles.Count == 0)
                    return null;

                // 2. 筛选支持目标 Minecraft 版本的文件
                var filtered = allFiles
                    .Where(f => f.GameVersions != null && f.GameVersions.Contains(targetVersion))
                    .ToList();

                if (!filtered.Any())
                    return null;

                // 3. 如果指定了加载器，进一步筛选
                if (targetLoader.HasValue)
                {
                    filtered = filtered
                        .Where(f => f.ModLoaders != null && f.ModLoaders.Contains(targetLoader.Value))
                        .ToList();
                    if (!filtered.Any())
                        return null;
                }

                // 4. 按发布日期降序排列，取第一个（最新发布）
                var latest = filtered.OrderByDescending(f => f.ReleaseDate).FirstOrDefault();
                return latest;
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"获取模组文件失败 (projectId={projectId})");
                return null;
            }
        }
    }

    /// <summary>
    /// 自动下载模组资源（不弹窗，自动选择实例）
    /// </summary>
    /// <param name="file">要下载的资源文件信息</param>
    /// <param name="project">当前项目信息（用于依赖解析）</param>
    /// <param name="cachedFolder">缓存的上次下载文件夹路径（按类型缓存，可忽略）</param>
    public static void DownloadModResourceAuto(ModComp.CompFile file, ModComp.CompProject project, Dictionary<ModComp.CompType, string> cachedFolder, bool isLast)
    {
        ModBase.RunInNewThread(() =>
        {
            try
            {
                var desc = file.Type switch
                {
                    ModComp.CompType.ModPack => Lang.Text("Download.Comp.Type.Modpack"),
                    ModComp.CompType.Mod => Lang.Text("Download.Comp.Type.Mod"),
                    ModComp.CompType.ResourcePack => Lang.Text("Download.Comp.Type.ResourcePack"),
                    ModComp.CompType.Shader => Lang.Text("Download.Comp.Type.Shader"),
                    ModComp.CompType.DataPack => Lang.Text("Download.Comp.Type.DataPack"),
                    ModComp.CompType.World => Lang.Text("Download.Comp.Type.World"),
                    _ => ""
                };

                // 获取子文件夹
                string subFolder = file.Type switch
                {
                    ModComp.CompType.Mod => "mods\\",
                    ModComp.CompType.ResourcePack => "resourcepacks\\",
                    ModComp.CompType.Shader => "shaderpacks\\",
                    ModComp.CompType.World => "saves\\",
                    ModComp.CompType.DataPack => "", // 数据包在版本根目录
                    _ => ""
                };

                // 获取加载器要求
                var allowedLoaders = file.ModLoaders.Any() ? file.ModLoaders : project.ModLoaders;
                ModBase.Log($"[Comp] {desc}要求的加载器种类：{(allowedLoaders.Any() ? string.Join(" / ", allowedLoaders) : "无要求")}");

                // 判断实例是否兼容
                Func<McInstance, bool> isVersionSuitable = version =>
                {
                    if (version is null) return false;
                    if (!version.IsLoaded) version.Load();

                    // 版本检测（Mod 和数据包）
                    if (file.Type == ModComp.CompType.Mod || file.Type == ModComp.CompType.DataPack)
                        if (file.GameVersions.Any(v => v.Contains(".")) &&
                            !file.GameVersions.Any(v => v.Contains(".") && v == version.Info.VanillaName))
                            return false;

                    if (!allowedLoaders.Any()) return true;
                    if (allowedLoaders.Contains(ModComp.CompLoaderType.Forge) && version.Info.HasForge) return true;
                    if (allowedLoaders.Contains(ModComp.CompLoaderType.Fabric) &&
                        (version.Info.HasFabric || version.Info.HasLegacyFabric)) return true;
                    if (allowedLoaders.Contains(ModComp.CompLoaderType.NeoForge) && version.Info.HasNeoForge) return true;
                    if (allowedLoaders.Contains(ModComp.CompLoaderType.LiteLoader) && version.Info.HasLiteLoader) return true;
                    return false;
                };

                // 确定保存路径
                string targetFolder = null;

                // 优先当前选中的实例
                var selectedInstance = ModInstanceList.McMcInstanceSelected;
                if (selectedInstance != null && isVersionSuitable(selectedInstance))
                {
                    targetFolder = Path.Combine(selectedInstance.PathIndie, subFolder);
                    ModBase.Log($"[Comp] 使用当前实例：{targetFolder}");
                }
                else
                {
                    // 搜索所有实例
                    var needLoad = ModInstanceList.mcInstanceListLoader.State != ModBase.LoadState.Finished;
                    if (needLoad)
                    {
                        HintService.Hint(Lang.Text("Download.Comp.Detail.FindingApplicableInstance"));
                        ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                            ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\", true);
                    }

                    var suitableVersions = ModInstanceList.mcInstanceList.Values.SelectMany(l => l)
                        .Where(v => isVersionSuitable(v))
                        .Select(v => new DirectoryInfo(Path.Combine(v.PathIndie, subFolder)));

                    if (suitableVersions.Any())
                    {
                        var bestDir = suitableVersions
                            .OrderByDescending(dir => dir.Exists ? dir.LastWriteTimeUtc : DateTime.MinValue)
                            .ThenByDescending(dir => dir.Exists ? dir.GetFiles().Length : -1)
                            .First();
                        targetFolder = bestDir.FullName;
                        ModBase.Log($"[Comp] 使用合适的实例：{targetFolder}");
                    }
                    else
                    {
                        targetFolder = ModFolder.mcFolderSelected;
                        if (needLoad)
                            HintService.Hint(Lang.Text("Download.Comp.Detail.NoApplicableInstance"));
                        else
                            ModBase.Log("[Comp] 未找到兼容实例，使用默认 MC 文件夹");
                    }
                }

                // 确保目录存在
                Directory.CreateDirectory(targetFolder);

                // 生成文件名
                string fileName = ModComp.CompFileNameGet(project, file);
                string targetPath = Path.Combine(targetFolder, fileName);

                // 如果文件已存在跳过下载
                if (File.Exists(targetPath))
                {
                    ModBase.Log($"[Comp] 文件已存在，跳过下载：{targetPath}");
                    HintService.Hint($"文件已存在，跳过：{Path.GetFileName(targetPath)}", HintType.Info);
                    return;
                }

                // 记录缓存路径（可选）
                if (cachedFolder.ContainsKey(file.Type))
                    cachedFolder[file.Type] = targetFolder;
                else
                    cachedFolder.Add(file.Type, targetFolder);

                // 依赖处理（如果需要）
                if (file.Type == ModComp.CompType.Mod && Config.Download.Comp.AutoInstallDependencies && file.Dependencies.Any())
                {
                    // 找出目标实例（用于依赖安装）
                    McInstance targetInstance = null;
                    var knownInstances = new List<McInstance>();
                    if (ModInstanceList.McMcInstanceSelected != null)
                        knownInstances.Add(ModInstanceList.McMcInstanceSelected);
                    knownInstances.AddRange(ModInstanceList.mcInstanceList.Values.SelectMany(list => list)
                        .Where(instance => instance != null));

                    targetInstance = knownInstances
                        .Distinct()
                        .FirstOrDefault(instance =>
                            targetFolder.StartsWith(instance.PathIndie, StringComparison.OrdinalIgnoreCase));

                    if (targetInstance != null && !targetInstance.IsLoaded)
                        targetInstance.Load();

                    // 解析依赖（参考原逻辑）
                    var mcVersion = targetInstance?.Info?.VanillaName
                                     ?? file.GameVersions.FirstOrDefault(v => v.Contains("."))
                                     ?? string.Empty;

                    var targetLoaders = new List<ModComp.CompLoaderType>();
                    if (targetInstance != null)
                    {
                        if (targetInstance.Info.HasForge) targetLoaders.Add(ModComp.CompLoaderType.Forge);
                        if (targetInstance.Info.HasFabric || targetInstance.Info.HasLegacyFabric)
                            targetLoaders.Add(ModComp.CompLoaderType.Fabric);
                        if (targetInstance.Info.HasQuilt) targetLoaders.Add(ModComp.CompLoaderType.Quilt);
                        if (targetInstance.Info.HasNeoForge) targetLoaders.Add(ModComp.CompLoaderType.NeoForge);
                        if (targetInstance.Info.HasLiteLoader) targetLoaders.Add(ModComp.CompLoaderType.LiteLoader);
                    }
                    if (!targetLoaders.Any())
                        targetLoaders = allowedLoaders.ToList();

                    ModBase.Log($"[CompDeps] 开始解析前置: {file.Dependencies.Count} 个依赖");
                    var request = ModCompDependency.BuildRequest(file, project, mcVersion, targetLoaders, targetFolder);
                    var resolver = new ModDependencyResolver();
                    var result = resolver.Resolve(request);

                    void DownloadDependencies()
                    {
                        if (!result.ToInstall.Any())
                        {
                            ModBase.Log("[CompDeps] 所有前置均已安装，仅下载本体");
                            return;
                        }
                        ModBase.Log($"[CompDeps] 准备下载: {result.ToInstall.Count} 个前置");
                        var depDownloads = ModCompDependency.BuildDependencyDownloads(result, targetFolder);
                        foreach (var (depFilename, downloadFile) in depDownloads)
                        {
                            var depLoaderName = Lang.Text("Download.Comp.Detail.DownloadResource", desc,
                                ModBase.GetFileNameWithoutExtentionFromPath(depFilename));
                            var depLoaders = new List<ModLoader.LoaderBase>
                        {
                            new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadFile"),
                                new List<DownloadFile> { downloadFile })
                            {
                                ProgressWeight = 6,
                                block = true
                            }
                        };
                            var depLoader = new ModLoader.LoaderCombo<int>(depLoaderName, depLoaders);
                            depLoader.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
                            depLoader.Start(1);
                            ModLoader.LoaderTaskbarAdd(depLoader);
                        }
                    }

                    if (result.Unresolved.Any() || result.ToInstall.Any())
                    {
                        var installChoice = ModCompDependency.ConfirmDependencyInstall(result);
                        switch (installChoice)
                        {
                            case ModComp.CompDepsInstallTypes.Unresolved:
                                ModBase.Log("[CompDeps] 存在无法解析的前置，但仍尝试下载可用的");
                                DownloadDependencies();
                                break;
                            case ModComp.CompDepsInstallTypes.WithDeps:
                                DownloadDependencies();
                                break;
                            case ModComp.CompDepsInstallTypes.WithoutDeps:
                                ModBase.Log("[CompDeps] 用户选择仅下载本体，跳过依赖");
                                break;
                            case ModComp.CompDepsInstallTypes.Cancel:
                                ModBase.Log("[CompDeps] 用户取消下载");
                                return;
                        }
                    }
                    else
                    {
                        ModBase.Log("[CompDeps] 所有必需前置已安装");
                    }
                }

                // 构造并启动下载任务
                var loaderName = Lang.Text("Download.Comp.Detail.DownloadResource", desc,
                    ModBase.GetFileNameWithoutExtentionFromPath(targetPath));
                var loaders = new List<ModLoader.LoaderBase>
            {
                new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadFile"),
                    new List<DownloadFile> { file.ToNetFile(targetPath) })
                {
                    ProgressWeight = 6,
                    block = true
                }
            };

                var loader = new ModLoader.LoaderCombo<int>(loaderName, loaders);

                loader.OnStateChanged = (s) =>
                {
                    ModDownloadLib.LoaderStateChangedHintOnly(s);

                    if (isLast && (s.State == ModBase.LoadState.Finished || s.State == ModBase.LoadState.Failed || s.State == ModBase.LoadState.Aborted))
                    {
                        inUpdate = false;
                        ModBase.RunInUi(() => HintService.Hint("所有mod同步完成", HintType.Success));
                    }
                };
                loader.Start(1);
                ModLoader.LoaderTaskbarAdd(loader);

                // 通知 UI 刷新
                ModBase.RunInUi(() =>
                {
                    ModMain.frmMain.BtnExtraDownload.ShowRefresh();
                    ModMain.frmMain.BtnExtraDownload.Ribble();
                });
            }
            catch (Exception ex)
            {
                if (isLast)
                {
                    inUpdate = false;
                    ModBase.RunInUi(() => HintService.Hint("所有mod同步完成", HintType.Success));
                }

                ModBase.Log(ex, "保存资源文件失败", ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Download.Comp.Error.OperationFailed"));
            }
        }, "Download CompDetail AutoSave");
    }
}