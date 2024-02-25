using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace SelfDyeing
{
    [DefOf]
    public static class LocalDefOf
    {
        static LocalDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LocalDefOf));
        }

        [MayRequireIdeology]
        public static ThoughtDef WearingColor_Favorite;
        [MayRequireIdeology]
        public static ThoughtDef WearingColor_Ideo;
    }

    public class JobGiver_SelfDyeing : ThinkNode_JobGiver
    {
        Apparel getToDye(List<Apparel> applist, Color? color1, Color? color2, bool overriding)
        {
            if (color1 == null)
                return null;
            //
            var a = applist.RandomElementByWeightWithFallback(
                    delegate (Apparel x) {
                        CompColorable comp = x.TryGetComp<CompColorable>();
                        return comp != null && comp.Active && !comp.Color.IndistinguishableFrom(color1.Value) && (color2 == null || (!comp.Color.IndistinguishableFrom(color2.Value))) ? 1f : 0f;
                    }
                , null);
            //
            if (a == null && overriding)
            {
                a = applist.RandomElementByWeightWithFallback(
                        delegate (Apparel x) {
                            CompColorable comp = x.TryGetComp<CompColorable>();
                            return comp != null && !comp.Color.IndistinguishableFrom(color1.Value) && (color2 == null || (!comp.Color.IndistinguishableFrom(color2.Value))) ? 1f : 0f;
                        }
                    , null);
            }
            //
            return a;
        }

        Apparel getToDye(ref List<Apparel> applist, Pawn pawn, bool hasBuff, GameComponent_SelfDyeing comp, out Color? color)
        {
            if (!comp.UsePatterns)
            {
                color = null;
                return null;
            }
            //
            int i = 0;
            while (i < applist.Count)
            {
                foreach (var pattern in comp.Patterns)
                {
                    if (pattern.ApparelOverlap(pawn, applist[i]) || pattern.coloringMode == ColoringMode.None && pattern.Empty)
                    {
                        if (pattern.color == null)
                            switch (pattern.coloringMode)
                            {
                                case ColoringMode.Primary:
                                    color = comp.IdeoColor ? pawn.Ideo?.ApparelColor : pawn.story?.favoriteColor;
                                    break;
                                case ColoringMode.Secondary:
                                    color = comp.IdeoColor ? pawn.story?.favoriteColor : pawn.Ideo?.ApparelColor;
                                    break;
                                default:
                                    color = null;
                                    break;
                            }
                        else
                            color = pattern.color;
                        //
                        if (color != null)
                        {
                            CompColorable colorable = applist[i].TryGetComp<CompColorable>();
                            if (colorable != null && (colorable.Active || comp.OverrideMode == OverrideMode.Always || comp.OverrideMode == OverrideMode.Required && pattern.coloringMode == ColoringMode.Primary && !hasBuff) && !colorable.Color.IndistinguishableFrom(color.Value))
                                return applist[i];
                        }
                        applist.RemoveAt(i);
                        i--;
                        break;
                    }
                }
                i++;
            }
            color = null;
            return null;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ModsConfig.IdeologyActive || pawn.IsQuestLodger() || pawn.needs?.mood?.thoughts?.situational == null || pawn.style == null || pawn.style.LookChangeDesired)
            {
                return null;
            }
            //
            var comp = Current.Game.GetComponent<GameComponent_SelfDyeing>();
            if (comp == null || !comp.Active && !comp.UsePatterns)
                return null;
            //
            if (Find.TickManager.TicksGame < pawn.style.nextStyleChangeAttemptTick)
            {
                return null;
            }
            //
            Thing station = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.StylingStation), PathEndMode.InteractionCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false), null);
            if (station == null)
            {
                pawn.style.ResetNextStyleChangeAttemptTick();
                return null;
            }
            //
            Color? primary;
            Color? secondary;
            bool overriding;

            bool thoughtpresent;
            List <Thought> activeThoughts = new List<Thought>();
            pawn.needs.mood.thoughts.situational.AppendMoodThoughts(activeThoughts);
            thoughtpresent = activeThoughts.Any(x => comp.IdeoColor && x.def == LocalDefOf.WearingColor_Ideo || !comp.IdeoColor && x.def == LocalDefOf.WearingColor_Favorite);
            //
            List<Apparel> applist = new List<Apparel>(pawn.apparel.WornApparel);
            Apparel apparel = getToDye(ref applist, pawn, thoughtpresent, comp, out primary);
            if (apparel == null)
            {
                if (!comp.Active) //auto-color disabled and patterns returned nothing
                    return null;
                switch (comp.PaintMode)
                {
                    case PaintMode.Full:
                        primary = comp.IdeoColor ? pawn.Ideo?.ApparelColor : pawn.story?.favoriteColor;
                        secondary = null;
                        overriding = comp.OverrideMode == OverrideMode.Always || comp.OverrideMode == OverrideMode.Required && !thoughtpresent;
                        break;
                    case PaintMode.TwoColor:
                        primary = thoughtpresent ? comp.IdeoColor ? pawn.story?.favoriteColor : pawn.Ideo?.ApparelColor : comp.IdeoColor ? pawn.Ideo?.ApparelColor : pawn.story?.favoriteColor;
                        secondary = thoughtpresent ? comp.IdeoColor ? pawn.Ideo?.ApparelColor : pawn.story?.favoriteColor : null;
                        overriding = comp.OverrideMode == OverrideMode.Always || comp.OverrideMode == OverrideMode.Required && !thoughtpresent;
                        break;
                    default:
                        if (thoughtpresent)
                        {
                            pawn.style.ResetNextStyleChangeAttemptTick();
                            return null;
                        }
                        primary = comp.IdeoColor ? pawn.Ideo?.ApparelColor : pawn.story?.favoriteColor;
                        secondary = null;
                        overriding = comp.OverrideMode == OverrideMode.Always || comp.OverrideMode == OverrideMode.Required && !thoughtpresent;
                        break;
                }

                //Log.Message($"applist = {applist.Count}, {primary}, {secondary}, {overriding}");
                apparel = getToDye(applist, primary, secondary, overriding);
            }
            //
            //Log.Message($"get to dye try again = {apparel}");
            if (apparel == null)
            {
                pawn.style.ResetNextStyleChangeAttemptTick();
                return null;
            }
            //
            List<Thing> list = pawn.Map.listerThings.ThingsOfDef(ThingDefOf.Dye);
            if (list.NullOrEmpty())
            {
                pawn.style.ResetNextStyleChangeAttemptTick();
                return null;
            }

            list.SortBy((Thing t) => t.Position.DistanceToSquared(pawn.Position));
            //
            var dye = list[0];
            //
            var job = JobMaker.MakeJob(JobDefOf.RecolorApparel);
            List<LocalTargetInfo> dyeList = job.GetTargetQueue(TargetIndex.A);
            List<LocalTargetInfo> appList = job.GetTargetQueue(TargetIndex.B);
            dyeList.Add(dye);
            apparel.TryGetComp<CompColorable>().DesiredColor = primary;
            appList.Add(apparel);
            job.SetTarget(TargetIndex.C, station);
            job.count = 1;

            pawn.style.ResetNextStyleChangeAttemptTick();
            return job;
        }
    }
}
