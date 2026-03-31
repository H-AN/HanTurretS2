using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanTurretS2;

public class HanTurretEvents
{
    private readonly ILogger<HanTurretEvents> _logger;
    private readonly ISwiftlyCore _core;
    private readonly IOptionsMonitor<HanTurretS2MainConfig> _mainconfig;
    private readonly IOptionsMonitor<HanTurretS2Config> _config;
    private readonly HanTurretGlobals _globals;
    private readonly HanTurretAIService _aiservice;
    private readonly HanTurretHelpers _helpers;
    private readonly HanTurretEffectService _effect;

    public HanTurretEvents(ISwiftlyCore core, ILogger<HanTurretEvents> logger,
        IOptionsMonitor<HanTurretS2Config> config, HanTurretGlobals globals,
        IOptionsMonitor<HanTurretS2MainConfig> mainconfig, HanTurretAIService aiservice,
        HanTurretHelpers helpers, HanTurretEffectService Effect)
    {
        _core = core;
        _logger = logger;
        _config = config;
        _globals = globals;
        _mainconfig = mainconfig;
        _aiservice = aiservice;
        _helpers = helpers;
        _effect = Effect;
    }

    public void HookEvents()
    {
        _core.Event.OnPrecacheResource += Event_OnPrecacheResource;

        _core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
        _core.GameEvent.HookPre<EventRoundEnd>(OnRoundEnd);
        _core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeath);

        _core.Event.OnMapUnload += Event_OnMapUnload;
        _core.Event.OnClientConnected += Event_OnClientConnected;
        _core.Event.OnClientDisconnected += Event_OnClientDisconnected;
        _core.Event.OnEntityTakeDamage += Event_OnEntityTakeDamage;
    }

    private void Event_OnEntityTakeDamage(SwiftlyS2.Shared.Events.IOnEntityTakeDamageEvent @event)
    {
        if (@event.Info.DamageType != DamageTypes_t.DMG_SLASH)
            return;

        var victim = @event.Entity;
        if (victim == null || !victim.IsValid)
            return;

        var victimEntity = victim.Entity;
        if (victimEntity == null || !victimEntity.IsValid)
            return;

        if (string.IsNullOrEmpty(victimEntity.Name) || !victimEntity.Name.StartsWith("华仔炮塔_"))
            return;

        var attacker = @event.Info.Attacker.Value;
        if (attacker == null || !attacker.IsValid)
            return;

        var attackerPawn = attacker.As<CCSPlayerPawn>();
        if (attackerPawn == null || !attackerPawn.IsValid)
            return;

        var attackerPlayer = _core.PlayerManager.GetPlayerFromPawn(attackerPawn);
        if (attackerPlayer == null || !attackerPlayer.IsValid || attackerPlayer.IsFakeClient)
            return;

        var phy = victim.As<CPhysicsPropOverride>();
        if (phy == null || !phy.IsValid || !phy.IsValidEntity)
            return;

        uint hitRaw = _core.EntitySystem.GetRefEHandle(phy).Raw;
        uint finalPhyRaw = hitRaw;

        if (_globals.TurretHeadToPhysics.TryGetValue(hitRaw, out uint mainFromHead))
            finalPhyRaw = mainFromHead;
        else if (_globals.TurretBaseToPhysics.TryGetValue(hitRaw, out uint mainFromBase))
            finalPhyRaw = mainFromBase;

        var phyHandle = new CHandle<CPhysicsPropOverride>(finalPhyRaw);
        var phyEntity = phyHandle.Value;
        if (phyEntity == null || !phyEntity.IsValid || !phyEntity.IsValidEntity)
            return;

        if (!_globals.TurretData.TryGetValue(finalPhyRaw, out var turretData))
            return;

        bool isFriendly = false;
        if (_globals.TurretToPlayer.TryGetValue(finalPhyRaw, out var ownerSessionId))
        {
            var owner = _core.PlayerManager.GetPlayerFromSessionId(ownerSessionId);
            var ownerPawn = owner?.PlayerPawn;
            if (owner != null && owner.IsValid && ownerPawn != null && ownerPawn.IsValid)
            {
                isFriendly = ownerPawn.TeamNum == attackerPawn.TeamNum;
            }
        }
        else
        {
            string teamStr = string.IsNullOrEmpty(turretData.Team) ? "all" : turretData.Team.ToLowerInvariant();
            if (teamStr == "ct")
                isFriendly = attackerPawn.TeamNum == 3;
            else if (teamStr == "t")
                isFriendly = attackerPawn.TeamNum == 2;
        }

        int amount = Math.Max(1, (int)@event.Info.Damage);

        if (!isFriendly && turretData.Canbreakage)
        {
            phyEntity.Health -= amount;
            phyEntity.HealthUpdated();
            _helpers.EmitSoundFromPhyEntity(phyHandle, "Breakable.MatMetal");

            if (phyEntity.Health <= 0)
            {
                _aiservice.KillTurret(phyHandle);
                return;
            }
        }
        else if (isFriendly && turretData.CanFixes)
        {
            if (phyEntity.Health < phyEntity.MaxHealth)
            {
                phyEntity.Health += amount;
                if (phyEntity.Health > phyEntity.MaxHealth)
                    phyEntity.Health = phyEntity.MaxHealth;

                phyEntity.HealthUpdated();
                _helpers.EmitSoundFromPhyEntity(phyHandle, "SolidMetal.BulletImpact");
            }
        }

        ShowTurretInfo(attackerPlayer, phyHandle);
        _effect.CreateParticleAtPos(phyHandle, "particles/explosions_fx/explosion_c4_interior_sparktrails.vpcf");
    }

    private void Event_OnClientDisconnected(SwiftlyS2.Shared.Events.IOnClientDisconnectedEvent @event)
    {
        ulong sessionId = 0;
        _globals.PlayerSessionCache.TryGetValue(@event.PlayerId, out sessionId);
        _aiservice.RemoveAllPlayerTurrets(@event.PlayerId, sessionId);
    }

    private void Event_OnClientConnected(SwiftlyS2.Shared.Events.IOnClientConnectedEvent @event)
    {
        var player = _core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player == null || !player.IsValid)
            return;

        if (player.SessionId != 0)
        {
            _globals.PlayerSessionCache[player.PlayerID] = player.SessionId;
        }
    }

    private void Event_OnPrecacheResource(SwiftlyS2.Shared.Events.IOnPrecacheResourceEvent @event)
    {
        @event.AddItem("models/stk_sentry_guns/sentry/sentry_physbox.vmdl");
        @event.AddItem("models/stk_sentry_guns/sentry/base.vmdl");
        @event.AddItem("soundevents/game_sounds_physics.vsndevts");
        @event.AddItem("soundevents/game_sounds_weapons.vsndevts");
        @event.AddItem("particles/explosions_fx/explosion_c4_short.vpcf");
        @event.AddItem("particles/explosions_fx/explosion_c4_interior_sparktrails.vpcf");

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
            foreach (var turret in turretList)
            {
                if (!string.IsNullOrEmpty(turret.Model))
                {
                    @event.AddItem(turret.Model);
                }
                if (!string.IsNullOrEmpty(turret.PrecacheSoundEvent))
                {
                    @event.AddItem(turret.PrecacheSoundEvent);
                }
                if (!string.IsNullOrEmpty(turret.MuzzleParticle))
                {
                    @event.AddItem(turret.MuzzleParticle);
                }
            }
        }
    }

    private void Event_OnMapUnload(SwiftlyS2.Shared.Events.IOnMapUnloadEvent @event)
    {
        _globals.TurretCanFire = false;
        ClearRuntimeState();
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        _globals.TurretCanFire = true;
        ClearRuntimeState();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _globals.TurretCanFire = false;
        ClearRuntimeState();
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        _aiservice.RemoveAllPlayerTurrets(player.PlayerID, player.SessionId);
        return HookResult.Continue;
    }

    public void ShowTurretInfo(IPlayer player, CHandle<CPhysicsPropOverride> sentryHandle)
    {
        if (!_mainconfig.CurrentValue.ShowTurretInfo)
            return;

        if (!sentryHandle.IsValid)
            return;

        if (player == null || !player.IsValid || player.IsFakeClient || !player.IsAlive)
            return;

        var sentry = sentryHandle.Value;
        if (sentry == null || !sentry.IsValid || !sentry.IsValidEntity)
            return;

        if (!_globals.TurretToPlayer.TryGetValue(sentryHandle.Raw, out var ownerSessionId))
            return;

        var owner = _core.PlayerManager.GetPlayerFromSessionId(ownerSessionId);
        if (owner == null || !owner.IsValid || owner.IsFakeClient)
            return;

        if (!_globals.TurretData.TryGetValue(sentryHandle.Raw, out var turretData) || turretData == null)
            return;

        string breakagemessage = turretData.Canbreakage
            ? $"{_core.Translation.GetPlayerLocalizer(player)["TurretHudCanBreakage"]}"
            : $"{_core.Translation.GetPlayerLocalizer(player)["TurretHudCantBreakage"]}";
        string canfixmessage = turretData.CanFixes
            ? $"{_core.Translation.GetPlayerLocalizer(player)["TurretHudCanFix"]}"
            : $"{_core.Translation.GetPlayerLocalizer(player)["TurretHudCantFix"]}";
        string message =
            $"<span><font color='red'> {_core.Translation.GetPlayerLocalizer(player)["TurretHudOwnerPlayer", owner.Name, turretData.Name]}</font></span><br>" +
            $"<span><font color='orange'>{_core.Translation.GetPlayerLocalizer(player)["TurretHudLeftHealth"]} </font><font color='red'>{sentry.Health}</font></span><br>" +
            $"<span><font color='orange'>{_core.Translation.GetPlayerLocalizer(player)["TurretHudMaxHealth"]} </font><font color='red'>{sentry.MaxHealth}</font></span><br>" +
            $"<span><font color='green'>{breakagemessage} </font></span><br>" +
            $"<span><font color='green'>{canfixmessage} </font></span><br>";

        _ = player.SendCenterHTMLAsync(message);
    }

    private void ClearRuntimeState()
    {
        foreach (var task in _globals.SentryThink.Values.ToList())
        {
            task?.Cancel();
        }

        _globals.SentryThink.Clear();
        _globals.sentryParticles.Clear();
        _globals.TurretData.Clear();
        _globals.PlayerTurretCounts.Clear();
        _globals.PlayerSessionCache.Clear();
        _globals.TurretToPlayer.Clear();
        _globals.TurretOwner.Clear();
        _globals.TurretPartsMap.Clear();
        _globals.TurretHeadToPhysics.Clear();
        _globals.TurretBaseToPhysics.Clear();
    }
}
