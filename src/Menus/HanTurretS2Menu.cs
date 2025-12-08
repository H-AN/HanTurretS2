using System.Drawing;
using System.Reflection.Emit;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Cecil.Cil;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SteamAPI;

namespace HanTurretS2;

public class HanTurretMenu
{
    private readonly ILogger<HanTurretMenu> _logger;
    private readonly ISwiftlyCore _core;
    private readonly IOptionsMonitor<HanTurretS2Config> _config;
    private readonly HanTurretMenuHelper _menuhelper;
    private readonly HanTurretS2Service _service;
    public HanTurretMenu(ISwiftlyCore core, ILogger<HanTurretMenu> logger
        , IOptionsMonitor<HanTurretS2Config> config,
        HanTurretMenuHelper menuhelper, HanTurretS2Service service)
    {
        _core = core;
        _logger = logger;
        _config = config;
        _menuhelper = menuhelper;
        _service = service;
    }
    
    public IMenuAPI OpenTurretMenu(IPlayer player)
    {
        var main = _core.MenusAPI.CreateBuilder();
        IMenuAPI menu = _menuhelper.CreateMenu($"{_core.Translation.GetPlayerLocalizer(player)["MenuTitle"]}");

        // 顶部滚动文字
        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            $"{_core.Translation.GetPlayerLocalizer(player)["MenuSelelctTurret"]}",
            Color.Red, Color.LightBlue, Color.Red),
            updateIntervalMs: 500, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        var turretList = _config.CurrentValue.TurretList;
        if (turretList != null && turretList.Count > 0)
        {
            foreach (var turretCfg in turretList)
            {

                if (!turretCfg.Enable) 
                    continue;

                var pawn = player.PlayerPawn;
                if(pawn == null || !pawn.IsValid)
                    continue;


                string teamStr = string.IsNullOrEmpty(turretCfg.Team) ? "all" : turretCfg.Team.ToLower();

                int playerTeam = pawn.TeamNum;
                if (teamStr != "all")
                {
                    if (teamStr == "t" && playerTeam != 2) 
                        continue;
                    if (teamStr == "ct" && playerTeam != 3) 
                        continue;
                }

                var steamId = player.SteamID;
                if (steamId == 0)
                    continue;

                if (!string.IsNullOrEmpty(turretCfg.Permissions) && !_core.Permission.PlayerHasPermission(steamId, turretCfg.Permissions))
                    continue;

                string priceText;
                if (turretCfg.Price > 0)
                {
                    priceText = $"${turretCfg.Price}";
                }
                else
                {
                    priceText = _core.Translation.GetPlayerLocalizer(player)["FreeText"] ?? "免费";
                }

                string limitText;
                if (turretCfg.Limit > 0)
                {
                    limitText = $"{turretCfg.Limit}";
                }
                else
                {
                    limitText = _core.Translation.GetPlayerLocalizer(player)["LimitText"] ?? "∞";
                }

                string buttonText = $"{turretCfg.Name}[{priceText}丨{_core.Translation.GetPlayerLocalizer(player)["MenuLimitText"] ?? "限制"}: {limitText}]";

                var turretButton = new ButtonMenuOption(buttonText)
                {
                    TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
                    CloseAfterClick = false
                };
                turretButton.Tag = "extend";

                turretButton.Click += async (_, args) =>
                {
                    var clicker = args.Player;
                    _core.Scheduler.NextTick(() =>
                    {
                        if (!clicker.IsValid)
                            return;

                        _service.CreateSentryPhysics(clicker, turretCfg.Name);
                    });
                };

                menu.AddOption(turretButton);
            }
        }

        _core.MenusAPI.OpenMenuForPlayer(player, menu);
        return menu;
    }


}
