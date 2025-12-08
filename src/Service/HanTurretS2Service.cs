using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using static HanTurretS2.HanTurretGlobals;
using static HanTurretS2.HanTurretS2Config;

namespace HanTurretS2;

public class HanTurretS2Service
{
    private readonly ILogger<HanTurretS2Service> _logger;
    private readonly ISwiftlyCore _core;
    private readonly IOptionsMonitor<HanTurretS2MainConfig> _mainconfig;
    private readonly IOptionsMonitor<HanTurretS2Config> _config;
    private readonly HanTurretGlobals _globals;
    private readonly HanTurretHelpers _helpers;
    private readonly HanTurretEffectService _effect;
    private readonly HanTurretAIService _aiservice;
    public HanTurretS2Service(ISwiftlyCore core, ILogger<HanTurretS2Service> logger,
        HanTurretGlobals globals, IOptionsMonitor<HanTurretS2Config> config,
        IOptionsMonitor<HanTurretS2MainConfig> mainconfig, HanTurretHelpers helpers,
        HanTurretEffectService Effect, HanTurretAIService aiservice)
    {
        _core = core;
        _logger = logger;
        _mainconfig = mainconfig;
        _config = config;
        _globals = globals;
        _helpers = helpers;
        _effect = Effect;
        _aiservice = aiservice;
    }

    public CPhysicsPropOverride CreateSentryPhysics(IPlayer player, string turretName)
    {
        if (player == null || !player.IsValid)
            return null;

        var Controller = player.Controller;
        if (Controller == null || !Controller.IsValid)
            return null;

        if (string.IsNullOrEmpty(turretName))
            return null;

        SwiftlyS2.Shared.Natives.Vector Pos = _helpers.GetForwardPosition(player, 120f);

        var TurretCfg = _config.CurrentValue;
        var turret = GetTurretConfigByName(turretName, _logger);

        if (turret == null)
            return null;

        var SteamId = player.SteamID;
        if (SteamId == 0)
            return null;

        if (!_globals.PlayerTurretCounts.TryGetValue(SteamId, out var turretDict))
        {
            turretDict = new Dictionary<string, HashSet<uint>>();
            _globals.PlayerTurretCounts[SteamId] = turretDict;
        }

        if (!turretDict.TryGetValue(turretName, out var turretSet))
        {
            turretSet = new HashSet<uint>();
            turretDict[turretName] = turretSet;
        }

        if (turret.Limit > 0 && turretSet.Count >= turret.Limit)
        {
            player.SendMessage(MessageType.Chat, $"{_core.Translation.GetPlayerLocalizer(player)["TurretLimit", turret.Limit]}");
            return null;
        }

        int Price = turret.Price;
        var Ims = Controller.InGameMoneyServices;
        if (Ims != null && Ims.IsValid && Price > 0)
        {
            int current = Ims.Account;
            if (current >= Price)
            {
                Ims.Account = current - Price;
                Controller.InGameMoneyServicesUpdated();
            }
            else
            {
                player.SendMessage(MessageType.Chat, $"{_core.Translation.GetPlayerLocalizer(player)["NoMoney"]}");
                return null;
            }
        }


        CPhysicsPropOverride Physics = _core.EntitySystem.CreateEntity<CPhysicsPropOverride>();
        if (Physics == null)
            return null;

        string phyModel = string.IsNullOrEmpty(_mainconfig.CurrentValue.TurretPhysboxModel)
        ? "models/stk_sentry_guns/sentry/sentry_physbox.vmdl"
        : _mainconfig.CurrentValue.TurretPhysboxModel;

        Physics.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        Physics.SetModel(phyModel);
        Physics.Collision.CollisionGroup = (byte)CollisionGroup.Dissolving;
        Physics.Collision.CollisionGroupUpdated();

        Physics.DispatchSpawn();

        Physics.Teleport(Pos, null, null);

        string propName = $"华仔炮塔_{Random.Shared.Next(1000000, 9999999)}";
        Physics!.Entity!.Name = propName;


        _globals.TurretData[Physics.Index] = new Turrets
        {
            Name = turret.Name,
            Model = turret.Model,
            Range = turret.Range,
            Rate = turret.Rate,
            Damage = turret.Damage,
            KnockBack = turret.KnockBack,
            FireAnim = turret.FireAnim,
            Team = turret.Team,
            Price = turret.Price,
            Limit = turret.Limit,
            Permissions = turret.Permissions,
            GlowColor = turret.GlowColor,
            laserColor = turret.laserColor,
            TurretFireSound = turret.TurretFireSound,
            MuzzleParticle = turret.MuzzleParticle,
            MuzzleAttachment = turret.MuzzleAttachment
        };

        var Base = CreateSentryBase(player, Physics, propName, turret.GlowColor, Pos);
        var Sentry = CreateSentry(player, Physics, propName, turret.Model,
            turret.MuzzleParticle, turret.Range, turret.Rate, turret.Damage, turret.KnockBack,
            turret.FireAnim, turret.TurretFireSound, turret.MuzzleAttachment, turret.laserColor,
            turret.GlowColor, Pos);

        turretSet.Add(Physics.Index);

        return Physics;
    }

    public CBaseModelEntity CreateSentryBase(IPlayer player, CPhysicsPropOverride Physics, string propName, string GlowColor, SwiftlyS2.Shared.Natives.Vector Pos)
    {
        var SentryBase = _core.EntitySystem.CreateEntityByDesignerName<CBaseModelEntity>("prop_dynamic_override");
        if (SentryBase == null)
            return null;

        SentryBase.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

        SentryBase.Teleport(Pos, null, null);
        SentryBase.DispatchSpawn();

        string BaseName = propName + "_Base";
        SentryBase!.Entity!.Name = BaseName;

        SentryBase!.AcceptInput("SetParent", "!activator", Physics, SentryBase);

        string baseModel = string.IsNullOrEmpty(_mainconfig.CurrentValue.TurretBaseModel)
        ? "models/stk_sentry_guns/sentry/base.vmdl"
        : _mainconfig.CurrentValue.TurretBaseModel;

        _core.Scheduler.NextTick(() =>
        {
            SentryBase.SetModel(baseModel);
            SentryBase.MaxHealth = 3000;
            SentryBase.Health = 3000;
            _effect.SetGlow(SentryBase, GlowColor);
        });

        return SentryBase;
    }

    public CBaseModelEntity CreateSentry(IPlayer player, CPhysicsPropOverride Physics, string propName, string Model,
        string Particle, float Range, float Rate, float Damage, float KnockBack, string FireAnim, string FireSound,
        string MuzzleAttachment, string laserColor, string GlowColor, SwiftlyS2.Shared.Natives.Vector Pos)
    {
        var Sentry = _core.EntitySystem.CreateEntityByDesignerName<CBaseModelEntity>("prop_dynamic_override");
        if (Sentry == null)
            return null;

        Sentry.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

        var SentryPos = new SwiftlyS2.Shared.Natives.Vector(Pos.X, Pos.Y, Pos.Z + 20);
        Sentry.Teleport(SentryPos, null, null);
        Sentry.DispatchSpawn();

        string BaseName = propName + "_Sentry";
        Sentry!.Entity!.Name = BaseName;

        Sentry.AcceptInput("SetParent", "!activator", Physics, Sentry);

        _core.Scheduler.NextTick(() =>
        {
            Sentry.SetModel(Model);
            Sentry.MaxHealth = 3000;
            Sentry.Health = 3000;
            _effect.SetGlow(Sentry, GlowColor);
            _aiservice.SentryThink(player, Sentry, Range, Rate, Damage, KnockBack, FireAnim, FireSound, MuzzleAttachment, laserColor);
            _effect.SetupMuzzle(Sentry, Particle, MuzzleAttachment);
        });

        return Sentry;
    }

    public void RemoveTurretFromCount(IPlayer player, CPhysicsPropOverride entIndex, string turretName)
    {
        var entRef = _core.EntitySystem.GetRefEHandle(entIndex);
        if (!entRef.IsValid)
            return;

        var RefIndex = entRef.EntityIndex;

        var SteamId = player.SteamID;
        if (SteamId == 0)
            return;

        if (_globals.PlayerTurretCounts.TryGetValue(SteamId, out var turretDict))
        {
            if (turretDict.TryGetValue(turretName, out var set))
            {
                set.Remove(RefIndex);

                if (set.Count == 0)
                    turretDict.Remove(turretName);
            }
        }
    }

    public Turret? GetTurretConfigByName(string name, ILogger? logger = null)
    {
        var selectedTurret = _config.CurrentValue.TurretList.FirstOrDefault(t =>
            t.Enable && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (selectedTurret == null)
        {
            _logger.LogWarning($"{_core.Localizer["TurretNameError", name]}"); 
        }
        return selectedTurret;
    }
}