﻿using System;
using System.Web;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.Routing;

namespace Webtools {

    /// <summary>
    /// API Browser
    /// </summary>
    public class APIBrowser : IHttpHandler {

        #region Properties
        /// <summary>
        /// Assemblies to ignore
        /// </summary>
        private static readonly string[] ASSEMBLIES_TO_IGNORE = { "Microsoft", "System", "mscorlib" };

        /// <summary>
        /// Assemblies reflected
        /// </summary>
        private List<Assembly> AssembliesDisplayed = new List<Assembly>();
        #endregion

        #region IHttpHandler Members

        /// <summary>
        /// You will need to configure this handler in the web.config file of your
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.</returns>
        public bool IsReusable {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context) {
            RenderPageFromAssembly(
                context.Response.OutputStream,
                AppDomain.CurrentDomain.GetAssemblies());
        }
        #endregion

        #region Util Methods
        /// <summary>
        /// Checks if the assembly should be ignored.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        private bool IgnoreAssembly(Assembly assembly) {
            foreach (var name in ASSEMBLIES_TO_IGNORE)
                if (assembly.FullName.StartsWith(name))
                    return true;
            return false;
        }

        /// <summary>
        /// Retrieves the embedded resource.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns></returns>
        private string RetrieveEmbeddedResource(string resourceName) {
            string data = string.Empty;
            if (!string.IsNullOrEmpty(resourceName)) {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                using (var resourceStream = currentAssembly.GetManifestResourceStream(resourceName)) {
                    if (null != resourceStream) {
                        using (var resourceStreamReader = new StreamReader(resourceStream)) {
                            data = resourceStreamReader.ReadToEnd();
                        }
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Determines whether the specified type is controller.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// 	<c>true</c> if the specified type is controller; otherwise, <c>false</c>.
        /// </returns>
        private bool IsController(Type type) {
            return (null == type || null == type.BaseType)
                ? false
                : (type.BaseType.FullName != "System.Web.Mvc.Controller")
                    ? IsController(type.BaseType)
                    : true;
        }

        /// <summary>
        /// Determines whether the specified methos is a MVC action method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        private static string IsMvcActionMethod(MethodInfo method) {
            var attributes = method.GetCustomAttributes(true);
            foreach (var attrib in attributes) {
                if (attrib.GetType().Name == "HttpPostAttribute")
                    return "POST";
                if (attrib.GetType().Name == "HttpGetAttribute")
                    return "GET";
            }
            return null;
        }
        #endregion

        #region Render Methods
        /// <summary>
        /// Renders the page from assembly.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        /// <param name="assemblies">The assemblies.</param>
        private void RenderPageFromAssembly(Stream outputStream, Assembly[] assemblies) {
            List<Type> types = new List<Type>();
            foreach (var assembly in assemblies) {
                if (!IgnoreAssembly(assembly)) {
                    types.AddRange(assembly.GetTypes());
                }
            }

            //using(TextWriter tx = new StringWriter(str))
            using (StreamWriter streamWriter = new StreamWriter(outputStream))
            using (HtmlTextWriter htmlWriter = new HtmlTextWriter(streamWriter)) {
               
                //begin page
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);

                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Head);
                
                        //title
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Title);
                            htmlWriter.Write("API Browser");
                        htmlWriter.RenderEndTag();

                        //css
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text/css");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Style);                
                            htmlWriter.Write(RetrieveEmbeddedResource("Webtools.APIBrowser.Styles.css"));
                        htmlWriter.RenderEndTag();

                        //jQuery
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Src, "http://ajax.googleapis.com/ajax/libs/jquery/1.7.0/jquery.min.js");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Script);
                        htmlWriter.RenderEndTag();

                        //custom javascript
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text/javascript");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Script);
                            htmlWriter.Write(RetrieveEmbeddedResource("Webtools.APIBrowser.Async.js"));
                        htmlWriter.RenderEndTag();

                    htmlWriter.RenderEndTag();

                    //begin body
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);

                        //header
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.H1);
                        htmlWriter.Write("API Browser");
                        htmlWriter.RenderEndTag();

                        //Find all the controller types
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, "content");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                            foreach (var type in types) {
                                if (IsController(type)) {
                                    RenderController(htmlWriter, type);
                                }
                            }

                            //footer line
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Hr);
                            htmlWriter.RenderEndTag();

                            //assemblies displayed
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.H3);
                            htmlWriter.Write("Reflected Assemblies");
                            htmlWriter.RenderEndTag();
                            htmlWriter.WriteBreak();
                            foreach (var a in AssembliesDisplayed) {
                                htmlWriter.RenderBeginTag(HtmlTextWriterTag.B);
                                htmlWriter.Write(a.FullName);
                                htmlWriter.RenderEndTag();
                                htmlWriter.WriteBreak();
                                htmlWriter.Write("[{0}]", a.EscapedCodeBase);
                                htmlWriter.WriteBreak();
                                htmlWriter.WriteBreak();
                            }

                            //footer line
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Hr);
                            htmlWriter.RenderEndTag();

                            //footer text
                            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "footer");
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                                htmlWriter.Write("Powered by ");
                                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#");
                                htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);                
                                    htmlWriter.Write("API Browser");
                                htmlWriter.RenderEndTag();
                                htmlWriter.Write(", Version {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                                htmlWriter.WriteBreak();
                                htmlWriter.Write("Copyright (c) 2011 Gaurang Sinha. All rights reserved. Licensed under GPL version 2");
                            htmlWriter.RenderEndTag();
                        htmlWriter.RenderEndTag();

                    //close body
                    htmlWriter.RenderEndTag();

                //close html
                htmlWriter.RenderEndTag();
            }
        }

        /// <summary>
        /// Renders the controller.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="controller">The controller.</param>
        private void RenderController(HtmlTextWriter htmlWriter, Type controller) {
            bool titleHeaderExists = false;            
            MethodInfo[] methods = controller.GetMethods();
            if (null != methods && methods.Length > 0) {
                foreach (var method in methods) {
                    var mType = IsMvcActionMethod(method);
                    if (null != mType) {
                        if (false == titleHeaderExists) {
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.H2);
                            htmlWriter.Write("/" + controller.Name.Replace("Controller", string.Empty));
                            htmlWriter.RenderEndTag();
                            titleHeaderExists = true;
                            if (!AssembliesDisplayed.Contains(controller.Assembly))
                                AssembliesDisplayed.Add(controller.Assembly);
                        }
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                        RenderMethod(htmlWriter, method);
                        htmlWriter.RenderEndTag();
                    }
                }
                if (titleHeaderExists) {
                    htmlWriter.WriteBreak();
                }
            }
        }

        /// <summary>
        /// Renders the method.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="method">The method.</param>
        private void RenderMethod(HtmlTextWriter htmlWriter, MethodInfo method) {
            string formAction = string.Format("/{0}/{1}", method.ReflectedType.Name.Replace("Controller", string.Empty), method.Name);
            string formId = string.Format("{0}_{1}", method.ReflectedType.Name.Replace("Controller", string.Empty), method.Name);
            string httpMethod = IsMvcActionMethod(method);

            //header
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "heading_" + httpMethod.ToLower());
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);            
            
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H3);

                    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "http_method");
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Span);

                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#" + formAction);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_content').slideToggle(500);");
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                            htmlWriter.Write(IsMvcActionMethod(method));
                        htmlWriter.RenderEndTag();

                    htmlWriter.RenderEndTag();

                    htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "path");
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Span);
                            
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#" + formAction);
                        htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_content').slideToggle(500);");            
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                        htmlWriter.Write(formAction);
                        htmlWriter.RenderEndTag();

                    htmlWriter.RenderEndTag();

                htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();
            
            //render parameters
            RenderMethodParameters(htmlWriter, method.GetParameters(), formId, httpMethod, formAction);
        }

        /// <summary>
        /// Renders the method parameters.
        /// </summary>
        /// <param name="htmlWriter">The HTML writer.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="formId">The form id.</param>
        /// <param name="method">The method.</param>
        /// <param name="action">The action.</param>
        private void RenderMethodParameters(HtmlTextWriter htmlWriter, ParameterInfo[] parameters, string formId, string method, string action) {
            //
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_content");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "content_" + method.ToLower());
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none;");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId);
                htmlWriter.AddAttribute("action", action);
                htmlWriter.AddAttribute("method", method);
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Form);

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Table);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Thead);
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
                                htmlWriter.Write("Parameter");
                            htmlWriter.RenderEndTag();
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
                                htmlWriter.Write("Value");
                            htmlWriter.RenderEndTag();
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
                                htmlWriter.Write("Type");
                            htmlWriter.RenderEndTag();
                        htmlWriter.RenderEndTag();
                    htmlWriter.RenderEndTag();
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);

                    foreach (var parameter in parameters) {
                        htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
                                htmlWriter.Write(parameter.Name);
                            htmlWriter.RenderEndTag();
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
                                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_" + parameter.Name);
                                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Name, parameter.Name);
                                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Input);
                                htmlWriter.RenderEndTag();
                            htmlWriter.RenderEndTag();
                            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
                                htmlWriter.Write(parameter.ParameterType.Name);
                            htmlWriter.RenderEndTag();
                        htmlWriter.RenderEndTag();
                    }

                    //close tbody
                    htmlWriter.RenderEndTag();

                //close table
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Type, "submit");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Value, "Submit");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Input);
                htmlWriter.RenderEndTag();

                htmlWriter.Write("&nbsp;");

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_hider");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Href, "#");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, "$('#" + formId + "_response').slideUp();$(this).fadeOut();return false;");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.A);
                    htmlWriter.Write("Hide Responses");
                htmlWriter.RenderEndTag();

            //close form
            htmlWriter.RenderEndTag();

            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response");
            htmlWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                    htmlWriter.Write("URL");
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_url");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_url");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.RenderEndTag();

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                    htmlWriter.Write("Response Body");
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_body");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_body");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.RenderEndTag();

                htmlWriter.RenderBeginTag(HtmlTextWriterTag.H4);
                htmlWriter.Write("Response Status");
                htmlWriter.RenderEndTag();

                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Id, formId + "_response_header");
                htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "response_header");
                htmlWriter.RenderBeginTag(HtmlTextWriterTag.Div);
                htmlWriter.RenderEndTag();

                htmlWriter.RenderEndTag();

            htmlWriter.RenderEndTag();
        }
        #endregion
    }
}
