using System.Collections.Generic;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

/// <summary>Shared mutable state flowing through resolution sub-steps.</summary>
internal class ResolutionContext
{
    public required GameState State { get; init; }
    public Dictionary<int, int> DamageTaken { get; set; } = new();
    public int MaxDamage { get; set; }
    public int AttackerIndex { get; set; } = -1;
}
