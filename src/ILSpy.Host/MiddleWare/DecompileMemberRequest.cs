// See the LICENSE file in the project root for more information.

namespace MsilDecompiler.Host
{
    public class DecompileMemberRequest : RequestBase
    {
        public uint TypeRid { get; set; }
        public uint MemberType { get; set; }
        public uint MemberRid { get; set; }
    }
}
