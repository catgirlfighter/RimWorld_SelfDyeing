using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;


namespace SelfDyeing
{
    public class CompSelfDyeing : ThingComp
    {
        private GameComponent_SelfDyeing comp = null;
        public GameComponent_SelfDyeing Comp { get { if (comp == null) comp = Current.Game.GetComponent<GameComponent_SelfDyeing>(); return comp; } }

        public override void PostExposeData()
        {
            //deprecated, left for painless transition
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                bool active = false;
                bool ideo = false;
                bool full = false;
                Scribe_Values.Look(ref active, "active", false, false);
                Scribe_Values.Look(ref ideo, "ideo", false, false);
                Scribe_Values.Look(ref full, "full", false, false);
                if (active && !Comp.Loaded)
                {
                    Comp.Loaded = true;
                    Comp.Active = active;
                    Comp.IdeoColor = ideo;
                    Comp.PaintMode = full ? PaintMode.Full : PaintMode.Partial;
                }
            }
        }

        //public static Texture2D DyeTex = null;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            //only one selected allowed
            if (Find.Selector.SingleSelectedThing == null)
                yield break;
            //
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CommandSelfDyeingToggleLabel".Translate();
            command_Action.defaultDesc = "";
            command_Action.icon = ThingDefOf.Dye.uiIcon;
            command_Action.action = delegate ()
            {
                Find.WindowStack.Add(new Dialog_ColorPatterns());
            };
            yield return command_Action;
            yield break;
        }
    }

    public class CompProperties_SelfDyeing : CompProperties
    {
        public CompProperties_SelfDyeing()
        {
            compClass = typeof(CompSelfDyeing);
        }
    }
}
