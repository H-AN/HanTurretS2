

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;


namespace HanTurretS2;

public class HanTurretHelpers
{
    private readonly ILogger<HanTurretHelpers> _logger;
    private readonly ISwiftlyCore _core;

    public HanTurretHelpers(ISwiftlyCore core, ILogger<HanTurretHelpers> logger)
    {
        _core = core;
        _logger = logger;
    }

    public SwiftlyS2.Shared.Natives.Vector CalculateKnockbackDirection(CHandle<CBaseModelEntity> sentryHandle, IPlayer target, float force)
    {
        if (!sentryHandle.IsValid)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var Sentry = sentryHandle.Value;
        if (Sentry == null || !Sentry.IsValid)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var sentryPos = Sentry.AbsOrigin;
        if (sentryPos == null)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var pawn = target.PlayerPawn;
        if (pawn == null || !pawn.IsValid)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var targetPos = pawn.AbsOrigin;
        if (targetPos == null)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var dir = new SwiftlyS2.Shared.Natives.Vector(
            targetPos.Value.X - sentryPos.Value.X,
            targetPos.Value.Y - sentryPos.Value.Y,
            targetPos.Value.Z - sentryPos.Value.Z
        );

        float length = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
        if (length <= 0.01f) return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        return new SwiftlyS2.Shared.Natives.Vector(
            dir.X / length * force,
            dir.Y / length * force,
            50f 
        );
    }


    public void EmitSoundFromEntity(CHandle<CBaseModelEntity> sentryHandle, string SoundPath)
    {
        if (!sentryHandle.IsValid)
            return;

        var Sentry = sentryHandle.Value;
        if (Sentry == null || !Sentry.IsValid)
            return;

        if (!string.IsNullOrEmpty(SoundPath))
        {
            var sound = new SwiftlyS2.Shared.Sounds.SoundEvent(SoundPath, 1.0f, 1.0f);
            sound.SourceEntityIndex = (int)Sentry.Index;
            sound.Recipients.AddAllPlayers();
            _core.Scheduler.NextTick(() =>{sound.Emit();});
        }
    }

    public bool GetAimPosition(CHandle<CBaseModelEntity> sentryHandle, IPlayer Target)
    {
        if (!sentryHandle.IsValid)
            return false;

        var Sentry = sentryHandle.Value;
        if (Sentry == null || !Sentry.IsValid)
            return false;

        var sentryOrigin = Sentry.AbsOrigin;
        if (sentryOrigin == null)
            return false;

        var startPos = new SwiftlyS2.Shared.Natives.Vector(sentryOrigin.Value.X, sentryOrigin.Value.Y, sentryOrigin.Value.Z + 20f);

        var targetPawn = Target.PlayerPawn;
        if (targetPawn == null || !targetPawn.IsValid)
            return false;

        var targetOrigin = targetPawn.AbsOrigin;
        if (targetOrigin == null)
            return false;

        var endPos = new SwiftlyS2.Shared.Natives.Vector(targetOrigin.Value.X, targetOrigin.Value.Y, targetOrigin.Value.Z + 60f);

        var trace = new CGameTrace();
        _core.Trace.SimpleTrace(
            startPos,
            endPos,
            RayType_t.RAY_TYPE_LINE,
            RnQueryObjectSet.Static | RnQueryObjectSet.Dynamic,
            MaskTrace.Solid, 
            MaskTrace.Empty,
            MaskTrace.Empty,
            CollisionGroup.NPC, // 忽略NPC
            ref trace,
            Sentry // 忽略炮塔自身
        );

        //Fraction == 1.0，表示射线到达了终点 (目标位置)，中间没有固体阻挡。
        //Fraction < 1.0，表示射线击中了障碍物。
        if (trace.Fraction >= 1.0f)
        {
            return true;
        }

        return false;
    }


    public bool TryParseColor(string colorStr, out SwiftlyS2.Shared.Natives.Color color)
    {
        color = new SwiftlyS2.Shared.Natives.Color(255, 100, 0, 255); // 默认值
        var parts = colorStr.Split(',');
        if (parts.Length < 3 || parts.Length > 4)
            return false;

        if (byte.TryParse(parts[0].Trim(), out byte r) &&
            byte.TryParse(parts[1].Trim(), out byte g) &&
            byte.TryParse(parts[2].Trim(), out byte b))
        {
            byte a = (parts.Length == 4 && byte.TryParse(parts[3].Trim(), out byte parsedA)) ? parsedA : (byte)255;
            color = new SwiftlyS2.Shared.Natives.Color(r, g, b, a);
            return true;
        }
        return false;
    }

    public SwiftlyS2.Shared.Natives.Vector GetForwardPosition(IPlayer player, float distance = 100f)
    {
        if (player == null || !player.IsValid)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        var Origin = pawn.AbsOrigin;
        if (Origin == null)
            return new SwiftlyS2.Shared.Natives.Vector(0, 0, 0);

        SwiftlyS2.Shared.Natives.Vector origin = new SwiftlyS2.Shared.Natives.Vector(
            Origin.Value.X,
            Origin.Value.Y,
            Origin.Value.Z
        );

        SwiftlyS2.Shared.Natives.QAngle angle = new SwiftlyS2.Shared.Natives.QAngle(
            pawn.EyeAngles.Pitch,
            pawn.EyeAngles.Yaw,
            pawn.EyeAngles.Roll
        );

        float yaw = angle.Yaw * MathF.PI / 180f;
        SwiftlyS2.Shared.Natives.Vector forward = new SwiftlyS2.Shared.Natives.Vector(MathF.Cos(yaw), MathF.Sin(yaw), 0);

        SwiftlyS2.Shared.Natives.Vector target = origin + forward * distance;
        target.Z += 10f;

        return target;
    }

}