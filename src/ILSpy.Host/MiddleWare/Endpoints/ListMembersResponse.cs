// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace ILSpy.Host
{
    public class ListMembersResponse
    {
        public IEnumerable<MemberData> Members { get; set; }
    }
}
