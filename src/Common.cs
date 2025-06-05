using BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyo.Fr.EmuNWA
{
    public static class Common
    {
        private class TranslationClass
        {
            public String bizhawkDomainName = "";
            public String nwaDomainName = "";
        }
        static private Dictionary<String, Dictionary<String, List<TranslationClass>>> domainTranslations = new()
        { { "SNES", new() {
                    {"BSNESv115+", new() {
                                     new() { bizhawkDomainName = "CARTRIDGE_ROM", nwaDomainName = "CARTROM"},
                                     new() { bizhawkDomainName = "CARTRIDGE_RAM", nwaDomainName = "SRAM"}
                                   } },
                    {"Faust",      new() {
                                     new() { bizhawkDomainName = "OAMLO", nwaDomainName = "OAM"}
                                   } },
                    {"NOCORE",     new() {
                                     new() {bizhawkDomainName = "CARTRAM", nwaDomainName = "SRAM"}
                                   } },
                    { "SubBSNESv115+", new() {
                                     new() {bizhawkDomainName = "OBJECTS", nwaDomainName = ""}
                                   } }
                    }
          }
        };

        public static string domainToNWAName(string bizhawkName, string system, string coreName)
        {
            if (bizhawkName == "Waterbox PageData")
                return "";

            if (domainTranslations.ContainsKey(system))
            {
                var systemDefs = domainTranslations[system][coreName];
                foreach (var def in systemDefs)
                {
                    if (def.bizhawkDomainName == bizhawkName)
                        return def.nwaDomainName;
                }
                if (domainTranslations[system]["NOCORE"] != null)
                {
                    var nocoreDefs = domainTranslations[system]["NOCORE"];
                    foreach (var def in nocoreDefs)
                    {
                        if (def.bizhawkDomainName == bizhawkName)
                            return def.nwaDomainName;
                    }
                }
            }
            return bizhawkName;
        }
        public static string NWANameToDomain(string nwaName, string system, string coreName)
        {
            if (domainTranslations.ContainsKey(system))
            {
                var systemDefs = domainTranslations[system][coreName];
                foreach (var def in systemDefs)
                {
                    if (def.nwaDomainName == nwaName)
                        return def.bizhawkDomainName;
                }
                if (domainTranslations[system]["NOCORE"] != null)
                {
                    var nocoreDefs = domainTranslations[system][coreName];
                    foreach (var def in nocoreDefs)
                    {
                        if (def.nwaDomainName == nwaName)
                            return def.bizhawkDomainName;
                    }
                }
            }
            return nwaName;
        }
        public static int NWANumber(String intString)
        {
            if (intString[0] == '$')
            {
                return int.Parse(intString.Substring(1), System.Globalization.NumberStyles.HexNumber);
            }
            return int.Parse(intString);
        }
    }
}
