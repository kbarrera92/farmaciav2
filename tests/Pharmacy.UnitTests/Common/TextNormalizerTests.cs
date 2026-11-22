using Pharmacy.Core.Common;

namespace Pharmacy.UnitTests.Common;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("ANALGÉSICOS Y ANTIPIRÉTICOS", "Analgésicos y antipiréticos")]
    [InlineData("  vitaminas  ", "Vitaminas")]
    [InlineData("pago POS", "Pago POS")]
    [InlineData("TARJETA BAC", "Tarjeta BAC")]
    [InlineData("zona 10 CC oakland", "Zona 10 CC oakland")]
    [InlineData("ÑANDÚ", "Ñandú")]
    // Known limit of the acronym rule: short words typed in upper case are kept.
    [InlineData("CAJA CHICA", "CAJA chica")]
    [InlineData("1ra avenida", "1ra avenida")]
    [InlineData("Uso   interno", "Uso   interno")]
    public void Sentence_case_capitalizes_only_the_first_letter_and_keeps_acronyms(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.ToSentenceCase(input));

    [Theory]
    [InlineData("KEVIN BARRERA", "Kevin Barrera")]
    [InlineData("josé ángel pérez-molina", "José Ángel Pérez-Molina")]
    [InlineData("farmacia ahorro S.A.", "Farmacia Ahorro S.A.")]
    [InlineData(" maría de los ángeles ", "María De Los Ángeles")]
    public void Title_case_capitalizes_each_word_and_keeps_acronyms(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.ToTitleCase(input));

    [Theory]
    [InlineData("zona 1, Ciudad de Guatemala", "Zona 1, Ciudad de Guatemala")]
    [InlineData(" 5a. calle 3-20 ", "5a. calle 3-20")]
    public void Capitalize_first_keeps_the_rest_as_written(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.CapitalizeFirst(input));

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("ACCESO TOTAL", "Acceso total")]
    public void Optional_values_become_null_when_blank(string? input, string? expected) =>
        Assert.Equal(expected, TextNormalizer.ToSentenceCaseOrNull(input));
}
