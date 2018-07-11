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
    [JournalEntryType(JournalTypeEnum.ModuleSell)]
    public class JournalModuleSell : JournalEntry, ILedgerJournalEntry, IShipInformation
    {
        public JournalModuleSell(JObject evt ) : base(evt, JournalTypeEnum.ModuleSell)
        {
            SlotFD = JournalFieldNaming.NormaliseFDSlotName(evt["Slot"].Str());
            Slot = JournalFieldNaming.GetBetterSlotName(SlotFD);

            SellItemFD = JournalFieldNaming.NormaliseFDItemName(evt["SellItem"].Str());
            SellItem = JournalFieldNaming.GetBetterItemName(SellItemFD);
            SellItemLocalised = JournalFieldNaming.CheckLocalisation(evt["SellItem_Localised"].Str(),SellItem);

            SellPrice = evt["SellPrice"].Long();

            ShipFD = JournalFieldNaming.NormaliseFDShipName(evt["Ship"].Str());
            Ship = JournalFieldNaming.GetBetterShipName(ShipFD);
            ShipId = evt["ShipID"].Int();

            MarketID = evt["MarketID"].LongNull();
        }

        public string Slot { get; set; }
        public string SlotFD { get; set; }
        public string SellItem { get; set; }
        public string SellItemFD { get; set; }
        public string SellItemLocalised { get; set; }
        public long SellPrice { get; set; }
        public string Ship { get; set; }
        public string ShipFD { get; set; }
        public int ShipId { get; set; }
        public long? MarketID { get; set; }

        public void Ledger(Ledger mcl, DB.SQLiteConnectionUser conn)
        {
            string s = (SellItemLocalised.Length > 0) ? SellItemLocalised : SellItem;

            mcl.AddEvent(Id, EventTimeUTC, EventTypeID, s + " on " + Ship, SellPrice);
        }

        public void ShipInformation(ShipInformationList shp, string whereami, ISystem system, DB.SQLiteConnectionUser conn)
        {
            shp.ModuleSell(this);
        }
        public override void FillInformation(out string info, out string detailed) 
        {
            
            info = BaseUtils.FieldBuilder.Build("", SellItemLocalised, "< from ".Txb(this), Slot, "Price:; cr;N0".Txb(this), SellPrice);
            detailed = "";
        }

    }
}
