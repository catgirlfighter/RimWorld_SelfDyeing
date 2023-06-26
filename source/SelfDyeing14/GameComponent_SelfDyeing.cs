using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace SelfDyeing
{
    public enum PaintMode
    {
        Partial = 0,
        Full = 1,
        TwoColor = 2
    }

    public enum OverrideMode
    {
        Always = 0,
        Required = 1,
        Never = 2
    }

    public enum ColoringMode
    {
        Primary = 0,
        Secondary = 1,
        None = 2
    }

    public class ColorPattern: IExposable
    {
        public List<Outfit> Outfits = new List<Outfit>();
        public List<Outfit> AOutfits = new List<Outfit>();
        public List<ApparelLayerDef> Layers = new List<ApparelLayerDef>();
        public List<BodyPartGroupDef> BodyPartGroups = new List<BodyPartGroupDef>();
        //public bool usePrimary = true;
        public ColoringMode coloringMode;
        public Color? color;

        public void ExposeData()
        {
            Scribe_Values.Look(ref coloringMode, "coloringMode", ColoringMode.Primary);
            Scribe_Values.Look(ref color, "color", null);
            Scribe_Collections.Look(ref Outfits, "outfits", LookMode.Reference);
            Scribe_Collections.Look(ref AOutfits, "aoutfits", LookMode.Reference);
            Scribe_Collections.Look(ref Layers, "layers", LookMode.Def);
            Scribe_Collections.Look(ref BodyPartGroups, "groups", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                bool usePrimary = true;
                Scribe_Values.Look(ref usePrimary, "primary", true);
                if (!usePrimary) coloringMode = ColoringMode.Secondary;
            }

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Outfits == null) Outfits = new List<Outfit>();
                if (AOutfits == null) AOutfits = new List<Outfit>();
                if (Layers == null) Layers = new List<ApparelLayerDef>();
                if (BodyPartGroups == null) BodyPartGroups = new List<BodyPartGroupDef>();
            }
        }

        public bool Empty { get => Outfits.NullOrEmpty() && AOutfits.NullOrEmpty() && Layers.NullOrEmpty() && BodyPartGroups.NullOrEmpty(); }
        public int Count { get => Outfits.Count() + AOutfits.Count() + Layers.Count() + BodyPartGroups.Count(); }

        public void CopyTo(ColorPattern dst)
        {
            dst.color = color;
            dst.coloringMode = coloringMode;
            dst.Outfits.Clear();
            dst.Outfits.AddRange(Outfits);
            dst.AOutfits.Clear();
            dst.AOutfits.AddRange(AOutfits);
            dst.Layers.Clear();
            dst.Layers.AddRange(Layers);
            dst.BodyPartGroups.Clear();
            dst.BodyPartGroups.AddRange(BodyPartGroups);
        }

        public bool ApparelOverlap(Pawn pawn, Apparel apparel)
        {
            //Log.Message($"outfits={Outfits.Count}, aoutfits={AOutfits.Count}, layers={Layers.Count}, bpgs={BodyPartGroups.Count}");
            if (Empty) return false;
            //
            if (!Outfits.NullOrEmpty() && (pawn.outfits?.CurrentOutfit == null || !Outfits.Contains(pawn.outfits.CurrentOutfit)))
                return false;

            if (!AOutfits.NullOrEmpty() && AOutfits.FirstOrDefault(o => o.filter.Allows(apparel)) == null)
                return false;

            if (!Layers.NullOrEmpty())
                foreach (var layer in Layers)
                    if (!apparel.def.apparel.layers.Contains(layer))
                        return false;

            if (!BodyPartGroups.NullOrEmpty())
            {
                var appBPGs = apparel.def.apparel.GetInterferingBodyPartGroups(pawn.RaceProps.body);
                foreach (var bpg in BodyPartGroups)
                    if (!appBPGs.Contains(bpg))
                        return false;
            }

            return true;               
        }
    }

    public class GameComponent_SelfDyeing : GameComponent
    {
        private bool active = false;
        private bool ideoColor = false;
        private PaintMode paintMode = 0;
        private List<ColorPattern> patterns = new List<ColorPattern>();
        public bool Loaded = false;
        //private bool overriding = false;
        private OverrideMode overrideMode;
        private bool usePatterns = false;

        public bool Active { get => active; set { if (value == active) return; active = value; } }
        public bool IdeoColor { get => ideoColor; set { if (value == ideoColor) return; ideoColor = value; } }
        public PaintMode PaintMode { get => paintMode; set { if (value == paintMode) return; paintMode = value; } }
        //public bool Overriding { get => overriding; set { if (value == overriding) return; overriding = value; } }
        public OverrideMode OverrideMode { get => overrideMode; set { if (value == overrideMode) return; overrideMode = value; } }
        public bool UsePatterns { get => usePatterns; set { if (value == usePatterns) return; usePatterns = value; } }
        public List<ColorPattern> Patterns { get => patterns; }

        //public GameComponent_SelfDyeing()
        //{
        //}

        public GameComponent_SelfDyeing(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref active, "active", false);
            Scribe_Values.Look(ref ideoColor, "ideo", false);
            Scribe_Values.Look(ref paintMode, "full", PaintMode.Partial);
            Scribe_Values.Look(ref overrideMode, "overrideMode", OverrideMode.Required);
            Scribe_Values.Look(ref usePatterns, "usePatterns", false);
            Scribe_Collections.Look(ref patterns, "patterns");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                bool overriding = false;
                Scribe_Values.Look(ref overriding, "overriding", false);
                if (overriding) overrideMode = OverrideMode.Always;
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars && active && !Loaded)
                Loaded = true;

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (patterns == null) patterns = new List<ColorPattern>();
            }
        }
    }
}
