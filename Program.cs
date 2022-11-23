using System.Net;
using System.Security.Cryptography.X509Certificates;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace InOutProxy
{
    public class Program
    {
        public class InOutProxy
        {
            private readonly ProxyServer server;
            private readonly ExplicitProxyEndPoint explicitEndPoint;
            private readonly string targetHost;
            private readonly string targetUrl;

            private async Task BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
            {
                await Task.Run(() =>
                {
                    string url = e.HttpClient.Request.Url;
                    e.DecryptSsl = false;
                    // Console.WriteLine(url);
                    if (url.Contains(targetHost))
                    {
                        e.DecryptSsl = true;
                    }
                });
            }

            private async Task BeforeRequest(object sender, SessionEventArgs e)
            {
                var url = e.HttpClient.Request.Url;
                // Console.WriteLine(url);
                if (url.StartsWith(targetUrl) && e.HttpClient.Request.Method == HttpMethod.Post.ToString())
                {
                    var inOrOut = "出";
                    var referer = e.HttpClient.Request.Headers.GetHeaders("Referer");
                    if (referer != null && referer.Count > 0 && referer[0].Value.Contains("type=0"))
                    {
                        inOrOut = "入";
                    }

                    string responseText =
                        "{\"e\":0,\"m\":\"操作成功\",\"d\":{\"list\":{\"qrcode\":{\"content\":\"通过\",\"color\":\"#00BB00\"},\"status\":\"允许" +
                        inOrOut +
                        "校\"}}}";

                    e.Ok(responseText, new[]{
                        new HttpHeader("content-type", "application/json; charset=UTF-8")
                    });
                    return;
                }

                await Task.FromResult(0);
            }

            public void Go()
            {
                server.Start();
                // server.SetAsSystemHttpsProxy(explicitEndPoint);
            }

            // public void Stop()
            // {
            //     explicitEndPoint.BeforeTunnelConnectRequest -= BeforeTunnelConnectRequest;
            //     server.BeforeResponse -= BeforeRequest;
            //     server.Stop();
            //     // server.DisableAllSystemProxies();
            // }

            public InOutProxy(string targetHost, string targetUrl, int port)
            {
                this.targetUrl = targetUrl;
                this.targetHost = targetHost;

                explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, port, true);
                explicitEndPoint.BeforeTunnelConnectRequest += BeforeTunnelConnectRequest;

                server = new ProxyServer();
                server.CertificateManager.CertificateValidDays = 300;
                server.CertificateManager.EnsureRootCertificate();
                server.CertificateManager.TrustRootCertificate(true);
                var bytes = server.CertificateManager.RootCertificate.Export(X509ContentType.Cert);
                File.WriteAllBytes("root-ca.cer", bytes);
                
                server.BeforeRequest += BeforeRequest;

                server.AddEndPoint(explicitEndPoint);
            }
        }

        public static void Main(string[] args)
        {
            var port = 51299;
            var inoutProxy = new InOutProxy("service.bupt.edu.cn", "https://service.bupt.edu.cn/site/data-source/detail", port);
            inoutProxy.Go();
            // Console.Read();
            // inoutProxy.Stop();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}