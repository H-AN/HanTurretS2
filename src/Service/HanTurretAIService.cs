using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
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
        HanTurretGlobals globals, HanTurretHelpers helpers,
        HanTurretEffectService Effect, HanTurretCombatService combat)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
        _effect = Effect;
        _combat = combat;
    }

    public void SentryThink(ulong ownerSessionId, CHandle<CBaseModelEntity> SentryHandle, float Range, float Rate, float Damage, float KnockBack, string FireAnim, string FireSound, string MuzzleAttachment, string laserColor)
    {
        if (!SentryHandle.IsValid)
            return;

        var sentry = SentryHandle.Value;
        if (sentry == null || !sentry.IsValid)
            return;

        CancelThink(SentryHandle.Raw);

        var cts = _core.Scheduler.RepeatBySeconds(Rate, () =>
        {
            if (!SentryHandle.IsValid || sentry == null || !sentry.IsValid || !_globals.TurretCanFire)
            {
                CancelThink(SentryHandle.Raw);
                return;
            }

            var owner = _core.PlayerManager.GetPlayerFromSessionId(ownerSessionId);
            if (owner == null || !owner.IsValid || owner.IsFakeClient)
            {
                if (_globals.TurretHeadToPhysics.TryGetValue(SentryHandle.Raw, out var phyRaw))
                {
                    KillTurret(new CHandle<CPhysicsPropOverride>(phyRaw));
                }
                else
                {
                    CancelThink(SentryHandle.Raw);
                }

                return;
            }

            var ownerPawn = owner.PlayerPawn;
            if (ownerPawn == null || !ownerPawn.IsValid)
                return;

            var allPlayers = _core.PlayerManager.GetAllPlayers();
            float range = Range;
            float fireRange = Range - 200f;
            IPlayer? closestPlayer = null;
            float closestDist = float.MaxValue;

            foreach (var player in allPlayers)
            {
                if (player == null || !player.IsValid)
                    continue;

                if (player.PlayerID == owner.PlayerID)
                    continue;

                var pawn = player.PlayerPawn;
                if (pawn == null || !pawn.IsValid)
                    continue;

                var controller = player.Controller;
                if (controller == null || !controller.IsValid || !controller.PawnIsAlive)
                    continue;

                if (pawn.TeamNum == ownerPawn.TeamNum)
                    continue;

                var pOrigin = pawn.AbsOrigin;
                var sOrigin = sentry.AbsOrigin;
                if (pOrigin == null || sOrigin == null)
                    continue;

                var dir = new SwiftlyS2.Shared.Natives.Vector(
                    pOrigin.Value.X - sOrigin.Value.X,
                    pOrigin.Value.Y - sOrigin.Value.Y,
                    pOrigin.Value.Z - sOrigin.Value.Z
                );

                float dist = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
                if (dist > range)
                    continue;

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPlayer = player;
                }
            }

            if (closestPlayer == null)
                return;

            var closestPlayerPawn = closestPlayer.PlayerPawn;
            if (closestPlayerPawn == null || !closestPlayerPawn.IsValid)
                return;

            var cOrigin = closestPlayerPawn.AbsOrigin;
            var sentryOrigin = sentry.AbsOrigin;
            if (cOrigin == null || sentryOrigin == null)
                return;

            var direction = new SwiftlyS2.Shared.Natives.Vector(
                cOrigin.Value.X - sentryOrigin.Value.X,
                cOrigin.Value.Y - sentryOrigin.Value.Y,
                cOrigin.Value.Z - sentryOrigin.Value.Z
            );
            float distToTarget = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z);
            if (distToTarget <= 0f || closestDist > fireRange)
                return;

            var normalizedDir = new SwiftlyS2.Shared.Natives.Vector(
                direction.X / distToTarget,
                direction.Y / distToTarget,
                direction.Z / distToTarget
            );

            float yaw = MathF.Atan2(normalizedDir.Y, normalizedDir.X) * (180f / MathF.PI);
            float pitch = -MathF.Asin(normalizedDir.Z) * (180f / MathF.PI);

            if (_helpers.GetAimPosition(SentryHandle, closestPlayer))
            {
                sentry.Teleport(null, new SwiftlyS2.Shared.Natives.QAngle(pitch, yaw, 0), null);
                sentry.AcceptInput("SetAnimation", $"{FireAnim}");
                _helpers.EmitSoundFromEntity(SentryHandle, $"{FireSound}");
                _effect.ToggleMuzzle(SentryHandle, 0.3f);
                _effect.CreateTracer(SentryHandle, closestPlayer, laserColor);
                _combat.ApplyDamage(owner, closestPlayer, SentryHandle, Damage, DamageTypes_t.DMG_BULLET);
                _combat.ApplyKnockBack(SentryHandle, closestPlayer, KnockBack);
            }
        });

        _globals.SentryThink[SentryHandle.Raw] = cts;
        _core.Scheduler.StopOnMapChange(cts);
    }

    public void RemoveAllPlayerTurrets(int playerID, ulong ownerSessionId)
    {
        if (ownerSessionId == 0)
        {
            _globals.PlayerSessionCache.TryGetValue(playerID, out ownerSessionId);
        }

        if (ownerSessionId != 0 && _globals.TurretOwner.TryGetValue(ownerSessionId, out var set))
        {
            foreach (var phyRaw in set.ToList())
            {
                var phyHandle = new CHandle<CPhysicsPropOverride>(phyRaw);
                KillTurret(phyHandle);
            }

            set.Clear();
            _globals.TurretOwner.Remove(ownerSessionId);
        }

        if (ownerSessionId != 0)
        {
            _globals.PlayerTurretCounts.Remove(ownerSessionId);
        }

        _globals.PlayerSessionCache.Remove(playerID);
    }

    public void KillTurret(CHandle<CPhysicsPropOverride> phyHandle)
    {
        if (!phyHandle.IsValid)
            return;

        uint phyRaw = phyHandle.Raw;
        UnlinkTurret(phyRaw);
        _effect.CreateExplosionAtPos(phyHandle);

        if (!_globals.TurretPartsMap.TryGetValue(phyRaw, out var parts))
        {
            _core.Scheduler.NextTick(() =>
            {
                if (phyHandle.IsValid)
                    phyHandle.Value?.AcceptInput("Kill", 0);
            });

            _globals.TurretData.Remove(phyRaw);
            return;
        }

        var headHandle = new CHandle<CBaseModelEntity>(parts.head);
        var baseHandle = new CHandle<CBaseModelEntity>(parts.baseEnt);

        CancelThink(parts.head);

        _core.Scheduler.NextTick(() =>
        {
            if (headHandle.IsValid)
                headHandle.Value?.AcceptInput("Kill", 0);

            if (baseHandle.IsValid)
                baseHandle.Value?.AcceptInput("Kill", 0);

            if (phyHandle.IsValid)
                phyHandle.Value?.AcceptInput("Kill", 0);
        });

        _globals.sentryParticles.Remove(parts.head);
        _globals.TurretHeadToPhysics.Remove(parts.head);
        _globals.TurretBaseToPhysics.Remove(parts.baseEnt);
        _globals.TurretPartsMap.Remove(phyRaw);
        _globals.TurretData.Remove(phyRaw);
    }

    public void UnlinkTurret(uint entityId)
    {
        if (!_globals.TurretToPlayer.TryGetValue(entityId, out var ownerSessionId))
            return;

        if (_globals.TurretOwner.TryGetValue(ownerSessionId, out var set))
        {
            set.Remove(entityId);
            if (set.Count == 0)
                _globals.TurretOwner.Remove(ownerSessionId);
        }

        if (_globals.PlayerTurretCounts.TryGetValue(ownerSessionId, out var turretDict))
        {
            foreach (var turretName in turretDict.Keys.ToList())
            {
                var turretSet = turretDict[turretName];
                turretSet.Remove(entityId);
                if (turretSet.Count == 0)
                    turretDict.Remove(turretName);
            }

            if (turretDict.Count == 0)
                _globals.PlayerTurretCounts.Remove(ownerSessionId);
        }

        _globals.TurretToPlayer.Remove(entityId);
    }

    private void CancelThink(uint sentryRaw)
    {
        if (_globals.SentryThink.TryGetValue(sentryRaw, out var task))
        {
            task?.Cancel();
            _globals.SentryThink.Remove(sentryRaw);
        }
    }
}
