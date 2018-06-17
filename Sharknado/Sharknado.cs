using RimWorld;
using Verse;
using UnityEngine;

namespace Sharknado
{
  [StaticConstructorOnStartup]
  public class Sharknado : TornadoCopy
  {
    protected static readonly Material SharknadoMaterial = MaterialPool.MatFrom("Things/Ethereal/Sharknado", ShaderDatabase.Transparent, MapMaterialRenderQueues.Tornado);

    protected override Material GetMaterial()
    {
      return SharknadoMaterial;
    }

    public override void Tick()
    {
      if (base.Spawned && this.IsHashIntervalTick(500))
      {
        IntVec3 nearbyLoc = new Vector3(this.realPosition.x + Rand.Range(-10, 10), 0f, this.realPosition.y + Rand.Range(-10, 10)).ToIntVec3();
        Pawn shark = PawnGenerator.GeneratePawn(PawnKindDef.Named("LandShark"), null);
        Pawn spawned = (Pawn)GenSpawn.Spawn(shark, nearbyLoc, base.Map, Rot4.Random);
        spawned.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, null);
        Log.Message("Waiting for: " + this.ticksLeftToDisappear);
      }
      base.Tick();
    }
  }

  public class IncidentWorker_Sharknado : IncidentWorker_TornadoCopy
  {
    protected override TornadoCopy Spawn(IntVec3 loc, Map map)
    {
      return (TornadoCopy)GenSpawn.Spawn(ThingDef.Named("Sharknado"), loc, map);
    }
  }
}
