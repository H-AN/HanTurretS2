using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using static HanTurretS2.HanTurretGlobals;

namespace HanTurretS2;

public class HanTurretEffectService
{
    private readonly ILogger<HanTurretEffectService> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HanTurretGlobals _globals;
    private readonly HanTurretHelpers _helpers;
    public HanTurretEffectService(ISwiftlyCore core, ILogger<HanTurretEffectService> logger,
        HanTurretGlobals globals, HanTurretHelpers helpers)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
    }

    public void SetupMuzzle(CHandle<CBaseModelEntity> sentryHandle, string effectname, string MuzzleAttachment)
    {
        if (!sentryHandle.IsValid)
            return;

        var sentry = sentryHandle.Value;
        if (sentry == null || !sentry.IsValid)
            return;

        if (string.IsNullOrEmpty(MuzzleAttachment))
            return;

        var sentryOrigin = sentry.AbsOrigin;
        if (sentryOrigin == null)
            return;

        SwiftlyS2.Shared.Natives.Vector Position = new SwiftlyS2.Shared.Natives.Vector(sentryOrigin.Value.X, sentryOrigin.Value.Y, sentryOrigin.Value.Z);


        CParticleSystem particleL = null;
        CParticleSystem particleR = null;


        string[] attachments = MuzzleAttachment
            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();

        if (attachments.Length >= 1)
        {
            string attachmentL = attachments[0];

            particleL = _core.EntitySystem.CreateEntity<CParticleSystem>();
            particleL.EffectName = effectname;
            particleL.DispatchSpawn();
            particleL.Teleport(Position, null, null);
            particleL.AcceptInput("SetParent", "!activator", sentry, particleL);
            particleL.AcceptInput("SetParentAttachment", attachmentL, sentry, null);

        }

        if (attachments.Length >= 2)
        {
            string attachmentR = attachments[1];

            particleR = _core.EntitySystem.CreateEntity<CParticleSystem>();
            particleR.EffectName = effectname;
            particleR.DispatchSpawn();
            particleR.Teleport(Position, null, null);
            particleR.AcceptInput("SetParent", "!activator", sentry, particleR);
            particleR.AcceptInput("SetParentAttachment", attachmentR, sentry, null);

        }

        if (particleL != null || particleR != null)
        {
            _globals.sentryParticles[sentryHandle.Raw] = new SentryParticles
            {
                ParticleL = particleL,
                ParticleR = particleR,
            };
        }
    }

    public void ToggleMuzzle(CHandle<CBaseModelEntity> sentryHandle, float duration)
    {
        if (!sentryHandle.IsValid)
            return;

        var sentry = sentryHandle.Value;
        if (sentry == null || !sentry.IsValid)
            return;

        if (!_globals.sentryParticles.TryGetValue(sentryHandle.Raw, out var particles))
        {
            //_core.Logger.LogInformation($"{_core.Localizer["MuzzleError"]}");
            return;
        }

        if (particles.ParticleL != null)
        {
            particles.ParticleL.AddEntityIOEvent("Start", "", null!, null!, 0);
            particles.ParticleL.AddEntityIOEvent("Stop", "", null!, null!, duration);
        }

        if (particles.ParticleR != null)
        {
            particles.ParticleR.AddEntityIOEvent("Start", "", null!, null!, 0);
            particles.ParticleR.AddEntityIOEvent("Stop", "", null!, null!, duration);
        }
    }

    public void CreateTracer(CHandle<CBaseModelEntity> sentryHandle, IPlayer Target, string laserColor)
    {
        if (!sentryHandle.IsValid)
            return;

        var sentry = sentryHandle.Value;
        if (sentry == null || !sentry.IsValid)
            return;

        if (string.IsNullOrEmpty(laserColor))
            return;

        if (!_globals.sentryParticles.TryGetValue(sentryHandle.Raw, out var particles))
        {
            //_core.Logger.LogInformation($"{_core.Localizer["TracerError"]}");
            return;
        }

        var TargetPawn = Target.PlayerPawn;
        if (TargetPawn == null || !TargetPawn.IsValid)
            return;

        var TargetOrigin = TargetPawn.AbsOrigin;
        if (TargetOrigin == null)
            return;

        var TargetPos = new SwiftlyS2.Shared.Natives.Vector(
            TargetOrigin.Value.X,
            TargetOrigin.Value.Y,
            TargetOrigin.Value.Z + 55f
        );

        SwiftlyS2.Shared.Natives.Color color;
        if (!_helpers.TryParseColor(laserColor, out color))
        {
            _core.Logger.LogError($"{_core.Localizer["TracerColorError", laserColor]}");
            return;
        }

        if (particles.ParticleL != null && particles.ParticleL.IsValid)
        {
            var ParticleLPos = particles.ParticleL.AbsOrigin;
            if (ParticleLPos == null)
                return;

            var PositionL = new SwiftlyS2.Shared.Natives.Vector(
                ParticleLPos.Value.X,
                ParticleLPos.Value.Y,
                ParticleLPos.Value.Z
            );

            CreateAndKillBeam(PositionL, TargetPos, color);
        }

        if (particles.ParticleR != null && particles.ParticleR.IsValid)
        {
            var ParticleRPos = particles.ParticleR.AbsOrigin;
            if (ParticleRPos == null)
                return;

            var PositionR = new SwiftlyS2.Shared.Natives.Vector(
                ParticleRPos.Value.X,
                ParticleRPos.Value.Y,
                ParticleRPos.Value.Z
            );

            CreateAndKillBeam(PositionR, TargetPos, color);
        }
    }

    public void SetGlow(CBaseEntity entity, string glowColorStr)
    {
        if (string.IsNullOrEmpty(glowColorStr))
            return;

        if (!_helpers.TryParseColor(glowColorStr, out SwiftlyS2.Shared.Natives.Color parsedColor))
        {
            _core.Logger.LogError($"{_core.Localizer["GlowColorError", glowColorStr]}");
            return;
        }

        CBaseModelEntity modelGlow = _core.EntitySystem.CreateEntity<CBaseModelEntity>();
        CBaseModelEntity modelRelay = _core.EntitySystem.CreateEntity<CBaseModelEntity>();

        if (modelGlow == null || modelRelay == null)
            return;

        string modelName = entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

        modelRelay.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= unchecked((uint)~(1 << 2));
        modelRelay.SetModel(modelName);
        modelRelay.Spawnflags = 256u;
        modelRelay.RenderMode = RenderMode_t.kRenderNone;
        modelRelay.DispatchSpawn();

        modelGlow.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= unchecked((uint)~(1 << 2));
        modelGlow.SetModel(modelName);
        modelGlow.Spawnflags = 256u;
        modelGlow.DispatchSpawn();

        modelGlow.Glow.GlowColorOverride = parsedColor;
        modelGlow.Glow.GlowRange = 5000;
        modelGlow.Glow.GlowTeam = -1;
        modelGlow.Glow.GlowType = 3;
        modelGlow.Glow.GlowRangeMin = 100;

        modelRelay.AcceptInput("FollowEntity", "!activator", entity, modelRelay);
        modelGlow.AcceptInput("FollowEntity", "!activator", modelRelay, modelGlow);
    }

    private void CreateAndKillBeam(SwiftlyS2.Shared.Natives.Vector startPos, SwiftlyS2.Shared.Natives.Vector endPos, SwiftlyS2.Shared.Natives.Color color)
    {
        CBeam beam = _core.EntitySystem.CreateEntity<CBeam>();
        if (beam == null)
            return;
        beam.DispatchSpawn();

        beam.Render = color;
        beam.Width = 0.5f;
        beam.EndWidth = 0.5f;

        beam.Teleport(startPos, new SwiftlyS2.Shared.Natives.QAngle(0, 0, 0), new SwiftlyS2.Shared.Natives.Vector(0, 0, 0));
        beam.EndPos = endPos;

        beam.AddEntityIOEvent("Kill", "", null!, null!,  0.1f);
    }

}