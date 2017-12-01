﻿// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ILSpy.Host.Providers;
using Microsoft.AspNetCore.Http;
using Mono.Cecil;

namespace ILSpy.Host
{
    public class DecompileTypeMiddleware : BaseMiddleware
    {
        public DecompileTypeMiddleware(RequestDelegate next, IDecompilationProvider decompilationProvider)
            : base(next, decompilationProvider)
        {
        }

        public override string EndpointName => MsilDecompilerEndpoints.DecompileType;

        public override object Handle(HttpContext httpContext)
        {
            var requestObject = JsonHelper.DeserializeRequestObject(httpContext.Request.Body)
                .ToObject<DecompileTypeRequest>();
            var code = new DecompileCode
            {
                Decompiled = _decompilationProvider.GetCode(requestObject.AssemblyPath, TokenType.TypeDef, requestObject.Rid)
            };

            return code;
        }
    }
}