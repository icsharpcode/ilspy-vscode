using Mono.Cecil;

namespace ILSpy.Host.Providers
{
    public static class CecilExtensions
    {
        public static MemberSubKind GetMemberSubKind(this TypeDefinition t)
        {
            if (t.IsInterface)
            {
                return MemberSubKind.Interface;
            }
            else if (t.IsEnum)
            {
                return MemberSubKind.Enum;
            }
            else if (t.IsClass && t.IsValueType)
            {
                return MemberSubKind.Structure;
            }
            else if (t.IsClass)
            {
                return MemberSubKind.Class;
            }

            return MemberSubKind.None;
        }


        public static string GetPlatformDisplayName(this ModuleDefinition module)
        {
            switch (module.Architecture)
            {
                case TargetArchitecture.I386:
                    if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
                        return "AnyCPU (32-bit preferred)";
                    else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
                        return "x86";
                    else
                        return "AnyCPU (64-bit preferred)";
                case TargetArchitecture.AMD64:
                    return "x64";
                case TargetArchitecture.IA64:
                    return "Itanium";
                default:
                    return module.Architecture.ToString();
            }
        }

        public static string GetPlatformName(this ModuleDefinition module)
        {
            switch (module.Architecture)
            {
                case TargetArchitecture.I386:
                    if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
                        return "AnyCPU";
                    else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
                        return "x86";
                    else
                        return "AnyCPU";
                case TargetArchitecture.AMD64:
                    return "x64";
                case TargetArchitecture.IA64:
                    return "Itanium";
                default:
                    return module.Architecture.ToString();
            }
        }

        public static string GetRuntimeDisplayName(this ModuleDefinition module)
        {
            switch (module.Runtime)
            {
                case TargetRuntime.Net_1_0:
                    return ".NET 1.0";
                case TargetRuntime.Net_1_1:
                    return ".NET 1.1";
                case TargetRuntime.Net_2_0:
                    return ".NET 2.0";
                case TargetRuntime.Net_4_0:
                    return ".NET 4.0";
            }
            return null;
        }
    }
}
