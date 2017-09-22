using System.Collections.Generic;

namespace MsilDecompiler.Host
{
    public class ListTypeResponse
    {
        public IEnumerable<MemberData> Types { get; set; }
    }
}
