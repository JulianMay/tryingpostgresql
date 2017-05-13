using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresLol
{
    public static class Mapping
    {
        public static string Serialized(this Registration r)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(r);
            return json;
        }
        public static T Deserialized<T>(this string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}
