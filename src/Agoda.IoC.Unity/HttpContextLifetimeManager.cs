using System;
using System.Web;
using Microsoft.Practices.Unity;

namespace Agoda.IoC.Unity
{
    public class HttpContextLifetimeManager : LifetimeManager
    {
        private readonly object key = new object();

        public override object GetValue()
        {
            if (HttpContext.Current != null &&
                HttpContext.Current.Items.Contains(key))
                return HttpContext.Current.Items[key];
            else
                return null;
        }

        public override void RemoveValue()
        {
            if (HttpContext.Current != null)
                HttpContext.Current.Items.Remove(key);
        }

        public override void SetValue(object newValue)
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items[key] = newValue;

                var disposable = newValue as IDisposable;

                if (disposable != null)
                {
                    HttpContext.Current.AddOnRequestCompleted(_ => disposable.Dispose());
                }
            }
        }
    }
}