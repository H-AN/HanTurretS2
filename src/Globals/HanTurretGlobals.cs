using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using static Dapper.SqlMapper;
using static HanTurretS2.HanTurretS2Config;

namespace HanTurretS2;

public class HanTurretGlobals
{
    public Dictionary<uint, Turrets> TurretData = new();

    public Dictionary<uint, CancellationTokenSource> SentryThink = new Dictionary<uint, CancellationTokenSource>();

    public readonly Dictionary<uint, SentryParticles> sentryParticles = new Dictionary<uint, SentryParticles>();

    public Dictionary<uint, SentryParticles> TurretEffects = new();

    public Dictionary<ulong, Dictionary<string, HashSet<uint>>> PlayerTurretCounts = new();

    public bool TurretCanFire { get; set; }
    public class SentryParticles
    {
        public CParticleSystem ParticleL { get; set; }
        public CParticleSystem ParticleR { get; set; }
    }
    public class Turrets
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public float Range { get; set; } = 0f;
        public float Rate { get; set; } = 0f;
        public float Damage { get; set; } = 0f;
        public float KnockBack { get; set; } = 0f;
        public string FireAnim { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int Limit { get; set; } = 0;
        public int Price { get; set; } = 0;
        public string Permissions { get; set; } = string.Empty;
        public string GlowColor { get; set; } = string.Empty;
        public string laserColor { get; set; } = string.Empty;
        public string TurretFireSound { get; set; } = string.Empty;
        public string MuzzleParticle { get; set; } = string.Empty;
        public string MuzzleAttachment { get; set; } = string.Empty;
    }

}