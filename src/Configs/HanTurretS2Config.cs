using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Cecil.Cil;
using SwiftlyS2.Shared;

namespace HanTurretS2;

public class HanTurretS2Config
{
    public class Turret
    {
        public bool Enable { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public float Range { get; set; } = 0f;
        public float Rate { get; set; } = 0f;
        public float Damage { get; set; } = 0f;
        public float KnockBack { get; set; } = 0f;
        public string FireAnim { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int Price { get; set; } = 0;
        public int Limit { get; set; } = 0;
        public string Permissions { get; set; } = string.Empty;
        public string GlowColor { get; set; } = string.Empty;
        public string laserColor{ get; set; } = string.Empty;
        public string TurretFireSound{ get; set; } = string.Empty;
        public string PrecacheSoundEvent { get; set; } = string.Empty;
        public string MuzzleParticle { get; set; } = string.Empty;
        public string MuzzleAttachment { get; set; } = string.Empty;
    }
    public List<Turret> TurretList { get; set; } = new List<Turret>();

}