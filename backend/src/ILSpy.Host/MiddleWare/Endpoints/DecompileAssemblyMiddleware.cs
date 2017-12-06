// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class DecompileAssemblyMiddleware : BaseMiddleware
    {
        public DecompileAssemblyMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.DecompileAssembly;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<DecompileAssemblyRequest>();
            var code = new DecompileCode { Decompiled = _decompilationProvider.GetCode(requestObject.AssemblyPath, TokenType.Assembly, 0) };
            return code;
        }
    }
}
