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
    [JournalEntryType(JournalTypeEnum.Shipyard)]
    public class JournalShipyard : JournalEntry, IAdditionalFiles
    {
        public JournalShipyard(JObject evt) : base(evt, JournalTypeEnum.Shipyard)
        {
            Rescan(evt);
        }

        public void Rescan(JObject evt)
        {
            Yard = new ShipYard(evt["StationName"].Str(), evt["StarSystem"].Str(), EventTimeUTC, evt["PriceList"]?.ToObjectProtected<ShipYard.ShipyardItem[]>());
            MarketID = evt["MarketID"].LongNull();
            Horizons = evt["Horizons"].BoolNull();
            AllowCobraMkIV = evt["AllowCobraMkIV"].BoolNull();
        }

        public bool ReadAdditionalFiles(string directory, ref JObject jo)
        {
            JObject jnew = ReadAdditionalFile(System.IO.Path.Combine(directory, "Shipyard.json"));
            if (jnew != null)        // new json, rescan
            {
                jo = jnew;      // replace current
                Rescan(jo);
            }
            return jnew != null;
        }

        public ShipYard Yard { get; set; }
        public long? MarketID { get; set; }
        public bool? Horizons { get; set; }
        public bool? AllowCobraMkIV { get; set; }

        public override void FillInformation(out string info, out string detailed) 
        {
            info = "";
            detailed = "";

            if (Yard.Ships != null)
            {
                if (Yard.Ships.Length < 5)
                {
                    foreach (ShipYard.ShipyardItem m in Yard.Ships)
                        info = info.AppendPrePad(m.ShipType_Localised.Alt(m.ShipType), ", ");
                }
                else
                    info = Yard.Ships.Length.ToString() + " " + "Ships".Txb(this);

                foreach (ShipYard.ShipyardItem m in Yard.Ships)
                {
                    detailed = detailed.AppendPrePad(BaseUtils.FieldBuilder.Build("",m.ShipType_Localised.Alt(m.ShipType), "; cr;N0", m.ShipPrice), System.Environment.NewLine);
                }
            }
        }

    }
}
