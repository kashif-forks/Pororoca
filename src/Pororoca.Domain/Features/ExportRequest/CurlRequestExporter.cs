using System.Text;
using Pororoca.Domain.Features.Entities.Pororoca;
using Pororoca.Domain.Features.Entities.Pororoca.Http;
using static Pororoca.Domain.Features.Common.JsonUtils;
using static Pororoca.Domain.Features.TranslateRequest.Http.PororocaHttpRequestTranslator;

namespace Pororoca.Domain.Features.ExportRequest;

public static class CurlRequestExporter
{
    public static string ExportAsCurlRequest(PororocaHttpRequest req, PororocaRequestAuth? colScopedAuth)
    {
        StringBuilder sb = new();
        sb.AppendLine("curl " + req.Url + " \\");

        if (req.HttpVersion == 2.0m)
        {
            sb.AppendLine("--http2 \\");
        }
        else if (req.HttpVersion == 3.0m)
        {
            sb.AppendLine("--http3 \\");
        }

        sb.AppendLine(req.HttpMethod == "GET" ? "-G \\" : $"-X {req.HttpMethod} \\");

        var auth = req.CustomAuth != null && req.CustomAuth.Mode == PororocaRequestAuthMode.InheritFromCollection ?
                   colScopedAuth :
                   req.CustomAuth;

        if (auth != null)
        {
            switch (auth.Mode)
            {
                case PororocaRequestAuthMode.Basic:
                    sb.AppendLine($"--basic --user {auth.BasicAuthLogin}:{auth.BasicAuthPassword} \\");
                    break;
                case PororocaRequestAuthMode.Bearer:
                    sb.AppendLine($"--oauth2-bearer '{auth.BearerToken}' \\");
                    break;
                case PororocaRequestAuthMode.Windows:
                    sb.AppendLine($"--ntlm --user {auth.Windows!.Login}:{auth.Windows.Password} \\");
                    break;
                case PororocaRequestAuthMode.ClientCertificate:
                    string certType = auth.ClientCertificate!.Type switch
                    {
                        PororocaRequestAuthClientCertificateType.Pem => "PEM",
                        PororocaRequestAuthClientCertificateType.Pkcs12 => "P12",
                        _ => "PEM"
                    };
                    sb.Append($"--cert-type {certType} --cert {auth.ClientCertificate!.CertificateFilePath}");
                    if (!string.IsNullOrWhiteSpace(auth.ClientCertificate.PrivateKeyFilePath))
                    {
                        sb.Append($" --key {auth.ClientCertificate.PrivateKeyFilePath}");
                    }
                    if (!string.IsNullOrWhiteSpace(auth.ClientCertificate.FilePassword))
                    {
                        sb.Append($" --pass {auth.ClientCertificate.FilePassword}");
                    }
                    sb.AppendLine(" \\");
                    break;
                default:
                    break;
            }
        }

        if (req.Headers != null)
        {
            foreach (var h in req.Headers)
            {
                sb.AppendLine($"--header '{h.Key}: {h.Value}' \\");
            }
        }

        if (req.Body != null)
        {
            switch (req.Body.Mode)
            {
                case PororocaHttpRequestBodyMode.Raw:
                case PororocaHttpRequestBodyMode.File:
                    bool isFile = req.Body.Mode == PororocaHttpRequestBodyMode.File;
                    if (req.Body.ContentType != null && req.Body.ContentType.Contains("json"))
                    {
                        sb.AppendLine(
                            isFile ?
                            $"--json @{req.Body.FileSrcPath} \\" :
                            $"--json '{TryMinifyJsonString(req.Body.RawContent!)}' \\");
                    }
                    else
                    {
                        sb.AppendLine($"--header 'Content-Type: {req.Body.ContentType}' \\");
                        sb.AppendLine($"--data {(isFile ? ('@' + req.Body.FileSrcPath) : ('\'' + req.Body.RawContent + '\''))} \\");
                    }
                    break;
                case PororocaHttpRequestBodyMode.UrlEncoded:
                    foreach (var p in req.Body.UrlEncodedValues!)
                    {
                        sb.AppendLine($"-d '{p.Key}={p.Value}' \\");
                    }
                    break;
                case PororocaHttpRequestBodyMode.FormData:
                    foreach (var p in req.Body.FormDataValues!)
                    {
                        string value = p.Type switch
                        {
                            PororocaHttpRequestFormDataParamType.Text => p.TextValue!,
                            PororocaHttpRequestFormDataParamType.File => '@' + p.FileSrcPath!,
                            _ => string.Empty
                        };
                        sb.AppendLine($"-F '{p.Key}={value};type={p.ContentType}' \\");
                    }
                    break;
                case PororocaHttpRequestBodyMode.GraphQl:
                    string gqlJson = MakeGraphQlJson(req.Body.GraphQlValues!);
                    sb.AppendLine($"--json '{gqlJson}' \\");
                    break;
                default:
                    break;
            }
        }

        // removing whitespaces, backslashes and line-breaks at the end
        return sb.ToString().TrimEnd(' ', '\\', '\r', '\n');
    }

    private static string TryMinifyJsonString(string originalJson)
    {
        // sometimes we can't minify a JSON,
        // especially if there is a templated variable in it, e.g.
        // { "id": {{ MyId }} }
        try
        {
            return MinifyJsonString(originalJson);
        }
        catch
        {
            return originalJson;
        }
    }
}