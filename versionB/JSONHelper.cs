using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.Script.Serialization;
using System.Data;
using System.ComponentModel;

namespace MockupV1
{
    public static class JSONHelper
    {
        public static string ToJSON(this object obj)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }

        public static string ToJSON(this object obj, int recursionDepth)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.RecursionLimit = recursionDepth;
            Console.WriteLine(obj);
            return serializer.Serialize(obj);
        }

        public static Record ToData(this string obj)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Record>(obj);
        }
    }
}
