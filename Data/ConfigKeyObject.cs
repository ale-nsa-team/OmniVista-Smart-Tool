namespace PoEWizard.Data
{
    public class ConfigKeyObject
    {
        public string Key { get; set; }
        public object Object { get; set; }
        public ConfigKeyObject(string key, object obj)
        {
            Key = key;
            Object = obj;
        }
    }
}
