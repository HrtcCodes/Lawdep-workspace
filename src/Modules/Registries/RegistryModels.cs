using System;
using System.Collections.Generic;

namespace WorkSpace.Modules.Registries
{
    public class RegistryRow
    {
        public string Id { get; set; }
        public Dictionary<string, string> Values { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdatedAt { get; set; }

        public RegistryRow()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            Values = new Dictionary<string, string>();
        }
    }

    public class RegistryDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Columns { get; set; }
        public List<RegistryRow> Rows { get; set; }

        public RegistryDefinition()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            Columns = new List<string>();
            Rows = new List<RegistryRow>();
        }
    }

    public class RegistriesDatabase
    {
        public List<RegistryDefinition> Registries { get; set; }
        public RegistriesDatabase() { Registries = new List<RegistryDefinition>(); }
    }
}
