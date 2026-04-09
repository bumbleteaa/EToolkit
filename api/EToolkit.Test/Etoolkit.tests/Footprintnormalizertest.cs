using EToolkit.Application;
using Xunit;

namespace EToolkit.Tests.Application;

/// <summary>
/// Unit test untuk FootprintNormalizer.
///
/// Strategi: karena FootprintNormalizer adalah pure class tanpa dependency
/// eksternal, semua test bisa menggunakan instance nyata tanpa mock.
/// Setiap test group mencakup satu "rule" dari NormalizeFootprint().
/// </summary>
public class FootprintNormalizerTests
{
    // SUT — System Under Test
    private readonly FootprintNormalizer _sut = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Rule 0: Input kosong / whitespace / null
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeFootprint_EmptyOrWhitespace_ReturnsUnknown(string footprint)
    {
        var result = _sut.NormalizeFootprint(footprint);

        Assert.Equal(NormalizedKind.Unknown, result.Kind);
        // Key dan Canonical harus string kosong, bukan null atau throw
        Assert.Equal(string.Empty, result.Key);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rule 2: Passive component — dikenali dari digit 4-karakter (atau 3 + pad)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("0201")]
    [InlineData("0402")]
    [InlineData("0603")]
    [InlineData("0805")]
    [InlineData("1206")]
    [InlineData("1210")]
    [InlineData("2010")]
    [InlineData("2512")]
    public void NormalizeFootprint_ExactPassiveSize_ReturnsStandardPackage(string footprint)
    {
        var result = _sut.NormalizeFootprint(footprint);

        Assert.Equal(NormalizedKind.StandardPackage, result.Kind);
        Assert.Equal("PASSIVE", result.Family);
        // Canonical harus tepat = size string, bukan raw yang mungkin mengandung noise
        Assert.Equal(footprint, result.Canonical);
    }

    [Fact]
    public void NormalizeFootprint_ThreeDigitPassive_PadsAndRecognizes()
    {
        // "201" → digit extraction → "201" (3 char) → pad kiri → "0201" → ada di set → StandardPackage
        // Ini penting: beberapa CAD tool export footprint tanpa leading zero
        var result = _sut.NormalizeFootprint("201");

        Assert.Equal(NormalizedKind.StandardPackage, result.Kind);
        Assert.Equal("0201", result.Canonical);
    }

    [Fact]
    public void NormalizeFootprint_UnknownFourDigit_ReturnsUnknown()
    {
        // "9999" punya 4 digit tapi tidak ada di tabel passive — harus Unknown
        var result = _sut.NormalizeFootprint("9999");

        Assert.Equal(NormalizedKind.Unknown, result.Kind);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rule 3: Placeable family — SOT, SOIC, QFN, TSSOP, dll.
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SOT23-5", "SOT")]
    [InlineData("SOT23", "SOT")]
    [InlineData("SOIC8", "SOIC")]
    [InlineData("SOIC-16", "SOIC")]
    [InlineData("QFN32", "QFN")]
    [InlineData("QFP44", "QFP")]
    [InlineData("LQFP64", "LQFP")]
    [InlineData("TQFP100", "TQFP")]
    [InlineData("TSSOP16", "TSSOP")]
    [InlineData("SSOP20", "SSOP")]
    [InlineData("DFN8", "DFN")]
    [InlineData("BGA256", "BGA")]
    [InlineData("DO214AC", "DO")]
    [InlineData("SM4001", "SM")]
    public void NormalizeFootprint_PlaceableFamily_ReturnsStandardPackage(string footprint, string expectedFamily)
    {
        var result = _sut.NormalizeFootprint(footprint);

        Assert.Equal(NormalizedKind.StandardPackage, result.Kind);
        Assert.Equal(expectedFamily, result.Family);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rule 3b: Non-placeable family — CONN, HDR, TESTPOINT, FIDUCIAL, dll.
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("CONN10", "CONN")]
    [InlineData("CON4", "CON")]
    [InlineData("HDR2X10", "HDR")]
    [InlineData("HEADER6", "HEADER")]
    [InlineData("TESTPOINT", "TESTPOINT")]
    [InlineData("TP1", "TP")]
    [InlineData("FIDUCIAL", "FIDUCIAL")]
    [InlineData("FID1", "FID")]
    [InlineData("MECH4", "MECH")]
    [InlineData("HOLE3MM", "HOLE")]
    [InlineData("MOUNT4", "MOUNT")]
    [InlineData("DNP", "DNP")]   // normalizer level — RecordFilteringService reject lebih awal
    public void NormalizeFootprint_NonPlaceableFamily_ReturnsNonPlaceable(string footprint, string expectedFamily)
    {
        var result = _sut.NormalizeFootprint(footprint);

        Assert.Equal(NormalizedKind.NonPlaceable, result.Kind);
        Assert.Equal(expectedFamily, result.Family);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rule 4: Generic keyword — RES, CAP, IND, LED
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("RES100R", "RES")]   // "100R" → digit "100" → 3 char → pad → "0100" → tidak ada di set
    [InlineData("CAP10UF", "CAP")]   // digit "10" → 2 char → tidak valid
    [InlineData("IND100NH", "IND")]   // digit "100" → pad → "0100" → tidak ada di set
    [InlineData("LED5MM", "LED")]   // digit "5" → 1 char → tidak valid
    public void NormalizeFootprint_GenericKeyword_ReturnsGenericFootprint(string footprint, string expectedFamily)
    {
        var result = _sut.NormalizeFootprint(footprint);

        Assert.Equal(NormalizedKind.GenericFootprint, result.Kind);
        Assert.Equal(expectedFamily, result.Family);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unknown — tidak cocok di mana pun
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("XYZABC123")]   // prefix tidak dikenal
    [InlineData("CUSTOM_PKG")]  // prefix CUSTOM tidak ada di set manapun
    [InlineData("12345678")]    // murni digit, bukan passive 4-char
    public void NormalizeFootprint_UnrecognizedFootprint_ReturnsUnknown(string footprint)
    {
        var result = _sut.NormalizeFootprint(footprint);

        Assert.Equal(NormalizedKind.Unknown, result.Kind);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Properti struktural — berlaku untuk semua kasus
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NormalizeFootprint_RawFieldAlwaysPreservesInput()
    {
        // Raw harus identik dengan input asli, termasuk casing dan karakter khusus —
        // ini penting karena Raw digunakan untuk logging dan display ke operator.
        const string raw = "SOT-23/5";
        var result = _sut.NormalizeFootprint(raw);

        Assert.Equal(raw, result.Raw);
    }

    [Fact]
    public void NormalizeFootprint_KeyIsAlwaysAlphanumericUppercase()
    {
        // Key digunakan sebagai lookup key → harus bebas dari noise karakter
        var result = _sut.NormalizeFootprint("sot-23/5");

        Assert.Equal("SOT235", result.Key);
    }

    [Fact]
    public void NormalizeFootprint_KeyIsCaseNormalized()
    {
        // "soic8" dan "SOIC8" harus menghasilkan key yang sama
        var lower = _sut.NormalizeFootprint("soic8");
        var upper = _sut.NormalizeFootprint("SOIC8");

        Assert.Equal(lower.Key, upper.Key);
        Assert.Equal(lower.Kind, upper.Kind);
    }
}