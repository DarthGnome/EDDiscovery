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
using System.Collections.Generic;
using System.Linq;

namespace EliteDangerousCore.JournalEvents
{
    [JournalEntryType(JournalTypeEnum.Market)]
    public class JournalMarket : JournalCommodityPricesBase, IAdditionalFiles
    {
        public JournalMarket(JObject evt) : base(evt, JournalTypeEnum.Market)
        {
            Rescan(evt);
        }

        public void Rescan(JObject evt)
        {
            Station = evt["StationName"].Str();
            StarSystem = evt["StarSystem"].Str();
            MarketID = evt["MarketID"].LongNull();
            Commodities = new List<CCommodities>(); // always made..

            JArray jcommodities = (JArray)evt["Items"];
            if (jcommodities != null )
            {
                foreach (JObject commodity in jcommodities)
                {
                    CCommodities com = new CCommodities(commodity, true);
                    Commodities.Add(com);
                }
            }
        }

        public bool Equals(JournalMarket other)
        {
            return string.Compare(Station, other.Station) == 0 && string.Compare(StarSystem, other.StarSystem) == 0 && CollectionStaticHelpers.Equals(Commodities, other.Commodities);
        }

        public bool ReadAdditionalFiles(string directory, ref JObject jo)
        {
            JObject jnew = ReadAdditionalFile(System.IO.Path.Combine(directory, "Market.json"));
            if (jnew != null)        // new json, rescan
            {
                jo = jnew;      // replace current
                Rescan(jo);
            }
            return jnew != null;
        }

    }
}
