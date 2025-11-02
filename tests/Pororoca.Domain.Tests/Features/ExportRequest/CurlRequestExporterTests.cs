using Pororoca.Domain.Features.Entities.Pororoca;
using Pororoca.Domain.Features.Entities.Pororoca.Http;
using Xunit;
using static Pororoca.Domain.Features.Entities.Pororoca.Http.PororocaHttpRequestFormDataParam;
using static Pororoca.Domain.Features.ExportRequest.CurlRequestExporter;

namespace Pororoca.Domain.Tests.Features.ExportRequest;

public static class CurlRequestExporterTests
{
    [Fact]
    public static void Should_export_req_with_http2_correctly()
    {
        // GIVEN
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 2.0m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br");

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--http2", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_http3_correctly()
    {
        // GIVEN
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 3.0m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br");

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--http3", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_method_GET_correctly()
    {
        // GIVEN
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br");

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("-G", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_method_DELETE_correctly()
    {
        // GIVEN
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "DELETE",
            Url: "http://www.pudim.com.br");

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("-X DELETE", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_basic_auth_correctly()
    {
        // GIVEN
        var auth = PororocaRequestAuth.MakeBasicAuth("usy", "pwx");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            CustomAuth: auth);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--basic --user usy:pwx", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_bearer_auth_correctly()
    {
        // GIVEN
        var auth = PororocaRequestAuth.MakeBearerAuth("mysupertokem");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            CustomAuth: auth);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--oauth2-bearer 'mysupertokem'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_windows_ntlm_auth_correctly()
    {
        // GIVEN
        var auth = PororocaRequestAuth.MakeWindowsAuth(false, "DOMINIO\\USUARIO", "SENHA123", null);
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            CustomAuth: auth);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--ntlm --user DOMINIO\\USUARIO:SENHA123", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_PEM_client_certificate_auth_correctly()
    {
        // GIVEN
        var auth = PororocaRequestAuth.MakeClientCertificateAuth(PororocaRequestAuthClientCertificateType.Pem, "cert.pem", "privkey.pem", "SENHA123");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            CustomAuth: auth);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--cert-type PEM --cert cert.pem --key privkey.pem --pass SENHA123", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_PKCS12_client_certificate_auth_correctly()
    {
        // GIVEN
        var auth = PororocaRequestAuth.MakeClientCertificateAuth(PororocaRequestAuthClientCertificateType.Pkcs12, "cert.p12", null, "SENHAPKCS12");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            CustomAuth: auth);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--cert-type P12 --cert cert.p12 --pass SENHAPKCS12", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_inherited_from_collection_auth_correctly()
    {
        // GIVEN
        var auth = PororocaRequestAuth.MakeBearerAuth("mysupertokem_INHERITED");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            CustomAuth: PororocaRequestAuth.InheritedFromCollection);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: auth);

        // THEN
        Assert.Contains("--oauth2-bearer 'mysupertokem_INHERITED'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_custom_headers_correctly()
    {
        // GIVEN
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Headers:
            [
                new(true, "Content-Language", "pt"),
                new(true, "Date", "2025-10-31")
            ]);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--header 'Content-Language: pt'", cmdLine);
        Assert.Contains("--header 'Date: 2025-10-31'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_raw_minifiable_json_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeRawContent("{\n\"prop\":9,\n\"Arr\":\n[\n\"aaa\",\n\"bbb\",\n\"ccc\"\n]\n}", "application/json");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        // JSON should be minified, preserving property names
        Assert.Contains("--json '{\"prop\":9,\"Arr\":[\"aaa\",\"bbb\",\"ccc\"]}'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_raw_unminifiable_json_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeRawContent("{\n\"prop\":\n{{ TemplatedVar }}\n}", "application/json");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        // JSON should be preserved unminified, because templated vars block minification
        Assert.Contains("--json '{\n\"prop\":\n{{ TemplatedVar }}\n}'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_raw_other_content_type_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeRawContent("bacon ipsum picanha", "text/plain");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--header 'Content-Type: text/plain'", cmdLine);
        Assert.Contains("--data 'bacon ipsum picanha'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_file_json_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeFileContent("arquivo.json", "text/json");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--json @arquivo.json", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_file_other_content_type_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeFileContent("fotografia.jpg", "image/jpeg");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--header 'Content-Type: image/jpeg'", cmdLine);
        Assert.Contains("--data @fotografia.jpg", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_url_encoded_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeUrlEncodedContent(
        [
            new(true, "key1", "value1"),
            new(true, "key2", "value2"),
        ]);
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("-d 'key1=value1'", cmdLine);
        Assert.Contains("-d 'key2=value2'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_form_data_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeFormDataContent(
        [
            MakeTextParam(true, "key_text1", "text1", "text/plain"),
            MakeTextParam(true, "key_text2", "{\"a\":\"u\"}", "application/json"),
            MakeFileParam(true, "key_file1", "/home/usu/dir1/arquivo.pdf", "application/pdf"),
            MakeFileParam(true, "key_file2", "C:\\Users\\Usu\\Music\\song1.mp3", "audio/mpeg3")
        ]);
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("-F 'key_text1=text1;type=text/plain'", cmdLine);
        Assert.Contains("-F 'key_text2={\"a\":\"u\"};type=application/json'", cmdLine);
        Assert.Contains("-F 'key_file1=@/home/usu/dir1/arquivo.pdf;type=application/pdf'", cmdLine);
        Assert.Contains("-F 'key_file2=@C:\\Users\\Usu\\Music\\song1.mp3;type=audio/mpeg3'", cmdLine);
    }

    [Fact]
    public static void Should_export_req_with_graphql_body_correctly()
    {
        // GIVEN
        var body = PororocaHttpRequestBody.MakeGraphQlContent("gql_QUERY", "{\"var1\":104,\n\n\"var3\":-14}");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 1.1m,
            HttpMethod: "GET",
            Url: "http://www.pudim.com.br",
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        Assert.Contains("--json '{\"query\":\"gql_QUERY\",\"variables\":{\"var1\":104,\"var3\":-14}}'", cmdLine);
    }

    [Fact]
    public static void Should_export_complex_req_correctly()
    {
        // GIVEN
        List<PororocaKeyValueParam> headers =
        [
            new(true, "Content-Language", "pl"),
            new(true, "Date", "2025-10-30")
        ];
        var body = PororocaHttpRequestBody.MakeRawContent("{\n\"prop\":10,\n\"Arr2\":\n[\n\"x\",\n\"y\",\n\"z\"\n]\n}", "application/json");
        var auth = PororocaRequestAuth.MakeClientCertificateAuth(PororocaRequestAuthClientCertificateType.Pem, "cert.pem", "privkey.pem", "SENHA123");
        PororocaHttpRequest req = new(string.Empty,
            HttpVersion: 3.0m,
            HttpMethod: "POST",
            Url: "http://www.pudim.com.br/api/endpoint",
            CustomAuth: auth,
            Headers: headers,
            Body: body);

        // WHEN
        string cmdLine = ExportAsCurlRequest(req, colScopedAuth: null);

        // THEN
        // JSON should be minified, preserving property names
        const string expectedCmd =
@"curl http://www.pudim.com.br/api/endpoint \
--http3 \
-X POST \
--cert-type PEM --cert cert.pem --key privkey.pem --pass SENHA123 \
--header 'Content-Language: pl' \
--header 'Date: 2025-10-30' \
--json '{""prop"":10,""Arr2"":[""x"",""y"",""z""]}'";

        Assert.Equal(expectedCmd, cmdLine);
    }
}