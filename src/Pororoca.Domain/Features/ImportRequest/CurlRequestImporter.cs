using Pororoca.Domain.Features.Common;
using Pororoca.Domain.Features.Entities.Pororoca;
using Pororoca.Domain.Features.Entities.Pororoca.Http;
using static Pororoca.Domain.Features.Entities.Pororoca.Http.PororocaHttpRequestBody;
using static Pororoca.Domain.Features.Entities.Pororoca.Http.PororocaHttpRequestFormDataParam;
using static Pororoca.Domain.Features.Entities.Pororoca.PororocaRequestAuth;

namespace Pororoca.Domain.Features.ImportRequest;

public static partial class CurlRequestImporter
{
    public static PororocaHttpRequest? ImportCurlRequest(string? curlCmdLine)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(curlCmdLine))
            {
                return null;
            }

            var curlKvps = ParseCurlCommandLineParams(curlCmdLine);

            bool hasMultipleDataParams = curlKvps.Count(x => x.Key == "-d" || x.Key.StartsWith("--data")) >= 2;

            decimal httpVersion = 1.1m;
            string? httpMethodFromData = null, httpMethodFromRequest = null;
            string url = string.Empty;
            List<string>? urlQueryKvs = null;

            PororocaRequestAuthMode? authMode = null;
            string? authUser = null, authPassword = null, bearerToken = null;
            PororocaRequestAuthClientCertificateType? authCertType = null;
            string? authCertFilePath = null, authCertPrivateKeyFilePath = null, authCertPrivateKeyPassword = null;
            List<PororocaKeyValueParam>? headers = null;
            PororocaHttpRequestBodyMode? bodyMode = null;
            string? bodyContentType = null;
            string? bodyRaw = null;
            string? bodyFilePath = null;
            List<PororocaKeyValueParam>? urlEncodedParams = null;
            List<PororocaHttpRequestFormDataParam>? formDataParams = null;

            foreach (var kv in curlKvps)
            {
                // cURL docs: https://curl.se/docs/manpage.html
                // if param value begins with @, then it's a file

                Action act = kv.Key switch
                {
                    "-0" or "--http1.0" => () => httpVersion = 1.0m,
                    "--http1.1" => () => httpVersion = 1.1m,
                    "--http2" or "--http2-prior-knowledge" => () => httpVersion = 2.0m,
                    "--http3" or "--http3-only" => () => httpVersion = 3.0m,

                    "--basic" => () => authMode = PororocaRequestAuthMode.Basic,
                    "--oauth2-bearer" => () =>
                    {
                        authMode = PororocaRequestAuthMode.Bearer;
                        bearerToken = kv.Value;
                    },
                    "--ntlm" => () => authMode = PororocaRequestAuthMode.Windows,
                    "-u" or "--user" => () =>
                    {
                        var (usr, pwd) = SplitKeyValueSeparatedByChar(kv.Value, ':');
                        authUser = usr;
                        authPassword = pwd;
                    },

                    "--cert-type" => () =>
                    {
                        authCertType = kv.Value switch
                        {
                            "PEM" => PororocaRequestAuthClientCertificateType.Pem,
                            "P12" => PororocaRequestAuthClientCertificateType.Pkcs12,
                            _ => PororocaRequestAuthClientCertificateType.Pem,
                        };
                    },
                    "-E" or "--cert" => () =>
                    {
                        authMode = PororocaRequestAuthMode.ClientCertificate;
                        authCertFilePath = kv.Value;
                    },
                    "--key" => () => authCertPrivateKeyFilePath = kv.Value,
                    "--pass" => () => authCertPrivateKeyPassword = kv.Value,

                    "-X" or "--request" => () => httpMethodFromRequest = kv.Value,
                    "-G" or "--get" => () => httpMethodFromRequest = "GET",

                    "--url" => () => url = kv.Value,
                    "--url-query" => () =>
                    {
                        urlQueryKvs ??= new();
                        urlQueryKvs.Add(kv.Value);
                    },

                    "-H" or "--header" => () =>
                    {
                        var (headerName, headerValue) = SplitKeyValueSeparatedByChar(kv.Value, ':');

                        if (headerName == "Content-Type")
                        {
                            bodyContentType = headerValue;
                        }
                        else
                        {
                            headers ??= new();
                            headers.Add(new(true, headerName, headerValue));
                        }
                    },
                    "-e" or "--referer" => () =>
                    {
                        headers ??= new();
                        headers.Add(new(true, "Referer", kv.Value));
                    },
                    "-A" or "--user-agent" => () =>
                    {
                        headers ??= new();
                        headers.Add(new(true, "User-Agent", kv.Value));
                    },

                    "-d" or
                    "--data" or
                    "--data-ascii" or
                    "--data-raw" or
                    "--data-urlencode" or
                    "--data-binary" => () =>
                    {
                        httpMethodFromData = "POST";                        
                        if (hasMultipleDataParams)
                        {
                            bodyMode = PororocaHttpRequestBodyMode.UrlEncoded;
                            var (key, value) = SplitKeyValueSeparatedByChar(kv.Value, '=');
                            urlEncodedParams ??= new();
                            urlEncodedParams.Add(new(true, key, value));
                        }
                        else if (kv.Value.StartsWith('@'))
                        {
                            bodyMode = PororocaHttpRequestBodyMode.File;
                            bodyFilePath = kv.Value.TrimStart('@');
                            MimeTypesDetector.TryFindMimeTypeForFile(bodyFilePath, out bodyContentType);
                        }
                        else
                        {
                            bodyMode = PororocaHttpRequestBodyMode.Raw;
                            bodyRaw = kv.Value;
                            bodyContentType ??= MimeTypesDetector.DefaultMimeTypeForText;
                        }
                    },
                    "-F" or "--form" => () =>
                    {
                        httpMethodFromData = "POST";
                        bodyMode = PororocaHttpRequestBodyMode.FormData;
                        string? formParamContentType = null;
                        string formParamKey, formParamValue;
                        if (kv.Value.Contains(';'))
                        {
                            var (part1, part2) = SplitKeyValueSeparatedByChar(kv.Value, ';');
                            if (part2.StartsWith("type"))
                            {
                                formParamContentType = SplitKeyValueSeparatedByChar(part2, '=').Item2;
                            }
                            (formParamKey, formParamValue) = SplitKeyValueSeparatedByChar(part1, '=');
                        }
                        else
                        {
                            (formParamKey, formParamValue) = SplitKeyValueSeparatedByChar(kv.Value, '=');
                        }

                        formDataParams ??= new();
                        if (formParamValue.StartsWith('@'))
                        {
                            if (formParamContentType == null)
                            {
                                MimeTypesDetector.TryFindMimeTypeForFile(formParamValue, out formParamContentType);
                            }
                            formDataParams.Add(MakeFileParam(true, formParamKey, formParamValue.TrimStart('@'), formParamContentType ?? MimeTypesDetector.DefaultMimeTypeForText));
                        }
                        else
                        {
                            formDataParams.Add(MakeTextParam(true, formParamKey, formParamValue, formParamContentType ?? MimeTypesDetector.DefaultMimeTypeForText));
                        }
                    },
                    "--json" => () =>
                    {
                        httpMethodFromData = "POST";
                        headers ??= new();
                        headers.Add(new(true, "Accept", MimeTypesDetector.DefaultMimeTypeForJson));
                        bodyContentType = MimeTypesDetector.DefaultMimeTypeForJson;
                        if (kv.Value.StartsWith('@'))
                        {
                            bodyMode = PororocaHttpRequestBodyMode.File;
                            bodyFilePath = kv.Value.TrimStart('@');
                        }
                        else
                        {
                            bodyMode = PororocaHttpRequestBodyMode.Raw;
                            bodyRaw = kv.Value;
                        }
                    },
                    "-T" or "--upload-file" => () =>
                    {
                        httpMethodFromData = "PUT";
                        bodyMode = PororocaHttpRequestBodyMode.File;
                        bodyFilePath = kv.Value;
                        MimeTypesDetector.TryFindMimeTypeForFile(bodyFilePath, out bodyContentType);
                    },

                    _ => () => { }
                };

                try
                {
                    act();
                }
                catch
                {
                    // forgive localized errors on individual parameters
                    // try to save the global request parsing
                }
            }

            PororocaHttpRequestBody? body = bodyMode switch
            {
                PororocaHttpRequestBodyMode.Raw => MakeRawContent(bodyRaw!, bodyContentType!),
                PororocaHttpRequestBodyMode.File => MakeFileContent(bodyFilePath!, bodyContentType!),
                PororocaHttpRequestBodyMode.UrlEncoded => MakeUrlEncodedContent(urlEncodedParams!),
                PororocaHttpRequestBodyMode.FormData => MakeFormDataContent(formDataParams!),
                _ => null
            };

            PororocaRequestAuth? auth = authMode switch
            {
                PororocaRequestAuthMode.Basic => MakeBasicAuth(authUser!, authPassword!),
                PororocaRequestAuthMode.Bearer => MakeBearerAuth(bearerToken!),
                PororocaRequestAuthMode.Windows => MakeWindowsAuth(false, authUser!, authPassword!, null),
                PororocaRequestAuthMode.ClientCertificate => MakeClientCertificateAuth((PororocaRequestAuthClientCertificateType)authCertType!, authCertFilePath!, authCertPrivateKeyFilePath, authCertPrivateKeyPassword),
                _ => null
            };

            PororocaHttpRequest req = new(
                Name: "cURL req",
                HttpVersion: httpVersion,
                HttpMethod: httpMethodFromRequest ?? httpMethodFromData ?? "GET",
                Url: urlQueryKvs == null ? url : url + '?' + string.Join('&', urlQueryKvs),
                Headers: headers,
                Body: body,
                CustomAuth: auth,
                ResponseCaptures: null);

            return req;
        }
        catch
        {
            return null;
        }
    }

    private static (string, string) SplitKeyValueSeparatedByChar(ReadOnlySpan<char> s, char separator)
    {
        if (s.Length < 3)
        {
            return (string.Empty, string.Empty);
        }

        int i = s.IndexOf(separator);
        if (i != -1)
        {
            return (s[..i].Trim().ToString(), s[(i + 1)..].Trim().ToString());
        }
        else
        {
            return (s.ToString(), string.Empty);
        }
    }
}