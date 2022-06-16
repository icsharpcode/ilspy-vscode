﻿using System;

namespace TestAssembly
{
    /// <summary>
    /// Some class
    /// </summary>
    public class SomeClass
    {
        /// <summary>
        /// some private class field
        /// </summary>
        private int _ProgId;

        /// <summary>
        /// Some public class propertywith getter and setter
        /// </summary>
        public int ProgId {
            get {
                return _ProgId;
            }
            set {
                _ProgId = value;
            }
        }

        static SomeClass() { }

        public SomeClass() { }

        /// <summary>
        /// Some class ctor
        /// </summary>
        /// <param name="ProgramId"></param>
        public SomeClass(int ProgramId)
        {
            ProgId = ProgramId;
        }

        public class NestedC
        {
            public void M() { }
        }
    }
}