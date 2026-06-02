using AudioAnalyzer.Services;
using Xunit;

namespace AudioAnalyzer.Tests;

public class KeyDisplayServiceTests
{
    private readonly KeyDisplayService _service;

    public KeyDisplayServiceTests()
    {
        _service = new KeyDisplayService();
    }

    [Fact]
    public void GetKeyDisplayText_ReturnsDash_WhenInputIsEmpty()
    {
        var result = _service.GetKeyDisplayText("", "Major", -1, false);
        Assert.Equal("--", result);
    }

    [Fact]
    public void GetKeyDisplayText_ReturnsCorrectMajorKey_WhenRelativeIsFalse()
    {
        var result = _service.GetKeyDisplayText("C", "Major", 0, false);
        Assert.Equal("C Major", result);
    }

    [Fact]
    public void GetKeyDisplayText_ReturnsCorrectRelativeMinor_WhenInputIsMajor()
    {
        // C Major (0) -> A Minor (9)
        var result = _service.GetKeyDisplayText("C", "Major", 0, true);
        Assert.Equal("A Minor", result);
    }

    [Fact]
    public void GetKeyDisplayText_ReturnsCorrectRelativeMajor_WhenInputIsMinor()
    {
        // A Minor (9) -> C Major (0)
        var result = _service.GetKeyDisplayText("A", "Minor", 9, true);
        Assert.Equal("C Major", result);
    }

    [Fact]
    public void CalculateScaleNotes_ReturnsCorrectNotes_ForCMajor()
    {
        // C Major: C(0), D(2), E(4), F(5), G(7), A(9), B(11)
        var result = _service.CalculateScaleNotes(0, "Major", false);
        
        Assert.True(result[0]);
        Assert.True(result[2]);
        Assert.True(result[4]);
        Assert.True(result[5]);
        Assert.True(result[7]);
        Assert.True(result[9]);
        Assert.True(result[11]);
        
        Assert.False(result[1]);
        Assert.False(result[3]);
        Assert.False(result[6]);
        Assert.False(result[8]);
        Assert.False(result[10]);
    }
}
