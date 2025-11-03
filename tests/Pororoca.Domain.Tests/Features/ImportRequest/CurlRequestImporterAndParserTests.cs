using Pororoca.Domain.Features.Common;
using Pororoca.Domain.Features.Entities.Pororoca;
using Pororoca.Domain.Features.Entities.Pororoca.Http;
using Xunit;
using static Pororoca.Domain.Features.ImportRequest.CurlRequestImporter;

namespace Pororoca.Domain.Tests.Features.ImportRequest;

public static partial class CurlRequestImporterAndParserTests
{
    private const string testCurlSingleLineSimple =
@"curl http://localhost:5000/123e4567-e89b-12d3-a456-426614174000";

    private const string testCurlBashMultilineFull =
@"curl \
http://localhost:8008 \
-o out.json \
 -H 'Authorization: Bearer some-token'  \
 --http2 \
 --request  POST  \
           -6 \
--dns-ipv6-addr 2a04:4e42::561                                          \
 --header ""Content-Type: application/json"" \
--data '[{""id"": 123, ""status"": ""pending""}, {""id"": 456, ""status"": ""complete""}]' \
--json ""[{\""id\"": 123, \""status\"": \""pending\""}, {\""id\"": 456, \""status\"": \""complete\""}]"" \
  --data-binary 'some text \'single-quotes\''   \
--data-raw ""some text \""double-quotes\""""   ";

    private const string testCurlPowerShellMultiline =
@"curl http://localhost:5000/123e4567-e89b-12d3-a456-426614174000 `
  --request PUT `
  --header 'Content-Type: application/json; charset=utf-8' `
  -d ""{\""id\"":\""b52c031f-db5a-49e1-a965-bb695775d2a5\"",\""title\"":\""My shopping list\"",\""items\"":[{\""quantity\"":1,\""itemName\"":\""Rice 5kg\""},{\""quantity\"":2,\""itemName\"":\""Beans 1kg\""}]}""";

    private const string testCurlBashMultilineMalformatted =
@"CURL \
http://localhost:8008 \
-o out.json 
 -H 'Authorization: Bearer some-token'  \
 --http2 \ ";

    private const string testCurlSpecifiedUrl =
@"curl --url https://example.com";

    private const string testCurlUrlQuery =
@"curl --url-query name=val https://example.com";

    private const string testCurlUrlQueryMultiple =
@"curl --url-query name=val --url-query name2=val2 https://example.com";

    private const string testCurlUrlQueryMultipleSpecifiedUrl =
@"curl --url-query name=val --url https://example.com --url-query name2=val2";

    private const string testCurlBasicAuth =
@"curl -u name:password --basic https://example.com";

    private const string testCurlBearerAuth =
@"curl --oauth2-bearer ""mF_9.B5f-4.1JqM"" https://example.com";

    private const string testCurlWindowsNtlm =
@"curl -u user:secret --ntlm https://example.com";

    private const string testCurlCertificatePem =
@"curl --cert certfile.pem --cert-type PEM --key keyfile.pem https://example.com"; // PEM or P12

    private const string testCurlCertificatePkcs12 =
@"curl --cert /home/myuser/certs/certfile.p12 --cert-type P12 --pass pass123 https://example.com"; // PEM or P12

    private const string testCurlUploadFile =
@"curl -T ""img[1-1000].png"" https://example.com/";

    private const string testCurlUrlEncode1 =
@"curl --data-urlencode name=val https://example.com";

    private const string testCurlUrlEncode2 =
@"curl --data-urlencode =encodethis https://example.com";

    private const string testCurlUrlEncode3 =
@"curl --data-urlencode name@file https://example.com";

    private const string testCurlUrlEncode4 =
@"curl --data-urlencode @fileonly.jpg https://example.com";

    private const string testCurlUrlEncodeMultiple =
@"curl -d ""name=curl"" -d ""tool=cmdline"" -d @filename https://example.com";

    private const string testCurlFormData =
@"curl -F name=John -F shoesize=11 -F profile=@portrait.jpg -F ""web=@content.json;type=application/fhir+json"" https://example.com/upload.cgi";

    private const string testCurlUserAgentReferer =
@"curl https://example.com -A ""Mozilla/5.0 (X11; Linux x86_64; rv:60.0) Gecko/20100101 Firefox/81.0"" --referer ""https://fake.example""";

    private const string testCurlJsonRaw =
"curl --json '[1, 2, \"aaaa\", {\"k\": 123}]' https://example.com";

    private const string testCurlJsonFile =
"curl --json @myjsonfile.json https://example.com";

    private const string testCurlGetWithBody =
"curl --json '[]' -G https://example.com";

    #region PARSER

    [Fact]
    public static void Should_parse_curl_cmd_single_line_correctly()
    {
        // GIVEN, WHEN
        var kvs = ParseCurlCommandLineParams(testCurlSingleLineSimple);

        // THEN
        Assert.NotNull(kvs);
        var kv = Assert.Single(kvs);
        Assert.Equal(new("--url", "http://localhost:5000/123e4567-e89b-12d3-a456-426614174000"), kv);
    }

    [Fact]
    public static void Should_parse_curl_cmd_bash_multiline_single_double_quotes_correctly()
    {
        // GIVEN, WHEN
        var kvs = ParseCurlCommandLineParams(testCurlBashMultilineFull);

        // THEN
        Assert.NotNull(kvs);
        Assert.NotEmpty(kvs);
        Assert.Equal(new("-o", "out.json"), kvs[0]);
        Assert.Equal(new("-H", "Authorization: Bearer some-token"), kvs[1]);
        Assert.Equal(new("--http2", string.Empty), kvs[2]);
        Assert.Equal(new("--request", "POST"), kvs[3]);
        Assert.Equal(new("-6", string.Empty), kvs[4]);
        Assert.Equal(new("--dns-ipv6-addr", "2a04:4e42::561"), kvs[5]);
        Assert.Equal(new("--header", "Content-Type: application/json"), kvs[6]);
        Assert.Equal(new("--data", "[{\"id\": 123, \"status\": \"pending\"}, {\"id\": 456, \"status\": \"complete\"}]"), kvs[7]);
        Assert.Equal(new("--json", "[{\"id\": 123, \"status\": \"pending\"}, {\"id\": 456, \"status\": \"complete\"}]"), kvs[8]);
        Assert.Equal(new("--data-binary", "some text 'single-quotes'"), kvs[9]);
        Assert.Equal(new("--data-raw", "some text \"double-quotes\""), kvs[10]);
        Assert.Equal(new("--url", "http://localhost:8008"), kvs[11]);
    }

    [Fact]
    public static void Should_parse_curl_cmd_powershell_multiline_correctly()
    {
        // GIVEN, WHEN
        var kvs = ParseCurlCommandLineParams(testCurlPowerShellMultiline);

        // THEN
        Assert.NotNull(kvs);
        Assert.NotEmpty(kvs);
        Assert.Equal(4, kvs.Count);
        Assert.Equal(new("--request", "PUT"), kvs[0]);
        Assert.Equal(new("--header", "Content-Type: application/json; charset=utf-8"), kvs[1]);
        Assert.Equal(new("-d", "{\"id\":\"b52c031f-db5a-49e1-a965-bb695775d2a5\",\"title\":\"My shopping list\",\"items\":[{\"quantity\":1,\"itemName\":\"Rice 5kg\"},{\"quantity\":2,\"itemName\":\"Beans 1kg\"}]}"), kvs[2]);
        Assert.Equal(new("--url", "http://localhost:5000/123e4567-e89b-12d3-a456-426614174000"), kvs[3]);
    }

    [Fact]
    public static void Should_parse_curl_cmd_bash_multiline_malformatted_correctly_with_leniency()
    {
        // GIVEN, WHEN
        var kvs = ParseCurlCommandLineParams(testCurlBashMultilineMalformatted);

        // THEN
        Assert.NotNull(kvs);
        Assert.NotEmpty(kvs);
        Assert.Equal(4, kvs.Count);
        Assert.Equal(new("-o", "out.json"), kvs[0]);
        Assert.Equal(new("-H", "Authorization: Bearer some-token"), kvs[1]);
        Assert.Equal(new("--http2", string.Empty), kvs[2]);
        Assert.Equal(new("--url", "http://localhost:8008"), kvs[3]);
    }

    [Theory]
    [InlineData("https://pt.aliexpress.com/w/wholesale-Ferramentas.html?spm=a2g0o.productlist.allcategoriespc.32.67d7WyO3WyO3Nq&q=Ferramentas&s=qp_nw&osf=category_navigate&sg_search_params=&guide_trace=74d3409d-f41a-409d-a5dc-8e0f7612ef3a&scene_id=37749&searchBizScene=openSearch&recog_lang=pt&bizScene=mainSearch&guideModule=category_navigate_horizon&postCatIds=18%2C201768104%2C150401%2C100005769&scene=search&isFromCategory=y")]
    [InlineData("https://www.cse.wustl.edu/~jain/cis788-97/ftp/virtual_lans/index.html#:~:text=%20Why%20use%20VLAN%27s%3F%20%201%20Performance%20.,LAN%2C%20recabling%2C%20new%20station%20addressing%2C%20and...%20More%20")]
    [InlineData("https://web.stanford.edu/group/mota/education/Physics%2087N%20Final%20Projects/Group%20Gamma/gecko.htm")]
    public static void Should_parse_complex_urls_on_curl_cmd_line(string url)
    {
        // GIVEN
        string cmdLine =
@$"curl \
{url} \
-o out.json \
 --http2";

        // WHEN
        var kvs = ParseCurlCommandLineParams(cmdLine);

        // THEN
        Assert.NotNull(kvs);
        Assert.NotEmpty(kvs);
        Assert.Equal(3, kvs.Count);
        Assert.Equal(new("-o", "out.json"), kvs[0]);
        Assert.Equal(new("--http2", string.Empty), kvs[1]);
        Assert.Equal(new("--url", url), kvs[2]);
    }

    #endregion

    #region IMPORT

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public static void Should_not_import_empty_curl_cmd(string? cmd)
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(cmd);

        // THEN
        Assert.Null(req);
    }

    [Fact]
    public static void Should_not_import_cmd_without_the_word_curl()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest("carl http://www.pudim.com.br/index.html");

        // THEN
        Assert.Null(req);
    }

    [Fact]
    public static void Should_import_curl_cmd_single_line_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlSingleLineSimple);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("http://localhost:5000/123e4567-e89b-12d3-a456-426614174000", req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Fact]
    public static void Should_import_curl_cmd_multiline_with_body_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlPowerShellMultiline);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("PUT", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("http://localhost:5000/123e4567-e89b-12d3-a456-426614174000", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.Raw, req.Body.Mode);
        Assert.Equal("application/json; charset=utf-8", req.Body.ContentType);
        Assert.Equal("{\"id\":\"b52c031f-db5a-49e1-a965-bb695775d2a5\",\"title\":\"My shopping list\",\"items\":[{\"quantity\":1,\"itemName\":\"Rice 5kg\"},{\"quantity\":2,\"itemName\":\"Beans 1kg\"}]}", req.Body.RawContent);
        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Fact]
    public static void Should_import_curl_cmd_http2_and_header_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlBashMultilineMalformatted);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(2.0m, req.HttpVersion);
        Assert.Equal("http://localhost:8008", req.Url);

        Assert.NotNull(req.Headers);
        var h = Assert.Single(req.Headers);
        Assert.True(h.Enabled);
        Assert.Equal("Authorization", h.Key);
        Assert.Equal("Bearer some-token", h.Value);

        Assert.Null(req.Body);
        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Theory]
    [InlineData(1.0, "https://example.com", "curl -0 https://example.com")]
    [InlineData(1.0, "https://example.com", "curl --http1.0 \"https://example.com\"")]
    [InlineData(1.1, "https://example.com", "curl --http1.1 'https://example.com'")]
    [InlineData(2.0, "http://example.com", "curl --http2 http://example.com")]
    [InlineData(2.0, "http://example.com", "curl --http2-prior-knowledge \"http://example.com\"")]
    [InlineData(3.0, "http://example.com", "curl --http3 'http://example.com'")]
    [InlineData(3.0, "https://example.com", "curl --http3-only https://example.com")]
    public static void Should_import_curl_cmd_http_versions_correctly(decimal expectedHttpVersion, string expectedUrl, string cmd)
    {
        // This test also validates parsing of valueless flags 
        // located before an URL, that is at the end of the cmd

        // GIVEN, WHEN
        var req = ImportCurlRequest(cmd);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(expectedHttpVersion, req.HttpVersion);
        Assert.Equal(expectedUrl, req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Theory]
    [InlineData("https://example.com", testCurlSpecifiedUrl)]
    [InlineData("https://example.com?name=val", testCurlUrlQuery)]
    [InlineData("https://example.com?name=val&name2=val2", testCurlUrlQueryMultiple)]
    [InlineData("https://example.com?name=val&name2=val2", testCurlUrlQueryMultipleSpecifiedUrl)]
    public static void Should_import_curl_cmd_url_query_params_and_specified_url_correctly(string expectedUrl, string cmd)
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(cmd);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal(expectedUrl, req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Fact]
    public static void Should_import_curl_cmd_basic_auth_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlBasicAuth);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.ResponseCaptures);
        Assert.NotNull(req.CustomAuth);
        Assert.Equal(PororocaRequestAuthMode.Basic, req.CustomAuth.Mode);
        Assert.Equal("name", req.CustomAuth.BasicAuthLogin);
        Assert.Equal("password", req.CustomAuth.BasicAuthPassword);
    }

    [Fact]
    public static void Should_import_curl_cmd_bearer_auth_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlBearerAuth);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.ResponseCaptures);
        Assert.NotNull(req.CustomAuth);
        Assert.Equal(PororocaRequestAuthMode.Bearer, req.CustomAuth.Mode);
        Assert.Equal("mF_9.B5f-4.1JqM", req.CustomAuth.BearerToken);
    }

    [Fact]
    public static void Should_import_curl_cmd_windows_auth_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlWindowsNtlm);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.ResponseCaptures);
        Assert.NotNull(req.CustomAuth);
        Assert.Equal(PororocaRequestAuthMode.Windows, req.CustomAuth.Mode);
        Assert.NotNull(req.CustomAuth.Windows);
        Assert.Equal("user", req.CustomAuth.Windows.Login);
        Assert.Equal("secret", req.CustomAuth.Windows.Password);
    }

    [Fact]
    public static void Should_import_curl_cmd_client_certificate_PEM_auth_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlCertificatePem);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.ResponseCaptures);
        Assert.NotNull(req.CustomAuth);
        Assert.Equal(PororocaRequestAuthMode.ClientCertificate, req.CustomAuth.Mode);
        Assert.NotNull(req.CustomAuth.ClientCertificate);
        Assert.Equal(PororocaRequestAuthClientCertificateType.Pem, req.CustomAuth.ClientCertificate.Type);
        Assert.Equal("certfile.pem", req.CustomAuth.ClientCertificate.CertificateFilePath);
        Assert.Equal("keyfile.pem", req.CustomAuth.ClientCertificate.PrivateKeyFilePath);
    }

    [Fact]
    public static void Should_import_curl_cmd_client_certificate_PKCS12_auth_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlCertificatePkcs12);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.Null(req.Body);
        Assert.Null(req.ResponseCaptures);
        Assert.NotNull(req.CustomAuth);
        Assert.Equal(PororocaRequestAuthMode.ClientCertificate, req.CustomAuth.Mode);
        Assert.NotNull(req.CustomAuth.ClientCertificate);
        Assert.Equal(PororocaRequestAuthClientCertificateType.Pkcs12, req.CustomAuth.ClientCertificate.Type);
        Assert.Equal("/home/myuser/certs/certfile.p12", req.CustomAuth.ClientCertificate.CertificateFilePath);
        Assert.Equal("pass123", req.CustomAuth.ClientCertificate.FilePassword);
    }

    [Fact]
    public static void Should_import_curl_cmd_upload_file_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUploadFile);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("PUT", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com/", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.File, req.Body.Mode);
        Assert.Equal("img[1-1000].png", req.Body.FileSrcPath);
        Assert.Equal("image/png", req.Body.ContentType);
    }

    [Fact]
    public static void Should_import_curl_cmd_url_encoded_body_1_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUrlEncode1);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.Raw, req.Body.Mode);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForText, req.Body.ContentType);
        Assert.Equal("name=val", req.Body.RawContent);
    }

    [Fact]
    public static void Should_import_curl_cmd_url_encoded_body_2_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUrlEncode2);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.Raw, req.Body.Mode);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForText, req.Body.ContentType);
        Assert.Equal("=encodethis", req.Body.RawContent);
    }

    [Fact]
    public static void Should_import_curl_cmd_url_encoded_body_3_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUrlEncode3);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.Raw, req.Body.Mode);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForText, req.Body.ContentType);
        Assert.Equal("name@file", req.Body.RawContent);
    }

    [Fact]
    public static void Should_import_curl_cmd_url_encoded_body_4_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUrlEncode4);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.File, req.Body.Mode);
        Assert.Equal("fileonly.jpg", req.Body.FileSrcPath);
        Assert.Equal("image/jpeg", req.Body.ContentType);
    }

    [Fact]
    public static void Should_import_curl_cmd_url_encoded_multiple_params_body_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUrlEncodeMultiple);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.UrlEncoded, req.Body.Mode);
        Assert.NotNull(req.Body.UrlEncodedValues);
        Assert.Equal(3, req.Body.UrlEncodedValues.Count);

        Assert.True(req.Body.UrlEncodedValues[0].Enabled);
        Assert.Equal("name", req.Body.UrlEncodedValues[0].Key);
        Assert.Equal("curl", req.Body.UrlEncodedValues[0].Value);

        Assert.True(req.Body.UrlEncodedValues[1].Enabled);
        Assert.Equal("tool", req.Body.UrlEncodedValues[1].Key);
        Assert.Equal("cmdline", req.Body.UrlEncodedValues[1].Value);

        Assert.True(req.Body.UrlEncodedValues[2].Enabled);
        Assert.Equal("@filename", req.Body.UrlEncodedValues[2].Key);
        Assert.Equal(string.Empty, req.Body.UrlEncodedValues[2].Value);
    }

    [Fact]
    public static void Should_import_curl_cmd_form_data_multiple_params_body_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlFormData);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com/upload.cgi", req.Url);
        Assert.Null(req.Headers);
        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.FormData, req.Body.Mode);
        Assert.NotNull(req.Body.FormDataValues);
        Assert.Equal(4, req.Body.FormDataValues.Count);

        Assert.True(req.Body.FormDataValues[0].Enabled);
        Assert.Equal(PororocaHttpRequestFormDataParamType.Text, req.Body.FormDataValues[0].Type);
        Assert.Equal("name", req.Body.FormDataValues[0].Key);
        Assert.Equal("John", req.Body.FormDataValues[0].TextValue);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForText, req.Body.FormDataValues[0].ContentType);

        Assert.True(req.Body.FormDataValues[1].Enabled);
        Assert.Equal(PororocaHttpRequestFormDataParamType.Text, req.Body.FormDataValues[1].Type);
        Assert.Equal("shoesize", req.Body.FormDataValues[1].Key);
        Assert.Equal("11", req.Body.FormDataValues[1].TextValue);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForText, req.Body.FormDataValues[1].ContentType);

        Assert.True(req.Body.FormDataValues[2].Enabled);
        Assert.Equal(PororocaHttpRequestFormDataParamType.File, req.Body.FormDataValues[2].Type);
        Assert.Equal("profile", req.Body.FormDataValues[2].Key);
        Assert.Equal("portrait.jpg", req.Body.FormDataValues[2].FileSrcPath);
        Assert.Equal("image/jpeg", req.Body.FormDataValues[2].ContentType);

        Assert.True(req.Body.FormDataValues[3].Enabled);
        Assert.Equal(PororocaHttpRequestFormDataParamType.File, req.Body.FormDataValues[3].Type);
        Assert.Equal("web", req.Body.FormDataValues[3].Key);
        Assert.Equal("content.json", req.Body.FormDataValues[3].FileSrcPath);
        Assert.Equal("application/fhir+json", req.Body.FormDataValues[3].ContentType);
    }

    [Fact]
    public static void Should_import_curl_cmd_user_agent_and_referer_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlUserAgentReferer);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);

        Assert.NotNull(req.Headers);
        Assert.Equal(2, req.Headers.Count);
        Assert.True(req.Headers[0].Enabled);
        Assert.Equal("User-Agent", req.Headers[0].Key);
        Assert.Equal("Mozilla/5.0 (X11; Linux x86_64; rv:60.0) Gecko/20100101 Firefox/81.0", req.Headers[0].Value);
        Assert.True(req.Headers[1].Enabled);
        Assert.Equal("Referer", req.Headers[1].Key);
        Assert.Equal("https://fake.example", req.Headers[1].Value);

        Assert.Null(req.Body);
        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Fact]
    public static void Should_import_curl_cmd_json_raw_post_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlJsonRaw);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);

        Assert.NotNull(req.Headers);
        var h = Assert.Single(req.Headers);
        Assert.True(h.Enabled);
        Assert.Equal("Accept", h.Key);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForJson, h.Value);

        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.Raw, req.Body.Mode);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForJson, req.Body.ContentType);
        Assert.Equal("[1, 2, \"aaaa\", {\"k\": 123}]", req.Body.RawContent);

        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Fact]
    public static void Should_import_curl_cmd_json_file_post_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlJsonFile);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("POST", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);

        Assert.NotNull(req.Headers);
        var h = Assert.Single(req.Headers);
        Assert.True(h.Enabled);
        Assert.Equal("Accept", h.Key);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForJson, h.Value);

        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.File, req.Body.Mode);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForJson, req.Body.ContentType);
        Assert.Equal("myjsonfile.json", req.Body.FileSrcPath);

        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    [Fact]
    public static void Should_import_curl_cmd_get_with_body_correctly()
    {
        // GIVEN, WHEN
        var req = ImportCurlRequest(testCurlGetWithBody);

        // THEN
        Assert.NotNull(req);
        Assert.Equal("GET", req.HttpMethod);
        Assert.Equal(1.1m, req.HttpVersion);
        Assert.Equal("https://example.com", req.Url);

        Assert.NotNull(req.Headers);
        var h = Assert.Single(req.Headers);
        Assert.True(h.Enabled);
        Assert.Equal("Accept", h.Key);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForJson, h.Value);

        Assert.NotNull(req.Body);
        Assert.Equal(PororocaHttpRequestBodyMode.Raw, req.Body.Mode);
        Assert.Equal(MimeTypesDetector.DefaultMimeTypeForJson, req.Body.ContentType);
        Assert.Equal("[]", req.Body.RawContent);

        Assert.Null(req.CustomAuth);
        Assert.Null(req.ResponseCaptures);
    }

    #endregion
}