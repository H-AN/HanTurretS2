

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Cecil.Cil;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace HanTurretS2;

public class HanTurretEvents
{
    private readonly ILogger<HanTurretEvents> _logger;
    private readonly ISwiftlyCore _core;
    private readonly IOptionsMonitor<HanTurretS2MainConfig> _mainconfig;
    private readonly IOptionsMonitor<HanTurretS2Config> _config;
    private readonly HanTurretGlobals _globals;

    public HanTurretEvents(ISwiftlyCore core, ILogger<HanTurretEvents> logger,
        IOptionsMonitor<HanTurretS2Config> config, HanTurretGlobals globals,
        IOptionsMonitor<HanTurretS2MainConfig> mainconfig)
    {
        _core = core;
        _logger = logger;
        _config = config;
        _globals = globals;
        _mainconfig = mainconfig;
    }

    public void HookEvents()
    {
        _core.Event.OnPrecacheResource += Event_OnPrecacheResource;

        _core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _core.GameEvent.HookPre<EventRoundEnd>(OnRoundEnd);

        _core.Event.OnMapUnload += Event_OnMapUnload;
    }

    

    private void Event_OnPrecacheResource(SwiftlyS2.Shared.Events.IOnPrecacheResourceEvent @event)
    {
        @event.AddItem("models/stk_sentry_guns/sentry/sentry_physbox.vmdl");
        @event.AddItem("models/stk_sentry_guns/sentry/base.vmdl");

        var maincfg = _mainconfig.CurrentValue;
        if (!string.IsNullOrEmpty(maincfg.TurretBaseModel))
        {
            @event.AddItem(maincfg.TurretBaseModel);
        }
        if (!string.IsNullOrEmpty(maincfg.TurretPhysboxModel))
        {
            @event.AddItem(maincfg.TurretPhysboxModel);
        }

        var turretList = _config.CurrentValue.TurretList;
        if (turretList != null && turretList.Count > 0)
        {
            foreach (var bturretox in turretList)
            {
                if (!string.IsNullOrEmpty(bturretox.Model))
                {
                    @event.AddItem(bturretox.Model);
                }
                if (!string.IsNullOrEmpty(bturretox.PrecacheSoundEvent))
                {
                    @event.AddItem(bturretox.PrecacheSoundEvent);
                }
                if (!string.IsNullOrEmpty(bturretox.MuzzleParticle))
                {
                    @event.AddItem(bturretox.MuzzleParticle);
                }
            }
        }

    }

    private void Event_OnMapUnload(SwiftlyS2.Shared.Events.IOnMapUnloadEvent @event)
    {
        _globals.TurretCanFire = false;
        _globals.sentryParticles.Clear();
        _globals.TurretData.Clear();
        _globals.PlayerTurretCounts.Clear();
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        _globals.TurretCanFire = true;
        _globals.sentryParticles.Clear();
        _globals.TurretData.Clear();
        _globals.PlayerTurretCounts.Clear();

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _globals.TurretCanFire = false;
        _globals.sentryParticles.Clear();
        _globals.TurretData.Clear();
        _globals.PlayerTurretCounts.Clear();

        return HookResult.Continue;
    }

}