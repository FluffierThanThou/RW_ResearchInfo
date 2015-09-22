﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Fluffy
{
    public class MainTabWindow_Research : MainTabWindow
    {
        private const float LeftAreaWidth = 330f;

        private const int ModeSelectButHeight = 40;

        private const float ProjectTitleHeight = 50f;

        private const float ProjectTitleLeftMargin = 20f;

        private const int ProjectIntervalY = 25;

        protected ResearchProjectDef selectedProject;

        private enum showResearch
        {
            All,
            Completed,
            Available
        }

        private showResearch showResearchedProjects = showResearch.Available;

        private Vector2 projectListScrollPosition = default(Vector2);

        private bool noBenchWarned;

        private static readonly Texture2D BarFillTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.85f));

        private static readonly Texture2D BarBGTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f));

        public override float TabButtonBarPercent
        {
            get
            {
                ResearchProjectDef currentProj = Find.ResearchManager.currentProj;
                if (currentProj == null)
                {
                    return 0f;
                }
                return currentProj.PercentComplete;
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.selectedProject = Find.ResearchManager.currentProj;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            if (!this.noBenchWarned)
            {
                if (!Find.ListerBuildings.ColonistsHaveBuilding(ThingDefOf.ResearchBench))
                {
                    Find.WindowStack.Add(new Dialog_Message("ResearchMenuWithoutBench".Translate(), null));
                }
                this.noBenchWarned = true;
            }
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 300f), "Research".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 75f, 330f, inRect.height - 75f);
            Rect rect2 = new Rect(rect.xMax + 10f, 45f, inRect.width - rect.width - 10f, inRect.height - 45f);
            Widgets.DrawMenuSection(rect, false);
            Widgets.DrawMenuSection(rect2, true);
            Rect outRect = rect.ContractedBy(10f);
            IEnumerable<ResearchProjectDef> source;

            // Add an "all" tab, if you don't like it, chuck it out.
            if (this.showResearchedProjects == showResearch.All)
            {
                source = from proj in DefDatabase<ResearchProjectDef>.AllDefs
                         where !proj.prerequisites.Contains(proj)
                         select proj;
            }
            else if (this.showResearchedProjects == showResearch.Completed)
            {
                source = from proj in DefDatabase<ResearchProjectDef>.AllDefs
                         where proj.IsFinished && proj.PrereqsFulfilled
                         select proj;
            }
            else
            {
                source = from proj in DefDatabase<ResearchProjectDef>.AllDefs
                         where !proj.IsFinished && proj.PrereqsFulfilled
                         select proj;
            }
            float height = (float)(25 * source.Count<ResearchProjectDef>() + 100);
            Rect rect3 = new Rect(0f, 0f, outRect.width - 16f, height);
            Widgets.BeginScrollView(outRect, ref this.projectListScrollPosition, rect3);
            Rect position = rect3.ContractedBy(10f);
            GUI.BeginGroup(position);
            int num = 0;
            foreach (ResearchProjectDef current in from rp in source
                                                   orderby rp.totalCost
                                                   select rp)
            {
                Rect rect4 = new Rect(0f, (float)num, position.width, 25f);
                if (this.selectedProject == current)
                {
                    GUI.DrawTexture(rect4, TexUI.HighlightTex);
                }

                string text = current.LabelCap + " (" + current.totalCost.ToString("F0") + ")";
                Rect rect5 = new Rect(rect4);
                rect5.x += 6f;
                rect5.width -= 6f;
                float num2 = Text.CalcHeight(text, rect5.width);
                if (rect5.height < num2)
                {
                    rect5.height = num2 + 3f;
                }
                // give the label a colour if we're in the all tab.
                Color textColor;
                if (this.showResearchedProjects == showResearch.All)
                {
                    if (current.IsFinished)
                    {
                        textColor = new Color(1f, 1f, 1f);
                    }
                    else if (!current.PrereqsFulfilled)
                    {
                        textColor = new Color(.6f, .6f, .6f);
                    }
                    else
                    {
                        textColor = new Color(.8f, .85f, 1f);
                    }
                } 
                else
                {
                    textColor = new Color(.8f, .85f, 1f);
                }
                if (Widgets.TextButton(rect5, text, false, true, textColor))
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    this.selectedProject = current;
                }
                num += 25;
            }
            GUI.EndGroup();
            Widgets.EndScrollView();
            List<TabRecord> list = new List<TabRecord>();
            TabRecord item = new TabRecord("RI.All".Translate(), delegate
            {
                this.showResearchedProjects = showResearch.All;
            }, this.showResearchedProjects == showResearch.All);
            list.Add(item);
            TabRecord item2 = new TabRecord("Researched".Translate(), delegate
            {
                this.showResearchedProjects = showResearch.Completed;
            }, this.showResearchedProjects == showResearch.Completed);
            list.Add(item2);
            TabRecord item3 = new TabRecord("RI.Available".Translate(), delegate
            {
                this.showResearchedProjects = showResearch.Available;
            }, this.showResearchedProjects == showResearch.Available);
            list.Add(item3);
            TabDrawer.DrawTabs(rect, list);
            Rect position2 = rect2.ContractedBy(20f);
            GUI.BeginGroup(position2);
            if (this.selectedProject != null)
            {
                Text.Font = GameFont.Medium;
                GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
                Rect rect6 = new Rect(20f, 0f, position2.width - 20f, 50f);
                Widgets.Label(rect6, this.selectedProject.LabelCap);
                GenUI.ResetLabelAlign();
                Text.Font = GameFont.Small;
                Rect rect7 = new Rect(0f, 50f, position2.width, position2.height - 50f);
                string desc = this.selectedProject.description;

                // select prerequisites
                desc += ".\n\n";
                string[] prereqs = selectedProject.prerequisites.Select(def => def.LabelCap).ToArray();
                desc += "RI.Prerequisites".Translate() + ": ";
                if (prereqs == null || prereqs.Count() == 0)
                {
                    desc += "RI.none".Translate();
                }
                else
                {
                    desc += String.Join(", ", prereqs);
                }
                desc += ".\n\n";

                // select follow-ups
                string[] follow = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(def => def.prerequisites.Contains(selectedProject)).Select(def => def.LabelCap).ToArray();
                desc += "RI.LeadsTo".Translate() + ": ";
                if (follow == null || follow.Count() == 0)
                {
                    desc += "RI.none".Translate();
                }
                else
                {
                    desc += String.Join(", ", follow);
                }
                desc += ".\n\n";

                //// find all unlocks
                //desc += "Unlocks: ";
                //string[] unlocks = getUnlocks(selectedProject);
                //if (unlocks == null || unlocks.Count() == 0)
                //{
                //    desc += "none";
                //}
                //else
                //{
                //    desc += String.Join(", ", unlocks);
                //}
                //desc += ".\n\n";


                Widgets.Label(rect7, desc);
                Rect rect8 = new Rect(position2.width / 2f - 50f, 300f, 100f, 50f);
                if (this.selectedProject.IsFinished)
                {
                    Widgets.DrawMenuSection(rect8, true);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect8, "Finished".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else if (this.selectedProject == Find.ResearchManager.currentProj)
                {
                    Widgets.DrawMenuSection(rect8, true);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect8, "InProgress".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else if (!this.selectedProject.PrereqsFulfilled)
                {
                    Widgets.DrawMenuSection(rect8);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect8, "RI.PreReqLocked".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else
                {
                    if (Widgets.TextButton(rect8, "Research".Translate(), true, false))
                    {
                        SoundDef.Named("ResearchStart").PlayOneShotOnCamera();
                        Find.ResearchManager.currentProj = this.selectedProject;
                    }
                    if (Prefs.DevMode)
                    {
                        Rect rect9 = rect8;
                        rect9.x += rect9.width + 4f;
                        if (Widgets.TextButton(rect9, "Debug Insta-finish", true, false))
                        {
                            Find.ResearchManager.currentProj = this.selectedProject;
                            Find.ResearchManager.InstantFinish(this.selectedProject);
                        }
                    }
                }
                Rect rect10 = new Rect(15f, 450f, position2.width - 30f, 35f);
                Widgets.FillableBar(rect10, this.selectedProject.PercentComplete, MainTabWindow_Research.BarFillTex, MainTabWindow_Research.BarBGTex, true);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect10, this.selectedProject.ProgressNumbersString);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            GUI.EndGroup();
        }


        //// too many options, different ways, dll unlocking. This won't work.
        //public string[] getUnlocks(ResearchProjectDef def)
        //{
        //    string[] recipes = DefDatabase<RecipeDef>.AllDefsListForReading.Where(rd => re)
        //}
    }
}
