using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using System.Text.RegularExpressions;

namespace SelfDyeing
{
    [StaticConstructorOnStartup]
    public class Dialog_ColorPatterns_Tex
    {
        public static readonly Texture2D DragButton = ContentFinder<Texture2D>.Get("UI/Icons/Drag");
    }
    public class Dialog_ColorPatterns : Window
    {
        const int pic_in = 24;
        const int pic_out = 28;
        const int col_pic_in = 26;
        const int b_pre_line = 1;
        const int b_line = 3;
        const int col_pic_sep = 1;
        int listId = -1;

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
                if (allColors == null)
                {
                    allColors = (from x in DefDatabase<ColorDef>.AllDefsListForReading
                                      where x.colorType == ColorType.Ideo
                                      select x into ic
                                      select ic.color).ToList();
                    allColors.SortByColor((Color x) => x);
                }
                return allColors;
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

        private Vector2 scrollbar = default;
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
                    if (ReorderableWidget.Dragging) return;
                    List<FloatMenuOption> list = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("CommandSelfDyeingPartialLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            mode = PaintMode.Partial;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingFullLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            mode = PaintMode.Full;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingTwoColorLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            mode = PaintMode.TwoColor;
                        }, (Thing)null, Color.white)
                    };
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
                    if (ReorderableWidget.Dragging) return;
                    List<FloatMenuOption> list = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("CommandSelfDyeingOverridngAlwaysLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            overrideMode = OverrideMode.Always;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingOverridingRequiredLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            overrideMode = OverrideMode.Required;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingOverridingNeverLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            overrideMode = OverrideMode.Never;
                        }, (Thing)null, Color.white)
                    };
                    Find.WindowStack.Add(new FloatMenu(list));
                };
            }
            top += pic_in;

        }
        //
        void AddPatternMenu(Rect rect, ref int top)
        {
            if (patterns.Count > 0)
            {
                top += b_pre_line;
                Widgets.DrawLineHorizontal(rect.x, top, rect.width);
                top += b_line;
            }
            //
            if(ActionLabel(new Rect(rect.x, top, rect.width, pic_in), "CommandSelfDyeingAddPatternLabel".Translate(), ""))
            {
                if (ReorderableWidget.Dragging) return;
                patterns.Add(new ColorPattern());
            };
            top += pic_in;
        }
        //
        void PatternList(Rect rect, ref int top, ref int _listId)
        {
            var inrect = new Rect(rect.x + pic_out, rect.y, rect.width - pic_out, pic_in);

            if (Event.current.type == EventType.Repaint)
            {
                _listId = ReorderableWidget.NewGroup(
                    delegate (int from, int to) 
                    {
                        if (to == patterns.Count) to -= 1;
                        (patterns[from], patterns[to]) = (patterns[to], patterns[from]);
                    },
                    ReorderableDirection.Vertical, new Rect(rect.x, top + pic_out, rect.width, rect.height));
            }

            for (int patternidx = 0; patternidx < patterns.Count; patternidx++)
            {
                int p_height = 0;
                if (patternidx > 0)
                {
                    p_height += b_pre_line;
                    Widgets.DrawLineHorizontal(rect.x, top, rect.width);
                    p_height += b_line;
                }

                GUI.DrawTexture(new Rect(rect.x, top + p_height, pic_in, pic_in), Dialog_ColorPatterns_Tex.DragButton);

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
                if (ActionLabel(new Rect(inrect.x, top + p_height, inrect.width / 3, inrect.height), "CommandSelfDyeingPatternColorLabel".Translate() + ": " + label))
                {
                    if (ReorderableWidget.Dragging) return;
                    List<FloatMenuOption> list = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("CommandSelfDyeingPatternColorPrimaryLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            pattern.color = null;
                            pattern.coloringMode = ColoringMode.Primary;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingPatternColorSecondaryLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            pattern.color = null;
                            pattern.coloringMode = ColoringMode.Secondary;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingPatternColorCustomLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            pattern.color = AllColors[0];
                            pattern.coloringMode = ColoringMode.None;
                        }, (Thing)null, Color.white),
                        new FloatMenuOption("CommandSelfDyeingPatternColorNoneLabel".Translate(), delegate ()
                        {
                            if (ReorderableWidget.Dragging) return;
                            pattern.color = null;
                            pattern.coloringMode = ColoringMode.None;
                        }, (Thing)null, Color.white)
                    };
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                // delete button
                if (PicRightActionLabel(new Rect(inrect.x + inrect.width / 3 * 2, top + p_height, inrect.width / 3, inrect.height), "CommandSelfDyeingPatternDelete".Translate()))
                {
                    if (ReorderableWidget.Dragging) return;
                    patterns.Remove(pattern);
                }
                // color picker
                p_height += pic_in;
                if (pattern.color != null)
                {
                    Color tmp = pattern.color.Value;
                    int colorLines = (1 + AllColors.Count / ((int)inrect.width / col_pic_in));
                    p_height += col_pic_sep;
                    Rect cpRect = new Rect(inrect.x, top + p_height, inrect.width, colorLines * col_pic_in + colorLines * 2);
                    if (!ReorderableWidget.Dragging &&  Widgets.ColorSelector(cpRect, ref tmp, allColors, out var h))
                    {
                        pattern.color = tmp;
                    }
                    p_height += col_pic_in * colorLines + colorLines * 2;
                }
                //
                int i = 0;
                Rect rulerect;
                // outfits
                for (var outfitidx = 0; outfitidx < pattern.Outfits.Count; outfitidx ++)
                {
                    var outfit = pattern.Outfits[outfitidx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + p_height + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "SelfDyeingListItemLabel".Translate("SelfDyeingPawnOutfitShort".Translate(), outfit.label)))
                    {
                        if (ReorderableWidget.Dragging) return;
                        pattern.Outfits.Remove(outfit);
                    }
                    i++;      
                }
                //apparel outfits
                for (var outfitidx = 0; outfitidx < pattern.AOutfits.Count; outfitidx++)
                {
                    var aoutfit = pattern.AOutfits[outfitidx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + p_height + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "SelfDyeingListItemLabel".Translate("SelfDyeingApparelOutfitShort".Translate(), aoutfit.label)))
                    {
                        if (ReorderableWidget.Dragging) return;
                        pattern.AOutfits.Remove(aoutfit);
                    }
                    i++;
                }
                // layers
                for (var layeridx = 0; layeridx < pattern.Layers.Count; layeridx++)
                {
                    var layer = pattern.Layers[layeridx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + p_height + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "SelfDyeingListItemLabel".Translate("SelfDyeingApparelLayerShort".Translate(), layer.label.CapitalizeFirst())))
                    {
                        if (ReorderableWidget.Dragging) return;
                        pattern.Layers.Remove(layer);
                    }
                    i++;
                }
                // BodyPartGroups
                //foreach (var group in pattern.BodyPartGroups)
                for(var groupidx = 0; groupidx < pattern.BodyPartGroups.Count; groupidx++)
                {
                    var group = pattern.BodyPartGroups[groupidx];
                    rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + p_height + pic_in * (i / 3), inrect.width / 3, pic_in);
                    if (PicLeftActionLabel(rulerect, "SelfDyeingListItemLabel".Translate("SelfDyeingApparelBodyPartShort".Translate(), group.label.CapitalizeFirst())))
                    {
                        if (ReorderableWidget.Dragging) return;
                        pattern.BodyPartGroups.Remove(group);
                    }
                    i++;
                }
                //
                rulerect = new Rect(inrect.x + inrect.width / 3 * (i % 3), top + p_height + pic_in * (i / 3), inrect.width / 3, pic_in);
                if (ActionLabel(rulerect, "CommandSelfDyeingAddRuleLabel".Translate()))
                {
                    if (ReorderableWidget.Dragging) return;
                    var list = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("SelfDyeingPawnOutfitLabel".Translate(), delegate ()
                        {
                            var sublist = new List<FloatMenuOption>();
                            foreach (var outfit in Current.Game.outfitDatabase.AllOutfits)
                                sublist.Add(new FloatMenuOption(outfit.label, delegate ()
                                {
                                    pattern.Outfits.Add(outfit);
                                }, (Thing)null, Color.white));
                            Find.WindowStack.Add(new FloatMenu(sublist, "SelfDyeingPawnOutfitLabel".Translate()));
                        }),
                        new FloatMenuOption("SelfDyeingApparelOutfitLabel".Translate(), delegate ()
                        {
                            var sublist = new List<FloatMenuOption>();
                            foreach (var aoutfit in Current.Game.outfitDatabase.AllOutfits)
                                sublist.Add(new FloatMenuOption(aoutfit.label, delegate ()
                                {
                                    pattern.AOutfits.Add(aoutfit);
                                }, (Thing)null, Color.white));
                            Find.WindowStack.Add(new FloatMenu(sublist, "SelfDyeingApparelOutfitLabel".Translate()));
                        }),
                        new FloatMenuOption("SelfDyeingApparelLayerLabel".Translate(), delegate ()
                        {
                            var sublist = new List<FloatMenuOption>();
                            foreach (var layer in DefDatabase<ApparelLayerDef>.AllDefsListForReading)
                                sublist.Add(new FloatMenuOption(layer.label.CapitalizeFirst(), delegate ()
                                {
                                    pattern.Layers.Add(layer);
                                }, (Thing)null, Color.white));
                            Find.WindowStack.Add(new FloatMenu(sublist, "SelfDyeingApparelLayerLabel".Translate()));
                        }),
                        new FloatMenuOption("SelfDyeingApparelBodyPartLabel".Translate(), delegate ()
                        {
                            var sublist = new List<FloatMenuOption>();
                            foreach (var group in DefDatabase<BodyPartGroupDef>.AllDefsListForReading.Where(x => x.listOrder > 0))
                                sublist.Add(new FloatMenuOption(group.label.CapitalizeFirst(), delegate ()
                                {
                                    pattern.BodyPartGroups.Add(group);
                                }, (Thing)null, Color.white));
                            Find.WindowStack.Add(new FloatMenu(sublist, "SelfDyeingApparelBodyPartLabel".Translate()));
                        }),
                    };
                    //
                    Find.WindowStack.Add(new FloatMenu(list));


                };
                /*p1*/
                p_height += pic_in * (1 + (i / 3));
                ReorderableWidget.Reorderable(_listId, new Rect(rect.x, top, rect.width, p_height - 1), false, true);
                //if (ReorderableWidget.Dragging && ReorderableWidget.)  Widgets.DrawHighlight(rect);
                top += p_height;
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
                PatternList(viewRect, ref top, ref listId);
                AddPatternMenu(viewRect, ref top);
                Widgets.EndScrollView();

            }
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + inRect.height - 40f, 200f, 40f), "Cancel".Translate()))
            {
                if (ReorderableWidget.Dragging) return;
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - 200f, inRect.y + inRect.height - 40f, 200f, 40f), "Accept".Translate()))
            {
                if (ReorderableWidget.Dragging) return;
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
