namespace MsilDecompiler.Host
{
    public class DecompileMemberRequest
    {
        public uint TypeRid { get; set; }
        public uint MemberType { get; set; }
        public uint MemberRid { get; set; }
    }
}
