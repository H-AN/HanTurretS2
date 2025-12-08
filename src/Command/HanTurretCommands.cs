

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace HanTurretS2;

public class HanTurretCommands
{
    private readonly ILogger<HanTurretCommands> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HanTurretMenu _menus;
    private readonly IOptionsMonitor<HanTurretS2MainConfig> _mainconfig;

    public HanTurretCommands(ISwiftlyCore core, ILogger<HanTurretCommands> logger,
       HanTurretMenu menus, IOptionsMonitor<HanTurretS2MainConfig> mainconfig)
    {
        _core = core;
        _logger = logger;
        _menus = menus;
        _mainconfig = mainconfig;
    }

    public void Commands()
    {
        string MenuCommand = string.IsNullOrEmpty(_mainconfig.CurrentValue.MenuCommand)? "sw_turret": _mainconfig.CurrentValue.MenuCommand;
        _core.Command.RegisterCommand($"{MenuCommand}", OpenTurretMenu, true);
    }

    public void OpenTurretMenu(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) 
            return;

        var Controller = player.Controller;
        if (Controller == null || !Controller.IsValid) 
            return;

        if (!Controller.PawnIsAlive)
        {
            player.SendMessage(MessageType.Chat, $"{_core.Translation.GetPlayerLocalizer(player)["MustBeAlive"]}");
            return;
        }
            

        _menus.OpenTurretMenu(player);

    }

}