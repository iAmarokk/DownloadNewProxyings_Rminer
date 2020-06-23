using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadNewProxyings_Rminer
{
    class Program
    {
        private const int ProxyingChecksCount = 3;
        static CancellationToken cancellationToken = new CancellationToken();
        private static readonly TimeSpan ProxyingChecksMinDelay = TimeSpan.FromMinutes(1);
        private static readonly Regex PageTitleCountRegex = new Regex(@"<span class=""page-title-count""[^>]*>[0-9\s]+</span>");
        private const string CheckProxyUrl = "https://www.avito.ru/tatarstan/nedvizhimost";

        static void Main(string[] args)
        {

            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\Skynet\source\repos\DownloadNewProxyings_Rminer\bin\Debug\proxy.txt");
            for (int j = 0; j < lines.Length; j++)
            {
                var wc = new WebClient
                {
                    Proxy = new WebProxy(lines[j]),
                    Headers =
                                 {
                                     ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                                     ["Accept-Encoding"] = "identity",
                                     ["Accept-Language"] = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3",
                                     ["Cache-Control"] = "max-age=0",
                                     ["Host"] = "www.avito.ru",
                                     ["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0"
                                 },
                    Encoding = Encoding.UTF8
                };

                using (wc)
                {
                    var sw = new Stopwatch();
                    var proxyDelays = new long[ProxyingChecksCount];
                    for (var i = 0; i < proxyDelays.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var token = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(ProxyingChecksMinDelay).Token, cancellationToken).Token;
                            using (token.Register(wc.CancelAsync))
                            {
                                sw.Restart();
                                var html = wc.DownloadString(CheckProxyUrl);
                                var count = PageTitleCountRegex.Match(html).Value;

                                if (string.IsNullOrEmpty(count))
                                {
                                    throw new WebException("Captcha received!", WebExceptionStatus.UnknownError);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            
                        }
                        finally
                        {
                            proxyDelays[i] = sw.ElapsedMilliseconds;
                        }
                    }

                    Console.WriteLine(proxyDelays.Average()); 
                }
            }

        }
    }
}
