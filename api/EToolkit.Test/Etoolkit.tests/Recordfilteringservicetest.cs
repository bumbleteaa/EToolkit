using EToolkit.Application;
using EToolkit.Infrastructure;
using Moq;
using Xunit;

namespace EToolkit.Tests.Application;

/// <summary>
/// Unit test untuk RecordFilteringService.
///
/// Strategi: IFootprintNormalizer dan IRecordIssueCollector di-mock,
/// sehingga setiap test fokus murni pada logika klasifikasi di Classify()
/// tanpa bergantung pada implementasi normalizer yang konkret.
///
/// Artinya: test di sini membuktikan "jika normalizer bilang X, maka
/// FilteringService harus memutuskan Y" — bukan menguji normalisasi itu sendiri
/// (itu urusan FootprintNormalizerTests).
/// </summary>
public class RecordFilteringServiceTests
{
    private readonly Mock<IFootprintNormalizer> _normalizer = new(MockBehavior.Strict);
    private readonly Mock<IRecordIssueCollector> _collector = new();
    private readonly RecordFilteringService _sut;

    public RecordFilteringServiceTests()
    {
        _sut = new RecordFilteringService(_normalizer.Object, _collector.Object);
    }

    // ─── Builder helper ────────────────────────────────────────────────────────
    // Satu tempat untuk membuat row valid secara default; test tinggal override
    // field yang relevan — mengurangi noise setup di tiap test.

    private static CsvComponentPlacementRow Row(
        string? footprint = "0603",
        string? value = "100R",
        string? desc = null,
        string? name = "R1",
        string? side = "Top") =>
        new()
        {
            Footprint = footprint,
            Value = value,
            Desc = desc,
            Name = name,
            Side = side,
        };

    // Setup mock normalizer untuk footprint tertentu, mengembalikan NormalizedKind yang diinginkan.
    private void SetupNormalizer(string footprint, NormalizedKind kind, string? family = null)
    {
        var key = new string(footprint.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        _normalizer
            .Setup(n => n.NormalizeFootprint(footprint))
            .Returns(new NormalizedFootprint(footprint, key, key, kind, family));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Early-exit: EMPTY_VALUE — footprint kosong / whitespace / null
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ClassifyRecords_EmptyFootprint_RejectsBeforeNormalization(string? footprint)
    {
        // Normalizer tidak boleh dipanggil sama sekali untuk kasus ini —
        // kalau dipanggil, MockBehavior.Strict akan melempar exception.
        var result = _sut.ClassifyRecords(new[] { Row(footprint: footprint) }).Single();

        Assert.Equal(RowStatus.Rejected, result.Status);
        Assert.Equal("EMPTY_VALUE", result.RejectCode);
        // Verifikasi eksplisit bahwa normalizer tidak disentuh
        _normalizer.VerifyNoOtherCalls();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Early-exit: DNP — lebih prioritas dari normalisasi
    // ─────────────────────────────────────────────────────────────────────────

    // Keempat field yang dicek untuk DNP harus masing-masing bisa memicu reject.
    [Theory]
    [InlineData("DNP", null, null, null)]   // Value = DNP
    [InlineData(null, "DNP", null, null)]   // Desc  = DNP
    [InlineData(null, null, "DNP", null)]   // Footprint = DNP (tapi ditangkap sebelum empty-check? Tidak — empty-check dulu. Footprint "DNP" bukan empty.)
    [InlineData(null, null, null, "DNP")]  // Name  = DNP
    public void ClassifyRecords_DnpInAnyField_RejectsWithDnpCode(
        string? value, string? desc, string? footprintOverride, string? name)
    {
        // Untuk kasus footprint override = "DNP", kita set row dengan footprint itu.
        // Untuk kasus lain, footprint = "0603" agar tidak terkena EMPTY_VALUE.
        var fp = footprintOverride ?? "0603";
        var row = Row(footprint: fp, value: value ?? "100R", desc: desc, name: name ?? "R1");

        var result = _sut.ClassifyRecords(new[] { row }).Single();

        Assert.Equal(RowStatus.Rejected, result.Status);
        Assert.Equal("DNP", result.RejectCode);
        // Normalizer tidak boleh dipanggil — DNP adalah veto tanpa syarat
        _normalizer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("dnp")]
    [InlineData("Dnp")]
    [InlineData("DNP")]
    public void ClassifyRecords_DnpIsCaseInsensitive(string dnpValue)
    {
        var result = _sut.ClassifyRecords(new[] { Row(value: dnpValue) }).Single();

        Assert.Equal(RowStatus.Rejected, result.Status);
        Assert.Equal("DNP", result.RejectCode);
    }

    [Fact]
    public void ClassifyRecords_ValueContainsDnpAsSubstring_DoesNotReject()
    {
        // "DNPX" atau "ADNP" bukan DNP — ContainsDnp menggunakan Equals, bukan Contains
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");
        var row = Row(value: "DNPX");

        var result = _sut.ClassifyRecords(new[] { row }).Single();

        // Harus lolos ke normalisasi, bukan tertolak oleh DNP guard
        Assert.Equal(RowStatus.Accepted, result.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Klasifikasi berbasis hasil normalizer
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClassifyRecords_StandardPackage_AcceptsRow()
    {
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");

        var result = _sut.ClassifyRecords(new[] { Row(footprint: "0603") }).Single();

        Assert.Equal(RowStatus.Accepted, result.Status);
        Assert.Null(result.RejectCode);           // Accepted tidak punya kode
        Assert.NotNull(result.Normalized);        // Normalized harus ada
    }

    [Fact]
    public void ClassifyRecords_NonPlaceable_RejectsWithNonPlaceableCode()
    {
        SetupNormalizer("CONN10", NormalizedKind.NonPlaceable, "CONN");

        var result = _sut.ClassifyRecords(new[] { Row(footprint: "CONN10") }).Single();

        Assert.Equal(RowStatus.Rejected, result.Status);
        Assert.Equal("NON_PLACEABLE", result.RejectCode);
    }

    [Fact]
    public void ClassifyRecords_GenericFootprint_ReturnsUnknownWithCode()
    {
        // GenericFootprint → operator perlu memutuskan secara manual apakah mau include
        SetupNormalizer("RES0402", NormalizedKind.GenericFootprint, "RES");

        var result = _sut.ClassifyRecords(new[] { Row(footprint: "RES0402") }).Single();

        Assert.Equal(RowStatus.Unknown, result.Status);
        Assert.Equal("GENERIC_FOOTPRINT", result.RejectCode);
    }

    [Fact]
    public void ClassifyRecords_UnknownFootprint_ReturnsUnknownWithCode()
    {
        SetupNormalizer("XYZABC", NormalizedKind.Unknown);

        var result = _sut.ClassifyRecords(new[] { Row(footprint: "XYZABC") }).Single();

        Assert.Equal(RowStatus.Unknown, result.Status);
        Assert.Equal("UNKNOWN_FOOTPRINT", result.RejectCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FilteredRecord — thin wrapper yang hanya mengembalikan Accepted
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FilteredRecord_ExcludesRejectedAndUnknownRows()
    {
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");
        SetupNormalizer("CONN10", NormalizedKind.NonPlaceable, "CONN");
        SetupNormalizer("XYZABC", NormalizedKind.Unknown);

        var rows = new[]
        {
            Row(footprint: "0603",   name: "R1"),   // → Accepted
            Row(footprint: "CONN10", name: "J1"),   // → Rejected
            Row(footprint: "XYZABC", name: "U1"),   // → Unknown
            Row(footprint: "0603",   name: "C1"),   // → Accepted
        };

        var filtered = _sut.FilteredRecord(rows).ToList();

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, r => Assert.Equal("0603", r.Footprint));
        // Hanya R1 dan C1 yang lolos
        Assert.Contains(filtered, r => r.Name == "R1");
        Assert.Contains(filtered, r => r.Name == "C1");
    }

    [Fact]
    public void FilteredRecord_WhenAllRejected_ReturnsEmptyList()
    {
        SetupNormalizer("CONN10", NormalizedKind.NonPlaceable, "CONN");

        var rows = Enumerable.Range(1, 5).Select(i => Row(footprint: "CONN10", name: $"J{i}"));
        var filtered = _sut.FilteredRecord(rows).ToList();

        Assert.Empty(filtered);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ClassifyRecords — semua baris dikembalikan (Accepted, Rejected, Unknown)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClassifyRecords_ReturnsAllRowsRegardlessOfStatus()
    {
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");
        SetupNormalizer("CONN10", NormalizedKind.NonPlaceable, "CONN");
        SetupNormalizer("XYZABC", NormalizedKind.Unknown);

        var rows = new[]
        {
            Row(footprint: "0603"),   // Accepted
            Row(footprint: "CONN10"), // Rejected
            Row(footprint: "XYZABC"), // Unknown
        };

        var classified = _sut.ClassifyRecords(rows).ToList();

        Assert.Equal(3, classified.Count);
        Assert.Single(classified, r => r.Status == RowStatus.Accepted);
        Assert.Single(classified, r => r.Status == RowStatus.Rejected);
        Assert.Single(classified, r => r.Status == RowStatus.Unknown);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Integrasi dengan IRecordIssueCollector
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClassifyRecords_CallsCollectorForEveryRow()
    {
        // Collector.Report harus dipanggil tepat sekali per row — tidak lebih, tidak kurang.
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");

        var rows = Enumerable.Range(1, 7).Select(i => Row(footprint: "0603", name: $"R{i}")).ToArray();
        _ = _sut.ClassifyRecords(rows).ToList(); // materialize agar yield berjalan

        _collector.Verify(c => c.Report(It.IsAny<AnnotatedRow>()), Times.Exactly(7));
    }

    [Fact]
    public void ClassifyRecords_CollectorReceivesCorrectAnnotatedRow()
    {
        SetupNormalizer("CONN10", NormalizedKind.NonPlaceable, "CONN");
        var row = Row(footprint: "CONN10", name: "J1");

        AnnotatedRow? captured = null;
        _collector
            .Setup(c => c.Report(It.IsAny<AnnotatedRow>()))
            .Callback<AnnotatedRow>(a => captured = a);

        _ = _sut.ClassifyRecords(new[] { row }).ToList();

        Assert.NotNull(captured);
        Assert.Equal(RowStatus.Rejected, captured!.Status);
        Assert.Equal("NON_PLACEABLE", captured.RejectCode);
        Assert.Same(row, captured.Row); // referensi yang sama, bukan salinan
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Properti struktural pada AnnotatedRow
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClassifyRecords_AnnotatedRowPreservesOriginalRowReference()
    {
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");
        var original = Row(footprint: "0603", name: "C42");

        var result = _sut.ClassifyRecords(new[] { original }).Single();

        // Row di AnnotatedRow harus referensi yang sama — bukan salinan deep copy
        Assert.Same(original, result.Row);
    }

    [Fact]
    public void ClassifyRecords_AcceptedRow_NormalizedIsPopulated()
    {
        SetupNormalizer("0603", NormalizedKind.StandardPackage, "PASSIVE");

        var result = _sut.ClassifyRecords(new[] { Row(footprint: "0603") }).Single();

        // Accepted row harus membawa data Normalized agar consumer bisa inspect
        Assert.NotNull(result.Normalized);
        Assert.Equal("0603", result.Normalized!.Raw);
    }

    [Fact]
    public void ClassifyRecords_RejectedByEmptyFootprint_NormalizedIsNull()
    {
        // Early-exit path tidak melalui normalizer → Normalized harus null
        var result = _sut.ClassifyRecords(new[] { Row(footprint: "") }).Single();

        Assert.Null(result.Normalized);
    }
}