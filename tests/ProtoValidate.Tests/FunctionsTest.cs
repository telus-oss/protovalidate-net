using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Cel;
using Google.Protobuf;
using NUnit.Framework;
using ProtoValidate.Internal.Cel;

#nullable disable
namespace ProtoValidate.Tests;

[TestFixture]
public class FunctionsTest
{

    #region IsNan Tests

    [Test]
    public void IsNan_WithNanFloat_ReturnsTrue()
    {
        var result = Functions.IsNan(new object[] { float.NaN });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsNan_WithNanDouble_ReturnsTrue()
    {
        var result = Functions.IsNan(new object[] { double.NaN });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsNan_WithNormalFloat_ReturnsFalse()
    {
        var result = Functions.IsNan(new object[] { 1.5f });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsNan_WithNormalDouble_ReturnsFalse()
    {
        var result = Functions.IsNan(new object[] { 1.5 });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsNan_WithInvalidArgumentCount_ThrowsException()
    {
        Assert.Throws<CelExpressionParserException>(() => Functions.IsNan(new object[] { }));
        Assert.Throws<CelExpressionParserException>(() => Functions.IsNan(new object[] { 1.0, 2.0 }));
    }

    [Test]
    public void IsNan_WithInvalidType_ThrowsException()
    {
        Assert.Throws<CelNoSuchOverloadException>(() => Functions.IsNan(new object[] { "invalid" }));
    }

    #endregion

    #region IsInf Tests

    [Test]
    public void IsInf_WithPositiveInfinityFloat_ReturnsTrue()
    {
        var result = Functions.IsInf(new object[] { float.PositiveInfinity });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsInf_WithNegativeInfinityDouble_ReturnsTrue()
    {
        var result = Functions.IsInf(new object[] { double.NegativeInfinity });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsInf_WithNormalFloat_ReturnsFalse()
    {
        var result = Functions.IsInf(new object[] { 1.5f });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsInf_WithInvalidArgumentCount_ThrowsException()
    {
        Assert.Throws<CelExpressionParserException>(() => Functions.IsInf(new object[] { }));
    }

    #endregion

    #region IsIP Tests

    [Test]
    public void IsIP_WithValidIPv4_ReturnsTrue()
    {
        var result = Functions.IsIP(new object[] { "192.168.1.1" });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsIP_WithValidIPv6_ReturnsTrue()
    {
        var result = Functions.IsIP(new object[] { "2001:0db8:85a3:0000:0000:8a2e:0370:7334" });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsIP_WithIPv4AndVersion4_ReturnsTrue()
    {
        var result = Functions.IsIP(new object[] { "192.168.1.1", 4 });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsIP_WithIPv4AndVersion6_ReturnsFalse()
    {
        var result = Functions.IsIP(new object[] { "192.168.1.1", 6 });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsIP_WithBrackets_ReturnsFalse()
    {
        var result = Functions.IsIP(new object[] { "[192.168.1.1]" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsIP_WithInvalidIP_ReturnsFalse()
    {
        var result = Functions.IsIP(new object[] { "256.256.256.256" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsIP_WithInvalidVersion_ReturnsFalse()
    {
        var result = Functions.IsIP(new object[] { "192.168.1.1", 5 });
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region IsEmail Tests

    [Test]
    public void IsEmail_WithValidEmail_ReturnsTrue()
    {
        var result = Functions.IsEmail(new object[] { "test@example.com" });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsEmail_WithEmailContainingAngleBrackets_ReturnsFalse()
    {
        var result = Functions.IsEmail(new object[] { "test<@example.com" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsEmail_WithTooLongEmail_ReturnsFalse()
    {
        var longEmail = new string('a', 250) + "@example.com";
        var result = Functions.IsEmail(new object[] { longEmail });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsEmail_With_All_Characters_ReturnsTrue()
    {
        var emailWithAllChars = @"a0!#$%&'*+-/=?^_`{|}~@example.com";
        var result = Functions.IsEmail(new object[] { emailWithAllChars });
        Assert.That(result, Is.EqualTo(true));
    }
    [Test]
    public void IsEmail_WithInvalidFormat_ReturnsFalse()
    {
        var result = Functions.IsEmail(new object[] { "invalid-email" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsEmail_WithTrailingNewline_ReturnsFalse()
    {
        var result = Functions.IsEmail(new object[] { "test@example.com\n" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsEmail_WithTrailingCarriageReturn_ReturnsFalse()
    {
        var result = Functions.IsEmail(new object[] { "test@example.com\r" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsEmail_WithTrailingCarriageReturnNewline_ReturnsFalse()
    {
        var result = Functions.IsEmail(new object[] { "test@example.com\r\n" });
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region IsHostname Tests

    [Test]
    public void IsHostname_WithValidHostname_ReturnsTrue()
    {
        var result = Functions.IsHostname(new object[] { "example.com" });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsHostname_WithTooLongHostname_ReturnsFalse()
    {
        var longHostname = new string('a', 254);
        var result = Functions.IsHostname(new object[] { longHostname });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsHostname_WithAllDigits_ReturnsFalse()
    {
        var result = Functions.IsHostname(new object[] { "123.456.789" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsHostname_WithHyphenAtStart_ReturnsFalse()
    {
        var result = Functions.IsHostname(new object[] { "-example.com" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsHostname_WithTrailingDot_ReturnsTrue()
    {
        var result = Functions.IsHostname(new object[] { "example.com." });
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region IsHostAndPort Tests

    [Test]
    public void IsHostAndPort_WithValidHostAndPort_ReturnsTrue()
    {
        var result = Functions.IsHostAndPort(new object[] { "example.com:80", false });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsHostAndPort_WithHostOnlyAndPortNotRequired_ReturnsTrue()
    {
        var result = Functions.IsHostAndPort(new object[] { "example.com", false });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsHostAndPort_WithHostOnlyAndPortRequired_ReturnsFalse()
    {
        var result = Functions.IsHostAndPort(new object[] { "example.com", true });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsHostAndPort_WithIPv6AndPort_ReturnsTrue()
    {
        var result = Functions.IsHostAndPort(new object[] { "[::1]:80", false });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsHostAndPort_WithInvalidPort_ReturnsFalse()
    {
        var result = Functions.IsHostAndPort(new object[] { "example.com:99999", false });
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region IsUri Tests

    [Test]
    [TestCase(@"https://foo%2x")]
    [TestCase(@"foo:/nz/\u001f")]
    [TestCase(@"https://foo%c3x%96")]
    [TestCase(@"foo:nz/^")]
    [TestCase(@"https://foo%")]
    [TestCase(@"foo:^")]
    [TestCase(@"https://example.com:x")]
    [TestCase(@"https://foo@你好.com")]
    [TestCase(@"https://\u001f@example.com")]
    [TestCase(@"https://[::1%25foo%]")]
    [TestCase(@"https://example.com#%2x")]
    [TestCase(@"https://example.com?%2x")]
    [TestCase(@"https://example.com?^")]
    [TestCase(@"https://[::1%25]")]
    [TestCase(@"foo:%x")]
    [TestCase(@"foo://example.com/^")]
    [TestCase(@"https://]@example.com")]
    [TestCase(@"-foo://example.com")]
    [TestCase(@"foo%20bar://example.com")]
    [TestCase(@"https://%2x@example.com")]
    [TestCase(@"foo:\u001f")]
    [TestCase(@"https://@@example.com")]
    [TestCase(@"foo://example.com/%x")]
    [TestCase(@"https://example.com#^")]
    [TestCase(@"https://[2001::0370::7334]")]
    [TestCase(@"foo:/\u001f")]
    [TestCase(@"https://example.com#\u001f")]
    [TestCase(@"https://[::1%25foo%2x]")]
    [TestCase(@"foo:/nz/^")]
    [TestCase(@"foo:nz/\u001f")]
    [TestCase(@"https://2001:0db8:85a3:0000:0000:8a2e:0370:7334")]
    [TestCase(@"https://example.com##")]
    [TestCase(@"https://[@example.com")]
    [TestCase(@"foo:/^")]
    [TestCase(@"https://example.com?\u001f")]
    [TestCase(@"")]
    [TestCase(@" https://example.com")]
    [TestCase(@"https://[v1x]")]
    [TestCase(@"//example.com/foo")]
    [TestCase(@":foo://example.com")]
    [TestCase(@"foo^bar://example.com")]
    [TestCase(@"https://[::1%25foo%c3x%96]")]
    [TestCase(@"https://example.com:8a")]
    [TestCase(@"https://%@example.com")]
    [TestCase(@"https://\u001f.com")]
    [TestCase(@"foo://example.com/\u001f")]
    [TestCase(@" ")]
    [TestCase(@"https://^.com")]
    [TestCase(@"https://example.com: 1")]
    [TestCase(@"foo\u001fbar://example.com")]
    [TestCase(@"foo:/nz/%x")]
    [TestCase(@"https://[::1%eth0]")]
    [TestCase(@"https://^@example.com")]
    [TestCase(@"foo:/%x")]
    [TestCase(@"https://example.com#%")]
    [TestCase(@".foo://example.com")]
    [TestCase(@"foo:nz/%x")]
    [TestCase(@"https://example.com ")]
    [TestCase(@"./")]
    [TestCase(@"1foo://example.com")]
    public void IsUri_Invalid_ReturnsFalse(string uri)
    {
        var result = Functions.IsUri(new object[] { uri });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    [TestCase(@"https://!$&'()*+,;=@example.com")]
    [TestCase(@"https://[2001:0db8:85a3:0000:0000:8a2e:0370:7334]")]
    [TestCase(@"https://example.com:")]
    [TestCase(@"https://example.com#%c3x%96")]
    [TestCase(@"https://example.com?#frag")]
    [TestCase(@"https://joe@example.com/foo")]
    [TestCase(@"https://example.com#!$&'()*+,;=")]
    [TestCase(@"https:///@example.com")]
    [TestCase(@"https://[::1%25eth0]")]
    [TestCase(@"https://example.com:1")]
    [TestCase(@"foo://example.com/")]
    [TestCase(@"foo://example.com/%c3x%96")]
    [TestCase(@"foo:nz?q#f")]
    [TestCase(@"https://!$&'()*+,;=._~0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [TestCase(@"https://example.com:0")]
    [TestCase(@"foo:/%c3%96")]
    [TestCase(@"foo:/nz/%c3%96%c3")]
    [TestCase(@"foo:nz/a")]
    [TestCase(@"https://example.com/foo/bar?baz=quux#frag")]
    [TestCase(@"foo0123456789azAZ+-.://example.com")]
    [TestCase(@"https://[v1.x]")]
    [TestCase(@"foo:/nz/%c3x%96")]
    [TestCase(@"foo://example.com/%61%20%23")]
    [TestCase(@"ftp://example.com")]
    [TestCase(@"https://foo")]
    [TestCase(@"https://example.com:65536")]
    [TestCase(@"https://user:password@example.com")]
    [TestCase(@"https://#@example.com")]
    [TestCase(@"https://foo%c3%96")]
    [TestCase(@"foo:/nz/%61%20%23")]
    [TestCase(@"foo:nz/")]
    [TestCase(@"foo:%c3%96")]
    [TestCase(@"foo://example.com")]
    [TestCase(@"https://example.com/foo/bar/")]
    [TestCase(@"https://0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~!$&'()*+,;=::@example.com")]
    [TestCase(@"foo:nz/%61%20%23")]
    [TestCase(@"https://[v1234AF.x]")]
    [TestCase(@"https://[vF.-!$&'()*+,;=._~0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ]")]
    [TestCase(@"https://example.com:65535")]
    [TestCase(@"foo:/nz/a")]
    [TestCase(@"https://foo%61%20%23")]
    [TestCase(@"https://example.com/foo")]
    [TestCase(@"foo:@%20!$&()*+,;=0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~:")]
    [TestCase(@"foo://example.com/0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20")]
    [TestCase(@"foo://example.com/0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20")]
    [TestCase(@"https://example.com")]
    [TestCase(@"foo:/%c3x%96")]
    [TestCase(@"foo:nz/0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20")]
    [TestCase(@"https://example.com#%61%20%23")]
    [TestCase(@"https://0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~@example.com")]
    [TestCase(@"https://[::1%25foo%61%20%23]")]
    [TestCase(@"foo:/@%20!$&()*+,;=0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~:")]
    [TestCase(@"foo:nz//segment//segment/")]
    [TestCase(@"foo:")]
    [TestCase(@"foo:?q#f")]
    [TestCase(@"https://:")]
    [TestCase(@"foo://example.com/segment//segment/")]
    [TestCase(@"https://example.com?%c3x%96")]
    [TestCase(@"A://")]
    [TestCase(@"https://example.com?%61%20%23")]
    [TestCase(@"https://example.com?a=b&c&&=1&==")]
    [TestCase(@"https://:@example.com")]
    [TestCase(@"foo://example.com/a")]
    [TestCase(@"https://example.com#/?")]
    [TestCase(@"foo:/nz")]
    [TestCase(@"foo:nz/%c3x%96")]
    [TestCase(@"https://example.com?baz=quux")]
    [TestCase(@"https://example.com?;")]
    [TestCase(@"https://example.com/#0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"https://example.com/foo/bar")]
    [TestCase(@"https://127.0.0.1")]
    [TestCase(@"https://example.com?!$&'()*+,=")]
    [TestCase(@"https://user@example.com")]
    [TestCase(@"https://example.com:8080")]
    [TestCase(@"https://%c3%963@example.com")]
    [TestCase(@"foo:/nz/0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20")]
    [TestCase(@"foo:%c3x%96")]
    [TestCase(@"foo://example.com/%c3%96")]
    [TestCase(@"https://%61%20%23@example.com")]
    [TestCase(@"https://:8080")]
    [TestCase(@"foo:/nz?q#f")]
    [TestCase(@"foo:/%61%20%23")]
    [TestCase(@"foo:nz/%c3%96")]
    [TestCase(@"scheme0123456789azAZ+-.://userinfo0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~!$&'()*+,;=::@host!$&'()*+,;=._~0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ:0123456789/path0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20//foo/?query0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?#fragment0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"https://%c3x%963@example.com")]
    [TestCase(@"https://?@example.com")]
    [TestCase(@"https://256.0.0.1")]
    [TestCase(@"foo:/nz/")]
    [TestCase(@"foo:nz")]
    [TestCase(@"https://:::@example.com")]
    [TestCase(@"https://[::1%25foo%c3%96]")]
    [TestCase(@"https://example.com/foo/bar")]
    [TestCase(@"https://example.com?%c3%96%c3")]
    [TestCase(@"https://example.com?0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?")]
    [TestCase(@"https://example.com/foo")]
    [TestCase(@"foo:%61%20%23")]
    [TestCase(@"https://example.com?/?")]
    [TestCase(@"https://example.com#%c3%96")]
    [TestCase(@"foo:/nz//segment//segment/")]
    [TestCase(@"https://example.com?:@")]

    public void IsUri_Valid_ReturnsTrue(string uri)
    {
        var result = Functions.IsUri(new object[] { uri });
        Assert.That(result, Is.EqualTo(true));
    }
    #endregion

    #region IsUriRef Tests
    [Test]
    [TestCase(@":")]
    [TestCase(@"/nz/%x")]
    [TestCase(@"##")]
    [TestCase(@"/\u001f")]
    [TestCase(@"/%x")]
    [TestCase(@"/?%")]
    [TestCase(@"?%")]
    [TestCase(@"/nz/^")]
    [TestCase(@".##")]
    [TestCase(@"/foo/\u001f")]
    [TestCase(@"1foo://example.com")]
    [TestCase(@".#^")]
    [TestCase(@"#^")]
    [TestCase(@"//host#%")]
    [TestCase(@"/^")]
    [TestCase(@"/nz/\u001f")]
    [TestCase(@"?\u001f")]
    [TestCase(@"//host/\u001f")]
    [TestCase(@"%x")]
    [TestCase(@".?\u001f")]
    [TestCase(@" ./foo")]
    [TestCase(@".?^")]
    [TestCase(@"/#\u001f")]
    [TestCase(@"//host#^")]
    [TestCase(@"/?^")]
    [TestCase(@"./foo ")]
    [TestCase(@"./%x")]
    [TestCase(@".#%")]
    [TestCase(@"//host?\u001f")]
    [TestCase(@"//host##")]
    [TestCase(@"/##")]
    [TestCase(@"./foo/\u001f")]
    [TestCase(@"//host#\u001f")]
    [TestCase(@"\u001f")]
    [TestCase(@".?%")]
    [TestCase(@"#%")]
    [TestCase(@":")]
    [TestCase(@".#\u001f")]
    [TestCase(@"./\u001f")]
    [TestCase(@"//host/%x")]
    [TestCase(@"#\u001f")]
    [TestCase(@"^")]
    [TestCase(@"./^")]
    [TestCase(@" ")]
    [TestCase(@"//host?^")]
    [TestCase(@"/#^")]
    [TestCase(@"/#%")]
    [TestCase(@"//host?%")]
    [TestCase(@"/?\u001f")]
    [TestCase(@"?^")]


    public void IsUriRef_Invalid_ReturnsFalse(string uriRef)
    {
        var result = Functions.IsUriRef(new object[] { uriRef });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    [TestCase(@"//host/%61%20%23")]
    [TestCase(@"")]
    [TestCase(@"//[::1]")]
    [TestCase(@"#0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"//host//")]
    [TestCase(@"/nz")]
    [TestCase(@"/%c3%96")]
    [TestCase(@"./%c3%96")]
    [TestCase(@"")]
    [TestCase(@"%c3%96")]
    [TestCase(@"./%c3x%96")]
    [TestCase(@".?0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?")]
    [TestCase(@"//host/a/b/c/")]
    [TestCase(@"*")]
    [TestCase(@"//host#0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"/foo/bar?baz=quux")]
    [TestCase(@"./foo/bar")]
    [TestCase(@"./foo")]
    [TestCase(@"./0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20")]
    [TestCase(@"/:")]
    [TestCase(@"@%20!$&()*+,;=0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~")]
    [TestCase(@"//host/0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&()*+,;=:@")]
    [TestCase(@"./%61%20%23")]
    [TestCase(@"./a/b/c/")]
    [TestCase(@".?baz=quux")]
    [TestCase(@"./foo/bar?baz=quux")]
    [TestCase(@"//host/a/b/c")]
    [TestCase(@"//host/foo/bar#frag")]
    [TestCase(@"/%61%20%23")]
    [TestCase(@"//host?baz=quux")]
    [TestCase(@"./foo/bar?baz=quux#frag")]
    [TestCase(@"//host#frag")]
    [TestCase(@"/nz/")]
    [TestCase(@"%61%20%23")]
    [TestCase(@"./foo")]
    [TestCase(@"/?baz=quux")]
    [TestCase(@"//0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~!$&'()*+,;=::@example.com")]
    [TestCase(@"/@%20!$&()*+,;=0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~:")]
    [TestCase(@"./foo/bar#frag")]
    [TestCase(@"//host")]
    [TestCase(@"//host/%c3x%96")]
    [TestCase(@"//host?0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?")]
    [TestCase(@"/#0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"?0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?")]
    [TestCase(@"/foo/bar#frag")]
    [TestCase(@"/?0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?")]
    [TestCase(@"/nz/0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&()*+,;=:@")]
    [TestCase(@"//host/foo/bar?baz=quux")]
    [TestCase(@"/nz/%c3x%96")]
    [TestCase(@"//127.0.0.1")]
    [TestCase(@"/nz/%61%20%23")]
    [TestCase(@"./a/b/c")]
    [TestCase(@".///")]
    [TestCase(@"/%c3x%96")]
    [TestCase(@"/nz/%c3%96")]
    [TestCase(@".#0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"//host:8080")]
    [TestCase(@"//host/%c3%96")]
    [TestCase(@"/")]
    [TestCase(@"//host/")]
    [TestCase(@"/#frag")]
    [TestCase(@"./")]
    [TestCase(@".#frag")]
    [TestCase(@"//userinfo0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~!$&'()*+,;=::@host!$&'()*+,;=._~0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ:0123456789/path0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%20!$&'()*+,;=:@%20//foo/?query0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?#fragment0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-._~%20!$&'()*+,=;:@?/")]
    [TestCase(@"//host/foo?baz=quux#frag")]
    [TestCase(@"%c3x%96")]


    public void IsUriRef_Valid_ReturnsTrue(string uriRef)
    {
        var result = Functions.IsUriRef(new object[] { uriRef });
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Unique Tests

    [Test]
    public void Unique_WithUniqueItems_ReturnsTrue()
    {
        var list = new List<string> { "a", "b", "c" };
        var result = Functions.Unique(new object[] { list });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Unique_WithDuplicateItems_ReturnsFalse()
    {
        var list = new List<string> { "a", "b", "a" };
        var result = Functions.Unique(new object[] { list });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Unique_WithEmptyList_ReturnsTrue()
    {
        var list = new List<string>();
        var result = Functions.Unique(new object[] { list });
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region ByteString Functions Tests

    [Test]
    public void StartsWith_Bytes_WithMatchingPrefix_ReturnsTrue()
    {
        var bytes1 = ByteString.CopyFromUtf8("hello world");
        var bytes2 = ByteString.CopyFromUtf8("hello");
        var result = Functions.StartsWith_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void StartsWith_Bytes_WithNonMatchingPrefix_ReturnsFalse()
    {
        var bytes1 = ByteString.CopyFromUtf8("hello world");
        var bytes2 = ByteString.CopyFromUtf8("world");
        var result = Functions.StartsWith_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void StartsWith_Bytes_WithLongerPrefix_ReturnsFalse()
    {
        var bytes1 = ByteString.CopyFromUtf8("hi");
        var bytes2 = ByteString.CopyFromUtf8("hello");
        var result = Functions.StartsWith_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void EndsWith_Bytes_WithMatchingSuffix_ReturnsTrue()
    {
        var bytes1 = ByteString.CopyFromUtf8("hello world");
        var bytes2 = ByteString.CopyFromUtf8("world");
        var result = Functions.EndsWith_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void EndsWith_Bytes_WithNonMatchingSuffix_ReturnsFalse()
    {
        var bytes1 = ByteString.CopyFromUtf8("hello world");
        var bytes2 = ByteString.CopyFromUtf8("hello");
        var result = Functions.EndsWith_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Contains_Bytes_WithContainedSubstring_ReturnsTrue()
    {
        var bytes1 = ByteString.CopyFromUtf8("hello world");
        var bytes2 = ByteString.CopyFromUtf8("lo wo");
        var result = Functions.Contains_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Contains_Bytes_WithNonContainedSubstring_ReturnsFalse()
    {
        var bytes1 = ByteString.CopyFromUtf8("hello world");
        var bytes2 = ByteString.CopyFromUtf8("xyz");
        var result = Functions.Contains_Bytes(new object[] { bytes1, bytes2 });
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region IP Prefix Tests

    [Test]
    public void IsIPPrefix_WithValidIPv4Prefix_ReturnsTrue()
    {
        var result = Functions.IsIPPrefix(new object[] { "192.168.1.0/24" });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsIPPrefix_WithValidIPv6Prefix_ReturnsTrue()
    {
        var result = Functions.IsIPPrefix(new object[] { "2001:db8::/32" });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsIPPrefix_WithInvalidMask_ReturnsFalse()
    {
        var result = Functions.IsIPPrefix(new object[] { "192.168.1.0/33" });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsIPPrefix_WithVersionMismatch_ReturnsFalse()
    {
        var result = Functions.IsIPPrefixWithVersion(new object[] { "192.168.1.0/24", 6 });
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void IsIPPrefix_WithValidNetworkAddress_ReturnsTrue()
    {
        var result = Functions.IsIPPrefixWithVersion(new object[] { "192.168.1.0/24", 4, true });
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsIPPrefix_WithInvalidNetworkAddress_ReturnsFalse()
    {
        var result = Functions.IsIPPrefixWithVersion(new object[] { "192.168.1.1/24", 4, true });
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region TryParseStrictIPv4 Tests

    [Test]
    public void TryParseStrictIPv4_WithValidIPv4_ReturnsTrue()
    {
        var result = Functions.TryParseStrictIPv4("192.168.1.1", out var address);
        Assert.That(result, Is.True);
        Assert.That(address.ToString(), Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public void TryParseStrictIPv4_WithShorthandNotation_ReturnsFalse()
    {
        var result = Functions.TryParseStrictIPv4("127.1", out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseStrictIPv4_WithIPv6_ReturnsFalse()
    {
        var result = Functions.TryParseStrictIPv4("::1", out _);
        Assert.That(result, Is.False);
    }

    #endregion

    #region TryParseStrictIpv6 Tests

    [Test]
    public void TryParseStrictIpv6_WithValidIPv6_ReturnsTrue()
    {
        var result = Functions.TryParseStrictIpv6("2001:db8::1", out var address, out var zoneId);
        Assert.That(result, Is.True);
        Assert.That(address, Is.Not.Null);
        Assert.That(zoneId, Is.Null);
    }

    [Test]
    public void TryParseStrictIpv6_WithZoneId_ReturnsTrue()
    {
        var result = Functions.TryParseStrictIpv6("fe80::1%eth0", out var address, out var zoneId);
        Assert.That(result, Is.True);
        Assert.That(address, Is.Not.Null);
        Assert.That(zoneId, Is.EqualTo("eth0"));
    }

    [Test]
    public void TryParseStrictIpv6_WithEmptyZoneId_ReturnsFalse()
    {
        var result = Functions.TryParseStrictIpv6("fe80::1%", out _, out _);
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryParseStrictIpv6_WithIPv4_ReturnsFalse()
    {
        var result = Functions.TryParseStrictIpv6("192.168.1.1", out _, out _);
        Assert.That(result, Is.False);
    }

    #endregion

    #region Exception Tests

    [Test]
    public void IsHostAndPort_WithInvalidArgumentTypes_ThrowsException()
    {
        Assert.Throws<CelNoSuchOverloadException>(() =>
            Functions.IsHostAndPort(new object[] { 123, "invalid" }));
    }

    #endregion

}