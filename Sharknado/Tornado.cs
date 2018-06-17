﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace Sharknado
{
  /*
   * Copied from decompilation from B18 -- too many items were protected to overwrite...
   */
  [StaticConstructorOnStartup]
  public class TornadoCopy : ThingWithComps
  {
    protected Vector2 realPosition;

    protected float direction;

    protected int spawnTick;

    protected int leftFadeOutTicks = -1;

    protected int ticksLeftToDisappear = -1;

    protected Sustainer sustainer;

    protected static MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

    protected static ModuleBase directionNoise;

    protected const float Wind = 5f;

    protected const int CloseDamageIntervalTicks = 15;

    protected const float FarDamageMTBTicks = 15f;

    protected const int CloseDamageRadius = 3;

    protected const int FarDamageRadius = 8;

    protected const float BaseDamage = 30f;

    protected const int SpawnMoteEveryTicks = 4;

    protected static readonly IntRange DurationTicks = new IntRange(2700, 10080);

    protected const float DownedPawnDamageFactor = 0.2f;

    protected const float AnimalPawnDamageFactor = 0.75f;

    protected const float BuildingDamageFactor = 0.8f;

    protected const float PlantDamageFactor = 1.7f;

    protected const float ItemDamageFactor = 0.68f;

    protected const float CellsPerSecond = 1.7f;

    protected const float DirectionChangeSpeed = 0.78f;

    protected const float DirectionNoiseFrequency = 0.002f;

    protected const float TornadoAnimationSpeed = 25f;

    protected const float ThreeDimensionalEffectStrength = 4f;

    protected const int FadeInTicks = 120;

    protected const int FadeOutTicks = 120;

    protected const float MaxMidOffset = 2f;

    protected static readonly Material TornadoMaterial = MaterialPool.MatFrom("Things/Ethereal/Tornado", ShaderDatabase.Transparent, MapMaterialRenderQueues.Tornado);

    protected static readonly FloatRange PartsDistanceFromCenter = new FloatRange(1f, 10f);

    protected static readonly float ZOffsetBias = -4f * TornadoCopy.PartsDistanceFromCenter.min;

    protected static List<Thing> tmpThings = new List<Thing>();

    protected float FadeInOutFactor
    {
      get
      {
        float a = Mathf.Clamp01((float)(Find.TickManager.TicksGame - this.spawnTick) / 120f);
        float b = (this.leftFadeOutTicks >= 0) ? Mathf.Min((float)this.leftFadeOutTicks / 120f, 1f) : 1f;
        return Mathf.Min(a, b);
      }
    }

    public override void ExposeData()
    {
      base.ExposeData();
      Scribe_Values.Look<Vector2>(ref this.realPosition, "realPosition", default(Vector2), false);
      Scribe_Values.Look<float>(ref this.direction, "direction", 0f, false);
      Scribe_Values.Look<int>(ref this.spawnTick, "spawnTick", 0, false);
      Scribe_Values.Look<int>(ref this.leftFadeOutTicks, "leftFadeOutTicks", 0, false);
      Scribe_Values.Look<int>(ref this.ticksLeftToDisappear, "ticksLeftToDisappear", 0, false);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
      base.SpawnSetup(map, respawningAfterLoad);
      if (!respawningAfterLoad)
      {
        Vector3 vector = base.Position.ToVector3Shifted();
        this.realPosition = new Vector2(vector.x, vector.z);
        this.direction = Rand.Range(0f, 360f);
        this.spawnTick = Find.TickManager.TicksGame;
        this.leftFadeOutTicks = -1;
        this.ticksLeftToDisappear = TornadoCopy.DurationTicks.RandomInRange;
      }
      this.CreateSustainer();
    }

    public override void Tick()
    {
      if (base.Spawned)
      {
        if (this.sustainer == null)
        {
          Log.Error("Tornado sustainer is null.");
          this.CreateSustainer();
        }
        this.sustainer.Maintain();
        this.UpdateSustainerVolume();
        base.GetComp<CompWindSource>().wind = 5f * this.FadeInOutFactor;
        if (this.leftFadeOutTicks > 0)
        {
          this.leftFadeOutTicks--;
          if (this.leftFadeOutTicks == 0)
          {
            this.Destroy(DestroyMode.Vanish);
          }
        }
        else
        {
          if (TornadoCopy.directionNoise == null)
          {
            TornadoCopy.directionNoise = new Perlin(0.0020000000949949026, 2.0, 0.5, 4, 1948573612, QualityMode.Medium);
          }
          this.direction += (float)TornadoCopy.directionNoise.GetValue((double)Find.TickManager.TicksAbs, (double)((float)(this.thingIDNumber % 500) * 1000f), 0.0) * 0.78f;
          this.realPosition = this.realPosition.Moved(this.direction, 0.0283333343f);
          IntVec3 intVec = new Vector3(this.realPosition.x, 0f, this.realPosition.y).ToIntVec3();
          if (intVec.InBounds(base.Map))
          {
            base.Position = intVec;
            if (this.IsHashIntervalTick(15))
            {
              this.DamageCloseThings();
            }
            if (Rand.MTBEventOccurs(15f, 1f, 1f))
            {
              this.DamageFarThings();
            }
            if (this.ticksLeftToDisappear > 0)
            {
              this.ticksLeftToDisappear--;
              if (this.ticksLeftToDisappear == 0)
              {
                this.leftFadeOutTicks = 120;
                Messages.Message("MessageTornadoDissipated".Translate(), new TargetInfo(base.Position, base.Map, false), MessageTypeDefOf.PositiveEvent);
              }
            }
            if (this.IsHashIntervalTick(4) && !this.CellImmuneToDamage(base.Position))
            {
              float num = Rand.Range(0.6f, 1f);
              MoteMaker.ThrowTornadoDustPuff(new Vector3(this.realPosition.x, 0f, this.realPosition.y)
              {
                y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead)
              } + Vector3Utility.RandomHorizontalOffset(1.5f), base.Map, Rand.Range(1.5f, 3f), new Color(num, num, num));
            }
          }
          else
          {
            this.leftFadeOutTicks = 120;
            Messages.Message("MessageTornadoLeftMap".Translate(), new TargetInfo(base.Position, base.Map, false), MessageTypeDefOf.PositiveEvent);
          }
        }
      }
    }

    public override void Draw()
    {
      Rand.PushState();
      Rand.Seed = this.thingIDNumber;
      for (int i = 0; i < 180; i++)
      {
        this.DrawTornadoPart(TornadoCopy.PartsDistanceFromCenter.RandomInRange, Rand.Range(0f, 360f), Rand.Range(0.9f, 1.1f), Rand.Range(0.52f, 0.88f));
      }
      Rand.PopState();
    }

    protected virtual Material GetMaterial()
    {
      return TornadoMaterial;
    }

    protected void DrawTornadoPart(float distanceFromCenter, float initialAngle, float speedMultiplier, float colorMultiplier)
    {
      int ticksGame = Find.TickManager.TicksGame;
      float num = 1f / distanceFromCenter;
      float num2 = 25f * speedMultiplier * num;
      float num3 = (initialAngle + (float)ticksGame * num2) % 360f;
      Vector2 vector = this.realPosition.Moved(num3, this.AdjustedDistanceFromCenter(distanceFromCenter));
      vector.y += distanceFromCenter * 4f;
      vector.y += TornadoCopy.ZOffsetBias;
      Vector3 vector2 = new Vector3(vector.x, Altitudes.AltitudeFor(AltitudeLayer.Weather) + 0.046875f * Rand.Range(0f, 1f), vector.y);
      float num4 = distanceFromCenter * 3f;
      float num5 = 1f;
      if (num3 > 270f)
      {
        num5 = GenMath.LerpDouble(270f, 360f, 0f, 1f, num3);
      }
      else if (num3 > 180f)
      {
        num5 = GenMath.LerpDouble(180f, 270f, 1f, 0f, num3);
      }
      float num6 = Mathf.Min(distanceFromCenter / (TornadoCopy.PartsDistanceFromCenter.max + 2f), 1f);
      float d = Mathf.InverseLerp(0.18f, 0.4f, num6);
      Vector3 a = new Vector3(Mathf.Sin((float)ticksGame / 1000f + (float)(this.thingIDNumber * 10)) * 2f, 0f, 0f);
      vector2 += a * d;
      float a2 = Mathf.Max(1f - num6, 0f) * num5 * this.FadeInOutFactor;
      Color value = new Color(colorMultiplier, colorMultiplier, colorMultiplier, a2);
      TornadoCopy.matPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
      Matrix4x4 matrix = Matrix4x4.TRS(vector2, Quaternion.Euler(0f, num3, 0f), new Vector3(num4, 1f, num4));
      Graphics.DrawMesh(MeshPool.plane10, matrix, GetMaterial(), 0, null, 0, TornadoCopy.matPropertyBlock);
    }

    protected float AdjustedDistanceFromCenter(float distanceFromCenter)
    {
      float num = Mathf.Min(distanceFromCenter / 8f, 1f);
      num *= num;
      return distanceFromCenter * num;
    }

    protected void UpdateSustainerVolume()
    {
      this.sustainer.info.volumeFactor = this.FadeInOutFactor;
    }

    protected void CreateSustainer()
    {
      LongEventHandler.ExecuteWhenFinished(delegate
      {
        SoundDef soundDef = SoundDef.Named("Tornado");
        this.sustainer = soundDef.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
        this.UpdateSustainerVolume();
      });
    }

    protected void DamageCloseThings()
    {
      int num = GenRadial.NumCellsInRadius(3f);
      for (int i = 0; i < num; i++)
      {
        IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
        if (intVec.InBounds(base.Map) && !this.CellImmuneToDamage(intVec))
        {
          Pawn firstPawn = intVec.GetFirstPawn(base.Map);
          if (firstPawn == null || !firstPawn.Downed || !Rand.Bool)
          {
            float damageFactor = GenMath.LerpDouble(0f, 3f, 1f, 0.2f, intVec.DistanceTo(base.Position));
            this.DoDamage(intVec, damageFactor);
          }
        }
      }
    }

    protected void DamageFarThings()
    {
      IntVec3 c = (from x in GenRadial.RadialCellsAround(base.Position, 8f, true)
                   where x.InBounds(base.Map)
                   select x).RandomElement<IntVec3>();
      if (this.CellImmuneToDamage(c))
      {
        return;
      }
      this.DoDamage(c, 0.5f);
    }

    protected bool CellImmuneToDamage(IntVec3 c)
    {
      if (c.Roofed(base.Map) && c.GetRoof(base.Map).isThickRoof)
      {
        return true;
      }
      Building edifice = c.GetEdifice(base.Map);
      return edifice != null && edifice.def.category == ThingCategory.Building && (edifice.def.building.isNaturalRock || (edifice.def == ThingDefOf.Wall && edifice.Faction == null));
    }

    protected void DoDamage(IntVec3 c, float damageFactor)
    {
      TornadoCopy.tmpThings.Clear();
      TornadoCopy.tmpThings.AddRange(c.GetThingList(base.Map));
      Vector3 vector = c.ToVector3Shifted();
      Vector2 b = new Vector2(vector.x, vector.z);
      float angle = -this.realPosition.AngleTo(b) + 180f;
      for (int i = 0; i < TornadoCopy.tmpThings.Count; i++)
      {
        BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = null;
        switch (TornadoCopy.tmpThings[i].def.category)
        {
          case ThingCategory.Pawn:
            {
              Pawn pawn = (Pawn)TornadoCopy.tmpThings[i];
              battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Tornado, null);
              Find.BattleLog.Add(battleLogEntry_DamageTaken);
              if (pawn.RaceProps.baseHealthScale < 1f)
              {
                damageFactor *= pawn.RaceProps.baseHealthScale;
              }
              if (pawn.RaceProps.Animal)
              {
                damageFactor *= 0.75f;
              }
              if (pawn.Downed)
              {
                damageFactor *= 0.2f;
              }
              break;
            }
          case ThingCategory.Item:
            damageFactor *= 0.68f;
            break;
          case ThingCategory.Plant:
            damageFactor *= 1.7f;
            break;
          case ThingCategory.Building:
            damageFactor *= 0.8f;
            break;
        }
        int amount = Mathf.Max(GenMath.RoundRandom(30f * damageFactor), 1);
        TornadoCopy.tmpThings[i].TakeDamage(new DamageInfo(DamageDefOf.TornadoScratch, amount, angle, this, null, null, DamageInfo.SourceCategory.ThingOrUnknown)).InsertIntoLog(battleLogEntry_DamageTaken);
      }
      TornadoCopy.tmpThings.Clear();
    }
  }
}
