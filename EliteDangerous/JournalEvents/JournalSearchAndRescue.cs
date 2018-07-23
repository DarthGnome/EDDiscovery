﻿/*
 * Copyright © 2017 EDDiscovery development team
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
using Newtonsoft.Json.Linq;
using System.Linq;

namespace EliteDangerousCore.JournalEvents
{
    [JournalEntryType(JournalTypeEnum.SearchAndRescue)]
    public class JournalSearchAndRescue : JournalEntry
    {
        public JournalSearchAndRescue(JObject evt) : base(evt, JournalTypeEnum.SearchAndRescue)
        {
            Name = evt["Name"].Str();
            Name = JournalFieldNaming.FDNameTranslation(Name); // some premangling
            FriendlyName = MaterialCommodityData.GetNameByFDName(Name);
            Count = evt["Count"].Int();
            Reward = evt["Reward"].Long();
            MarketID = evt["MarketID"].LongNull();
        }

        public string Name { get; set; }            // Hyperspace, Supercruise
        public string FriendlyName { get; set; }            // Hyperspace, Supercruise
        public int Count { get; set; }
        public long Reward { get; set; }
        public long? MarketID { get; set; }

        public override void FillInformation(out string info, out string detailed) 
        {
            info = BaseUtils.FieldBuilder.Build("",FriendlyName , "Num:".Txb(this), Count, "Reward:".Tx(this), Reward);
            detailed = "";
        }
    }
}
