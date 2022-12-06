using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using Mikano_API;

namespace Mikano_API.Models
{

    public class KHelpers
    {

        public object KUpdateModel(object oldModel, object newModel)
        {
            var objectType = oldModel.GetType();
            var properties = objectType.GetProperties();

            foreach (var prop in properties)
            {
                var newPropValue = prop.GetValue(newModel, null);
                prop.SetValue(oldModel, newPropValue);
            }
            return oldModel;
        }

    }

}