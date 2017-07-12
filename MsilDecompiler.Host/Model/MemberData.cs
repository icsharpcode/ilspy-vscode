using Mono.Cecil;

namespace MsilDecompiler.Host
{
    public class MemberData
    {
        public string Name { get; set; }

        public MetadataToken Token { get; set; }
    }
}
