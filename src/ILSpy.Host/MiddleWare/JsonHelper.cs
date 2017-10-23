// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace MsilDecompiler.Host
{
    static public class JsonHelper
    {
        public static JToken DeserializeRequestObject(Stream readStream)
        {
            try
            {
                using (var streamReader = new StreamReader(readStream))
                {
                    using (var textReader = new JsonTextReader(streamReader))
                    {
                        return JToken.Load(textReader);
                    }
                }
            }
            catch
            {
                return new JObject();
            }
        }
    }
}
