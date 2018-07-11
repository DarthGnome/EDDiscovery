﻿/*
 * Copyright © 2016-2018 EDDiscovery development team
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
    [JournalEntryType(JournalTypeEnum.MassModuleStore)]
    public class JournalMassModuleStore : JournalEntry, IShipInformation
    {
        public JournalMassModuleStore(JObject evt) : base(evt, JournalTypeEnum.MassModuleStore)
        {
            ShipFD = JournalFieldNaming.NormaliseFDShipName(evt["Ship"].Str());
            Ship = JournalFieldNaming.GetBetterShipName(ShipFD);
            ShipId = evt["ShipID"].Int();
            ModuleItems = evt["Items"]?.ToObjectProtected<ModuleItem[]>();
            MarketID = evt["MarketID"].LongNull();

            if ( ModuleItems != null )
            {
                foreach (ModuleItem i in ModuleItems)
                {
                    i.SlotFD = JournalFieldNaming.NormaliseFDSlotName(i.Slot);
                    i.Slot = JournalFieldNaming.GetBetterSlotName(i.SlotFD);
                    i.NameFD = JournalFieldNaming.NormaliseFDItemName(i.Name);
                    i.Name = JournalFieldNaming.GetBetterItemName(i.NameFD);
                }
            }
        }

        public string Ship { get; set; }
        public string ShipFD { get; set; }
        public int ShipId { get; set; }
        public long? MarketID { get; set; }

        public ModuleItem[] ModuleItems { get; set; }

        public void ShipInformation(ShipInformationList shp, string whereami, ISystem system, DB.SQLiteConnectionUser conn)
        {
            shp.MassModuleStore(this);
        }

        public override void FillInformation(out string info, out string detailed) 
        {
            info = BaseUtils.FieldBuilder.Build("Total modules:".Txb(this), ModuleItems?.Count());
            detailed = "";

            if ( ModuleItems != null )
                foreach (ModuleItem m in ModuleItems)
                {
                    detailed = detailed.AppendPrePad(BaseUtils.FieldBuilder.Build("", m.Name, ";(Hot)".Txb(this), m.Hot), ", ");
                }
        }

        public class ModuleItem
        {
            public string SlotFD;
            public string Slot;
            public string NameFD;
            public string Name;
            public string EngineerModifications;
            public double? Quality { get; set; }
            public int? Level { get; set; }
            public bool? Hot { get; set; }
        }
    }
}
