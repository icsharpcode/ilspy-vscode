// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Host.Configuration
{
    public class ConsoleArgs
    {
        public ConsoleArgs(string[] args)
        {
            Args = args;
        }

        public string[] Args { get; }
    }
}
