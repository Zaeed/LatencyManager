/*  Copyright 2010 Zaeed (Matt Green)

    http://www.viridianphotos.com

    This file is part of Zaeed's Plugins for BFBC2 PRoCon.
    Zaeed's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Zaeed's Plugins for PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with Zaeed's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Xml;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents {

    using EventType = PRoCon.Core.Events.EventType;
    using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

    public class CLatencyManager : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string strHostName;
        private string strPort;
        private string strPRoConVersion;

        private string strSelectType;
        private string strCountryKickMessage;
        private string strPingKickMessage;
        private string strPingMethod;
        private string strIngameType;
        private string strGameMode;
        
		private int intPlayerLimit;
        private int intPingLimit;
		private int intAverageMinCount;
		private int intAverageAccuracy;
		private bool boolPlayerLimitReached;

        public List<string> lsSelectedCountries;
		public List<string> lsWhitelist;
		public List<string> lsCheckedPlayers;
		private List<string> lsPlayerInfo;
		private Dictionary<string, string> dicCountrys;
		private Dictionary <string, int> dicMissingPings;

		private enumBoolYesNo debugMessages;
		private enumBoolYesNo displayVersionMessage;
        private enumBoolYesNo enableCountryKick;
        private enumBoolYesNo enablePingKick;
        private enumBoolYesNo enableNoPingKick;
        private enumBoolYesNo ingameMessage;
        
		private Dictionary<string, LinkedList<int>> dicPlayerPings;
		private Dictionary<string, int> dicPingCalled;

        private string[] strCountryList = {"N/A","Asia/Pacific Region","Europe","Andorra","United Arab Emirates","Afghanistan",
			    "Antigua and Barbuda","Anguilla","Albania","Armenia","Netherlands Antilles","Angola",
			    "Antarctica","Argentina","American Samoa","Austria","Australia","Aruba","Azerbaijan",
			    "Bosnia and Herzegovina","Barbados","Bangladesh","Belgium","Burkina Faso","Bulgaria",
			    "Bahrain","Burundi","Benin","Bermuda","Brunei Darussalam","Bolivia","Brazil","Bahamas",
			    "Bhutan","Bouvet Island","Botswana","Belarus","Belize","Canada","Cocos (Keeling) Islands",
			    "Congo, The Democratic Republic of the","Central African Republic","Congo","Switzerland",
			    "Cote D'Ivoire","Cook Islands","Chile","Cameroon","China","Colombia","Costa Rica","Cuba",
			    "Cape Verde","Christmas Island","Cyprus","Czech Republic","Germany","Djibouti","Denmark",
			    "Dominica","Dominican Republic","Algeria","Ecuador","Estonia","Egypt","Western Sahara",
			    "Eritrea","Spain","Ethiopia","Finland","Fiji","Falkland Islands (Malvinas)",
			    "Micronesia, Federated States of","Faroe Islands","France","France, Metropolitan","Gabon",
			    "United Kingdom","Grenada","Georgia","French Guiana","Ghana","Gibraltar","Greenland",
			    "Gambia","Guinea","Guadeloupe","Equatorial Guinea","Greece",
			    "South Georgia and the South Sandwich Islands","Guatemala","Guam","Guinea-Bissau","Guyana",
			    "Hong Kong","Heard Island and McDonald Islands","Honduras","Croatia","Haiti","Hungary",
			    "Indonesia","Ireland","Israel","India","British Indian Ocean Territory","Iraq",
			    "Iran, Islamic Republic of","Iceland","Italy","Jamaica","Jordan","Japan","Kenya",
			    "Kyrgyzstan","Cambodia","Kiribati","Comoros","Saint Kitts and Nevis",
			    "Korea, Democratic People's Republic of","Korea, Republic of","Kuwait","Cayman Islands",
			    "Kazakstan","Lao People's Democratic Republic","Lebanon","Saint Lucia","Liechtenstein",
			    "Sri Lanka","Liberia","Lesotho","Lithuania","Luxembourg","Latvia","Libyan Arab Jamahiriya",
			    "Morocco","Monaco","Moldova, Republic of","Madagascar","Marshall Islands","Macedonia",
			    "Mali","Myanmar","Mongolia","Macau","Northern Mariana Islands","Martinique","Mauritania",
			    "Montserrat","Malta","Mauritius","Maldives","Malawi","Mexico","Malaysia","Mozambique",
			    "Namibia","New Caledonia","Niger","Norfolk Island","Nigeria","Nicaragua","Netherlands",
			    "Norway","Nepal","Nauru","Niue","New Zealand","Oman","Panama","Peru","French Polynesia",
			    "Papua New Guinea","Philippines","Pakistan","Poland","Saint Pierre and Miquelon",
			    "Pitcairn Islands","Puerto Rico","Palestinian Territory","Portugal","Palau","Paraguay",
			    "Qatar","Reunion","Romania","Russian Federation","Rwanda","Saudi Arabia",
			    "Solomon Islands","Seychelles","Sudan","Sweden","Singapore","Saint Helena","Slovenia",
			    "Svalbard and Jan Mayen","Slovakia","Sierra Leone","San Marino","Senegal","Somalia",
			    "Suriname","Sao Tome and Principe","El Salvador","Syrian Arab Republic","Swaziland",
			    "Turks and Caicos Islands","Chad","French Southern Territories","Togo","Thailand",
			    "Tajikistan","Tokelau","Turkmenistan","Tunisia","Tonga","Timor-Leste","Turkey",
			    "Trinidad and Tobago","Tuvalu","Taiwan","Tanzania, United Republic of","Ukraine","Uganda",
			    "United States Minor Outlying Islands","United States","Uruguay","Uzbekistan",
			    "Holy See (Vatican City State)","Saint Vincent and the Grenadines","Venezuela",
			    "Virgin Islands, British","Virgin Islands, U.S.","Vietnam","Vanuatu","Wallis and Futuna",
			    "Samoa","Yemen","Mayotte","Serbia","South Africa","Zambia","Montenegro","Zimbabwe",
			    "Anonymous Proxy","Satellite Provider","Other","Aland Islands","Guernsey","Isle of Man",
			    "Jersey","Saint Barthelemy","Saint Martin"
            };

        public CLatencyManager()
        {
            //Country kick
            this.strSelectType = "Allow countries";
            this.lsSelectedCountries = new List<string>();
            this.lsSelectedCountries.Add("Australia");
			this.strCountryKickMessage = "Sorry, players from %country% are not allowed.";
			this.lsCheckedPlayers = new List<string>();
            this.enableCountryKick = enumBoolYesNo.No;
			this.dicCountrys = new Dictionary<String, String>();

            //Ping kick
            this.enablePingKick = enumBoolYesNo.No;
            this.enableNoPingKick = enumBoolYesNo.No;
            this.intPingLimit = 300;
            this.strPingMethod = "Instant kick";
            this.strPingKickMessage = "High ping (%ping%)";
			this.dicPlayerPings = new Dictionary<string, LinkedList<int>>();
			this.intAverageAccuracy = 20;
			this.intAverageMinCount = 2;
			this.dicPingCalled = new Dictionary<String, int>(); 
			this.dicMissingPings = new Dictionary<string, int>();

            //General
			this.intPlayerLimit = 16;
            this.lsPlayerInfo = new List<string>();
            this.lsWhitelist = new List<string>();
            this.debugMessages = enumBoolYesNo.Yes;
			this.displayVersionMessage = enumBoolYesNo.Yes;
			this.boolPlayerLimitReached = false;
            this.ingameMessage = enumBoolYesNo.No;
            this.strIngameType = "Yell";
            this.strGameMode = "BF4";
          
        }

        public string GetPluginName() {
            return "Latency Manager";
        }

        public string GetPluginVersion() {
            return "1.0.1.12";
        }

        public string GetPluginAuthor() {
            return "Zaeed";
        }

        public string GetPluginWebsite() {
            return "www.viridianphotos.com";
        }

        public string GetPluginDescription() {
            return @"
<p>If you find my plugins useful, please feel free to donate</p>
<blockquote>
<form action=""https://www.paypal.com/cgi-bin/webscr/"" method=""POST"" target=""_blank"">
<input type=""hidden"" name=""cmd"" value=""_s-xclick"">
<input type=""hidden"" name=""encrypted"" value=""-----BEGIN PKCS7-----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-----END PKCS7-----"">
<input type=""image"" src=""https://www.paypal.com/en_AU/i/btn/btn_donate_LG.gif"" border=""0"" name=""submit"" alt=""PayPal - The safer, easier way to pay online."">
<img alt="""" border=""0"" src=""https://www.paypal.com/en_AU/i/scr/pixel.gif"" width=""1"" height=""1"">
</form>
</blockquote>


<h2>Description</h2>
<p>The Latency Manager plugin gives you two options for removing players that might be causing lag on your server.  The first option is the Country Kicking method.  This allows you to either list countries that are not allowed on your server, or specify only the countrys allowed. 

The second option is to kick based on the players Ping.  Two methods are avialable here; instant kick, and average based kick.  The averaging method samples the players ping over time, and then only kicks if their average ping is above the threshold.

</p>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
            this.strHostName = strHostName;
            this.strPort = strPort;
            Array.Sort(this.strCountryList);
            this.strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnVersion", "OnPunkbusterPlayerInfo", "OnPlayerLeft", "OnPlayerPingedByAdmin", "OnPlayerJoin", "OnListPlayers", "OnServerInfo");
        }

        public void OnPluginEnable() {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bLatency Manager ^2Enabled!");
		    this.ExecuteCommand("procon.protected.tasks.add", "CountryCheckerTask", "1", "30", "-1", "procon.protected.plugins.call", "CLatencyManager", "CountryCheck");
		    this.ExecuteCommand("procon.protected.tasks.add", "PingCheckerTask", "1", "30", "-1", "procon.protected.plugins.call", "CLatencyManager", "PingCheck");
            this.ExecuteCommand("procon.protected.send", "version");
			this.lsCheckedPlayers.Clear();
			VersionCheck();
        }

        public void OnPluginDisable() {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bLatency Manager ^1Disabled" );
			this.ExecuteCommand("procon.protected.tasks.remove", "CountryCheckerTask");
			this.ExecuteCommand("procon.protected.tasks.remove", "PingCheckerTask");
            this.ExecuteCommand("procon.protected.tasks.remove", "VersionTrackerTask");
        }

        public override void OnVersion(string serverType, string version) 
        {
            this.strGameMode = serverType;
        }

        public List<CPluginVariable> GetDisplayPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Country Functions|Enable Country based kick?", typeof(enumBoolYesNo), this.enableCountryKick));
            if (this.enableCountryKick == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Country Functions|Action", "enum.proconCVersionCheckerCountryAction(Allow countries|Disallow countries)", this.strSelectType));
                lstReturn.Add(new CPluginVariable("Country Functions|Country list", String.Format("enumCountryBuilder({0})", String.Join("|", this.strCountryList)), "N/A"));
                lstReturn.Add(new CPluginVariable("Country Functions|Selected countries", typeof(string[]), this.lsSelectedCountries.ToArray()));
                lstReturn.Add(new CPluginVariable("Country Functions|Country kick message", typeof(string), this.strCountryKickMessage));
                
            }
			lstReturn.Add(new CPluginVariable("Ping Functions|Enable high ping kick?", typeof(enumBoolYesNo), this.enablePingKick));
            if (this.enablePingKick == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Ping Functions|Ping Kick Method", "enum.proconCVersionCheckerPingAction(Average ping|Instant kick)", this.strPingMethod));
				if (this.strPingMethod.CompareTo("Average ping")==0)
				{
					lstReturn.Add(new CPluginVariable("Ping Functions|Pings to sample for average", typeof(int), this.intAverageAccuracy));
					lstReturn.Add(new CPluginVariable("Ping Functions|Minimum pings before kicking", typeof(int), this.intAverageMinCount));
				}
				lstReturn.Add(new CPluginVariable("Ping Functions|Ping limit", typeof(int), this.intPingLimit));
                lstReturn.Add(new CPluginVariable("Ping Functions|High ping kick message", typeof(string), this.strPingKickMessage));
                lstReturn.Add(new CPluginVariable("Ping Functions|Instant kick no ping?", typeof(enumBoolYesNo), this.enableNoPingKick));
            }
            
			lstReturn.Add(new CPluginVariable("Settings|Debug messages?", typeof(enumBoolYesNo), this.debugMessages));
            lstReturn.Add(new CPluginVariable("Settings|Ingame messages?", typeof(enumBoolYesNo), this.ingameMessage));
            if (this.ingameMessage == enumBoolYesNo.Yes)
            {
               lstReturn.Add(new CPluginVariable("Settings|Yell or Say ingame message", "enum.proconCVersionCheckerIngameAction(Yell|Say)", this.strIngameType));
            }
			lstReturn.Add(new CPluginVariable("Settings|Display plugin update message?", typeof(enumBoolYesNo), this.displayVersionMessage));
			lstReturn.Add(new CPluginVariable("Settings|Minimum players before rule enforced (0: none)", typeof(int), this.intPlayerLimit));
            lstReturn.Add(new CPluginVariable("Settings|Whitelist", typeof(string[]), this.lsWhitelist.ToArray()));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables() {
            
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Enable Country based kick?", typeof(enumBoolYesNo), this.enableCountryKick));
            lstReturn.Add(new CPluginVariable("Action", "enum.proconCCountrySelectorAction(Allow countries|Disallow countries)", this.strSelectType));
            lstReturn.Add(new CPluginVariable("Country list", String.Format("enumCountryBuilder({0})", String.Join("|", this.strCountryList)), "N/A"));
            lstReturn.Add(new CPluginVariable("Selected countries", typeof(string[]), this.lsSelectedCountries.ToArray()));
            lstReturn.Add(new CPluginVariable("Country kick message", typeof(string), this.strCountryKickMessage));
             
			lstReturn.Add(new CPluginVariable("Enable high ping kick?", typeof(enumBoolYesNo), this.enablePingKick));
            lstReturn.Add(new CPluginVariable("Ping Kick Method", "enum.proconCCountrySelectorPingAction(Average ping|Instant kick)", this.strPingMethod));
            lstReturn.Add(new CPluginVariable("Ping limit", typeof(int), this.intPingLimit));
            lstReturn.Add(new CPluginVariable("High ping kick message", typeof(string), this.strPingKickMessage));
            lstReturn.Add(new CPluginVariable("Pings to sample for average", typeof(int), this.intAverageAccuracy));
			lstReturn.Add(new CPluginVariable("Minimum pings before kicking", typeof(int), this.intAverageMinCount));
            lstReturn.Add(new CPluginVariable("Instant kick no ping?", typeof(enumBoolYesNo), this.enableNoPingKick));
			lstReturn.Add(new CPluginVariable("Debug messages?", typeof(enumBoolYesNo), this.debugMessages));
			lstReturn.Add(new CPluginVariable("Minimum players before rule enforced (0: none)", typeof(int), this.intPlayerLimit));
            lstReturn.Add(new CPluginVariable("Whitelist", typeof(string[]), this.lsWhitelist.ToArray()));
			lstReturn.Add(new CPluginVariable("Display plugin update message?", typeof(enumBoolYesNo), this.displayVersionMessage));

            lstReturn.Add(new CPluginVariable("Ingame messages?", typeof(enumBoolYesNo), this.ingameMessage));
            lstReturn.Add(new CPluginVariable("Yell or Say ingame message", "enum.proconCVersionCheckerIngameAction(Say|Yell)", this.strIngameType));

            return lstReturn;
        }


        public void SetPluginVariable(string strVariable, string strValue) {

			int iValue = 16; 


			//COUNTRY VARIABLES
            if (strVariable.CompareTo("Action") == 0) 
			{
                this.strSelectType = strValue;
            }
            else if (strVariable.CompareTo("Country list") == 0)
            {
                if (strValue.CompareTo("N/A") != 0)
                {
                    this.lsSelectedCountries.Add(strValue);
                }
            }
            else if (strVariable.CompareTo("Selected countries") == 0)
            {
                this.lsSelectedCountries = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("Enable Country based kick?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.enableCountryKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Country kick message") == 0) 
			{
                this.strCountryKickMessage = strValue;
            }



			// PING VARIABLES
			else if (strVariable.CompareTo("Enable high ping kick?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.enablePingKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Ping limit") == 0 && int.TryParse(strValue, out iValue) == true)
            {
                this.intPingLimit = iValue;
            }
			else if (strVariable.CompareTo("High ping kick message") == 0) 
			{
                this.strPingKickMessage = strValue;
            }
			else if (strVariable.CompareTo("Ping Kick Method") == 0) 
			{
                this.strPingMethod = strValue;
            }
            else if (strVariable.CompareTo("Instant kick no ping?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.enableNoPingKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
             
			else if (strVariable.CompareTo("Pings to sample for average") == 0 && int.TryParse(strValue, out iValue) == true)
			{
                if (iValue < this.intAverageMinCount)
                {
                    this.intAverageMinCount = iValue;
                }
                this.intAverageAccuracy = iValue;
			}
			else if (strVariable.CompareTo("Minimum pings before kicking") == 0 && int.TryParse(strValue, out iValue) == true)
			{
			
                if (iValue > this.intAverageAccuracy) 
                { 
                    this.intAverageMinCount = this.intAverageAccuracy; 
                }
				else
				{
					this.intAverageMinCount = iValue;
				}
			}

			// GENERAL VARIABLES
			else if (strVariable.CompareTo("Minimum players before rule enforced (0: none)") == 0 && int.TryParse(strValue, out iValue) == true)
            {
                this.intPlayerLimit = iValue;
            }
			else if (strVariable.CompareTo("Whitelist") == 0)
            {
                this.lsWhitelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("Debug messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.debugMessages = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Display plugin update message?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.displayVersionMessage = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
				if(this.displayVersionMessage == enumBoolYesNo.No)
				{
					this.ExecuteCommand("procon.protected.tasks.remove", "VersionTrackerTask");
				}
            }
            else if (strVariable.CompareTo("Yell or Say ingame message") == 0)
            {
                this.strIngameType = strValue;
            }
            else if (strVariable.CompareTo("Ingame messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.ingameMessage = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
        }

	    public override void OnServerInfo(CServerInfo serverInfo) 
		{
			if (serverInfo.PlayerCount >= this.intPlayerLimit)
			{
				this.boolPlayerLimitReached = true;
			}
			else
			{
				this.boolPlayerLimitReached = false;
			}
		}

		public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
		{
			if (this.lsCheckedPlayers.Contains(cpiPlayer.SoldierName))
            {
                this.lsCheckedPlayers.Remove(cpiPlayer.SoldierName);
                
            }
			if (this.dicPingCalled.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.dicPingCalled.Remove(cpiPlayer.SoldierName);
            }
            if (this.lsPlayerInfo.Contains(cpiPlayer.SoldierName) == true)
            {
                this.lsPlayerInfo.Remove(cpiPlayer.SoldierName);
            }
			if (this.dicCountrys.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.dicCountrys.Remove(cpiPlayer.SoldierName);
            }
            if (this.dicPlayerPings.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.dicPlayerPings.Remove(cpiPlayer.SoldierName);
            }
		}

		public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {			
            if ((lstPlayers != null))
			{
				foreach(CPlayerInfo playerInfo in lstPlayers)
				{
					if (this.lsPlayerInfo.Contains(playerInfo.SoldierName) == false)
                    {
                        this.lsPlayerInfo.Add(playerInfo.SoldierName);
                    }

                    if ((this.enableNoPingKick == enumBoolYesNo.Yes) && !this.strGameMode.Equals("BF3") && ((playerInfo.Ping <= 0) || (playerInfo.Ping > 10000)))
		            {
						if(this.dicMissingPings.ContainsKey(playerInfo.SoldierName))
						{
                            int missCounts = this.dicMissingPings[playerInfo.SoldierName];
	                	   	if(missCounts >= 3)
	                	   	{
	                	   		Kick(playerInfo.SoldierName, "Missing ping", "", "");
	                	   		this.dicMissingPings.Remove(playerInfo.SoldierName);
	                	   	}
	                	   	else
	                	   	{
                                this.dicMissingPings[playerInfo.SoldierName] = missCounts + 1;
	                	   	}
						}
						else
						{
							this.dicMissingPings.Add(playerInfo.SoldierName, 1);
						}
		                
		            }
					if(!this.strGameMode.Equals("BF3"))
					{
					this.ExecuteCommand("procon.protected.tasks.add", playerInfo.SoldierName + "PingTask", "5", "1", "1", "procon.protected.plugins.call", "CLatencyManager", "PingAdd", playerInfo.SoldierName, playerInfo.Ping.ToString());
					}
				}
                List<string> orphanPlayers = new List<string>();
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    orphanPlayers.Add(cpiPlayer.SoldierName);  //Convert lstPlayers into a list of strings cause i'm lazy
                }
                List<string> orphans = lsPlayerInfo.Except(orphanPlayers).ToList();
				foreach(String player in orphans) lsPlayerInfo.Remove(player);
			}
           // this.ExecuteCommand("procon.protected.tasks.remove", "PingCheckerTask");
            
            if (lstPlayers.Count == 0)
            {
            	this.lsPlayerInfo.Clear();
            	this.dicPlayerPings.Clear();
            	this.dicPingCalled.Clear();
            	this.lsCheckedPlayers.Clear();
            }
        }

		public override void OnPlayerPingedByAdmin(string soldierName, int ping) 
		{
            if ((this.enableNoPingKick == enumBoolYesNo.Yes) && ((ping <= 0) || (ping > 10000)))
            {
                if (this.dicMissingPings.ContainsKey(soldierName))
						{
                            int missCounts = this.dicMissingPings[soldierName];
	                	   	if(missCounts >= 3)
	                	   	{
                                Kick(soldierName, "Missing ping", "", "");
                                this.dicMissingPings.Remove(soldierName);
	                	   	}
	                	   	else
	                	   	{
                                this.dicMissingPings[soldierName] = missCounts+1;
	                	   	}
						}
						else
						{
                            this.dicMissingPings.Add(soldierName, 1);
						}
            }
            this.ExecuteCommand("procon.protected.tasks.add", soldierName + "PingTask", "5", "1", "1", "procon.protected.plugins.call", "CLatencyManager", "PingAdd", soldierName, ping.ToString());
		}

        public override void OnPlayerJoin(string strSoldierName)
        {
            if (this.lsPlayerInfo.Contains(strSoldierName) == false)
            {
                this.lsPlayerInfo.Add(strSoldierName);
            }
        }

        public void PingAdd(string strSoldierName, string ping)
        {
            if (this.lsPlayerInfo.Contains(strSoldierName) == true)
            {
            	
                if (this.dicPingCalled.ContainsKey(strSoldierName))
                {
                    this.dicPingCalled[strSoldierName] = int.Parse(ping);
                }
                else
                {
                    this.dicPingCalled.Add(strSoldierName, int.Parse(ping));
                }
            }
            else
            {
                if (this.dicPingCalled.ContainsKey(strSoldierName))
                {
                    this.dicPingCalled.Remove(strSoldierName);
                }
            }
        }

        public void PingCheck()
        {
        	if (this.enablePingKick == enumBoolYesNo.Yes)
        	{
	            List<string> remove = new List<string>();
	            foreach (KeyValuePair<string, int> latestPings in this.dicPingCalled)
	            {
	                bool playerKicked = false;
	                if ((this.lsPlayerInfo.Contains(latestPings.Key) == true) && (latestPings.Value > 0) && (latestPings.Value < 10000) && (!this.lsWhitelist.Contains(latestPings.Key)))
	                {
	                    if (this.strPingMethod.CompareTo("Average ping") == 0)
	                    {
	                        if (this.dicPlayerPings.ContainsKey(latestPings.Key) == true)
	                        {
	                            LinkedList<int> playerPings = this.dicPlayerPings[latestPings.Key];
	
	                            if (!playerPings.Last.Equals(latestPings.Value))
	                            {
									playerPings.AddLast(latestPings.Value);
	
									if (playerPings.Count >= this.intAverageMinCount)
									{
										int totalPing = 0;
										foreach (int pings in playerPings)
										{
											totalPing += pings;
										}
										if (((totalPing / playerPings.Count) > this.intPingLimit) && (this.boolPlayerLimitReached))
										{
											 float ping = (totalPing / playerPings.Count);
	                                         Kick(latestPings.Key, this.strPingKickMessage, "", ping.ToString());
										//	 DebugMessage(String.Format("^8High average ping kick {0} ({1})", latestPings.Key, (totalPing / playerPings.Count)));
	                                         this.dicPlayerPings.Remove(latestPings.Key);
	                                         remove.Add(latestPings.Key);
	                                         playerKicked = true;
										}
									}
	                                if (!playerKicked)
	                                {
	                                    if (playerPings.Count >= this.intAverageAccuracy)
	                                    {
	                                        playerPings.RemoveFirst();
	                                    }
	
	                                    this.dicPlayerPings[latestPings.Key] = playerPings;
	                                }
	                            }
	                        }
	                        else
	                        {
	                            LinkedList<int> newPing = new LinkedList<int>();
	                            newPing.AddLast(latestPings.Value);
	                            this.dicPlayerPings.Add(latestPings.Key, newPing);
	                        }
	                    }
	                    else 
	                    {
	                        if ((this.intPingLimit < latestPings.Value) && (this.boolPlayerLimitReached))
	                        { 
								string ping = latestPings.Value.ToString();
								Kick(latestPings.Key, this.strPingKickMessage, "", ping);
								remove.Add(latestPings.Key);
	                          //  DebugMessage(String.Format("^8Instant kick for high ping {0} ({1})", latestPings.Key, latestPings.Value));
	                        }
	                    }
	                }
	            }
	            foreach (string player in remove)
	            {
	                if (this.dicPingCalled.ContainsKey(player))
	                {
	                    this.dicPingCalled.Remove(player);
	                }
	            }
        	}
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
			if (cpbiPlayer != null)
            {
                if (this.dicCountrys.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.dicCountrys.Add(cpbiPlayer.SoldierName, cpbiPlayer.PlayerCountry);
                }
                else
                {
                    this.dicCountrys[cpbiPlayer.SoldierName] = cpbiPlayer.PlayerCountry;
                }
			}
        }

		public void CountryCheck()
		{
			if (this.enableCountryKick == enumBoolYesNo.Yes)
			{
				foreach (KeyValuePair<string, string> cpbiPlayer in this.dicCountrys)
				{
					if ( (this.lsSelectedCountries == null) || (this.lsSelectedCountries.Count == 0) || (this.lsWhitelist.Contains(cpbiPlayer.Key)) || (this.lsCheckedPlayers.Contains(cpbiPlayer.Key)))
					{
						//Do nothing.
					}
					else if (((String.Compare(this.strSelectType, "Allow countries", true)==0) && !(this.lsSelectedCountries.Contains(cpbiPlayer.Value))) || ((String.Compare(this.strSelectType, "Disallow countries", true)==0) && (this.lsSelectedCountries.Contains(cpbiPlayer.Value))))
					{
						if (this.boolPlayerLimitReached)
						{
							Kick(cpbiPlayer.Key, this.strCountryKickMessage, cpbiPlayer.Value, "");
						//	DebugMessage(String.Format("^8Kicking {0} ({1})", cpbiPlayer.Key, cpbiPlayer.Value));
							
						}
					}
					else
					{
						this.lsCheckedPlayers.Add(cpbiPlayer.Key);
					}	

				}			  
			}
			this.dicCountrys.Clear();
		}

        public void Kick(string strSoldierName, string strMessage, string strCountry, string strPing)
		{
            if (this.lsPlayerInfo.Contains(strSoldierName) == true)
            {
                
                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSoldierName, strMessage.Replace("%ping%", strPing).Replace("%country%", strCountry).Replace("%player%", strSoldierName));
                DebugMessage(String.Format("^8Kicking {0} : {1}", strSoldierName, strMessage.Replace("%ping%", strPing).Replace("%country%", strCountry).Replace("%player%", strSoldierName)));

                if (this.ingameMessage == enumBoolYesNo.Yes)
                {
                    switch (this.strIngameType)
                    {
                        case "Yell":
                    		this.ExecuteCommand("procon.protected.send", "admin.yell", String.Format("Kicking {0}. Reason: {1}", strSoldierName, strMessage.Replace("%ping%", strPing).Replace("%country%", strCountry).Replace("%player%", strSoldierName)), "4", "all");
                            break;
                        case "Say":
                            this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Kicking {0}. Reason: {1}", strSoldierName, strMessage.Replace("%ping%", strPing).Replace("%country%", strCountry).Replace("%player%", strSoldierName)), "all");
                            break;
                        default:
                            break;
                    }
                }
            }
		}

		public void DebugMessage(string strMessage)
		{
			if(this.debugMessages == enumBoolYesNo.Yes)
			{
				this.ExecuteCommand("procon.protected.pluginconsole.write", "^bLatency Manager: " + strMessage);
			}
		}

		public void VersionCheck()
		{
			if (this.displayVersionMessage == enumBoolYesNo.Yes)
			{
				try
				{
					XmlDocument xml = new XmlDocument();
					xml.Load("http://www.viridianphotos.com/VersionControl.xml");
					XmlNodeList xList = xml.SelectNodes("//plugin");
					foreach (XmlNode node in xList)
					{
						if((node.SelectSingleNode(".//title").InnerText.Equals(GetPluginName())) && (this.displayVersionMessage == enumBoolYesNo.Yes) && (!node.SelectSingleNode(".//version").InnerText.Equals(GetPluginVersion())))
						{
							this.ExecuteCommand("procon.protected.tasks.add", "VersionTrackerTask", "1", "300", "-1", "procon.protected.chat.write", String.Format("^b^6 You are running version {0} of the Latency Manager plugin.  Version {1} is now available.  Visit https://forum.myrcon.com/ for more information", GetPluginVersion(), node.SelectSingleNode(".//version").InnerText));
						}
					}
				}
				catch (Exception)
				{
					this.displayVersionMessage = enumBoolYesNo.No;
                    this.ExecuteCommand("procon.protected.tasks.remove", "VersionTrackerTask");
				}
			}
		}

		
    }

}
