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
            //activator
            /*
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.defaultLabel = "CommandSelfDyeingToggleLabel".Translate();
            command_Toggle.icon = ThingDefOf.Dye.uiIcon;
            command_Toggle.isActive = (() => Comp.Active);
            command_Toggle.toggleAction = delegate ()
            {
                Comp.Active = !Comp.Active;
            };
            if (Comp.Active)
            {
                command_Toggle.defaultDesc = "CommandSelfDyeingToggleDescActive".Translate();
            }
            else
            {
                command_Toggle.defaultDesc = "CommandSelfDyeingToggleDescInactive".Translate();
            }
            yield return command_Toggle;

            if (!Comp.Active)
            {
                yield break;
            }
            //Ideo Colors
            if (Faction.OfPlayer.ideos?.PrimaryIdeo != null)
            {
                command_Toggle = new Command_Toggle();
                command_Toggle.defaultLabel = "CommandSelfDyeingIdeoToggleLabel".Translate();
                command_Toggle.icon = Faction.OfPlayer.ideos.PrimaryIdeo.Icon;
                command_Toggle.isActive = (() => Comp.IdeoColor);
                command_Toggle.toggleAction = delegate ()
                {
                    Comp.IdeoColor = !Comp.IdeoColor;
                };
                if (Comp.IdeoColor)
                {
                    command_Toggle.defaultDesc = "CommandSelfDyeingIdeoToggleDescActive".Translate();
                }
                else
                {
                    command_Toggle.defaultDesc = "CommandSelfDyeingIdeoToggleDescInactive".Translate();
                }
                yield return command_Toggle;
            }
            //Full Paint
            command_Action = new Command_Action();
            switch (Comp.PaintMode)
            {
                case PaintMode.Partial:
                    command_Action.defaultLabel = "CommandSelfDyeingPartialLabel".Translate();
                    command_Action.defaultDesc = "CommandSelfDyeingPartialDesc".Translate();
                    command_Action.icon = SelfDyeing_Utility.texPartial;
                    break;
                case PaintMode.Full:
                    command_Action.defaultLabel = "CommandSelfDyeingFullLabel".Translate();
                    command_Action.defaultDesc = "CommandSelfDyeingFullDesc".Translate();
                    command_Action.icon = SelfDyeing_Utility.texFull;
                    break;
                case PaintMode.TwoColor:
                    command_Action.defaultLabel = "CommandSelfDyeingTwoColorLabel".Translate();
                    command_Action.defaultDesc = "CommandSelfDyeingTwoColorDesc".Translate();
                    command_Action.icon = SelfDyeing_Utility.texTwoColor;
                    break;
            }
            command_Action.action = delegate ()
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption("CommandSelfDyeingPartialLabel".Translate(), delegate ()
                {
                    Comp.PaintMode = PaintMode.Partial;
                }, SelfDyeing_Utility.texPartial, Color.white));
                list.Add(new FloatMenuOption("CommandSelfDyeingFullLabel".Translate(), delegate ()
                {
                    Comp.PaintMode = PaintMode.Full;
                }, SelfDyeing_Utility.texFull, Color.white));
                list.Add(new FloatMenuOption("CommandSelfDyeingTwoColorLabel".Translate(), delegate ()
                {
                    Comp.PaintMode = PaintMode.TwoColor;
                }, SelfDyeing_Utility.texTwoColor, Color.white));
                Find.WindowStack.Add(new FloatMenu(list));
            };
            yield return command_Action;
            yield break;
            */
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
