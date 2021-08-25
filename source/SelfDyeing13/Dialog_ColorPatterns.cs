using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace SelfDyeing
{
    public class Dialog_ColorPatterns : Window
    {
        const int pic_in = 24;
        const int pic_out = 28;
        const int col_pic_in = 26;
        const int b_pre_line = 1;
        const int b_line = 3;
        const int col_pic_sep = 1;

        private GameComponent_SelfDyeing comp = null;

        public override Vector2 InitialSize { get => new Vector2(950f, 750f); }
        public GameComponent_SelfDyeing Comp { get { if (comp == null) comp = Current.Game.GetComponent<GameComponent_SelfDyeing>(); return comp; } }

        public Dialog_ColorPatterns()
        {
            forcePause = true;
        }

        //copy-paste from Dialog_StylingStation
        private List<Color> allColors;
        private List<Color> AllColors
        {
            get
            {
                if (this.allColors == null)
                {
                    this.allColors = (from x in DefDatabase<ColorDef>.AllDefsListForReading
                                      where !x.hairOnly
                                      select x into ic
                                      select ic.color).ToList<Color>();
                    //if (this.pawn.Ideo != null && !this.allColors.Any((Color c) => this.pawn.Ideo.ApparelColor.IndistinguishableFrom(c)))
                    //{
                    //    this.allColors.Add(this.pawn.Ideo.ApparelColor);
                    //}
                    //if (this.pawn.story != null && this.pawn.story.favoriteColor != null && !this.allColors.Any((Color c) => this.pawn.story.favoriteColor.Value.IndistinguishableFrom(c)))
                    //{
                    //    this.allColors.Add(this.pawn.story.favoriteColor.Value);
                    //}
                    this.allColors.SortByColor((Color x) => x);
                }
                return this.allColors;
            }
        }

        public override void PostOpen() {
            if (Comp == null)
            {
                Close(true);
                return;
            }
            base.PostOpen();
            active = Comp.Active;
            ideo = Comp.IdeoColor;
            mode = Comp.PaintMode;
            overrideMode = Comp.OverrideMode;
            usepatterns = Comp.UsePatterns;
            patterns = new List<ColorPattern>();
            foreach (var pattern in Comp.Patterns)
            {
                var tmp = new ColorPattern();
                pattern.CopyTo(tmp);
                patterns.Add(tmp);
            }
        }

        //copy-paste from Widgets
        private static void CheckboxDraw(float x, float y, bool active, float size = pic_in)
        {
            if(active)
                GUI.DrawTexture(new Rect(x, y, size, size), Widgets.CheckboxOnTex);
            else
                GUI.DrawTexture(new Rect(x, y, size, size), Widgets.CheckboxOffTex);
        }

        bool ActionLabel(Rect rect, string label, string tooltip = null, Texture2D tex = null)
        {
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            if (tooltip != null) TooltipHandler.TipRegion(rect, tooltip);
            //
            Rect inrect;
            if (tex == null)
            {
                inrect = new Rect(rect.x + 2, rect.y, rect.width - 2, pic_in);
            }
            else
            {
                inrect = new Rect(rect.x + pic_out, rect.y, rect.width - pic_out, pic_in);
            }
            //
            TextAnchor oldanchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(inrect, label);
            Text.Anchor = oldanchor;
            if (tex != null)
            {
                var bRect = new Rect(rect.x, rect.y, pic_in, pic_in);
                GUI.DrawTexture(bRect, tex);
            }
            return Widgets.ButtonInvisible(inrect);
        }

        bool PicLeftActionLabel(Rect rect, string label)
        {
            var inrect = new Rect(rect.x, rect.y, rect.width, pic_in);
            if (Mouse.IsOver(inrect)) Widgets.DrawHighlight(inrect);
            TextAnchor oldanchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inrect.x + pic_out, inrect.y, inrect.width - pic_out, inrect.height), label);
            Text.Anchor = oldanchor;
            var bRect = new Rect(inrect.x, inrect.y, pic_in, inrect.height);
            GUI.DrawTexture(bRect, Widgets.CheckboxOffTex);
            return Widgets.ButtonInvisible(bRect);
        }

        bool PicRightActionLabel(Rect rect, string label)
        {
            var inrect = new Rect(rect.x, rect.y, rect.width, pic_in);
            if (Mouse.IsOver(inrect)) Widgets.DrawHighlight(inrect);
            //TooltipHandler.TipRegion(inrect, tooltip);
            TextAnchor oldanchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inrect.x, inrect.y, inrect.width - pic_out, inrect.height), label);
            Text.Anchor = oldanchor;
            var bRect = new Rect(inrect.x + inrect.width - pic_in, inrect.y, pic_in, inrect.height);
            GUI.DrawTexture(bRect, Widgets.CheckboxOffTex);
            return Widgets.ButtonInvisible(bRect);
        }

        void Checkbox(Rect rect, string label, ref bool checkOn, string tooltip = null)
        {
            var inrect = new Rect(rect.x, rect.y, rect.width, pic_in);
            if (Mouse.IsOver(inrect)) Widgets.DrawHighlight(inrect);
            if(tooltip != null)
                TooltipHandler.TipRegion(inrect, tooltip);
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inrect.x + pic_out, inrect.y, inrect.width - pic_out, inrect.height), label);
            Text.Anchor = anchor;
            if (Widgets.ButtonInvisible(inrect))
            {
                checkOn = !checkOn;
            };
            CheckboxDraw(rect.x, rect.y, checkOn);
        }

        private Vector2 scrollbar = default(Vector2);
        private bool active;
        private bool ideo;
        private PaintMode mode;
        private OverrideMode overrideMode;
        private bool usepatterns;
        private List<ColorPattern> patterns;

        void Toggles(Rect rect, ref int top)
        {
            var inrect = new Rect(rect.x, top, rect.width / 3, pic_in);
            //
            Checkbox(inrect, "CommandSelfDyeingToggleLabel".Translate() + ": " + (active ? "SelfDyeingEnabled".Translate() : "SelfDyeingDisabled".Translate())
                , ref active
                , active ? "CommandSelfDyeingToggleDescActive".Translate() : "CommandSelfDyeingToggleDescInactive".Translate()
                );
            //
            if (active)
            {
                inrect.x += inrect.width;
                Checkbox(inrect, "CommandSelfDyeingIdeoToggleLabel".Translate() + ": " + (ideo ? "SelfDyeingPrimary".Translate() : "SelfDyeingSecondary".Translate())
                    , ref ideo
                    , ideo ? "CommandSelfDyeingIdeoToggleDescActive".Translate() : "CommandSelfDyeingIdeoToggleDescInactive".Translate()
                    );
                //
                inrect.x += inrect.width;
                string label;
                string desc;
                switch (mode)
                {
                    case PaintMode.Full:
                        label = "CommandSelfDyeingFullLabel".Translate();
                        desc = "CommandSelfDyeingFullDesc".Translate();
                        break;
                    case PaintMode.TwoColor:
                        label = "CommandSelfDyeingTwoColorLabel".Translate();
                        desc = "CommandSelfDyeingTwoColorDesc".Translate();
                        break;
                    default:
                        label = "CommandSelfDyeingPartialLabel".Translate();
                        desc = "CommandSelfDyeingPartialDesc".Translate();
                        break;
                }
                //
                if (ActionLabel(inrect, "CommandSelfDyeingModeLabel".Translate() + ": " + label, desc))
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        list.Add(new FloatMenuOption("CommandSelfDyeingPartialLabel".Translate(), delegate ()
                        {
                            mode = PaintMode.Partial;
                        }, null, Color.white));
                        list.Add(new FloatMenuOption("CommandSelfDyeingFullLabel".Translate(), delegate ()
                        {
                            mode = PaintMode.Full;
                        }, null, Color.white));
                        list.Add(new FloatMenuOption("CommandSelfDyeingTwoColorLabel".Translate(), delegate ()
                        {
                            mode = PaintMode.TwoColor;
                        }, null, Color.white));
                        Find.WindowStack.Add(new FloatMenu(list));
                    };
                //
                top += pic_in;
                inrect.x = rect.x;
                inrect.y = top;
                //
                inrect.x = inrect.x;
                Checkbox(inrect, "CommandSelfDyeingPatternsToggleLabel".Translate() + ": " + (usepatterns ? "SelfDyeingEnabled".Translate() : "SelfDyeingDisabled".Translate())
                    , ref usepatterns
                    , usepatterns ? "CommandSelfDyeingPatternsToggleDescActive".Translate() : "CommandSelfDyeingPatternsToggleDescInactive".Translate()
                    );
                //
                inrect.x += inrect.width;
                //
                switch (overrideMode)
                {
                    case OverrideMode.Always:
                        label = "CommandSelfDyeingOverridngAlwaysLabel".Translate();
                        desc = "CommandSelfDyeingOverridingAlwaysDesc".Translate();
                        break;
                    case OverrideMode.Required:
                        label = "CommandSelfDyeingOverridingRequiredLabel".Translate();
                        desc = "CommandSelfDyeingOverridingRequiredDesc".Translate();
                        break;
                    default:
                        label = "CommandSelfDyeingOverridingNeverLabel".Translate();
                        desc = "CommandSelfDyeingOverridingNeverDesc".Translate();
                        break;
                }

                desc += "\n\n" + "CommandSelfDyeingOverridingNote".Translate();

                if (ActionLabel(inrect, "CommandSelfDyeingOverridingToggleLabel".Translate() + ": " + label, desc))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption("CommandSelfDyeingOverridngAlwaysLabel".Translate(), delegate ()
                    {
                        overrideMode = OverrideMode.Always;
                    }, null, Color.white));
                    list.Add(new FloatMenuOption("CommandSelfDyeingOverridingRequiredLabel".Translate(), delegate ()
                    {
                        overrideMode = OverrideMode.Required;
                    }, null, Color.white));
                    list.Add(new FloatMenuOption("CommandSelfDyeingOverridingNeverLabel".Translate(), delegate ()
                    {
                        overrideMode = OverrideMode.Never;
                    }, null, Color.white));
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                //Checkbox(inrect, "CommandSelfDyeingOverridingToggleLabel".Translate() + ": " + (overriding ? "SelfDyeingEnabled".Translate() : "SelfDyeingDisabled".Translate())
                //    , ref overriding
                //    , overriding ? "CommandSelfDyeingOverridingToggleDescActive".Translate() + "\n\n" + "CommandSelfDyeingOverridingNote".Translate()
                //        : "CommandSelfDyeingOverridingToggleDescInactive".Translate() + "\n\n" + "CommandSelfDyeingOverridingNote".Translate()
                //    );
            }
            top += pic_in;

        }
        //
        void AddPatternMenu(Rect rect, ref int top)
        {
            //if (patterns.Count > 0 && patterns[patterns.Count - 1].Empty)
            //    return;
            //
            if (patterns.Count > 0)
            {
                top += b_pre_line;
                Widgets.DrawLineHorizontal(rect.x, top, rect.width);
                top += b_line;
            }
            //
            if(ActionLabel(new Rect(rect.x, top, rect.width, pic_in), "CommandSelfDyeingAddPatternLabel".Translate(), ""))
            {
                patterns.Add(new ColorPattern());
            };
            top += pic_in;
        }
        //
        void PatternList(Rect rect, ref int top)
        {
            var inrect = new Rect(rect.x + pic_out, rect.y, rect.width - pic_out, pic_in);
            for (int patternidx = 0; patternidx < patterns.Count; patternidx++)
            {
                if (patternidx > 0)
                {
                    top += b_pre_line;
                    Widgets.DrawLineHorizontal(rect.x, top, rect.width);
                    top += b_line;
                }
                if (patternidx > 0 && Widgets.ButtonImage(new Rect(rect.x, top, pic_in, pic_in), TexButton.ReorderUp))
                {
                    var tmp = patterns[patternidx];
                    patterns[patternidx] = patterns[patternidx - 1];
                    patterns[patternidx - 1] = tmp;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }

                if (patternidx < patterns.Count - 1 && Widgets.ButtonImage(new Rect(rect.x, top + pic_in, pic_in, pic_in), TexButton.ReorderDown))
                {
                    var tmp = patterns[patternidx];
                    patterns[patternidx] = patterns[patternidx + 1];
                    patterns[patternidx + 1] = tmp;
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }

                ColorPattern pattern = patterns[patternidx];
                string label;
                if(pattern.color == null)
                    switch (pattern.coloringMode)
                    {
                        case ColoringMode.Primary:
                            label = "CommandSelfDyeingPatternColorPrimaryLabel".Translate();
                            break;
                        case ColoringMode.Secondary:
                            label = "CommandSelfDyeingPatternColorSecondaryLabel".Translate();
                            break;
                        default:
                            label = "CommandSelfDyeingPatternColorNoneLabel".Translate();
                            break;
                    }
                else
                    label = "CommandSelfDyeingPatternColorCustomLabel".Translate();
                //
                if (ActionLabel(new Rect(inrect.x, top, inrect.width / 3, inrect.height), "CommandSelfDyeingPatternColorLabel".Translate() + ": " + label))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption("CommandSelfDyeingPatternColorPrimaryLabel".Translate(), delegate ()
                    {
                        pattern.color = null;
                        pattern.coloringMode = ColoringMode.Primary;
                    }, null, Color.white));
                    list.Add(new FloatMenuOption("CommandSelfDyeingPatternColorSecondaryLabel".Translate(), delegate ()
                    {
                        pattern.color = null;
                        pattern.coloringMode = ColoringMode.Secondary;
                    }, null, Color.white));
                    list.Add(new FloatMenuOption("CommandSelfDyeingPatternColorCustomLabel".Translate(), delegate ()
                    {
                        pattern.color = AllColors[0];
                        pattern.coloringMode = ColoringMode.None;
                    }, null, Color.white));
                    list.Add(new FloatMenuOption("CommandSelfDyeingPatternColorNoneLabel".Translate(), delegate ()
                    {
                        pattern.color = null;
                        pattern.coloringMode = ColoringMode.None;
                    }, null, Color.white));
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                //
                if (PicRightActionLabel(new Rect(inrect.x + inrect.width / 3 * 2, top, inrect.width / 3, inrect.height), "CommandSelfDyeingPatternDelete".Translate()))
                {
                    patterns.Remove(pattern);
                }
                //
                top += pic_in;
                if (pattern.color != null)
                {
                    Color tmp = pattern.color.Value;
                    int colorLines = (1 + AllColors.Count / ((int)inrect.width / col_pic_in));
                    top += col_pic_sep;
                    Rect cpRect = new Rect(inrect.x, top, inrect.width, colorLines * col_pic_in + colorLines * 2);
                    if (Widgets.ColorSelector(cpRect, ref tmp, allColors))
                    {
                        pattern.color = tmp;
                    }
                    top += col_pic_in * colorLines + colorLines * 2;
                }
                //
                int i = 0;
                Rect rulerect;
                // outfits
                for (var outfitidx = 0; outfitidx < pattern.Outfits.Count; outfitidx ++)
                {
                    var outfit = pattern.Outfits[outfitidx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "Outfit".Translate() + ": " + outfit.label))
                    {
                        pattern.Outfits.Remove(outfit);
                    }
                    i++;      
                }
                // layers
                for (var layeridx = 0; layeridx < pattern.Layers.Count; layeridx++)
                {
                    var layer = pattern.Layers[layeridx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "Layer".Translate() + ": " + layer.label.CapitalizeFirst()))
                    {
                        pattern.Layers.Remove(layer);
                    }
                    i++;
                }
                // BodyPartGroups
                //foreach (var group in pattern.BodyPartGroups)
                for(var groupidx = 0; groupidx < pattern.BodyPartGroups.Count; groupidx++)
                {
                    var group = pattern.BodyPartGroups[groupidx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "CommandSelfDyeingBodyPartGroupLabel".Translate() + ": " + group.label.CapitalizeFirst()))
                    {
                        pattern.BodyPartGroups.Remove(group);
                    }
                    i++;
                }
                //
                rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + pic_in * (i / 3), inrect.width / 3, pic_in);
                if (ActionLabel(rulerect, "CommandSelfDyeingAddRuleLabel".Translate()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach(var outfit in Current.Game.outfitDatabase.AllOutfits)
                        list.Add(new FloatMenuOption("Outfit".Translate() + ": " + outfit.label, delegate ()
                        {
                            pattern.Outfits.Add(outfit);
                        }, null, Color.white));
                    //
                    foreach(var layer in DefDatabase<ApparelLayerDef>.AllDefsListForReading)
                        list.Add(new FloatMenuOption("Layer".Translate() + ": " + layer.label.CapitalizeFirst(), delegate ()
                        {
                            pattern.Layers.Add(layer);
                        }, null, Color.white));
                    //
                    foreach (var group in DefDatabase<BodyPartGroupDef>.AllDefsListForReading.Where(x => x.listOrder > 0))
                        list.Add(new FloatMenuOption("CommandSelfDyeingBodyPartGroupLabel".Translate() + ": " + group.label.CapitalizeFirst(), delegate ()
                        {
                            pattern.BodyPartGroups.Add(group);
                        }, null, Color.white));
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                /*p1*/
                top += pic_in * (1 + (i / 3));
                //Log.Message($"top={top}");
            }
        }
        //
        float PatternsSubelementHeight(float inrectwidth)
        {
            var i = (patterns.Count + 1) * (pic_in + b_pre_line + b_line);
            foreach (var p in patterns)
            {
                /*p1*/
                i += (1 + p.Count / 3) * pic_in;
                
                if (p.color != null)
                {
                    var c = (1 + AllColors.Count / ((int)inrectwidth / col_pic_in));
                    i += col_pic_in * c + c * 2 + col_pic_sep;
                }
            }
            //
            //Log.Message($"newtop={i}");
            return i;
        }
        //
        public override void DoWindowContents(Rect inRect)
        {
            int top = 0;
            Toggles(inRect, ref top);
            if (active && usepatterns)
            {
                Rect scrollRect = new Rect(inRect);
                top += 4;
                scrollRect.y += top;
                scrollRect.height -= top + 4f + 40f;
                Widgets.DrawMenuSection(scrollRect);
                top += 4;
                scrollRect.y = top;
                scrollRect.x += 4f;
                scrollRect.height -= 8f;
                scrollRect.width -= 8f;
                Rect viewRect = new Rect(scrollRect);
                viewRect.width -= 20f;
                viewRect.height = PatternsSubelementHeight(viewRect.width - pic_out);
                Widgets.BeginScrollView(scrollRect, ref scrollbar, viewRect);
                PatternList(viewRect, ref top);
                AddPatternMenu(viewRect, ref top);
                Widgets.EndScrollView();
            }
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + inRect.height - 40f, 200f, 40f), "Cancel".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - 200f, inRect.y + inRect.height - 40f, 200f, 40f), "Accept".Translate()))
            {
                Comp.Active = active;
                Comp.IdeoColor = ideo;
                Comp.PaintMode = mode;
                Comp.OverrideMode = overrideMode;
                Comp.UsePatterns = usepatterns;
                Comp.Patterns.Clear();
                Comp.Patterns.AddRange(patterns);
                Close();
            }
        }
    }
}
