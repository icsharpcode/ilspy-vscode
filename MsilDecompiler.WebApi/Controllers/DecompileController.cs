using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using MsilDecompiler.WebApi.Providers;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace MsilDecompiler.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class DecompileController : Controller
    {
        private ILogger _logger;
        private readonly IDecompilationProvider _decompilationProvider;

        public DecompileController(IDecompilationProvider decompilationProvider, ILoggerFactory loggerFactory)
        {
            _decompilationProvider = decompilationProvider;
            _logger = loggerFactory.CreateLogger<DecompileController>();
        }

        [HttpGet("types")]
        public IEnumerable<Tuple<string, MetadataToken>> Get()
        {
            return _decompilationProvider.GetTypeTuples();
        }

        [HttpGet("types/{type}/{rid}")]
        public string Get(TokenType type, uint rid)
        {
            return _decompilationProvider.GetCode(type, rid);
        }

        public string GetChildren(TokenType type, uint rid)
        {
            return _decompilationProvider.GetChildren(type, rid);
        }
    }
}
