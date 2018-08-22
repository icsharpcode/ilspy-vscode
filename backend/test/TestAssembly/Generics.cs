namespace Generics
{
    public class C<T> { }

    public class C<T1, T2> { }

    public interface I<T> { }

    public interface I<T1, T2> { }

    public interface I<T1, T2, T3> { }

    public class A
    {
        public void M<T>() { }

        public void M<T1, T2>() { }

        public class NestedC<T> { }

        public class NestedC<T1, T2> { }
    }
}