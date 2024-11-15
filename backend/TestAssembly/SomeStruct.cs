namespace TestAssembly
{
    using System;

    /// <summary>
    /// Some struct
    /// </summary>
    internal struct SomeStruct
    {
        /// <summary>
        ///  some public struct property with getter and setter.
        /// </summary>
        public int Prop { get; set; }

        public string StructMethod()
        {
            var someClass = new SomeClass();
            return someClass.ToString();
        }
    }
}
