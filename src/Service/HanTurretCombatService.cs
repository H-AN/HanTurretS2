using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanTurretS2;

public class HanTurretCombatService
{
    private readonly ILogger<HanTurretCombatService> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HanTurretHelpers _helpers;
    public HanTurretCombatService(ISwiftlyCore core, ILogger<HanTurretCombatService> logger,
            HanTurretHelpers helpers)
    {
        _core = core;
        _logger = logger;
        _helpers = helpers;
    }

    public void ApplyDamage(IPlayer attacker, IPlayer target, CHandle<CBaseModelEntity> sentryHandle, float damageAmount, DamageTypes_t damageType = DamageTypes_t.DMG_BULLET)
    {
        if (!sentryHandle.IsValid)
            return;

        var sentry = sentryHandle.Value;
        if (sentry == null || !sentry.IsValid)
            return;

        var AttackerPawn = attacker.PlayerPawn;
        if (AttackerPawn == null || !AttackerPawn.IsValid)
            return;

        var TargetPawn = target.PlayerPawn;
        if (TargetPawn == null || !TargetPawn.IsValid)
            return;

        CBaseEntity inflictorEntity = sentry;
        CBaseEntity attackerEntity = AttackerPawn;
        CBaseEntity abilityEntity = sentry;


        var damageInfo = new CTakeDamageInfo(inflictorEntity, attackerEntity, abilityEntity, damageAmount, damageType);

        damageInfo.DamageForce = new SwiftlyS2.Shared.Natives.Vector(0, 0, 10f);

        var targetPos = TargetPawn.AbsOrigin;
        if (targetPos != null)
        {
            damageInfo.DamagePosition = targetPos.Value;
        }
        target.TakeDamage(damageInfo);
    }

    public void ApplyKnockBack(CHandle<CBaseModelEntity> sentryHandle, IPlayer target, float force)
    {
        if (!sentryHandle.IsValid)
            return;

        var sentry = sentryHandle.Value;
        if (sentry == null || !sentry.IsValid || !sentry.IsValidEntity)
            return;

        if (target == null || !target.IsValid || force <= 0)
            return;

        var targetPawn = target.PlayerPawn;
        if (targetPawn == null || !targetPawn.IsValid)
            return;

        var sentryRotation = sentry.AbsRotation;
        if (sentryRotation == null)
            return;

        QAngle sentryAngle = sentryRotation.Value;
        sentryAngle.ToDirectionVectors(out Vector vecKnockback, out _, out _);
        var pushVelocity = vecKnockback * force;
        var vel = targetPawn.AbsVelocity;
        targetPawn.Teleport(null, null, vel + pushVelocity);
    }



}
