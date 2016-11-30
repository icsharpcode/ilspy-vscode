using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MsilDecompiler.WebApi.Providers
{
    public enum NodeKind
    {
        Assembly,
        Reference,
        AssemblyReference,
        ModuleReference,
        Namespace,
        Type,
        BaseTypes,
        BaseTypesEntry,
        DerivedTypes,
        DerivedTypesEntry,
        Event,
        Field,
        Method,
        Property,
        Resources
    }
}
