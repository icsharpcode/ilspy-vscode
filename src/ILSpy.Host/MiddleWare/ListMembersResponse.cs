using System.Collections.Generic;

namespace MsilDecompiler.Host
{
    public class ListMembersResponse
    {
        public IEnumerable<MemberData> Members { get; set; }
    }
}
