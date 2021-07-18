using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using HugsLib;
using HugsLib.Settings;

namespace Sharknado
{
    public class SharknadoMod : ModBase
    {
        const float DEFAULT_DAMAGE_MULT = 1.5F;

        public enum CloudinessEnums { ClearSkies = 5, CloudyWithAChanceOfSharks = 25, StormSpotted = 150, SharkFallExpected = 1000, TwisterWithFins = 900000 };
        public enum SharkEnthusiasmEnums { AlreadyFed = 100, ALittleHungry = 150, Unfed = 300, Starved = 500 }
        // Can't set numbers correctly here or the sorting will be wrong in the interface
        public enum SpawnEnums { AirSharksOnly, JustAFewSharks, Sharknado, OhGodTheSharks, SharknadoIsLife };
        public static readonly Dictionary<SpawnEnums, int> spawnValues = new Dictionary<SpawnEnums, int>(5)
        {
            { SpawnEnums.AirSharksOnly, 0 },
            { SpawnEnums.JustAFewSharks, 1500 },
            { SpawnEnums.Sharknado, 500 },
            { SpawnEnums.OhGodTheSharks, 100 },
            { SpawnEnums.SharknadoIsLife, 10 }
        };
        public enum TornadoEnums { Sharknado = 1, Sharknado2TheSecondOne = 2, Sharknado3OhHellNo = 3, SharknadoThe4thAwakens = 4, Sharknado5GlobalSwarm = 5, Sharknado6 = 6 };

        public override string ModIdentifier {
            get { return "Sharknado"; }
        }

        public static SettingHandle<CloudinessEnums> cloudiness;
        public static SettingHandle<SharkEnthusiasmEnums> enthusiasm;
        public static SettingHandle<SpawnEnums> spawnRate;
        public static SettingHandle<TornadoEnums> tornadoCount;

        public void UpdateBaseChance() {
            IncidentDef sharknadoDef = IncidentDef.Named("Sharknado");
            sharknadoDef.baseChance = ((float)cloudiness.Value) / 100.0F;
        }

        public override void DefsLoaded() {
            cloudiness = Settings.GetHandle<CloudinessEnums>(
                "cloudiness",
                "Sharknado.Cloudiness".Translate(),
                "Sharknado.CloudinessDescription".Translate(),
                CloudinessEnums.StormSpotted,
                null,
                "Sharknado.Cloudiness_");
            UpdateBaseChance();
            cloudiness.ValueChanged += newValue => { UpdateBaseChance(); };

            enthusiasm = Settings.GetHandle<SharkEnthusiasmEnums>(
                "enthusiasm",
                "Sharknado.SharkEnthusiasm".Translate(),
                "Sharknado.SharkEnthusiasmDescription".Translate(),
                SharkEnthusiasmEnums.ALittleHungry,
                null,
                "Sharknado.SharkEnthusiasm_");

            spawnRate = Settings.GetHandle<SpawnEnums>(
                "spawnTickRate",
                "Sharknado.SpawnTickRate".Translate(),
                "Sharknado.SpawnTickRateDescription".Translate(),
                SpawnEnums.Sharknado,
                null,
                "Sharknado.SpawnTickRate_");

            tornadoCount = Settings.GetHandle<TornadoEnums>(
                "tornadoCount",
                "Sharknado.TornadoCount".Translate(),
                "Sharknado.TornadoCountDescription".Translate(),
                TornadoEnums.Sharknado,
                null,
                "Sharknado.TornadoCount_");
	    }
    }

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
            int spawnTickRate = SharknadoMod.spawnValues[SharknadoMod.spawnRate.Value];
            if (base.Spawned && spawnTickRate > 0 && this.IsHashIntervalTick(spawnTickRate))
            {
                IntVec3 nearbyLoc = new Vector3(this.realPosition.x + Rand.Range(-10, 10), 0f, this.realPosition.y + Rand.Range(-10, 10)).ToIntVec3();
                if (nearbyLoc.InBounds(base.Map))
                {
                    Pawn shark = (Pawn)PawnGenerator.GeneratePawn(PawnKindDef.Named("LandShark"), null);
                    if (shark == null)
                    {
                        Log.Error("Couldn't spawn shark :(");
                    }
                    else
                    {
                        Pawn spawned = (Pawn)GenSpawn.Spawn(shark, nearbyLoc, base.Map, Rot4.Random);
                        // He's a man eater!
                        spawned.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, false, false, null);
                    }
                }
            }
            base.Tick();
        }

        protected override void DoDamage(IntVec3 c, float damageFactor)
        {
            base.DoDamage(c, damageFactor * ((float)SharknadoMod.enthusiasm.Value) / 100.0F);
        }
    }

    public class IncidentWorker_Sharknado : IncidentWorker_TornadoCopy
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            TornadoCopy anyTornado = null;
            Map map = (Map)parms.target;
            for (int i = 0; i < (int)SharknadoMod.tornadoCount.Value; i++) {
                TornadoCopy tornado = TrySpawnOnMap(map);
                if (tornado != null) {
                    anyTornado = tornado;
                }
            }
            if (anyTornado != null) { 
                SendStandardLetter(parms, anyTornado);
                return true;
            }
            return false;
        }

        protected override TornadoCopy Spawn(IntVec3 loc, Map map)
        {
            return (TornadoCopy)GenSpawn.Spawn(ThingDef.Named("Sharknado"), loc, map);
        }
    }
}
