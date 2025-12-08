using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanTurretS2;

public class HanTurretAIService
{
    private readonly ILogger<HanTurretAIService> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HanTurretGlobals _globals;
    private readonly HanTurretHelpers _helpers;
    private readonly HanTurretEffectService _effect;
    private readonly HanTurretCombatService _combat;
    public HanTurretAIService(ISwiftlyCore core, ILogger<HanTurretAIService> logger,
        HanTurretGlobals globals,HanTurretHelpers helpers,
        HanTurretEffectService Effect, HanTurretCombatService combat)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
        _effect = Effect;
        _combat = combat;
    }

    public void SentryThink(IPlayer Owner, CBaseModelEntity Sentry, float Range, float Rate, float Damage, float KnockBack, string FireAnim, string FireSound, string MuzzleAttachment, string laserColor)
    {
        var entRef = _core.EntitySystem.GetRefEHandle(Sentry);
        if (!entRef.IsValid)
            return;

        var entIndex = entRef.EntityIndex;
        if (_globals.SentryThink.TryGetValue(entIndex, out var task))
        {
            task?.Cancel();
            task = null;
            _globals.SentryThink.Remove(entIndex);
        }

        _globals.SentryThink[entIndex] = _core.Scheduler.RepeatBySeconds(Rate, () =>
        {
            if (!entRef.IsValid || !_globals.TurretCanFire)
            {
                if (_globals.SentryThink.TryGetValue(entIndex, out var task))
                {
                    task?.Cancel();
                    task = null;
                    _globals.SentryThink.Remove(entIndex);
                }
            }

            var allPlayers = _core.PlayerManager.GetAllPlayers();
            float range = Range;
            float fireRange = Range - 200f;
            IPlayer closestPlayer = null;
            float closestDist = float.MaxValue;

            foreach (var player in allPlayers)
            {
                if (player == null || !player.IsValid)
                    continue;

                var pawn = player.PlayerPawn;
                if (pawn == null || !pawn.IsValid)
                    continue;

                var controller = player.Controller;
                if (controller == null || !controller.IsValid)
                    continue;

                if (!controller.PawnIsAlive)
                    continue;

                var OwnerPawn = Owner.PlayerPawn;
                if (OwnerPawn == null || !OwnerPawn.IsValid)
                    continue;

                if (pawn.TeamNum == OwnerPawn.TeamNum)
                    continue;

                var RefValue = entRef.Value;
                if (RefValue == null || !RefValue.IsValid)
                    continue;

                var pOrigin = pawn.AbsOrigin;
                var sOrigin = RefValue.AbsOrigin;
                if (pOrigin == null || sOrigin == null)
                    continue;



                var dir = new SwiftlyS2.Shared.Natives.Vector(
                    pOrigin.Value.X - sOrigin.Value.X,
                    pOrigin.Value.Y - sOrigin.Value.Y,
                    pOrigin.Value.Z - sOrigin.Value.Z
                );

                float dist = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
                if (dist > range) continue;

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPlayer = player;
                }
            }

            if (closestPlayer != null)
            {
                var closestPlayerPawn = closestPlayer.PlayerPawn;
                if (closestPlayerPawn == null || !closestPlayerPawn.IsValid)
                    return;

                var RefValue = entRef.Value;
                if (RefValue == null || !RefValue.IsValid)
                    return;

                var cOrigin = closestPlayerPawn.AbsOrigin;
                var sOrigin = RefValue.AbsOrigin;
                if (cOrigin == null || sOrigin == null)
                    return;

                var dir = new SwiftlyS2.Shared.Natives.Vector(
                    cOrigin.Value.X - sOrigin.Value.X,
                    cOrigin.Value.Y - sOrigin.Value.Y,
                    cOrigin.Value.Z - sOrigin.Value.Z
                );
                float dist = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);

                if (dist > 0f)
                {
                    var normalizedDir = new SwiftlyS2.Shared.Natives.Vector(
                        dir.X / dist,
                        dir.Y / dist,
                        dir.Z / dist
                    );

                    float yaw = MathF.Atan2(normalizedDir.Y, normalizedDir.X) * (180f / MathF.PI);
                    float pitch = -MathF.Asin(normalizedDir.Z) * (180f / MathF.PI);


                    if (closestDist <= fireRange)
                    {
                        if (_helpers.GetAimPosition(RefValue, closestPlayer))
                        {
                            Sentry.Teleport(null, new SwiftlyS2.Shared.Natives.QAngle(pitch, yaw, 0), null);
                            Sentry.AcceptInput("SetAnimation", $"{FireAnim}");
                            _helpers.EmitSoundFromEntity(Sentry, $"{FireSound}");
                            _effect.ToggleMuzzle(Sentry, 0.3f);
                            _effect.CreateTracer(Sentry, closestPlayer, laserColor);
                            _combat.ApplyDamage(Owner, closestPlayer, Sentry, Damage, DamageTypes_t.DMG_BULLET);
                            _combat.ApplyKnockBack(Sentry, closestPlayer, KnockBack);
                        }
                    }

                }
            }
        });

        _core.Scheduler.StopOnMapChange(_globals.SentryThink[entRef.EntityIndex]);
    }
}