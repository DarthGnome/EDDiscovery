﻿/*
 * Copyright © 2016 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 *
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using EliteDangerousCore;
using EliteDangerousCore.DB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDangerousCore
{
    public static class Recipes
    {
        public class Recipe
        {
            public string name;
            public string ingredientsstring;
            public string ingredientsstringlong;
            public string[] ingredients;
            public int[] count;

            public int Count { get { return ingredients.Length; } }

            public Recipe(string n, string indg)
            {
                name = n;
                ingredientsstring = indg;
                string[] ilist = indg.Split(',');
                ingredients = new string[ilist.Length];
                count = new int[ilist.Length];

                ingredientsstringlong = "";
                for (int i = 0; i < ilist.Length; i++)
                {
                    //Thanks to 10Fe and 10 Ni to synthesise a limpet we can no longer assume the first character is a number and the rest is the material

                    string s = new string(ilist[i].TakeWhile(c => !Char.IsLetter(c)).ToArray());
                    ingredients[i] = ilist[i].Substring(s.Length);
                    count[i] = int.Parse(s);
                    MaterialCommodityData mcd = MaterialCommodityData.GetByShortName(ingredients[i]);
                    System.Diagnostics.Debug.Assert(mcd != null, "Recipe is " + name + " " + indg);
                    ingredientsstringlong = ingredientsstringlong.AppendPrePad(count[i].ToStringInvariant() + " x " + mcd.Name, Environment.NewLine);
                }
            }
        }

        public class SynthesisRecipe : Recipe
        {
            public string level;

            public SynthesisRecipe(string n, string l, string indg)
                : base(n, indg)
            {
                level = l;
            }
        }

        public class EngineeringRecipe : Recipe
        {
            public string level;
            public string modulesstring;
            public string[] modules;
            public string engineersstring;
            public string[] engineers;

            public EngineeringRecipe(string n, string indg, string mod, string lvl, string engnrs)
                : base(n, indg)
            {
                level = lvl;
                modulesstring = mod;
                modules = mod.Split(',');
                engineersstring = engnrs;
                engineers = engnrs.Split(',');
            }
        }

        public class TechBrokerUnlockRecipe : Recipe
        {
            public TechBrokerUnlockRecipe(string n, string indg)
                : base(n, indg)
            { }
        }

        static public void ResetUsed(List<MaterialCommodities> mcl)
        {
            for (int i = 0; i < mcl.Count; i++)
                mcl[i].scratchpad = mcl[i].Count;
        }

        //return maximum can make, how many made, needed string.
        static public Tuple<int, int, string, string> HowManyLeft(List<MaterialCommodities> list, Recipe r, int tomake = 0)
        {
            int max = int.MaxValue;
            System.Text.StringBuilder needed = new System.Text.StringBuilder(64);
            System.Text.StringBuilder neededlong = new System.Text.StringBuilder(64);

            for (int i = 0; i < r.ingredients.Length; i++)
            {
                string ingredient = r.ingredients[i];

                int mi = list.FindIndex(x => x.Details.Shortname.Equals(ingredient));
                int got = (mi >= 0) ? list[mi].scratchpad : 0;
                int sets = got / r.count[i];

                max = Math.Min(max, sets);

                int need = r.count[i] * tomake;

                if (got < need)
                {
                    string dispshort;
                    string displong;
                    if (mi > 0)     // if got one..
                    {
                        dispshort = (list[mi].Details.IsEncodedOrManufactured) ? " " + list[mi].Details.Name : list[mi].Details.Shortname;
                        displong = " " + list[mi].Details.Name;
                    }
                    else
                    {
                        MaterialCommodityData db = MaterialCommodityData.GetByShortName(ingredient);
                        dispshort = (db.Category == MaterialCommodityData.MaterialEncodedCategory || db.Category == MaterialCommodityData.MaterialManufacturedCategory) ? " " + db.Name : db.Shortname;
                        displong = " " + db.Name;
                    }

                    string sshort = (need - got).ToStringInvariant() + dispshort;
                    string slong = (need - got).ToStringInvariant() + " x " + displong + Environment.NewLine;

                    if (needed.Length == 0)
                    {
                        needed.Append("Need:" + sshort);
                        neededlong.Append("Need:" + Environment.NewLine + slong);
                    }
                    else
                    {
                        needed.Append("," + sshort);
                        neededlong.Append(slong);
                    }
                }
            }

            int made = 0;

            if (max > 0 && tomake > 0)             // if we have a set, and use it up
            {
                made = Math.Min(max, tomake);                // can only make this much
                System.Text.StringBuilder usedstrshort = new System.Text.StringBuilder(64);
                System.Text.StringBuilder usedstrlong = new System.Text.StringBuilder(64);

                for (int i = 0; i < r.ingredients.Length; i++)
                {
                    int mi = list.FindIndex(x => x.Details.Shortname.Equals(r.ingredients[i]));
                    System.Diagnostics.Debug.Assert(mi != -1);
                    int used = r.count[i] * made;
                    list[mi].scratchpad -= used;

                    string dispshort = (list[mi].Details.IsEncodedOrManufactured) ? " " + list[mi].Details.Name : list[mi].Details.Shortname;
                    string displong = " " + list[mi].Details.Name;

                    usedstrshort.AppendPrePad(used.ToStringInvariant() + dispshort, ",");
                    usedstrlong.AppendPrePad(used.ToStringInvariant() + " x " + displong, Environment.NewLine);
                }

                needed.AppendPrePad("Used: " + usedstrshort.ToString(), ", ");
                neededlong.Append("Used: " + Environment.NewLine + usedstrlong.ToString());
            }

            return new Tuple<int, int, string, string>(max, made, needed.ToNullSafeString(), neededlong.ToNullSafeString());
        }

        static public List<MaterialCommodities> GetShoppingList(List<Tuple<Recipe, int>> target, List<MaterialCommodities> list)
        {
            List<MaterialCommodities> shoppingList = new List<MaterialCommodities>();

            foreach (Tuple<Recipe, int> want in target)
            {
                Recipe r = want.Item1;
                int wanted = want.Item2;
                for (int i = 0; i < r.ingredients.Length; i++)
                {
                    string ingredient = r.ingredients[i];
                    int mi = list.FindIndex(x => x.Details.Shortname.Equals(ingredient));
                    int got = (mi >= 0) ? list[mi].scratchpad : 0;
                    int need = r.count[i] * wanted;

                    if (got < need)
                    {
                        int shopentry = shoppingList.FindIndex(x => x.Details.Shortname.Equals(ingredient));
                        if (shopentry >= 0)
                            shoppingList[shopentry].scratchpad += (need - got);
                        else
                        {
                            MaterialCommodityData db = MaterialCommodityData.GetByShortName(ingredient);
                            if (db != null)       // MUST be there, its well know, but will check..
                            {
                                MaterialCommodities mc = new MaterialCommodities(db);        // make a new entry
                                mc.scratchpad = (need - got);
                                shoppingList.Add(mc);
                            }
                        }
                        if (mi >= 0) list[mi].scratchpad = 0;
                    }
                    else
                    {
                        if (mi >= 0) list[mi].scratchpad -= need;
                    }
                }
            }
            return shoppingList;
        }

        public static string UsedInSythesisByFDName(string fdname)
        {
            MaterialCommodityData mc = MaterialCommodityData.GetByFDName(fdname);
            return Recipes.UsedInSynthesisByShortName(mc?.Shortname ?? "--");
        }

        public static string UsedInSynthesisByShortName(string shortname)
        {
            if (SynthesisRecipesByMaterial.ContainsKey(shortname))
                return String.Join(",", SynthesisRecipesByMaterial[shortname].Select(x => x.name + "-" + x.level));
            else
                return "";
        }

        public static SynthesisRecipe FindSynthesis(string recipename, string level)
        {
            return SynthesisRecipes.Find(x => x.name.Equals(recipename, StringComparison.InvariantCultureIgnoreCase) && x.level.Equals(level, StringComparison.InvariantCultureIgnoreCase));
        }

        public static List<SynthesisRecipe> SynthesisRecipes = new List<SynthesisRecipe>()
        {
            new SynthesisRecipe( "FSD", "Premium","1C,1Ge,1Nb,1As,1Po,1Y" ),
            new SynthesisRecipe( "FSD", "Standard","1C,1V,1Ge,1Cd,1Nb" ),
            new SynthesisRecipe( "FSD", "Basic","1C,1V,1Ge" ),

            new SynthesisRecipe( "AFM Refill", "Premium","6V,4Cr,2Zn,2Zr,1Te,1Ru" ),
            new SynthesisRecipe( "AFM Refill", "Standard","6V,2Mn,1Mo,1Zr,1Sn" ),
            new SynthesisRecipe( "AFM Refill", "Basic","3V,2Ni,2Cr,2Zn" ),

            new SynthesisRecipe( "SRV Ammo", "Premium","2P,2Se,1Mo,1Tc" ),
            new SynthesisRecipe( "SRV Ammo", "Standard","1P,1Se,1Mn,1Mo" ),
            new SynthesisRecipe( "SRV Ammo", "Basic","1P,2S" ),

            new SynthesisRecipe( "SRV Repair", "Premium","2V,1Zn,2Cr,1W,1Te" ),
            new SynthesisRecipe( "SRV Repair", "Standard","3Ni,2V,1Mn,1Mo" ),
            new SynthesisRecipe( "SRV Repair", "Basic","2Fe,1Ni" ),

            new SynthesisRecipe( "SRV Refuel", "Premium","1S,1As,1Hg,1Tc" ),
            new SynthesisRecipe( "SRV Refuel", "Standard","1P,1S,1As,1Hg" ),
            new SynthesisRecipe( "SRV Refuel", "Basic","1P,1S" ),

            new SynthesisRecipe( "Plasma Munitions", "Premium", "5Se,4Mo,4Cd,2Tc" ),
            new SynthesisRecipe( "Plasma Munitions", "Standard","5P,1Se,3Mn,4Mo" ),
            new SynthesisRecipe( "Plasma Munitions", "Basic","4P,3S,1Mn" ),

            new SynthesisRecipe( "Explosive Munitions", "Premium","5P,4As,5Hg,5Nb,5Po" ),
            new SynthesisRecipe( "Explosive Munitions", "Standard","6P,6S,4As,2Hg" ),
            new SynthesisRecipe( "Explosive Munitions", "Basic","4S,3Fe,3Ni,4C" ),

            new SynthesisRecipe( "Small Calibre Munitions", "Premium","2P,2S,2Zr,2Hg,2W,1Sb" ),
            new SynthesisRecipe( "Small Calibre Munitions", "Standard","2P,2Fe,2Zr,2Zn,2Se" ),
            new SynthesisRecipe( "Small Calibre Munitions", "Basic","2S,2Fe,1Ni" ),

            new SynthesisRecipe( "High Velocity Munitions", "Premium","4V,2Zr,4W,2Y" ),
            new SynthesisRecipe( "High Velocity Munitions", "Standard","4Fe,3V,2Zr,2W" ),
            new SynthesisRecipe( "High Velocity Munitions", "Basic","2Fe,1V" ),

            new SynthesisRecipe( "Large Calibre Munitions", "Premium","8Zn,1As,1Hg,2W,2Sb" ),
            new SynthesisRecipe( "Large Calibre Munitions", "Standard","3P,2Zr,3Zn,1As,2Sn" ),
            new SynthesisRecipe( "Large Calibre Munitions", "Basic","2S,4Ni,3C" ),

            new SynthesisRecipe( "Limpets", "Basic", "10Fe,10Ni"),

            new SynthesisRecipe( "Chaff", "Premium", "1CC,2FiC,1ThA,1PRA"),
            new SynthesisRecipe( "Chaff", "Standard", "1CC,2FiC,1ThA"),
            new SynthesisRecipe( "Chaff", "Basic", "1CC,1FiC"),

            new SynthesisRecipe( "Heat Sinks", "Premium", "2BaC,2HCW,2HE,1PHR"),
            new SynthesisRecipe( "Heat Sinks", "Standard", "2BaC,2HCW,2HE"),
            new SynthesisRecipe( "Heat Sinks", "Basic", "1BaC,1HCW"),

            new SynthesisRecipe( "Life Support", "Basic", "2Fe,1Ni"),

            new SynthesisRecipe("AX Small Calibre Munitions", "Basic", "2Fe,1Ni,2S,2WP"),
            new SynthesisRecipe("AX Small Calibre Munitions", "Standard", "2Fe,2P,2Zr,3UES,4WP" ),
            new SynthesisRecipe("AX Small Calibre Munitions", "Premium", "3Fe,2P,2Zr,4UES,2UKCP,6WP" ),

            new SynthesisRecipe("Guardian Plasma Charger Munitions", "Basic", "3Cr,2HDP,3GPC,4GSWC"),
            new SynthesisRecipe("Guardian Plasma Charger Munitions", "Standard", "4Cr,2HE,2PA,2GPCe,2GTC"),
            new SynthesisRecipe("Guardian Plasma Charger Munitions", "Premium", "6Cr,2Zr,4HE,6PA,4GPCe,3GSWP"),

            new SynthesisRecipe("Guardian Gauss Cannon Munitions", "Basic", "3Mn,2FoC,2GPC,4GSWC"),
            new SynthesisRecipe("Guardian Gauss Cannon Munitions", "Standard", "5Mn,3HRC,5FoC,4GPC,3GSWP"),
            new SynthesisRecipe("Guardian Gauss Cannon Munitions", "Premium", "8Mn,4HRC,6FiC,10FoC"),

            new SynthesisRecipe("Enzyme Missile Launcher Munitions", "Basic", "3Fe,3S,4BMC,3PE,3WP,2Pb"),
            new SynthesisRecipe("Enzyme Missile Launcher Munitions", "Standard", "6S,4W,5BMC,6PE,4WP,4Pb"),
            new SynthesisRecipe("Enzyme Missile Launcher Munitions", "Premium", "5P,4W,6BMC,5PE,4WP,6Pb"),

            new SynthesisRecipe("AX Remote Flak Munitions", "Basic", "4Ni,3C,2S"),
            new SynthesisRecipe("AX Remote Flak Munitions", "Standard", "2Sn,3Zn,1As,3UKTC,2WP"),
            new SynthesisRecipe("AX Remote Flak Munitions", "Premium", "8Zn,2W,1As,3UES,4UKTC,1WP"),

            new SynthesisRecipe("Flechette Launcher Munitions", "Basic", "1W,3EA,2MC,2B"),
            new SynthesisRecipe("Flechette Launcher Munitions", "Standard", "4W,6EA,4MC,4B"),
            new SynthesisRecipe("Flechette Launcher Munitions", "Premium", "6W,5EA,9MC,6B"),

            new SynthesisRecipe("Guardian Shard Cannon Munitions", "Basic", "3C,2V,3CS,3GPCe,5GSWC"),
            new SynthesisRecipe("Guardian Shard Cannon Munitions", "Standard", "4CS,2GPCe,2GSWP"),
            new SynthesisRecipe("Guardian Shard Cannon Munitions", "Premium", "8C,3Se,4V,8CS"),

            new SynthesisRecipe("Guardian Shard Cannon Munitions", "Basic", "3GR,2HDP,2FoC,2PA,2Pb"),
            new SynthesisRecipe("Guardian Shard Cannon Munitions", "Standard", "5GR,3HDP,4FoC,5PA,3Pb"),
            new SynthesisRecipe("Guardian Shard Cannon Munitions", "Premium", "7GR,4HDP,6FoC,8PA,5Pb"),

            new SynthesisRecipe("AX Explosive Munitions", "Basic", "3Fe,3Ni,4C,3PE"),
            new SynthesisRecipe("AX Explosive Munitions", "Standard", "6S,6P,2Hg,4UKOC,4PE"),
            new SynthesisRecipe("AX Explosive Munitions", "Premium", "5W,4Hg,2Po,5BMC,5PE,6SFD"),
        };

        public static Dictionary<string, List<SynthesisRecipe>> SynthesisRecipesByMaterial =
            SynthesisRecipes.SelectMany(r => r.ingredients.Select(i => new { mat = i, recipe = r }))
                            .GroupBy(a => a.mat)
                            .ToDictionary(g => g.Key, g => g.Select(a => a.recipe).ToList());

        public static List<EngineeringRecipe> EngineeringRecipes = new List<EngineeringRecipe>()
        {

            #region AUTOMATICALLY PRODUCED DATA

            // DATA FROM Frontiers excel spreadsheet
            // DO NOT UPDATE MANUALLY - use the netlogentry frontierdata scanner to do this

        new EngineeringRecipe("Shielded", "1WSE", "AFM", "1", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "AFM", "2", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "AFM", "3", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "AFM", "4", "Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "AFM", "5", "Unknown" ),
        new EngineeringRecipe("Lightweight Armour", "1Fe", "Armour", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Lightweight Armour", "1Fe,1CCo", "Armour", "2", "Selene Jean" ),
        new EngineeringRecipe("Lightweight Armour", "1Fe,1CCo,1HDC", "Armour", "3", "Selene Jean" ),
        new EngineeringRecipe("Lightweight Armour", "1Ge,1CCe,1FPC", "Armour", "4", "Selene Jean" ),
        new EngineeringRecipe("Lightweight Armour", "1CCe,1Sn,1MGA", "Armour", "5", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Armour", "1Ni", "Armour", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Armour", "1C,1Zn", "Armour", "2", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Armour", "1SAll,1V,1Zr", "Armour", "3", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Armour", "1GA,1W,1Hg", "Armour", "4", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Armour", "1PA,1Mo,1Ru", "Armour", "5", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Armour", "1C", "Armour", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Armour", "1C,1SHE", "Armour", "2", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Armour", "1C,1SHE,1HDC", "Armour", "3", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Armour", "1V,1SS,1FPC", "Armour", "4", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Armour", "1W,1CoS,1FCC", "Armour", "5", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Armour", "1Ni", "Armour", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Armour", "1Ni,1V", "Armour", "2", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Armour", "1SAll,1V,1HDC", "Armour", "3", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Armour", "1GA,1W,1FPC", "Armour", "4", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Armour", "1PA,1Mo,1FCC", "Armour", "5", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Armour", "1HCW", "Armour", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Armour", "1Ni,1HDP", "Armour", "2", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Armour", "1SAll,1V,1HE", "Armour", "3", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Armour", "1GA,1W,1HV", "Armour", "4", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Armour", "1PA,1Mo,1PHR", "Armour", "5", "Selene Jean" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Beam Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Beam Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Beam Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Beam Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Beam Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Beam Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Beam Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Beam Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Beam Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Beam Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Beam Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Beam Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Beam Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Beam Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Beam Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Beam Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Beam Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Beam Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Beam Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Beam Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Beam Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Beam Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Beam Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Beam Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Beam Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Beam Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Beam Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Beam Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Beam Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Beam Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Focused Weapon", "1Fe", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Fe,1CCo", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Fe,1Cr,1CCe", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Ge,1FoC,1PCa", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Focused Weapon", "1Nb,1RFC,1MSC", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Burst Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Burst Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Burst Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Burst Laser", "4", "Broo Tarquin" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Burst Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V,1Nb", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("High Capacity Magazine", "1ME,1HDC,1Sn", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("High Capacity Magazine", "1MC,1FPC,1MSC", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Cannon", "1", "Unknown" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Cannon", "2", "Unknown" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Cannon", "3", "Unknown" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Cannon", "4", "Unknown" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Cannon", "5", "Unknown" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Cannon", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Cannon", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Cannon", "3", "The Sarge" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Cannon", "4", "The Sarge" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Cannon", "5", "The Sarge" ),
        new EngineeringRecipe("Fast Scanner", "1P", "Cargo Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC", "Cargo Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC,1OSK", "Cargo Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1Mn,1FoC,1AEA", "Cargo Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1As,1RFC,1AEC", "Cargo Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1P", "Cargo Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Cargo Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Cargo Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Cargo Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Cargo Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe", "Cargo Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC", "Cargo Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC,1UED", "Cargo Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Ge,1EA,1DED", "Cargo Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Nb,1PCa,1CED", "Cargo Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Cargo Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Cargo Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Cargo Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Cargo Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Cargo Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Cargo Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Cargo Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Cargo Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Cargo Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Cargo Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS", "Cargo Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge", "Cargo Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge,1CSD", "Cargo Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1ME,1Nb,1DSD", "Cargo Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MC,1Sn,1CFSD", "Cargo Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Chaff Ammo Capacity", "1MS", "Chaff Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1P", "Chaff Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Chaff Launcher", "2", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Chaff Launcher", "3", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Chaff Launcher", "4", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Chaff Launcher", "5", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Chaff Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Chaff Launcher", "2", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Chaff Launcher", "3", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Chaff Launcher", "4", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Chaff Launcher", "5", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1WSE", "Chaff Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Chaff Launcher", "2", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Chaff Launcher", "3", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Chaff Launcher", "4", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Chaff Launcher", "5", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1P", "Collection Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Collection Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Collection Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Collection Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Collection Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Collection Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Collection Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Collection Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Collection Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Collection Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Collection Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Collection Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Collection Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Collection Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Collection Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1P", "ECM", "1", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "ECM", "2", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "ECM", "3", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "ECM", "4", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "ECM", "5", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni", "ECM", "1", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "ECM", "2", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "ECM", "3", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "ECM", "4", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "ECM", "5", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1WSE", "ECM", "1", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "ECM", "2", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "ECM", "3", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "ECM", "4", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "ECM", "5", "Ram Tah" ),
        new EngineeringRecipe("Dirty Drive Tuning", "1SLF", "Engine", "1", "Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Dirty Drive Tuning", "1SLF,1ME", "Engine", "2", "Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Dirty Drive Tuning", "1SLF,1Cr,1MC", "Engine", "3", "Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Dirty Drive Tuning", "1MCF,1Se,1CCom", "Engine", "4", "Professor Palin" ),
        new EngineeringRecipe("Dirty Drive Tuning", "1CIF,1Cd,1PI", "Engine", "5", "Professor Palin" ),
        new EngineeringRecipe("Drive Strengthening", "1C", "Engine", "1", "Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Drive Strengthening", "1HCW,1V", "Engine", "2", "Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Drive Strengthening", "1HCW,1V,1SS", "Engine", "3", "Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Drive Strengthening", "1HDP,1HDC,1CoS", "Engine", "4", "Professor Palin" ),
        new EngineeringRecipe("Drive Strengthening", "1HE,1FPC,1IS", "Engine", "5", "Professor Palin" ),
        new EngineeringRecipe("Clean Drive Tuning", "1S", "Engine", "1", "Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Clean Drive Tuning", "1SLF,1CCo", "Engine", "2", "Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Clean Drive Tuning", "1SLF,1CCo,1UED", "Engine", "3", "Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Clean Drive Tuning", "1MCF,1CCe,1DED", "Engine", "4", "Professor Palin" ),
        new EngineeringRecipe("Clean Drive Tuning", "1CCe,1Sn,1CED", "Engine", "5", "Professor Palin" ),
        new EngineeringRecipe("Double Shot", "1C", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Double Shot", "1C,1ME", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Double Shot", "1C,1ME,1CIF", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Double Shot", "1V,1MC,1SFP", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("Double Shot", "1HDC,1CCom,1EFW", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V,1Nb", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1ME,1HDC,1Sn", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1MC,1FPC,1MSC", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Frag Cannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Frag Cannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Frag Cannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Frag Cannon", "4", "Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Frag Cannon", "5", "Zacariah Nemo" ),
        new EngineeringRecipe("Faster FSD Boot Sequence", "1GR", "FSD", "1", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Faster FSD Boot Sequence", "1GR,1Cr", "FSD", "2", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Faster FSD Boot Sequence", "1GR,1HDP,1Se", "FSD", "3", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Faster FSD Boot Sequence", "1HC,1HE,1Cd", "FSD", "4", "Elvira Martuuk,Felicity Farseer" ),
        new EngineeringRecipe("Faster FSD Boot Sequence", "1EA,1HV,1Te", "FSD", "5", "Elvira Martuuk,Felicity Farseer" ),
        new EngineeringRecipe("Increased FSD Range", "1ADWE", "FSD", "1", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Increased FSD Range", "1ADWE,1CP", "FSD", "2", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Increased FSD Range", "1P,1CP,1SWS", "FSD", "3", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Increased FSD Range", "1Mn,1CHD,1EHT", "FSD", "4", "Elvira Martuuk,Felicity Farseer" ),
        new EngineeringRecipe("Increased FSD Range", "1As,1CM,1DWEx", "FSD", "5", "Elvira Martuuk,Felicity Farseer" ),
        new EngineeringRecipe("Shielded FSD", "1Ni", "FSD", "1", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Shielded FSD", "1C,1SHE", "FSD", "2", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Shielded FSD", "1C,1Zn,1SS", "FSD", "3", "Colonel Bris Dekker,Elvira Martuuk,Felicity Farseer,Professor Palin" ),
        new EngineeringRecipe("Shielded FSD", "1V,1HDC,1CoS", "FSD", "4", "Elvira Martuuk,Felicity Farseer" ),
        new EngineeringRecipe("Shielded FSD", "1W,1FPC,1IS", "FSD", "5", "Elvira Martuuk,Felicity Farseer" ),
        new EngineeringRecipe("Expanded FSD Interdictor Capture Arc", "1MS", "FSD Interdictor", "1", "Colonel Bris Dekker,Felicity Farseer,Tiana Fortune" ),
        new EngineeringRecipe("Expanded FSD Interdictor Capture Arc", "1UEF,1ME", "FSD Interdictor", "2", "Colonel Bris Dekker,Tiana Fortune" ),
        new EngineeringRecipe("Expanded FSD Interdictor Capture Arc", "1GR,1TEC,1MC", "FSD Interdictor", "3", "Colonel Bris Dekker,Tiana Fortune" ),
        new EngineeringRecipe("Expanded FSD Interdictor Capture Arc", "1ME,1SWS,1DSD", "FSD Interdictor", "4", "Colonel Bris Dekker" ),
        new EngineeringRecipe("Expanded FSD Interdictor Capture Arc", "1MC,1EHT,1CFSD", "FSD Interdictor", "5", "Unknown" ),
        new EngineeringRecipe("Longer Range FSD Interdictor", "1UEF", "FSD Interdictor", "1", "Colonel Bris Dekker,Felicity Farseer,Tiana Fortune" ),
        new EngineeringRecipe("Longer Range FSD Interdictor", "1ADWE,1TEC", "FSD Interdictor", "2", "Colonel Bris Dekker,Tiana Fortune" ),
        new EngineeringRecipe("Longer Range FSD Interdictor", "1ABSD,1AFT,1OSK", "FSD Interdictor", "3", "Colonel Bris Dekker,Tiana Fortune" ),
        new EngineeringRecipe("Longer Range FSD Interdictor", "1USA,1SWS,1AEA", "FSD Interdictor", "4", "Colonel Bris Dekker" ),
        new EngineeringRecipe("Longer Range FSD Interdictor", "1CSD,1EHT,1AEC", "FSD Interdictor", "5", "Unknown" ),
        new EngineeringRecipe("Shielded", "1WSE", "Fuel Scoop", "1", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Fuel Scoop", "2", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Fuel Scoop", "3", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Fuel Scoop", "4", "Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Fuel Scoop", "5", "Unknown" ),
        new EngineeringRecipe("Lightweight", "1P", "Fuel Transfer Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Fuel Transfer Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Fuel Transfer Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Fuel Transfer Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Fuel Transfer Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Fuel Transfer Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Fuel Transfer Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Fuel Transfer Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Fuel Transfer Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Fuel Transfer Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Fuel Transfer Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Fuel Transfer Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Fuel Transfer Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Fuel Transfer Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Fuel Transfer Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1P", "Hatch Breaker Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Hatch Breaker Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Hatch Breaker Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Hatch Breaker Limpet", "4", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Hatch Breaker Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Hatch Breaker Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Hatch Breaker Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Hatch Breaker Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Hatch Breaker Limpet", "4", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Hatch Breaker Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Hatch Breaker Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Hatch Breaker Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Hatch Breaker Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Hatch Breaker Limpet", "4", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Hatch Breaker Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Heatsink Ammo Capacity", "1MS,1V,1Nb", "Heat Sink Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1P", "Heat Sink Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Heat Sink Launcher", "2", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Heat Sink Launcher", "3", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Heat Sink Launcher", "4", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Heat Sink Launcher", "5", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Heat Sink Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Heat Sink Launcher", "2", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Heat Sink Launcher", "3", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Heat Sink Launcher", "4", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Heat Sink Launcher", "5", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1WSE", "Heat Sink Launcher", "1", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Heat Sink Launcher", "2", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Heat Sink Launcher", "3", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Heat Sink Launcher", "4", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Heat Sink Launcher", "5", "Ram Tah" ),
        new EngineeringRecipe("Lightweight Hull Reinforcement", "1Fe", "Hull Reinforcement", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Lightweight Hull Reinforcement", "1Fe,1CCo", "Hull Reinforcement", "2", "Selene Jean" ),
        new EngineeringRecipe("Lightweight Hull Reinforcement", "1Fe,1CCo,1HDC", "Hull Reinforcement", "3", "Selene Jean" ),
        new EngineeringRecipe("Lightweight Hull Reinforcement", "1Ge,1CCe,1FPC", "Hull Reinforcement", "4", "Selene Jean" ),
        new EngineeringRecipe("Lightweight Hull Reinforcement", "1CCe,1Sn,1MGA", "Hull Reinforcement", "5", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Hull Reinforcement", "1Ni", "Hull Reinforcement", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Hull Reinforcement", "1C,1Zn", "Hull Reinforcement", "2", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Hull Reinforcement", "1SAll,1V,1Zr", "Hull Reinforcement", "3", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Hull Reinforcement", "1GA,1W,1Hg", "Hull Reinforcement", "4", "Selene Jean" ),
        new EngineeringRecipe("Blast Resistant Hull Reinforcement", "1PA,1Mo,1Ru", "Hull Reinforcement", "5", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Hull Reinforcement", "1C", "Hull Reinforcement", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Hull Reinforcement", "1C,1SHE", "Hull Reinforcement", "2", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Hull Reinforcement", "1C,1SHE,1HDC", "Hull Reinforcement", "3", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Hull Reinforcement", "1V,1SS,1FPC", "Hull Reinforcement", "4", "Selene Jean" ),
        new EngineeringRecipe("Heavy Duty Hull Reinforcement", "1W,1CoS,1FCC", "Hull Reinforcement", "5", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Hull Reinforcement", "1Ni", "Hull Reinforcement", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Hull Reinforcement", "1Ni,1V", "Hull Reinforcement", "2", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Hull Reinforcement", "1SAll,1V,1HDC", "Hull Reinforcement", "3", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Hull Reinforcement", "1GA,1W,1FPC", "Hull Reinforcement", "4", "Selene Jean" ),
        new EngineeringRecipe("Kinetic Resistant Hull Reinforcement", "1PA,1Mo,1FCC", "Hull Reinforcement", "5", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Hull Reinforcement", "1HCW", "Hull Reinforcement", "1", "Liz Ryder,Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Hull Reinforcement", "1Ni,1HDP", "Hull Reinforcement", "2", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Hull Reinforcement", "1SAll,1V,1HE", "Hull Reinforcement", "3", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Hull Reinforcement", "1GA,1W,1HV", "Hull Reinforcement", "4", "Selene Jean" ),
        new EngineeringRecipe("Thermal Resistant Hull Reinforcement", "1PA,1Mo,1PHR", "Hull Reinforcement", "5", "Selene Jean" ),
        new EngineeringRecipe("Fast Scanner", "1P", "Kill Warrant Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC", "Kill Warrant Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC,1OSK", "Kill Warrant Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1Mn,1FoC,1AEA", "Kill Warrant Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1As,1RFC,1AEC", "Kill Warrant Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1P", "Kill Warrant Scanner", "1", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Kill Warrant Scanner", "2", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Kill Warrant Scanner", "3", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Kill Warrant Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Kill Warrant Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe", "Kill Warrant Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC", "Kill Warrant Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC,1UED", "Kill Warrant Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Ge,1EA,1DED", "Kill Warrant Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Nb,1PCa,1CED", "Kill Warrant Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Kill Warrant Scanner", "1", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Kill Warrant Scanner", "2", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Kill Warrant Scanner", "3", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Kill Warrant Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Kill Warrant Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Kill Warrant Scanner", "1", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Kill Warrant Scanner", "2", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Kill Warrant Scanner", "3", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Kill Warrant Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Kill Warrant Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS", "Kill Warrant Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge", "Kill Warrant Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge,1CSD", "Kill Warrant Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1ME,1Nb,1DSD", "Kill Warrant Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MC,1Sn,1CFSD", "Kill Warrant Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1P", "Life Support", "1", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Life Support", "2", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Life Support", "3", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Life Support", "4", "Lori Jameson" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Life Support", "5", "Unknown" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Life Support", "1", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Life Support", "2", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Life Support", "3", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Life Support", "4", "Lori Jameson" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Life Support", "5", "Unknown" ),
        new EngineeringRecipe("Shielded", "1WSE", "Life Support", "1", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Life Support", "2", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Life Support", "3", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Life Support", "4", "Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Life Support", "5", "Unknown" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS", "Mine", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V", "Mine", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V,1Nb", "Mine", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1ME,1HDC,1Sn", "Mine", "4", "Juri Ishmaak" ),
        new EngineeringRecipe("High Capacity Magazine", "1MC,1FPC,1MSC", "Mine", "5", "Juri Ishmaak" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Mine", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Mine", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Mine", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Mine", "4", "Juri Ishmaak" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Mine", "5", "Juri Ishmaak" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Mine", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Mine", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Mine", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Mine", "4", "Juri Ishmaak" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Mine", "5", "Juri Ishmaak" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Mine", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Mine", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Mine", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Mine", "4", "Juri Ishmaak" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Mine", "5", "Juri Ishmaak" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS", "Missile", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V", "Missile", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V,1Nb", "Missile", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1ME,1HDC,1Sn", "Missile", "4", "Liz Ryder" ),
        new EngineeringRecipe("High Capacity Magazine", "1MC,1FPC,1MSC", "Missile", "5", "Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Missile", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Missile", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Missile", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Missile", "4", "Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Missile", "5", "Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Missile", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Missile", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Missile", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Missile", "4", "Liz Ryder" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Missile", "5", "Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Missile", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Missile", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Missile", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Missile", "4", "Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Missile", "5", "Liz Ryder" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V,1Nb", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("High Capacity Magazine", "1ME,1HDC,1Sn", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MC,1FPC,1MSC", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Multicannon", "1", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Multicannon", "2", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Multicannon", "3", "Tod McQuinn,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Multicannon", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Multicannon", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Focused Weapon", "1Fe", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Focused Weapon", "1Fe,1CCo", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Focused Weapon", "1Fe,1Cr,1CCe", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Focused Weapon", "1Ge,1FoC,1PCa", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Focused Weapon", "1Nb,1RFC,1MSC", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Plasma Accelerator", "1", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Plasma Accelerator", "2", "Bill Turner,Zacariah Nemo" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Plasma Accelerator", "3", "Bill Turner" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Plasma Accelerator", "4", "Bill Turner" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Plasma Accelerator", "5", "Bill Turner" ),
        new EngineeringRecipe("Lightweight", "1P", "Point Defence", "1", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Point Defence", "2", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Point Defence", "3", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Point Defence", "4", "Ram Tah" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Point Defence", "5", "Ram Tah" ),
        new EngineeringRecipe("Point Defence Ammo Capacity", "1MS,1V,1Nb", "Point Defence", "1", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Point Defence", "1", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Point Defence", "2", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Point Defence", "3", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Point Defence", "4", "Ram Tah" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Point Defence", "5", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1WSE", "Point Defence", "1", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Point Defence", "2", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Point Defence", "3", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Point Defence", "4", "Ram Tah" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Point Defence", "5", "Ram Tah" ),
        new EngineeringRecipe("High Charge Capacity Power Distributor", "1S", "Power Distributor", "1", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("High Charge Capacity Power Distributor", "1SLF,1Cr", "Power Distributor", "2", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("High Charge Capacity Power Distributor", "1SLF,1Cr,1HDC", "Power Distributor", "3", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("High Charge Capacity Power Distributor", "1MCF,1Se,1FPC", "Power Distributor", "4", "The Dweller" ),
        new EngineeringRecipe("High Charge Capacity Power Distributor", "1CIF,1FPC,1MSC", "Power Distributor", "5", "The Dweller" ),
        new EngineeringRecipe("Charge Enhanced Power Distributor", "1SLF", "Power Distributor", "1", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Charge Enhanced Power Distributor", "1SLF,1CP", "Power Distributor", "2", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Charge Enhanced Power Distributor", "1GR,1MCF,1CHD", "Power Distributor", "3", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Charge Enhanced Power Distributor", "1HC,1CIF,1CM", "Power Distributor", "4", "The Dweller" ),
        new EngineeringRecipe("Charge Enhanced Power Distributor", "1CIF,1CM,1EFC", "Power Distributor", "5", "The Dweller" ),
        new EngineeringRecipe("Engine Focused Power Distributor", "1S", "Power Distributor", "1", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Engine Focused Power Distributor", "1S,1CCo", "Power Distributor", "2", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Engine Focused Power Distributor", "1ABSD,1Cr,1EA", "Power Distributor", "3", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Engine Focused Power Distributor", "1USA,1Se,1PCa", "Power Distributor", "4", "The Dweller" ),
        new EngineeringRecipe("Engine Focused Power Distributor", "1CSD,1Cd,1MSC", "Power Distributor", "5", "The Dweller" ),
        new EngineeringRecipe("System Focused Power Distributor", "1S", "Power Distributor", "1", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("System Focused Power Distributor", "1S,1CCo", "Power Distributor", "2", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("System Focused Power Distributor", "1ABSD,1Cr,1EA", "Power Distributor", "3", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("System Focused Power Distributor", "1USA,1Se,1PCa", "Power Distributor", "4", "The Dweller" ),
        new EngineeringRecipe("System Focused Power Distributor", "1CSD,1Cd,1MSC", "Power Distributor", "5", "The Dweller" ),
        new EngineeringRecipe("Weapon Focused Power Distributor", "1S", "Power Distributor", "1", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Weapon Focused Power Distributor", "1S,1CCo", "Power Distributor", "2", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Weapon Focused Power Distributor", "1ABSD,1HC,1Se", "Power Distributor", "3", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Weapon Focused Power Distributor", "1USA,1EA,1Cd", "Power Distributor", "4", "The Dweller" ),
        new EngineeringRecipe("Weapon Focused Power Distributor", "1CSD,1PCa,1Te", "Power Distributor", "5", "The Dweller" ),
        new EngineeringRecipe("Shielded Power Distributor", "1WSE", "Power Distributor", "1", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Shielded Power Distributor", "1C,1SHE", "Power Distributor", "2", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Shielded Power Distributor", "1C,1SHE,1HDC", "Power Distributor", "3", "Hera Tani,Marco Qwent,The Dweller" ),
        new EngineeringRecipe("Shielded Power Distributor", "1V,1SS,1FPC", "Power Distributor", "4", "The Dweller" ),
        new EngineeringRecipe("Shielded Power Distributor", "1W,1CoS,1FCC", "Power Distributor", "5", "The Dweller" ),
        new EngineeringRecipe("Armoured Power Plant", "1WSE", "Power Plant", "1", "Felicity Farseer,Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Armoured Power Plant", "1C,1SHE", "Power Plant", "2", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Armoured Power Plant", "1C,1SHE,1HDC", "Power Plant", "3", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Armoured Power Plant", "1V,1SS,1FPC", "Power Plant", "4", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Armoured Power Plant", "1W,1CoS,1FCC", "Power Plant", "5", "Hera Tani" ),
        new EngineeringRecipe("Overcharged Power Plant", "1S", "Power Plant", "1", "Felicity Farseer,Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Overcharged Power Plant", "1HCW,1CCo", "Power Plant", "2", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Overcharged Power Plant", "1HCW,1CCo,1Se", "Power Plant", "3", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Overcharged Power Plant", "1HDP,1CCe,1Cd", "Power Plant", "4", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Overcharged Power Plant", "1CCe,1CM,1Te", "Power Plant", "5", "Hera Tani" ),
        new EngineeringRecipe("Low Emissions Power Plant", "1Fe", "Power Plant", "1", "Felicity Farseer,Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Low Emissions Power Plant", "1Fe,1IED", "Power Plant", "2", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Low Emissions Power Plant", "1Fe,1IED,1HE", "Power Plant", "3", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Low Emissions Power Plant", "1Ge,1UED,1HV", "Power Plant", "4", "Hera Tani,Marco Qwent" ),
        new EngineeringRecipe("Low Emissions Power Plant", "1Nb,1DED,1PHR", "Power Plant", "5", "Hera Tani" ),
        new EngineeringRecipe("Lightweight", "1P", "Prospecting Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Prospecting Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Prospecting Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Prospecting Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Prospecting Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Prospecting Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Prospecting Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Prospecting Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Prospecting Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Prospecting Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Prospecting Limpet", "1", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Prospecting Limpet", "2", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Prospecting Limpet", "3", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Prospecting Limpet", "4", "Ram Tah,The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Prospecting Limpet", "5", "The Sarge,Tiana Fortune" ),
        new EngineeringRecipe("Efficient Weapon", "1S", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1S,1HDP", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1ESED,1Cr,1HE", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1IED,1Se,1HV", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Efficient Weapon", "1UED,1Cd,1PHR", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Focused Weapon", "1Fe", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Fe,1CCo", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Fe,1Cr,1CCe", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Ge,1FoC,1PCa", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Focused Weapon", "1Nb,1RFC,1MSC", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Ni,1CCo,1EA", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zn,1CCe,1PCa", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Overcharged Weapon", "1Zr,1CPo,1EFW", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MS,1HDP", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1SLF,1ME,1PAll", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1MCF,1MC,1ThA", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Rapid Fire Modification", "1PAll,1CCom,1Tc", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Pulse Laser", "1", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Pulse Laser", "2", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Pulse Laser", "3", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Pulse Laser", "4", "Broo Tarquin,The Dweller" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Pulse Laser", "5", "Broo Tarquin" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS", "Rail Gun", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V", "Rail Gun", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MS,1V,1Nb", "Rail Gun", "3", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1ME,1HDC,1Sn", "Rail Gun", "4", "Tod McQuinn" ),
        new EngineeringRecipe("High Capacity Magazine", "1MC,1FPC,1MSC", "Rail Gun", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Rail Gun", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Rail Gun", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Rail Gun", "3", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Rail Gun", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Rail Gun", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1S", "Rail Gun", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF", "Rail Gun", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1S,1MCF,1FoC", "Rail Gun", "3", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1MCF,1FoC,1CPo", "Rail Gun", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Long-Range Weapon", "1CIF,1ThA,1BiC", "Rail Gun", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni", "Rail Gun", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF", "Rail Gun", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1Ni,1MCF,1EA", "Rail Gun", "3", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1MCF,1EA,1CPo", "Rail Gun", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Short-Range Blaster", "1CIF,1CCom,1BiC", "Rail Gun", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Rail Gun", "1", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Rail Gun", "2", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Rail Gun", "3", "The Sarge,Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Rail Gun", "4", "Tod McQuinn" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Rail Gun", "5", "Tod McQuinn" ),
        new EngineeringRecipe("Shielded", "1WSE", "Refineries", "1", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Refineries", "2", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Refineries", "3", "Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Refineries", "4", "Lori Jameson" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Refineries", "5", "Unknown" ),
        new EngineeringRecipe("Light Weight Scanner", "1P", "Sensor", "1", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Light Weight Scanner", "1SAll,1Mn", "Sensor", "2", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Light Weight Scanner", "1SAll,1Mn,1CCe", "Sensor", "3", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Light Weight Scanner", "1CCo,1PA,1PLA", "Sensor", "4", "Lei Cheung,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Light Weight Scanner", "1CCe,1PLA,1PRA", "Sensor", "5", "Lei Cheung,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe", "Sensor", "1", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC", "Sensor", "2", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC,1UED", "Sensor", "3", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Ge,1EA,1DED", "Sensor", "4", "Lei Cheung,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Nb,1PCa,1CED", "Sensor", "5", "Lei Cheung,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS", "Sensor", "1", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge", "Sensor", "2", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge,1CSD", "Sensor", "3", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1ME,1Nb,1DSD", "Sensor", "4", "Lei Cheung,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MC,1Sn,1CFSD", "Sensor", "5", "Lei Cheung,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Blast Resistant Shield Booster", "1Fe", "Shield Booster", "1", "Didi Vatermann,Felicity Farseer,Lei Cheung" ),
        new EngineeringRecipe("Blast Resistant Shield Booster", "1Fe,1CCo", "Shield Booster", "2", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Blast Resistant Shield Booster", "1Fe,1CCo,1FoC", "Shield Booster", "3", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Blast Resistant Shield Booster", "1Ge,1USS,1RFC", "Shield Booster", "4", "Didi Vatermann" ),
        new EngineeringRecipe("Blast Resistant Shield Booster", "1Nb,1ASPA,1EFC", "Shield Booster", "5", "Didi Vatermann" ),
        new EngineeringRecipe("Heavy Duty Shield Booster", "1GR", "Shield Booster", "1", "Didi Vatermann,Felicity Farseer,Lei Cheung" ),
        new EngineeringRecipe("Heavy Duty Shield Booster", "1DSCR,1HC", "Shield Booster", "2", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Heavy Duty Shield Booster", "1DSCR,1HC,1Nb", "Shield Booster", "3", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Heavy Duty Shield Booster", "1ISSA,1EA,1Sn", "Shield Booster", "4", "Didi Vatermann" ),
        new EngineeringRecipe("Heavy Duty Shield Booster", "1USS,1PCa,1Sb", "Shield Booster", "5", "Didi Vatermann" ),
        new EngineeringRecipe("Kinetic Resistant Shield Booster", "1Fe", "Shield Booster", "1", "Didi Vatermann,Felicity Farseer,Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shield Booster", "1GR,1Ge", "Shield Booster", "2", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shield Booster", "1SAll,1HC,1FoC", "Shield Booster", "3", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shield Booster", "1GA,1USS,1RFC", "Shield Booster", "4", "Didi Vatermann" ),
        new EngineeringRecipe("Kinetic Resistant Shield Booster", "1PA,1ASPA,1EFC", "Shield Booster", "5", "Didi Vatermann" ),
        new EngineeringRecipe("Resistance Augmented Shield Booster", "1P", "Shield Booster", "1", "Didi Vatermann,Felicity Farseer,Lei Cheung" ),
        new EngineeringRecipe("Resistance Augmented Shield Booster", "1P,1CCo", "Shield Booster", "2", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Resistance Augmented Shield Booster", "1P,1CCo,1FoC", "Shield Booster", "3", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Resistance Augmented Shield Booster", "1Mn,1CCe,1RFC", "Shield Booster", "4", "Didi Vatermann" ),
        new EngineeringRecipe("Resistance Augmented Shield Booster", "1CCe,1RFC,1IS", "Shield Booster", "5", "Didi Vatermann" ),
        new EngineeringRecipe("Thermal Resistant Shield Booster", "1Fe", "Shield Booster", "1", "Didi Vatermann,Felicity Farseer,Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shield Booster", "1HCW,1Ge", "Shield Booster", "2", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shield Booster", "1HCW,1HDP,1FoC", "Shield Booster", "3", "Didi Vatermann,Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shield Booster", "1HDP,1USS,1RFC", "Shield Booster", "4", "Didi Vatermann" ),
        new EngineeringRecipe("Thermal Resistant Shield Booster", "1HE,1ASPA,1EFC", "Shield Booster", "5", "Didi Vatermann" ),
        new EngineeringRecipe("Rapid Charge Shield Cell Bank", "1S", "Shield Cell Bank", "1", "Elvira Martuuk,Lori Jameson" ),
        new EngineeringRecipe("Rapid Charge Shield Cell Bank", "1GR,1Cr", "Shield Cell Bank", "2", "Lori Jameson" ),
        new EngineeringRecipe("Rapid Charge Shield Cell Bank", "1S,1HC,1PAll", "Shield Cell Bank", "3", "Lori Jameson" ),
        new EngineeringRecipe("Rapid Charge Shield Cell Bank", "1Cr,1EA,1ThA", "Shield Cell Bank", "4", "Unknown" ),
        new EngineeringRecipe("Specialised Shield Cell Bank", "1SLF", "Shield Cell Bank", "1", "Elvira Martuuk,Lori Jameson" ),
        new EngineeringRecipe("Specialised Shield Cell Bank", "1SLF,1CCo", "Shield Cell Bank", "2", "Lori Jameson" ),
        new EngineeringRecipe("Specialised Shield Cell Bank", "1ESED,1CCo,1CIF", "Shield Cell Bank", "3", "Lori Jameson" ),
        new EngineeringRecipe("Specialised Shield Cell Bank", "1CCo,1CIF,1Y", "Shield Cell Bank", "4", "Unknown" ),
        new EngineeringRecipe("Kinetic Resistant Shields", "1DSCR", "Shield Generator", "1", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shields", "1DSCR,1MCF", "Shield Generator", "2", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shields", "1DSCR,1MCF,1Se", "Shield Generator", "3", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shields", "1ISSA,1FoC,1Hg", "Shield Generator", "4", "Lei Cheung" ),
        new EngineeringRecipe("Kinetic Resistant Shields", "1USS,1RFC,1Ru", "Shield Generator", "5", "Lei Cheung" ),
        new EngineeringRecipe("Enhanced, Low Power Shields", "1DSCR", "Shield Generator", "1", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Enhanced, Low Power Shields", "1DSCR,1Ge", "Shield Generator", "2", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Enhanced, Low Power Shields", "1DSCR,1Ge,1PAll", "Shield Generator", "3", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Enhanced, Low Power Shields", "1ISSA,1Nb,1ThA", "Shield Generator", "4", "Lei Cheung" ),
        new EngineeringRecipe("Enhanced, Low Power Shields", "1USS,1Sn,1MGA", "Shield Generator", "5", "Lei Cheung" ),
        new EngineeringRecipe("Reinforced Shields", "1P", "Shield Generator", "1", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Reinforced Shields", "1P,1CCo", "Shield Generator", "2", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Reinforced Shields", "1P,1CCo,1MC", "Shield Generator", "3", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Reinforced Shields", "1Mn,1CCe,1CCom", "Shield Generator", "4", "Lei Cheung" ),
        new EngineeringRecipe("Reinforced Shields", "1As,1CPo,1IC", "Shield Generator", "5", "Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shields", "1DSCR", "Shield Generator", "1", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shields", "1DSCR,1Ge", "Shield Generator", "2", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shields", "1DSCR,1Ge,1Se", "Shield Generator", "3", "Didi Vatermann,Elvira Martuuk,Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shields", "1ISSA,1FoC,1Hg", "Shield Generator", "4", "Lei Cheung" ),
        new EngineeringRecipe("Thermal Resistant Shields", "1USS,1RFC,1Ru", "Shield Generator", "5", "Lei Cheung" ),
        new EngineeringRecipe("Fast Scanner", "1P", "Surface Scanner", "1", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC", "Surface Scanner", "2", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC,1OSK", "Surface Scanner", "3", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Fast Scanner", "1Mn,1FoC,1AEA", "Surface Scanner", "4", "Lei Cheung,Hera Tani,Juri Ishmaak,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Fast Scanner", "1As,1RFC,1AEC", "Surface Scanner", "5", "Lei Cheung,Hera Tani,Juri Ishmaak,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe", "Surface Scanner", "1", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC", "Surface Scanner", "2", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC,1UED", "Surface Scanner", "3", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Ge,1EA,1DED", "Surface Scanner", "4", "Lei Cheung,Hera Tani,Juri Ishmaak,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Long-Range Scanner", "1Nb,1PCa,1CED", "Surface Scanner", "5", "Lei Cheung,Hera Tani,Juri Ishmaak,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS", "Surface Scanner", "1", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge", "Surface Scanner", "2", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge,1CSD", "Surface Scanner", "3", "Felicity Farseer,Lei Cheung,Hera Tani,Juri Ishmaak,Tiana Fortune,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1ME,1Nb,1DSD", "Surface Scanner", "4", "Lei Cheung,Hera Tani,Juri Ishmaak,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MC,1Sn,1CFSD", "Surface Scanner", "5", "Lei Cheung,Hera Tani,Juri Ishmaak,Bill Turner,Lori Jameson" ),
        new EngineeringRecipe("Light Weight Mount", "1P", "Torpedo", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn", "Torpedo", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1SAll,1Mn,1CCe", "Torpedo", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1CCo,1PA,1PLA", "Torpedo", "4", "Liz Ryder" ),
        new EngineeringRecipe("Light Weight Mount", "1CCe,1PLA,1PRA", "Torpedo", "5", "Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni", "Torpedo", "1", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE", "Torpedo", "2", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Ni,1SHE,1W", "Torpedo", "3", "Juri Ishmaak,Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1Zn,1W,1Mo", "Torpedo", "4", "Liz Ryder" ),
        new EngineeringRecipe("Sturdy Mount", "1HDC,1Mo,1Tc", "Torpedo", "5", "Liz Ryder" ),
        new EngineeringRecipe("Fast Scanner", "1P", "Wake Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC", "Wake Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1P,1FFC,1OSK", "Wake Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1Mn,1FoC,1AEA", "Wake Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Fast Scanner", "1As,1RFC,1AEC", "Wake Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1P", "Wake Scanner", "1", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn", "Wake Scanner", "2", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1SAll,1Mn,1CCe", "Wake Scanner", "3", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCo,1PA,1PLA", "Wake Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Lightweight", "1CCe,1PLA,1PRA", "Wake Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe", "Wake Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC", "Wake Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Fe,1HC,1UED", "Wake Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Ge,1EA,1DED", "Wake Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Long-Range Scanner", "1Nb,1PCa,1CED", "Wake Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni", "Wake Scanner", "1", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE", "Wake Scanner", "2", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Ni,1SHE,1W", "Wake Scanner", "3", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1Zn,1W,1Mo", "Wake Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Reinforced", "1HDC,1Mo,1Tc", "Wake Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1WSE", "Wake Scanner", "1", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE", "Wake Scanner", "2", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1C,1SHE,1HDC", "Wake Scanner", "3", "Bill Turner,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1V,1SS,1FPC", "Wake Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Shielded", "1W,1CoS,1FCC", "Wake Scanner", "5", "Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS", "Wake Scanner", "1", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge", "Wake Scanner", "2", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MS,1Ge,1CSD", "Wake Scanner", "3", "Bill Turner,Juri Ishmaak,Lori Jameson,Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1ME,1Nb,1DSD", "Wake Scanner", "4", "Tiana Fortune" ),
        new EngineeringRecipe("Wide Angle Scanner", "1MC,1Sn,1CFSD", "Wake Scanner", "5", "Tiana Fortune" ),


#endregion
        };

        public static Dictionary<string, List<EngineeringRecipe>> EngineeringRecipesByMaterial =
            EngineeringRecipes.SelectMany(r => r.ingredients.Select(i => new { mat = i, recipe = r }))
                              .GroupBy(a => a.mat)
                              .ToDictionary(g => g.Key, g => g.Select(a => a.recipe).ToList());

        #region Use the netlogentry frontierdata to update this

        public static List<TechBrokerUnlockRecipe> TechBrokerUnlocks = new List<TechBrokerUnlockRecipe>()
        {
            new TechBrokerUnlockRecipe("Causticmissile Fixed Medium","16UKEC,18UKOC,16Mo,15W,6RB"),
            new TechBrokerUnlockRecipe("Flechettelauncher Fixed Medium","30Fe,24Mo,22Re,26Ge,8CMMC"),
            new TechBrokerUnlockRecipe("Flechettelauncher Turret Medium","28Fe,28Mo,20Re,24Ge,10AM"),
            new TechBrokerUnlockRecipe("Plasmashockcannon Fixed Medium","24V,26W,20Re,28Tc,6IOD"),
            new TechBrokerUnlockRecipe("Plasmashockcannon Gimbal Medium","24V,22W,20Re,28Tc,10PC"),
            new TechBrokerUnlockRecipe("Plasmashockcannon Turret Medium","24V,22W,20Re,28Tc,8PTB"),
            new TechBrokerUnlockRecipe("Corrosionproofcargorack Size 4 Class 1","16MA,26Fe,18CM,22RB,12NFI"),
            new TechBrokerUnlockRecipe("Metaalloyhullreinforcement Size 1 Class 1","16MA,15FoC,22ASPA,20CCom,12RMP"),
            new TechBrokerUnlockRecipe("Guardianpowerplant Size 2","1GMBS,18GPC,21PEOD,15HRC,10EGA"),
            new TechBrokerUnlockRecipe("Guardian Gausscannon Fixed Medium","1GWBS,18GPCe,20GTC,15Mn,6MEC"),
            new TechBrokerUnlockRecipe("Guardian Plasmalauncher Fixed Medium","1GWBS,18GPC,16GSWP,14Cr,8MWCH"),
            new TechBrokerUnlockRecipe("Guardian Plasmalauncher Turret Medium","2GWBS,21GPC,20GSWP,16Cr,8AM"),
            new TechBrokerUnlockRecipe("Guardian Shardcannon Fixed Medium","1GWBS,20GSWC,18GTC,14C,12PTB"),
            new TechBrokerUnlockRecipe("Guardian Shardcannon Turret Medium","2GWBS,16GSWC,20GTC,15C,12MCC"),
            new TechBrokerUnlockRecipe("Guardianpowerdistributor Size 1","1GMBS,20PAOD,24GPCe,18PA,6HSI"),
            new TechBrokerUnlockRecipe("Int Guardian Shield Reinforcement Size 1 Class 1","1GMBS,17GPCe,20GTC,24PDOD,8DIS"),
            new TechBrokerUnlockRecipe("Int Guardian Hull Reinforcement Size 1 Class 1","1GMBS,21GSWC,16PBOD,16PGOD,12RMP"),
            new TechBrokerUnlockRecipe("Int Guardian Module Reinforcement Size 1 Class 1","1GMBS,18GSWC,15PEOD,20GPC,9RMP"),
            new TechBrokerUnlockRecipe("Guardianfsdbooster Size 1","1GMBS,21GPCe,21GTC,24FoC,8HNSM"),
            new TechBrokerUnlockRecipe("Plasmashockcannon Fixed Large","28V,26W,24Re,26Tc,8PC"),
            new TechBrokerUnlockRecipe("Plasmashockcannon Gimbal Large","28V,24W,24Re,22Tc,12PTB"),
            new TechBrokerUnlockRecipe("Plasmashockcannon Turret Large","26V,28W,22Re,24Tc,10IOD"),
            new TechBrokerUnlockRecipe("Guardian Shardcannon Fixed Large","1GWBS,20GSWC,28GTC,20C,18MCC"),
            new TechBrokerUnlockRecipe("Guardian Shardcannon Turret Large","2GWBS,20GSWC,26GTC,28C,12MCC"),
            new TechBrokerUnlockRecipe("Guardian Plasmalauncher Fixed Large","1GWBS,28GPC,20GSWP,28Cr,10MWCH"),
            new TechBrokerUnlockRecipe("Guardian Plasmalauncher Turret Large","2GWBS,20GPC,24GSWP,26Cr,10AM"),
            new TechBrokerUnlockRecipe("Hpt Plasma Shock Cannon Fixed Small","8V,10W,8Re,12Tc,4PC"),
            new TechBrokerUnlockRecipe("Hpt Plasma Shock Cannon Gimbal Small","10V,11W,8Re,10Tc,4PTB"),
            new TechBrokerUnlockRecipe("Hpt Plasma Shock Cannon Turret Small","8V,12W,10Re,10Tc,4IOD"),
            new TechBrokerUnlockRecipe("Hpt Guardian Plasma Launcher Fixed Small","1GWBS,12GPCe,12GSWP,15GTC"),
            new TechBrokerUnlockRecipe("Hpt Guardian Plasma Launcher Turret Small","1GWBS,12GPCe,12GTC,15GSWP"),
            new TechBrokerUnlockRecipe("Hpt Guardian Shard Cannon Fixed Small","1GWBS,12GPC,12GTC,15GSWP"),
            new TechBrokerUnlockRecipe("Hpt Guardian Shard Cannon Turret Small","1GWBS,12GPC,15GTC,12GSWP"),
            new TechBrokerUnlockRecipe("Hpt Guardian Gauss Cannon Fixed Small","1GWBS,12GPC,12GSWC,15GSWP"),
            new TechBrokerUnlockRecipe("GDN Hybrid Fighter V 1","1GMVB,25GPCe,26PEOD,18PBOD,25GTC"),
            new TechBrokerUnlockRecipe("GDN Hybrid Fighter V 2","1GMVB,25GPCe,26PEOD,18GSWC,25GTC"),
            new TechBrokerUnlockRecipe("GDN Hybrid Fighter V 3","1GMVB,25GPCe,26PEOD,18GSWP,25GTC"),
        };

        #endregion
    }

    public class RecipeFilterSelector
    {
        ExtendedControls.CheckedListControlCustom cc;
        string dbstring;
        public event EventHandler Changed;

        private int reserved = 1;

        private List<string> options;

        public RecipeFilterSelector(List<string> opts)
        {
            options = opts;
        }

        public void FilterButton(string db, Control ctr, Color back, Color fore, Form parent)
        {
            FilterButton(db, ctr, back, fore, parent, options);
        }

        public void FilterButton(string db, Control ctr, Color back, Color fore, Form parent, List<string> list)
        {
            FilterButton(db, ctr.PointToScreen(new Point(0, ctr.Size.Height)), new Size(ctr.Width * 2, 400), back, fore, parent, list);
        }

        public void FilterButton(string db, Point p, Size s, Color back, Color fore, Form parent)
        {
            FilterButton(db, p, s, back, fore, parent, options);
        }

        public void FilterButton(string db, Point p, Size s, Color back, Color fore, Form parent, List<string> list)
        {
            if (cc == null)
            {
                dbstring = db;
                cc = new ExtendedControls.CheckedListControlCustom();
                cc.Items.Add("All");
                cc.Items.Add("None");

                cc.Items.AddRange(list.ToArray());

                cc.SetChecked(SQLiteDBClass.GetSettingString(dbstring, "All"));
                SetFilterSet();

                cc.FormClosed += FilterClosed;
                cc.CheckedChanged += FilterCheckChanged;
                cc.PositionSize(p, s);
                cc.SetColour(back, fore);
                cc.Show(parent);
            }
            else
                cc.Close();
        }

        private void SetFilterSet()
        {
            string list = cc.GetChecked(reserved);
            cc.SetChecked(list.Equals("All"), 0, 1);
            cc.SetChecked(list.Equals("None"), 1, 1);

        }

        private void FilterCheckChanged(Object sender, ItemCheckEventArgs e)
        {
            //Console.WriteLine("Changed " + e.Index);

            cc.SetChecked(e.NewValue == CheckState.Checked, e.Index, 1);        // force check now (its done after it) so our functions work..

            if (e.Index == 0 && e.NewValue == CheckState.Checked)
                cc.SetChecked(true, reserved);

            if (e.Index == 1 && e.NewValue == CheckState.Checked)
                cc.SetChecked(false, reserved);

            SetFilterSet();
        }

        private void FilterClosed(Object sender, FormClosedEventArgs e)
        {
            SQLiteDBClass.PutSettingString(dbstring, cc.GetChecked(2));
            cc = null;

            if (Changed != null)
                Changed(sender, e);
        }
    }
}