using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Data;
using Newtonsoft.Json;

namespace SharpRaven.Log4Net.Extra
{
    public class HttpExtra
    {
        private readonly dynamic httpContext;

        private HttpExtra(dynamic httpContext)
        {
            this.httpContext = httpContext;
            Request = GetRequest();
            Response = GetResponse();
            Session = GetSession();
        }


        public static HttpExtra GetHttpExtra()
        {
            var context = GetHttpContext();
            return context == null ? null : new HttpExtra(context);
        }

        public object Request { get; }
        public object Response { get; }
        public object Session { get; }


        private object GetResponse()
        {
            try
            {
                return new
                {
                    Cookies = Convert(x => x.Response.Cookies, GetValueFromCookieCollection),
                    Headers = Convert(x => x.Response.Headers),
                    ContentEncoding = httpContext.Response.ContentEncoding.HeaderName,
                    HeaderEncoding = httpContext.Response.HeaderEncoding.HeaderName,
                    httpContext.Response.ContentType,
                    httpContext.Response.Charset,
                    httpContext.Response.Expires,
                    httpContext.Response.ExpiresAbsolute,
                    httpContext.Response.IsClientConnected,
                    httpContext.Response.IsRequestBeingRedirected,
                    httpContext.Response.RedirectLocation,
                    httpContext.Response.SuppressContent,
                    httpContext.Response.TrySkipIisCustomErrors,
                    Status = new
                    {
                        this.httpContext.Response.Status,
                        Code = this.httpContext.Response.StatusCode,
                        Description = this.httpContext.Response.StatusDescription,
                        SubCode = this.httpContext.Response.SubStatusCode,
                    }
                };
            }
            catch (Exception exception)
            {
                return new
                {
                    Exception = exception
                };
            }
        }


        private object GetRequest()
        {
            try
            {
                return new
                {
                    ServerVariables = Convert(x => x.Request.ServerVariables),
                    Form = Convert(x => x.Request.Form),
                    Cookies = Convert(x => x.Request.Cookies, GetValueFromCookieCollection),
                    Headers = Convert(x => x.Request.Headers),
                    //Params = Convert(x => x.Request.Params),
                    ContentEncoding = httpContext.Request.ContentEncoding.HeaderName,
                    httpContext.Request.AcceptTypes,
                    httpContext.Request.ApplicationPath,
                    httpContext.Request.ContentType,
                    httpContext.Request.CurrentExecutionFilePath,
                    httpContext.Request.CurrentExecutionFilePathExtension,
                    httpContext.Request.FilePath,
                    httpContext.Request.HttpMethod,
                    httpContext.Request.IsAuthenticated,
                    httpContext.Request.IsLocal,
                    httpContext.Request.IsSecureConnection,
                    httpContext.Request.Path,
                    httpContext.Request.PathInfo,
                    httpContext.Request.PhysicalApplicationPath,
                    httpContext.Request.PhysicalPath,
                    httpContext.Request.QueryString,
                    httpContext.Request.RawUrl,
                    httpContext.Request.TotalBytes,
                    httpContext.Request.Url,
                    httpContext.Request.UserAgent,
                    User = new
                    {
                        Languages = this.httpContext.Request.UserLanguages,
                        Host = new
                        {
                            Address = this.httpContext.Request.UserHostAddress,
                            Name = this.httpContext.Request.UserHostName,
                        }
                    }
                };
            }
            catch (Exception exception)
            {
                return new
                {
                    Exception = exception
                };
            }
        }


        private object GetSession()
        {
            try
            {
                if (httpContext.Session == null)
                {
                    return null;
                }

                return new
                {
                    Contents = GetValueFromSession(httpContext.Session)
                };
            }
            catch (Exception exception)
            {
                return new
                {
                    Exception = exception
                };
            }
        }

        private static dynamic GetHttpContext()
        {
            var systemWeb = AppDomain.CurrentDomain
                                     .GetAssemblies()
                                     .FirstOrDefault(assembly => assembly.FullName.StartsWith("System.Web"));

            if (systemWeb == null)
                return null;

            var httpContextType = systemWeb.GetExportedTypes()
                                           .FirstOrDefault(type => type.Name == "HttpContext");

            if (httpContextType == null)
                return null;

            var currentHttpContextProperty = httpContextType.GetProperty("Current",
                                                                         BindingFlags.Static | BindingFlags.Public);

            return currentHttpContextProperty == null ? null : currentHttpContextProperty.GetValue(null, null);
        }

        private IDictionary<string, string> Convert(Func<dynamic, NameObjectCollectionBase> collectionGetter, Func<NameObjectCollectionBase, object, string> valueFromCollectionGetter = null)
        {
            if (httpContext == null)
                return null;

            if (valueFromCollectionGetter == null)
                valueFromCollectionGetter = (c, key) => ((NameValueCollection)c)[key.ToString()];

            IDictionary<string, string> dictionary = new Dictionary<string, string>();

            try
            {
                NameObjectCollectionBase collection = collectionGetter.Invoke(httpContext);

                foreach (var key in collection.Keys)
                {
                    // NOTE: Ignore these keys as they just add duplicate information. [asbjornu]
                    if (key.ToString() == "ALL_HTTP" || key.ToString() == "ALL_RAW")
                        continue;

                    var value = valueFromCollectionGetter(collection, key);
                    dictionary.Add(key.ToString(), value);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return dictionary;
        }

        private static string GetValueFromCookieCollection(NameObjectCollectionBase cookieCollection, object key)
        {
            return ((dynamic)cookieCollection)[key.ToString()].Value;
        }

        private static IDictionary<string, object> GetValueFromSession(dynamic session)
        {
            var list = new Dictionary<string, object>
            {
                { "SessionID", session.SessionID },
                { "Timeout", session.Timeout },
                { "LCID", session.LCID }
            };

            foreach (var key in session.Keys)
            {
                var value = session[key];

                if (value is DataSet)
                    list.Add(key.ToString(), JsonConvert.SerializeObject(value, Formatting.Indented));
                else
                    list.Add(key.ToString(), value);
            }

            return list;
        }
    }
}
