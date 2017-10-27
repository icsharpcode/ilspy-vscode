using System;

namespace TestAssembly
{
    /// <summary>
    /// Some class
    /// </summary>
    public class C
    {
        /// <summary>
        /// some private class field
        /// </summary>
        private int _ProgId;

        /// <summary>
        /// Some public class propertywith getter and setter
        /// </summary>
        public int ProgId
        {
            get
            {
                return _ProgId;
            }
            set
            {
                _ProgId = value;
            }
        }

        /// <summary>
        /// Some class ctor
        /// </summary>
        /// <param name="ProgramId"></param>
        public C(int ProgramId)
        {
            ProgId = ProgramId;
        }

        public class NestedC
        {
            public void M() { }
        }
    }
}
