using System.Collections.Generic;

namespace MsilDecompiler.Host
{
    public class ListTypesResponse
    {
        public IEnumerable<MemberData> Types { get; set; }
    }
}
