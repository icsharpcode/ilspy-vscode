namespace A
{
    public class A1
    {
    }

    public class A2
    {
    }

    public class A3
    {
    }

    namespace  B
    {
        public class AB1
        {
        }

        public class AB2
        {
        }

        namespace C
        {
            public class ABC1
            {
            }

            public class ABC2
            {
            }
        }
    }
}

namespace A.B
{
    class AB3 { }
    class AB4 { }
}


namespace A.B.C
{
    class ABC3 { }
    class ABC4 { }

    namespace D
    {
        class ABCD1 { }
    }
}

namespace A.B.C.D
{
    class ABCD2 { }
}

