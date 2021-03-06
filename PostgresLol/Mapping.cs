﻿using System;
using System.Collections.Generic;
using System.Linq;

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

        public static T[] With<T>(this T[] array, T newMember)
        {
            return array.Concat(Enumerable.Repeat(newMember, 1)).ToArray();
        }

        /// <summary>
        /// Parses queryObject into postgresql condition-expression
        /// </summary>
        public static string AsQueryString(this RegistrationQuery q)
        {
            var ands = new List<string>();




            if (q.ForUser != Guid.Empty)
            {
                ands.Add(string.Format("(entity @> '{{\"ResponsibleId\" :\"{0}\"}}' OR entity ->'AssigneeIds' ? '{0}')", q.ForUser));
            }
            

            if(ands.Count == 0)
                return $"select entity from registrations";
            return $"select entity from registrations where {string.Join(" and ",ands)}";
        }
    }
}
