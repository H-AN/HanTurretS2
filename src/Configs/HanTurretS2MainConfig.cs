using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Cecil.Cil;
using SwiftlyS2.Shared;

namespace HanTurretS2;

public class HanTurretS2MainConfig
{
    public string MenuCommand { get; set; } = string.Empty;
    public string TurretBaseModel { get; set; } = string.Empty;
    public string TurretPhysboxModel { get; set; } = string.Empty;

}