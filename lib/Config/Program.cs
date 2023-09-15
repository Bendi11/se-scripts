using System;
using VRage.Game.ModAPI.Ingame.Utilities;

public sealed class ConfigValue {
    public String Section, Name, Description;
    public ConfigValue(string section, string name, string description) {
        Section = section;
        Name = name;
        Description = description;
    }

    void ThrowEx(string type) { throw new Exception($"INI value {Section}.{Name} could not be understood as {type}"); }
    
    public void RequireDouble(MyIni ini, out double v) { if(!ini.Get(Section, Name).TryGetDouble(out v)) { ThrowEx("number"); } }
    public void RequireUint(MyIni ini, out ulong v) { if(!ini.Get(Section, Name).TryGetUInt64(out v)) { ThrowEx("whole number"); } }
    public void RequireString(MyIni ini, out string v) { if(!ini.Get(Section, Name).TryGetString(out v)) { ThrowEx("string"); } }

    public void DefaultDouble(MyIni ini, out double v, double dflt) { v = ini.Get(Section, Name).ToDouble(dflt); }
    public void DefaultUint(MyIni ini, out ulong v, ulong dflt) { v = ini.Get(Section, Name).ToUInt64(dflt); }
    public void DefaultString(MyIni ini, out string v, string dflt) { v = ini.Get(Section, Name).ToString(dflt); }
}
