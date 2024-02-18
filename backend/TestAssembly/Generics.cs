namespace Generics
{
    public class ClassGeneric<T> { }

    public class ClassGeneric<T1, T2> { }

    public interface IGeneric<T> { }

    public interface IGeneric<T1, T2> { }

    public interface IGeneric<T1, T2, T3> { }

    public class AClass
    {
        public void M<T>() { }

        public void M<T1, T2>() { }

        public class NestedClass<T> { }

        public class NestedClass<T1, T2> { }
    }
}