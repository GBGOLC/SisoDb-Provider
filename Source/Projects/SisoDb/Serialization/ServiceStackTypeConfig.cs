using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PineCone.Structures.Schemas;
using ServiceStack.Text;

namespace SisoDb.Serialization
{
    internal static class ServiceStackTypeConfig<T>
    {
        private static readonly IStructureTypeReflecter StructureTypeReflecter;

        private static readonly Type ItemType;

        private static readonly Type TypeConfigType;

        private static readonly ConcurrentDictionary<Type, object> ConfigChildSync;

        static ServiceStackTypeConfig()
        {
            ConfigChildSync = new ConcurrentDictionary<Type, object>();

            StructureTypeReflecter = new StructureTypeReflecter();
            ItemType = typeof(T);
            TypeConfigType = typeof(TypeConfig<>);

            TypeConfig<T>.Properties = ExcludePropertiesThatHoldStructures(TypeConfig<T>.Properties);
        }

        internal static void Config(Type type)
        {
            var propertiesAllreadyExcludedInStaticCtor = type == ItemType;
            if (propertiesAllreadyExcludedInStaticCtor)
                return;

            if (ConfigChildSync.ContainsKey(type))
                return;

            if (ConfigChildSync.TryAdd(type, new object()))
                ConfigureTypeConfigToExcludeReferencedStructures(type);
        }

        private static void ConfigureTypeConfigToExcludeReferencedStructures(Type type)
        {
            var propertiesAllreadyExcludedInStaticCtor = type == ItemType;
            if (propertiesAllreadyExcludedInStaticCtor)
                return;

            var cfg = TypeConfigType.MakeGenericType(type);
            var propertiesProperty = cfg.GetProperty("Properties", BindingFlags.Static | BindingFlags.Public);
            propertiesProperty.SetValue(
                null,
                ExcludePropertiesThatHoldStructures((PropertyInfo[])propertiesProperty.GetValue(null, new object[] { })),
                new object[] { });
        }

        private static PropertyInfo[] ExcludePropertiesThatHoldStructures(IEnumerable<PropertyInfo> properties)
        {
            return properties.Where(p => !StructureTypeReflecter.HasIdProperty(p.PropertyType)).ToArray();
        }
    }
}